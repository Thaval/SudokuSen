namespace SudokuSen.UI;

/// <summary>
/// Statistik-Menü
/// </summary>
public partial class StatsMenu : Control
{
    // Cached Service References
    private ThemeService _themeService = null!;
    private AudioService _audioService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;
    private LocalizationService _localizationService = null!;

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
    private Label _avgTimeInsane = null!;

    // Mistakes
    private Label _avgMistakesKids = null!;
    private Label _avgMistakesEasy = null!;
    private Label _avgMistakesMedium = null!;
    private Label _avgMistakesHard = null!;
    private Label _avgMistakesInsane = null!;

    // Daily
    private Label _dailyStreak = null!;
    private Label _dailyToday = null!;
    private RichTextLabel _dailyRecent = null!;
    private CenterContainer _dailyCalendarContainer = null!;

    // Techniques
    private RichTextLabel _techniquesSummary = null!;

    // Scenarios
    private RichTextLabel _scenarioSummary = null!;

    // Heatmap
    private CenterContainer _heatmapContainer = null!;

    public override void _Ready()
    {
        // Cache service references
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");

        UiNavigationSfx.Wire(this, _audioService);

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
        _avgTimeInsane = timeSection.GetNode<Label>("AvgTimeInsane");

        var mistakesSection = statsContainer.GetNode<VBoxContainer>("MistakesSection");
        _avgMistakesKids = mistakesSection.GetNode<Label>("AvgMistakesKids");
        _avgMistakesEasy = mistakesSection.GetNode<Label>("AvgMistakesEasy");
        _avgMistakesMedium = mistakesSection.GetNode<Label>("AvgMistakesMedium");
        _avgMistakesHard = mistakesSection.GetNode<Label>("AvgMistakesHard");
        _avgMistakesInsane = mistakesSection.GetNode<Label>("AvgMistakesInsane");

        var dailySection = statsContainer.GetNode<VBoxContainer>("DailySection");
        _dailyStreak = dailySection.GetNode<Label>("DailyStreak");
        _dailyToday = dailySection.GetNode<Label>("DailyToday");
        _dailyRecent = dailySection.GetNode<RichTextLabel>("DailyRecent");
        _dailyCalendarContainer = dailySection.GetNode<CenterContainer>("DailyCalendarContainer");

        var techniquesSection = statsContainer.GetNode<VBoxContainer>("TechniquesSection");
        _techniquesSummary = techniquesSection.GetNode<RichTextLabel>("TechniquesSummary");

        var scenarioSection = statsContainer.GetNode<VBoxContainer>("ScenarioSection");
        _scenarioSummary = scenarioSection.GetNode<RichTextLabel>("ScenarioSummary");

        var heatmapSection = statsContainer.GetNode<VBoxContainer>("HeatmapSection");
        _heatmapContainer = heatmapSection.GetNode<CenterContainer>("HeatmapContainer");

        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;

        _localizationService.LanguageChanged += OnLanguageChanged;

        ApplyLocalization();
        CalculateStats();
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _localizationService.LanguageChanged -= OnLanguageChanged;
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
        var history = _saveService.History;
        var settings = _saveService.Settings;
        var loc = _localizationService;

        // Nur abgeschlossene Spiele, OHNE Tutorials und Szenarien für Hauptstatistiken
        var completed = history.Where(h => h.Status != GameStatus.InProgress && !h.IsTutorial && !h.IsScenario).ToList();
        var wins = completed.Where(h => h.Status == GameStatus.Won).ToList();
        var losses = completed.Where(h => h.Status == GameStatus.Lost).ToList();

        // Szenarien separat für Szenario-Statistiken
        var scenarioCompleted = history.Where(h => h.Status != GameStatus.InProgress && h.IsScenario).ToList();
        var scenarioWins = scenarioCompleted.Where(h => h.Status == GameStatus.Won).ToList();

        // Übersicht mit Icons
        _totalGames.Text = loc.Get("stats.total_games_line", completed.Count);
        _winsLosses.Text = loc.Get("stats.wins_losses_line", wins.Count, losses.Count);

        double winRate = completed.Count > 0 ? (double)wins.Count / completed.Count * 100 : 0;
        _winRateLabel.Text = loc.Get("stats.win_rate_line", winRate);
        _winRateBar.Value = winRate;

        // Zeiten (nur gewonnene Spiele) mit Icons
        if (wins.Count > 0)
        {
            var bestTime = wins.Min(w => w.DurationSeconds);
            var worstTime = wins.Max(w => w.DurationSeconds);
            _bestTime.Text = loc.Get("stats.best_time_line", FormatTime(bestTime));
            _worstTime.Text = loc.Get("stats.worst_time_line", FormatTime(worstTime));
        }
        else
        {
            _bestTime.Text = loc.Get("stats.best_time_line", "--:--");
            _worstTime.Text = loc.Get("stats.worst_time_line", "--:--");
        }

        // Durchschnittliche Zeit pro Schwierigkeit
        _avgTimeKids.Text = loc.Get("stats.avg_time_difficulty", loc.GetDifficultyDisplay(Difficulty.Kids), GetAvgTime(wins, Difficulty.Kids));
        _avgTimeEasy.Text = loc.Get("stats.avg_time_difficulty", loc.GetDifficultyDisplay(Difficulty.Easy), GetAvgTime(wins, Difficulty.Easy));
        _avgTimeMedium.Text = loc.Get("stats.avg_time_difficulty", loc.GetDifficultyDisplay(Difficulty.Medium), GetAvgTime(wins, Difficulty.Medium));
        _avgTimeHard.Text = loc.Get("stats.avg_time_difficulty", loc.GetDifficultyDisplay(Difficulty.Hard), GetAvgTime(wins, Difficulty.Hard));
        _avgTimeInsane.Text = loc.Get("stats.avg_time_difficulty", loc.GetDifficultyDisplay(Difficulty.Insane), GetAvgTime(wins, Difficulty.Insane));

        // Durchschnittliche Fehler pro Schwierigkeit
        _avgMistakesKids.Text = loc.Get("stats.avg_mistakes_difficulty", loc.GetDifficultyDisplay(Difficulty.Kids), GetAvgMistakes(completed, Difficulty.Kids));
        _avgMistakesEasy.Text = loc.Get("stats.avg_mistakes_difficulty", loc.GetDifficultyDisplay(Difficulty.Easy), GetAvgMistakes(completed, Difficulty.Easy));
        _avgMistakesMedium.Text = loc.Get("stats.avg_mistakes_difficulty", loc.GetDifficultyDisplay(Difficulty.Medium), GetAvgMistakes(completed, Difficulty.Medium));
        _avgMistakesHard.Text = loc.Get("stats.avg_mistakes_difficulty", loc.GetDifficultyDisplay(Difficulty.Hard), GetAvgMistakes(completed, Difficulty.Hard));
        _avgMistakesInsane.Text = loc.Get("stats.avg_mistakes_difficulty", loc.GetDifficultyDisplay(Difficulty.Insane), GetAvgMistakes(completed, Difficulty.Insane));

        // Daily mit Icons
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        bool doneToday = settings.HasCompletedDaily(today);
        _dailyStreak.Text = loc.Get("stats.daily.streak_line", settings.DailyStreakCurrent, settings.DailyStreakBest);
        _dailyToday.Text = doneToday ? loc.Get("stats.daily.today_done_line") : loc.Get("stats.daily.today_open_line");
        _dailyRecent.Visible = false; // Hide the old text-based calendar
        RenderDailyCalendar(settings);

        // Techniques
        _techniquesSummary.Text = BuildTechniqueSummary(settings);

        // Scenarios
        _scenarioSummary.Text = BuildScenarioSummary(scenarioCompleted, scenarioWins);

        // Heatmap
        RenderHeatmap(settings);
    }

    private void RenderDailyCalendar(SettingsData settings)
    {
        // Clear old
        foreach (var child in _dailyCalendarContainer.GetChildren())
        {
            child.QueueFree();
        }

        var colors = _themeService.CurrentColors;

        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysFromMonday - 7); // Start from last week's Monday

        // 8 columns: week label + 7 days
        var grid = new GridContainer();
        grid.Columns = 8;
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        _dailyCalendarContainer.AddChild(grid);

        // Header row: empty + Mo Di Mi Do Fr Sa So
        string[] dayNames = GetDayNames();
        foreach (var dayName in dayNames)
        {
            var label = new Label();
            label.Text = dayName;
            label.CustomMinimumSize = new Vector2(32, 24);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeColorOverride("font_color", colors.TextSecondary);
            label.AddThemeFontSizeOverride("font_size", 12);
            grid.AddChild(label);
        }

        // Week rows
        for (int week = 0; week < 2; week++)
        {
            // Week label
            var weekLabel = new Label();
            weekLabel.Text = _localizationService.Get("stats.week_label", week + 1);
            weekLabel.CustomMinimumSize = new Vector2(32, 32);
            weekLabel.HorizontalAlignment = HorizontalAlignment.Center;
            weekLabel.VerticalAlignment = VerticalAlignment.Center;
            weekLabel.AddThemeColorOverride("font_color", colors.TextSecondary);
            weekLabel.AddThemeFontSizeOverride("font_size", 11);
            grid.AddChild(weekLabel);

            // Days
            for (int day = 0; day < 7; day++)
            {
                var date = weekStart.AddDays(week * 7 + day);
                string s = date.ToString("yyyy-MM-dd");
                bool done = settings.HasCompletedDaily(s);
                bool isFuture = date > today;
                bool isToday = date == today;

                var cell = new PanelContainer();
                cell.CustomMinimumSize = new Vector2(32, 32);

                var style = _themeService.CreatePanelStyleBox(6, 0);
                if (isToday)
                {
                    style.BorderColor = colors.Accent;
                    style.SetBorderWidthAll(2);
                    style.BgColor = done ? colors.Accent.Darkened(0.3f) : colors.CellBackground;
                }
                else if (done)
                {
                    style.BgColor = colors.Accent.Darkened(0.2f);
                }
                else if (isFuture)
                {
                    style.BgColor = colors.CellBackground.Darkened(0.3f);
                }
                else
                {
                    style.BgColor = colors.CellBackground;
                }
                cell.AddThemeStyleboxOverride("panel", style);

                var dayLabel = new Label();
                dayLabel.Text = done ? "✓" : (isFuture ? "" : date.Day.ToString());
                dayLabel.HorizontalAlignment = HorizontalAlignment.Center;
                dayLabel.VerticalAlignment = VerticalAlignment.Center;
                dayLabel.AddThemeColorOverride("font_color", done ? colors.Background : colors.TextPrimary);
                dayLabel.AddThemeFontSizeOverride("font_size", done ? 14 : 11);
                cell.AddChild(dayLabel);

                string tooltipDate = _localizationService.CurrentLanguage == Language.German
                    ? date.ToString("dd.MM.yyyy")
                    : date.ToString("yyyy-MM-dd");
                cell.TooltipText = tooltipDate + (done ? " ✓" : (isFuture ? "" : " ✗"));
                grid.AddChild(cell);
            }
        }
    }

    private string BuildDailyRecentText(SettingsData settings, int days)
    {
        var sb = new System.Text.StringBuilder();
        var loc = _localizationService;
        sb.Append(loc.Get("stats.daily_recent.header"));

        // Week labels
        var dayNames = GetDayNames();
        sb.Append("    " + string.Join("  ", dayNames.Skip(1)) + "\n");

        // Find the Monday of the current week
        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysFromMonday - 7); // Start from last week's Monday

        for (int week = 0; week < 2; week++)
        {
            string weekLabel = loc.Get("stats.week_label", week + 1);
            sb.Append($"{weekLabel,-4}");
            for (int day = 0; day < 7; day++)
            {
                var date = weekStart.AddDays(week * 7 + day);
                string s = date.ToString("yyyy-MM-dd");
                bool done = settings.HasCompletedDaily(s);
                bool isFuture = date > today;

                if (isFuture)
                    sb.Append("·   ");
                else if (done)
                    sb.Append("✅  ");
                else
                    sb.Append("⬜  ");
            }
            sb.Append("\n");
        }
        return sb.ToString();
    }

    private string BuildTechniqueSummary(SettingsData settings)
    {
        var shown = settings.TechniqueHintShownCounts;
        var applied = settings.TechniqueHintAppliedCounts;

        if (shown.Count == 0)
        {
            return _localizationService.Get("stats.techniques.none");
        }

        var top = shown
            .OrderByDescending(kv => kv.Value)
            .Take(6)
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.Append(_localizationService.Get("stats.techniques.header"));
        foreach (var kv in top)
        {
            applied.TryGetValue(kv.Key, out int ok);
            double rate = kv.Value > 0 ? (double)ok / kv.Value * 100 : 0;
            string bar = GetProgressBar(rate);

            string techniqueName = TechniqueInfo.Techniques.TryGetValue(kv.Key, out var tech)
                ? tech.Name
                : kv.Key;

            sb.Append(_localizationService.Get("stats.techniques.line", techniqueName, kv.Value, ok, rate, bar));
        }
        return sb.ToString();
    }

    private string BuildScenarioSummary(List<HistoryEntry> scenarioCompleted, List<HistoryEntry> scenarioWins)
    {
        if (scenarioCompleted.Count == 0)
        {
            return _localizationService.Get("stats.scenario.none");
        }

        var sb = new System.Text.StringBuilder();

        // Overall stats
        double winRate = scenarioCompleted.Count > 0 ? (double)scenarioWins.Count / scenarioCompleted.Count * 100 : 0;
        sb.Append(_localizationService.Get("stats.scenario.overview_header"));
        sb.Append(_localizationService.Get("stats.scenario.overview_line", scenarioCompleted.Count, scenarioWins.Count, winRate));

        // Group by technique
        var byTechnique = scenarioCompleted
            .Where(s => !string.IsNullOrEmpty(s.ScenarioTechnique))
            .GroupBy(s => s.ScenarioTechnique!)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .ToList();

        if (byTechnique.Count > 0)
        {
            sb.Append(_localizationService.Get("stats.scenario.by_tech_header"));
            foreach (var group in byTechnique)
            {
                int total = group.Count();
                int won = group.Count(e => e.Status == GameStatus.Won);
                double rate = total > 0 ? (double)won / total * 100 : 0;
                string bar = GetProgressBar(rate);

                // Get best time for won scenarios
                var wonEntries = group.Where(e => e.Status == GameStatus.Won).ToList();
                string bestTimeStr = wonEntries.Count > 0
                    ? FormatTimeStatic(wonEntries.Min(e => e.DurationSeconds))
                    : "--:--";

                sb.Append(_localizationService.Get("stats.scenario.line", group.Key, won, total, rate, bestTimeStr, bar));
            }
        }

        return sb.ToString();
    }

    private static string FormatTimeStatic(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.Hours > 0)
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private static string GetProgressBar(double percent)
    {
        int filled = (int)(percent / 10);
        int empty = 10 - filled;
        return "█".PadRight(filled, '█').PadRight(10, '░');
    }

    private void RenderHeatmap(SettingsData settings)
    {
        // Clear old
        foreach (var child in _heatmapContainer.GetChildren())
        {
            child.QueueFree();
        }

        settings.EnsureHeatmapSizes();
        var colors = _themeService.CurrentColors;

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

                var sb = _themeService.CreatePanelStyleBox(4, 0);
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
        _audioService.PlayClick();
        _appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void OnLanguageChanged(int languageIndex)
    {
        ApplyLocalization();
        CalculateStats();
    }

    private void ApplyLocalization()
    {
        var loc = _localizationService;

        _title.Text = loc.Get("stats.title");
        _backButton.Text = loc.Get("menu.back");
        _backButton.TooltipText = loc.Get("settings.back.tooltip");

        var statsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer");

        statsContainer.GetNode<Label>("OverviewSection/OverviewTitle").Text = loc.Get("stats.overview");
        statsContainer.GetNode<Label>("TimeSection/TimeTitle").Text = loc.Get("stats.times");
        statsContainer.GetNode<Label>("MistakesSection/MistakesTitle").Text = loc.Get("stats.mistakes_title");
        statsContainer.GetNode<Label>("DailySection/DailyTitle").Text = loc.Get("stats.daily");
        statsContainer.GetNode<Label>("TechniquesSection/TechniquesTitle").Text = loc.Get("stats.techniques");
        statsContainer.GetNode<Label>("ScenarioSection/ScenarioTitle").Text = loc.Get("stats.scenarios");
        statsContainer.GetNode<Label>("HeatmapSection/HeatmapTitle").Text = loc.Get("stats.heatmap");
    }

    private string[] GetDayNames()
    {
        return _localizationService.CurrentLanguage == Language.German
            ? new[] { "", "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" }
            : new[] { "", "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" };
    }

    private void ApplyTheme()
    {
        var colors = _themeService.CurrentColors;

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);

        var panelStyle = _themeService.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _backButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);

        // Alle Labels im Panel
        ApplyLabelTheme(colors);

        _dailyRecent.AddThemeColorOverride("default_color", colors.TextPrimary);
        _techniquesSummary.AddThemeColorOverride("default_color", colors.TextPrimary);
        _scenarioSummary.AddThemeColorOverride("default_color", colors.TextPrimary);
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
