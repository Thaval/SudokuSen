using System.Text.Json;

namespace MySudoku.Services;

/// <summary>
/// Autoload: Speicherdienst für Settings, SaveGame und History
/// </summary>
public partial class SaveService : Node
{
    // Settings are always stored in user:// (to bootstrap custom path)
    private const string SETTINGS_PATH = "user://settings.json";

    // These paths can be overridden via CustomStoragePath
    private const string DEFAULT_SAVEGAME_FILE = "savegame.json";
    private const string DEFAULT_HISTORY_FILE = "history.json";

    public SettingsData Settings { get; private set; } = new();
    public SudokuGameState? CurrentGame { get; private set; }
    public List<HistoryEntry> History { get; private set; } = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Gets the effective storage path for save data (user:// or custom path)
    /// </summary>
    public string GetStorageBasePath()
    {
        if (string.IsNullOrWhiteSpace(Settings.CustomStoragePath))
            return "user://";

        // Ensure path ends with separator
        string path = Settings.CustomStoragePath.Replace('\\', '/');
        if (!path.EndsWith('/'))
            path += '/';
        return path;
    }

    /// <summary>
    /// Gets the full path for savegame file
    /// </summary>
    public string GetSavegamePath() => GetStorageBasePath() + DEFAULT_SAVEGAME_FILE;

    /// <summary>
    /// Gets the full path for history file
    /// </summary>
    public string GetHistoryPath() => GetStorageBasePath() + DEFAULT_HISTORY_FILE;

    /// <summary>
    /// Gets the resolved absolute path of the current storage directory
    /// </summary>
    public string GetResolvedStoragePath()
    {
        string basePath = GetStorageBasePath();
        if (basePath.StartsWith("user://"))
        {
            return ProjectSettings.GlobalizePath(basePath);
        }
        return basePath.TrimEnd('/');
    }

    public override void _Ready()
    {
        GD.Print("[Save] SaveService ready");
        LoadAll();
    }

    public void LoadAll()
    {
        GD.Print("[Save] LoadAll() start");
        LoadSettings();
        EnsureStorageDirectory();
        LoadSaveGame();
        LoadHistory();
        GD.Print($"[Save] LoadAll() done | storage={GetResolvedStoragePath()}");
    }

    /// <summary>
    /// Ensures the custom storage directory exists
    /// </summary>
    private void EnsureStorageDirectory()
    {
        if (string.IsNullOrWhiteSpace(Settings.CustomStoragePath))
            return;

        string path = Settings.CustomStoragePath.Replace('\\', '/');
        if (!DirAccess.DirExistsAbsolute(path))
        {
            var err = DirAccess.MakeDirRecursiveAbsolute(path);
            if (err != Error.Ok)
            {
                GD.PrintErr($"Konnte Speicherpfad nicht erstellen: {path} - {err}");
            }
        }
    }

    #region Settings

    public void LoadSettings()
    {
        if (FileAccess.FileExists(SETTINGS_PATH))
        {
            try
            {
                using var file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                Settings = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
                Settings.EnsureHeatmapSizes();

                GD.Print($"[Save] Settings loaded from {ProjectSettings.GlobalizePath(SETTINGS_PATH)} | theme={Settings.ThemeIndex}, colorblind={Settings.ColorblindPaletteEnabled}, sfx={Settings.SoundEnabled}({Settings.Volume}%), music={Settings.MusicEnabled}({Settings.MusicVolume}%)");
            }
            catch (Exception e)
            {
                GD.PrintErr($"Fehler beim Laden der Einstellungen: {e.Message}");
                Settings = new();
            }
        }
        else
        {
            GD.Print($"[Save] Settings file not found at {ProjectSettings.GlobalizePath(SETTINGS_PATH)} (using defaults)");
        }
    }

    public void SaveSettings()
    {
        try
        {
            using var file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Write);
            string json = JsonSerializer.Serialize(Settings, _jsonOptions);
            file.StoreString(json);

            GD.Print($"[Save] Settings saved to {ProjectSettings.GlobalizePath(SETTINGS_PATH)} | theme={Settings.ThemeIndex}, colorblind={Settings.ColorblindPaletteEnabled}, sfx={Settings.SoundEnabled}({Settings.Volume}%), music={Settings.MusicEnabled}({Settings.MusicVolume}%)");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Fehler beim Speichern der Einstellungen: {e.Message}");
        }
    }

    #endregion

    #region SaveGame

    public bool HasSaveGame => CurrentGame != null && CurrentGame.Status == GameStatus.InProgress;

    public void LoadSaveGame()
    {
        string savePath = GetSavegamePath();
        if (FileAccess.FileExists(savePath))
        {
            try
            {
                using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                var saveData = JsonSerializer.Deserialize<SaveGameData>(json);
                if (saveData != null)
                {
                    CurrentGame = saveData.ToGameState();
                    GD.Print($"[Save] SaveGame loaded from {ProjectSettings.GlobalizePath(savePath)}");
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"Fehler beim Laden des Spielstands: {e.Message}");
                CurrentGame = null;
            }
        }
        else
        {
            GD.Print($"[Save] No SaveGame at {ProjectSettings.GlobalizePath(savePath)}");
        }
    }

    public void SaveCurrentGame(SudokuGameState gameState)
    {
        CurrentGame = gameState;
        EnsureStorageDirectory();
        try
        {
            string savePath = GetSavegamePath();
            using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
            var saveData = SaveGameData.FromGameState(gameState);
            string json = JsonSerializer.Serialize(saveData, _jsonOptions);
            file.StoreString(json);
            GD.Print($"[Save] SaveGame saved to {ProjectSettings.GlobalizePath(savePath)} | status={gameState.Status}, elapsed={gameState.ElapsedSeconds:0.0}s");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Fehler beim Speichern des Spielstands: {e.Message}");
        }
    }

    public void DeleteSaveGame()
    {
        CurrentGame = null;
        string savePath = GetSavegamePath();
        if (FileAccess.FileExists(savePath))
        {
            DirAccess.RemoveAbsolute(savePath);
            GD.Print($"[Save] SaveGame deleted at {ProjectSettings.GlobalizePath(savePath)}");
        }
    }

    #endregion

    #region History

    public void LoadHistory()
    {
        string historyPath = GetHistoryPath();
        if (FileAccess.FileExists(historyPath))
        {
            try
            {
                using var file = FileAccess.Open(historyPath, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                History = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new();
                GD.Print($"[Save] History loaded from {ProjectSettings.GlobalizePath(historyPath)} | count={History.Count}");
            }
            catch (Exception e)
            {
                GD.PrintErr($"Fehler beim Laden des Verlaufs: {e.Message}");
                History = new();
            }
        }
        else
        {
            GD.Print($"[Save] No History at {ProjectSettings.GlobalizePath(historyPath)}");
        }
    }

    public void AddHistoryEntry(HistoryEntry entry)
    {
        History.Insert(0, entry); // Neueste zuerst
        SaveHistory();
    }

    public void SaveHistory()
    {
        EnsureStorageDirectory();
        try
        {
            string historyPath = GetHistoryPath();
            using var file = FileAccess.Open(historyPath, FileAccess.ModeFlags.Write);
            string json = JsonSerializer.Serialize(History, _jsonOptions);
            file.StoreString(json);
            GD.Print($"[Save] History saved to {ProjectSettings.GlobalizePath(historyPath)} | count={History.Count}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Fehler beim Speichern des Verlaufs: {e.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Hilfsklasse für die Serialisierung des Spielstands
/// </summary>
public class SaveGameData
{
    public List<CellData> Cells { get; set; } = new();
    public int Difficulty { get; set; }
    public DateTime StartTime { get; set; }
    public double ElapsedSeconds { get; set; }
    public int Mistakes { get; set; }
    public bool IsDeadlyMode { get; set; }
    public int Status { get; set; }

    public bool IsDaily { get; set; }
    public string? DailyDate { get; set; }

    public bool ChallengeNoNotes { get; set; }
    public bool ChallengePerfectRun { get; set; }
    public int ChallengeHintLimit { get; set; }
    public int ChallengeTimeAttackSeconds { get; set; }
    public int HintsUsed { get; set; }

    public class CellData
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Value { get; set; }
        public bool IsGiven { get; set; }
        public int Solution { get; set; }
        public bool[] Notes { get; set; } = new bool[9];
    }

    public static SaveGameData FromGameState(SudokuGameState state)
    {
        var data = new SaveGameData
        {
            Difficulty = (int)state.Difficulty,
            StartTime = state.StartTime,
            ElapsedSeconds = state.ElapsedSeconds,
            Mistakes = state.Mistakes,
            IsDeadlyMode = state.IsDeadlyMode,
            Status = (int)state.Status,
            IsDaily = state.IsDaily,
            DailyDate = state.DailyDate,
            ChallengeNoNotes = state.ChallengeNoNotes,
            ChallengePerfectRun = state.ChallengePerfectRun,
            ChallengeHintLimit = state.ChallengeHintLimit,
            ChallengeTimeAttackSeconds = state.ChallengeTimeAttackSeconds,
            HintsUsed = state.HintsUsed
        };

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var cell = state.Grid[row, col];
                data.Cells.Add(new CellData
                {
                    Row = row,
                    Col = col,
                    Value = cell.Value,
                    IsGiven = cell.IsGiven,
                    Solution = cell.Solution,
                    Notes = (bool[])cell.Notes.Clone()
                });
            }
        }

        return data;
    }

    public SudokuGameState ToGameState()
    {
        var state = new SudokuGameState
        {
            Difficulty = (Difficulty)Difficulty,
            StartTime = StartTime,
            ElapsedSeconds = ElapsedSeconds,
            Mistakes = Mistakes,
            IsDeadlyMode = IsDeadlyMode,
            Status = (GameStatus)Status,
            IsDaily = IsDaily,
            DailyDate = DailyDate,
            ChallengeNoNotes = ChallengeNoNotes,
            ChallengePerfectRun = ChallengePerfectRun,
            ChallengeHintLimit = ChallengeHintLimit,
            ChallengeTimeAttackSeconds = ChallengeTimeAttackSeconds,
            HintsUsed = HintsUsed
        };

        foreach (var cellData in Cells)
        {
            var cell = new SudokuCell
            {
                Value = cellData.Value,
                IsGiven = cellData.IsGiven,
                Solution = cellData.Solution
            };
            // Restore notes (handle old saves without Notes)
            if (cellData.Notes != null)
            {
                for (int i = 0; i < Math.Min(cellData.Notes.Length, cell.Notes.Length); i++)
                {
                    cell.Notes[i] = cellData.Notes[i];
                }
            }
            state.Grid[cellData.Row, cellData.Col] = cell;
        }

        return state;
    }
}
