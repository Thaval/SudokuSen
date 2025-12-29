namespace MySudoku.UI;

/// <summary>
/// Einstellungsmenü
/// </summary>
public partial class SettingsMenu : Control
{
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private OptionButton _themeOption = null!;
    private CheckButton _deadlyCheck = null!;
    private CheckButton _hideCheck = null!;
    private CheckButton _highlightCheck = null!;

    private CheckButton _learnCheck = null!;
    private CheckButton _colorblindCheck = null!;
    private HSlider _uiScaleSlider = null!;
    private Label _uiScaleValue = null!;

    private CheckButton _smartCleanupCheck = null!;
    private CheckButton _houseAutoFillCheck = null!;

    private CheckButton _challengeNoNotesCheck = null!;
    private CheckButton _challengePerfectCheck = null!;
    private OptionButton _challengeHintsOption = null!;
    private OptionButton _challengeTimeOption = null!;
    private Button _backButton = null!;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("Title");

        var settingsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer/SettingsContainer");
        _themeOption = settingsContainer.GetNode<OptionButton>("ThemeRow/ThemeOption");
        _deadlyCheck = settingsContainer.GetNode<CheckButton>("DeadlyRow/DeadlyCheck");
        _hideCheck = settingsContainer.GetNode<CheckButton>("HideRow/HideCheck");
        _highlightCheck = settingsContainer.GetNode<CheckButton>("HighlightRow/HighlightCheck");

        _learnCheck = settingsContainer.GetNode<CheckButton>("LearnRow/LearnCheck");
        _colorblindCheck = settingsContainer.GetNode<CheckButton>("ColorblindRow/ColorblindCheck");
        _uiScaleSlider = settingsContainer.GetNode<HSlider>("UiScaleRow/UiScaleSlider");
        _uiScaleValue = settingsContainer.GetNode<Label>("UiScaleRow/UiScaleValue");

        _smartCleanupCheck = settingsContainer.GetNode<CheckButton>("SmartCleanupRow/SmartCleanupCheck");
        _houseAutoFillCheck = settingsContainer.GetNode<CheckButton>("HouseAutoFillRow/HouseAutoFillCheck");

        _challengeNoNotesCheck = settingsContainer.GetNode<CheckButton>("ChallengeNoNotesRow/ChallengeNoNotesCheck");
        _challengePerfectCheck = settingsContainer.GetNode<CheckButton>("ChallengePerfectRow/ChallengePerfectCheck");
        _challengeHintsOption = settingsContainer.GetNode<OptionButton>("ChallengeHintsRow/ChallengeHintsOption");
        _challengeTimeOption = settingsContainer.GetNode<OptionButton>("ChallengeTimeRow/ChallengeTimeOption");
        _backButton = GetNode<Button>("BackButton");

        // Theme-Optionen
        _themeOption.AddItem("Hell", 0);
        _themeOption.AddItem("Dunkel", 1);

        _challengeHintsOption.AddItem("Aus", 0);
        _challengeHintsOption.AddItem("3", 3);
        _challengeHintsOption.AddItem("5", 5);
        _challengeHintsOption.AddItem("10", 10);

        _challengeTimeOption.AddItem("Aus", 0);
        _challengeTimeOption.AddItem("10 min", 10);
        _challengeTimeOption.AddItem("15 min", 15);
        _challengeTimeOption.AddItem("20 min", 20);

        // Werte laden
        LoadSettings();

        // Events
        _themeOption.ItemSelected += OnThemeSelected;
        _deadlyCheck.Toggled += OnDeadlyToggled;
        _hideCheck.Toggled += OnHideToggled;
        _highlightCheck.Toggled += OnHighlightToggled;

        _learnCheck.Toggled += OnLearnToggled;
        _colorblindCheck.Toggled += OnColorblindToggled;
        _uiScaleSlider.ValueChanged += OnUiScaleChanged;

        _smartCleanupCheck.Toggled += OnSmartCleanupToggled;
        _houseAutoFillCheck.Toggled += OnHouseAutoFillToggled;

        _challengeNoNotesCheck.Toggled += OnChallengeNoNotesToggled;
        _challengePerfectCheck.Toggled += OnChallengePerfectToggled;
        _challengeHintsOption.ItemSelected += OnChallengeHintsSelected;
        _challengeTimeOption.ItemSelected += OnChallengeTimeSelected;
        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;
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

    private void LoadSettings()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        var settings = saveService.Settings;

        _themeOption.Selected = settings.ThemeIndex;
        _deadlyCheck.ButtonPressed = settings.DeadlyModeEnabled;
        _hideCheck.ButtonPressed = settings.HideCompletedNumbers;
        _highlightCheck.ButtonPressed = settings.HighlightRelatedCells;

        _learnCheck.ButtonPressed = settings.LearnModeEnabled;
        _colorblindCheck.ButtonPressed = settings.ColorblindPaletteEnabled;

        _uiScaleSlider.Value = settings.UiScalePercent;
        _uiScaleValue.Text = $"{settings.UiScalePercent}%";

        _smartCleanupCheck.ButtonPressed = settings.SmartNoteCleanupEnabled;
        _houseAutoFillCheck.ButtonPressed = settings.HouseAutoFillEnabled;

        _challengeNoNotesCheck.ButtonPressed = settings.ChallengeNoNotes;
        _challengePerfectCheck.ButtonPressed = settings.ChallengePerfectRun;

        SelectOptionById(_challengeHintsOption, settings.ChallengeHintLimit);
        SelectOptionById(_challengeTimeOption, settings.ChallengeTimeAttackMinutes);
    }

    private void SaveSettings()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.SaveSettings();
    }

    private void OnThemeSelected(long index)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ThemeIndex = (int)index;
        SaveSettings();

        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.SetTheme((int)index);
    }

    private void OnLearnToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.LearnModeEnabled = pressed;
        SaveSettings();
    }

    private void OnColorblindToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ColorblindPaletteEnabled = pressed;
        SaveSettings();

        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.SetColorblindPalette(pressed);
    }

    private void OnUiScaleChanged(double value)
    {
        int pct = (int)Math.Round(value);
        _uiScaleValue.Text = $"{pct}%";

        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.UiScalePercent = pct;
        SaveSettings();

        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ApplyUiScale(pct);
    }

    private void OnSmartCleanupToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.SmartNoteCleanupEnabled = pressed;
        SaveSettings();
    }

    private void OnHouseAutoFillToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.HouseAutoFillEnabled = pressed;
        SaveSettings();
    }

    private void OnDeadlyToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.DeadlyModeEnabled = pressed;
        SaveSettings();
    }

    private void OnHideToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.HideCompletedNumbers = pressed;
        SaveSettings();
    }

    private void OnHighlightToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.HighlightRelatedCells = pressed;
        SaveSettings();
    }

    private void OnChallengeNoNotesToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ChallengeNoNotes = pressed;
        SaveSettings();
    }

    private void OnChallengePerfectToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ChallengePerfectRun = pressed;
        SaveSettings();
    }

    private void OnChallengeHintsSelected(long index)
    {
        int id = _challengeHintsOption.GetItemId((int)index);
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ChallengeHintLimit = id;
        SaveSettings();
    }

    private void OnChallengeTimeSelected(long index)
    {
        int id = _challengeTimeOption.GetItemId((int)index);
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ChallengeTimeAttackMinutes = id;
        SaveSettings();
    }

    private static void SelectOptionById(OptionButton option, int id)
    {
        for (int i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemId(i) == id)
            {
                option.Selected = i;
                return;
            }
        }
        option.Selected = 0;
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

        var panelStyle = theme.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);

        var settingsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer/SettingsContainer");
        ApplyThemeRecursive(settingsContainer, theme, colors);

        // Back Button
        _backButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);
    }

    private static void ApplyThemeRecursive(Node node, ThemeService theme, ThemeService.ThemeColors colors)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Label label)
            {
                if (label.Name.ToString().Contains("Title"))
                    label.AddThemeColorOverride("font_color", colors.Accent);
                else
                    label.AddThemeColorOverride("font_color", colors.TextPrimary);
            }
            else if (child is BaseButton button)
            {
                button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
                button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
                button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
                button.AddThemeStyleboxOverride("disabled", theme.CreateButtonStyleBox(disabled: true));
                button.AddThemeColorOverride("font_color", colors.TextPrimary);
                button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
            }

            if (child is Node n)
            {
                ApplyThemeRecursive(n, theme, colors);
            }
        }
    }
}
