namespace SudokuSen.Services;

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

    // Flag für neues Spiel vs fortgesetztes Spiel
    public bool IsNewGame { get; private set; }

    // Scene Paths
    public const string SCENE_MAIN_MENU = "res://Scenes/MainMenu.tscn";
    public const string SCENE_DIFFICULTY = "res://Scenes/DifficultyMenu.tscn";
    public const string SCENE_GAME = "res://Scenes/GameScene.tscn";
    public const string SCENE_SETTINGS = "res://Scenes/SettingsMenu.tscn";
    public const string SCENE_HISTORY = "res://Scenes/HistoryMenu.tscn";
    public const string SCENE_STATS = "res://Scenes/StatsMenu.tscn";
    public const string SCENE_TIPS = "res://Scenes/TipsMenu.tscn";
    public const string SCENE_SCENARIOS = "res://Scenes/ScenariosMenu.tscn";

    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// Navigiert zu einer Szene
    /// </summary>
    public void NavigateTo(string scenePath)
    {
        EmitSignal(SignalName.SceneChangeRequested, scenePath);
    }

    /// <summary>
    /// Zurück zum Hauptmenü
    /// </summary>
    public void GoToMainMenu()
    {
        NavigateTo(SCENE_MAIN_MENU);
    }

    /// <summary>
    /// Startet ein neues Spiel
    /// </summary>
    public void StartNewGame(Difficulty difficulty)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");

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
        var saveService = GetNode<SaveService>("/root/SaveService");

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
    /// Startet ein Szenario-Spiel für eine bestimmte Technik
    /// </summary>
    public void StartScenarioGame(string techniqueId)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");

        // Bestimme passende Schwierigkeit basierend auf Technik (1=Easy, 2=Medium, 3=Hard)
        Difficulty difficulty = Difficulty.Medium;
        if (TechniqueInfo.Techniques.TryGetValue(techniqueId, out var technique))
        {
            difficulty = technique.DefaultDifficulty switch
            {
                1 => Difficulty.Easy,
                2 => Difficulty.Medium,
                3 => Difficulty.Hard,
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
        var saveService = GetNode<SaveService>("/root/SaveService");
        var tutorialService = GetNodeOrNull<TutorialService>("/root/TutorialService");

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
        if (CurrentGame != null)
        {
            CurrentGame.ElapsedSeconds = elapsed;
        }
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
