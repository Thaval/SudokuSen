using SudokuSen.Logic;
using SudokuSen.Models;
using SudokuSen.Services;

namespace SudokuSen.UI;

/// <summary>
/// Szenarien-Menü - Wähle eine Technik zum Üben oder starte ein Tutorial
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
    private AudioService _audioService = null!;
    private AppState _appState = null!;
    private LocalizationService _localizationService = null!;
    private TutorialService? _tutorialService;

    public override void _Ready()
    {
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _appState = GetNode<AppState>("/root/AppState");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");
        _tutorialService = GetNodeOrNull<TutorialService>("/root/TutorialService");

        UiNavigationSfx.Wire(this, _audioService);

        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("Title");
        _description = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Description");
        _scrollContainer = GetNode<ScrollContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ScrollContainer");
        _techniquesContainer = _scrollContainer.GetNode<VBoxContainer>("TechniquesContainer");
        _backButton = GetNode<Button>("BackButton");

        _backButton.Pressed += OnBackPressed;

        CreateTechniqueButtons();
        ApplyTheme();
        ApplyLocalization();

        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;
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

    private void CreateTechniqueButtons()
    {
        var colors = _themeService.CurrentColors;

        // === TUTORIALS SECTION ===
        CreateTutorialsSection(colors);

        // Separator after tutorials
        var tutorialSep = new HSeparator();
        tutorialSep.CustomMinimumSize = new Vector2(0, 24);
        _techniquesContainer.AddChild(tutorialSep);

        // === TECHNIQUE SCENARIOS HEADER ===
        var scenariosTitle = new Label();
        scenariosTitle.Text = _localizationService.Get("scenarios.techniques.title");
        scenariosTitle.AddThemeFontSizeOverride("font_size", 18);
        scenariosTitle.AddThemeColorOverride("font_color", colors.TextSecondary);
        _techniquesContainer.AddChild(scenariosTitle);

        var scenariosSubtitle = new Label();
        scenariosSubtitle.Text = _localizationService.Get("scenarios.techniques.desc");
        scenariosSubtitle.AddThemeFontSizeOverride("font_size", 14);
        scenariosSubtitle.AddThemeColorOverride("font_color", colors.TextSecondary);
        _techniquesContainer.AddChild(scenariosSubtitle);

        var headerSpacer = new Control();
        headerSpacer.CustomMinimumSize = new Vector2(0, 12);
        _techniquesContainer.AddChild(headerSpacer);

        // === TECHNIQUE SCENARIOS ===
        foreach (var group in TechniqueInfo.PracticeGroups)
        {
            // Kategorie-Header
            var categoryLabel = new Label();
            categoryLabel.Text = _localizationService.Get(group.CategoryKey);
            categoryLabel.AddThemeFontSizeOverride("font_size", 20);
            categoryLabel.AddThemeColorOverride("font_color", colors.Accent);
            _techniquesContainer.AddChild(categoryLabel);

            // Spacer
            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, 8);
            _techniquesContainer.AddChild(spacer);

            // Technik-Buttons
            foreach (var techId in group.TechniqueIds)
            {
                if (!TechniqueInfo.Techniques.TryGetValue(techId, out var technique))
                    continue;

                var button = new Button();
                button.Name = $"Btn{techId}";
                var techName = _localizationService.GetTechniqueName(techId);
                var techDesc = _localizationService.GetTechniqueDescription(techId);
                button.Text = $"🎯 {techName}";
                button.TooltipText = _localizationService.Get("scenarios.technique.tooltip", techDesc);
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

    private void CreateTutorialsSection(ThemeService.ThemeColors colors)
    {
        // Tutorials header
        var tutorialTitle = new Label();
        tutorialTitle.Text = _localizationService.Get("scenarios.tutorials.title");
        tutorialTitle.AddThemeFontSizeOverride("font_size", 20);
        tutorialTitle.AddThemeColorOverride("font_color", colors.Accent);
        _techniquesContainer.AddChild(tutorialTitle);

        // Subtitle description
        var tutorialSubtitle = new Label();
        tutorialSubtitle.Text = _localizationService.Get("scenarios.tutorials.desc");
        tutorialSubtitle.AddThemeFontSizeOverride("font_size", 14);
        tutorialSubtitle.AddThemeColorOverride("font_color", colors.TextSecondary);
        _techniquesContainer.AddChild(tutorialSubtitle);

        // Spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 8);
        _techniquesContainer.AddChild(spacer);

        // Define tutorials inline (matches TutorialService definitions)
        var tutorials = new[]
        {
            (Id: "getting_started", NameKey: "tutorial.getting_started", DescKey: "tutorial.getting_started.desc", Difficulty: TutorialDifficulty.Easy, Minutes: 6),
            (Id: "basic_techniques", NameKey: "tutorial.basic_techniques", DescKey: "tutorial.basic_techniques.desc", Difficulty: TutorialDifficulty.Medium, Minutes: 8),
            (Id: "advanced_features", NameKey: "tutorial.advanced_features", DescKey: "tutorial.advanced_features.desc", Difficulty: TutorialDifficulty.Medium, Minutes: 10),
            (Id: "advanced_techniques", NameKey: "tutorial.advanced_techniques", DescKey: "tutorial.advanced_techniques.desc", Difficulty: TutorialDifficulty.Hard, Minutes: 15),
            (Id: "challenge_modes", NameKey: "tutorial.challenge_modes", DescKey: "tutorial.challenge_modes.desc", Difficulty: TutorialDifficulty.Hard, Minutes: 8),
        };

        foreach (var tutorial in tutorials)
        {
            var button = new Button();
            button.Name = $"BtnTutorial_{tutorial.Id}";

            // Emoji based on difficulty
            string difficultyEmoji = tutorial.Difficulty switch
            {
                TutorialDifficulty.Easy => "🟢",
                TutorialDifficulty.Medium => "🟠",
                TutorialDifficulty.Hard => "🔴",
                _ => "📖"
            };

            var tutorialName = _localizationService.Get(tutorial.NameKey);
            var tutorialDesc = _localizationService.Get(tutorial.DescKey);
            button.Text = $"{difficultyEmoji} {tutorialName}";
            button.TooltipText = $"{tutorialDesc}\n\n{_localizationService.Get("scenarios.minutes", tutorial.Minutes)}";
            button.CustomMinimumSize = new Vector2(0, 44);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            // Button-Style
            button.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
            button.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
            button.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
            button.AddThemeColorOverride("font_color", colors.TextPrimary);

            var capturedTutorialId = tutorial.Id;
            button.Pressed += () => OnTutorialSelected(capturedTutorialId);

            _techniquesContainer.AddChild(button);
        }
    }

    private void OnTutorialSelected(string tutorialId)
    {
        GD.Print($"[ScenariosMenu] Starting tutorial: {tutorialId}");

        // Start tutorial game
        _appState.StartTutorialGame(tutorialId);
    }

    private void OnBackPressed()
    {
        _audioService.PlayClick();
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

    private void OnLanguageChanged(int languageIndex)
    {
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        _title.Text = _localizationService.Get("scenarios.title");
        _description.Text = _localizationService.Get("scenarios.description");
        _backButton.Text = _localizationService.Get("menu.back");
        _backButton.TooltipText = _localizationService.Get("settings.back.tooltip");
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