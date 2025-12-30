namespace MySudoku.UI;

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

    private Button _kidsButton = null!;
    private Button _easyButton = null!;
    private Button _mediumButton = null!;
    private Button _hardButton = null!;
    private Button _backButton = null!;
    private Label _kidsTechniques = null!;
    private Label _easyTechniques = null!;
    private Label _mediumTechniques = null!;
    private Label _hardTechniques = null!;
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

        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("Title");
        _description = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Description");

        var buttonContainer = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ButtonContainer");
        _kidsButton = buttonContainer.GetNode<Button>("KidsContainer/KidsButton");
        _easyButton = buttonContainer.GetNode<Button>("EasyContainer/EasyButton");
        _mediumButton = buttonContainer.GetNode<Button>("MediumContainer/MediumButton");
        _hardButton = buttonContainer.GetNode<Button>("HardContainer/HardButton");
        _kidsTechniques = buttonContainer.GetNode<Label>("KidsContainer/KidsTechniques");
        _easyTechniques = buttonContainer.GetNode<Label>("EasyContainer/EasyTechniques");
        _mediumTechniques = buttonContainer.GetNode<Label>("MediumContainer/MediumTechniques");
        _hardTechniques = buttonContainer.GetNode<Label>("HardContainer/HardTechniques");
        _backButton = GetNode<Button>("BackButton");

        _kidsButton.Pressed += () => OnDifficultySelected(Difficulty.Kids);
        _easyButton.Pressed += () => OnDifficultySelected(Difficulty.Easy);
        _mediumButton.Pressed += () => OnDifficultySelected(Difficulty.Medium);
        _hardButton.Pressed += () => OnDifficultySelected(Difficulty.Hard);
        _backButton.Pressed += OnBackPressed;

        // Technik-Beschreibungen unter den Buttons
        UpdateTechniqueLabels();

        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;

        _easyButton.GrabFocus();
    }

    /// <summary>
    /// Aktualisiert die Technik-Labels basierend auf den Einstellungen
    /// </summary>
    private void UpdateTechniqueLabels()
    {
        var settings = _saveService.Settings;

        _kidsTechniques.Text = TechniqueInfo.GetShortTechniqueList(Difficulty.Kids, settings.GetTechniquesForDifficulty(Difficulty.Kids));
        _easyTechniques.Text = TechniqueInfo.GetShortTechniqueList(Difficulty.Easy, settings.GetTechniquesForDifficulty(Difficulty.Easy));
        _mediumTechniques.Text = TechniqueInfo.GetShortTechniqueList(Difficulty.Medium, settings.GetTechniquesForDifficulty(Difficulty.Medium));
        _hardTechniques.Text = TechniqueInfo.GetShortTechniqueList(Difficulty.Hard, settings.GetTechniquesForDifficulty(Difficulty.Hard));
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
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

        ApplyButtonTheme(_kidsButton, new Color("2196f3")); // Blau für Kids
        ApplyButtonTheme(_easyButton, new Color("4caf50")); // Grün
        ApplyButtonTheme(_mediumButton, new Color("ff9800")); // Orange
        ApplyButtonTheme(_hardButton, new Color("f44336")); // Rot
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
