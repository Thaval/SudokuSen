namespace SudokuSen.UI;

/// <summary>
/// Einstellungsmenü
/// </summary>
public partial class SettingsMenu : Control
{
    // Cached Service References
    private ThemeService _themeService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;
    private AudioService _audioService = null!;
    private LocalizationService _localizationService = null!;

    // Flag to prevent event cascading during LoadSettings
    private bool _isLoadingSettings = false;

    private PanelContainer _panel = null!;
    private Label _title = null!;

    // Storage path
    private LineEdit _storagePathEdit = null!;
    private Button _storagePathBrowse = null!;
    private Label _storagePathInfo = null!;
    private FileDialog? _fileDialog;

    private OptionButton _languageOption = null!;
    private OptionButton _themeOption = null!;
    private CheckButton _deadlyCheck = null!;
    private CheckButton _hideCheck = null!;
    private CheckButton _highlightCheck = null!;

    private CheckButton _learnCheck = null!;
    private CheckButton _colorblindCheck = null!;
    private HSlider _uiScaleSlider = null!;
    private Label _uiScaleValue = null!;

    // Puzzles
    private OptionButton _puzzlesModeOption = null!;

    // Audio
    private CheckButton _sfxCheck = null!;
    private HSlider _sfxVolumeSlider = null!;
    private Label _sfxVolumeValue = null!;
    private CheckButton _musicCheck = null!;
    private HSlider _musicVolumeSlider = null!;
    private Label _musicVolumeValue = null!;
    private OptionButton _menuMusicOption = null!;
    private OptionButton _gameMusicOption = null!;

    private CheckButton _smartCleanupCheck = null!;
    private CheckButton _houseAutoFillCheck = null!;

    private OptionButton _challengeDifficultyOption = null!;
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
        // Cache service references
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");

        _saveService.EnsureLoaded();

        UiNavigationSfx.Wire(this, _audioService);

        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("Title");

        var settingsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer/SettingsContainer");

        // Storage path controls
        _storagePathEdit = settingsContainer.GetNode<LineEdit>("StoragePathRow/StoragePathEdit");
        _storagePathBrowse = settingsContainer.GetNode<Button>("StoragePathRow/StoragePathBrowse");
        _storagePathInfo = settingsContainer.GetNode<Label>("StoragePathInfo");

        _languageOption = settingsContainer.GetNode<OptionButton>("LanguageRow/LanguageOption");
        _themeOption = settingsContainer.GetNode<OptionButton>("ThemeRow/ThemeOption");
        _deadlyCheck = settingsContainer.GetNode<CheckButton>("DeadlyRow/DeadlyCheck");
        _hideCheck = settingsContainer.GetNode<CheckButton>("HideRow/HideCheck");
        _highlightCheck = settingsContainer.GetNode<CheckButton>("HighlightRow/HighlightCheck");

        _learnCheck = settingsContainer.GetNode<CheckButton>("LearnRow/LearnCheck");
        _colorblindCheck = settingsContainer.GetNode<CheckButton>("ColorblindRow/ColorblindCheck");
        _uiScaleSlider = settingsContainer.GetNode<HSlider>("UiScaleRow/UiScaleSlider");
        _uiScaleValue = settingsContainer.GetNode<Label>("UiScaleRow/UiScaleValue");

        // Puzzles
        _puzzlesModeOption = settingsContainer.GetNode<OptionButton>("PuzzlesModeRow/PuzzlesModeOption");

        // Audio
        _sfxCheck = settingsContainer.GetNode<CheckButton>("SfxRow/SfxCheck");
        _sfxVolumeSlider = settingsContainer.GetNode<HSlider>("SfxVolumeRow/SfxVolumeSlider");
        _sfxVolumeValue = settingsContainer.GetNode<Label>("SfxVolumeRow/SfxVolumeValue");
        _musicCheck = settingsContainer.GetNode<CheckButton>("MusicRow/MusicCheck");
        _musicVolumeSlider = settingsContainer.GetNode<HSlider>("MusicVolumeRow/MusicVolumeSlider");
        _musicVolumeValue = settingsContainer.GetNode<Label>("MusicVolumeRow/MusicVolumeValue");

        // Music track selection - create dynamically after MusicVolumeRow
        CreateMusicTrackOptions(settingsContainer);

        _smartCleanupCheck = settingsContainer.GetNode<CheckButton>("SmartCleanupRow/SmartCleanupCheck");
        _houseAutoFillCheck = settingsContainer.GetNode<CheckButton>("HouseAutoFillRow/HouseAutoFillCheck");

        _challengeDifficultyOption = settingsContainer.GetNode<OptionButton>("ChallengeDifficultyRow/ChallengeDifficultyOption");
        _challengeNoNotesCheck = settingsContainer.GetNode<CheckButton>("ChallengeNoNotesRow/ChallengeNoNotesCheck");
        _challengePerfectCheck = settingsContainer.GetNode<CheckButton>("ChallengePerfectRow/ChallengePerfectCheck");
        _challengeHintsOption = settingsContainer.GetNode<OptionButton>("ChallengeHintsRow/ChallengeHintsOption");
        _challengeTimeOption = settingsContainer.GetNode<OptionButton>("ChallengeTimeRow/ChallengeTimeOption");

        _techniquesContainer = settingsContainer.GetNode<VBoxContainer>("TechniquesContainer");
        _resetTechniquesButton = settingsContainer.GetNode<Button>("ResetTechniquesRow/ResetTechniquesButton");

        _backButton = GetNode<Button>("BackButton");

        // Techniken-UI erstellen
        CreateTechniqueUI();

        // Configure UI scale slider with dynamic bounds
        ConfigureUiScaleSlider();

        // Wire back button EARLY to ensure it's always functional
        _backButton.Pressed += OnBackPressed;
        _backButton.Disabled = false;
        _backButton.MouseFilter = MouseFilterEnum.Stop;
        // Apply theme to back button immediately
        var colors = _themeService.CurrentColors;
        _backButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);

        // Initial localization (populates option buttons and labels) before loading settings
        GD.Print("[UI] SettingsMenu: About to ApplyLocalization...");
        try
        {
            ApplyLocalization();
            GD.Print("[UI] SettingsMenu: ApplyLocalization done");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[UI] SettingsMenu: ApplyLocalization FAILED: {ex.Message}\n{ex.StackTrace}");
        }

        // Werte laden
        GD.Print("[UI] SettingsMenu: About to LoadSettings...");
        try
        {
            LoadSettings();
            GD.Print("[UI] SettingsMenu: LoadSettings done");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[UI] SettingsMenu: LoadSettings FAILED: {ex.Message}\n{ex.StackTrace}");
        }

        UpdateAudioUi();

        GD.Print("[UI] SettingsMenu: Ready complete (events wired, settings loaded)");

        // Events - Storage path
        _storagePathEdit.TextSubmitted += OnStoragePathSubmitted;
        _storagePathEdit.FocusExited += OnStoragePathFocusExited;
        _storagePathBrowse.Pressed += OnStoragePathBrowsePressed;

        // Events - Language
        _languageOption.ItemSelected += OnLanguageSelected;

        // Events - Theme & Display
        _themeOption.ItemSelected += OnThemeSelected;
        _deadlyCheck.Toggled += OnDeadlyToggled;
        _hideCheck.Toggled += OnHideToggled;
        _highlightCheck.Toggled += OnHighlightToggled;

        _learnCheck.Toggled += OnLearnToggled;
        _colorblindCheck.Toggled += OnColorblindToggled;
        _uiScaleSlider.ValueChanged += OnUiScaleChanged;

        // Events - Puzzles
        _puzzlesModeOption.ItemSelected += OnPuzzlesModeSelected;

        // Events - Audio
        _sfxCheck.Toggled += OnSfxToggled;
        _sfxVolumeSlider.ValueChanged += OnSfxVolumeChanged;
        _musicCheck.Toggled += OnMusicToggled;
        _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
        _menuMusicOption.ItemSelected += OnMenuMusicSelected;
        _gameMusicOption.ItemSelected += OnGameMusicSelected;

        _smartCleanupCheck.Toggled += OnSmartCleanupToggled;
        _houseAutoFillCheck.Toggled += OnHouseAutoFillToggled;

        _challengeDifficultyOption.ItemSelected += OnChallengeDifficultySelected;
        _challengeNoNotesCheck.Toggled += OnChallengeNoNotesToggled;
        _challengePerfectCheck.Toggled += OnChallengePerfectToggled;
        _challengeHintsOption.ItemSelected += OnChallengeHintsSelected;
        _challengeTimeOption.ItemSelected += OnChallengeTimeSelected;

        _resetTechniquesButton.Pressed += OnResetTechniquesPressed;

        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    private void CreateMusicTrackOptions(VBoxContainer settingsContainer)
    {
        // Find the index of MusicVolumeRow to insert after it
        var musicVolumeRow = settingsContainer.GetNode<HBoxContainer>("MusicVolumeRow");
        int insertIndex = musicVolumeRow.GetIndex() + 1;

        // Menu Music Row
        var menuMusicRow = new HBoxContainer();
        menuMusicRow.Name = "MenuMusicRow";
        var menuMusicLabel = new Label();
        menuMusicLabel.Name = "MenuMusicLabel";
        menuMusicLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        menuMusicRow.AddChild(menuMusicLabel);

        _menuMusicOption = new OptionButton();
        _menuMusicOption.CustomMinimumSize = new Vector2(150, 0);
        for (int i = 0; i < AudioService.MusicTrackNames.Length; i++)
        {
            _menuMusicOption.AddItem(AudioService.MusicTrackNames[i], i);
        }
        menuMusicRow.AddChild(_menuMusicOption);
        settingsContainer.AddChild(menuMusicRow);
        settingsContainer.MoveChild(menuMusicRow, insertIndex);

        // Game Music Row
        var gameMusicRow = new HBoxContainer();
        gameMusicRow.Name = "GameMusicRow";
        var gameMusicLabel = new Label();
        gameMusicLabel.Name = "GameMusicLabel";
        gameMusicLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        gameMusicRow.AddChild(gameMusicLabel);

        _gameMusicOption = new OptionButton();
        _gameMusicOption.CustomMinimumSize = new Vector2(150, 0);
        for (int i = 0; i < AudioService.MusicTrackNames.Length; i++)
        {
            _gameMusicOption.AddItem(AudioService.MusicTrackNames[i], i);
        }
        gameMusicRow.AddChild(_gameMusicOption);
        settingsContainer.AddChild(gameMusicRow);
        settingsContainer.MoveChild(gameMusicRow, insertIndex + 1);
    }

    private void CreateTechniqueUI()
    {
        var difficulties = new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Insane };
        var difficultyColors = new Dictionary<Difficulty, Color>
        {
            { Difficulty.Easy, new Color("4caf50") },
            { Difficulty.Medium, new Color("ff9800") },
            { Difficulty.Hard, new Color("f44336") },
            { Difficulty.Insane, new Color("9c27b0") }
        };

        foreach (var difficulty in difficulties)
        {
            var defaultTechniques = TechniqueInfo.GetDefaultTechniques(difficulty);

            // Container für diese Schwierigkeit
            var diffContainer = new VBoxContainer();
            diffContainer.Name = $"Diff{difficulty}Container";

            // Header mit Collapse-Button
            var headerContainer = new HBoxContainer();
            var headerButton = new Button();
            headerButton.Name = $"Header{difficulty}";
            headerButton.Text = $"▼ {_localizationService.GetDifficultyDisplay(difficulty)}";
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
                string arrow = marginContainer.Visible ? "▼" : "▶";
                headerButton.Text = $"{arrow} {_localizationService.GetDifficultyDisplay(difficulty)}";
            };

            // Checkbox-Dictionary für diese Schwierigkeit
            _techniqueCheckboxes[difficulty] = new Dictionary<string, CheckButton>();

            // Techniken für diese Schwierigkeit erstellen
            foreach (var techId in TechniqueInfo.AllTechniqueIds)
            {
                if (!TechniqueInfo.Techniques.TryGetValue(techId, out var tech))
                    continue;

                // Zeige Techniken die für diese Schwierigkeit oder niedriger sind
                bool isRelevant = defaultTechniques.Contains(techId);
                // Oder Techniken die potenziell aktiviert werden könnten
                bool isPossible = tech.DifficultyLevel <= (int)difficulty + 1;

                if (!isPossible) continue;

                var checkRow = new HBoxContainer();

                var checkButton = new CheckButton();
                checkButton.Name = $"Tech{difficulty}{techId}";
                checkButton.Text = _localizationService.GetTechniqueName(techId);
                checkButton.TooltipText = _localizationService.Get(
                    "settings.technique.tooltip",
                    _localizationService.GetTechniqueDescription(techId),
                    GetLevelName(tech.DifficultyLevel)
                );
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

    private string GetLevelName(int level)
    {
        return level switch
        {
            1 => _localizationService.Get("settings.technique.level.easy"),
            2 => _localizationService.Get("settings.technique.level.medium"),
            3 => _localizationService.Get("settings.technique.level.hard"),
            _ => _localizationService.Get("common.unknown")
        };
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

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        GD.Print($"[UI] SettingsMenu.LoadSettings: Starting - Settings.LanguageIndex={_saveService.Settings.LanguageIndex}, ThemeIndex={_saveService.Settings.ThemeIndex}");

        var settings = _saveService.Settings;

        // Storage path
        _storagePathEdit.Text = settings.CustomStoragePath;
        UpdateStoragePathInfo();

        // Language
        _languageOption.Selected = settings.LanguageIndex;

        _themeOption.Selected = settings.ThemeIndex;
        _deadlyCheck.ButtonPressed = settings.DeadlyModeEnabled;
        _hideCheck.ButtonPressed = settings.HideCompletedNumbers;
        _highlightCheck.ButtonPressed = settings.HighlightRelatedCells;

        _learnCheck.ButtonPressed = settings.LearnModeEnabled;
        _colorblindCheck.ButtonPressed = settings.ColorblindPaletteEnabled;

        _uiScaleSlider.Value = settings.UiScalePercent;
        _uiScaleValue.Text = $"{settings.UiScalePercent}%";

        // Puzzles
        SelectOptionById(_puzzlesModeOption, (int)settings.PuzzlesMode);

        // Audio
        _sfxCheck.ButtonPressed = settings.SoundEnabled;
        _sfxVolumeSlider.Value = settings.Volume;
        _sfxVolumeValue.Text = $"{settings.Volume}%";
        _musicCheck.ButtonPressed = settings.MusicEnabled;
        _musicVolumeSlider.Value = settings.MusicVolume;
        _musicVolumeValue.Text = $"{settings.MusicVolume}%";
        SelectOptionById(_menuMusicOption, settings.MenuMusicTrack);
        SelectOptionById(_gameMusicOption, settings.GameMusicTrack);

        _smartCleanupCheck.ButtonPressed = settings.SmartNoteCleanupEnabled;
        _houseAutoFillCheck.ButtonPressed = settings.HouseAutoFillEnabled;

        SelectOptionById(_challengeDifficultyOption, settings.ChallengeDifficulty);
        _challengeNoNotesCheck.ButtonPressed = settings.ChallengeNoNotes;
        _challengePerfectCheck.ButtonPressed = settings.ChallengePerfectRun;

        SelectOptionById(_challengeHintsOption, settings.ChallengeHintLimit);
        SelectOptionById(_challengeTimeOption, settings.ChallengeTimeAttackMinutes);

        // Technik-Checkboxen laden
        LoadTechniqueSettings();

        GD.Print($"[UI] SettingsMenu: loaded | theme={settings.ThemeIndex}, colorblind={settings.ColorblindPaletteEnabled}, sfx={settings.SoundEnabled}({settings.Volume}%), music={settings.MusicEnabled}({settings.MusicVolume}%), menuTrack={settings.MenuMusicTrack}, gameTrack={settings.GameMusicTrack}, uiScale={settings.UiScalePercent}%");

        _isLoadingSettings = false;

        GD.Print($"[UI] SettingsMenu: post-load | langItems={_languageOption.ItemCount}, themeItems={_themeOption.ItemCount}, langSelected={_languageOption.Selected}, themeSelected={_themeOption.Selected}");

        UpdateAudioUi();
    }

    private void UpdateAudioUi()
    {
        bool sfxOn = _sfxCheck.ButtonPressed;
        _sfxVolumeSlider.Editable = sfxOn;

        bool musicOn = _musicCheck.ButtonPressed;
        _musicVolumeSlider.Editable = musicOn;
        _menuMusicOption.Disabled = !musicOn;
        _gameMusicOption.Disabled = !musicOn;
    }

    private void UpdateStoragePathInfo()
    {
        string resolvedPath = _saveService.GetResolvedStoragePath();
        _storagePathInfo.Text = _localizationService.Get("settings.storage.info", resolvedPath);
    }

    private void LoadTechniqueSettings()
    {

        var settings = _saveService.Settings;

        foreach (var difficulty in _techniqueCheckboxes.Keys)
        {
            var enabledTechniques = TechniqueInfo.GetConfiguredTechniques(settings, difficulty);

            foreach (var (techId, checkbox) in _techniqueCheckboxes[difficulty])
            {
                checkbox.SetPressedNoSignal(enabledTechniques.Contains(techId));
            }
        }
    }

    private void SaveSettings()
    {
        GD.Print("[UI] SettingsMenu: SaveSettings()" );
        _saveService.SaveSettings();
    }

    private void OnTechniqueToggled(Difficulty difficulty, string techId, bool pressed)
    {

        var settings = _saveService.Settings;

        // Aktuelle Techniken für diese Schwierigkeit holen (oder Standard)
        var currentTechniques = TechniqueInfo.GetConfiguredTechniques(settings, difficulty);

        if (pressed)
            currentTechniques.Add(techId);
        else
            currentTechniques.Remove(techId);

        settings.SetTechniquesForDifficulty(difficulty, currentTechniques);
        SaveSettings();
    }

    private void OnResetTechniquesPressed()
    {

        _saveService.Settings.ResetTechniquesToDefault();
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
        GD.Print($"[UI] SettingsMenu: ApplyStoragePath path='{path}'");

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
                    _storagePathInfo.Text = _localizationService.Get("settings.storage.invalid");
                    _storagePathEdit.Text = _saveService.Settings.CustomStoragePath;
                    return;
                }
            }
        }

        _saveService.Settings.CustomStoragePath = cleanPath;
        GD.Print($"[UI] SettingsMenu: Storage path set to '{cleanPath}', saving + reloading");
        SaveSettings();

        // Reload data from new location
        _saveService.LoadAll();
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
                Title = _localizationService.Get("settings.storage.select_title"),
                Size = new Vector2I(600, 400)
            };
            _fileDialog.DirSelected += OnStoragePathSelected;
            AddChild(_fileDialog);
        }

        // Set initial directory

        string currentPath = _saveService.GetResolvedStoragePath();
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

    private void OnLanguageSelected(long index)
    {
        GD.Print($"[UI] SettingsMenu: Language selected = {index}, selectedIdx={_languageOption.Selected}, items={_languageOption.ItemCount}, _isLoadingSettings={_isLoadingSettings}");
        if (_isLoadingSettings) return; // Don't save during loading

        _audioService.PlayClick();
        GD.Print($"[UI] SettingsMenu: Before SetLanguage - Settings.LanguageIndex={_saveService.Settings.LanguageIndex}");
        _localizationService.SetLanguage((int)index);
        GD.Print($"[UI] SettingsMenu: After SetLanguage - Settings.LanguageIndex={_saveService.Settings.LanguageIndex}");
        // Refresh immediately so the settings screen updates in-place
        ApplyLocalization();
    }

    private void OnThemeSelected(long index)
    {
        GD.Print($"[UI] SettingsMenu: Theme selected = {index}, selectedIdx={_themeOption.Selected}, items={_themeOption.ItemCount}, _isLoadingSettings={_isLoadingSettings}");
        if (_isLoadingSettings) return; // Don't save during loading

        _audioService.PlayClick();
        GD.Print($"[UI] SettingsMenu: Before save - Settings.ThemeIndex={_saveService.Settings.ThemeIndex}");
        _saveService.Settings.ThemeIndex = (int)index;
        GD.Print($"[UI] SettingsMenu: After assignment - Settings.ThemeIndex={_saveService.Settings.ThemeIndex}");
        SaveSettings();
        GD.Print($"[UI] SettingsMenu: After save - Settings.ThemeIndex={_saveService.Settings.ThemeIndex}");

        _themeService.SetTheme((int)index);
    }

    private void OnLanguageChanged(int languageIndex)
    {
        GD.Print($"[UI] SettingsMenu: LanguageChanged signal received, applying localization (langIndex={languageIndex})");
        ApplyLocalization();
    }

    private void OnLearnToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: LearnMode toggled = {pressed}");
        _saveService.Settings.LearnModeEnabled = pressed;
        SaveSettings();
    }

    private void OnColorblindToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: Farbblind-Palette toggled = {pressed}");
        _saveService.Settings.ColorblindPaletteEnabled = pressed;
        SaveSettings();


        _themeService.SetColorblindPalette(pressed);
    }

    private void ConfigureUiScaleSlider()
    {
        var bounds = _themeService.GetUiScaleBounds();

        _uiScaleSlider.MinValue = bounds.Min;
        _uiScaleSlider.MaxValue = bounds.Max;
        _uiScaleSlider.Step = 5;

        // Localized tooltip explaining the scale range
        _uiScaleSlider.TooltipText = _localizationService.Get("settings.ui_scale.tooltip", bounds.Min, bounds.Max, bounds.Recommended);

        GD.Print($"[UI] SettingsMenu: UI scale bounds configured: {bounds.Min}%-{bounds.Max}% (recommended: {bounds.Recommended}%)");
    }

    private void OnUiScaleChanged(double value)
    {
        int pct = (int)Math.Round(value);

        // Get current bounds and clamp
        var bounds = _themeService.GetUiScaleBounds();
        pct = Math.Clamp(pct, bounds.Min, bounds.Max);

        _uiScaleValue.Text = $"{pct}%";

        GD.Print($"[UI] SettingsMenu: UI scale changed = {pct}%");


        _saveService.Settings.UiScalePercent = pct;
        SaveSettings();


        _themeService.ApplyUiScale(pct);
    }

    private void OnPuzzlesModeSelected(long index)
    {
        if (_isLoadingSettings) return;
        int modeId = _puzzlesModeOption.GetItemId((int)index);
        GD.Print($"[UI] SettingsMenu: Puzzles mode selected = {modeId}");
        _saveService.Settings.PuzzlesMode = (SettingsData.PuzzleSourceMode)modeId;
        SaveSettings();
    }

    private void OnSfxToggled(bool pressed)
    {
        if (_isLoadingSettings) return;
        GD.Print($"[UI] SettingsMenu: SFX enabled = {pressed}");
        _saveService.Settings.SoundEnabled = pressed;
        SaveSettings();
        _audioService.SoundEnabled = pressed;
        UpdateAudioUi();
    }

    private void OnSfxVolumeChanged(double value)
    {
        int pct = (int)Math.Round(value);
        _sfxVolumeValue.Text = $"{pct}%";

        GD.Print($"[UI] SettingsMenu: SFX volume = {pct}%");
        _saveService.Settings.Volume = pct;
        SaveSettings();
        _audioService.SfxVolume = pct / 100f;
        // Play a test sound
        _audioService.PlayClick();
    }

    private void OnMusicToggled(bool pressed)
    {
        if (_isLoadingSettings) return;
        GD.Print($"[UI] SettingsMenu: Music enabled = {pressed}");
        _saveService.Settings.MusicEnabled = pressed;
        SaveSettings();
        _audioService.MusicEnabled = pressed;
        UpdateAudioUi();
    }

    private void OnMusicVolumeChanged(double value)
    {
        int pct = (int)Math.Round(value);
        _musicVolumeValue.Text = $"{pct}%";

        GD.Print($"[UI] SettingsMenu: Music volume = {pct}%");
        _saveService.Settings.MusicVolume = pct;
        SaveSettings();
        _audioService.MusicVolume = pct / 100f;
    }

    private void OnMenuMusicSelected(long index)
    {
        if (!_isLoadingSettings) _audioService.PlayClick();
        int trackId = _menuMusicOption.GetItemId((int)index);
        string trackName = trackId < AudioService.MusicTrackNames.Length ? AudioService.MusicTrackNames[trackId] : $"Track {trackId}";
        GD.Print($"[UI] SettingsMenu: Menu music track = {trackId} ({trackName})");
        _saveService.Settings.MenuMusicTrack = trackId;
        SaveSettings();
        _audioService.MenuMusicTrack = trackId;
    }

    private void OnGameMusicSelected(long index)
    {
        if (!_isLoadingSettings) _audioService.PlayClick();
        int trackId = _gameMusicOption.GetItemId((int)index);
        string trackName = trackId < AudioService.MusicTrackNames.Length ? AudioService.MusicTrackNames[trackId] : $"Track {trackId}";
        GD.Print($"[UI] SettingsMenu: Game music track = {trackId} ({trackName})");
        _saveService.Settings.GameMusicTrack = trackId;
        SaveSettings();
        _audioService.GameMusicTrack = trackId;
    }

    private void OnSmartCleanupToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: Smart cleanup toggled = {pressed}");
        _saveService.Settings.SmartNoteCleanupEnabled = pressed;
        SaveSettings();
    }

    private void OnHouseAutoFillToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: House auto-fill toggled = {pressed}");
        _saveService.Settings.HouseAutoFillEnabled = pressed;
        SaveSettings();
    }

    private void OnDeadlyToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: Deadly mode toggled = {pressed}");
        _saveService.Settings.DeadlyModeEnabled = pressed;
        SaveSettings();
    }

    private void OnHideToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: Hide completed numbers toggled = {pressed}");
        _saveService.Settings.HideCompletedNumbers = pressed;
        SaveSettings();
    }

    private void OnHighlightToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: Highlight related cells toggled = {pressed}");
        _saveService.Settings.HighlightRelatedCells = pressed;
        SaveSettings();
    }

    private void OnChallengeDifficultySelected(long index)
    {
        int id = _challengeDifficultyOption.GetItemId((int)index);
        GD.Print($"[UI] SettingsMenu: Challenge difficulty = {id} (0=Auto, 1=Easy, 2=Medium, 3=Hard)");
        _saveService.Settings.ChallengeDifficulty = id;
        SaveSettings();
    }

    private void OnChallengeNoNotesToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: Challenge 'No notes' toggled = {pressed}");
        _saveService.Settings.ChallengeNoNotes = pressed;
        SaveSettings();
    }

    private void OnChallengePerfectToggled(bool pressed)
    {
        GD.Print($"[UI] SettingsMenu: Challenge 'Perfect run' toggled = {pressed}");
        _saveService.Settings.ChallengePerfectRun = pressed;
        SaveSettings();
    }

    private void OnChallengeHintsSelected(long index)
    {
        int id = _challengeHintsOption.GetItemId((int)index);

        GD.Print($"[UI] SettingsMenu: Challenge hint limit = {id}");

        _saveService.Settings.ChallengeHintLimit = id;
        SaveSettings();
    }

    private void OnChallengeTimeSelected(long index)
    {
        int id = _challengeTimeOption.GetItemId((int)index);

        GD.Print($"[UI] SettingsMenu: Challenge time attack minutes = {id}");

        _saveService.Settings.ChallengeTimeAttackMinutes = id;
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
        _audioService.PlayClick();
        _appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void ApplyLocalization()
    {
        var loc = _localizationService;
        var settings = _saveService.Settings;
        string basePath = "CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer/SettingsContainer";

        // Title & navigation
        _title.Text = loc.Get("settings.title");
        _backButton.Text = loc.Get("menu.back");
        _backButton.TooltipText = loc.Get("settings.back.tooltip");

        // Storage
        GetNode<Label>($"{basePath}/StorageTitle").Text = loc.Get("settings.storage.title");
        var storageRow = GetNode<HBoxContainer>($"{basePath}/StoragePathRow");
        storageRow.TooltipText = loc.Get("settings.storage.tooltip");
        GetNode<Label>($"{basePath}/StoragePathRow/StoragePathLabel").Text = loc.Get("settings.storage.path");
        _storagePathEdit.PlaceholderText = loc.Get("settings.storage.placeholder");
        _storagePathEdit.TooltipText = loc.Get("settings.storage.tooltip");
        _storagePathBrowse.TooltipText = loc.Get("settings.storage.browse.tooltip");
        _storagePathInfo.Text = loc.Get("settings.storage.info", _saveService.GetResolvedStoragePath());

        // Appearance
        GetNode<Label>($"{basePath}/AppearanceTitle").Text = loc.Get("settings.appearance.title");
        var languageRow = GetNode<HBoxContainer>($"{basePath}/LanguageRow");
        languageRow.TooltipText = loc.Get("settings.language.tooltip");
        GetNode<Label>($"{basePath}/LanguageRow/LanguageLabel").Text = loc.Get("settings.language.label");

        var themeRow = GetNode<HBoxContainer>($"{basePath}/ThemeRow");
        themeRow.TooltipText = loc.Get("settings.theme.tooltip");
        GetNode<Label>($"{basePath}/ThemeRow/ThemeLabel").Text = loc.Get("settings.theme");

        var deadlyRow = GetNode<HBoxContainer>($"{basePath}/DeadlyRow");
        deadlyRow.TooltipText = loc.Get("settings.deadly_mode.tooltip");
        GetNode<Label>($"{basePath}/DeadlyRow/DeadlyLabel").Text = loc.Get("settings.deadly_mode");

        var hideRow = GetNode<HBoxContainer>($"{basePath}/HideRow");
        hideRow.TooltipText = loc.Get("settings.hide_completed.tooltip");
        GetNode<Label>($"{basePath}/HideRow/HideLabel").Text = loc.Get("settings.hide_completed");

        var highlightRow = GetNode<HBoxContainer>($"{basePath}/HighlightRow");
        highlightRow.TooltipText = loc.Get("settings.highlight_cells.tooltip");
        GetNode<Label>($"{basePath}/HighlightRow/HighlightLabel").Text = loc.Get("settings.highlight_cells");

        var learnRow = GetNode<HBoxContainer>($"{basePath}/LearnRow");
        learnRow.TooltipText = loc.Get("settings.learn_mode.tooltip");
        GetNode<Label>($"{basePath}/LearnRow/LearnLabel").Text = loc.Get("settings.learn_mode");

        var colorblindRow = GetNode<HBoxContainer>($"{basePath}/ColorblindRow");
        colorblindRow.TooltipText = loc.Get("settings.colorblind.tooltip");
        GetNode<Label>($"{basePath}/ColorblindRow/ColorblindLabel").Text = loc.Get("settings.colorblind");

        var uiScaleRow = GetNode<HBoxContainer>($"{basePath}/UiScaleRow");
        var bounds = _themeService.GetUiScaleBounds();
        uiScaleRow.TooltipText = loc.Get("settings.ui_scale.tooltip", bounds.Min, bounds.Max, bounds.Recommended);
        GetNode<Label>($"{basePath}/UiScaleRow/UiScaleLabel").Text = loc.Get("settings.ui_scale.label");
        _uiScaleSlider.TooltipText = uiScaleRow.TooltipText;

        // Puzzles
        GetNode<Label>($"{basePath}/PuzzlesTitle").Text = loc.Get("settings.puzzles.title");
        var puzzlesModeRow = GetNode<HBoxContainer>($"{basePath}/PuzzlesModeRow");
        puzzlesModeRow.TooltipText = loc.Get("settings.puzzles.mode.tooltip");
        GetNode<Label>($"{basePath}/PuzzlesModeRow/PuzzlesModeLabel").Text = loc.Get("settings.puzzles.mode");

        // Audio
        GetNode<Label>($"{basePath}/AudioTitle").Text = loc.Get("settings.sound");

        var sfxRow = GetNode<HBoxContainer>($"{basePath}/SfxRow");
        sfxRow.TooltipText = loc.Get("settings.sound.effects.tooltip");
        GetNode<Label>($"{basePath}/SfxRow/SfxLabel").Text = loc.Get("settings.sound.effects");

        var sfxVolumeRow = GetNode<HBoxContainer>($"{basePath}/SfxVolumeRow");
        sfxVolumeRow.TooltipText = loc.Get("settings.sound.volume.tooltip");
        GetNode<Label>($"{basePath}/SfxVolumeRow/SfxVolumeLabel").Text = loc.Get("settings.sound.volume");

        var musicRow = GetNode<HBoxContainer>($"{basePath}/MusicRow");
        musicRow.TooltipText = loc.Get("settings.music.tooltip");
        GetNode<Label>($"{basePath}/MusicRow/MusicLabel").Text = loc.Get("settings.music");

        var musicVolumeRow = GetNode<HBoxContainer>($"{basePath}/MusicVolumeRow");
        musicVolumeRow.TooltipText = loc.Get("settings.music.volume.tooltip");
        GetNode<Label>($"{basePath}/MusicVolumeRow/MusicVolumeLabel").Text = loc.Get("settings.music.volume");

        GetNode<Label>($"{basePath}/MenuMusicRow/MenuMusicLabel").Text = loc.Get("settings.music.menu");
        GetNode<Label>($"{basePath}/GameMusicRow/GameMusicLabel").Text = loc.Get("settings.music.game");

        // Notes assistant
        GetNode<Label>($"{basePath}/NotesAssistTitle").Text = loc.Get("settings.notes.title");

        var smartRow = GetNode<HBoxContainer>($"{basePath}/SmartCleanupRow");
        smartRow.TooltipText = loc.Get("settings.notes.smart_cleanup.tooltip");
        GetNode<Label>($"{basePath}/SmartCleanupRow/SmartCleanupLabel").Text = loc.Get("settings.notes.smart_cleanup");

        var houseRow = GetNode<HBoxContainer>($"{basePath}/HouseAutoFillRow");
        houseRow.TooltipText = loc.Get("settings.notes.house_autofill.tooltip");
        GetNode<Label>($"{basePath}/HouseAutoFillRow/HouseAutoFillLabel").Text = loc.Get("settings.notes.house_autofill");

        // Challenge settings
        GetNode<Label>($"{basePath}/ChallengeTitle").Text = loc.Get("settings.challenge.title");

        var challengeDiffRow = GetNode<HBoxContainer>($"{basePath}/ChallengeDifficultyRow");
        challengeDiffRow.TooltipText = loc.Get("settings.challenge.difficulty.tooltip");
        GetNode<Label>($"{basePath}/ChallengeDifficultyRow/ChallengeDifficultyLabel").Text = loc.Get("settings.challenge.difficulty");

        var challengeNoNotesRow = GetNode<HBoxContainer>($"{basePath}/ChallengeNoNotesRow");
        challengeNoNotesRow.TooltipText = loc.Get("settings.challenge.no_notes.tooltip");
        GetNode<Label>($"{basePath}/ChallengeNoNotesRow/ChallengeNoNotesLabel").Text = loc.Get("settings.challenge.no_notes");

        var challengePerfectRow = GetNode<HBoxContainer>($"{basePath}/ChallengePerfectRow");
        challengePerfectRow.TooltipText = loc.Get("settings.challenge.perfect.tooltip");
        GetNode<Label>($"{basePath}/ChallengePerfectRow/ChallengePerfectLabel").Text = loc.Get("settings.challenge.perfect");

        var challengeHintsRow = GetNode<HBoxContainer>($"{basePath}/ChallengeHintsRow");
        challengeHintsRow.TooltipText = loc.Get("settings.challenge.hints.tooltip");
        GetNode<Label>($"{basePath}/ChallengeHintsRow/ChallengeHintsLabel").Text = loc.Get("settings.challenge.hints");

        var challengeTimeRow = GetNode<HBoxContainer>($"{basePath}/ChallengeTimeRow");
        challengeTimeRow.TooltipText = loc.Get("settings.challenge.time.tooltip");
        GetNode<Label>($"{basePath}/ChallengeTimeRow/ChallengeTimeLabel").Text = loc.Get("settings.challenge.time");

        // Techniques
        GetNode<Label>($"{basePath}/TechniquesTitle").Text = loc.Get("settings.techniques.title");
        GetNode<Label>($"{basePath}/TechniquesDescription").Text = loc.Get("settings.techniques.description");
        _resetTechniquesButton.Text = loc.Get("settings.techniques.reset");
        _resetTechniquesButton.TooltipText = loc.Get("settings.techniques.reset.tooltip");

        // Option buttons - use loading flag to prevent event cascades
        bool wasLoading = _isLoadingSettings;
        _isLoadingSettings = true;
        PopulateOptionButtons(settings);
        _isLoadingSettings = wasLoading;

        // Technique headers
        UpdateTechniqueHeaders();
    }

    private void PopulateOptionButtons(SettingsData settings)
    {
        var languageItems = new List<(string text, int id)>
        {
            (_localizationService.Get("settings.language.german"), (int)Language.German),
            (_localizationService.Get("settings.language.english"), (int)Language.English)
        };
        PopulateOptionButton(_languageOption, languageItems, settings.LanguageIndex);

        var themeItems = new List<(string text, int id)>
        {
            (_localizationService.Get("settings.theme.light"), 0),
            (_localizationService.Get("settings.theme.dark"), 1)
        };
        PopulateOptionButton(_themeOption, themeItems, settings.ThemeIndex);

        // Puzzles mode
        var puzzlesModeItems = new List<(string text, int id)>
        {
            (_localizationService.Get("settings.puzzles.mode.both"), 0),
            (_localizationService.Get("settings.puzzles.mode.prebuilt"), 1),
            (_localizationService.Get("settings.puzzles.mode.dynamic"), 2)
        };
        PopulateOptionButton(_puzzlesModeOption, puzzlesModeItems, (int)settings.PuzzlesMode);

        var challengeDifficultyItems = new List<(string text, int id)>
        {
            (_localizationService.Get("settings.challenge.auto"), 0),
            (_localizationService.Get("settings.challenge.easy"), 1),
            (_localizationService.Get("settings.challenge.medium"), 2),
            (_localizationService.Get("settings.challenge.hard"), 3)
        };
        PopulateOptionButton(_challengeDifficultyOption, challengeDifficultyItems, settings.ChallengeDifficulty);

        var hintsItems = new List<(string text, int id)>
        {
            (_localizationService.Get("settings.common.off"), 0),
            ("3", 3),
            ("5", 5),
            ("10", 10)
        };
        PopulateOptionButton(_challengeHintsOption, hintsItems, settings.ChallengeHintLimit);

        var timeItems = new List<(string text, int id)>
        {
            (_localizationService.Get("settings.common.off"), 0),
            (_localizationService.Get("settings.time.minutes", 10), 10),
            (_localizationService.Get("settings.time.minutes", 15), 15),
            (_localizationService.Get("settings.time.minutes", 20), 20)
        };
        PopulateOptionButton(_challengeTimeOption, timeItems, settings.ChallengeTimeAttackMinutes);
    }

    private static void PopulateOptionButton(OptionButton option, IEnumerable<(string text, int id)> items, int selectedId)
    {
        option.Clear();
        int selectedIndex = 0;
        int idx = 0;
        foreach (var (text, id) in items)
        {
            option.AddItem(text, id);
            if (id == selectedId)
            {
                selectedIndex = idx;
            }
            idx++;
        }

        if (option.ItemCount > 0)
        {
            option.Select(selectedIndex);
        }
    }

    private void UpdateTechniqueHeaders()
    {
        // Guard against being called before technique UI is created
        if (_techniqueCheckboxes == null || _techniqueCheckboxes.Count == 0)
            return;

        foreach (var difficulty in _techniqueCheckboxes.Keys)
        {
            var diffContainer = _techniquesContainer?.GetNodeOrNull<VBoxContainer>($"Diff{difficulty}Container");
            if (diffContainer == null) continue;

            var headerButton = diffContainer.GetNodeOrNull<Button>($"Header{difficulty}");
            if (headerButton == null) continue;

            // Second child is the margin container that holds checkboxes
            var marginContainer = diffContainer.GetChild(1) as MarginContainer;
            bool expanded = marginContainer == null || marginContainer.Visible;
            string arrow = expanded ? "▼" : "▶";
            string diffText = _localizationService.GetDifficultyDisplay(difficulty);
            headerButton.Text = $"{arrow} {diffText}";
        }
    }

    private void ApplyTheme()
    {

        var colors = _themeService.CurrentColors;

        var panelStyle = _themeService.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);

        var settingsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/MarginContainer/VBoxContainer/SettingsContainer");
        ApplyThemeRecursive(settingsContainer, _themeService, colors);

        // Back Button
        _backButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
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
                // Keep default CheckButton visuals (switch/checkbox). Only adjust font colors.
                if (button is CheckButton)
                {
                    button.AddThemeColorOverride("font_color", colors.TextPrimary);
                    button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
                }
                else
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
            }

            if (child is Node n)
            {
                ApplyThemeRecursive(n, theme, colors);
            }
        }
    }
}
