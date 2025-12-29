namespace MySudoku.UI;

/// <summary>
/// Einstellungsmenü
/// </summary>
public partial class SettingsMenu : Control
{
    private PanelContainer _panel = null!;
    private Label _title = null!;
    
    // Storage path
    private LineEdit _storagePathEdit = null!;
    private Button _storagePathBrowse = null!;
    private Label _storagePathInfo = null!;
    private FileDialog? _fileDialog;
    
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

    // Technique configuration
    private VBoxContainer _techniquesContainer = null!;
    private Button _resetTechniquesButton = null!;
    private readonly Dictionary<Difficulty, Dictionary<string, CheckButton>> _techniqueCheckboxes = new();

    private Button _backButton = null!;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("Title");

        var settingsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer/SettingsContainer");
        
        // Storage path controls
        _storagePathEdit = settingsContainer.GetNode<LineEdit>("StoragePathRow/StoragePathEdit");
        _storagePathBrowse = settingsContainer.GetNode<Button>("StoragePathRow/StoragePathBrowse");
        _storagePathInfo = settingsContainer.GetNode<Label>("StoragePathInfo");
        
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

        _techniquesContainer = settingsContainer.GetNode<VBoxContainer>("TechniquesContainer");
        _resetTechniquesButton = settingsContainer.GetNode<Button>("ResetTechniquesRow/ResetTechniquesButton");

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

        // Techniken-UI erstellen
        CreateTechniqueUI();

        // Werte laden
        LoadSettings();

        // Events - Storage path
        _storagePathEdit.TextSubmitted += OnStoragePathSubmitted;
        _storagePathEdit.FocusExited += OnStoragePathFocusExited;
        _storagePathBrowse.Pressed += OnStoragePathBrowsePressed;
        
        // Events - Theme & Display
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

        _resetTechniquesButton.Pressed += OnResetTechniquesPressed;

        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;
    }

    private void CreateTechniqueUI()
    {
        var difficulties = new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        var difficultyNames = new Dictionary<Difficulty, string>
        {
            { Difficulty.Easy, "🟢 Leicht" },
            { Difficulty.Medium, "🟠 Mittel" },
            { Difficulty.Hard, "🔴 Schwer" }
        };
        var difficultyColors = new Dictionary<Difficulty, Color>
        {
            { Difficulty.Easy, new Color("4caf50") },
            { Difficulty.Medium, new Color("ff9800") },
            { Difficulty.Hard, new Color("f44336") }
        };

        foreach (var difficulty in difficulties)
        {
            // Container für diese Schwierigkeit
            var diffContainer = new VBoxContainer();
            diffContainer.Name = $"Diff{difficulty}Container";

            // Header mit Collapse-Button
            var headerContainer = new HBoxContainer();
            var headerButton = new Button();
            headerButton.Name = $"Header{difficulty}";
            headerButton.Text = $"▼ {difficultyNames[difficulty]}";
            headerButton.Flat = true;
            headerButton.Alignment = HorizontalAlignment.Left;
            headerButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            // Farbigen Akzent hinzufügen
            var accentStyle = new StyleBoxFlat();
            accentStyle.BgColor = difficultyColors[difficulty].Darkened(0.6f);
            accentStyle.CornerRadiusTopLeft = 4;
            accentStyle.CornerRadiusTopRight = 4;
            accentStyle.ContentMarginLeft = 8;
            accentStyle.ContentMarginRight = 8;
            accentStyle.ContentMarginTop = 4;
            accentStyle.ContentMarginBottom = 4;
            headerButton.AddThemeStyleboxOverride("normal", accentStyle);

            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = difficultyColors[difficulty].Darkened(0.4f);
            hoverStyle.CornerRadiusTopLeft = 4;
            hoverStyle.CornerRadiusTopRight = 4;
            hoverStyle.ContentMarginLeft = 8;
            hoverStyle.ContentMarginRight = 8;
            hoverStyle.ContentMarginTop = 4;
            hoverStyle.ContentMarginBottom = 4;
            headerButton.AddThemeStyleboxOverride("hover", hoverStyle);

            headerContainer.AddChild(headerButton);
            diffContainer.AddChild(headerContainer);

            // Checkboxen-Container
            var checkboxContainer = new VBoxContainer();
            checkboxContainer.Name = $"Checkboxes{difficulty}";

            // Margin für Einrückung
            var marginContainer = new MarginContainer();
            marginContainer.AddThemeConstantOverride("margin_left", 24);
            marginContainer.AddChild(checkboxContainer);
            diffContainer.AddChild(marginContainer);

            // Collapse toggle
            headerButton.Pressed += () =>
            {
                marginContainer.Visible = !marginContainer.Visible;
                headerButton.Text = marginContainer.Visible
                    ? $"▼ {difficultyNames[difficulty]}"
                    : $"▶ {difficultyNames[difficulty]}";
            };

            // Checkbox-Dictionary für diese Schwierigkeit
            _techniqueCheckboxes[difficulty] = new Dictionary<string, CheckButton>();

            // Techniken für diese Schwierigkeit erstellen
            foreach (var techId in TechniqueInfo.AllTechniqueIds)
            {
                if (!TechniqueInfo.Techniques.TryGetValue(techId, out var tech))
                    continue;

                // Zeige Techniken die für diese Schwierigkeit oder niedriger sind
                bool isRelevant = TechniqueInfo.DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>()).Contains(techId);
                // Oder Techniken die potenziell aktiviert werden könnten
                bool isPossible = tech.DifficultyLevel <= (int)difficulty + 1;

                if (!isPossible) continue;

                var checkRow = new HBoxContainer();

                var checkButton = new CheckButton();
                checkButton.Name = $"Tech{difficulty}{techId}";
                checkButton.Text = tech.Name;
                checkButton.TooltipText = $"{tech.Description}\n\nSchwierigkeit: {GetLevelName(tech.DifficultyLevel)}";
                checkButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;

                // Farbige Markierung je nach Technik-Level
                if (tech.DifficultyLevel == 1)
                    checkButton.Modulate = new Color("8bc34a"); // Grün
                else if (tech.DifficultyLevel == 2)
                    checkButton.Modulate = new Color("ffb74d"); // Orange
                else
                    checkButton.Modulate = new Color("ef5350"); // Rot

                checkRow.AddChild(checkButton);
                checkboxContainer.AddChild(checkRow);

                _techniqueCheckboxes[difficulty][techId] = checkButton;

                // Event
                var capturedDifficulty = difficulty;
                var capturedTechId = techId;
                checkButton.Toggled += (pressed) => OnTechniqueToggled(capturedDifficulty, capturedTechId, pressed);
            }

            _techniquesContainer.AddChild(diffContainer);
        }
    }

    private static string GetLevelName(int level)
    {
        return level switch
        {
            1 => "Leicht",
            2 => "Mittel",
            3 => "Schwer",
            _ => "Unbekannt"
        };
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

        // Storage path
        _storagePathEdit.Text = settings.CustomStoragePath;
        UpdateStoragePathInfo();

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

        // Technik-Checkboxen laden
        LoadTechniqueSettings();
    }

    private void UpdateStoragePathInfo()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        string resolvedPath = saveService.GetResolvedStoragePath();
        _storagePathInfo.Text = $"📂 {resolvedPath}";
    }

    private void LoadTechniqueSettings()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        var settings = saveService.Settings;

        foreach (var difficulty in _techniqueCheckboxes.Keys)
        {
            var enabledTechniques = settings.GetTechniquesForDifficulty(difficulty)
                ?? TechniqueInfo.DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>());

            foreach (var (techId, checkbox) in _techniqueCheckboxes[difficulty])
            {
                checkbox.SetPressedNoSignal(enabledTechniques.Contains(techId));
            }
        }
    }

    private void SaveSettings()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.SaveSettings();
    }

    private void OnTechniqueToggled(Difficulty difficulty, string techId, bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        var settings = saveService.Settings;

        // Aktuelle Techniken für diese Schwierigkeit holen (oder Standard)
        var currentTechniques = settings.GetTechniquesForDifficulty(difficulty)
            ?? new HashSet<string>(TechniqueInfo.DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>()));

        if (pressed)
            currentTechniques.Add(techId);
        else
            currentTechniques.Remove(techId);

        settings.SetTechniquesForDifficulty(difficulty, currentTechniques);
        SaveSettings();
    }

    private void OnResetTechniquesPressed()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ResetTechniquesToDefault();
        SaveSettings();
        LoadTechniqueSettings();
    }

    #region Storage Path

    private void OnStoragePathSubmitted(string newText)
    {
        ApplyStoragePath(newText);
    }

    private void OnStoragePathFocusExited()
    {
        ApplyStoragePath(_storagePathEdit.Text);
    }

    private void ApplyStoragePath(string path)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        string cleanPath = path.Trim();
        
        // Validate path if not empty
        if (!string.IsNullOrEmpty(cleanPath))
        {
            // Normalize path separators
            cleanPath = cleanPath.Replace('\\', '/');
            
            // Check if path is valid and accessible
            if (!DirAccess.DirExistsAbsolute(cleanPath))
            {
                var err = DirAccess.MakeDirRecursiveAbsolute(cleanPath);
                if (err != Error.Ok)
                {
                    // Show error - path is invalid
                    _storagePathInfo.Text = "❌ Ungültiger Pfad - konnte nicht erstellt werden";
                    _storagePathEdit.Text = saveService.Settings.CustomStoragePath;
                    return;
                }
            }
        }

        saveService.Settings.CustomStoragePath = cleanPath;
        SaveSettings();
        
        // Reload data from new location
        saveService.LoadAll();
        UpdateStoragePathInfo();
    }

    private void OnStoragePathBrowsePressed()
    {
        if (_fileDialog == null)
        {
            _fileDialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.OpenDir,
                Access = FileDialog.AccessEnum.Filesystem,
                Title = "Speicherort auswählen",
                Size = new Vector2I(600, 400)
            };
            _fileDialog.DirSelected += OnStoragePathSelected;
            AddChild(_fileDialog);
        }

        // Set initial directory
        var saveService = GetNode<SaveService>("/root/SaveService");
        string currentPath = saveService.GetResolvedStoragePath();
        if (DirAccess.DirExistsAbsolute(currentPath))
        {
            _fileDialog.CurrentDir = currentPath;
        }

        _fileDialog.PopupCentered();
    }

    private void OnStoragePathSelected(string dir)
    {
        _storagePathEdit.Text = dir;
        ApplyStoragePath(dir);
    }

    #endregion

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
                // Überspringe Buttons mit benutzerdefinierten Styles (Technik-Header)
                if (!button.Name.ToString().StartsWith("Header"))
                {
                    button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
                    button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
                    button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
                    button.AddThemeStyleboxOverride("disabled", theme.CreateButtonStyleBox(disabled: true));
                }
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
