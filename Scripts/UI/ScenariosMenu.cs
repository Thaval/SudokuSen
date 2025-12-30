using MySudoku.Logic;
using MySudoku.Services;

namespace MySudoku.UI;

/// <summary>
/// Szenarien-Menü - Wähle eine Technik zum Üben
/// </summary>
public partial class ScenariosMenu : Control
{
    private Button _backButton = null!;
    private Label _title = null!;
    private Label _description = null!;
    private PanelContainer _panel = null!;
    private VBoxContainer _techniquesContainer = null!;
    private ScrollContainer _scrollContainer = null!;

    // Cached service references
    private ThemeService _themeService = null!;
    private AppState _appState = null!;

    // Gruppierte Techniken
    private static readonly (string Category, string[] TechniqueIds)[] TechniqueGroups = new[]
    {
        ("🟢 Leicht", new[] { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock" }),
        ("🟠 Mittel", new[] { "NakedPair", "NakedTriple", "HiddenPair", "PointingPair", "BoxLineReduction" }),
        ("🔴 Schwer", new[] { "XWing", "Swordfish", "XYWing", "Skyscraper", "SimpleColoring" })
    };

    public override void _Ready()
    {
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _appState = GetNode<AppState>("/root/AppState");

        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("Title");
        _description = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Description");
        _scrollContainer = GetNode<ScrollContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ScrollContainer");
        _techniquesContainer = _scrollContainer.GetNode<VBoxContainer>("TechniquesContainer");
        _backButton = GetNode<Button>("BackButton");

        _backButton.Pressed += OnBackPressed;

        CreateTechniqueButtons();
        ApplyTheme();

        _themeService.ThemeChanged += OnThemeChanged;
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

    private void CreateTechniqueButtons()
    {
        var colors = _themeService.CurrentColors;

        foreach (var (category, techniqueIds) in TechniqueGroups)
        {
            // Kategorie-Header
            var categoryLabel = new Label();
            categoryLabel.Text = category;
            categoryLabel.AddThemeFontSizeOverride("font_size", 20);
            categoryLabel.AddThemeColorOverride("font_color", colors.Accent);
            _techniquesContainer.AddChild(categoryLabel);

            // Spacer
            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, 8);
            _techniquesContainer.AddChild(spacer);

            // Technik-Buttons
            foreach (var techId in techniqueIds)
            {
                if (!TechniqueInfo.Techniques.TryGetValue(techId, out var technique))
                    continue;

                var button = new Button();
                button.Name = $"Btn{techId}";
                button.Text = $"🎯 {technique.Name}";
                button.TooltipText = $"{technique.Description}\n\nKlicke um ein Puzzle zu starten, das diese Technik erfordert.";
                button.CustomMinimumSize = new Vector2(0, 44);
                button.SizeFlagsHorizontal = SizeFlags.ExpandFill;

                // Button-Style
                button.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
                button.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
                button.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
                button.AddThemeColorOverride("font_color", colors.TextPrimary);

                var capturedTechId = techId;
                button.Pressed += () => OnTechniqueSelected(capturedTechId);

                _techniquesContainer.AddChild(button);
            }

            // Separator nach Kategorie
            var sep = new HSeparator();
            sep.CustomMinimumSize = new Vector2(0, 16);
            _techniquesContainer.AddChild(sep);
        }
    }

    private void OnTechniqueSelected(string techniqueId)
    {
        if (!TechniqueInfo.Techniques.TryGetValue(techniqueId, out var technique))
            return;

        GD.Print($"Starting scenario for technique: {technique.Name}");

        _appState.StartScenarioGame(techniqueId);
    }

    private void OnBackPressed()
    {
        _appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
        // Recreate buttons with new theme
        foreach (var child in _techniquesContainer.GetChildren())
        {
            child.QueueFree();
        }
        CallDeferred(nameof(CreateTechniqueButtons));
    }

    private void ApplyTheme()
    {
        var colors = _themeService.CurrentColors;

        var panelStyle = _themeService.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _description.AddThemeColorOverride("font_color", colors.TextSecondary);

        _backButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);
    }
}