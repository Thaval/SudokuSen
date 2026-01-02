namespace SudokuSen.UI;

/// <summary>
/// Spielverlauf-Menü
/// </summary>
public partial class HistoryMenu : Control
{
    private Button _backButton = null!;
    private Label _title = null!;
    private PanelContainer _panel = null!;
    private VBoxContainer _historyList = null!;
    private Label _emptyLabel = null!;
    private Control? _overlayContainer;

    // Cached service references
    private ThemeService _themeService = null!;
    private AudioService _audioService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;
    private LocalizationService _localizationService = null!;

    public override void _Ready()
    {
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");

        UiNavigationSfx.Wire(this, _audioService);

        _backButton = GetNode<Button>("BackButton");
        _title = GetNode<Label>("Title");
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _historyList = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/ScrollContainer/HistoryList");
        _emptyLabel = _historyList.GetNode<Label>("EmptyLabel");
        _overlayContainer = GetNodeOrNull<Control>("OverlayContainer");

        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        ApplyLocalization();
        LoadHistory();
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

    private void LoadHistory()
    {
        var history = _saveService.History;

        // Vorhandene Einträge löschen (außer EmptyLabel)
        foreach (var child in _historyList.GetChildren())
        {
            if (child != _emptyLabel)
                child.QueueFree();
        }

        if (history.Count == 0)
        {
            _emptyLabel.Visible = true;
            return;
        }

        _emptyLabel.Visible = false;

        var colors = _themeService.CurrentColors;

        foreach (var entry in history)
        {
            var entryPanel = CreateHistoryEntry(entry, _themeService, colors);
            _historyList.AddChild(entryPanel);
        }
    }

    private PanelContainer CreateHistoryEntry(HistoryEntry entry, ThemeService theme, ThemeService.ThemeColors colors)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(0, 80);
        panel.MouseFilter = MouseFilterEnum.Stop;

        var style = new StyleBoxFlat();
        style.BgColor = colors.CellBackground;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 12;
        style.ContentMarginBottom = 12;
        panel.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 24);
        panel.AddChild(hbox);

        // Status-Icon (farbig)
        var statusPanel = new Panel();
        statusPanel.CustomMinimumSize = new Vector2(8, 0);
        var statusStyle = new StyleBoxFlat();
        statusStyle.BgColor = entry.Status switch
        {
            GameStatus.Won => new Color("4caf50"),
            GameStatus.Lost => new Color("f44336"),
            GameStatus.InProgress => new Color("2196f3"),
            _ => new Color("9e9e9e")
        };
        statusStyle.CornerRadiusTopLeft = 4;
        statusStyle.CornerRadiusTopRight = 4;
        statusStyle.CornerRadiusBottomLeft = 4;
        statusStyle.CornerRadiusBottomRight = 4;
        statusPanel.AddThemeStyleboxOverride("panel", statusStyle);
        hbox.AddChild(statusPanel);

        // Info
        var infoVBox = new VBoxContainer();
        infoVBox.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        hbox.AddChild(infoVBox);

        // Datum + Schwierigkeit + Tutorial/Scenario badge
        var dateLabel = new Label();
        string badge = "";
        if (entry.IsTutorial)
            badge = " " + _localizationService.Get("history.badge.tutorial");
        else if (entry.IsScenario)
        {
            string techName = !string.IsNullOrWhiteSpace(entry.ScenarioTechnique)
                ? _localizationService.GetTechniqueName(entry.ScenarioTechnique)
                : _localizationService.Get("common.unknown");
            badge = " " + _localizationService.Get("history.badge.scenario", techName);
        }
        else if (entry.IsDaily)
            badge = " " + _localizationService.Get("history.badge.daily");

        string dateText = _localizationService.CurrentLanguage == Language.German
            ? entry.StartTime.ToString("dd.MM.yyyy HH:mm")
            : entry.StartTime.ToString("yyyy-MM-dd HH:mm");

        dateLabel.Text = $"{dateText} - {_localizationService.GetDifficultyDisplay(entry.Difficulty)}{badge}";
        dateLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        infoVBox.AddChild(dateLabel);

        // Status
        var statusLabel = new Label();
        statusLabel.Text = entry.Status switch
        {
            GameStatus.Won => _localizationService.Get("history.won"),
            GameStatus.Lost => _localizationService.Get("history.lost"),
            GameStatus.Abandoned => _localizationService.Get("history.abandoned"),
            GameStatus.InProgress => _localizationService.Get("history.in_progress"),
            _ => _localizationService.Get("common.unknown")
        };
        statusLabel.AddThemeColorOverride("font_color", colors.TextSecondary);
        infoVBox.AddChild(statusLabel);

        // Details
        var detailsVBox = new VBoxContainer();
        detailsVBox.AddThemeConstantOverride("separation", 2);
        hbox.AddChild(detailsVBox);

        var timeLabel = new Label();
        timeLabel.Text = _localizationService.Get("history.time", entry.GetFormattedDuration());
        timeLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        timeLabel.HorizontalAlignment = HorizontalAlignment.Right;
        detailsVBox.AddChild(timeLabel);

        var mistakesLabel = new Label();
        mistakesLabel.Text = _localizationService.Get("history.mistakes", entry.Mistakes);
        mistakesLabel.AddThemeColorOverride("font_color", colors.TextSecondary);
        mistakesLabel.HorizontalAlignment = HorizontalAlignment.Right;
        detailsVBox.AddChild(mistakesLabel);

        // Click to replay (if data available)
        panel.GuiInput += (InputEvent e) =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                if (!entry.HasReplayData)
                {
                    GD.Print("[History] Entry has no replay data to show.");
                    ShowNoReplayOverlay();
                    return;
                }

                _audioService.PlayClick();
                _appState.StartHistoryReplay(entry);
            }
        };

        return panel;
    }

    private void OnBackPressed()
    {
        _audioService.PlayClick();
        _appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
        LoadHistory(); // Neu laden mit neuem Theme
    }

    private void OnLanguageChanged(int languageIndex)
    {
        ApplyLocalization();
        LoadHistory();
    }

    private void ApplyLocalization()
    {
        _title.Text = _localizationService.Get("history.title");
        _backButton.Text = _localizationService.Get("menu.back");
        _backButton.TooltipText = _localizationService.Get("settings.back.tooltip");
        _emptyLabel.Text = _localizationService.Get("history.no_games");
    }

    private void ApplyTheme()
    {
        var colors = _themeService.CurrentColors;

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _emptyLabel.AddThemeColorOverride("font_color", colors.TextSecondary);

        var panelStyle = _themeService.CreatePanelStyleBox(8, 8);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _backButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);
    }

    private void ShowNoReplayOverlay()
    {
        if (_overlayContainer == null) return;

        var colors = _themeService.CurrentColors;

        var bg = new ColorRect();
        bg.Color = new Color(0, 0, 0, 0.7f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlayContainer.AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(380, 160);
        panel.AddThemeStyleboxOverride("panel", _themeService.CreatePanelStyleBox(12, 16));
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 12);
        vbox.CustomMinimumSize = new Vector2(340, 0);
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = _localizationService.Get("history.replay.unavailable.title", "Replay unavailable");
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", colors.TextPrimary);
        title.AddThemeFontSizeOverride("font_size", 22);
        vbox.AddChild(title);

        var msg = new Label();
        msg.Text = _localizationService.Get("history.replay.unavailable.msg", "This game was recorded before replays were stored.");
        msg.HorizontalAlignment = HorizontalAlignment.Center;
        msg.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        msg.AddThemeColorOverride("font_color", colors.TextSecondary);
        vbox.AddChild(msg);

        var btn = new Button();
        btn.Text = _localizationService.Get("common.close");
        btn.CustomMinimumSize = new Vector2(0, 44);
        btn.Pressed += () => bg.QueueFree();
        btn.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        btn.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        btn.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        btn.AddThemeColorOverride("font_color", colors.TextPrimary);
        vbox.AddChild(btn);
    }
}
