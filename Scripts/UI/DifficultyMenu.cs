namespace SudokuSen.UI;

/// <summary>
/// Schwierigkeitsauswahl-Menü
/// </summary>
public partial class DifficultyMenu : Control
{
    // Cached Service References
    private ThemeService _themeService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;
    private AudioService _audioService = null!;
    private LocalizationService _localizationService = null!;

    private Button _kidsButton = null!;
    private Button _easyButton = null!;
    private Button _mediumButton = null!;
    private Button _hardButton = null!;
    private Button _insaneButton = null!;
    private Button _backButton = null!;
    private Label _kidsTechniques = null!;
    private Label _easyTechniques = null!;
    private Label _mediumTechniques = null!;
    private Label _hardTechniques = null!;
    private Label _insaneTechniques = null!;
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private Label _description = null!;

    public override void _Ready()
    {
        // Cache service references
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");

        UiNavigationSfx.Wire(this, _audioService);

        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("Title");
        _description = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Description");

        var buttonContainer = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ButtonContainer");
        _kidsButton = buttonContainer.GetNode<Button>("KidsContainer/KidsButton");
        _easyButton = buttonContainer.GetNode<Button>("EasyContainer/EasyButton");
        _mediumButton = buttonContainer.GetNode<Button>("MediumContainer/MediumButton");
        _hardButton = buttonContainer.GetNode<Button>("HardContainer/HardButton");
        _insaneButton = buttonContainer.GetNode<Button>("InsaneContainer/InsaneButton");
        _kidsTechniques = buttonContainer.GetNode<Label>("KidsContainer/KidsTechniques");
        _easyTechniques = buttonContainer.GetNode<Label>("EasyContainer/EasyTechniques");
        _mediumTechniques = buttonContainer.GetNode<Label>("MediumContainer/MediumTechniques");
        _hardTechniques = buttonContainer.GetNode<Label>("HardContainer/HardTechniques");
        _insaneTechniques = buttonContainer.GetNode<Label>("InsaneContainer/InsaneTechniques");
        _backButton = GetNode<Button>("BackButton");

        _kidsButton.Pressed += () => OnDifficultySelected(Difficulty.Kids);
        _easyButton.Pressed += () => OnDifficultySelected(Difficulty.Easy);
        _mediumButton.Pressed += () => OnDifficultySelected(Difficulty.Medium);
        _hardButton.Pressed += () => OnDifficultySelected(Difficulty.Hard);
        _insaneButton.Pressed += () => OnDifficultySelected(Difficulty.Insane);
        _backButton.Pressed += OnBackPressed;

        // Technik-Beschreibungen unter den Buttons
        UpdateTechniqueLabels();
        UpdateLocalizedText();

        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        _easyButton.GrabFocus();
    }

    /// <summary>
    /// Updates all text to current language
    /// </summary>
    private void UpdateLocalizedText()
    {
        var l = _localizationService;
        _title.Text = l.Get("difficulty.title");
        _description.Text = l.Get("difficulty.description");
        _backButton.Text = l.Get("menu.back");

        _kidsButton.Text = l.Get("difficulty.kids");
        _kidsButton.TooltipText = l.Get("difficulty.kids.tooltip");
        _easyButton.Text = l.Get("difficulty.easy");
        _easyButton.TooltipText = l.Get("difficulty.easy.tooltip");
        _mediumButton.Text = l.Get("difficulty.medium");
        _mediumButton.TooltipText = l.Get("difficulty.medium.tooltip");
        _hardButton.Text = l.Get("difficulty.hard");
        _hardButton.TooltipText = l.Get("difficulty.hard.tooltip");
        _insaneButton.Text = l.Get("difficulty.insane");
        _insaneButton.TooltipText = l.Get("difficulty.insane.tooltip");
    }

    private void OnLanguageChanged(int languageIndex)
    {
        UpdateLocalizedText();
    }

    /// <summary>
    /// Aktualisiert die Technik-Labels basierend auf den Einstellungen
    /// </summary>
    private void UpdateTechniqueLabels()
    {
        var settings = _saveService.Settings;

        _kidsTechniques.Text = GetTechniqueSummary(Difficulty.Kids, settings.GetTechniquesForDifficulty(Difficulty.Kids));
        _easyTechniques.Text = GetTechniqueSummary(Difficulty.Easy, settings.GetTechniquesForDifficulty(Difficulty.Easy));
        _mediumTechniques.Text = GetTechniqueSummary(Difficulty.Medium, settings.GetTechniquesForDifficulty(Difficulty.Medium));
        _hardTechniques.Text = GetTechniqueSummary(Difficulty.Hard, settings.GetTechniquesForDifficulty(Difficulty.Hard));
        _insaneTechniques.Text = GetTechniqueSummary(Difficulty.Insane, settings.GetTechniquesForDifficulty(Difficulty.Insane));
    }

    private string GetTechniqueSummary(Difficulty difficulty, HashSet<string>? enabledTechniques)
    {
        var l = _localizationService;

        if (difficulty == Difficulty.Kids)
        {
            return l.Get("difficulty.kids.desc");
        }

        var techIds = enabledTechniques ?? TechniqueInfo.GetDefaultTechniques(difficulty);

        var previousTechIds = TechniqueInfo.GetCumulativeTechniques(_saveService.Settings, difficulty, includeUpTo: false);

        var uniqueTechs = new List<string>();
        foreach (var id in TechniqueInfo.AllTechniqueIds)
        {
            if (techIds.Contains(id) && !previousTechIds.Contains(id))
            {
                uniqueTechs.Add(l.GetTechniqueName(id));
            }
        }

        if (uniqueTechs.Count == 0)
        {
            return difficulty switch
            {
                Difficulty.Easy => l.Get("difficulty.easy.desc"),
                Difficulty.Medium => l.Get("difficulty.medium.desc"),
                Difficulty.Hard => l.Get("difficulty.hard.desc"),
                Difficulty.Insane => l.Get("difficulty.insane.desc"),
                _ => ""
            };
        }

        if (uniqueTechs.Count > 3)
        {
            return string.Join(", ", uniqueTechs.Take(3)) + "...";
        }

        return string.Join(", ", uniqueTechs);
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

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var colors = _themeService.CurrentColors;

        var panelStyle = _themeService.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _description.AddThemeColorOverride("font_color", colors.TextSecondary);

        // Technik-Labels stylen
        _kidsTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);
        _easyTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);
        _mediumTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);
        _hardTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);
        _insaneTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);

        ApplyButtonTheme(_kidsButton, new Color("2196f3")); // Blau für Kids
        ApplyButtonTheme(_easyButton, new Color("4caf50")); // Grün
        ApplyButtonTheme(_mediumButton, new Color("ff9800")); // Orange
        ApplyButtonTheme(_hardButton, new Color("f44336")); // Rot
        ApplyButtonTheme(_insaneButton, new Color("9c27b0")); // Lila für Insane
        ApplyButtonTheme(_backButton);
    }

    private void ApplyButtonTheme(Button button, Color? accentColor = null)
    {
        var colors = _themeService.CurrentColors;

        if (accentColor.HasValue)
        {
            var normalStyle = new StyleBoxFlat();
            normalStyle.BgColor = accentColor.Value.Darkened(0.2f);
            normalStyle.CornerRadiusTopLeft = 6;
            normalStyle.CornerRadiusTopRight = 6;
            normalStyle.CornerRadiusBottomLeft = 6;
            normalStyle.CornerRadiusBottomRight = 6;
            normalStyle.ContentMarginLeft = 16;
            normalStyle.ContentMarginRight = 16;
            normalStyle.ContentMarginTop = 8;
            normalStyle.ContentMarginBottom = 8;

            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = accentColor.Value;
            hoverStyle.CornerRadiusTopLeft = 6;
            hoverStyle.CornerRadiusTopRight = 6;
            hoverStyle.CornerRadiusBottomLeft = 6;
            hoverStyle.CornerRadiusBottomRight = 6;
            hoverStyle.ContentMarginLeft = 16;
            hoverStyle.ContentMarginRight = 16;
            hoverStyle.ContentMarginTop = 8;
            hoverStyle.ContentMarginBottom = 8;

            button.AddThemeStyleboxOverride("normal", normalStyle);
            button.AddThemeStyleboxOverride("hover", hoverStyle);
            button.AddThemeStyleboxOverride("pressed", hoverStyle);
            button.AddThemeStyleboxOverride("focus", hoverStyle);
            button.AddThemeColorOverride("font_color", Colors.White);
            button.AddThemeColorOverride("font_hover_color", Colors.White);
            button.AddThemeColorOverride("font_pressed_color", Colors.White);
        }
        else
        {
            button.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
            button.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
            button.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
            button.AddThemeStyleboxOverride("focus", _themeService.CreateButtonStyleBox(hover: true));
            button.AddThemeColorOverride("font_color", colors.TextPrimary);
            button.AddThemeColorOverride("font_hover_color", colors.TextPrimary);
            button.AddThemeColorOverride("font_pressed_color", colors.TextPrimary);
        }
    }

    private void OnDifficultySelected(Difficulty difficulty)
    {
        GD.Print($"[UI] DifficultyMenu: difficulty selected = {difficulty}");
        _audioService.PlayClick();
        _appState.StartNewGame(difficulty);
    }

    private void OnBackPressed()
    {
        GD.Print("[UI] DifficultyMenu: Back pressed");
        _audioService.PlayClick();
        _appState.GoToMainMenu();
    }
}
