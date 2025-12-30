namespace MySudoku.UI;

/// <summary>
/// Hauptmenü
/// </summary>
public partial class MainMenu : Control
{
    // Cached Service References
    private ThemeService _themeService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;
    private AudioService _audioService = null!;

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
        // Cache service references
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _audioService = GetNode<AudioService>("/root/AudioService");

        // Start menu music
        _audioService.StartMenuMusic();

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
        _themeService.ThemeChanged += OnThemeChanged;

        // Continue-Button nur anzeigen wenn SaveGame existiert
        UpdateContinueButton();
        UpdateDailyInfo();

        // Focus auf ersten verfügbaren Button
        CallDeferred(nameof(SetInitialFocus));
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
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
        _continueButton.Visible = _saveService.HasSaveGame;
    }

    private void UpdateDailyInfo()
    {
        var settings = _saveService.Settings;
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
        var colors = _themeService.CurrentColors;

        // Panel-Style
        var panelStyle = _themeService.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        // Title-Farbe
        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _subtitle.AddThemeColorOverride("font_color", colors.TextSecondary);
        _dailyInfo.AddThemeColorOverride("font_color", colors.TextSecondary);

        // Button-Styles
        ApplyButtonTheme(_continueButton);
        ApplyButtonTheme(_startButton);
        ApplyButtonTheme(_dailyButton);
        ApplyButtonTheme(_scenariosButton);
        ApplyButtonTheme(_settingsButton);
        ApplyButtonTheme(_historyButton);
        ApplyButtonTheme(_statsButton);
        ApplyButtonTheme(_tipsButton);
        ApplyButtonTheme(_quitButton);
    }

    private void ApplyButtonTheme(Button button)
    {
        var colors = _themeService.CurrentColors;

        button.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        button.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        button.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        button.AddThemeStyleboxOverride("disabled", _themeService.CreateButtonStyleBox(disabled: true));
        button.AddThemeStyleboxOverride("focus", _themeService.CreateButtonStyleBox(hover: true));

        button.AddThemeColorOverride("font_color", colors.TextPrimary);
        button.AddThemeColorOverride("font_hover_color", colors.TextPrimary);
        button.AddThemeColorOverride("font_pressed_color", colors.TextPrimary);
        button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
    }

    private void OnContinuePressed()
    {
        _audioService.PlayClick();
        _appState.ContinueGame();
    }

    private void OnStartPressed()
    {
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_DIFFICULTY);
    }

    private void OnDailyPressed()
    {
        _audioService.PlayClick();
        _appState.StartDailyGame();
    }

    private void OnSettingsPressed()
    {
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_SETTINGS);
    }

    private void OnHistoryPressed()
    {
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_HISTORY);
    }

    private void OnStatsPressed()
    {
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_STATS);
    }

    private void OnTipsPressed()
    {
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_TIPS);
    }

    private void OnScenariosPressed()
    {
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_SCENARIOS);
    }

    private void OnQuitPressed()
    {
        _audioService.PlayClick();
        GetTree().Quit();
    }
}
