namespace MySudoku.UI;

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

    // Cached service references
    private ThemeService _themeService = null!;
    private AudioService _audioService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;

    public override void _Ready()
    {
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");

        UiNavigationSfx.Wire(this, _audioService);

        _backButton = GetNode<Button>("BackButton");
        _title = GetNode<Label>("Title");
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _historyList = GetNode<VBoxContainer>("CenterContainer/Panel/ScrollContainer/HistoryList");
        _emptyLabel = _historyList.GetNode<Label>("EmptyLabel");

        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;

        LoadHistory();
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

        // Datum + Schwierigkeit
        var dateLabel = new Label();
        dateLabel.Text = $"{entry.StartTime:dd.MM.yyyy HH:mm} - {entry.GetDifficultyText()}";
        dateLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        infoVBox.AddChild(dateLabel);

        // Status
        var statusLabel = new Label();
        statusLabel.Text = entry.GetStatusText();
        statusLabel.AddThemeColorOverride("font_color", colors.TextSecondary);
        infoVBox.AddChild(statusLabel);

        // Details
        var detailsVBox = new VBoxContainer();
        detailsVBox.AddThemeConstantOverride("separation", 2);
        hbox.AddChild(detailsVBox);

        var timeLabel = new Label();
        timeLabel.Text = $"Zeit: {entry.GetFormattedDuration()}";
        timeLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        timeLabel.HorizontalAlignment = HorizontalAlignment.Right;
        detailsVBox.AddChild(timeLabel);

        var mistakesLabel = new Label();
        mistakesLabel.Text = $"Fehler: {entry.Mistakes}";
        mistakesLabel.AddThemeColorOverride("font_color", colors.TextSecondary);
        mistakesLabel.HorizontalAlignment = HorizontalAlignment.Right;
        detailsVBox.AddChild(mistakesLabel);

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
}
