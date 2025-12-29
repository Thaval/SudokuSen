namespace MySudoku.Services;

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
        CurrentGame.ScenarioTechnique = techniqueId;
        ApplyChallengeSettings(CurrentGame, saveService.Settings);
        IsNewGame = true;

        saveService.SaveCurrentGame(CurrentGame);

        EmitSignal(SignalName.GameStarted);
        NavigateTo(SCENE_GAME);
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
