namespace MySudoku.UI;

/// <summary>
/// Hauptmenü
/// </summary>
public partial class MainMenu : Control
{
    private Button _continueButton = null!;
    private Button _startButton = null!;
    private Button _dailyButton = null!;
    private Button _scenariosButton = null!;
    private Button _settingsButton = null!;
    private Button _historyButton = null!;
    private Button _statsButton = null!;
    private Button _tipsButton = null!;
    private Button _quitButton = null!;
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private Label _subtitle = null!;
    private Label _dailyInfo = null!;

    public override void _Ready()
    {
        // Hole Referenzen
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Title");
        _subtitle = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Subtitle");
        _dailyInfo = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/DailyInfo");

        var buttonContainer = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ButtonContainer");
        _continueButton = buttonContainer.GetNode<Button>("ContinueButton");
        _startButton = buttonContainer.GetNode<Button>("StartButton");
        _dailyButton = buttonContainer.GetNode<Button>("DailyButton");
        _scenariosButton = buttonContainer.GetNode<Button>("ScenariosButton");
        _settingsButton = buttonContainer.GetNode<Button>("SettingsButton");
        _historyButton = buttonContainer.GetNode<Button>("HistoryButton");
        _statsButton = buttonContainer.GetNode<Button>("StatsButton");
        _tipsButton = buttonContainer.GetNode<Button>("TipsButton");
        _quitButton = buttonContainer.GetNode<Button>("QuitButton");

        // Events verbinden
        _continueButton.Pressed += OnContinuePressed;
        _startButton.Pressed += OnStartPressed;
        _dailyButton.Pressed += OnDailyPressed;
        _scenariosButton.Pressed += OnScenariosPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _historyButton.Pressed += OnHistoryPressed;
        _statsButton.Pressed += OnStatsPressed;
        _tipsButton.Pressed += OnTipsPressed;
        _quitButton.Pressed += OnQuitPressed;

        // Theme anwenden
        ApplyTheme();
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;

        // Continue-Button nur anzeigen wenn SaveGame existiert
        UpdateContinueButton();
        UpdateDailyInfo();

        // Focus auf ersten verfügbaren Button
        CallDeferred(nameof(SetInitialFocus));
    }

    public override void _ExitTree()
    {
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged -= OnThemeChanged;
    }

    private void SetInitialFocus()
    {
        if (_continueButton.Visible)
            _continueButton.GrabFocus();
        else
            _startButton.GrabFocus();
    }

    private void UpdateContinueButton()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        _continueButton.Visible = saveService.HasSaveGame;
    }

    private void UpdateDailyInfo()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        var settings = saveService.Settings;
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        bool doneToday = settings.HasCompletedDaily(today);

        string streak = settings.DailyStreakCurrent > 0
            ? $"Streak: {settings.DailyStreakCurrent} (Best: {settings.DailyStreakBest})"
            : "Streak: 0";

        _dailyInfo.Text = doneToday
            ? $"Daily erledigt ✅  |  {streak}"
            : $"Daily offen  |  {streak}";

        // Button copy/tooltip
        _dailyButton.Text = doneToday ? "📅 Daily Sudoku (erledigt)" : "📅 Daily Sudoku";
        _dailyButton.TooltipText = doneToday
            ? "Daily für heute ist bereits erledigt.\nDu kannst es trotzdem erneut spielen (ohne extra Streak)."
            : "Tägliches Sudoku (deterministisch).";
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        // Panel-Style
        var panelStyle = theme.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        // Title-Farbe
        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _subtitle.AddThemeColorOverride("font_color", colors.TextSecondary);
        _dailyInfo.AddThemeColorOverride("font_color", colors.TextSecondary);

        // Button-Styles
        ApplyButtonTheme(_continueButton, theme);
        ApplyButtonTheme(_startButton, theme);
        ApplyButtonTheme(_dailyButton, theme);
        ApplyButtonTheme(_scenariosButton, theme);
        ApplyButtonTheme(_settingsButton, theme);
        ApplyButtonTheme(_historyButton, theme);
        ApplyButtonTheme(_statsButton, theme);
        ApplyButtonTheme(_tipsButton, theme);
        ApplyButtonTheme(_quitButton, theme);
    }

    private void ApplyButtonTheme(Button button, ThemeService theme)
    {
        var colors = theme.CurrentColors;

        button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        button.AddThemeStyleboxOverride("disabled", theme.CreateButtonStyleBox(disabled: true));
        button.AddThemeStyleboxOverride("focus", theme.CreateButtonStyleBox(hover: true));

        button.AddThemeColorOverride("font_color", colors.TextPrimary);
        button.AddThemeColorOverride("font_hover_color", colors.TextPrimary);
        button.AddThemeColorOverride("font_pressed_color", colors.TextPrimary);
        button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
    }

    private void OnContinuePressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.ContinueGame();
    }

    private void OnStartPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.NavigateTo(AppState.SCENE_DIFFICULTY);
    }

    private void OnDailyPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.StartDailyGame();
    }

    private void OnSettingsPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.NavigateTo(AppState.SCENE_SETTINGS);
    }

    private void OnHistoryPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.NavigateTo(AppState.SCENE_HISTORY);
    }

    private void OnStatsPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.NavigateTo(AppState.SCENE_STATS);
    }

    private void OnTipsPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.NavigateTo(AppState.SCENE_TIPS);
    }

    private void OnScenariosPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.NavigateTo(AppState.SCENE_SCENARIOS);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
