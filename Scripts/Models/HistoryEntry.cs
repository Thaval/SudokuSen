using System;

namespace MySudoku.Models;

/// <summary>
/// Ein Eintrag im Spielverlauf
/// </summary>
public class HistoryEntry
{
    /// <summary>Eindeutige ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Datum und Uhrzeit des Spielstarts</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Datum und Uhrzeit des Spielendes</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Schwierigkeitsgrad</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Spieldauer in Sekunden</summary>
    public double DurationSeconds { get; set; }

    /// <summary>Anzahl der Fehler</summary>
    public int Mistakes { get; set; }

    /// <summary>Spielstatus</summary>
    public GameStatus Status { get; set; }

    /// <summary>War Deadly Mode aktiv</summary>
    public bool WasDeadlyMode { get; set; }

    /// <summary>
    /// Erstellt einen HistoryEntry aus einem GameState
    /// </summary>
    public static HistoryEntry FromGameState(SudokuGameState state, GameStatus finalStatus)
    {
        return new HistoryEntry
        {
            StartTime = state.StartTime,
            EndTime = DateTime.Now,
            Difficulty = state.Difficulty,
            DurationSeconds = state.ElapsedSeconds,
            Mistakes = state.Mistakes,
            Status = finalStatus,
            WasDeadlyMode = state.IsDeadlyMode
        };
    }

    public string GetFormattedDuration()
    {
        var ts = TimeSpan.FromSeconds(DurationSeconds);
        if (ts.Hours > 0)
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    public string GetStatusText()
    {
        return Status switch
        {
            GameStatus.Won => "Gewonnen",
            GameStatus.Lost => "Verloren",
            GameStatus.Abandoned => "Abgebrochen",
            GameStatus.InProgress => "Läuft",
            _ => "Unbekannt"
        };
    }

    public string GetDifficultyText()
    {
        return Difficulty switch
        {
            Difficulty.Easy => "Leicht",
            Difficulty.Medium => "Mittel",
            Difficulty.Hard => "Schwer",
            _ => "Unbekannt"
        };
    }
}
