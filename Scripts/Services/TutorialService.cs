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

    private LocalizationService _localizationService = null!;

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
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");
        RegisterBuiltInTutorials();
        GD.Print("[TutorialService] Ready - registered tutorials: " + string.Join(", ", _tutorials.Keys));
    }

    private string L(string german, string english)
    {
        return _localizationService.CurrentLanguage == Language.German ? german : english;
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
                EmitSignal(SignalName.MessageRequested, step.WrongActionMessage, _localizationService.Get("game.hint"), (int)MessagePosition.BottomCenter, "");
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
            Name = _localizationService.Get("tutorial.getting_started"),
            Description = _localizationService.Get("tutorial.getting_started.desc"),
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
                Title = L("Tutorial: Erste Schritte", "Tutorial: Getting Started"),
                Message = L(
                    "Willkommen bei SudokuSen!\n\nIn diesem Tutorial lernst du die Benutzeroberfläche und grundlegende Steuerung kennen.\n\nDas Puzzle ist fast fertig – nur noch 5 Zellen fehlen!\n\n👆 Klicke auf \"Weiter\" um fortzufahren.",
                    "Welcome to SudokuSen!\n\nIn this tutorial you'll learn the user interface and basic controls.\n\nThe puzzle is almost finished — only 5 cells are missing!\n\n👆 Click \"Next\" to continue."
                ),
                Position = MessagePosition.CenterLeft
            },

            // Show the grid - point to edge, not center
            new ShowMessageStep
            {
                Title = L("📋 Das Spielfeld", "📋 The Board"),
                Message = L(
                    "Das ist das Sudoku-Spielfeld.\n\n• 9×9 Zellen, aufgeteilt in 9 Blöcke (3×3)\n• Jede Zahl 1-9 darf in jeder Zeile, Spalte und jedem Block nur EINMAL vorkommen\n• Graue Zahlen sind vorgegeben und können nicht geändert werden",
                    "This is the Sudoku board.\n\n• 9×9 cells, split into 9 blocks (3×3)\n• Each number 1-9 may appear only ONCE in every row, column, and block\n• Grey numbers are given and cannot be changed"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.GridEdge }
            },

            // Show axis labels - point to both "A" column and "1" row
            new ShowMessageStep
            {
                Title = L("🔤 Achsenbeschriftung", "🔤 Axis Labels"),
                Message = L(
                    "Oben siehst du Spalten A-I, links die Zeilen 1-9.\n\nSo kannst du Zellen eindeutig benennen:\n• E5 = Spalte E, Zeile 5 (die Mitte!)\n• A1 = oben links\n• I9 = unten rechts\n\nDas ist praktisch beim Besprechen von Zügen!",
                    "At the top you see columns A–I, on the left rows 1–9.\n\nThat lets you name cells unambiguously:\n• E5 = column E, row 5 (the center!)\n• A1 = top-left\n• I9 = bottom-right\n\nThis is handy when talking about moves!"
                ),
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
                Title = L("← Zurück-Button", "← Back Button"),
                Message = L(
                    "Mit diesem Button kehrst du zum Hauptmenü zurück.\n\n💾 Keine Sorge: Dein Spielstand wird automatisch gespeichert!",
                    "Use this button to return to the main menu.\n\n💾 Don't worry: your game is saved automatically!"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.BackButton }
            },

            // Show difficulty BEFORE timer
            new ShowMessageStep
            {
                Title = L("📊 Schwierigkeit", "📊 Difficulty"),
                Message = L(
                    "Die aktuelle Schwierigkeitsstufe:\n\n• 🟢 Kids (4×4)\n• 🟢 Leicht\n• 🟠 Mittel\n• 🔴 Schwer",
                    "The current difficulty level:\n\n• 🟢 Kids (4×4)\n• 🟢 Easy\n• 🟠 Medium\n• 🔴 Hard"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.DifficultyLabel }
            },

            // Show timer
            new ShowMessageStep
            {
                Title = L("⏱️ Timer", "⏱️ Timer"),
                Message = L(
                    "Hier siehst du die verstrichene Spielzeit.\n\nDie Zeit läuft automatisch, sobald du spielst.",
                    "Here you can see the elapsed play time.\n\nThe timer runs automatically while you play."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.Timer }
            },

            // Show mistakes counter
            new ShowMessageStep
            {
                Title = L("❌ Fehlerzähler", "❌ Mistake Counter"),
                Message = L(
                    "Hier werden deine Fehler gezählt.\n\n⚠️ WICHTIG: Im \"Deadly Modus\" (in den Einstellungen aktivierbar) endet das Spiel nach 3 Fehlern!\n\nFür dieses Tutorial ist der Deadly Modus deaktiviert.",
                    "Your mistakes are counted here.\n\n⚠️ IMPORTANT: In \"Deadly Mode\" (enabled in Settings), the game ends after 3 mistakes!\n\nFor this tutorial, Deadly Mode is disabled."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.MistakesLabel }
            },

            // ========================================
            // PART 2: Selecting Cells & Entering Numbers
            // ========================================

            // Step 1: Select the cell
            new ShowMessageStep
            {
                Title = L("🎯 Zelle auswählen", "🎯 Select a Cell"),
                Message = L(
                    "Lass uns eine Zelle ausfüllen!\n\nSiehst du die pulsierende Zelle E5 in der Mitte?\n\n👆 Klicke darauf!",
                    "Let's fill in a cell!\n\nDo you see the pulsing cell E5 in the center?\n\n👆 Click it!"
                ),
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
                Title = L("🔢 Zahl eingeben", "🔢 Enter a Number"),
                Message = L(
                    "Die Zelle E5 ist ausgewählt (blau).\n\nJetzt gib die richtige Zahl ein!\n\n🔍 Tipp: Schau welche Zahlen schon in Zeile 5, Spalte E und dem mittleren Block sind.\n\n💡 Die Lösung ist die 5!",
                    "Cell E5 is selected (blue).\n\nNow enter the correct number!\n\n🔍 Tip: Check which numbers already appear in row 5, column E, and the middle block.\n\n💡 The solution is 5!"
                ),
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
                Title = L("🎉 Perfekt!", "🎉 Perfect!"),
                Message = L(
                    "Sehr gut! Du hast die richtige Zahl gefunden.\n\nJetzt lernst du NOTIZEN kennen - ein wichtiges Werkzeug!",
                    "Great job! You found the correct number.\n\nNext up: NOTES — an important tool!"
                ),
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // PART 3: Notes - Interactive Practice
            // ========================================

            new ShowMessageStep
            {
                Title = L("📝 Notizen-Modus", "📝 Notes Mode"),
                Message = L(
                    "Manchmal bist du nicht sicher, welche Zahl passt.\n\nDafür gibt es den Notizen-Modus!\n\n👆 Klicke auf den Notizen-Button oder drücke 'N'.",
                    "Sometimes you're not sure which number fits.\n\nThat's what Notes Mode is for!\n\n👆 Click the Notes button or press 'N'."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NotesToggle },
                WaitForAction = ExpectedAction.ToggleNotesMode
            },

            // Select a cell for notes practice - (0,2) has solution 4
            new ShowMessageStep
            {
                Title = L("📝 Notiz setzen", "📝 Place a Note"),
                Message = L(
                    "Super! Du bist im Notizen-Modus.\n\nJetzt wähle die Zelle C1 (oben, dritte Spalte) aus.\n\n👆 Klicke auf die pulsierende Zelle!",
                    "Nice! You're in Notes Mode.\n\nNow select cell C1 (top row, third column).\n\n👆 Click the pulsing cell!"
                ),
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
                Title = L("📝 Notiz hinzufügen", "📝 Add a Note"),
                Message = L(
                    "Gib jetzt die Zahl 4 ein.\n\nIm Notizen-Modus wird sie als kleine Notiz angezeigt!",
                    "Now enter the number 4.\n\nIn Notes Mode it will appear as a small note!"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "4" },
                WaitForAction = ExpectedAction.ToggleNote,
                ExpectedCell = (0, 2),
                ExpectedNumber = 4
            },

            // Toggle it off
            new ShowMessageStep
            {
                Title = L("📝 Notiz entfernen", "📝 Remove a Note"),
                Message = L(
                    "Die 4 ist jetzt als Notiz sichtbar!\n\n👆 Drücke nochmal 4 um sie zu entfernen (Toggle).",
                    "The 4 is now visible as a note!\n\n👆 Press 4 again to remove it (toggle)."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "4" },
                WaitForAction = ExpectedAction.ToggleNote,
                ExpectedCell = (0, 2),
                ExpectedNumber = 4
            },

            // Show eraser alternative
            new ShowMessageStep
            {
                Title = L("🗑️ Radiergummi", "🗑️ Eraser"),
                Message = L(
                    "Du kannst Notizen auch mit dem Radiergummi löschen!\n\n⌨️ Oder drücke: Entf / Backspace / 0\n\nDer Radiergummi löscht ALLE Notizen der Zelle.",
                    "You can also delete notes using the eraser!\n\n⌨️ Or press: Del / Backspace / 0\n\nThe eraser removes ALL notes in the cell."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.EraseButton }
            },

            // ========================================
            // PART 4: Multi-Select with Notes (Interactive)
            // ========================================

            new ShowMessageStep
            {
                Title = L("🔲 Mehrfachauswahl", "🔲 Multi-Select"),
                Message = L(
                    "Jetzt probieren wir Mehrfachauswahl!\n\n• Strg + Klick → Zellen hinzufügen\n• Shift + Klick → Bereich auswählen\n\nDu bist noch im Notizen-Modus - perfekt!",
                    "Now let's try multi-select!\n\n• Ctrl + click → add cells\n• Shift + click → select a rectangle\n\nYou're still in Notes Mode — perfect!"
                ),
                Position = MessagePosition.CenterLeft
            },

            // Select first cell for multi-select
            new ShowMessageStep
            {
                Title = L("🔲 Erste Zelle wählen", "🔲 Select the First Cell"),
                Message = L(
                    "Wähle zuerst Zelle G3 aus.\n\n👆 Klicke auf die pulsierende Zelle!",
                    "First, select cell G3.\n\n👆 Click the pulsing cell!"
                ),
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
                Title = L("🔲 Zweite Zelle (Strg+Klick)", "🔲 Second Cell (Ctrl+Click)"),
                Message = L(
                    "Halte Strg gedrückt und klicke auf B7.\n\nDamit fügst du die Zelle zur Auswahl hinzu!",
                    "Hold Ctrl and click B7.\n\nThis adds the cell to the selection!"
                ),
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
                Title = L("🔲 Notiz für beide", "🔲 Note for Both"),
                Message = L(
                    "Beide Zellen sind markiert (blau umrandet).\n\nGib jetzt 3 ein - die Notiz wird in BEIDEN Zellen gesetzt!",
                    "Both cells are selected (blue outline).\n\nNow enter 3 — the note will be placed in BOTH cells!"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "3" },
                WaitForAction = ExpectedAction.ToggleNoteMultiSelect,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1) },
                ExpectedNumber = 3
            },

            // Add third cell with Ctrl+Click (this cell doesn't have 3 yet)
            new ShowMessageStep
            {
                Title = L("🔲 Dritte Zelle (Strg+Klick)", "🔲 Third Cell (Ctrl+Click)"),
                Message = L(
                    "Füge jetzt Zelle I9 hinzu.\n\nHalte Strg gedrückt und klicke darauf!",
                    "Now add cell I9.\n\nHold Ctrl and click it!"
                ),
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
                Title = L("🔲 Smart Toggle", "🔲 Smart Toggle"),
                Message = L(
                    "Drücke 3.\n\nG3 und B7 haben schon die 3, nur I9 bekommt sie neu!\n\n💡 Notizen werden nur dort gesetzt, wo sie noch fehlen.",
                    "Press 3.\n\nG3 and B7 already have the 3, so only I9 gets it now!\n\n💡 Notes are only added where they are missing."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "3" },
                WaitForAction = ExpectedAction.ToggleNoteMultiSelect,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1), (8, 8) },
                ExpectedNumber = 3
            },

            // Remove from all three (now all have it)
            new ShowMessageStep
            {
                Title = L("🔲 Alle entfernen", "🔲 Remove from All"),
                Message = L(
                    "Drücke 3 nochmal.\n\nJetzt haben ALLE drei die Notiz → sie wird aus allen entfernt!",
                    "Press 3 again.\n\nNow ALL three have the note → it will be removed from all of them!"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "3" },
                WaitForAction = ExpectedAction.ToggleNoteMultiSelect,
                ExpectedCells = new List<(int, int)> { (2, 6), (6, 1), (8, 8) },
                ExpectedNumber = 3
            },

            // Exit notes mode
            new ShowMessageStep
            {
                Title = L("📝 Fertig!", "📝 Done!"),
                Message = L(
                    "Klicke auf den Notizen-Button um den Modus zu beenden.\n\n💡 Tipp: Shift+Klick wählt einen ganzen Bereich!",
                    "Click the Notes button to exit Notes Mode.\n\n💡 Tip: Shift+click selects a whole rectangle!"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NotesToggle },
                WaitForAction = ExpectedAction.ToggleNotesMode
            },

            // ========================================
            // PART 5: Helper Buttons
            // ========================================

            new ShowMessageStep
            {
                Title = L("🛠️ Hilfreiche Buttons", "🛠️ Helpful Buttons"),
                Message = L(
                    "SudokuSen hat mehrere praktische Hilfsfunktionen.\n\nLass uns sie kennenlernen!",
                    "SudokuSen has several handy helper features.\n\nLet's take a quick look!"
                ),
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = L("💡 Hinweis-Button", "💡 Hint Button"),
                Message = L(
                    "Brauchst du Hilfe?\n\nDer Hinweis-Button zeigt dir den nächsten logischen Schritt mit Erklärung!\n\n📚 Perfekt zum Lernen neuer Lösungstechniken.",
                    "Need help?\n\nThe Hint button shows the next logical step with an explanation!\n\n📚 Perfect for learning new solving techniques."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HintButton }
            },

            new ShowMessageStep
            {
                Title = L("✨ Auto-Notizen", "✨ Auto Notes"),
                Message = L(
                    "Dieser Button füllt automatisch ALLE möglichen Kandidaten in leere Zellen ein.\n\n💡 Sehr praktisch für Anfänger!\n\n⚠️ Achtung: Bei schweren Puzzles können das viele Notizen sein.",
                    "This button automatically fills ALL possible candidates into empty cells.\n\n💡 Very handy for beginners!\n\n⚠️ Note: On hard puzzles this can create a lot of notes."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.AutoNotesButton }
            },

            new ShowMessageStep
            {
                Title = L("🔤 R/C/B Button", "🔤 R/C/B Button"),
                Message = L(
                    "Dieser Button füllt Notizen für Zeile (R), Spalte (C) oder Block (B) aus.\n\n👆 Rechtsklick: Modus wechseln (R→C→B)\n👆 Linksklick: Notizen für ausgewählte Zelle(n) setzen\n\n💡 Funktioniert auch bei Mehrfachauswahl!",
                    "This button fills notes for Row (R), Column (C), or Block (B).\n\n👆 Right-click: change mode (R→C→B)\n👆 Left-click: apply to the selected cell(s)\n\n💡 Works with multi-select too!"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HouseAutoFillButton }
            },

            new ShowMessageStep
            {
                Title = L("🗑️ Radiergummi", "🗑️ Eraser"),
                Message = L(
                    "Der Radiergummi löscht:\n\n• Die Zahl in der ausgewählten Zelle\n• ALLE Notizen in der Zelle\n\n⌨️ Alternativ: Entf oder Rücktaste",
                    "The eraser removes:\n\n• The number in the selected cell\n• ALL notes in the cell\n\n⌨️ Shortcut: Del or Backspace"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.EraseButton }
            },

            // ========================================
            // PART 6: Completion
            // ========================================

            new ShowMessageStep
            {
                Title = L("🎓 Tutorial abgeschlossen!", "🎓 Tutorial Complete!"),
                Message = L(
                    "Glückwunsch! Du kennst jetzt die Grundlagen von SudokuSen.\n\n📋 Zusammenfassung:\n• Zellen auswählen & Zahlen eingeben\n• Fehler werden rot markiert\n• Notizen für Kandidaten nutzen\n• Hilfsfunktionen bei Bedarf\n\n🎮 Viel Spaß beim Rätseln!",
                    "Congrats! You now know the basics of SudokuSen.\n\n📋 Summary:\n• Select cells and enter numbers\n• Mistakes are highlighted\n• Use notes for candidates\n• Use helper tools when needed\n\n🎮 Have fun solving!"
                ),
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
            Name = _localizationService.Get("tutorial.basic_techniques"),
            Description = _localizationService.Get("tutorial.basic_techniques.desc"),
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
                Title = L("Tutorial: Grundtechniken", "Tutorial: Basic Techniques"),
                Message = L(
                    "Willkommen zum Technik-Tutorial!\n\nHier lernst du die beiden wichtigsten Grundtechniken:\n\n• 🎯 Naked Single\n• 🔍 Hidden Single\n\nMit diesen Techniken lassen sich die meisten leichten und mittleren Puzzles lösen!",
                    "Welcome to the techniques tutorial!\n\nHere you'll learn the two most important basics:\n\n• 🎯 Naked Single\n• 🔍 Hidden Single\n\nWith these techniques you can solve most easy and medium puzzles!"
                ),
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // NAKED SINGLE EXPLANATION
            // ========================================

            new ShowMessageStep
            {
                Title = L("🎯 Naked Single", "🎯 Naked Single"),
                Message = L(
                    "Eine Zelle hat nur EINE mögliche Zahl.\n\nWarum? Weil alle anderen Zahlen (1-9) bereits in:\n• derselben Zeile ODER\n• derselben Spalte ODER\n• demselben 3×3-Block\nvorkommen.\n\n💡 Auch genannt: \"Sole Candidate\"",
                    "A cell has only ONE possible number.\n\nWhy? Because all other numbers (1-9) already appear in:\n• the same row OR\n• the same column OR\n• the same 3×3 box\n\n💡 Also called: \"Sole Candidate\""
                ),
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = L("🎯 Naked Single finden", "🎯 Finding a Naked Single"),
                Message = L(
                    "So findest du einen Naked Single:\n\n1. Wähle eine leere Zelle\n2. Prüfe welche Zahlen in der Zeile sind\n3. Prüfe welche Zahlen in der Spalte sind\n4. Prüfe welche Zahlen im Block sind\n5. Nur EINE Zahl übrig? → Das ist die Lösung!",
                    "How to find a Naked Single:\n\n1. Select an empty cell\n2. Check which numbers are in the row\n3. Check which numbers are in the column\n4. Check which numbers are in the box\n5. Only ONE number left? → That's the answer!"
                ),
                Position = MessagePosition.CenterLeft
            },

            // Interactive: Find and enter a Naked Single
            new ShowMessageStep
            {
                Title = L("🎯 Probiere es aus!", "🎯 Try it!"),
                Message = L(
                    "Sieh dir Zelle E5 (Mitte) an.\n\nDie pulsierende Zelle hat nur EINE mögliche Zahl.\n\n👆 Wähle sie aus!",
                    "Look at cell E5 (center).\n\nThe pulsing cell has only ONE possible number.\n\n👆 Select it!"
                ),
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
                Title = L("🎯 Analyse", "🎯 Analysis"),
                Message = L(
                    "Schau dir Zeile 5, Spalte E und den mittleren Block an.\n\nWelche Zahlen fehlen noch?\n\n✅ Nur die 5 kann hier stehen!\n\n👆 Gib 5 ein.",
                    "Look at row 5, column E, and the middle box.\n\nWhich numbers are still missing?\n\n✅ Only 5 can go here!\n\n👆 Enter 5."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "5" },
                WaitForAction = ExpectedAction.EnterCorrectNumber,
                ExpectedCell = (4, 4),
                ExpectedNumber = 5
            },

            new ClearHighlightsStep(),

            new ShowMessageStep
            {
                Title = L("🎉 Perfekt!", "🎉 Perfect!"),
                Message = L(
                    "Das war ein Naked Single!\n\n💡 Der Hinweis-Button zeigt dir solche Techniken automatisch.",
                    "That was a Naked Single!\n\n💡 The Hint button can show you techniques like this automatically."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HintButton }
            },

            // ========================================
            // HIDDEN SINGLE EXPLANATION
            // ========================================

            new ShowMessageStep
            {
                Title = L("🔍 Hidden Single", "🔍 Hidden Single"),
                Message = L(
                    "Eine Zahl kann nur an EINER Stelle in einer Zeile, Spalte oder Block stehen.\n\nDie Zelle selbst hat vielleicht mehrere Kandidaten - aber diese spezielle Zahl kann NUR hier hin!\n\n💡 Auch genannt: \"Unique Candidate\"",
                    "A number can only go in ONE place within a row, column, or box.\n\nThe cell itself may have several candidates — but this specific number can ONLY go here!\n\n💡 Also called: \"Unique Candidate\""
                ),
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = L("🔍 Drei Varianten", "🔍 Three Variants"),
                Message = L(
                    "Hidden Single gibt es in drei Varianten:\n\n📏 In der Zeile: Die Zahl kann nur in EINER Zelle der Zeile stehen\n\n📐 In der Spalte: Die Zahl kann nur in EINER Zelle der Spalte stehen\n\n📦 Im Block: Die Zahl kann nur in EINER Zelle des 3×3-Blocks stehen",
                    "Hidden Singles come in three variants:\n\n📏 In a row: the number fits in only ONE cell of the row\n\n📐 In a column: the number fits in only ONE cell of the column\n\n📦 In a box: the number fits in only ONE cell of the 3×3 box"
                ),
                Position = MessagePosition.CenterLeft
            },

            new ShowMessageStep
            {
                Title = L("🔍 Hidden Single finden", "🔍 Finding a Hidden Single"),
                Message = L(
                    "So findest du einen Hidden Single:\n\n1. Wähle eine Zahl (z.B. 4)\n2. Wähle eine Einheit (Zeile, Spalte, Block)\n3. Finde alle Zellen wo diese Zahl hin könnte\n4. Nur EINE Stelle möglich? → Hidden Single!",
                    "How to find a Hidden Single:\n\n1. Pick a number (e.g., 4)\n2. Pick a unit (row, column, box)\n3. Find all cells where this number could go\n4. Only ONE spot possible? → Hidden Single!"
                ),
                Position = MessagePosition.CenterLeft
            },

            // Interactive: Find and enter a Hidden Single
            new ShowMessageStep
            {
                Title = L("🔍 Probiere es aus!", "🔍 Try it!"),
                Message = L(
                    "Schau dir Zelle C1 an.\n\nDiese Zelle hat mehrere Kandidaten, ABER: Im ersten 3×3-Block (oben links) kann die 4 NUR hier stehen!\n\n👆 Wähle die Zelle aus.",
                    "Look at cell C1.\n\nThis cell has multiple candidates, BUT: in the first 3×3 box (top-left), the 4 can ONLY go here!\n\n👆 Select the cell."
                ),
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
                Title = L("🔍 Warum hier?", "🔍 Why here?"),
                Message = L(
                    "Schau dir den oberen linken 3×3-Block an.\n\nPrüfe jede leere Zelle: Kann die 4 dort stehen?\n\nDie 4 wird durch andere Zeilen und Spalten blockiert - nur C1 bleibt!\n\n👆 Gib 4 ein.",
                    "Look at the top-left 3×3 box.\n\nCheck every empty cell: can 4 go there?\n\nThe 4 is blocked by other rows and columns — only C1 remains!\n\n👆 Enter 4."
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.NumberPadButton, ButtonId = "4" },
                WaitForAction = ExpectedAction.EnterCorrectNumber,
                ExpectedCell = (0, 2),
                ExpectedNumber = 4
            },

            new ClearHighlightsStep(),

            new ShowMessageStep
            {
                Title = L("🎉 Ausgezeichnet!", "🎉 Excellent!"),
                Message = L(
                    "Das war ein Hidden Single im Block!\n\nDer Unterschied zu Naked Single:\n• Naked Single: Zelle hat nur 1 Kandidat\n• Hidden Single: Zahl hat nur 1 mögliche Zelle",
                    "That was a Hidden Single in a box!\n\nDifference vs. Naked Single:\n• Naked Single: the cell has only 1 candidate\n• Hidden Single: the number has only 1 possible cell"
                ),
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // USING THE HINT BUTTON
            // ========================================

            new ShowMessageStep
            {
                Title = L("💡 Hinweis-Button nutzen", "💡 Using the Hint Button"),
                Message = L(
                    "Der Hinweis-Button findet automatisch die nächste Technik!\n\nEr zeigt dir:\n• Welche Technik\n• Welche Zelle\n• Welche Zahl\n• Eine Erklärung\n\n👆 Klicke auf den Hinweis-Button!",
                    "The Hint button automatically finds the next technique!\n\nIt shows you:\n• Which technique\n• Which cell\n• Which number\n• An explanation\n\n👆 Click the Hint button!"
                ),
                Position = MessagePosition.CenterLeft,
                PointTo = new TutorialTarget { Type = TargetType.HintButton },
                WaitForAction = ExpectedAction.ClickButton
            },

            new ShowMessageStep
            {
                Title = L("📊 Zusammenfassung", "📊 Summary"),
                Message = L(
                    "Du kennst jetzt die zwei wichtigsten Techniken:\n\n🎯 Naked Single\n• Zelle hat nur 1 Kandidat\n• \"Diese Zelle MUSS X sein\"\n\n🔍 Hidden Single\n• Zahl hat nur 1 mögliche Zelle\n• \"X MUSS hier hin\"\n\n💡 Mit Notizen werden diese Techniken noch einfacher zu finden!",
                    "You now know the two most important techniques:\n\n🎯 Naked Single\n• Cell has only 1 candidate\n• \"This cell MUST be X\"\n\n🔍 Hidden Single\n• Number has only 1 possible cell\n• \"X MUST go here\"\n\n💡 With notes, these techniques become even easier to spot!"
                ),
                Position = MessagePosition.CenterLeft
            },

            // ========================================
            // COMPLETION
            // ========================================

            new ShowMessageStep
            {
                Title = L("🎓 Tutorial abgeschlossen!", "🎓 Tutorial Complete!"),
                Message = L(
                    "Glückwunsch! Du kennst jetzt die Grundtechniken.\n\n📚 Nächste Schritte:\n• Übe mit leichten Puzzles\n• Nutze Auto-Notizen für Übersicht\n• Der Hinweis-Button erklärt jeden Schritt\n\n🎮 Viel Erfolg beim Üben!",
                    "Congrats! You now know the basic techniques.\n\n📚 Next steps:\n• Practice with easy puzzles\n• Use Auto Notes for an overview\n• The Hint button explains every step\n\n🎮 Good luck practicing!"
                ),
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
            Name = _localizationService.Get("tutorial.advanced_features"),
            Description = _localizationService.Get("tutorial.advanced_features.desc"),
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
                    Title = L("Tutorial: Erweiterte Funktionen", "Tutorial: Advanced Features"),
                    Message = L(
                        "In diesem Tutorial lernst du die fortgeschrittenen Funktionen von SudokuSen kennen:\n\n• ✨ Auto-Notizen\n• 🔤 R/C/B-Button\n• 🔲 Bereichsauswahl\n• ⌨️ Tastaturkürzel\n• 🎨 Highlighting\n\nDiese Funktionen machen dich zum Profi!",
                        "In this tutorial you'll learn SudokuSen's advanced features:\n\n• ✨ Auto Notes\n• 🔤 R/C/B button\n• 🔲 Range selection\n• ⌨️ Keyboard shortcuts\n• 🎨 Highlighting\n\nThese features will level up your play!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // AUTO-NOTES DEEP DIVE
                // ========================================

                new ShowMessageStep
                {
                    Title = L("✨ Auto-Notizen", "✨ Auto Notes"),
                    Message = L(
                        "Der Auto-Notizen-Button füllt automatisch alle möglichen Kandidaten in ALLE leeren Zellen ein.\n\n💡 Sehr nützlich am Anfang eines Puzzles!\n\n⚠️ Bei schweren Puzzles können das viele Notizen sein - keine Sorge, das ist normal.",
                        "The Auto Notes button fills all possible candidates into ALL empty cells automatically.\n\n💡 Very useful at the start of a puzzle!\n\n⚠️ On hard puzzles this can be a lot of notes — that's normal."
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.AutoNotesButton }
                },

                new ShowMessageStep
                {
                    Title = L("✨ Probiere es aus!", "✨ Try it!"),
                    Message = L(
                        "Klicke auf den Auto-Notizen-Button.\n\nAlle leeren Zellen werden mit ihren möglichen Kandidaten gefüllt!",
                        "Click the Auto Notes button.\n\nAll empty cells will be filled with their possible candidates!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.AutoNotesButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                new ShowMessageStep
                {
                    Title = L("✨ Ergebnis analysieren", "✨ Analyze the result"),
                    Message = L(
                        "Siehst du die kleinen Zahlen in den leeren Zellen?\n\nDas sind alle Kandidaten, die dort theoretisch möglich sind.\n\n💡 Achte auf Zellen mit wenigen Kandidaten - dort findest du oft Naked Singles!",
                        "See the small numbers in the empty cells?\n\nThose are all candidates that could theoretically fit there.\n\n💡 Watch for cells with few candidates — that's where you often find Naked Singles!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("✨ Wann Auto-Notizen nutzen?", "✨ When to use Auto Notes?"),
                    Message = L(
                        "Auto-Notizen sind ideal für:\n\n✅ Puzzles ab \"Mittel\" Schwierigkeit\n✅ Wenn du fortgeschrittene Techniken üben willst\n✅ Um einen Überblick zu bekommen\n\n❌ Nicht nötig bei \"Leicht\" - dort reichen einfache Techniken",
                        "Auto Notes are ideal for:\n\n✅ Puzzles of \"Medium\" difficulty and above\n✅ When you want to practice advanced techniques\n✅ Getting a quick overview\n\n❌ Not necessary on \"Easy\" — basic techniques are enough"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // R/C/B BUTTON DEEP DIVE
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🔤 Der R/C/B-Button", "🔤 The R/C/B Button"),
                    Message = L(
                        "Dieser Button füllt Notizen nur für bestimmte Bereiche:\n\n• R = Row (Zeile)\n• C = Column (Spalte)\n• B = Block\n\n💡 Perfekt wenn du nur einen Teil des Puzzles analysieren willst!",
                        "This button fills notes only for a specific area:\n\n• R = Row\n• C = Column\n• B = Box\n\n💡 Perfect when you only want to analyze part of the puzzle!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HouseAutoFillButton }
                },

                new ShowMessageStep
                {
                    Title = L("🔤 Zelle auswählen", "🔤 Select a cell"),
                    Message = L(
                        "Wähle zuerst eine Zelle in der Mitte aus.\n\nDer R/C/B-Button arbeitet dann mit der Zeile, Spalte oder dem Block dieser Zelle.\n\n👆 Klicke auf E5!",
                        "First, select a cell in the center.\n\nThe R/C/B button will then use the row, column, or box of that cell.\n\n👆 Click E5!"
                    ),
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
                    Title = L("🔤 Modus wechseln", "🔤 Switch modes"),
                    Message = L(
                        "Der Button zeigt den aktuellen Modus an:\n\n• ▶ Row → Zeile 5\n• ▶ Col → Spalte E\n• ▶ Block → Mittlerer Block\n\n👆 Mit RECHTSKLICK wechselst du den Modus.\n👆 Mit LINKSKLICK führst du die Aktion aus.",
                        "The button shows the current mode:\n\n• ▶ Row → row 5\n• ▶ Col → column E\n• ▶ Block → middle box\n\n👆 Right-click changes the mode.\n👆 Left-click performs the action."
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HouseAutoFillButton }
                },

                new ShowMessageStep
                {
                    Title = L("🔤 Praktischer Einsatz", "🔤 When is it useful?"),
                    Message = L(
                        "Wann ist R/C/B besser als Auto-Notizen?\n\n✅ Du willst nur einen Bereich analysieren\n✅ Du hast schon Notizen und willst sie aktualisieren\n✅ Du arbeitest systematisch Zeile für Zeile\n\n💡 Profi-Tipp: Kombiniere mit Mehrfachauswahl!",
                        "When is R/C/B better than Auto Notes?\n\n✅ You only want to analyze one area\n✅ You already have notes and want to refresh them\n✅ You work systematically row by row\n\n💡 Pro tip: Combine it with multi-select!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // RANGE SELECTION (SHIFT+CLICK)
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🔲 Bereichsauswahl", "🔲 Range selection"),
                    Message = L(
                        "Mit Shift+Klick kannst du einen rechteckigen Bereich auswählen!\n\nDas ist extrem nützlich für:\n• Schnelles Setzen von Notizen\n• Löschen mehrerer Zellen\n• Analyse eines Blocks",
                        "With Shift+click you can select a rectangular range!\n\nThis is extremely useful for:\n• Quickly adding/removing notes\n• Clearing multiple cells\n• Analyzing a box"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🔲 So geht's", "🔲 How it works"),
                    Message = L(
                        "1. Klicke auf die erste Ecke (z.B. A1)\n2. Halte Shift gedrückt\n3. Klicke auf die gegenüberliegende Ecke (z.B. C3)\n\n→ Alle 9 Zellen dazwischen werden markiert!\n\n💡 Funktioniert auch diagonal über mehrere Blöcke.",
                        "1. Click the first corner (e.g., A1)\n2. Hold Shift\n3. Click the opposite corner (e.g., C3)\n\n→ All 9 cells in between will be selected!\n\n💡 Works diagonally across multiple boxes too."
                    ),
                    Position = MessagePosition.CenterLeft,
                    HighlightCells = new List<(int, int)> { (0, 0), (2, 2) },
                    HighlightStyle = HighlightStyle.Pulse
                },

                new ShowMessageStep
                {
                    Title = L("🔲 Erste Zelle wählen", "🔲 Select the first cell"),
                    Message = L(
                        "Wähle Zelle A1 aus (oben links).\n\n👆 Klicke darauf!",
                        "Select cell A1 (top-left).\n\n👆 Click it!"
                    ),
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
                    Title = L("🔲 Bereich erweitern", "🔲 Extend the range"),
                    Message = L(
                        "Halte jetzt Shift und klicke auf C3.\n\nDer gesamte obere linke Block (9 Zellen) wird markiert!",
                        "Now hold Shift and click C3.\n\nThe entire top-left box (9 cells) will be selected!"
                    ),
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
                    Title = L("🔲 Was kann ich damit tun?", "🔲 What can I do with it?"),
                    Message = L(
                        "Mit einem ausgewählten Bereich kannst du:\n\n📝 Im Notizen-Modus: Notiz in ALLEN Zellen setzen/entfernen\n🗑️ Mit Radiergummi: ALLE Zellen leeren\n🔤 Mit R/C/B: Notizen für den Bereich setzen\n\n💡 Spart enorm viel Zeit!",
                        "With a selected range you can:\n\n📝 In Notes mode: add/remove a note in ALL cells\n🗑️ With the eraser: clear ALL selected cells\n🔤 With R/C/B: fill notes for that range\n\n💡 Saves a ton of time!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // CTRL+CLICK MULTI-SELECT
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎯 Strg+Klick Auswahl", "🎯 Ctrl+Click selection"),
                    Message = L(
                        "Mit Strg+Klick wählst du einzelne Zellen aus - auch wenn sie nicht nebeneinander liegen!\n\n💡 Perfekt für:\n• Alle Zellen mit einer bestimmten Notiz\n• Zellen in verschiedenen Blöcken\n• Gezielte Bearbeitung",
                        "With Ctrl+click you can select individual cells — even if they're not adjacent!\n\n💡 Perfect for:\n• All cells that contain a specific note\n• Cells across different boxes\n• Targeted edits"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🎯 Kombination", "🎯 Combining selections"),
                    Message = L(
                        "Du kannst Shift und Strg kombinieren!\n\n1. Shift+Klick für ersten Bereich\n2. Strg+Shift+Klick für weiteren Bereich\n3. Strg+Klick für einzelne Zellen\n\n💡 So wählst du komplexe Muster aus!",
                        "You can combine Shift and Ctrl!\n\n1. Shift+click for the first range\n2. Ctrl+Shift+click for another range\n3. Ctrl+click for individual cells\n\n💡 This lets you select complex patterns!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // KEYBOARD SHORTCUTS
                // ========================================

                new ShowMessageStep
                {
                    Title = L("⌨️ Tastaturkürzel", "⌨️ Keyboard shortcuts"),
                    Message = L(
                        "Für schnelles Spielen gibt es viele Tastaturkürzel:\n\n• 1-9 → Zahl eingeben\n• N → Notizen-Modus umschalten\n• Entf/Backspace/0 → Zelle löschen\n• Pfeiltasten → Zelle wechseln\n• H → Hinweis anfordern",
                        "For fast play there are many keyboard shortcuts:\n\n• 1-9 → enter number\n• N → toggle Notes mode\n• Del/Backspace/0 → clear cell\n• Arrow keys → move selection\n• H → request a hint"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("⌨️ Navigation", "⌨️ Navigation"),
                    Message = L(
                        "Schnelle Navigation:\n\n• ↑↓←→ → Zur nächsten Zelle\n• Strg + ↑↓←→ → Zur nächsten LEEREN Zelle\n• Home → Zu A1 springen\n• End → Zu I9 springen\n\n💡 Für Profis: Nie die Maus benutzen!",
                        "Fast navigation:\n\n• ↑↓←→ → next cell\n• Ctrl + ↑↓←→ → next EMPTY cell\n• Home → jump to A1\n• End → jump to I9\n\n💡 Pro tip: never touch the mouse!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("⌨️ Mehrfachauswahl per Tastatur", "⌨️ Multi-select with keyboard"),
                    Message = L(
                        "Auch die Tastatur unterstützt Mehrfachauswahl:\n\n• Shift + Pfeiltasten → Bereich erweitern\n• Strg + Shift + Pfeiltasten → Bis zur nächsten leeren Zelle\n\n💡 Kombiniere mit Zahlen für Turbo-Eingabe!",
                        "The keyboard also supports multi-select:\n\n• Shift + Arrow keys → extend range\n• Ctrl + Shift + Arrow keys → extend to the next empty cell\n\n💡 Combine with numbers for turbo input!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // HIGHLIGHTING FEATURE
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎨 Highlighting", "🎨 Highlighting"),
                    Message = L(
                        "Wenn du eine Zahl eingibst, werden alle gleichen Zahlen hervorgehoben!\n\n💡 Das hilft dir:\n• Zu sehen wo eine Zahl schon ist\n• Fehler zu erkennen\n• Muster zu finden",
                        "When you enter a number, all matching numbers are highlighted!\n\n💡 This helps you:\n• See where a number already is\n• Spot mistakes\n• Find patterns"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🎨 Highlighting nutzen", "🎨 Using highlighting"),
                    Message = L(
                        "Klicke auf eine Zelle mit einer Zahl.\n\nAlle anderen Zellen mit der gleichen Zahl werden hervorgehoben!\n\n💡 Sehr nützlich für Hidden Singles - du siehst sofort wo die Zahl noch fehlt.",
                        "Click a cell that contains a number.\n\nAll other cells with the same number will be highlighted!\n\n💡 Very useful for Hidden Singles — you'll immediately see where a number is still missing."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // COMPLETION
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎓 Tutorial abgeschlossen!", "🎓 Tutorial Complete!"),
                    Message = L(
                        "Du kennst jetzt alle erweiterten Funktionen:\n\n✨ Auto-Notizen für schnellen Start\n🔤 R/C/B für gezielte Notizen\n🔲 Shift+Klick für Bereiche\n🎯 Strg+Klick für einzelne Zellen\n⌨️ Tastaturkürzel für Profis\n🎨 Highlighting für Übersicht\n\n🎮 Du bist bereit für schwere Puzzles!",
                        "You now know all advanced features:\n\n✨ Auto Notes for a quick start\n🔤 R/C/B for targeted notes\n🔲 Shift+click for ranges\n🎯 Ctrl+click for individual cells\n⌨️ Keyboard shortcuts for speed\n🎨 Highlighting for overview\n\n🎮 You're ready for hard puzzles!"
                    ),
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
            Name = _localizationService.Get("tutorial.advanced_techniques"),
            Description = _localizationService.Get("tutorial.advanced_techniques.desc"),
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
                    Title = L("Tutorial: Fortgeschrittene Techniken", "Tutorial: Advanced Techniques"),
                    Message = L(
                        "Willkommen zum Experten-Tutorial!\n\nHier lernst du die Techniken für mittlere und schwere Puzzles:\n\n• 👯 Naked & Hidden Pairs\n• 👉 Pointing Pairs\n• 📦 Box/Line Reduction\n• ✈️ X-Wing\n\nDiese Techniken sind essentiell für schwere Rätsel!",
                        "Welcome to the expert tutorial!\n\nHere you'll learn techniques for medium and hard puzzles:\n\n• 👯 Naked & Hidden Pairs\n• 👉 Pointing Pairs\n• 📦 Box/Line Reduction\n• ✈️ X-Wing\n\nThese techniques are essential for tough puzzles!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("📝 Voraussetzungen", "📝 Prerequisites"),
                    Message = L(
                        "Bevor wir beginnen:\n\n✅ Du solltest Naked Single kennen\n✅ Du solltest Hidden Single kennen\n✅ Du solltest mit Notizen arbeiten können\n\n💡 Falls nicht, mache zuerst die Tutorials \"Grundtechniken\" und \"Erweiterte Funktionen\"!",
                        "Before we begin:\n\n✅ You should know Naked Singles\n✅ You should know Hidden Singles\n✅ You should be comfortable using notes\n\n💡 If not, do the \"Basic Techniques\" and \"Advanced Features\" tutorials first!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // NAKED PAIR
                // ========================================

                new ShowMessageStep
                {
                    Title = L("👯 Naked Pair", "👯 Naked Pair"),
                    Message = L(
                        "Ein Naked Pair sind zwei Zellen in derselben Einheit (Zeile, Spalte oder Block), die GENAU die gleichen zwei Kandidaten haben.\n\nBeispiel:\n• Zelle A hat Kandidaten {3, 7}\n• Zelle B hat Kandidaten {3, 7}\n\n→ Die 3 und 7 MÜSSEN in diesen beiden Zellen sein!",
                        "A Naked Pair is two cells in the same unit (row, column, or box) that have EXACTLY the same two candidates.\n\nExample:\n• Cell A has candidates {3, 7}\n• Cell B has candidates {3, 7}\n\n→ The 3 and 7 MUST be in those two cells!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👯 Naked Pair Elimination", "👯 Naked Pair Elimination"),
                    Message = L(
                        "Was bedeutet das?\n\nWenn zwei Zellen nur {3, 7} haben können:\n→ KEINE andere Zelle in dieser Einheit kann 3 oder 7 sein!\n\n💡 Du kannst 3 und 7 aus allen anderen Zellen der Einheit entfernen.",
                        "What does that mean?\n\nIf two cells can only be {3, 7}:\n→ NO other cell in that unit can be 3 or 7!\n\n💡 You can remove 3 and 7 from all other cells in the unit."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👯 Naked Pair finden", "👯 Finding a Naked Pair"),
                    Message = L(
                        "So findest du Naked Pairs:\n\n1. Suche Zellen mit genau 2 Kandidaten\n2. Prüfe ob eine andere Zelle in derselben Einheit die GLEICHEN 2 Kandidaten hat\n3. Wenn ja → Naked Pair gefunden!\n4. Entferne diese Kandidaten aus allen anderen Zellen der Einheit",
                        "How to find Naked Pairs:\n\n1. Look for cells with exactly 2 candidates\n2. Check if another cell in the same unit has the SAME 2 candidates\n3. If yes → you found a Naked Pair!\n4. Remove those candidates from all other cells in the unit"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👯 Probiere es aus!", "👯 Try it!"),
                    Message = L(
                        "Nutze den Hinweis-Button um ein Naked Pair zu finden.\n\nDer Hinweis zeigt dir:\n• Wo das Pair ist\n• Welche Kandidaten betroffen sind\n• Welche Eliminierungen möglich sind\n\n👆 Klicke auf den Hinweis-Button!",
                        "Use the Hint button to find a Naked Pair.\n\nThe hint will show you:\n• Where the pair is\n• Which candidates are involved\n• Which eliminations are possible\n\n👆 Click the Hint button!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                // ========================================
                // HIDDEN PAIR
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🔍 Hidden Pair", "🔍 Hidden Pair"),
                    Message = L(
                        "Ein Hidden Pair ist schwerer zu finden!\n\nZwei Kandidaten kommen NUR in genau zwei Zellen einer Einheit vor - aber diese Zellen haben noch andere Kandidaten.\n\nBeispiel:\n• 3 und 7 sind nur in Zelle A und B möglich\n• Zelle A hat {2, 3, 7, 9}\n• Zelle B hat {1, 3, 5, 7}",
                        "A Hidden Pair is harder to spot!\n\nTwo candidates appear ONLY in exactly two cells of a unit — but those cells still have other candidates too.\n\nExample:\n• 3 and 7 are only possible in cells A and B\n• Cell A has {2, 3, 7, 9}\n• Cell B has {1, 3, 5, 7}"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🔍 Hidden Pair Elimination", "🔍 Hidden Pair Elimination"),
                    Message = L(
                        "Was bedeutet das?\n\nWenn 3 und 7 NUR in Zelle A und B sein können:\n→ A und B MÜSSEN 3 und 7 enthalten!\n→ Alle ANDEREN Kandidaten in A und B können entfernt werden!\n\n💡 Nach der Eliminierung wird aus dem Hidden Pair ein Naked Pair.",
                        "What does that mean?\n\nIf 3 and 7 can ONLY be in cells A and B:\n→ A and B MUST contain 3 and 7!\n→ All OTHER candidates in A and B can be removed!\n\n💡 After elimination, the Hidden Pair turns into a Naked Pair."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🔍 Hidden Pair finden", "🔍 Finding a Hidden Pair"),
                    Message = L(
                        "So findest du Hidden Pairs:\n\n1. Wähle eine Einheit (Zeile, Spalte, Block)\n2. Für jede Zahl: In welchen Zellen kommt sie vor?\n3. Gibt es zwei Zahlen, die NUR in denselben zwei Zellen vorkommen?\n4. Wenn ja → Hidden Pair!\n\n💡 Das ist aufwändiger als Naked Pair.",
                        "How to find Hidden Pairs:\n\n1. Choose a unit (row, column, box)\n2. For each number: which cells can it go in?\n3. Are there two numbers that appear ONLY in the same two cells?\n4. If yes → Hidden Pair!\n\n💡 This is more work than a Naked Pair."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // POINTING PAIRS
                // ========================================

                new ShowMessageStep
                {
                    Title = L("👉 Pointing Pair", "👉 Pointing Pair"),
                    Message = L(
                        "Ein Pointing Pair entsteht, wenn ein Kandidat in einem Block nur in einer Zeile oder Spalte vorkommt.\n\nBeispiel:\n• Im Block 1 kann die 5 nur in Zeile 1 stehen\n• Die 5 ist \"gefangen\" in dieser Zeile innerhalb des Blocks",
                        "A Pointing Pair happens when a candidate in a box appears only in a single row or column.\n\nExample:\n• In box 1, the 5 can only be in row 1\n• The 5 is \"locked\" into that row within the box"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👉 Pointing Elimination", "👉 Pointing Elimination"),
                    Message = L(
                        "Was bedeutet das?\n\nWenn die 5 im Block 1 nur in Zeile 1 sein kann:\n→ Die 5 in Zeile 1 MUSS im Block 1 sein!\n→ Entferne 5 aus allen Zellen von Zeile 1, die NICHT in Block 1 sind.\n\n💡 Der Kandidat \"zeigt\" aus dem Block hinaus.",
                        "What does that mean?\n\nIf the 5 in box 1 can only be in row 1:\n→ The 5 in row 1 MUST be inside box 1!\n→ Remove 5 from all cells in row 1 that are NOT in box 1.\n\n💡 The candidate \"points\" out of the box."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👉 Pointing Pair Beispiel", "👉 Pointing Pair example"),
                    Message = L(
                        "Visuell:\n\n  Block 1          Rest von Zeile 1\n┌─────────────┐   ┌─────────────────┐\n│ 5? │ 5? │   │   │ ❌5 │ ❌5 │ ❌5 │\n├────┼────┼───┤   └─────────────────┘\n│    │    │   │\n│    │    │   │\n└─────────────┘\n\nDie 5 kann aus dem Rest der Zeile entfernt werden!",
                        "Visual:\n\n  Box 1            Rest of row 1\n┌─────────────┐   ┌─────────────────┐\n│ 5? │ 5? │   │   │ ❌5 │ ❌5 │ ❌5 │\n├────┼────┼───┤   └─────────────────┘\n│    │    │   │\n│    │    │   │\n└─────────────┘\n\nThe 5 can be removed from the rest of the row!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👉 Probiere es aus!", "👉 Try it!"),
                    Message = L(
                        "Nutze den Hinweis-Button um einen Pointing Pair zu finden.\n\n👆 Klicke auf den Hinweis-Button!",
                        "Use the Hint button to find a Pointing Pair.\n\n👆 Click the Hint button!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                // ========================================
                // BOX/LINE REDUCTION
                // ========================================

                new ShowMessageStep
                {
                    Title = L("📦 Box/Line Reduction", "📦 Box/Line Reduction"),
                    Message = L(
                        "Box/Line Reduction ist das Gegenteil von Pointing:\n\nWenn ein Kandidat in einer Zeile/Spalte nur in einem Block vorkommt.\n\nBeispiel:\n• In Zeile 1 kann die 5 nur in Block 1 stehen\n• Die 5 ist \"gefangen\" in Block 1 innerhalb dieser Zeile",
                        "Box/Line Reduction is the opposite of Pointing:\n\nWhen a candidate in a row/column appears only within one box.\n\nExample:\n• In row 1, the 5 can only be in box 1\n• The 5 is \"locked\" into box 1 within that row"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("📦 Box/Line Elimination", "📦 Box/Line Elimination"),
                    Message = L(
                        "Was bedeutet das?\n\nWenn die 5 in Zeile 1 nur im Block 1 sein kann:\n→ Die 5 in Block 1 MUSS in Zeile 1 sein!\n→ Entferne 5 aus allen Zellen von Block 1, die NICHT in Zeile 1 sind.\n\n💡 Die Zeile \"reduziert\" die Möglichkeiten im Block.",
                        "What does that mean?\n\nIf the 5 in row 1 can only be in box 1:\n→ The 5 in box 1 MUST be in row 1!\n→ Remove 5 from all cells in box 1 that are NOT in row 1.\n\n💡 The row \"reduces\" options in the box."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("📦 Box/Line Beispiel", "📦 Box/Line example"),
                    Message = L(
                        "Visuell:\n\n  Zeile 1 in Block 1    Rest von Block 1\n┌─────────────────┐   ┌─────────────────┐\n│ 5? │ 5? │ 5? │   │ ❌5 │ ❌5 │ ❌5 │\n└─────────────────┘   │ ❌5 │ ❌5 │ ❌5 │\n                      └─────────────────┘\n\nDie 5 kann aus dem Rest des Blocks entfernt werden!",
                        "Visual:\n\n  Row 1 in box 1        Rest of box 1\n┌─────────────────┐   ┌─────────────────┐\n│ 5? │ 5? │ 5? │   │ ❌5 │ ❌5 │ ❌5 │\n└─────────────────┘   │ ❌5 │ ❌5 │ ❌5 │\n                      └─────────────────┘\n\nThe 5 can be removed from the rest of the box!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // X-WING
                // ========================================

                new ShowMessageStep
                {
                    Title = L("✈️ X-Wing", "✈️ X-Wing"),
                    Message = L(
                        "X-Wing ist eine mächtige Technik für schwere Puzzles!\n\nEin X-Wing entsteht, wenn ein Kandidat in genau zwei Zeilen NUR in denselben zwei Spalten vorkommt (oder umgekehrt).\n\n💡 Die vier Zellen bilden ein Rechteck.",
                        "X-Wing is a powerful technique for hard puzzles!\n\nAn X-Wing occurs when a candidate appears in exactly two rows ONLY in the same two columns (or vice versa).\n\n💡 The four cells form a rectangle."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("✈️ X-Wing Muster", "✈️ X-Wing pattern"),
                    Message = L(
                        "Beispiel für X-Wing mit der 5:\n\n     Spalte C    Spalte G\n        ↓           ↓\nZeile 2: 5? ─────── 5?  ←\n         │         │\n         │    X    │\n         │         │\nZeile 7: 5? ─────── 5?  ←\n\nDie 5 kommt in Zeile 2 und 7 NUR in Spalte C und G vor!",
                        "Example X-Wing with candidate 5:\n\n     Column C    Column G\n        ↓           ↓\nRow 2:   5? ─────── 5?  ←\n         │         │\n         │    X    │\n         │         │\nRow 7:   5? ─────── 5?  ←\n\nThe 5 appears in rows 2 and 7 ONLY in columns C and G!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("✈️ X-Wing Logik", "✈️ X-Wing logic"),
                    Message = L(
                        "Warum funktioniert X-Wing?\n\nDie 5 MUSS einmal in Zeile 2 und einmal in Zeile 7 stehen.\n\nEntweder:\n• 5 in C2 und G7\noder:\n• 5 in G2 und C7\n\n→ In beiden Fällen ist in Spalte C und G je eine 5!\n→ Entferne 5 aus allen anderen Zellen von Spalte C und G.",
                        "Why does X-Wing work?\n\nThe 5 MUST appear once in row 2 and once in row 7.\n\nEither:\n• 5 in C2 and G7\nor:\n• 5 in G2 and C7\n\n→ In both cases, columns C and G each contain a 5!\n→ Remove 5 from all other cells in columns C and G."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("✈️ X-Wing Elimination", "✈️ X-Wing elimination"),
                    Message = L(
                        "X-Wing Eliminierung:\n\n     Spalte C    Spalte G\n        ↓           ↓\nZeile 1: ❌5       ❌5\nZeile 2: 5? ─────── 5?  ← X-Wing\nZeile 3: ❌5       ❌5\n   ...    ❌5       ❌5\nZeile 7: 5? ─────── 5?  ← X-Wing\n   ...    ❌5       ❌5\n\nAlle anderen 5er in Spalte C und G werden entfernt!",
                        "X-Wing elimination:\n\n     Column C    Column G\n        ↓           ↓\nRow 1:   ❌5       ❌5\nRow 2:   5? ─────── 5?  ← X-Wing\nRow 3:   ❌5       ❌5\n   ...    ❌5       ❌5\nRow 7:   5? ─────── 5?  ← X-Wing\n   ...    ❌5       ❌5\n\nAll other 5s in columns C and G can be removed!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("✈️ Probiere es aus!", "✈️ Try it!"),
                    Message = L(
                        "X-Wings sind selten, aber mächtig!\n\nNutze den Hinweis-Button - er findet auch X-Wings.\n\n👆 Klicke auf den Hinweis-Button!",
                        "X-Wings are rare, but powerful!\n\nUse the Hint button — it can find X-Wings too.\n\n👆 Click the Hint button!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton },
                    WaitForAction = ExpectedAction.ClickButton
                },

                // ========================================
                // NAKED TRIPLE / QUAD
                // ========================================

                new ShowMessageStep
                {
                    Title = L("👯👯 Naked Triple", "👯👯 Naked Triple"),
                    Message = L(
                        "Naked Triples funktionieren wie Naked Pairs - aber mit drei Zellen!\n\nDrei Zellen in einer Einheit, die zusammen genau drei verschiedene Kandidaten haben.\n\nBeispiel:\n• Zelle A: {2, 5}\n• Zelle B: {2, 7}\n• Zelle C: {5, 7}\n\n→ Zusammen nur {2, 5, 7}!",
                        "Naked Triples work like Naked Pairs — but with three cells!\n\nThree cells in a unit that together contain exactly three different candidates.\n\nExample:\n• Cell A: {2, 5}\n• Cell B: {2, 7}\n• Cell C: {5, 7}\n\n→ Together only {2, 5, 7}!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👯👯 Triple Besonderheit", "👯👯 Triple detail"),
                    Message = L(
                        "Wichtig: Nicht jede Zelle muss alle drei Kandidaten haben!\n\n✅ Gültige Triples:\n• {2,5}, {2,7}, {5,7}\n• {2,5,7}, {2,5}, {5,7}\n• {2,5,7}, {2,5,7}, {2,5,7}\n\n❌ Ungültig:\n• {2,5,8}, {2,7}, {5,7} ← 4 Kandidaten!",
                        "Important: Not every cell has to contain all three candidates!\n\n✅ Valid triples:\n• {2,5}, {2,7}, {5,7}\n• {2,5,7}, {2,5}, {5,7}\n• {2,5,7}, {2,5,7}, {2,5,7}\n\n❌ Invalid:\n• {2,5,8}, {2,7}, {5,7} ← 4 candidates!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("👯👯👯 Naked Quad", "👯👯👯 Naked Quad"),
                    Message = L(
                        "Naked Quads: Vier Zellen mit zusammen genau vier Kandidaten.\n\nSeltener, aber das Prinzip ist gleich!\n\n💡 Je mehr Zellen, desto schwerer zu finden.\n💡 Der Hinweis-Button findet sie automatisch.",
                        "Naked Quads: four cells that together contain exactly four candidates.\n\nRarer, but the principle is the same!\n\n💡 The more cells, the harder to spot.\n💡 The Hint button can find them automatically."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // STRATEGY OVERVIEW
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎯 Strategie-Übersicht", "🎯 Strategy overview"),
                    Message = L(
                        "Welche Technik wann?\n\n1️⃣ Naked/Hidden Single - Immer zuerst!\n2️⃣ Naked/Hidden Pair - Wenn Singles nicht reichen\n3️⃣ Pointing/Box-Line - Für Block-Zeilen-Interaktion\n4️⃣ X-Wing - Für schwere Puzzles\n5️⃣ Triples/Quads - Wenn Pairs nicht reichen",
                        "Which technique when?\n\n1️⃣ Naked/Hidden Single — always first!\n2️⃣ Naked/Hidden Pair — when singles aren't enough\n3️⃣ Pointing/Box-Line — box/line interaction\n4️⃣ X-Wing — for hard puzzles\n5️⃣ Triples/Quads — when pairs aren't enough"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🎯 Systematisch arbeiten", "🎯 Work systematically"),
                    Message = L(
                        "Tipps für schwere Puzzles:\n\n1. Auto-Notizen am Anfang\n2. Alle Singles finden\n3. Nach Pairs suchen\n4. Pointing/Box-Line prüfen\n5. Bei Bedarf: X-Wing\n\n💡 Der Hinweis-Button zeigt die einfachste verfügbare Technik!",
                        "Tips for hard puzzles:\n\n1. Auto Notes at the start\n2. Find all singles\n3. Look for pairs\n4. Check Pointing/Box-Line\n5. If needed: X-Wing\n\n💡 The Hint button shows the easiest available technique!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // PRACTICE SUGGESTION
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎯 Jetzt üben!", "🎯 Practice time!"),
                    Message = L(
                        "Diese Techniken brauchen Übung!\n\n💡 Tipps zum Üben:\n• Starte mit Auto-Notizen\n• Nutze den Hinweis-Button zum Lernen\n• Analysiere jeden Hinweis genau\n• Versuche es beim nächsten Mal selbst",
                        "These techniques need practice!\n\n💡 Practice tips:\n• Start with Auto Notes\n• Use the Hint button to learn\n• Analyze every hint carefully\n• Try to do it yourself next time"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton }
                },

                new ShowMessageStep
                {
                    Title = L("📚 Weiterführende Ressourcen", "📚 Further resources"),
                    Message = L(
                        "Noch mehr lernen?\n\n🔗 Der Hinweis-Button erklärt JEDE Technik\n📖 Jeder Hinweis zeigt Schritt-für-Schritt\n🎮 Übung ist der beste Lehrer!\n\n💡 Das nächste Tutorial zeigt dir Challenge-Modi und Statistiken.",
                        "Want to learn more?\n\n🔗 The Hint button explains EVERY technique\n📖 Each hint is step-by-step\n🎮 Practice is the best teacher!\n\n💡 The next tutorial covers challenge modes and statistics."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // COMPLETION
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎓 Tutorial abgeschlossen!", "🎓 Tutorial Complete!"),
                    Message = L(
                        "Glückwunsch! Du kennst jetzt alle wichtigen Techniken:\n\n👯 Naked & Hidden Pairs\n👉 Pointing Pairs\n📦 Box/Line Reduction\n✈️ X-Wing\n👯👯 Triples & Quads\n\n💡 Mache als Nächstes das Tutorial \"Challenge-Modi\"!\n🎮 Viel Erfolg beim Üben!",
                        "Congrats! You now know the most important techniques:\n\n👯 Naked & Hidden Pairs\n👉 Pointing Pairs\n📦 Box/Line Reduction\n✈️ X-Wing\n👯👯 Triples & Quads\n\n💡 Next, do the \"Challenge Modes\" tutorial!\n🎮 Good luck practicing!"
                    ),
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
            Name = _localizationService.Get("tutorial.challenge_modes"),
            Description = _localizationService.Get("tutorial.challenge_modes.desc"),
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
                    Title = L("Tutorial: Challenge-Modi", "Tutorial: Challenge Modes"),
                    Message = L(
                        "Willkommen zum letzten Tutorial! 🏆\n\nHier lernst du alles über:\n\n• 💀 Deadly Mode - Kein Raum für Fehler!\n• ⏱️ Speedrunning - Jage Bestzeiten\n• 📊 Statistiken - Verfolge deinen Fortschritt\n• 🎯 Persönliche Ziele setzen\n\nBereit für die ultimative Herausforderung?",
                        "Welcome to the final tutorial! 🏆\n\nHere you'll learn about:\n\n• 💀 Deadly Mode — no room for mistakes!\n• ⏱️ Speedrunning — chase personal bests\n• 📊 Statistics — track your progress\n• 🎯 Setting personal goals\n\nReady for the ultimate challenge?"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // DEADLY MODE DEEP DIVE
                // ========================================

                new ShowMessageStep
                {
                    Title = L("💀 Deadly Mode", "💀 Deadly Mode"),
                    Message = L(
                        "Der Deadly Mode ist für echte Sudoku-Meister!\n\n⚠️ Die Regel ist einfach aber gnadenlos:\n\n🔴 3 Fehler = Spiel verloren!\n\nKein Zurück, keine zweite Chance.\nJeder Zug muss sitzen!",
                        "Deadly Mode is for true Sudoku masters!\n\n⚠️ The rule is simple but ruthless:\n\n🔴 3 mistakes = game over!\n\nNo undo, no second chance.\nEvery move must be correct!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.MistakesLabel }
                },

                new ShowMessageStep
                {
                    Title = L("💀 Warum Deadly Mode?", "💀 Why Deadly Mode?"),
                    Message = L(
                        "Deadly Mode trainiert dich:\n\n✅ Sorgfältiger zu arbeiten\n✅ Notizen konsequent zu nutzen\n✅ Nie zu raten\n✅ Logik vor Intuition\n\n💡 Du wirst ein besserer Spieler!\n\n⚙️ Aktiviere Deadly Mode in den Einstellungen.",
                        "Deadly Mode trains you to:\n\n✅ Be more careful\n✅ Use notes consistently\n✅ Never guess\n✅ Put logic over intuition\n\n💡 You'll become a better player!\n\n⚙️ Enable Deadly Mode in Settings."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("💀 Deadly Mode Strategien", "💀 Deadly Mode strategies"),
                    Message = L(
                        "So überlebst du den Deadly Mode:\n\n1️⃣ IMMER mit Auto-Notizen starten\n2️⃣ Keine Zahl ohne Beweis eintragen\n3️⃣ Bei Unsicherheit → Hinweis nutzen\n4️⃣ Systematisch arbeiten, nie springen\n5️⃣ Lieber 5 Min länger als 1 Fehler!",
                        "How to survive Deadly Mode:\n\n1️⃣ ALWAYS start with Auto Notes\n2️⃣ Never place a number without proof\n3️⃣ If unsure → use a hint\n4️⃣ Work systematically — don't jump around\n5️⃣ Better 5 minutes slower than 1 mistake!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("💀 Die 3-Fehler-Anzeige", "💀 The 3-mistake display"),
                    Message = L(
                        "Oben rechts siehst du deine Fehler:\n\n❌ ○ ○ = 1 Fehler - Vorsicht!\n❌ ❌ ○ = 2 Fehler - Letzte Chance!\n❌ ❌ ❌ = Game Over!\n\n💡 Jeder Fehler ist eine Lektion.\n💡 Analysiere: WARUM hast du den Fehler gemacht?",
                        "Top-right you can see your mistakes:\n\n❌ ○ ○ = 1 mistake — careful!\n❌ ❌ ○ = 2 mistakes — last chance!\n❌ ❌ ❌ = game over!\n\n💡 Every mistake is a lesson.\n💡 Ask yourself: WHY did you make it?"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.MistakesLabel }
                },

                // ========================================
                // TIMER & SPEEDRUNNING
                // ========================================

                new ShowMessageStep
                {
                    Title = L("⏱️ Der Timer", "⏱️ The timer"),
                    Message = L(
                        "Oben siehst du die verstrichene Zeit.\n\nDer Timer läuft sobald du startest und pausiert automatisch wenn du:\n\n• Das Spiel pausierst\n• Zur Hilfe wechselst\n• Das Fenster minimierst\n\n💡 Fair Play ist garantiert!",
                        "At the top you can see the elapsed time.\n\nThe timer starts when you begin and pauses automatically when you:\n\n• Pause the game\n• Open help\n• Minimize the window\n\n💡 Fair play guaranteed!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.Timer }
                },

                new ShowMessageStep
                {
                    Title = L("⏱️ Speedrunning Basics", "⏱️ Speedrunning basics"),
                    Message = L(
                        "Tipps für schnelleres Lösen:\n\n🚀 Tastatur statt Maus!\n   • Pfeiltasten zum Navigieren\n   • Zahlen direkt tippen\n   • N für Notiz-Modus\n\n🚀 Muster erkennen!\n   • Übung macht schneller\n   • Häufige Techniken automatisieren",
                        "Tips for solving faster:\n\n🚀 Keyboard instead of mouse!\n   • Arrow keys to navigate\n   • Type numbers directly\n   • N for Notes mode\n\n🚀 Recognize patterns!\n   • Practice makes you faster\n   • Automate common techniques"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("⏱️ Richtwerte für Zeiten", "⏱️ Time benchmarks"),
                    Message = L(
                        "Wie schnell bist du? Vergleiche:\n\n🟢 LEICHT:\n   Anfänger: 10-15 Min\n   Fortgeschritten: 5-10 Min\n   Profi: unter 3 Min\n\n🟠 MITTEL:\n   Anfänger: 20-30 Min\n   Fortgeschritten: 10-15 Min\n   Profi: unter 8 Min",
                        "How fast are you? Benchmarks:\n\n🟢 EASY:\n   Beginner: 10–15 min\n   Intermediate: 5–10 min\n   Pro: under 3 min\n\n🟠 MEDIUM:\n   Beginner: 20–30 min\n   Intermediate: 10–15 min\n   Pro: under 8 min"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("⏱️ Schwere Puzzles", "⏱️ Hard puzzles"),
                    Message = L(
                        "🔴 SCHWER:\n   Anfänger: 45-60 Min\n   Fortgeschritten: 20-30 Min\n   Profi: unter 15 Min\n\n💎 EXPERTE:\n   Weltklasse: unter 5 Min für schwer!\n\n💡 Vergleiche nur mit dir selbst.\n💡 Jede Verbesserung zählt!",
                        "🔴 HARD:\n   Beginner: 45–60 min\n   Intermediate: 20–30 min\n   Pro: under 15 min\n\n💎 EXPERT:\n   World-class: under 5 minutes for hard!\n\n💡 Only compare yourself to yourself.\n💡 Every improvement counts!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // STATISTICS
                // ========================================

                new ShowMessageStep
                {
                    Title = L("📊 Deine Statistiken", "📊 Your statistics"),
                    Message = L(
                        "SudokuSen speichert alles!\n\n📈 Erfasste Daten:\n• Gelöste Puzzles pro Schwierigkeit\n• Durchschnittliche Lösungszeit\n• Beste Zeit (Rekord!)\n• Fehlerquote\n• Verwendete Hinweise\n\n💡 Finde dein Dashboard im Hauptmenü!",
                        "SudokuSen tracks everything!\n\n📈 Tracked data:\n• Solved puzzles per difficulty\n• Average solve time\n• Best time (record!)\n• Mistake rate\n• Hints used\n\n💡 Find your dashboard in the main menu!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("📊 Fortschritt verfolgen", "📊 Track progress"),
                    Message = L(
                        "Warum Statistiken wichtig sind:\n\n📉 Erkenne Muster:\n   • Welche Schwierigkeit liegt dir?\n   • Wo brauchst du mehr Übung?\n\n📈 Motivation:\n   • Sieh deinen Fortschritt!\n   • Feiere neue Rekorde!\n\n💡 Kleine Verbesserungen summieren sich!",
                        "Why stats matter:\n\n📉 Spot patterns:\n   • Which difficulty suits you?\n   • Where do you need more practice?\n\n📈 Motivation:\n   • See your improvement!\n   • Celebrate new records!\n\n💡 Small gains add up!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // GAME HISTORY
                // ========================================

                new ShowMessageStep
                {
                    Title = L("📜 Spielverlauf", "📜 Game history"),
                    Message = L(
                        "Jedes Spiel wird gespeichert:\n\n📋 Du siehst:\n• Datum und Uhrzeit\n• Schwierigkeitsstufe\n• Deine Zeit\n• Fehleranzahl\n• Hinweise verwendet\n• Ob du gewonnen hast\n\n💡 Analysiere deine besten UND schlechtesten Spiele!",
                        "Every game is saved:\n\n📋 You'll see:\n• Date and time\n• Difficulty\n• Your time\n• Mistake count\n• Hints used\n• Whether you won\n\n💡 Analyze your best AND your worst games!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("📜 Aus Fehlern lernen", "📜 Learn from mistakes"),
                    Message = L(
                        "Ein verlorenes Spiel ist kein Versagen!\n\n🔍 Frage dich:\n• Wo habe ich geraten statt gedacht?\n• Welche Technik hätte geholfen?\n• War ich zu schnell oder müde?\n\n💡 Jeder Fehler macht dich besser!\n💡 Die besten Spieler haben am meisten verloren.",
                        "A lost game isn't failure!\n\n🔍 Ask yourself:\n• Where did I guess instead of think?\n• Which technique would have helped?\n• Was I too fast or tired?\n\n💡 Every mistake makes you better.\n💡 The best players have lost the most."
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // DIFFICULTY PROGRESSION
                // ========================================

                new ShowMessageStep
                {
                    Title = L("📈 Schwierigkeitsstufen", "📈 Difficulty levels"),
                    Message = L(
                        "SudokuSen bietet für jeden etwas:\n\n👶 KIDS (4×4)\n   Perfekt für Kinder und absolute Anfänger\n\n🟢 LEICHT (9×9)\n   Nur Naked & Hidden Singles\n   → Ideal zum Aufwärmen!",
                        "SudokuSen has something for everyone:\n\n👶 KIDS (4×4)\n   Perfect for kids and total beginners\n\n🟢 EASY (9×9)\n   Only Naked & Hidden Singles\n   → Great for warming up!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.DifficultyLabel }
                },

                new ShowMessageStep
                {
                    Title = L("📈 Mittlere Stufen", "📈 Mid tiers"),
                    Message = L(
                        "🟠 MITTEL\n   + Pointing Pairs\n   + Box/Line Reduction\n   → Hier lernst du die meisten Techniken!\n\n🟠 MITTEL+\n   + Naked/Hidden Pairs\n   → Der Übergang zum Fortgeschrittenen",
                        "🟠 MEDIUM\n   + Pointing Pairs\n   + Box/Line Reduction\n   → You'll learn most techniques here!\n\n🟠 MEDIUM+\n   + Naked/Hidden Pairs\n   → The step towards advanced"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("📈 Experten-Stufen", "📈 Expert tiers"),
                    Message = L(
                        "🔴 SCHWER\n   + X-Wing\n   + Naked/Hidden Triples\n   → Echte Herausforderungen!\n\n💎 EXPERTE\n   + Swordfish, XY-Wing\n   + Komplexe Verkettungen\n   → Nur für die Besten!",
                        "🔴 HARD\n   + X-Wing\n   + Naked/Hidden Triples\n   → Real challenges!\n\n💎 EXPERT\n   + Swordfish, XY-Wing\n   + Complex chains\n   → Only for the best!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // PERSONAL CHALLENGES
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎯 Setze dir Ziele!", "🎯 Set goals!"),
                    Message = L(
                        "Persönliche Herausforderungen:\n\n🥉 BRONZE:\n   • 10 Puzzles auf Leicht lösen\n   • Zeit unter 15 Min schaffen\n\n🥈 SILBER:\n   • 10 Puzzles auf Mittel lösen\n   • Ohne Hinweise gewinnen",
                        "Personal challenges:\n\n🥉 BRONZE:\n   • Solve 10 easy puzzles\n   • Finish under 15 minutes\n\n🥈 SILVER:\n   • Solve 10 medium puzzles\n   • Win without hints"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🎯 Höhere Ziele", "🎯 Higher goals"),
                    Message = L(
                        "🥇 GOLD:\n   • 10 Puzzles auf Schwer lösen\n   • Max 3 Hinweise pro Spiel\n   • Zeit unter 30 Min\n\n💎 DIAMANT:\n   • Schweres Puzzle ohne Hinweise\n   • Im Deadly Mode gewinnen!\n   • Unter 20 Min schaffen",
                        "🥇 GOLD:\n   • Solve 10 hard puzzles\n   • Max 3 hints per game\n   • Under 30 minutes\n\n💎 DIAMOND:\n   • Hard puzzle with no hints\n   • Win in Deadly Mode\n   • Finish under 20 minutes"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🎯 Ultimate Challenge", "🎯 Ultimate challenge"),
                    Message = L(
                        "🏆 MEISTER-CHALLENGE:\n\n   ✅ Schweres Puzzle\n   ✅ Deadly Mode (3 Fehler = Game Over)\n   ✅ Keine Hinweise\n   ✅ Unter 15 Minuten\n\nSchaffst du das? 💪\n\n💡 Tipp: Erst alle Tutorials abschließen!",
                        "🏆 MASTER CHALLENGE:\n\n   ✅ Hard puzzle\n   ✅ Deadly Mode (3 mistakes = game over)\n   ✅ No hints\n   ✅ Under 15 minutes\n\nCan you do it? 💪\n\n💡 Tip: finish all tutorials first!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // HINTS STRATEGY
                // ========================================

                new ShowMessageStep
                {
                    Title = L("💡 Hinweise als Lehrer", "💡 Hints as a teacher"),
                    Message = L(
                        "Der Hinweis-Button ist KEIN Cheat!\n\n📚 Er ist dein Lehrer:\n• Zeigt die einfachste verfügbare Technik\n• Erklärt WARUM es funktioniert\n• Hebt relevante Zellen hervor\n\n💡 Nutze Hinweise zum LERNEN, nicht zum Abkürzen!",
                        "The Hint button is NOT cheating!\n\n📚 It's your teacher:\n• Shows the easiest available technique\n• Explains WHY it works\n• Highlights relevant cells\n\n💡 Use hints to LEARN, not to skip thinking!"
                    ),
                    Position = MessagePosition.CenterLeft,
                    PointTo = new TutorialTarget { Type = TargetType.HintButton }
                },

                new ShowMessageStep
                {
                    Title = L("💡 Hinweis-Limitierung", "💡 Limiting hints"),
                    Message = L(
                        "Challenge: Limitiere deine Hinweise!\n\n📊 Tracking-Idee:\n   Woche 1: Max 10 Hinweise pro Puzzle\n   Woche 2: Max 5 Hinweise\n   Woche 3: Max 3 Hinweise\n   Woche 4: Max 1 Hinweis\n   Woche 5: Keine Hinweise!\n\n💡 Langsam reduzieren = nachhaltiges Lernen",
                        "Challenge: limit your hints!\n\n📊 Tracking idea:\n   Week 1: max 10 hints per puzzle\n   Week 2: max 5 hints\n   Week 3: max 3 hints\n   Week 4: max 1 hint\n   Week 5: no hints!\n\n💡 Reduce slowly = sustainable learning"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // FINAL TIPS
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🌟 Letzte Tipps", "🌟 Final tips"),
                    Message = L(
                        "Geheimnisse der Sudoku-Meister:\n\n1️⃣ Täglich 1-2 Puzzles = stetiger Fortschritt\n2️⃣ Verschiedene Schwierigkeiten spielen\n3️⃣ Nach Frustration: Pause machen!\n4️⃣ Fehler analysieren, nicht ignorieren\n5️⃣ Spaß haben! 🎮",
                        "Secrets of Sudoku masters:\n\n1️⃣ 1–2 puzzles a day = steady progress\n2️⃣ Play different difficulties\n3️⃣ If frustrated: take a break!\n4️⃣ Analyze mistakes — don't ignore them\n5️⃣ Have fun! 🎮"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🌟 Routine aufbauen", "🌟 Build a routine"),
                    Message = L(
                        "Die perfekte Sudoku-Routine:\n\n☀️ Morgens: 1 leichtes Puzzle zum Aufwärmen\n🌙 Abends: 1 schwieriges Puzzle zur Challenge\n\n📅 Wochenende: Deadly Mode ausprobieren!\n\n💡 Konsistenz schlägt Intensität.\n💡 15 Min täglich > 2 Std am Wochenende",
                        "The perfect Sudoku routine:\n\n☀️ Morning: 1 easy puzzle to warm up\n🌙 Evening: 1 hard puzzle as a challenge\n\n📅 Weekend: try Deadly Mode!\n\n💡 Consistency beats intensity.\n💡 15 min daily > 2 hours on the weekend"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                // ========================================
                // COMPLETION
                // ========================================

                new ShowMessageStep
                {
                    Title = L("🎓 Alle Tutorials abgeschlossen!", "🎓 All tutorials complete!"),
                    Message = L(
                        "HERZLICHEN GLÜCKWUNSCH! 🎉\n\nDu hast ALLE Tutorials gemeistert:\n\n✅ Erste Schritte\n✅ Grundtechniken\n✅ Erweiterte Funktionen\n✅ Fortgeschrittene Techniken\n✅ Challenge-Modi\n\nDu bist jetzt ein vollständig ausgebildeter Sudoku-Spieler!",
                        "CONGRATULATIONS! 🎉\n\nYou've completed ALL tutorials:\n\n✅ Getting Started\n✅ Basic Techniques\n✅ Advanced Features\n✅ Advanced Techniques\n✅ Challenge Modes\n\nYou're now a fully trained Sudoku player!"
                    ),
                    Position = MessagePosition.CenterLeft
                },

                new ShowMessageStep
                {
                    Title = L("🚀 Deine Reise beginnt!", "🚀 Your journey begins!"),
                    Message = L(
                        "Was kommt als Nächstes?\n\n1️⃣ Starte mit einem leichten Puzzle\n2️⃣ Arbeite dich durch die Schwierigkeiten\n3️⃣ Verfolge deine Statistiken\n4️⃣ Wage den Deadly Mode!\n5️⃣ Jage deine Bestzeiten!\n\n🏆 Viel Erfolg, Sudoku-Meister! 🏆",
                        "What's next?\n\n1️⃣ Start with an easy puzzle\n2️⃣ Work your way through difficulties\n3️⃣ Track your statistics\n4️⃣ Try Deadly Mode\n5️⃣ Chase your personal bests!\n\n🏆 Good luck, Sudoku master! 🏆"
                    ),
                    Position = MessagePosition.CenterLeft
                }
            }
        };

        return tutorial;
    }
}
