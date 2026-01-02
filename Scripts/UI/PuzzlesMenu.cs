namespace SudokuSen.UI;

using System.Linq;
using SudokuSen.Models;
using SudokuSen.Services;

/// <summary>
/// Puzzles browser menu - shows prebuilt puzzles with completion state.
/// </summary>
public partial class PuzzlesMenu : Control
{
    // Cached service references
    private ThemeService _themeService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;
    private AudioService _audioService = null!;
    private LocalizationService _localizationService = null!;

    private Button _backButton = null!;
    private Label _title = null!;
    private PanelContainer _panel = null!;
    private TabBar _difficultyTabs = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _puzzleList = null!;
    private Label _emptyLabel = null!;
    private ConfirmationDialog _confirmDialog = null!;

    private Difficulty _currentDifficulty = Difficulty.Easy;
    private string? _selectedPuzzleId;

    // Difficulty order for tabs (excluding Kids)
    private static readonly Difficulty[] TabDifficulties = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Insane };

    public override void _Ready()
    {
        // Cache services
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");

        UiNavigationSfx.Wire(this, _audioService);

        // UI references
        _backButton = GetNode<Button>("BackButton");
        _title = GetNode<Label>("Title");
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _difficultyTabs = GetNode<TabBar>("CenterContainer/Panel/MarginContainer/VBoxContainer/DifficultyTabs");
        _scrollContainer = GetNode<ScrollContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ScrollContainer");
        _puzzleList = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ScrollContainer/PuzzleList");
        _emptyLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/ScrollContainer/PuzzleList/EmptyLabel");
        _confirmDialog = GetNode<ConfirmationDialog>("ConfirmDialog");

        // Events
        _backButton.Pressed += OnBackPressed;
        _difficultyTabs.TabChanged += OnTabChanged;
        _confirmDialog.Confirmed += OnConfirmDialogConfirmed;

        // Theme
        ApplyTheme();
        ApplyLocalization();
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        // Initial display
        _difficultyTabs.CurrentTab = 0;
        _currentDifficulty = Difficulty.Easy;
        RefreshPuzzleList();
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnThemeChanged(int themeIndex) => ApplyTheme();
    private void OnLanguageChanged(int languageIndex)
    {
        ApplyLocalization();
        RefreshPuzzleList();
    }

    private void ApplyLocalization()
    {
        var l = _localizationService;
        _title.Text = l.Get("puzzles.title");
        _backButton.Text = l.Get("menu.back");
        _emptyLabel.Text = l.Get("puzzles.empty");

        // Tab titles
        for (int i = 0; i < TabDifficulties.Length; i++)
        {
            _difficultyTabs.SetTabTitle(i, l.GetDifficultyName(TabDifficulties[i]));
        }

        _confirmDialog.Title = l.Get("puzzles.confirm.title");
        _confirmDialog.OkButtonText = l.Get("puzzles.confirm.start");
        _confirmDialog.CancelButtonText = l.Get("dialog.cancel");
    }

    private void ApplyTheme()
    {
        var colors = _themeService.CurrentColors;

        // Panel
        _panel.AddThemeStyleboxOverride("panel", _themeService.CreatePanelStyleBox(12, 0));

        // Title
        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _emptyLabel.AddThemeColorOverride("font_color", colors.TextSecondary);

        // Back button
        _backButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);
    }

    private void OnBackPressed()
    {
        _audioService.PlayClick();
        _appState.GoToMainMenu();
    }

    private void OnTabChanged(long tabIndex)
    {
        _audioService.PlayClick();
        if (tabIndex >= 0 && tabIndex < TabDifficulties.Length)
        {
            _currentDifficulty = TabDifficulties[tabIndex];
            RefreshPuzzleList();
        }
    }

    private void RefreshPuzzleList()
    {
        // Clear existing items (keep EmptyLabel)
        foreach (var child in _puzzleList.GetChildren())
        {
            if (child != _emptyLabel)
                child.QueueFree();
        }

        var metas = PrebuiltPuzzleLibrary.GetMetadataByDifficulty(_currentDifficulty);
        _emptyLabel.Visible = metas.Count == 0;

        var colors = _themeService.CurrentColors;

        foreach (var meta in metas)
        {
            var row = CreatePuzzleRow(meta, colors);
            _puzzleList.AddChild(row);
        }
    }

    private HBoxContainer CreatePuzzleRow(PrebuiltPuzzleLibrary.PrebuiltPuzzleMetadata meta, ThemeService.ThemeColors colors)
    {
        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        bool completed = _saveService.Settings.HasCompletedPrebuiltPuzzle(meta.Id);

        // Status icon
        var statusLabel = new Label();
        statusLabel.Text = completed ? "✔" : "○";
        statusLabel.CustomMinimumSize = new Vector2(32, 0);
        statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        statusLabel.AddThemeColorOverride("font_color", completed ? new Color(0.2f, 0.8f, 0.3f) : colors.TextSecondary);
        row.AddChild(statusLabel);

        // Puzzle name
        var nameLabel = new Label();
        nameLabel.Text = GetDisplayName(meta);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        row.AddChild(nameLabel);

        // Completion badge
        if (completed)
        {
            var badge = new Label();
            badge.Text = _localizationService.Get("puzzles.completed");
            badge.AddThemeFontSizeOverride("font_size", 12);
            badge.AddThemeColorOverride("font_color", new Color(0.2f, 0.7f, 0.3f)); // Green success color
            row.AddChild(badge);
        }

        // Play button
        var playButton = new Button();
        playButton.Text = _localizationService.Get("puzzles.play");
        playButton.CustomMinimumSize = new Vector2(80, 36);
        ApplyButtonStyle(playButton, colors);
        string puzzleId = meta.Id; // capture for lambda
        playButton.Pressed += () => OnPuzzlePlayPressed(puzzleId);
        row.AddChild(playButton);

        return row;
    }

    private string GetDisplayName(PrebuiltPuzzleLibrary.PrebuiltPuzzleMetadata meta)
    {
        // Extract number from id suffix (easy_1 -> 1)
        string number = meta.Id.Split('_').Last();
        string difficultyName = _localizationService.GetDifficultyName(meta.Difficulty);
        return $"{difficultyName} #{number}";
    }

    private void ApplyButtonStyle(Button button, ThemeService.ThemeColors colors)
    {
        button.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        button.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        button.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        button.AddThemeColorOverride("font_color", colors.TextPrimary);
    }

    private void OnPuzzlePlayPressed(string puzzleId)
    {
        _audioService.PlayClick();
        _selectedPuzzleId = puzzleId;

        var puzzle = PrebuiltPuzzleLibrary.GetById(puzzleId);
        if (puzzle == null) return;

        bool completed = _saveService.Settings.HasCompletedPrebuiltPuzzle(puzzleId);

        // If not yet completed, start immediately without confirmation
        if (!completed)
        {
            _appState.StartPrebuiltPuzzle(puzzleId);
            return;
        }

        // Already completed - ask for confirmation to replay
        string name = puzzle.GetDisplayName(_localizationService);
        string status = _localizationService.Get("puzzles.confirm.replay");

        _confirmDialog.DialogText = $"{name}\n\n{status}";
        _confirmDialog.PopupCentered();
    }

    private void OnConfirmDialogConfirmed()
    {
        if (string.IsNullOrEmpty(_selectedPuzzleId)) return;
        _appState.StartPrebuiltPuzzle(_selectedPuzzleId);
    }
}
