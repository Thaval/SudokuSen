namespace SudokuSen.Services;
using SudokuSen.UI;

/// <summary>
/// Autoload: Verwaltet den App-Zustand und die Navigation
/// </summary>
public partial class AppState : Node
{
    [Signal]
    public delegate void SceneChangeRequestedEventHandler(string scenePath);

    [Signal]
    public delegate void GameStartedEventHandler();

    [Signal]
    public delegate void GameEndedEventHandler(int status);

    // Singleton-Zugriff
    public static AppState Instance { get; private set; } = null!;

    // Aktuelles Spiel
    public SudokuGameState? CurrentGame { get; private set; }

    // Szene, zu der aus dem Spiel zurück navigiert werden soll (z. B. Puzzles-/Szenarien-Übersicht)
    private string? _returnScenePath;

    // Flag für neues Spiel vs fortgesetztes Spiel
    public bool IsNewGame { get; private set; }

    // History replay flag
    public bool IsHistoryReplay { get; private set; }

    // Scene Paths
    public const string SCENE_MAIN_MENU = "res://Scenes/MainMenu.tscn";
    public const string SCENE_DIFFICULTY = "res://Scenes/DifficultyMenu.tscn";
    public const string SCENE_GAME = "res://Scenes/GameScene.tscn";
    public const string SCENE_SETTINGS = "res://Scenes/SettingsMenu.tscn";
    public const string SCENE_HISTORY = "res://Scenes/HistoryMenu.tscn";
    public const string SCENE_STATS = "res://Scenes/StatsMenu.tscn";
    public const string SCENE_TIPS = "res://Scenes/TipsMenu.tscn";
    public const string SCENE_SCENARIOS = "res://Scenes/ScenariosMenu.tscn";
    public const string SCENE_PUZZLES = "res://Scenes/PuzzlesMenu.tscn";

    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// Navigiert zu einer Szene
    /// </summary>
    public void NavigateTo(string scenePath)
    {
        // Prefer routing through the Main scene container if it is active
        if (GetTree().CurrentScene is Main main)
        {
            main.LoadSceneDirect(scenePath);
            return;
        }

        // Fallback: emit signal and change root scene directly
        EmitSignal(SignalName.SceneChangeRequested, scenePath);
        GetTree().ChangeSceneToFile(scenePath);
    }

    /// <summary>
    /// Zurück zum Hauptmenü
    /// </summary>
    public void GoToMainMenu()
    {
        NavigateTo(SCENE_MAIN_MENU);
    }

    /// <summary>
    /// Merkt sich die aktuelle Szene als Rücksprungziel, falls wir in ein Spiel wechseln.
    /// </summary>
    private void CaptureReturnScene()
    {
        _returnScenePath = GetTree().CurrentScene?.SceneFilePath;
        GD.Print($"[AppState] Captured return scene: {_returnScenePath}");
    }

    /// <summary>
    /// Geht zurück zur gemerkten Szene oder gibt false zurück, falls keine hinterlegt ist.
    /// </summary>
    public bool TryReturnToCapturedScene()
    {
        GD.Print($"[AppState] TryReturnToCapturedScene: _returnScenePath = {_returnScenePath}");
        if (string.IsNullOrEmpty(_returnScenePath))
            return false;

        var target = _returnScenePath;
        _returnScenePath = null;
        NavigateTo(target!);
        return true;
    }

    /// <summary>
    /// Startet ein neues Spiel
    /// </summary>
    public void StartNewGame(Difficulty difficulty)
    {
        IsHistoryReplay = false;
        var saveService = GetNode<SaveService>("/root/SaveService");

        // Neues Spiel kommt von der Schwierigkeits- oder Hauptmenü-Route -> kein spezieller Rücksprung
        _returnScenePath = null;

        CurrentGame = Logic.SudokuGenerator.Generate(difficulty);
        CurrentGame.IsDeadlyMode = saveService.Settings.DeadlyModeEnabled;
        ApplyChallengeSettings(CurrentGame, saveService.Settings);
        IsNewGame = true;

        saveService.SaveCurrentGame(CurrentGame);

        EmitSignal(SignalName.GameStarted);
        NavigateTo(SCENE_GAME);
    }

    public void StartDailyGame()
    {
        IsHistoryReplay = false;
        var saveService = GetNode<SaveService>("/root/SaveService");

        _returnScenePath = null;

        string date = DateTime.Today.ToString("yyyy-MM-dd");
        int seed = int.Parse(DateTime.Today.ToString("yyyyMMdd"));

        CurrentGame = Logic.SudokuGenerator.Generate(Difficulty.Medium, seed);
        CurrentGame.IsDaily = true;
        CurrentGame.DailyDate = date;
        CurrentGame.IsDeadlyMode = saveService.Settings.DeadlyModeEnabled;
        ApplyChallengeSettings(CurrentGame, saveService.Settings);
        IsNewGame = true;

        // mark played date
        saveService.Settings.DailyLastPlayedDate = date;
        saveService.SaveSettings();

        saveService.SaveCurrentGame(CurrentGame);

        EmitSignal(SignalName.GameStarted);
        NavigateTo(SCENE_GAME);
    }

    /// <summary>
    /// Startet ein vorgebautes (prebuilt) Puzzle.
    /// </summary>
    public void StartPrebuiltPuzzle(string puzzleId)
    {
        IsHistoryReplay = false;
        var saveService = GetNode<SaveService>("/root/SaveService");

        CaptureReturnScene();

        var puzzle = PrebuiltPuzzleLibrary.GetById(puzzleId);
        if (puzzle == null)
        {
            GD.PrintErr($"[AppState] Prebuilt puzzle not found: {puzzleId}");
            return;
        }

        CurrentGame = puzzle.ToGameState();
        CurrentGame.IsDeadlyMode = saveService.Settings.DeadlyModeEnabled;
        ApplyChallengeSettings(CurrentGame, saveService.Settings);
        IsNewGame = true;
        IsHistoryReplay = false;

        // Save prebuilt puzzle games so they can be continued
        saveService.SaveCurrentGame(CurrentGame);

        EmitSignal(SignalName.GameStarted);
        NavigateTo(SCENE_GAME);
    }

    /// <summary>
    /// Startet ein Szenario-Spiel für eine bestimmte Technik
    /// </summary>
    public void StartScenarioGame(string techniqueId)
    {
        IsHistoryReplay = false;
        var saveService = GetNode<SaveService>("/root/SaveService");

        CaptureReturnScene();

        // Bestimme passende Schwierigkeit basierend auf Technik (1=Easy, 2=Medium, 3=Hard, 4=Insane)
        Difficulty difficulty = Difficulty.Medium;
        if (TechniqueInfo.Techniques.TryGetValue(techniqueId, out var technique))
        {
            difficulty = technique.DefaultDifficulty switch
            {
                1 => Difficulty.Easy,
                2 => Difficulty.Medium,
                3 => Difficulty.Hard,
                4 => Difficulty.Insane,
                _ => Difficulty.Medium
            };
        }

        // Generiere Puzzle mit Fokus auf diese Technik
        CurrentGame = Logic.SudokuGenerator.GenerateForTechnique(techniqueId, difficulty);
        CurrentGame.IsDeadlyMode = saveService.Settings.DeadlyModeEnabled;
        CurrentGame.IsScenario = true;
        CurrentGame.ScenarioTechnique = techniqueId;
        ApplyChallengeSettings(CurrentGame, saveService.Settings);
        IsNewGame = true;
        IsHistoryReplay = false;

        // Don't save scenario games to regular save slot
        // saveService.SaveCurrentGame(CurrentGame);

        EmitSignal(SignalName.GameStarted);
        NavigateTo(SCENE_GAME);
    }

    /// <summary>
    /// Startet ein Tutorial-Spiel
    /// </summary>
    public void StartTutorialGame(string tutorialId)
    {
        IsHistoryReplay = false;
        var saveService = GetNode<SaveService>("/root/SaveService");
        var tutorialService = GetNodeOrNull<TutorialService>("/root/TutorialService");

        CaptureReturnScene();

        if (tutorialService == null)
        {
            GD.PrintErr("[AppState] TutorialService not found!");
            return;
        }

        var tutorial = tutorialService.GetTutorial(tutorialId);
        if (tutorial == null)
        {
            GD.PrintErr($"[AppState] Tutorial not found: {tutorialId}");
            return;
        }

        // Create a simple puzzle for the tutorial
        // For "getting_started", use an almost-complete puzzle
        CurrentGame = CreateTutorialPuzzle(tutorialId);
        CurrentGame.IsDeadlyMode = false; // Never deadly mode in tutorials
        CurrentGame.IsTutorial = true;
        CurrentGame.TutorialId = tutorialId;
        IsNewGame = true;

        // Don't save tutorial games to regular save slot
        // saveService.SaveCurrentGame(CurrentGame);

        EmitSignal(SignalName.GameStarted);
        NavigateTo(SCENE_GAME);

        // Start the tutorial after scene loads
        CallDeferred(nameof(StartTutorialDelayed), tutorialId);
    }

    private void StartTutorialDelayed(string tutorialId)
    {
        var tutorialService = GetNodeOrNull<TutorialService>("/root/TutorialService");
        tutorialService?.StartTutorial(tutorialId);
    }

    private Models.SudokuGameState CreateTutorialPuzzle(string tutorialId)
    {
        // Create pre-defined puzzles for tutorials
        var state = new Models.SudokuGameState
        {
            Difficulty = Difficulty.Easy,
            StartTime = DateTime.Now
        };

        // Initialize with a nearly-complete puzzle for "getting_started"
        // This is a valid Sudoku solution with only a few cells empty
        int[,] solution = new int[,]
        {
            { 5, 3, 4, 6, 7, 8, 9, 1, 2 },
            { 6, 7, 2, 1, 9, 5, 3, 4, 8 },
            { 1, 9, 8, 3, 4, 2, 5, 6, 7 },
            { 8, 5, 9, 7, 6, 1, 4, 2, 3 },
            { 4, 2, 6, 8, 5, 3, 7, 9, 1 },
            { 7, 1, 3, 9, 2, 4, 8, 5, 6 },
            { 9, 6, 1, 5, 3, 7, 2, 8, 4 },
            { 2, 8, 7, 4, 1, 9, 6, 3, 5 },
            { 3, 4, 5, 2, 8, 6, 1, 7, 9 }
        };

        // Cells to leave empty for tutorial (row, col)
        var emptyCells = tutorialId switch
        {
            "getting_started" => new[] { (4, 4), (0, 2), (2, 6), (6, 1), (8, 8) }, // 5 cells
            "basic_techniques" => new[] { (4, 4), (0, 2), (2, 6), (6, 1), (8, 8) }, // Same as getting_started for technique practice
            "notes_tutorial" => new[] { (0, 0), (0, 1), (1, 0), (1, 1), (2, 2), (3, 3), (4, 4), (5, 5) },
            _ => new[] { (4, 4), (0, 0), (8, 8) }
        };

        var emptySet = new HashSet<(int, int)>(emptyCells);

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                bool isEmpty = emptySet.Contains((r, c));
                state.Grid[r, c] = new Models.SudokuCell
                {
                    Value = isEmpty ? 0 : solution[r, c],
                    Solution = solution[r, c],
                    IsGiven = !isEmpty,
                    Notes = new bool[9]
                };
            }
        }

        return state;
    }

    /// <summary>
    /// Setzt ein gespeichertes Spiel fort
    /// </summary>
    public void ContinueGame()
    {
        IsHistoryReplay = false;
        var saveService = GetNode<SaveService>("/root/SaveService");

        if (saveService.CurrentGame != null)
        {
            CurrentGame = saveService.CurrentGame;
            IsNewGame = false;

            EmitSignal(SignalName.GameStarted);
            NavigateTo(SCENE_GAME);
        }
    }

    /// <summary>
    /// Beendet das aktuelle Spiel
    /// </summary>
    public void EndGame(GameStatus status)
    {
        IsHistoryReplay = false;
        if (CurrentGame == null) return;

        var saveService = GetNode<SaveService>("/root/SaveService");

        CurrentGame.Status = status;

        // Füge zum Verlauf hinzu
        var entry = HistoryEntry.FromGameState(CurrentGame, status);
        saveService.AddHistoryEntry(entry);

        // Daily streaks
        if (status == GameStatus.Won && CurrentGame.IsDaily && !string.IsNullOrWhiteSpace(CurrentGame.DailyDate))
        {
            saveService.Settings.MarkDailyCompleted(CurrentGame.DailyDate!);
            saveService.SaveSettings();
        }

        // Prebuilt puzzle completion
        if (status == GameStatus.Won && !string.IsNullOrWhiteSpace(CurrentGame.PrebuiltPuzzleId))
        {
            saveService.Settings.MarkPrebuiltPuzzleCompleted(CurrentGame.PrebuiltPuzzleId!);
            saveService.SaveSettings();
        }

        // Lösche SaveGame wenn beendet (nicht InProgress)
        if (status != GameStatus.InProgress)
        {
            saveService.DeleteSaveGame();
        }

        EmitSignal(SignalName.GameEnded, (int)status);
    }

    private static void ApplyChallengeSettings(SudokuGameState game, SettingsData settings)
    {
        game.ChallengeNoNotes = settings.ChallengeNoNotes;
        game.ChallengePerfectRun = settings.ChallengePerfectRun;
        game.ChallengeHintLimit = Math.Max(0, settings.ChallengeHintLimit);
        game.ChallengeTimeAttackSeconds = Math.Max(0, settings.ChallengeTimeAttackMinutes) * 60;
    }

    /// <summary>
    /// Speichert den aktuellen Spielstand
    /// </summary>
    public void SaveGame()
    {
        if (CurrentGame == null) return;

        if (IsHistoryReplay)
        {
            GD.Print("[AppState] Skip saving during history replay");
            return;
        }

        // Don't save tutorial or scenario games
        if (CurrentGame.IsTutorial || CurrentGame.IsScenario)
        {
            GD.Print("[AppState] Skipping save for tutorial/scenario game");
            return;
        }

        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.SaveCurrentGame(CurrentGame);
    }

    /// <summary>
    /// Aktualisiert die Spielzeit
    /// </summary>
    public void UpdateElapsedTime(double elapsed)
    {
        if (CurrentGame != null && !IsHistoryReplay)
        {
            CurrentGame.ElapsedSeconds = elapsed;
        }
    }

    /// <summary>
    /// Starts a read-only history replay for a finished puzzle.
    /// </summary>
    public void StartHistoryReplay(HistoryEntry entry)
    {
        if (!entry.HasReplayData)
        {
            GD.PrintErr("[AppState] History entry has no replay data");
            return;
        }

        // Explicitly set return to history menu (CaptureReturnScene may get wrong scene)
        _returnScenePath = SCENE_HISTORY;
        GD.Print($"[AppState] Set return scene to history: {_returnScenePath}");

        CurrentGame = entry.ToPuzzleState();
        CurrentGame.Status = GameStatus.InProgress;
        CurrentGame.IsTutorial = false;
        IsNewGame = false;
        IsHistoryReplay = true;

        NavigateTo(SCENE_GAME);
    }

    /// <summary>
    /// Registriert einen Fehler
    /// </summary>
    /// <returns>True wenn das Spiel vorbei ist (Deadly Mode)</returns>
    public bool RegisterMistake()
    {
        if (CurrentGame == null) return false;

        CurrentGame.Mistakes++;

        if (CurrentGame.IsDeadlyMode && CurrentGame.Mistakes >= 3)
        {
            return true; // Game Over
        }

        return false;
    }
}
