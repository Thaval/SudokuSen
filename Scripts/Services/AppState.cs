using Godot;
using System;
using MySudoku.Models;

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

        // Lösche SaveGame wenn beendet (nicht InProgress)
        if (status != GameStatus.InProgress)
        {
            saveService.DeleteSaveGame();
        }

        EmitSignal(SignalName.GameEnded, (int)status);
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
