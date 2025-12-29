using System.Text.Json;

namespace MySudoku.Services;

/// <summary>
/// Autoload: Speicherdienst für Settings, SaveGame und History
/// </summary>
public partial class SaveService : Node
{
    private const string SETTINGS_PATH = "user://settings.json";
    private const string SAVEGAME_PATH = "user://savegame.json";
    private const string HISTORY_PATH = "user://history.json";

    public SettingsData Settings { get; private set; } = new();
    public SudokuGameState? CurrentGame { get; private set; }
    public List<HistoryEntry> History { get; private set; } = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public override void _Ready()
    {
        LoadAll();
    }

    public void LoadAll()
    {
        LoadSettings();
        LoadSaveGame();
        LoadHistory();
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
            }
            catch (Exception e)
            {
                GD.PrintErr($"Fehler beim Laden der Einstellungen: {e.Message}");
                Settings = new();
            }
        }
    }

    public void SaveSettings()
    {
        try
        {
            using var file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Write);
            string json = JsonSerializer.Serialize(Settings, _jsonOptions);
            file.StoreString(json);
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
        if (FileAccess.FileExists(SAVEGAME_PATH))
        {
            try
            {
                using var file = FileAccess.Open(SAVEGAME_PATH, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                var saveData = JsonSerializer.Deserialize<SaveGameData>(json);
                if (saveData != null)
                {
                    CurrentGame = saveData.ToGameState();
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"Fehler beim Laden des Spielstands: {e.Message}");
                CurrentGame = null;
            }
        }
    }

    public void SaveCurrentGame(SudokuGameState gameState)
    {
        CurrentGame = gameState;
        try
        {
            using var file = FileAccess.Open(SAVEGAME_PATH, FileAccess.ModeFlags.Write);
            var saveData = SaveGameData.FromGameState(gameState);
            string json = JsonSerializer.Serialize(saveData, _jsonOptions);
            file.StoreString(json);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Fehler beim Speichern des Spielstands: {e.Message}");
        }
    }

    public void DeleteSaveGame()
    {
        CurrentGame = null;
        if (FileAccess.FileExists(SAVEGAME_PATH))
        {
            DirAccess.RemoveAbsolute(SAVEGAME_PATH);
        }
    }

    #endregion

    #region History

    public void LoadHistory()
    {
        if (FileAccess.FileExists(HISTORY_PATH))
        {
            try
            {
                using var file = FileAccess.Open(HISTORY_PATH, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                History = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Fehler beim Laden des Verlaufs: {e.Message}");
                History = new();
            }
        }
    }

    public void AddHistoryEntry(HistoryEntry entry)
    {
        History.Insert(0, entry); // Neueste zuerst
        SaveHistory();
    }

    public void SaveHistory()
    {
        try
        {
            using var file = FileAccess.Open(HISTORY_PATH, FileAccess.ModeFlags.Write);
            string json = JsonSerializer.Serialize(History, _jsonOptions);
            file.StoreString(json);
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

    public class CellData
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Value { get; set; }
        public bool IsGiven { get; set; }
        public int Solution { get; set; }
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
            Status = (int)state.Status
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
                    Solution = cell.Solution
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
            Status = (GameStatus)Status
        };

        foreach (var cellData in Cells)
        {
            state.Grid[cellData.Row, cellData.Col] = new SudokuCell
            {
                Value = cellData.Value,
                IsGiven = cellData.IsGiven,
                Solution = cellData.Solution
            };
        }

        return state;
    }
}
