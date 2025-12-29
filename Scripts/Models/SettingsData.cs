namespace MySudoku.Models;

/// <summary>
/// Spieleinstellungen
/// </summary>
public class SettingsData
{
    /// <summary>Aktuelles Theme (0 = Hell, 1 = Dunkel)</summary>
    public int ThemeIndex { get; set; } = 0;

    /// <summary>Deadly Modus aktiviert (Game Over nach 3 Fehlern)</summary>
    public bool DeadlyModeEnabled { get; set; } = false;

    /// <summary>True = Zahlen ausblenden bei 9x, False = ausgrauen</summary>
    public bool HideCompletedNumbers { get; set; } = false;

    /// <summary>Zeile/Spalte/Block bei Auswahl highlighten</summary>
    public bool HighlightRelatedCells { get; set; } = true;

    /// <summary>Sound aktiviert</summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>Lautstärke (0-100)</summary>
    public int Volume { get; set; } = 80;

    public SettingsData Clone()
    {
        return new SettingsData
        {
            ThemeIndex = ThemeIndex,
            DeadlyModeEnabled = DeadlyModeEnabled,
            HideCompletedNumbers = HideCompletedNumbers,
            HighlightRelatedCells = HighlightRelatedCells,
            SoundEnabled = SoundEnabled,
            Volume = Volume
        };
    }
}
