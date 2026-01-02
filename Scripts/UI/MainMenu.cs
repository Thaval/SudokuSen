namespace SudokuSen.UI;

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
    private LocalizationService _localizationService = null!;

    private Button _continueButton = null!;
    private Button _startButton = null!;
    private Button _dailyButton = null!;
    private Button _puzzlesButton = null!;
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
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");

        UiNavigationSfx.Wire(this, _audioService);

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
        _puzzlesButton = buttonContainer.GetNode<Button>("PuzzlesButton");
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
        _puzzlesButton.Pressed += OnPuzzlesPressed;
        _scenariosButton.Pressed += OnScenariosPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _historyButton.Pressed += OnHistoryPressed;
        _statsButton.Pressed += OnStatsPressed;
        _tipsButton.Pressed += OnTipsPressed;
        _quitButton.Pressed += OnQuitPressed;

        // Theme anwenden
        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        // Continue-Button nur anzeigen wenn SaveGame existiert
        UpdateContinueButton();
        UpdateDailyInfo();
        UpdateLocalizedText();

        // Focus auf ersten verfügbaren Button
        CallDeferred(nameof(SetInitialFocus));
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(int languageIndex)
    {
        UpdateLocalizedText();
        UpdateDailyInfo();
    }

    private void UpdateLocalizedText()
    {
        var l = _localizationService;
        _title.Text = l.Get("menu.title");
        _subtitle.Text = l.Get("menu.subtitle");
        _continueButton.Text = l.Get("menu.continue");
        _continueButton.TooltipText = l.Get("menu.continue.tooltip");
        _startButton.Text = l.Get("menu.new_game");
        _startButton.TooltipText = l.Get("menu.new_game.tooltip");
        _puzzlesButton.Text = l.Get("menu.puzzles");
        _puzzlesButton.TooltipText = l.Get("menu.puzzles.tooltip");
        _scenariosButton.Text = l.Get("menu.scenarios");
        _scenariosButton.TooltipText = l.Get("menu.scenarios.tooltip");
        _historyButton.Text = l.Get("menu.history");
        _historyButton.TooltipText = l.Get("menu.history.tooltip");
        _statsButton.Text = l.Get("menu.stats");
        _statsButton.TooltipText = l.Get("menu.stats.tooltip");
        _tipsButton.Text = l.Get("menu.tips");
        _tipsButton.TooltipText = l.Get("menu.tips.tooltip");
        _settingsButton.Text = l.Get("menu.settings");
        _settingsButton.TooltipText = l.Get("menu.settings.tooltip");
        _quitButton.Text = l.Get("menu.quit");
        _quitButton.TooltipText = l.Get("menu.quit.tooltip");
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
        var l = _localizationService;
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        bool doneToday = settings.HasCompletedDaily(today);

        string streakLabel = l.Get("stats.streak");
        string bestLabel = l.Get("stats.best_streak");
        string streak = settings.DailyStreakCurrent > 0
            ? $"{streakLabel}: {settings.DailyStreakCurrent} ({bestLabel}: {settings.DailyStreakBest})"
            : $"{streakLabel}: 0";

        string doneText = l.Get("stats.today.done");
        string openText = l.Get("stats.today.open");
        string dailyLabel = l.Get("menu.daily");
        _dailyInfo.Text = doneToday
            ? $"{dailyLabel} {doneText}  |  {streak}"
            : $"{dailyLabel} {openText}  |  {streak}";

        // Button copy/tooltip
        _dailyButton.Text = doneToday
            ? $"{l.Get("menu.daily")} ({doneText})"
            : l.Get("menu.daily");
        _dailyButton.TooltipText = doneToday
            ? l.Get("menu.daily.tooltip.done")
            : l.Get("menu.daily.tooltip.open");
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
        ApplyButtonTheme(_puzzlesButton);
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
        GD.Print("[UI] MainMenu: Continue pressed");
        _audioService.PlayClick();
        _appState.ContinueGame();
    }

    private void OnStartPressed()
    {
        GD.Print("[UI] MainMenu: Start pressed");
        _audioService.PlayClick();

        var settings = _saveService.Settings;

        // Check if any challenge mode is active
        bool hasChallengeActive = settings.ChallengeNoNotes ||
                                   settings.ChallengePerfectRun ||
                                   settings.ChallengeHintLimit > 0 ||
                                   settings.ChallengeTimeAttackMinutes > 0;

        // If challenge mode is active and difficulty is set, skip difficulty selection
        if (hasChallengeActive && settings.ChallengeDifficulty > 0)
        {
            // ChallengeDifficulty: 1=Easy, 2=Medium, 3=Hard, 4=Insane
            var difficulty = settings.ChallengeDifficulty switch
            {
                1 => Difficulty.Easy,
                2 => Difficulty.Medium,
                3 => Difficulty.Hard,
                4 => Difficulty.Insane,
                _ => Difficulty.Medium
            };
            GD.Print($"[UI] MainMenu: Challenge mode active, starting with difficulty {difficulty}");
            _appState.StartNewGame(difficulty);
            return;
        }

        // If challenge mode with Auto difficulty, use recommended difficulty
        if (hasChallengeActive && settings.ChallengeDifficulty == 0)
        {
            var recommendedDifficulty = _saveService.GetRecommendedDifficulty();
            GD.Print($"[UI] MainMenu: Challenge mode with Auto, recommended difficulty = {recommendedDifficulty}");
            _appState.StartNewGame(recommendedDifficulty);
            return;
        }

        // Normal flow: go to difficulty selection
        _appState.NavigateTo(AppState.SCENE_DIFFICULTY);
    }

    private void OnDailyPressed()
    {
        GD.Print("[UI] MainMenu: Daily pressed");
        _audioService.PlayClick();
        _appState.StartDailyGame();
    }

    private void OnSettingsPressed()
    {
        GD.Print("[UI] MainMenu: Settings pressed");
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_SETTINGS);
    }

    private void OnHistoryPressed()
    {
        GD.Print("[UI] MainMenu: History pressed");
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_HISTORY);
    }

    private void OnStatsPressed()
    {
        GD.Print("[UI] MainMenu: Stats pressed");
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_STATS);
    }

    private void OnTipsPressed()
    {
        GD.Print("[UI] MainMenu: Tips pressed");
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_TIPS);
    }

    private void OnPuzzlesPressed()
    {
        GD.Print("[UI] MainMenu: Puzzles pressed");
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_PUZZLES);
    }

    private void OnScenariosPressed()
    {
        GD.Print("[UI] MainMenu: Scenarios pressed");
        _audioService.PlayClick();
        _appState.NavigateTo(AppState.SCENE_SCENARIOS);
    }

    private void OnQuitPressed()
    {
        GD.Print("[UI] MainMenu: Quit pressed");
        _audioService.PlayClick();
        GetTree().Quit();
    }
}
