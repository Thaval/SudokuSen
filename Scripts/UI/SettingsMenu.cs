namespace MySudoku.UI;

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

    // Flag to prevent event cascading during LoadSettings
    private bool _isLoadingSettings = false;

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

        UiNavigationSfx.Wire(this, _audioService);

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

        UpdateAudioUi();

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

        // Events - Audio
        _sfxCheck.Toggled += OnSfxToggled;
        _sfxVolumeSlider.ValueChanged += OnSfxVolumeChanged;
        _musicCheck.Toggled += OnMusicToggled;
        _musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
        _menuMusicOption.ItemSelected += OnMenuMusicSelected;
        _gameMusicOption.ItemSelected += OnGameMusicSelected;

        _smartCleanupCheck.Toggled += OnSmartCleanupToggled;
        _houseAutoFillCheck.Toggled += OnHouseAutoFillToggled;

        _challengeNoNotesCheck.Toggled += OnChallengeNoNotesToggled;
        _challengePerfectCheck.Toggled += OnChallengePerfectToggled;
        _challengeHintsOption.ItemSelected += OnChallengeHintsSelected;
        _challengeTimeOption.ItemSelected += OnChallengeTimeSelected;

        _resetTechniquesButton.Pressed += OnResetTechniquesPressed;

        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;
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
        menuMusicLabel.Text = "Menü-Musik";
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
        gameMusicLabel.Text = "Spiel-Musik";
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

    private void LoadSettings()
    {
        _isLoadingSettings = true;

        var settings = _saveService.Settings;

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

        _challengeNoNotesCheck.ButtonPressed = settings.ChallengeNoNotes;
        _challengePerfectCheck.ButtonPressed = settings.ChallengePerfectRun;

        SelectOptionById(_challengeHintsOption, settings.ChallengeHintLimit);
        SelectOptionById(_challengeTimeOption, settings.ChallengeTimeAttackMinutes);

        // Technik-Checkboxen laden
        LoadTechniqueSettings();

        GD.Print($"[UI] SettingsMenu: loaded | theme={settings.ThemeIndex}, colorblind={settings.ColorblindPaletteEnabled}, sfx={settings.SoundEnabled}({settings.Volume}%), music={settings.MusicEnabled}({settings.MusicVolume}%), menuTrack={settings.MenuMusicTrack}, gameTrack={settings.GameMusicTrack}, uiScale={settings.UiScalePercent}%");

        _isLoadingSettings = false;

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
        _storagePathInfo.Text = $"📂 {resolvedPath}";
    }

    private void LoadTechniqueSettings()
    {

        var settings = _saveService.Settings;

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
        GD.Print("[UI] SettingsMenu: SaveSettings()" );
        _saveService.SaveSettings();
    }

    private void OnTechniqueToggled(Difficulty difficulty, string techId, bool pressed)
    {

        var settings = _saveService.Settings;

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
                    _storagePathEdit.Text = _saveService.Settings.CustomStoragePath;
                    return;
                }
            }
        }

        _saveService.Settings.CustomStoragePath = cleanPath;
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
                Title = "Speicherort auswählen",
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

    private void OnThemeSelected(long index)
    {
        if (!_isLoadingSettings) _audioService.PlayClick();
        GD.Print($"[UI] SettingsMenu: Theme selected = {index}");
        _saveService.Settings.ThemeIndex = (int)index;
        SaveSettings();


        _themeService.SetTheme((int)index);
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

    private void OnUiScaleChanged(double value)
    {
        int pct = (int)Math.Round(value);
        _uiScaleValue.Text = $"{pct}%";

        GD.Print($"[UI] SettingsMenu: UI scale changed = {pct}%");


        _saveService.Settings.UiScalePercent = pct;
        SaveSettings();


        _themeService.ApplyUiScale(pct);
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
        GD.Print("[UI] SettingsMenu: Back pressed");
        _audioService.PlayClick();
        _appState.GoToMainMenu();
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
