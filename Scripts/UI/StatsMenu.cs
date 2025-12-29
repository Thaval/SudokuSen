namespace MySudoku.UI;

/// <summary>
/// Statistik-Menü
/// </summary>
public partial class StatsMenu : Control
{
    private Button _backButton = null!;
    private Label _title = null!;
    private PanelContainer _panel = null!;

    // Overview
    private Label _totalGames = null!;
    private Label _winsLosses = null!;
    private Label _winRateLabel = null!;
    private ProgressBar _winRateBar = null!;

    // Times
    private Label _bestTime = null!;
    private Label _worstTime = null!;
    private Label _avgTimeKids = null!;
    private Label _avgTimeEasy = null!;
    private Label _avgTimeMedium = null!;
    private Label _avgTimeHard = null!;

    // Mistakes
    private Label _avgMistakesKids = null!;
    private Label _avgMistakesEasy = null!;
    private Label _avgMistakesMedium = null!;
    private Label _avgMistakesHard = null!;

    // Daily
    private Label _dailyStreak = null!;
    private Label _dailyToday = null!;
    private RichTextLabel _dailyRecent = null!;

    // Techniques
    private RichTextLabel _techniquesSummary = null!;

    // Heatmap
    private CenterContainer _heatmapContainer = null!;

    public override void _Ready()
    {
        _backButton = GetNode<Button>("BackButton");
        _title = GetNode<Label>("Title");
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");

        var statsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer");

        var overviewSection = statsContainer.GetNode<VBoxContainer>("OverviewSection");
        _totalGames = overviewSection.GetNode<Label>("TotalGames");
        _winsLosses = overviewSection.GetNode<Label>("WinsLosses");
        _winRateLabel = overviewSection.GetNode<Label>("WinRateContainer/WinRateLabel");
        _winRateBar = overviewSection.GetNode<ProgressBar>("WinRateContainer/WinRateBar");

        var timeSection = statsContainer.GetNode<VBoxContainer>("TimeSection");
        _bestTime = timeSection.GetNode<Label>("BestTime");
        _worstTime = timeSection.GetNode<Label>("WorstTime");
        _avgTimeKids = timeSection.GetNode<Label>("AvgTimeKids");
        _avgTimeEasy = timeSection.GetNode<Label>("AvgTimeEasy");
        _avgTimeMedium = timeSection.GetNode<Label>("AvgTimeMedium");
        _avgTimeHard = timeSection.GetNode<Label>("AvgTimeHard");

        var mistakesSection = statsContainer.GetNode<VBoxContainer>("MistakesSection");
        _avgMistakesKids = mistakesSection.GetNode<Label>("AvgMistakesKids");
        _avgMistakesEasy = mistakesSection.GetNode<Label>("AvgMistakesEasy");
        _avgMistakesMedium = mistakesSection.GetNode<Label>("AvgMistakesMedium");
        _avgMistakesHard = mistakesSection.GetNode<Label>("AvgMistakesHard");

        var dailySection = statsContainer.GetNode<VBoxContainer>("DailySection");
        _dailyStreak = dailySection.GetNode<Label>("DailyStreak");
        _dailyToday = dailySection.GetNode<Label>("DailyToday");
        _dailyRecent = dailySection.GetNode<RichTextLabel>("DailyRecent");

        var techniquesSection = statsContainer.GetNode<VBoxContainer>("TechniquesSection");
        _techniquesSummary = techniquesSection.GetNode<RichTextLabel>("TechniquesSummary");

        var heatmapSection = statsContainer.GetNode<VBoxContainer>("HeatmapSection");
        _heatmapContainer = heatmapSection.GetNode<CenterContainer>("HeatmapContainer");

        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;

        CalculateStats();
    }

    public override void _ExitTree()
    {
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged -= OnThemeChanged;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            OnBackPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    private void CalculateStats()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        var history = saveService.History;
        var settings = saveService.Settings;

        // Nur abgeschlossene Spiele
        var completed = history.Where(h => h.Status != GameStatus.InProgress).ToList();
        var wins = completed.Where(h => h.Status == GameStatus.Won).ToList();
        var losses = completed.Where(h => h.Status == GameStatus.Lost).ToList();

        // Übersicht
        _totalGames.Text = $"Spiele gesamt: {completed.Count}";
        _winsLosses.Text = $"Gewonnen: {wins.Count} | Verloren: {losses.Count}";

        double winRate = completed.Count > 0 ? (double)wins.Count / completed.Count * 100 : 0;
        _winRateLabel.Text = $"Gewinnrate: {winRate:F1}%";
        _winRateBar.Value = winRate;

        // Zeiten (nur gewonnene Spiele)
        if (wins.Count > 0)
        {
            var bestTime = wins.Min(w => w.DurationSeconds);
            var worstTime = wins.Max(w => w.DurationSeconds);
            _bestTime.Text = $"Beste Zeit: {FormatTime(bestTime)}";
            _worstTime.Text = $"Längste Zeit: {FormatTime(worstTime)}";
        }
        else
        {
            _bestTime.Text = "Beste Zeit: --:--";
            _worstTime.Text = "Längste Zeit: --:--";
        }

        // Durchschnittliche Zeit pro Schwierigkeit
        _avgTimeKids.Text = $"Ø Kids: {GetAvgTime(wins, Difficulty.Kids)}";
        _avgTimeEasy.Text = $"Ø Leicht: {GetAvgTime(wins, Difficulty.Easy)}";
        _avgTimeMedium.Text = $"Ø Mittel: {GetAvgTime(wins, Difficulty.Medium)}";
        _avgTimeHard.Text = $"Ø Schwer: {GetAvgTime(wins, Difficulty.Hard)}";

        // Durchschnittliche Fehler pro Schwierigkeit
        _avgMistakesKids.Text = $"Ø Kids: {GetAvgMistakes(completed, Difficulty.Kids)}";
        _avgMistakesEasy.Text = $"Ø Leicht: {GetAvgMistakes(completed, Difficulty.Easy)}";
        _avgMistakesMedium.Text = $"Ø Mittel: {GetAvgMistakes(completed, Difficulty.Medium)}";
        _avgMistakesHard.Text = $"Ø Schwer: {GetAvgMistakes(completed, Difficulty.Hard)}";

        // Daily
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        bool doneToday = settings.HasCompletedDaily(today);
        _dailyStreak.Text = $"Streak: {settings.DailyStreakCurrent}  (Best: {settings.DailyStreakBest})";
        _dailyToday.Text = doneToday ? "Heute: erledigt ✅" : "Heute: offen";
        _dailyRecent.Text = BuildDailyRecentText(settings, days: 14);

        // Techniques
        _techniquesSummary.Text = BuildTechniqueSummary(settings);

        // Heatmap
        RenderHeatmap(settings);
    }

    private static string BuildDailyRecentText(SettingsData settings, int days)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[b]Letzte Tage:[/b]\n");
        for (int i = 0; i < days; i++)
        {
            var date = DateTime.Today.AddDays(-i);
            string s = date.ToString("yyyy-MM-dd");
            bool done = settings.HasCompletedDaily(s);
            sb.Append(done ? "✅ " : "⬜ ");
            sb.Append(date.ToString("dd.MM"));
            if (i % 7 == 6) sb.Append("\n");
            else sb.Append("   ");
        }
        return sb.ToString();
    }

    private static string BuildTechniqueSummary(SettingsData settings)
    {
        var shown = settings.TechniqueHintShownCounts;
        var applied = settings.TechniqueHintAppliedCounts;

        if (shown.Count == 0)
        {
            return "Noch keine Technik-Daten (nutze 💡 Hinweise im Spiel).";
        }

        var top = shown
            .OrderByDescending(kv => kv.Value)
            .Take(6)
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.Append("[b]Meist gesehene Hinweise:[/b]\n");
        foreach (var kv in top)
        {
            applied.TryGetValue(kv.Key, out int ok);
            sb.Append($"• {kv.Key}: {kv.Value}x (angewendet: {ok}x)\n");
        }
        return sb.ToString();
    }

    private void RenderHeatmap(SettingsData settings)
    {
        // Clear old
        foreach (var child in _heatmapContainer.GetChildren())
        {
            child.QueueFree();
        }

        settings.EnsureHeatmapSizes();
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        int gridSize = 9;
        var heat = settings.MistakeHeatmap9;
        int max = heat.Count > 0 ? heat.Max() : 0;

        var grid = new GridContainer();
        grid.Columns = gridSize;
        _heatmapContainer.AddChild(grid);

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                int idx = row * 9 + col;
                int v = idx < heat.Count ? heat[idx] : 0;

                var cell = new PanelContainer();
                cell.CustomMinimumSize = new Vector2(32, 32);

                var sb = theme.CreatePanelStyleBox(4, 0);
                float t = max > 0 ? (float)v / max : 0f;
                // Blend between normal cell bg and accent
                sb.BgColor = colors.CellBackground.Lerp(colors.Accent, 0.55f * t);
                cell.AddThemeStyleboxOverride("panel", sb);

                var label = new Label();
                label.Text = v == 0 ? "" : v.ToString();
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                label.AddThemeColorOverride("font_color", colors.TextPrimary);
                cell.AddChild(label);

                grid.AddChild(cell);
            }
        }
    }

    private string GetAvgTime(System.Collections.Generic.List<HistoryEntry> entries, Difficulty difficulty)
    {
        var filtered = entries.Where(e => e.Difficulty == difficulty).ToList();
        if (filtered.Count == 0) return "--:--";

        double avg = filtered.Average(e => e.DurationSeconds);
        return FormatTime(avg);
    }

    private string GetAvgMistakes(System.Collections.Generic.List<HistoryEntry> entries, Difficulty difficulty)
    {
        var filtered = entries.Where(e => e.Difficulty == difficulty).ToList();
        if (filtered.Count == 0) return "-";

        double avg = filtered.Average(e => e.Mistakes);
        return $"{avg:F1}";
    }

    private string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.Hours > 0)
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private void OnBackPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);

        var panelStyle = theme.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _backButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);

        // Alle Labels im Panel
        ApplyLabelTheme(colors);

        _dailyRecent.AddThemeColorOverride("default_color", colors.TextPrimary);
        _techniquesSummary.AddThemeColorOverride("default_color", colors.TextPrimary);
    }

    private void ApplyLabelTheme(ThemeService.ThemeColors colors)
    {
        var statsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer");

        foreach (var section in statsContainer.GetChildren())
        {
            if (section is VBoxContainer vbox)
            {
                foreach (var child in vbox.GetChildren())
                {
                    if (child is Label label)
                    {
                        // Titel sind größer
                        if (label.Name.ToString().Contains("Title"))
                        {
                            label.AddThemeColorOverride("font_color", colors.Accent);
                        }
                        else
                        {
                            label.AddThemeColorOverride("font_color", colors.TextPrimary);
                        }
                    }
                    else if (child is HBoxContainer hbox)
                    {
                        foreach (var hchild in hbox.GetChildren())
                        {
                            if (hchild is Label hlabel)
                            {
                                hlabel.AddThemeColorOverride("font_color", colors.TextPrimary);
                            }
                        }
                    }
                }
            }
        }
    }
}
