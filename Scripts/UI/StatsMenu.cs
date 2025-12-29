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

    public override void _Ready()
    {
        _backButton = GetNode<Button>("VBoxContainer/Header/BackButton");
        _title = GetNode<Label>("VBoxContainer/Header/Title");
        _panel = GetNode<PanelContainer>("VBoxContainer/CenterContainer/Panel");

        var statsContainer = GetNode<VBoxContainer>("VBoxContainer/CenterContainer/Panel/MarginContainer/VBoxContainer");

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
    }

    private void ApplyLabelTheme(ThemeService.ThemeColors colors)
    {
        var statsContainer = GetNode<VBoxContainer>("VBoxContainer/CenterContainer/Panel/MarginContainer/VBoxContainer");

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
