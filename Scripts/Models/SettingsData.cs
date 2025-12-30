namespace MySudoku.Models;

/// <summary>
/// Spieleinstellungen
/// </summary>
public class SettingsData
{
    // Storage Configuration (at top - most important)
    /// <summary>Custom storage path for save data. Empty = default (user://)</summary>
    public string CustomStoragePath { get; set; } = "";

    /// <summary>Aktuelles Theme (0 = Hell, 1 = Dunkel)</summary>
    public int ThemeIndex { get; set; } = 0;

    /// <summary>Deadly Modus aktiviert (Game Over nach 3 Fehlern)</summary>
    public bool DeadlyModeEnabled { get; set; } = false;

    /// <summary>True = Zahlen ausblenden bei 9x, False = ausgrauen</summary>
    public bool HideCompletedNumbers { get; set; } = false;

    /// <summary>Zeile/Spalte/Block bei Auswahl highlighten</summary>
    public bool HighlightRelatedCells { get; set; } = true;

    /// <summary>Sound-Effekte aktiviert</summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>SFX-Lautstärke (0-100)</summary>
    public int Volume { get; set; } = 80;

    /// <summary>Musik aktiviert</summary>
    public bool MusicEnabled { get; set; } = true;

    /// <summary>Musik-Lautstärke (0-100)</summary>
    public int MusicVolume { get; set; } = 50;

    /// <summary>Musik-Track für Menü (0 = Aus, 1-3 = verschiedene Tracks)</summary>
    public int MenuMusicTrack { get; set; } = 3;

    /// <summary>Musik-Track für Spiel (0 = Aus, 1-3 = verschiedene Tracks)</summary>
    public int GameMusicTrack { get; set; } = 1;

    /// <summary>Lernmodus: zeigt bei Fehlern eine kurze Erklärung</summary>
    public bool LearnModeEnabled { get; set; } = true;

    /// <summary>Farbblind-freundliche Palette (bessere Kontraste)</summary>
    public bool ColorblindPaletteEnabled { get; set; } = false;

    /// <summary>UI-Skalierung in Prozent (z.B. 100 = normal)</summary>
    public int UiScalePercent { get; set; } = 100;

    // Notes assistant
    /// <summary>Nach dem Setzen einer Zahl: entferne diese Zahl automatisch aus Notizen in Zeile/Spalte/Block</summary>
    public bool SmartNoteCleanupEnabled { get; set; } = true;

    /// <summary>Zeigt im Spiel einen Button zum Auto-Füllen von Notizen für die gewählte House-Auswahl (Zeile/Spalte/Block)</summary>
    public bool HouseAutoFillEnabled { get; set; } = true;

    // Daily Sudoku
    public List<string> DailyCompletedDates { get; set; } = new(); // yyyy-MM-dd
    public string? DailyLastCompletedDate { get; set; }
    public string? DailyLastPlayedDate { get; set; }
    public int DailyStreakCurrent { get; set; } = 0;
    public int DailyStreakBest { get; set; } = 0;

    // Challenge Modes (apply to new games)
    public bool ChallengeNoNotes { get; set; } = false;
    public bool ChallengePerfectRun { get; set; } = false;
    public int ChallengeHintLimit { get; set; } = 0; // 0 = off
    public int ChallengeTimeAttackMinutes { get; set; } = 0; // 0 = off

    // Technique progression (tracked via hints)
    public Dictionary<string, int> TechniqueHintShownCounts { get; set; } = new();
    public Dictionary<string, int> TechniqueHintAppliedCounts { get; set; } = new();

    // Configurable techniques per difficulty
    public List<string>? TechniquesKids { get; set; }
    public List<string>? TechniquesEasy { get; set; }
    public List<string>? TechniquesMedium { get; set; }
    public List<string>? TechniquesHard { get; set; }

    // Mistake heatmap (aggregated over games)
    public List<int> MistakeHeatmap9 { get; set; } = new(); // 81 entries
    public List<int> MistakeHeatmap4 { get; set; } = new(); // 16 entries

    public void EnsureHeatmapSizes()
    {
        EnsureListSize(MistakeHeatmap9, 81);
        EnsureListSize(MistakeHeatmap4, 16);
    }

    public void RecordMistake(int gridSize, int row, int col)
    {
        EnsureHeatmapSizes();
        if (gridSize == 4)
        {
            int idx = row * 4 + col;
            if (idx >= 0 && idx < MistakeHeatmap4.Count) MistakeHeatmap4[idx]++;
            return;
        }

        int idx9 = row * 9 + col;
        if (idx9 >= 0 && idx9 < MistakeHeatmap9.Count) MistakeHeatmap9[idx9]++;
    }

    public bool HasCompletedDaily(string date)
    {
        return DailyCompletedDates.Contains(date);
    }

    public void MarkDailyCompleted(string date)
    {
        if (string.IsNullOrWhiteSpace(date)) return;
        if (HasCompletedDaily(date))
        {
            DailyLastPlayedDate = date;
            return;
        }

        DailyCompletedDates.Add(date);
        DailyLastPlayedDate = date;

        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var completedDate))
        {
            // Fallback: still record completion without streak logic
            DailyLastCompletedDate = date;
            DailyStreakCurrent = Math.Max(1, DailyStreakCurrent);
            if (DailyStreakCurrent > DailyStreakBest) DailyStreakBest = DailyStreakCurrent;
            return;
        }

        string yesterday = completedDate.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(DailyLastCompletedDate) && DailyLastCompletedDate == yesterday)
        {
            DailyStreakCurrent++;
        }
        else
        {
            DailyStreakCurrent = 1;
        }

        DailyLastCompletedDate = date;
        if (DailyStreakCurrent > DailyStreakBest) DailyStreakBest = DailyStreakCurrent;

        // Keep list bounded (last ~400 days)
        if (DailyCompletedDates.Count > 400)
        {
            DailyCompletedDates = DailyCompletedDates
                .OrderByDescending(d => d)
                .Take(400)
                .ToList();
        }
    }

    public void IncrementTechniqueShown(string technique)
    {
        if (string.IsNullOrWhiteSpace(technique)) return;
        TechniqueHintShownCounts.TryGetValue(technique, out int cur);
        TechniqueHintShownCounts[technique] = cur + 1;
    }

    public void IncrementTechniqueApplied(string technique)
    {
        if (string.IsNullOrWhiteSpace(technique)) return;
        TechniqueHintAppliedCounts.TryGetValue(technique, out int cur);
        TechniqueHintAppliedCounts[technique] = cur + 1;
    }

    /// <summary>
    /// Gibt die konfigurierten Techniken für eine Schwierigkeit zurück (oder null für Standard)
    /// </summary>
    public HashSet<string>? GetTechniquesForDifficulty(Difficulty difficulty)
    {
        var list = difficulty switch
        {
            Difficulty.Kids => TechniquesKids,
            Difficulty.Easy => TechniquesEasy,
            Difficulty.Medium => TechniquesMedium,
            Difficulty.Hard => TechniquesHard,
            _ => null
        };
        return list != null && list.Count > 0 ? new HashSet<string>(list) : null;
    }

    /// <summary>
    /// Setzt die Techniken für eine Schwierigkeit
    /// </summary>
    public void SetTechniquesForDifficulty(Difficulty difficulty, HashSet<string> techniques)
    {
        var list = techniques.ToList();
        switch (difficulty)
        {
            case Difficulty.Kids:
                TechniquesKids = list;
                break;
            case Difficulty.Easy:
                TechniquesEasy = list;
                break;
            case Difficulty.Medium:
                TechniquesMedium = list;
                break;
            case Difficulty.Hard:
                TechniquesHard = list;
                break;
        }
    }

    /// <summary>
    /// Setzt alle Technik-Konfigurationen auf Standard zurück
    /// </summary>
    public void ResetTechniquesToDefault()
    {
        TechniquesKids = null;
        TechniquesEasy = null;
        TechniquesMedium = null;
        TechniquesHard = null;
    }

    private static void EnsureListSize(List<int> list, int size)
    {
        if (list.Count == size) return;
        if (list.Count > size)
        {
            list.RemoveRange(size, list.Count - size);
            return;
        }
        while (list.Count < size) list.Add(0);
    }

    public SettingsData Clone()
    {
        return new SettingsData
        {
            CustomStoragePath = CustomStoragePath,
            ThemeIndex = ThemeIndex,
            DeadlyModeEnabled = DeadlyModeEnabled,
            HideCompletedNumbers = HideCompletedNumbers,
            HighlightRelatedCells = HighlightRelatedCells,
            SoundEnabled = SoundEnabled,
            Volume = Volume,
            MusicEnabled = MusicEnabled,
            MusicVolume = MusicVolume,
            MenuMusicTrack = MenuMusicTrack,
            GameMusicTrack = GameMusicTrack,
            LearnModeEnabled = LearnModeEnabled,
            ColorblindPaletteEnabled = ColorblindPaletteEnabled,
            UiScalePercent = UiScalePercent,
            SmartNoteCleanupEnabled = SmartNoteCleanupEnabled,
            HouseAutoFillEnabled = HouseAutoFillEnabled,
            DailyCompletedDates = new List<string>(DailyCompletedDates),
            DailyLastCompletedDate = DailyLastCompletedDate,
            DailyLastPlayedDate = DailyLastPlayedDate,
            DailyStreakCurrent = DailyStreakCurrent,
            DailyStreakBest = DailyStreakBest,
            ChallengeNoNotes = ChallengeNoNotes,
            ChallengePerfectRun = ChallengePerfectRun,
            ChallengeHintLimit = ChallengeHintLimit,
            ChallengeTimeAttackMinutes = ChallengeTimeAttackMinutes,
            TechniqueHintShownCounts = new Dictionary<string, int>(TechniqueHintShownCounts),
            TechniqueHintAppliedCounts = new Dictionary<string, int>(TechniqueHintAppliedCounts),
            TechniquesKids = TechniquesKids != null ? new List<string>(TechniquesKids) : null,
            TechniquesEasy = TechniquesEasy != null ? new List<string>(TechniquesEasy) : null,
            TechniquesMedium = TechniquesMedium != null ? new List<string>(TechniquesMedium) : null,
            TechniquesHard = TechniquesHard != null ? new List<string>(TechniquesHard) : null,
            MistakeHeatmap9 = new List<int>(MistakeHeatmap9),
            MistakeHeatmap4 = new List<int>(MistakeHeatmap4)
        };
    }
}
