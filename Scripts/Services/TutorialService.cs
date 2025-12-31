using SudokuSen.Models;

namespace SudokuSen.Services;

/// <summary>
/// Service that manages tutorial playback, including step progression,
/// animations, and user interaction validation.
/// </summary>
public partial class TutorialService : Node
{
    // Singleton access
    public static TutorialService? Instance { get; private set; }

    // Current tutorial state
    private TutorialData? _currentTutorial;
    private int _currentStepIndex = -1;
    private bool _isPlaying = false;
    private bool _isPaused = false;
    private bool _isWaitingForAction = false;

    // Timers for step automation
    private double _stepTimer = 0;
    private double _stepDuration = 0;

    // Events for UI updates (using simple types for Godot signal compatibility)
    [Signal] public delegate void TutorialStartedEventHandler(string tutorialId);
    [Signal] public delegate void TutorialEndedEventHandler(string tutorialId, bool completed);
    [Signal] public delegate void StepChangedEventHandler(int stepIndex, string stepType);
    [Signal] public delegate void MessageRequestedEventHandler(string message, string title, int position, string pointToJson);
    [Signal] public delegate void HighlightCellsRequestedEventHandler(int[] cellPositions, string style, string color);
    [Signal] public delegate void HighlightHouseRequestedEventHandler(string houseType, int index, string style);
    [Signal] public delegate void PointToElementRequestedEventHandler(string targetJson, string message);
    [Signal] public delegate void SimulateInputRequestedEventHandler(string inputType, int row, int col, int number, bool asNote);
    [Signal] public delegate void ClearHighlightsRequestedEventHandler();
    [Signal] public delegate void WaitingForActionEventHandler(string action, string hintMessage);

    // All available tutorials
    private readonly Dictionary<string, TutorialData> _tutorials = new();

    public bool IsPlaying => _isPlaying;
    public bool IsPaused => _isPaused;
    public TutorialData? CurrentTutorial => _currentTutorial;
    public int CurrentStepIndex => _currentStepIndex;
    public TutorialStep? CurrentStep => _currentTutorial != null && _currentStepIndex >= 0 && _currentStepIndex < _currentTutorial.Steps.Count
        ? _currentTutorial.Steps[_currentStepIndex]
        : null;

    /// <summary>
    /// Returns true if grid input (selecting cells, entering numbers) is currently allowed.
    /// During tutorials, grid input is only allowed when waiting for specific grid actions.
    /// </summary>
    public bool IsGridInputAllowed
    {
        get
        {
            // Not in a tutorial - allow all input
            if (!_isPlaying) return true;

            // Not waiting for action - block input
            if (!_isWaitingForAction) return false;

            // Check if the current step expects grid interaction
            var step = CurrentStep;
            if (step is ShowMessageStep msgStep && msgStep.WaitForAction.HasValue)
            {
                return IsGridRelatedAction(msgStep.WaitForAction.Value);
            }
            if (step is WaitForActionStep waitStep)
            {
                return IsGridRelatedAction(waitStep.Action);
            }

            return false;
        }
    }

    /// <summary>
    /// Checks if the expected action involves grid interaction.
    /// </summary>
    private static bool IsGridRelatedAction(ExpectedAction action)
    {
        return action switch
        {
            ExpectedAction.SelectCell => true,
            ExpectedAction.SelectMultipleCells => true,
            ExpectedAction.EnterCorrectNumber => true,
            ExpectedAction.EnterAnyNumber => true,
            ExpectedAction.EnterWrongNumber => true,
            ExpectedAction.AddNote => true,
            ExpectedAction.RemoveNote => true,
            ExpectedAction.ToggleNote => true,
            ExpectedAction.ToggleNoteMultiSelect => true,
            ExpectedAction.EraseCell => true,
            _ => false
        };
    }

    public override void _Ready()
    {
        Instance = this;
        RegisterBuiltInTutorials();
        GD.Print("[TutorialService] Ready - registered tutorials: " + string.Join(", ", _tutorials.Keys));
    }

    public override void _Process(double delta)
    {
        if (!_isPlaying || _isPaused || _isWaitingForAction) return;

        if (_stepTimer > 0)
        {
            _stepTimer -= delta * 1000; // Convert to ms
            if (_stepTimer <= 0)
            {
                // Auto-advance if current step doesn't wait for click
                var step = CurrentStep;
                if (step != null && !step.WaitForClick)
                {
                    AdvanceStep();
                }
            }
        }
    }

    /// <summary>
    /// Registers all built-in tutorials.
    /// </summary>
    private void RegisterBuiltInTutorials()
    {
        // Tutorial 1: Getting Started (Easy) - includes notes
        RegisterTutorial(CreateGettingStartedTutorial());

        // Tutorial 2: Basic Techniques (Medium)
        RegisterTutorial(CreateBasicTechniquesTutorial());

        // Tutorial 3: Advanced Features (Medium)
        RegisterTutorial(CreateAdvancedFeaturesTutorial());

        // Tutorial 4: Advanced Techniques (Hard)
        RegisterTutorial(CreateAdvancedTechniquesTutorial());

        // Tutorial 5: Challenge Modes (Hard)
        RegisterTutorial(CreateChallengeModesTutorial());
    }

    public void RegisterTutorial(TutorialData tutorial)
    {
        _tutorials[tutorial.Id] = tutorial;
    }

    public IEnumerable<TutorialData> GetAllTutorials()
    {
        return _tutorials.Values.OrderBy(t => t.Difficulty).ThenBy(t => t.Id);
    }

    public IEnumerable<TutorialData> GetTutorialsByDifficulty(TutorialDifficulty difficulty)
    {
        return _tutorials.Values.Where(t => t.Difficulty == difficulty);
    }

    public TutorialData? GetTutorial(string id)
    {
        return _tutorials.TryGetValue(id, out var tutorial) ? tutorial : null;
    }

    /// <summary>
    /// Starts playing a tutorial by ID.
    /// </summary>
    public void StartTutorial(string tutorialId)
    {
        if (!_tutorials.TryGetValue(tutorialId, out var tutorial))
        {
            GD.PrintErr($"[TutorialService] Tutorial not found: {tutorialId}");
            return;
        }

        StartTutorial(tutorial);
    }

    /// <summary>
    /// Starts playing a tutorial.
    /// </summary>
    public void StartTutorial(TutorialData tutorial)
    {
        _currentTutorial = tutorial;
        _currentStepIndex = -1;
        _isPlaying = true;
        _isPaused = false;
        _isWaitingForAction = false;

        GD.Print($"[TutorialService] Starting tutorial: {tutorial.Name}");
        EmitSignal(SignalName.TutorialStarted, tutorial.Id);

        // Start first step
        AdvanceStep();
    }

    /// <summary>
    /// Goes back to the previous step in the tutorial.
    /// </summary>
    public void PreviousStep()
    {
        if (_currentTutorial == null || !_isPlaying) return;
        if (_currentStepIndex <= 0)
        {
            // At first step, can't go back further
            return;
        }

        // Reset waiting state
        _isWaitingForAction = false;
        _waitingForShowMessageStep = null;
        EmitSignal(SignalName.ClearHighlightsRequested);

        // Find the previous interactive step (skip ClearHighlights, etc.)
        int targetIndex = _currentStepIndex - 1;
        while (targetIndex > 0)
        {
            var prevStep = _currentTutorial.Steps[targetIndex];
            // Stop at ShowMessageStep or WaitForActionStep
            if (prevStep is ShowMessageStep || prevStep is WaitForActionStep)
            {
                break;
            }
            targetIndex--;
        }

        _currentStepIndex = targetIndex - 1; // -1 because AdvanceStep will +1
        AdvanceStep();
    }

    /// <summary>
    /// Advances to the next step in the tutorial.
    /// </summary>
    public void AdvanceStep()
    {
        if (_currentTutorial == null || !_isPlaying) return;

        _currentStepIndex++;
        _isWaitingForAction = false;

        if (_currentStepIndex >= _currentTutorial.Steps.Count)
        {
            // Tutorial complete
            CompleteTutorial();
            return;
        }

        var step = _currentTutorial.Steps[_currentStepIndex];
        GD.Print($"[TutorialService] Step {_currentStepIndex + 1}/{_currentTutorial.Steps.Count}: {step.StepType}");

        // Handle delay
        if (step.DelayMs > 0)
        {
            _stepTimer = step.DelayMs;
            // Capture step locally to avoid race condition if tutorial is stopped during delay
            var capturedStep = step;
            var capturedStepIndex = _currentStepIndex;
            GetTree().CreateTimer(step.DelayMs / 1000.0).Timeout += () =>
            {
                // Only execute if we're still playing and on the same step
                if (_isPlaying && _currentStepIndex == capturedStepIndex)
                {
                    ExecuteStep(capturedStep);
                }
            };
            return;
        }

        ExecuteStep(step);
    }

    private void ExecuteStep(TutorialStep step)
    {
        EmitSignal(SignalName.StepChanged, _currentStepIndex, step.StepType);

        switch (step)
        {
            case ShowMessageStep msg:
                ExecuteShowMessage(msg);
                break;
            case HighlightCellsStep highlight:
                ExecuteHighlightCells(highlight);
                break;
            case HighlightHouseStep house:
                ExecuteHighlightHouse(house);
                break;
            case PointToElementStep point:
                ExecutePointToElement(point);
                break;
            case SimulateInputStep simulate:
                ExecuteSimulateInput(simulate);
                break;
            case WaitForActionStep wait:
                ExecuteWaitForAction(wait);
                break;
            case PauseStep pause:
                ExecutePause(pause);
                break;
            case ClearHighlightsStep clear:
                ExecuteClearHighlights(clear);
                break;
        }

        // Set timer for auto-advance
        if (!step.WaitForClick && step is not WaitForActionStep)
        {
            _stepTimer = step.DurationMs;
        }
    }

    private void ExecuteShowMessage(ShowMessageStep step)
    {
        // If there are cells to highlight, emit highlight signal first
        if (step.HighlightCells != null && step.HighlightCells.Count > 0)
        {
            var positions = step.HighlightCells.SelectMany(c => new[] { c.Row, c.Col }).ToArray();
            EmitSignal(SignalName.HighlightCellsRequested, positions, step.HighlightStyle.ToString(), "");
        }

        // Build the pointToJson - can be single target or multiple targets separated by ;
        string pointToJson = "";
        if (step.PointToMultiple != null && step.PointToMultiple.Count > 0)
        {
            // Multiple targets separated by semicolon
            pointToJson = string.Join(";", step.PointToMultiple.Select(t => SerializeTarget(t)));
        }
        else if (step.PointTo != null)
        {
            pointToJson = SerializeTarget(step.PointTo);
        }

        EmitSignal(SignalName.MessageRequested, step.Message, step.Title ?? "", (int)step.Position, pointToJson);

        // If this step also waits for an action, set up waiting
        if (step.WaitForAction.HasValue)
        {
            _isWaitingForAction = true;
            _waitingForShowMessageStep = step;
            EmitSignal(SignalName.WaitingForAction, step.WaitForAction.Value.ToString(), "");
        }
    }

    // Store reference to ShowMessageStep when waiting for action
    private ShowMessageStep? _waitingForShowMessageStep;

    private void ExecuteHighlightCells(HighlightCellsStep step)
    {
        // Convert cells to flat array: [row1, col1, row2, col2, ...]
        var positions = step.Cells.SelectMany(c => new[] { c.Row, c.Col }).ToArray();
        EmitSignal(SignalName.HighlightCellsRequested, positions, step.Style.ToString(), step.Color ?? "");
    }

    private void ExecuteHighlightHouse(HighlightHouseStep step)
    {
        EmitSignal(SignalName.HighlightHouseRequested, step.HouseType.ToString(), step.Index, step.Style.ToString());
    }

    private void ExecutePointToElement(PointToElementStep step)
    {
        string targetJson = SerializeTarget(step.Target);
        EmitSignal(SignalName.PointToElementRequested, targetJson, step.Message ?? "");
    }

    private void ExecuteSimulateInput(SimulateInputStep step)
    {
        int row = step.Cell?.Row ?? -1;
        int col = step.Cell?.Col ?? -1;
        int number = step.Number ?? -1;
        EmitSignal(SignalName.SimulateInputRequested, step.InputType.ToString(), row, col, number, step.AsNote);
    }

    private void ExecuteWaitForAction(WaitForActionStep step)
    {
        _isWaitingForAction = true;
        EmitSignal(SignalName.WaitingForAction, step.Action.ToString(), step.HintMessage ?? "");
    }

    private void ExecutePause(PauseStep step)
    {
        _stepTimer = step.PauseDurationMs;
    }

    private void ExecuteClearHighlights(ClearHighlightsStep step)
    {
        EmitSignal(SignalName.ClearHighlightsRequested);
    }

    /// <summary>
    /// Called when the user performs an action. Validates against expected action if waiting.
    /// </summary>
    public bool OnUserAction(ExpectedAction action, (int Row, int Col)? cell = null, int? number = null)
    {
        if (!_isWaitingForAction || _currentTutorial == null) return true;

        // Check if we're waiting on a ShowMessageStep with WaitForAction
        if (_waitingForShowMessageStep != null)
        {
            var msgStep = _waitingForShowMessageStep;
            bool isCorrect = msgStep.WaitForAction == action;

            if (isCorrect && msgStep.ExpectedCell.HasValue && cell.HasValue)
            {
                isCorrect = msgStep.ExpectedCell.Value == cell.Value;
            }

            if (isCorrect && msgStep.ExpectedNumber.HasValue && number.HasValue)
            {
                isCorrect = msgStep.ExpectedNumber.Value == number.Value;
            }

            if (isCorrect)
            {
                GD.Print($"[TutorialService] Correct action on ShowMessageStep: {action}");
                _waitingForShowMessageStep = null;
                AdvanceStep();
                return true;
            }
            return false;
        }

        return ValidateWaitForActionStep(action, cell, number);
    }

    /// <summary>
    /// Called when the user selects multiple cells. Validates against expected cells if waiting for multi-select.
    /// </summary>
    public bool OnMultiSelectAction(HashSet<(int row, int col)> selectedCells, int? number = null)
    {
        if (!_isWaitingForAction || _currentTutorial == null) return true;

        // Check if we're waiting on a ShowMessageStep with WaitForAction for multi-select
        if (_waitingForShowMessageStep != null)
        {
            var msgStep = _waitingForShowMessageStep;

            // For SelectMultipleCells action
            if (msgStep.WaitForAction == ExpectedAction.SelectMultipleCells && msgStep.ExpectedCells != null)
            {
                var expectedSet = new HashSet<(int, int)>(msgStep.ExpectedCells);
                if (selectedCells.SetEquals(expectedSet))
                {
                    GD.Print($"[TutorialService] Correct multi-select: {selectedCells.Count} cells");
                    _waitingForShowMessageStep = null;
                    AdvanceStep();
                    return true;
                }
                return false;
            }

            // For ToggleNoteMultiSelect action
            if (msgStep.WaitForAction == ExpectedAction.ToggleNoteMultiSelect && msgStep.ExpectedCells != null && number.HasValue)
            {
                var expectedSet = new HashSet<(int, int)>(msgStep.ExpectedCells);
                bool cellsMatch = selectedCells.SetEquals(expectedSet);
                bool numberMatches = !msgStep.ExpectedNumber.HasValue || msgStep.ExpectedNumber.Value == number.Value;

                if (cellsMatch && numberMatches)
                {
                    GD.Print($"[TutorialService] Correct multi-select note toggle: number {number} on {selectedCells.Count} cells");
                    _waitingForShowMessageStep = null;
                    AdvanceStep();
                    return true;
                }
                return false;
            }
        }

        return true;
    }

    private bool ValidateWaitForActionStep(ExpectedAction action, (int Row, int Col)? cell, int? number)
    {

        // Check for WaitForActionStep
        var step = CurrentStep as WaitForActionStep;
        if (step == null) return true;

        bool stepCorrect = step.Action == action;

        if (stepCorrect && step.ExpectedCell.HasValue && cell.HasValue)
        {
            stepCorrect = step.ExpectedCell.Value == cell.Value;
        }

        if (stepCorrect && step.ExpectedNumber.HasValue && number.HasValue)
        {
            stepCorrect = step.ExpectedNumber.Value == number.Value;
        }

        if (stepCorrect)
        {
            GD.Print($"[TutorialService] Correct action: {action}");
            AdvanceStep();
            return true;
        }
        else
        {
            GD.Print($"[TutorialService] Wrong action: {action} (expected {step.Action})");
            // Show wrong action message if defined
            if (!string.IsNullOrEmpty(step.WrongActionMessage))
            {
                EmitSignal(SignalName.MessageRequested, step.WrongActionMessage, "Hinweis", (int)MessagePosition.BottomCenter, "");
            }
            return false;
        }
    }

    /// <summary>
    /// Serializes a TutorialTarget to a simple string format for signal passing.
    /// Format: "Type|CellRow|CellCol|ButtonId"
    /// </summary>
    private string SerializeTarget(TutorialTarget target)
    {
        int cellRow = target.CellPosition?.Row ?? -1;
        int cellCol = target.CellPosition?.Col ?? -1;
        return $"{target.Type}|{cellRow}|{cellCol}|{target.ButtonId ?? ""}";
    }

    /// <summary>
    /// User clicked to advance (for steps with WaitForClick = true).
    /// </summary>
    public void OnUserClick()
    {
        if (!_isPlaying || _isPaused || _isWaitingForAction) return;

        var step = CurrentStep;
        if (step != null && step.WaitForClick)
        {
            AdvanceStep();
        }
    }

    /// <summary>
    /// Pauses the tutorial.
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
        GD.Print("[TutorialService] Paused");
    }

    /// <summary>
    /// Resumes the tutorial.
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
        GD.Print("[TutorialService] Resumed");
    }

    /// <summary>
    /// Skips to the next step.
    /// </summary>
    public void Skip()
    {
        if (_isPlaying)
        {
            _isWaitingForAction = false;
            AdvanceStep();
        }
    }

    /// <summary>
    /// Stops the tutorial.
    /// </summary>
    public void Stop()
    {
        if (_currentTutorial != null)
        {
            var id = _currentTutorial.Id;
            _currentTutorial = null;
            _currentStepIndex = -1;
            _isPlaying = false;
            _isPaused = false;
            _isWaitingForAction = false;

            EmitSignal(SignalName.ClearHighlightsRequested);
            EmitSignal(SignalName.TutorialEnded, id, false);
            GD.Print("[TutorialService] Stopped");
        }
    }

    private void CompleteTutorial()
    {
        if (_currentTutorial != null)
        {
            var id = _currentTutorial.Id;
            GD.Print($"[TutorialService] Completed: {_currentTutorial.Name}");

            _isPlaying = false;
            EmitSignal(SignalName.ClearHighlightsRequested);
            EmitSignal(SignalName.TutorialEnded, id, true);

            _currentTutorial = null;
            _currentStepIndex = -1;
        }
    }

    // ========================
    // Built-in Tutorial Definitions
    // ========================

    private TutorialData CreateGettingStartedTutorial()
    {
        var tutorial = new TutorialData
        {
            Id = "getting_started",
            Name = "Erste Schritte",
            Description = "Lerne die Benutzeroberfläche, Steuerung und Notizen kennen.",
            Difficulty = TutorialDifficulty.Easy,
            EstimatedMinutes = 6,
            // Pre-filled puzzle with only 5 cells remaining
            PuzzleData = "TUTORIAL_GETTING_STARTED"
        };

        tutorial.Steps = new List<TutorialStep>
        {
            // ========================================
            // PART 1: Welcome & UI Introduction
            // ========================================

            new ShowMessageStep
            {
                Title = "Tutorial: Erste Schritte",
                Message = "Willkommen bei SudokuSen!\n\nIn diesem Tutorial lernst du die Benutzeroberfläche und grundlegende Steuerung kennen.\n\nDas Puzzle ist fast fertig – nur noch 5 Zellen fehlen!\n\n👆 Klicke auf \"Weiter\" um fortzufahren.",
                Position = MessagePosition.CenterLeft
            },

            // Show the grid - point to edge, not center
            new ShowMessageStep
            {
                Title = "📋 Das Spielfeld",
                Message = "Das ist das Sudoku-Spielfeld.\n\n• 9×9 Zellen, aufgeteilt in 9 Blöcke (3×3)\n• Jede Zahl 1-9 darf in jeder Zeile, Spalte und jedem Block nur EINMAL vorkommen\n• Graue Zahlen sind vorgegeben und können nicht geändert werden",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.GridEdge }
            },

            // Show axis labels - point to both "A" column and "1" row
            new ShowMessageStep
            {
                Title = "🔤 Achsenbeschriftung",
                Message = "Oben siehst du Spalten A-I, links die Zeilen 1-9.\n\nSo kannst du Zellen eindeutig benennen:\n• E5 = Spalte E, Zeile 5 (die Mitte!)\n• A1 = oben links\n• I9 = unten rechts\n\nDas ist praktisch beim Besprechen von Zügen!",
                Position = MessagePosition.CenterLeft,
                PointToMultiple = new List<TutorialTarget>
                {
                    new TutorialTarget { Type = TargetType.ColumnLabel, ButtonId = "A" },
                    new TutorialTarget { Type = TargetType.RowLabel, ButtonId = "1" }
                }
            },

            // Show back button
            new ShowMessageStep
            {
                Title = "← Zurück-Button",
                Message = "Mit diesem Button kehrst du zum Hauptmenü zurück.\n\n💾 Keine Sorge: Dein Spielstand wird automatisch gespeichert!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.BackButton }
            },

            // Show difficulty BEFORE timer
            new ShowMessageStep
            {
                Title = "📊 Schwierigkeit",
                Message = "Die aktuelle Schwierigkeitsstufe:\n\n• 🟢 Kids (4×4)\n• 🟢 Leicht\n• 🟠 Mittel\n• 🔴 Schwer",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.DifficultyLabel }
            },

            // Show timer
            new ShowMessageStep
            {
                Title = "⏱️ Timer",
                Message = "Hier siehst du die verstrichene Spielzeit.\n\nDie Zeit läuft automatisch, sobald du spielst.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.Timer }
            },

            // Show mistakes counter
            new ShowMessageStep
            {
                Title = "❌ Fehlerzähler",
                Message = "Hier werden deine Fehler gezählt.\n\n⚠️ WICHTIG: Im \"Deadly Modus\" (in den Einstellungen aktivierbar) endet das Spiel nach 3 Fehlern!\n\nFür dieses Tutorial ist der Deadly Modus deaktiviert.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.MistakesLabel }
            },

            // ========================================
            // PART 2: Selecting Cells & Entering Numbers
            // ========================================

            // Step 1: Select the cell
            new ShowMessageStep
            {
                Title = "🎯 Zelle auswählen",
                Message = "Lass uns eine Zelle ausfüllen!\n\nSiehst du die pulsierende Zelle E5 in der Mitte?\n\n👆 Klicke darauf!",
                Position = MessagePosition.CenterLeft,
                HighlightCells = new List<(int, int)> { (4, 4) },
                HighlightStyle = HighlightStyle.Pulse,
                PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (4, 4) },
                WaitForAction = ExpectedAction.SelectCell,
                ExpectedCell = (4, 4)
            },

            new ClearHighlightsStep(),

            // Step 2: Enter the correct number directly (skip the "wrong number" experiment)
            new ShowMessageStep
            {
                Title = "🔢 Zahl eingeben",
                Message = "Die Zelle E5 ist ausgewählt (blau).\n\nJetzt gib die richtige Zahl ein!\n\n🔍 Tipp: Schau welche Zahlen schon in Zeile 5, Spalte E und dem mittleren Block sind.\n\n💡 Die Lösung ist die 5!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "5" },
                WaitForAction = ExpectedAction.EnterCorrectNumber,
                ExpectedCell = (4, 4),
                ExpectedNumber = 5
            },

            new ClearHighlightsStep(),

            // Step 3: Success message
            new ShowMessageStep
            {
                Title = "🎉 Perfekt!",
                Message = "Sehr gut! Du hast die richtige Zahl gefunden.\n\nJetzt lernst du NOTIZEN kennen - ein wichtiges Werkzeug!",
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // PART 3: Notes - Interactive Practice
            // ========================================

            new ShowMessageStep
            {
                Title = "📝 Notizen-Modus",
                Message = "Manchmal bist du nicht sicher, welche Zahl passt.\n\nDafür gibt es den Notizen-Modus!\n\n👆 Klicke auf den Notizen-Button oder drücke 'N'.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NotesToggle },
                WaitForAction = ExpectedAction.ToggleNotesMode
            },

            // Select a cell for notes practice - (0,2) has solution 4
            new ShowMessageStep
            {
                Title = "📝 Notiz setzen",
                Message = "Super! Du bist im Notizen-Modus.\n\nJetzt wähle die Zelle C1 (oben, dritte Spalte) aus.\n\n👆 Klicke auf die pulsierende Zelle!",
                Position = MessagePosition.CenterLeft,
                HighlightCells = new List<(int, int)> { (0, 2) },
                HighlightStyle = HighlightStyle.Pulse,
                PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (0, 2) },
                WaitForAction = ExpectedAction.SelectCell,
                ExpectedCell = (0, 2)
            },

            new ClearHighlightsStep(),

            // Add a note
            new ShowMessageStep
            {
                Title = "📝 Notiz hinzufügen",
                Message = "Gib jetzt die Zahl 4 ein.\n\nIm Notizen-Modus wird sie als kleine Notiz angezeigt!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "4" },
                WaitForAction = ExpectedAction.ToggleNote,
                ExpectedCell = (0, 2),
                ExpectedNumber = 4
            },

            // Toggle it off
            new ShowMessageStep
            {
                Title = "📝 Notiz entfernen",
                Message = "Die 4 ist jetzt als Notiz sichtbar!\n\n👆 Drücke nochmal 4 um sie zu entfernen (Toggle).",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "4" },
                WaitForAction = ExpectedAction.ToggleNote,
                ExpectedCell = (0, 2),
                ExpectedNumber = 4
            },

            // Show eraser alternative
            new ShowMessageStep
            {
                Title = "🗑️ Radiergummi",
                Message = "Du kannst Notizen auch mit dem Radiergummi löschen!\n\n⌨️ Oder drücke: Entf / Backspace / 0\n\nDer Radiergummi löscht ALLE Notizen der Zelle.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.EraseButton }
            },

            // ========================================
            // PART 4: Multi-Select with Notes (Interactive)
            // ========================================

            new ShowMessageStep
            {
                Title = "🔲 Mehrfachauswahl",
                Message = "Jetzt probieren wir Mehrfachauswahl!\n\n• Strg + Klick → Zellen hinzufügen\n• Shift + Klick → Bereich auswählen\n\nDu bist noch im Notizen-Modus - perfekt!",
                Position = MessagePosition.CenterLeft
            },

            // Select first cell for multi-select
            new ShowMessageStep
            {
                Title = "🔲 Erste Zelle wählen",
                Message = "Wähle zuerst Zelle G3 aus.\n\n👆 Klicke auf die pulsierende Zelle!",
                Position = MessagePosition.CenterLeft,
                HighlightCells = new List<(int, int)> { (2, 6) },
                HighlightStyle = HighlightStyle.Pulse,
                PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (2, 6) },
                WaitForAction = ExpectedAction.SelectCell,
                ExpectedCell = (2, 6)
            },

            new ClearHighlightsStep(),

            // Add second cell with Ctrl+Click
            new ShowMessageStep
            {
                Title = "🔲 Zweite Zelle (Strg+Klick)",
                Message = "Halte Strg gedrückt und klicke auf B7.\n\nDamit fügst du die Zelle zur Auswahl hinzu!",
                Position = MessagePosition.CenterLeft,
                HighlightCells = new List<(int, int)> { (6, 1) },
                HighlightStyle = HighlightStyle.Pulse,
                PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (6, 1) },
                WaitForAction = ExpectedAction.SelectMultipleCells,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1) }
            },

            new ClearHighlightsStep(),

            // Add note 3 to both cells
            new ShowMessageStep
            {
                Title = "🔲 Notiz für beide",
                Message = "Beide Zellen sind markiert (blau umrandet).\n\nGib jetzt 3 ein - die Notiz wird in BEIDEN Zellen gesetzt!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "3" },
                WaitForAction = ExpectedAction.ToggleNoteMultiSelect,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1) },
                ExpectedNumber = 3
            },

            // Add third cell with Ctrl+Click (this cell doesn't have 3 yet)
            new ShowMessageStep
            {
                Title = "🔲 Dritte Zelle (Strg+Klick)",
                Message = "Füge jetzt Zelle I9 hinzu.\n\nHalte Strg gedrückt und klicke darauf!",
                Position = MessagePosition.CenterLeft,
                HighlightCells = new List<(int, int)> { (8, 8) },
                HighlightStyle = HighlightStyle.Pulse,
                PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (8, 8) },
                WaitForAction = ExpectedAction.SelectMultipleCells,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1), (8, 8) }
            },

            new ClearHighlightsStep(),

            // Smart toggle - adds 3 only to I9 (G3 and B7 already have it)
            new ShowMessageStep
            {
                Title = "🔲 Smart Toggle",
                Message = "Drücke 3.\n\nG3 und B7 haben schon die 3, nur I9 bekommt sie neu!\n\n💡 Notizen werden nur dort gesetzt, wo sie noch fehlen.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "3" },
                WaitForAction = ExpectedAction.ToggleNoteMultiSelect,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1), (8, 8) },
                ExpectedNumber = 3
            },

            // Remove from all three (now all have it)
            new ShowMessageStep
            {
                Title = "🔲 Alle entfernen",
                Message = "Drücke 3 nochmal.\n\nJetzt haben ALLE drei die Notiz → sie wird aus allen entfernt!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "3" },
                WaitForAction = ExpectedAction.ToggleNoteMultiSelect,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1), (8, 8) },
                ExpectedNumber = 3
            },

            // Exit notes mode
            new ShowMessageStep
            {
                Title = "📝 Fertig!",
                Message = "Klicke auf den Notizen-Button um den Modus zu beenden.\n\n💡 Tipp: Shift+Klick wählt einen ganzen Bereich!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NotesToggle },
                WaitForAction = ExpectedAction.ToggleNotesMode
            },

            // ========================================
            // PART 5: Helper Buttons
            // ========================================

            new ShowMessageStep
            {
                Title = "🛠️ Hilfreiche Buttons",
                Message = "SudokuSen hat mehrere praktische Hilfsfunktionen.\n\nLass uns sie kennenlernen!",
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = "💡 Hinweis-Button",
                Message = "Brauchst du Hilfe?\n\nDer Hinweis-Button zeigt dir den nächsten logischen Schritt mit Erklärung!\n\n📚 Perfekt zum Lernen neuer Lösungstechniken.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HintButton }
            },

            new ShowMessageStep
            {
                Title = "✨ Auto-Notizen",
                Message = "Dieser Button füllt automatisch ALLE möglichen Kandidaten in leere Zellen ein.\n\n💡 Sehr praktisch für Anfänger!\n\n⚠️ Achtung: Bei schweren Puzzles können das viele Notizen sein.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.AutoNotesButton }
            },

            new ShowMessageStep
            {
                Title = "🔤 R/C/B Button",
                Message = "Dieser Button füllt Notizen für Zeile (R), Spalte (C) oder Block (B) aus.\n\n👆 Rechtsklick: Modus wechseln (R→C→B)\n👆 Linksklick: Notizen für ausgewählte Zelle(n) setzen\n\n💡 Funktioniert auch bei Mehrfachauswahl!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HouseAutoFillButton }
            },

            new ShowMessageStep
            {
                Title = "🗑️ Radiergummi",
                Message = "Der Radiergummi löscht:\n\n• Die Zahl in der ausgewählten Zelle\n• ALLE Notizen in der Zelle\n\n⌨️ Alternativ: Entf oder Rücktaste",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.EraseButton }
            },

            // ========================================
            // PART 6: Completion
            // ========================================

            new ShowMessageStep
            {
                Title = "🎓 Tutorial abgeschlossen!",
                Message = "Glückwunsch! Du kennst jetzt die Grundlagen von SudokuSen.\n\n📋 Zusammenfassung:\n• Zellen auswählen & Zahlen eingeben\n• Fehler werden rot markiert\n• Notizen für Kandidaten nutzen\n• Hilfsfunktionen bei Bedarf\n\n🎮 Viel Spaß beim Rätseln!",
                Position = MessagePosition.CenterLeft
            }
        };

        return tutorial;
    }

    private TutorialData CreateBasicTechniquesTutorial()
    {
        var tutorial = new TutorialData
        {
            Id = "basic_techniques",
            Name = "Grundtechniken",
            Description = "Naked Single, Hidden Single und mehr.",
            Difficulty = TutorialDifficulty.Medium,
            EstimatedMinutes = 8,
            PuzzleData = "TUTORIAL_BASIC_TECHNIQUES"
        };

        tutorial.Steps = new List<TutorialStep>
        {
            // ========================================
            // INTRO
            // ========================================

            new ShowMessageStep
            {
                Title = "Tutorial: Grundtechniken",
                Message = "Willkommen zum Technik-Tutorial!\n\nHier lernst du die beiden wichtigsten Grundtechniken:\n\n• 🎯 Naked Single\n• 🔍 Hidden Single\n\nMit diesen Techniken lassen sich die meisten leichten und mittleren Puzzles lösen!",
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // NAKED SINGLE EXPLANATION
            // ========================================

            new ShowMessageStep
            {
                Title = "🎯 Naked Single",
                Message = "Eine Zelle hat nur EINE mögliche Zahl.\n\nWarum? Weil alle anderen Zahlen (1-9) bereits in:\n• derselben Zeile ODER\n• derselben Spalte ODER\n• demselben 3×3-Block\nvorkommen.\n\n💡 Auch genannt: \"Sole Candidate\"",
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = "🎯 Naked Single finden",
                Message = "So findest du einen Naked Single:\n\n1. Wähle eine leere Zelle\n2. Prüfe welche Zahlen in der Zeile sind\n3. Prüfe welche Zahlen in der Spalte sind\n4. Prüfe welche Zahlen im Block sind\n5. Nur EINE Zahl übrig? → Das ist die Lösung!",
                Position = MessagePosition.CenterLeft
            },

            // Interactive: Find and enter a Naked Single
            new ShowMessageStep
            {
                Title = "🎯 Probiere es aus!",
                Message = "Sieh dir Zelle E5 (Mitte) an.\n\nDie pulsierende Zelle hat nur EINE mögliche Zahl.\n\n👆 Wähle sie aus!",
                Position = MessagePosition.CenterLeft,
                HighlightCells = new List<(int, int)> { (4, 4) },
                HighlightStyle = HighlightStyle.Pulse,
                PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (4, 4) },
                WaitForAction = ExpectedAction.SelectCell,
                ExpectedCell = (4, 4)
            },

            new ClearHighlightsStep(),

            new ShowMessageStep
            {
                Title = "🎯 Analyse",
                Message = "Schau dir Zeile 5, Spalte E und den mittleren Block an.\n\nWelche Zahlen fehlen noch?\n\n✅ Nur die 5 kann hier stehen!\n\n👆 Gib 5 ein.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "5" },
                WaitForAction = ExpectedAction.EnterCorrectNumber,
                ExpectedCell = (4, 4),
                ExpectedNumber = 5
            },

            new ClearHighlightsStep(),

            new ShowMessageStep
            {
                Title = "🎉 Perfekt!",
                Message = "Das war ein Naked Single!\n\n💡 Der Hinweis-Button zeigt dir solche Techniken automatisch.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HintButton }
            },

            // ========================================
            // HIDDEN SINGLE EXPLANATION
            // ========================================

            new ShowMessageStep
            {
                Title = "🔍 Hidden Single",
                Message = "Eine Zahl kann nur an EINER Stelle in einer Zeile, Spalte oder Block stehen.\n\nDie Zelle selbst hat vielleicht mehrere Kandidaten - aber diese spezielle Zahl kann NUR hier hin!\n\n💡 Auch genannt: \"Unique Candidate\"",
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = "🔍 Drei Varianten",
                Message = "Hidden Single gibt es in drei Varianten:\n\n📏 In der Zeile: Die Zahl kann nur in EINER Zelle der Zeile stehen\n\n📐 In der Spalte: Die Zahl kann nur in EINER Zelle der Spalte stehen\n\n📦 Im Block: Die Zahl kann nur in EINER Zelle des 3×3-Blocks stehen",
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = "🔍 Hidden Single finden",
                Message = "So findest du einen Hidden Single:\n\n1. Wähle eine Zahl (z.B. 4)\n2. Wähle eine Einheit (Zeile, Spalte, Block)\n3. Finde alle Zellen wo diese Zahl hin könnte\n4. Nur EINE Stelle möglich? → Hidden Single!",
                Position = MessagePosition.CenterLeft
            },

            // Interactive: Find and enter a Hidden Single
            new ShowMessageStep
            {
                Title = "🔍 Probiere es aus!",
                Message = "Schau dir Zelle C1 an.\n\nDiese Zelle hat mehrere Kandidaten, ABER: Im ersten 3×3-Block (oben links) kann die 4 NUR hier stehen!\n\n👆 Wähle die Zelle aus.",
                Position = MessagePosition.CenterLeft,
                HighlightCells = new List<(int, int)> { (0, 2) },
                HighlightStyle = HighlightStyle.Pulse,
                PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (0, 2) },
                WaitForAction = ExpectedAction.SelectCell,
                ExpectedCell = (0, 2)
            },

            new ClearHighlightsStep(),

            new ShowMessageStep
            {
                Title = "🔍 Warum hier?",
                Message = "Schau dir den oberen linken 3×3-Block an.\n\nPrüfe jede leere Zelle: Kann die 4 dort stehen?\n\nDie 4 wird durch andere Zeilen und Spalten blockiert - nur C1 bleibt!\n\n👆 Gib 4 ein.",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "4" },
                WaitForAction = ExpectedAction.EnterCorrectNumber,
                ExpectedCell = (0, 2),
                ExpectedNumber = 4
            },

            new ClearHighlightsStep(),

            new ShowMessageStep
            {
                Title = "🎉 Ausgezeichnet!",
                Message = "Das war ein Hidden Single im Block!\n\nDer Unterschied zu Naked Single:\n• Naked Single: Zelle hat nur 1 Kandidat\n• Hidden Single: Zahl hat nur 1 mögliche Zelle",
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // USING THE HINT BUTTON
            // ========================================

            new ShowMessageStep
            {
                Title = "💡 Hinweis-Button nutzen",
                Message = "Der Hinweis-Button findet automatisch die nächste Technik!\n\nEr zeigt dir:\n• Welche Technik\n• Welche Zelle\n• Welche Zahl\n• Eine Erklärung\n\n👆 Klicke auf den Hinweis-Button!",
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HintButton },
                WaitForAction = ExpectedAction.ClickButton
            },

            new ShowMessageStep
            {
                Title = "📊 Zusammenfassung",
                Message = "Du kennst jetzt die zwei wichtigsten Techniken:\n\n🎯 Naked Single\n• Zelle hat nur 1 Kandidat\n• \"Diese Zelle MUSS X sein\"\n\n🔍 Hidden Single\n• Zahl hat nur 1 mögliche Zelle\n• \"X MUSS hier hin\"\n\n💡 Mit Notizen werden diese Techniken noch einfacher zu finden!",
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // COMPLETION
            // ========================================

            new ShowMessageStep
            {
                Title = "🎓 Tutorial abgeschlossen!",
                Message = "Glückwunsch! Du kennst jetzt die Grundtechniken.\n\n📚 Nächste Schritte:\n• Übe mit leichten Puzzles\n• Nutze Auto-Notizen für Übersicht\n• Der Hinweis-Button erklärt jeden Schritt\n\n🎮 Viel Erfolg beim Üben!",
                Position = MessagePosition.CenterLeft
            }
        };

        return tutorial;
    }

    private TutorialData CreateAdvancedFeaturesTutorial()
    {
        var tutorial = new TutorialData
        {
            Id = "advanced_features",
            Name = "Erweiterte Funktionen",
            Description = "Auto-Fill, Mehrfachauswahl, R/C/B und Tastaturkürzel.",
            Difficulty = TutorialDifficulty.Medium,
            EstimatedMinutes = 10,
            PuzzleData = "TUTORIAL_ADVANCED",
            Steps = new List<TutorialStep>
            {
                // ========================================
                // INTRO
                // ========================================

                new ShowMessageStep
                {
                    Title = "Tutorial: Erweiterte Funktionen",
                    Message = "In diesem Tutorial lernst du die fortgeschrittenen Funktionen von SudokuSen kennen:\n\n• ✨ Auto-Notizen\n• 🔤 R/C/B-Button\n• 🔲 Bereichsauswahl\n• ⌨️ Tastaturkürzel\n• 🎨 Highlighting\n\nDiese Funktionen machen dich zum Profi!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // AUTO-NOTES DEEP DIVE
                // ========================================

                new ShowMessageStep
                {
                    Title = "✨ Auto-Notizen",
                    Message = "Der Auto-Notizen-Button füllt automatisch alle möglichen Kandidaten in ALLE leeren Zellen ein.\n\n💡 Sehr nützlich am Anfang eines Puzzles!\n\n⚠️ Bei schweren Puzzles können das viele Notizen sein - keine Sorge, das ist normal.",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.AutoNotesButton }
                },

                new ShowMessageStep
                {
                    Title = "✨ Probiere es aus!",
                    Message = "Klicke auf den Auto-Notizen-Button.\n\nAlle leeren Zellen werden mit ihren möglichen Kandidaten gefüllt!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.AutoNotesButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                new ShowMessageStep
                {
                    Title = "✨ Ergebnis analysieren",
                    Message = "Siehst du die kleinen Zahlen in den leeren Zellen?\n\nDas sind alle Kandidaten, die dort theoretisch möglich sind.\n\n💡 Achte auf Zellen mit wenigen Kandidaten - dort findest du oft Naked Singles!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "✨ Wann Auto-Notizen nutzen?",
                    Message = "Auto-Notizen sind ideal für:\n\n✅ Puzzles ab \"Mittel\" Schwierigkeit\n✅ Wenn du fortgeschrittene Techniken üben willst\n✅ Um einen Überblick zu bekommen\n\n❌ Nicht nötig bei \"Leicht\" - dort reichen einfache Techniken",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // R/C/B BUTTON DEEP DIVE
                // ========================================

                new ShowMessageStep
                {
                    Title = "🔤 Der R/C/B-Button",
                    Message = "Dieser Button füllt Notizen nur für bestimmte Bereiche:\n\n• R = Row (Zeile)\n• C = Column (Spalte)\n• B = Block\n\n💡 Perfekt wenn du nur einen Teil des Puzzles analysieren willst!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HouseAutoFillButton }
                },

                new ShowMessageStep
                {
                    Title = "🔤 Zelle auswählen",
                    Message = "Wähle zuerst eine Zelle in der Mitte aus.\n\nDer R/C/B-Button arbeitet dann mit der Zeile, Spalte oder dem Block dieser Zelle.\n\n👆 Klicke auf E5!",
                    Position = MessagePosition.CenterLeft,
                    HighlightCells = new List<(int, int)> { (4, 4) },
                    HighlightStyle = HighlightStyle.Pulse,
                    PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (4, 4) },
                    WaitForAction = ExpectedAction.SelectCell,
                    ExpectedCell = (4, 4)
                },

                new ClearHighlightsStep(),

                new ShowMessageStep
                {
                    Title = "🔤 Modus wechseln",
                    Message = "Der Button zeigt den aktuellen Modus an:\n\n• ▶ Row → Zeile 5\n• ▶ Col → Spalte E\n• ▶ Block → Mittlerer Block\n\n👆 Mit RECHTSKLICK wechselst du den Modus.\n👆 Mit LINKSKLICK führst du die Aktion aus.",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HouseAutoFillButton }
                },

                new ShowMessageStep
                {
                    Title = "🔤 Praktischer Einsatz",
                    Message = "Wann ist R/C/B besser als Auto-Notizen?\n\n✅ Du willst nur einen Bereich analysieren\n✅ Du hast schon Notizen und willst sie aktualisieren\n✅ Du arbeitest systematisch Zeile für Zeile\n\n💡 Profi-Tipp: Kombiniere mit Mehrfachauswahl!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // RANGE SELECTION (SHIFT+CLICK)
                // ========================================

                new ShowMessageStep
                {
                    Title = "🔲 Bereichsauswahl",
                    Message = "Mit Shift+Klick kannst du einen rechteckigen Bereich auswählen!\n\nDas ist extrem nützlich für:\n• Schnelles Setzen von Notizen\n• Löschen mehrerer Zellen\n• Analyse eines Blocks",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🔲 So geht's",
                    Message = "1. Klicke auf die erste Ecke (z.B. A1)\n2. Halte Shift gedrückt\n3. Klicke auf die gegenüberliegende Ecke (z.B. C3)\n\n→ Alle 9 Zellen dazwischen werden markiert!\n\n💡 Funktioniert auch diagonal über mehrere Blöcke.",
                    Position = MessagePosition.CenterLeft,
                    HighlightCells = new List<(int, int)> { (0, 0), (2, 2) },
                    HighlightStyle = HighlightStyle.Pulse
                },

                new ShowMessageStep
                {
                    Title = "🔲 Erste Zelle wählen",
                    Message = "Wähle Zelle A1 aus (oben links).\n\n👆 Klicke darauf!",
                    Position = MessagePosition.CenterLeft,
                    HighlightCells = new List<(int, int)> { (0, 0) },
                    HighlightStyle = HighlightStyle.Pulse,
                    PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (0, 0) },
                    WaitForAction = ExpectedAction.SelectCell,
                    ExpectedCell = (0, 0)
                },

                new ClearHighlightsStep(),

                new ShowMessageStep
                {
                    Title = "🔲 Bereich erweitern",
                    Message = "Halte jetzt Shift und klicke auf C3.\n\nDer gesamte obere linke Block (9 Zellen) wird markiert!",
                    Position = MessagePosition.CenterLeft,
                    HighlightCells = new List<(int, int)> { (2, 2) },
                    HighlightStyle = HighlightStyle.Pulse,
                    PointTo = new TutorialTarget { Type = TargetType.Cell, CellPosition = (2, 2) },
                    WaitForAction = ExpectedAction.SelectMultipleCells,
                    ExpectedCells = new List<(int, int)> { (0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2), (2, 0), (2, 1), (2, 2) }
                },

                new ClearHighlightsStep(),

                new ShowMessageStep
                {
                    Title = "🔲 Was kann ich damit tun?",
                    Message = "Mit einem ausgewählten Bereich kannst du:\n\n📝 Im Notizen-Modus: Notiz in ALLEN Zellen setzen/entfernen\n🗑️ Mit Radiergummi: ALLE Zellen leeren\n🔤 Mit R/C/B: Notizen für den Bereich setzen\n\n💡 Spart enorm viel Zeit!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // CTRL+CLICK MULTI-SELECT
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎯 Strg+Klick Auswahl",
                    Message = "Mit Strg+Klick wählst du einzelne Zellen aus - auch wenn sie nicht nebeneinander liegen!\n\n💡 Perfekt für:\n• Alle Zellen mit einer bestimmten Notiz\n• Zellen in verschiedenen Blöcken\n• Gezielte Bearbeitung",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🎯 Kombination",
                    Message = "Du kannst Shift und Strg kombinieren!\n\n1. Shift+Klick für ersten Bereich\n2. Strg+Shift+Klick für weiteren Bereich\n3. Strg+Klick für einzelne Zellen\n\n💡 So wählst du komplexe Muster aus!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // KEYBOARD SHORTCUTS
                // ========================================

                new ShowMessageStep
                {
                    Title = "⌨️ Tastaturkürzel",
                    Message = "Für schnelles Spielen gibt es viele Tastaturkürzel:\n\n• 1-9 → Zahl eingeben\n• N → Notizen-Modus umschalten\n• Entf/Backspace/0 → Zelle löschen\n• Pfeiltasten → Zelle wechseln\n• H → Hinweis anfordern",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "⌨️ Navigation",
                    Message = "Schnelle Navigation:\n\n• ↑↓←→ → Zur nächsten Zelle\n• Strg + ↑↓←→ → Zur nächsten LEEREN Zelle\n• Home → Zu A1 springen\n• End → Zu I9 springen\n\n💡 Für Profis: Nie die Maus benutzen!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "⌨️ Mehrfachauswahl per Tastatur",
                    Message = "Auch die Tastatur unterstützt Mehrfachauswahl:\n\n• Shift + Pfeiltasten → Bereich erweitern\n• Strg + Shift + Pfeiltasten → Bis zur nächsten leeren Zelle\n\n💡 Kombiniere mit Zahlen für Turbo-Eingabe!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // HIGHLIGHTING FEATURE
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎨 Highlighting",
                    Message = "Wenn du eine Zahl eingibst, werden alle gleichen Zahlen hervorgehoben!\n\n💡 Das hilft dir:\n• Zu sehen wo eine Zahl schon ist\n• Fehler zu erkennen\n• Muster zu finden",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🎨 Highlighting nutzen",
                    Message = "Klicke auf eine Zelle mit einer Zahl.\n\nAlle anderen Zellen mit der gleichen Zahl werden hervorgehoben!\n\n💡 Sehr nützlich für Hidden Singles - du siehst sofort wo die Zahl noch fehlt.",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // COMPLETION
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎓 Tutorial abgeschlossen!",
                    Message = "Du kennst jetzt alle erweiterten Funktionen:\n\n✨ Auto-Notizen für schnellen Start\n🔤 R/C/B für gezielte Notizen\n🔲 Shift+Klick für Bereiche\n🎯 Strg+Klick für einzelne Zellen\n⌨️ Tastaturkürzel für Profis\n🎨 Highlighting für Übersicht\n\n🎮 Du bist bereit für schwere Puzzles!",
                    Position = MessagePosition.CenterLeft
                }
            }
        };

        return tutorial;
    }

    private TutorialData CreateAdvancedTechniquesTutorial()
    {
        var tutorial = new TutorialData
        {
            Id = "advanced_techniques",
            Name = "Fortgeschrittene Techniken",
            Description = "Pairs, Pointing, Box/Line, X-Wing und mehr.",
            Difficulty = TutorialDifficulty.Hard,
            EstimatedMinutes = 15,
            PuzzleData = "TUTORIAL_ADVANCED_TECHNIQUES",
            Steps = new List<TutorialStep>
            {
                // ========================================
                // INTRO
                // ========================================

                new ShowMessageStep
                {
                    Title = "Tutorial: Fortgeschrittene Techniken",
                    Message = "Willkommen zum Experten-Tutorial!\n\nHier lernst du die Techniken für mittlere und schwere Puzzles:\n\n• 👯 Naked & Hidden Pairs\n• 👉 Pointing Pairs\n• 📦 Box/Line Reduction\n• ✈️ X-Wing\n\nDiese Techniken sind essentiell für schwere Rätsel!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "📝 Voraussetzungen",
                    Message = "Bevor wir beginnen:\n\n✅ Du solltest Naked Single kennen\n✅ Du solltest Hidden Single kennen\n✅ Du solltest mit Notizen arbeiten können\n\n💡 Falls nicht, mache zuerst die Tutorials \"Grundtechniken\" und \"Erweiterte Funktionen\"!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // NAKED PAIR
                // ========================================

                new ShowMessageStep
                {
                    Title = "👯 Naked Pair",
                    Message = "Ein Naked Pair sind zwei Zellen in derselben Einheit (Zeile, Spalte oder Block), die GENAU die gleichen zwei Kandidaten haben.\n\nBeispiel:\n• Zelle A hat Kandidaten {3, 7}\n• Zelle B hat Kandidaten {3, 7}\n\n→ Die 3 und 7 MÜSSEN in diesen beiden Zellen sein!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👯 Naked Pair Elimination",
                    Message = "Was bedeutet das?\n\nWenn zwei Zellen nur {3, 7} haben können:\n→ KEINE andere Zelle in dieser Einheit kann 3 oder 7 sein!\n\n💡 Du kannst 3 und 7 aus allen anderen Zellen der Einheit entfernen.",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👯 Naked Pair finden",
                    Message = "So findest du Naked Pairs:\n\n1. Suche Zellen mit genau 2 Kandidaten\n2. Prüfe ob eine andere Zelle in derselben Einheit die GLEICHEN 2 Kandidaten hat\n3. Wenn ja → Naked Pair gefunden!\n4. Entferne diese Kandidaten aus allen anderen Zellen der Einheit",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👯 Probiere es aus!",
                    Message = "Nutze den Hinweis-Button um ein Naked Pair zu finden.\n\nDer Hinweis zeigt dir:\n• Wo das Pair ist\n• Welche Kandidaten betroffen sind\n• Welche Eliminierungen möglich sind\n\n👆 Klicke auf den Hinweis-Button!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                // ========================================
                // HIDDEN PAIR
                // ========================================

                new ShowMessageStep
                {
                    Title = "🔍 Hidden Pair",
                    Message = "Ein Hidden Pair ist schwerer zu finden!\n\nZwei Kandidaten kommen NUR in genau zwei Zellen einer Einheit vor - aber diese Zellen haben noch andere Kandidaten.\n\nBeispiel:\n• 3 und 7 sind nur in Zelle A und B möglich\n• Zelle A hat {2, 3, 7, 9}\n• Zelle B hat {1, 3, 5, 7}",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🔍 Hidden Pair Elimination",
                    Message = "Was bedeutet das?\n\nWenn 3 und 7 NUR in Zelle A und B sein können:\n→ A und B MÜSSEN 3 und 7 enthalten!\n→ Alle ANDEREN Kandidaten in A und B können entfernt werden!\n\n💡 Nach der Eliminierung wird aus dem Hidden Pair ein Naked Pair.",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🔍 Hidden Pair finden",
                    Message = "So findest du Hidden Pairs:\n\n1. Wähle eine Einheit (Zeile, Spalte, Block)\n2. Für jede Zahl: In welchen Zellen kommt sie vor?\n3. Gibt es zwei Zahlen, die NUR in denselben zwei Zellen vorkommen?\n4. Wenn ja → Hidden Pair!\n\n💡 Das ist aufwändiger als Naked Pair.",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // POINTING PAIRS
                // ========================================

                new ShowMessageStep
                {
                    Title = "👉 Pointing Pair",
                    Message = "Ein Pointing Pair entsteht, wenn ein Kandidat in einem Block nur in einer Zeile oder Spalte vorkommt.\n\nBeispiel:\n• Im Block 1 kann die 5 nur in Zeile 1 stehen\n• Die 5 ist \"gefangen\" in dieser Zeile innerhalb des Blocks",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👉 Pointing Elimination",
                    Message = "Was bedeutet das?\n\nWenn die 5 im Block 1 nur in Zeile 1 sein kann:\n→ Die 5 in Zeile 1 MUSS im Block 1 sein!\n→ Entferne 5 aus allen Zellen von Zeile 1, die NICHT in Block 1 sind.\n\n💡 Der Kandidat \"zeigt\" aus dem Block hinaus.",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👉 Pointing Pair Beispiel",
                    Message = "Visuell:\n\n  Block 1          Rest von Zeile 1\n┌─────────────┐   ┌─────────────────┐\n│ 5? │ 5? │   │   │ ❌5 │ ❌5 │ ❌5 │\n├────┼────┼───┤   └─────────────────┘\n│    │    │   │\n│    │    │   │\n└─────────────┘\n\nDie 5 kann aus dem Rest der Zeile entfernt werden!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👉 Probiere es aus!",
                    Message = "Nutze den Hinweis-Button um einen Pointing Pair zu finden.\n\n👆 Klicke auf den Hinweis-Button!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                // ========================================
                // BOX/LINE REDUCTION
                // ========================================

                new ShowMessageStep
                {
                    Title = "📦 Box/Line Reduction",
                    Message = "Box/Line Reduction ist das Gegenteil von Pointing:\n\nWenn ein Kandidat in einer Zeile/Spalte nur in einem Block vorkommt.\n\nBeispiel:\n• In Zeile 1 kann die 5 nur in Block 1 stehen\n• Die 5 ist \"gefangen\" in Block 1 innerhalb dieser Zeile",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "📦 Box/Line Elimination",
                    Message = "Was bedeutet das?\n\nWenn die 5 in Zeile 1 nur im Block 1 sein kann:\n→ Die 5 in Block 1 MUSS in Zeile 1 sein!\n→ Entferne 5 aus allen Zellen von Block 1, die NICHT in Zeile 1 sind.\n\n💡 Die Zeile \"reduziert\" die Möglichkeiten im Block.",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "📦 Box/Line Beispiel",
                    Message = "Visuell:\n\n  Zeile 1 in Block 1    Rest von Block 1\n┌─────────────────┐   ┌─────────────────┐\n│ 5? │ 5? │ 5? │   │ ❌5 │ ❌5 │ ❌5 │\n└─────────────────┘   │ ❌5 │ ❌5 │ ❌5 │\n                      └─────────────────┘\n\nDie 5 kann aus dem Rest des Blocks entfernt werden!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // X-WING
                // ========================================

                new ShowMessageStep
                {
                    Title = "✈️ X-Wing",
                    Message = "X-Wing ist eine mächtige Technik für schwere Puzzles!\n\nEin X-Wing entsteht, wenn ein Kandidat in genau zwei Zeilen NUR in denselben zwei Spalten vorkommt (oder umgekehrt).\n\n💡 Die vier Zellen bilden ein Rechteck.",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "✈️ X-Wing Muster",
                    Message = "Beispiel für X-Wing mit der 5:\n\n     Spalte C    Spalte G\n        ↓           ↓\nZeile 2: 5? ─────── 5?  ←\n         │         │\n         │    X    │\n         │         │\nZeile 7: 5? ─────── 5?  ←\n\nDie 5 kommt in Zeile 2 und 7 NUR in Spalte C und G vor!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "✈️ X-Wing Logik",
                    Message = "Warum funktioniert X-Wing?\n\nDie 5 MUSS einmal in Zeile 2 und einmal in Zeile 7 stehen.\n\nEntweder:\n• 5 in C2 und G7\noder:\n• 5 in G2 und C7\n\n→ In beiden Fällen ist in Spalte C und G je eine 5!\n→ Entferne 5 aus allen anderen Zellen von Spalte C und G.",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "✈️ X-Wing Elimination",
                    Message = "X-Wing Eliminierung:\n\n     Spalte C    Spalte G\n        ↓           ↓\nZeile 1: ❌5       ❌5\nZeile 2: 5? ─────── 5?  ← X-Wing\nZeile 3: ❌5       ❌5\n   ...    ❌5       ❌5\nZeile 7: 5? ─────── 5?  ← X-Wing\n   ...    ❌5       ❌5\n\nAlle anderen 5er in Spalte C und G werden entfernt!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "✈️ Probiere es aus!",
                    Message = "X-Wings sind selten, aber mächtig!\n\nNutze den Hinweis-Button - er findet auch X-Wings.\n\n👆 Klicke auf den Hinweis-Button!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                // ========================================
                // NAKED TRIPLE / QUAD
                // ========================================

                new ShowMessageStep
                {
                    Title = "👯👯 Naked Triple",
                    Message = "Naked Triples funktionieren wie Naked Pairs - aber mit drei Zellen!\n\nDrei Zellen in einer Einheit, die zusammen genau drei verschiedene Kandidaten haben.\n\nBeispiel:\n• Zelle A: {2, 5}\n• Zelle B: {2, 7}\n• Zelle C: {5, 7}\n\n→ Zusammen nur {2, 5, 7}!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👯👯 Triple Besonderheit",
                    Message = "Wichtig: Nicht jede Zelle muss alle drei Kandidaten haben!\n\n✅ Gültige Triples:\n• {2,5}, {2,7}, {5,7}\n• {2,5,7}, {2,5}, {5,7}\n• {2,5,7}, {2,5,7}, {2,5,7}\n\n❌ Ungültig:\n• {2,5,8}, {2,7}, {5,7} ← 4 Kandidaten!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "👯👯👯 Naked Quad",
                    Message = "Naked Quads: Vier Zellen mit zusammen genau vier Kandidaten.\n\nSeltener, aber das Prinzip ist gleich!\n\n💡 Je mehr Zellen, desto schwerer zu finden.\n💡 Der Hinweis-Button findet sie automatisch.",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // STRATEGY OVERVIEW
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎯 Strategie-Übersicht",
                    Message = "Welche Technik wann?\n\n1️⃣ Naked/Hidden Single - Immer zuerst!\n2️⃣ Naked/Hidden Pair - Wenn Singles nicht reichen\n3️⃣ Pointing/Box-Line - Für Block-Zeilen-Interaktion\n4️⃣ X-Wing - Für schwere Puzzles\n5️⃣ Triples/Quads - Wenn Pairs nicht reichen",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🎯 Systematisch arbeiten",
                    Message = "Tipps für schwere Puzzles:\n\n1. Auto-Notizen am Anfang\n2. Alle Singles finden\n3. Nach Pairs suchen\n4. Pointing/Box-Line prüfen\n5. Bei Bedarf: X-Wing\n\n💡 Der Hinweis-Button zeigt die einfachste verfügbare Technik!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // PRACTICE SUGGESTION
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎯 Jetzt üben!",
                    Message = "Diese Techniken brauchen Übung!\n\n💡 Tipps zum Üben:\n• Starte mit Auto-Notizen\n• Nutze den Hinweis-Button zum Lernen\n• Analysiere jeden Hinweis genau\n• Versuche es beim nächsten Mal selbst",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton }
                },

                new ShowMessageStep
                {
                    Title = "📚 Weiterführende Ressourcen",
                    Message = "Noch mehr lernen?\n\n🔗 Der Hinweis-Button erklärt JEDE Technik\n📖 Jeder Hinweis zeigt Schritt-für-Schritt\n🎮 Übung ist der beste Lehrer!\n\n💡 Das nächste Tutorial zeigt dir Challenge-Modi und Statistiken.",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // COMPLETION
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎓 Tutorial abgeschlossen!",
                    Message = "Glückwunsch! Du kennst jetzt alle wichtigen Techniken:\n\n👯 Naked & Hidden Pairs\n👉 Pointing Pairs\n📦 Box/Line Reduction\n✈️ X-Wing\n👯👯 Triples & Quads\n\n💡 Mache als Nächstes das Tutorial \"Challenge-Modi\"!\n🎮 Viel Erfolg beim Üben!",
                    Position = MessagePosition.CenterLeft
                }
            }
        };

        return tutorial;
    }

    private TutorialData CreateChallengeModesTutorial()
    {
        var tutorial = new TutorialData
        {
            Id = "challenge_modes",
            Name = "Challenge-Modi",
            Description = "Deadly Mode, Statistiken und persönliche Bestzeiten.",
            Difficulty = TutorialDifficulty.Hard,
            EstimatedMinutes = 8,
            PuzzleData = "TUTORIAL_CHALLENGES",
            Steps = new List<TutorialStep>
            {
                // ========================================
                // INTRO
                // ========================================

                new ShowMessageStep
                {
                    Title = "Tutorial: Challenge-Modi",
                    Message = "Willkommen zum letzten Tutorial! 🏆\n\nHier lernst du alles über:\n\n• 💀 Deadly Mode - Kein Raum für Fehler!\n• ⏱️ Speedrunning - Jage Bestzeiten\n• 📊 Statistiken - Verfolge deinen Fortschritt\n• 🎯 Persönliche Ziele setzen\n\nBereit für die ultimative Herausforderung?",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // DEADLY MODE DEEP DIVE
                // ========================================

                new ShowMessageStep
                {
                    Title = "💀 Deadly Mode",
                    Message = "Der Deadly Mode ist für echte Sudoku-Meister!\n\n⚠️ Die Regel ist einfach aber gnadenlos:\n\n🔴 3 Fehler = Spiel verloren!\n\nKein Zurück, keine zweite Chance.\nJeder Zug muss sitzen!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.MistakesLabel }
                },

                new ShowMessageStep
                {
                    Title = "💀 Warum Deadly Mode?",
                    Message = "Deadly Mode trainiert dich:\n\n✅ Sorgfältiger zu arbeiten\n✅ Notizen konsequent zu nutzen\n✅ Nie zu raten\n✅ Logik vor Intuition\n\n💡 Du wirst ein besserer Spieler!\n\n⚙️ Aktiviere Deadly Mode in den Einstellungen.",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "💀 Deadly Mode Strategien",
                    Message = "So überlebst du den Deadly Mode:\n\n1️⃣ IMMER mit Auto-Notizen starten\n2️⃣ Keine Zahl ohne Beweis eintragen\n3️⃣ Bei Unsicherheit → Hinweis nutzen\n4️⃣ Systematisch arbeiten, nie springen\n5️⃣ Lieber 5 Min länger als 1 Fehler!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "💀 Die 3-Fehler-Anzeige",
                    Message = "Oben rechts siehst du deine Fehler:\n\n❌ ○ ○ = 1 Fehler - Vorsicht!\n❌ ❌ ○ = 2 Fehler - Letzte Chance!\n❌ ❌ ❌ = Game Over!\n\n💡 Jeder Fehler ist eine Lektion.\n💡 Analysiere: WARUM hast du den Fehler gemacht?",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.MistakesLabel }
                },

                // ========================================
                // TIMER & SPEEDRUNNING
                // ========================================

                new ShowMessageStep
                {
                    Title = "⏱️ Der Timer",
                    Message = "Oben siehst du die verstrichene Zeit.\n\nDer Timer läuft sobald du startest und pausiert automatisch wenn du:\n\n• Das Spiel pausierst\n• Zur Hilfe wechselst\n• Das Fenster minimierst\n\n💡 Fair Play ist garantiert!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.Timer }
                },

                new ShowMessageStep
                {
                    Title = "⏱️ Speedrunning Basics",
                    Message = "Tipps für schnelleres Lösen:\n\n🚀 Tastatur statt Maus!\n   • Pfeiltasten zum Navigieren\n   • Zahlen direkt tippen\n   • N für Notiz-Modus\n\n🚀 Muster erkennen!\n   • Übung macht schneller\n   • Häufige Techniken automatisieren",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "⏱️ Richtwerte für Zeiten",
                    Message = "Wie schnell bist du? Vergleiche:\n\n🟢 LEICHT:\n   Anfänger: 10-15 Min\n   Fortgeschritten: 5-10 Min\n   Profi: unter 3 Min\n\n🟠 MITTEL:\n   Anfänger: 20-30 Min\n   Fortgeschritten: 10-15 Min\n   Profi: unter 8 Min",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "⏱️ Schwere Puzzles",
                    Message = "🔴 SCHWER:\n   Anfänger: 45-60 Min\n   Fortgeschritten: 20-30 Min\n   Profi: unter 15 Min\n\n💎 EXPERTE:\n   Weltklasse: unter 5 Min für schwer!\n\n💡 Vergleiche nur mit dir selbst.\n💡 Jede Verbesserung zählt!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // STATISTICS
                // ========================================

                new ShowMessageStep
                {
                    Title = "📊 Deine Statistiken",
                    Message = "SudokuSen speichert alles!\n\n📈 Erfasste Daten:\n• Gelöste Puzzles pro Schwierigkeit\n• Durchschnittliche Lösungszeit\n• Beste Zeit (Rekord!)\n• Fehlerquote\n• Verwendete Hinweise\n\n💡 Finde dein Dashboard im Hauptmenü!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "📊 Fortschritt verfolgen",
                    Message = "Warum Statistiken wichtig sind:\n\n📉 Erkenne Muster:\n   • Welche Schwierigkeit liegt dir?\n   • Wo brauchst du mehr Übung?\n\n📈 Motivation:\n   • Sieh deinen Fortschritt!\n   • Feiere neue Rekorde!\n\n💡 Kleine Verbesserungen summieren sich!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // GAME HISTORY
                // ========================================

                new ShowMessageStep
                {
                    Title = "📜 Spielverlauf",
                    Message = "Jedes Spiel wird gespeichert:\n\n📋 Du siehst:\n• Datum und Uhrzeit\n• Schwierigkeitsstufe\n• Deine Zeit\n• Fehleranzahl\n• Hinweise verwendet\n• Ob du gewonnen hast\n\n💡 Analysiere deine besten UND schlechtesten Spiele!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "📜 Aus Fehlern lernen",
                    Message = "Ein verlorenes Spiel ist kein Versagen!\n\n🔍 Frage dich:\n• Wo habe ich geraten statt gedacht?\n• Welche Technik hätte geholfen?\n• War ich zu schnell oder müde?\n\n💡 Jeder Fehler macht dich besser!\n💡 Die besten Spieler haben am meisten verloren.",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // DIFFICULTY PROGRESSION
                // ========================================

                new ShowMessageStep
                {
                    Title = "📈 Schwierigkeitsstufen",
                    Message = "SudokuSen bietet für jeden etwas:\n\n👶 KIDS (4×4)\n   Perfekt für Kinder und absolute Anfänger\n\n🟢 LEICHT (9×9)\n   Nur Naked & Hidden Singles\n   → Ideal zum Aufwärmen!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.DifficultyLabel }
                },

                new ShowMessageStep
                {
                    Title = "📈 Mittlere Stufen",
                    Message = "🟠 MITTEL\n   + Pointing Pairs\n   + Box/Line Reduction\n   → Hier lernst du die meisten Techniken!\n\n🟠 MITTEL+\n   + Naked/Hidden Pairs\n   → Der Übergang zum Fortgeschrittenen",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "📈 Experten-Stufen",
                    Message = "🔴 SCHWER\n   + X-Wing\n   + Naked/Hidden Triples\n   → Echte Herausforderungen!\n\n💎 EXPERTE\n   + Swordfish, XY-Wing\n   + Komplexe Verkettungen\n   → Nur für die Besten!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // PERSONAL CHALLENGES
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎯 Setze dir Ziele!",
                    Message = "Persönliche Herausforderungen:\n\n🥉 BRONZE:\n   • 10 Puzzles auf Leicht lösen\n   • Zeit unter 15 Min schaffen\n\n🥈 SILBER:\n   • 10 Puzzles auf Mittel lösen\n   • Ohne Hinweise gewinnen",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🎯 Höhere Ziele",
                    Message = "🥇 GOLD:\n   • 10 Puzzles auf Schwer lösen\n   • Max 3 Hinweise pro Spiel\n   • Zeit unter 30 Min\n\n💎 DIAMANT:\n   • Schweres Puzzle ohne Hinweise\n   • Im Deadly Mode gewinnen!\n   • Unter 20 Min schaffen",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🎯 Ultimate Challenge",
                    Message = "🏆 MEISTER-CHALLENGE:\n\n   ✅ Schweres Puzzle\n   ✅ Deadly Mode (3 Fehler = Game Over)\n   ✅ Keine Hinweise\n   ✅ Unter 15 Minuten\n\nSchaffst du das? 💪\n\n💡 Tipp: Erst alle Tutorials abschließen!",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // HINTS STRATEGY
                // ========================================

                new ShowMessageStep
                {
                    Title = "💡 Hinweise als Lehrer",
                    Message = "Der Hinweis-Button ist KEIN Cheat!\n\n📚 Er ist dein Lehrer:\n• Zeigt die einfachste verfügbare Technik\n• Erklärt WARUM es funktioniert\n• Hebt relevante Zellen hervor\n\n💡 Nutze Hinweise zum LERNEN, nicht zum Abkürzen!",
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton }
                },

                new ShowMessageStep
                {
                    Title = "💡 Hinweis-Limitierung",
                    Message = "Challenge: Limitiere deine Hinweise!\n\n📊 Tracking-Idee:\n   Woche 1: Max 10 Hinweise pro Puzzle\n   Woche 2: Max 5 Hinweise\n   Woche 3: Max 3 Hinweise\n   Woche 4: Max 1 Hinweis\n   Woche 5: Keine Hinweise!\n\n💡 Langsam reduzieren = nachhaltiges Lernen",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // FINAL TIPS
                // ========================================

                new ShowMessageStep
                {
                    Title = "🌟 Letzte Tipps",
                    Message = "Geheimnisse der Sudoku-Meister:\n\n1️⃣ Täglich 1-2 Puzzles = stetiger Fortschritt\n2️⃣ Verschiedene Schwierigkeiten spielen\n3️⃣ Nach Frustration: Pause machen!\n4️⃣ Fehler analysieren, nicht ignorieren\n5️⃣ Spaß haben! 🎮",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🌟 Routine aufbauen",
                    Message = "Die perfekte Sudoku-Routine:\n\n☀️ Morgens: 1 leichtes Puzzle zum Aufwärmen\n🌙 Abends: 1 schwieriges Puzzle zur Challenge\n\n📅 Wochenende: Deadly Mode ausprobieren!\n\n💡 Konsistenz schlägt Intensität.\n💡 15 Min täglich > 2 Std am Wochenende",
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // COMPLETION
                // ========================================

                new ShowMessageStep
                {
                    Title = "🎓 Alle Tutorials abgeschlossen!",
                    Message = "HERZLICHEN GLÜCKWUNSCH! 🎉\n\nDu hast ALLE Tutorials gemeistert:\n\n✅ Erste Schritte\n✅ Grundtechniken\n✅ Erweiterte Funktionen\n✅ Fortgeschrittene Techniken\n✅ Challenge-Modi\n\nDu bist jetzt ein vollständig ausgebildeter Sudoku-Spieler!",
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = "🚀 Deine Reise beginnt!",
                    Message = "Was kommt als Nächstes?\n\n1️⃣ Starte mit einem leichten Puzzle\n2️⃣ Arbeite dich durch die Schwierigkeiten\n3️⃣ Verfolge deine Statistiken\n4️⃣ Wage den Deadly Mode!\n5️⃣ Jage deine Bestzeiten!\n\n🏆 Viel Erfolg, Sudoku-Meister! 🏆",
                    Position = MessagePosition.CenterLeft
                }
            }
        };

        return tutorial;
    }
}
