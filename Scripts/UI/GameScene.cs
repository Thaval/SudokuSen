namespace SudokuSen.UI;

/// <summary>
/// Die Haupt-Spielszene mit dem Sudoku-Grid
/// </summary>
public partial class GameScene : Control
{
    // Cached Service References (initialized in _Ready)
    private ThemeService _themeService = null!;
    private SaveService _saveService = null!;
    private AppState _appState = null!;
    private AudioService _audioService = null!;
    private LocalizationService _localizationService = null!;
    private TutorialService? _tutorialService;

    // UI-Elemente
    private Button _backButton = null!;
    private Button _hintButton = null!;
    private Button _notesButton = null!;
    private Button _autoCandidatesButton = null!;
    private Button _houseAutoFillButton = null!;
    private Label _difficultyLabel = null!;
    private Label _timerLabel = null!;
    private Label _mistakesLabel = null!;
    private PanelContainer _gridPanel = null!;
    private GridContainer _gridContainer = null!;
    private VBoxContainer _gridWrapper = null!;
    private HBoxContainer _numberPad = null!;
    private Control _overlayContainer = null!;

    // Tutorial overlay
    private TutorialOverlay? _tutorialOverlay;

    // Axis Labels
    private Label[] _colLabels = new Label[9]; // A-I (or A-D for Kids)
    private Label[] _rowLabels = new Label[9]; // 1-9 (or 1-4 for Kids)

    // Zellen - dynamisch allokiert basierend auf GridSize
    private SudokuCellButton[,]? _cellButtons;
    private Button[] _numberButtons = new Button[10]; // 1-9 + Eraser (oder 1-4 für Kids)

    // Spielzustand
    private SudokuGameState? _gameState;
    private int _selectedRow = -1;
    private int _selectedCol = -1;
    private int _highlightedNumber = 0;
    private double _elapsedTime = 0;
    private int _lastTimerSecond = -1; // Cache to avoid per-frame string allocations
    private bool _isGameOver = false;
    private bool _isPaused = false;
    private bool _isNotesMode = false;
    private bool _showAutoCandidates = false;

    // Reusable arrays for hot paths (avoid per-frame allocations)
    private readonly bool[] _candidatesPool = new bool[9];
    private readonly bool[] _emptyCandidates4 = new bool[4];
    private readonly bool[] _emptyCandidates9 = new bool[9];

    private enum HouseAutoFillMode
    {
        Row,
        Column,
        Block
    }

    private HouseAutoFillMode _houseAutoFillMode = HouseAutoFillMode.Row;

    // Multi-Select
    private HashSet<(int row, int col)> _selectedCells = new();
    private bool _isDragging = false;
    private (int row, int col)? _dragStart = null;

    // Hint-System
    private HintService.Hint? _currentHint = null;
    private int _hintPage = 0;
    private Control? _hintOverlay = null;
    private HashSet<(int row, int col)> _hintHighlightedCells = new();

    // Solution path
    private Button? _solutionPathButton;
    private Control? _solutionPathOverlay;
    private VBoxContainer? _solutionPathList;
    private Label? _solutionPathHeader;
    private OptionButton? _solutionPathSelector;
    private SolutionPathService.SolutionPath? _solutionPath;
    private List<SolutionPathService.SolutionPath>? _solutionPathVariants;
    private int _solutionPathSelectedIndex;
    private int[,]? _solutionPathOriginalSnapshot;
    private int? _solutionPathActiveIndex;
    private PanelContainer? _solutionPathDetailPanel;
    private RichTextLabel? _solutionPathDetailLabel;
    private int? _solutionPathDetailSelectedIndex;

    // History replay navigation
    private bool _isHistoryReplay = false;
    private List<int[,]> _historySnapshots = new();
    private int _historyStepIndex = 0;
    private Button? _historyFirstButton;
    private Button? _historyPrevButton;
    private Button? _historyNextButton;
    private Button? _historyLastButton;
    private Button? _historyStepsButton;
    private HBoxContainer? _historyNavBar;
    private AcceptDialog? _historyJumpDialog;
    private LineEdit? _historyJumpInput;
    private readonly HashSet<(int row, int col)> _historyCurrentStepCells = new();

    private (int row, int col, int value, string techniqueId)? _lastHintTracking = null;

    public override void _Ready()
    {
        // Cache service references
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");
        _tutorialService = GetNodeOrNull<TutorialService>("/root/TutorialService");

        _saveService.SettingsChanged += OnSettingsChanged;

        // Start game music
        _audioService.StartGameMusic();

        // UI-Referenzen holen
        _backButton = GetNode<Button>("VBoxContainer/HeaderMargin/Header/BackButton");
        _difficultyLabel = GetNode<Label>("VBoxContainer/HeaderMargin/Header/DifficultyLabel");
        _timerLabel = GetNode<Label>("VBoxContainer/HeaderMargin/Header/TimerLabel");
        _mistakesLabel = GetNode<Label>("VBoxContainer/HeaderMargin/Header/MistakesLabel");
        _gridPanel = GetNode<PanelContainer>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/GridPanel");
        _gridWrapper = GetNode<VBoxContainer>("VBoxContainer/GridCenterContainer/GridWrapper");
        _gridContainer = GetNode<GridContainer>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/GridPanel/GridContainer");
        _numberPad = GetNode<HBoxContainer>("VBoxContainer/NumberPadMargin/NumberPadContainer/NumberPad");
        _overlayContainer = GetNode<Control>("OverlayContainer");

        // Achsen-Labels erstellen
        CreateAxisLabels();

        // Hint-Button erstellen und zum Header hinzufügen
        CreateHintButton();
        CreateSolutionPathButton();
        CreateAutoCandidatesButton();
        CreateNotesButton();
        CreateHouseAutoFillButton();

        // Events
        _backButton.Pressed += OnBackPressed;

        // Grid erstellen
        CreateGrid();
        CreateNumberPad();

        // Theme anwenden
        ApplyTheme();
        ApplyLocalization();
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        // Spiel laden
        LoadGame();

        // Tutorial overlay erstellen (nach LoadGame, damit Grid existiert)
        SetupTutorialOverlay();
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _localizationService.LanguageChanged -= OnLanguageChanged;

        _saveService.SettingsChanged -= OnSettingsChanged;

        // Spiel speichern beim Verlassen (aber nicht Tutorials/Szenarien)
        if (_gameState != null && !_isGameOver && !_gameState.IsTutorial && !_gameState.IsScenario)
        {
            _gameState.ElapsedSeconds = _elapsedTime;
            _appState.SaveGame();
        }
    }

    private void OnSettingsChanged()
    {
        if (_gameState == null) return;
        ApplyChallengeUi();
        UpdateGrid();
        UpdateNumberCounts();
    }

    /// <summary>
    /// Applies standard button styling with normal, hover, pressed, and disabled states.
    /// </summary>
    private void ApplyButtonStyle(Button button, bool includeDisabled = true)
    {
        var colors = _themeService.CurrentColors;
        var normalStyle = _themeService.CreateButtonStyleBox();
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("focus", normalStyle); // Prevent white border on click
        button.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        button.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        if (includeDisabled)
        {
            button.AddThemeStyleboxOverride("disabled", _themeService.CreateButtonStyleBox(disabled: true));
        }
        button.AddThemeColorOverride("font_color", colors.TextPrimary);
    }

    public override void _Process(double delta)
    {
        if (_gameState == null || _isGameOver || _isPaused) return;

        _elapsedTime += delta;

        // Only update display when second actually changes (avoid per-frame string allocations)
        int currentSecond = (int)_elapsedTime;
        if (currentSecond != _lastTimerSecond)
        {
            UpdateTimerDisplay();
        }

        // Time Attack
        if (_gameState.ChallengeTimeAttackSeconds > 0 && _elapsedTime >= _gameState.ChallengeTimeAttackSeconds)
        {
            _isGameOver = true;
            _gameState.ElapsedSeconds = _elapsedTime;
            _appState.EndGame(GameStatus.Lost);
            ShowTimeAttackOverlay();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isGameOver || _gameState == null || _isPaused) return;

        // Maus-Events für Drag-Select und Background-Klick
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    // Klick auf Hintergrund? Auswahl aufheben
                    var mousePos = mouseButton.Position;
                    if (!IsClickOnInteractiveElement(mousePos))
                    {
                        ClearAllSelection();
                        UpdateGrid();
                        UpdateNumberCounts();
                    }
                }
                else
                {
                    // Maus losgelassen - Drag beenden
                    _isDragging = false;
                    _dragStart = null;
                }
            }
        }

        // Tastatureingabe
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // Pfeilnavigation
            if (keyEvent.Keycode == Key.Up || keyEvent.Keycode == Key.Down ||
                keyEvent.Keycode == Key.Left || keyEvent.Keycode == Key.Right)
            {
                HandleArrowNavigation(keyEvent);
                GetViewport().SetInputAsHandled();
                return;
            }

            int number = 0;

            // Zahlen 1-9
            if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
            {
                number = (int)keyEvent.Keycode - (int)Key.Key0;
            }
            else if (keyEvent.Keycode >= Key.Kp1 && keyEvent.Keycode <= Key.Kp9)
            {
                number = (int)keyEvent.Keycode - (int)Key.Kp0;
            }
            // Löschen
            else if (keyEvent.Keycode == Key.Delete || keyEvent.Keycode == Key.Backspace)
            {
                if (HasSelection())
                {
                    TrySetNumberOnSelection(0);
                    GetViewport().SetInputAsHandled();
                }
                return;
            }
            // N = Notizen-Modus umschalten
            else if (keyEvent.Keycode == Key.N)
            {
                if (!(_gameState?.ChallengeNoNotes ?? false))
                {
                    OnNotesButtonPressed();
                }
                GetViewport().SetInputAsHandled();
                return;
            }
            // ESC = Auswahl aufheben oder Zurück
            else if (keyEvent.Keycode == Key.Escape)
            {
                if (_selectedCells.Count > 1)
                {
                    // Mehrfachauswahl aufheben, nur eine Zelle behalten
                    ClearMultiSelection();
                    UpdateGrid();
                    GetViewport().SetInputAsHandled();
                    return;
                }
                OnBackPressed();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (number >= 1 && number <= 9 && HasSelection())
            {
                // Prüfe ob die Zahl für dieses Grid gültig ist
                int maxNumber = _gameState?.GridSize ?? 9;
                if (number <= maxNumber)
                {
                    TrySetNumberOnSelection(number);
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }

    private void HandleArrowNavigation(InputEventKey keyEvent)
    {
        if (_gameState == null) return;

        int gridSize = _gameState.GridSize;

        // Wenn keine Zelle ausgewählt, starte in der Mitte
        if (_selectedRow < 0 || _selectedCol < 0)
        {
            _selectedRow = gridSize / 2;
            _selectedCol = gridSize / 2;
        }
        else
        {
            int newRow = _selectedRow;
            int newCol = _selectedCol;

            switch (keyEvent.Keycode)
            {
                case Key.Up:
                    newRow = (_selectedRow - 1 + gridSize) % gridSize;
                    break;
                case Key.Down:
                    newRow = (_selectedRow + 1) % gridSize;
                    break;
                case Key.Left:
                    newCol = (_selectedCol - 1 + gridSize) % gridSize;
                    break;
                case Key.Right:
                    newCol = (_selectedCol + 1) % gridSize;
                    break;
            }

            _selectedRow = newRow;
            _selectedCol = newCol;
        }

        // Bei Shift+Pfeiltaste: zur Mehrfachauswahl hinzufügen
        if (keyEvent.ShiftPressed)
        {
            _selectedCells.Add((_selectedRow, _selectedCol));
        }
        else
        {
            // Normale Navigation: nur eine Zelle auswählen
            _selectedCells.Clear();
            _selectedCells.Add((_selectedRow, _selectedCol));
        }

        // Highlight aktualisieren
        var cell = _gameState!.Grid[_selectedRow, _selectedCol];
        _highlightedNumber = cell.Value != 0 ? cell.Value : 0;

        UpdateGrid();
        UpdateNumberCounts(); // Highlight im Numpad aktualisieren
    }

    private bool HasSelection()
    {
        return _selectedRow >= 0 && _selectedCol >= 0;
    }

    private void ClearMultiSelection()
    {
        _selectedCells.Clear();
        if (_selectedRow >= 0 && _selectedCol >= 0)
        {
            _selectedCells.Add((_selectedRow, _selectedCol));
        }
    }

    private void ClearAllSelection()
    {
        _selectedRow = -1;
        _selectedCol = -1;
        _selectedCells.Clear();
        _highlightedNumber = 0;
    }

    private bool IsClickOnInteractiveElement(Vector2 mousePos)
    {
        // Check if click is on grid panel
        if (_gridPanel != null)
        {
            var gridRect = _gridPanel.GetGlobalRect();
            if (gridRect.HasPoint(mousePos)) return true;
        }

        // Check if click is on number pad
        if (_numberPad != null)
        {
            var padRect = _numberPad.GetGlobalRect();
            if (padRect.HasPoint(mousePos)) return true;
        }

        // Check if click is on any button in header (back, hint, notes, etc.)
        if (_backButton != null && _backButton.GetGlobalRect().HasPoint(mousePos)) return true;
        if (_hintButton != null && _hintButton.GetGlobalRect().HasPoint(mousePos)) return true;
        if (_notesButton != null && _notesButton.GetGlobalRect().HasPoint(mousePos)) return true;
        if (_autoCandidatesButton != null && _autoCandidatesButton.GetGlobalRect().HasPoint(mousePos)) return true;
        if (_houseAutoFillButton != null && _houseAutoFillButton.GetGlobalRect().HasPoint(mousePos)) return true;

        // Check if click is on hint overlay
        if (_hintOverlay != null && _hintOverlay.Visible)
        {
            var overlayRect = _hintOverlay.GetGlobalRect();
            if (overlayRect.HasPoint(mousePos)) return true;
        }

        return false;
    }

    private void TrySetNumberOnSelection(int number)
    {
        if (_gameState == null || _isGameOver) return;

        // During tutorials, block grid input unless specifically waiting for it
        if (_tutorialService != null && !_tutorialService.IsGridInputAllowed)
        {
            GD.Print("[Game] Number input blocked during tutorial");
            return;
        }

        // Notes mode: apply to all selected cells
        if (_isNotesMode)
        {
            // Delete in notes mode clears notes (does not delete values)
            if (number == 0)
            {
                if (_selectedCells.Count > 0)
                {
                    foreach (var (row, col) in _selectedCells)
                    {
                        if (row < 0 || col < 0) continue;
                        var cell = _gameState.Grid[row, col];
                        if (cell.IsGiven) continue;
                        for (int i = 0; i < 9; i++) cell.Notes[i] = false;
                    }
                }
                else if (_selectedRow >= 0 && _selectedCol >= 0)
                {
                    var cell = _gameState.Grid[_selectedRow, _selectedCol];
                    if (!cell.IsGiven)
                    {
                        for (int i = 0; i < 9; i++) cell.Notes[i] = false;
                    }
                }

                SaveAndUpdate();
                return;
            }

            // Add notes for the pressed number across multi-selection
            if (number > 0)
            {
                int idx = number - 1;
                // Grid safety (Kids mode etc.)
                if (idx < 0 || idx >= 9) return;

                // Multi-select: smart toggle - if ALL have the note, remove it; otherwise add it
                if (_selectedCells.Count > 1)
                {
                    // First check if all selected cells have this note
                    bool allHaveNote = true;
                    foreach (var (row, col) in _selectedCells)
                    {
                        var cell = _gameState.Grid[row, col];
                        if (cell.IsGiven) continue;
                        if (cell.Value != 0) continue;
                        if (idx >= cell.Notes.Length) continue;
                        if (!cell.Notes[idx])
                        {
                            allHaveNote = false;
                            break;
                        }
                    }

                    // If all have the note, remove it from all; otherwise add to all
                    bool setTo = !allHaveNote;
                    foreach (var (row, col) in _selectedCells)
                    {
                        var cell = _gameState.Grid[row, col];
                        if (cell.IsGiven) continue;
                        if (cell.Value != 0) continue;
                        if (idx >= cell.Notes.Length) continue;
                        cell.Notes[idx] = setTo;
                    }
                    _audioService.PlayNotePlaceOrRemove(setTo);
                    SaveAndUpdate();

                    // Tutorial notification for multi-select note toggle
                    NotifyTutorialMultiSelect(number);
                    return;
                }
            }
        }

        // Wenn Mehrfachauswahl, auf alle anwenden
        if (_selectedCells.Count > 1)
        {
            foreach (var (row, col) in _selectedCells)
            {
                _selectedRow = row;
                _selectedCol = col;
                TrySetNumber(number);
            }
            // _selectedRow/_selectedCol are already set to the last cell from the loop
        }
        else
        {
            TrySetNumber(number);
        }
    }

    private void CreateGrid()
    {
        // Grid wird erst erstellt wenn das Spiel geladen ist
        // Siehe LoadGame() und RecreateGridForGameState()
    }

    private void RecreateGridForGameState()
    {
        if (_gameState == null) return;

        int gridSize = _gameState.GridSize;
        int blockSize = _gameState.BlockSize;

        // Alte Zellen entfernen
        foreach (var child in _gridContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Neue Zellen erstellen
        _cellButtons = new SudokuCellButton[gridSize, gridSize];
        _gridContainer.Columns = gridSize;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                var cellButton = new SudokuCellButton(row, col);
                cellButton.SetGridConfig(gridSize, blockSize);
                // Größere Zellen für kleineres Grid (4x4 = 110px, 9x9 = 48px)
                int cellSize = gridSize == 4 ? 110 : 48;
                cellButton.CustomMinimumSize = new Vector2(cellSize, cellSize);
                cellButton.CellClicked += OnCellClicked;
                cellButton.CellHovered += OnCellHovered;

                _cellButtons[row, col] = cellButton;
                _gridContainer.AddChild(cellButton);
            }
        }

        // Theme auf neue Zellen anwenden
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                _cellButtons[row, col].ApplyTheme(_themeService);
            }
        }

        // Hint-Button nur für 9x9 anzeigen
        if (_hintButton != null)
        {
            _hintButton.Visible = gridSize == 9;
        }

        // Achsen-Labels aktualisieren
        UpdateAxisLabelsForGridSize();
    }

    private void CreateNumberPad()
    {
        // NumberPad wird erst erstellt wenn das Spiel geladen ist
        // Siehe LoadGame() und RecreateNumberPadForGameState()
    }

    private void RecreateNumberPadForGameState()
    {
        if (_gameState == null) return;

        int maxNumber = _gameState.GridSize; // 4 für Kids, 9 für andere

        // Alte Buttons entfernen (außer Notes-/Assist-Buttons, die werden am Ende wieder hinzugefügt)
        // Note: QueueFree is deferred by Godot, so no ToList() needed
        foreach (var child in _numberPad.GetChildren())
        {
            if (child != _notesButton && child != _houseAutoFillButton)
            {
                child.QueueFree();
            }
        }

        // Neue Number-Buttons erstellen
        for (int i = 1; i <= maxNumber; i++)
        {
            var button = new Button();
            button.Text = i.ToString();
            button.CustomMinimumSize = new Vector2(50, 60);
            button.TooltipText = _localizationService.Get("game.number.tooltip", i);
            int num = i; // Capture für Lambda
            button.Pressed += () => OnNumberPadPressed(num);
            _numberButtons[i] = button;
            _numberPad.AddChild(button);
        }

        // Buttons für nicht verwendete Zahlen nullen
        for (int i = maxNumber + 1; i <= 9; i++)
        {
            _numberButtons[i] = null!;
        }

        // Radiergummi
        var eraserButton = new Button();
        eraserButton.Text = "⌫";
        eraserButton.CustomMinimumSize = new Vector2(50, 60);
        eraserButton.TooltipText = _localizationService.Get("game.eraser.tooltip");
        eraserButton.Pressed += () => OnNumberPadPressed(0);
        _numberButtons[0] = eraserButton;
        _numberPad.AddChild(eraserButton);

        // Assist-Buttons ans Ende verschieben
        if (_houseAutoFillButton != null)
        {
            _numberPad.MoveChild(_houseAutoFillButton, _numberPad.GetChildCount() - 1);
        }
        if (_notesButton != null)
        {
            _numberPad.MoveChild(_notesButton, _numberPad.GetChildCount() - 1);
        }

        // Theme anwenden
        var colors = _themeService.CurrentColors;
        foreach (var button in _numberButtons)
        {
            if (button == null) continue;
            ApplyButtonStyle(button);
            button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
            button.AddThemeFontSizeOverride("font_size", 24);
        }
    }

    private void CreateAxisLabels()
    {
        var colors = _themeService.CurrentColors;

        // Spalten-Labels (A-I) oben - use Control for absolute positioning
        var colLabelsContainer = GetNode<Control>("VBoxContainer/GridCenterContainer/GridWrapper/ColLabelsContainer/ColLabels");
        string[] colNames = { "A", "B", "C", "D", "E", "F", "G", "H", "I" };

        for (int i = 0; i < 9; i++)
        {
            var label = new Label();
            label.Text = colNames[i];
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 14);
            label.AddThemeColorOverride("font_color", colors.TextSecondary);
            _colLabels[i] = label;
            colLabelsContainer.AddChild(label);
        }

        // Zeilen-Labels (1-9) links - use Control for absolute positioning
        var rowLabelsContainer = GetNode<Control>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/RowLabels");

        for (int i = 0; i < 9; i++)
        {
            var label = new Label();
            label.Text = (i + 1).ToString();
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 14);
            label.AddThemeColorOverride("font_color", colors.TextSecondary);
            _rowLabels[i] = label;
            rowLabelsContainer.AddChild(label);
        }
    }

    private void UpdateAxisLabelsForGridSize()
    {
        if (_gameState == null) return;

        int gridSize = _gameState.GridSize;

        // Hide labels for cells beyond grid size
        for (int i = 0; i < 9; i++)
        {
            if (_colLabels[i] != null)
                _colLabels[i].Visible = i < gridSize;
            if (_rowLabels[i] != null)
                _rowLabels[i].Visible = i < gridSize;
        }

        // Position labels after frame so cells are laid out
        CallDeferred(nameof(PositionAxisLabels));
    }

    private void PositionAxisLabels()
    {
        if (_gameState == null || _cellButtons == null) return;

        int gridSize = _gameState.GridSize;
        var colLabelsContainer = GetNode<Control>("VBoxContainer/GridCenterContainer/GridWrapper/ColLabelsContainer/ColLabels");
        var rowLabelsContainer = GetNode<Control>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/RowLabels");

        // Position column labels (A-I) centered above each cell
        for (int col = 0; col < gridSize; col++)
        {
            if (_colLabels[col] == null) continue;

            var cell = _cellButtons[0, col];
            var cellLocalPos = cell.Position; // Position relative to GridContainer
            var cellSize = cell.Size;

            // Calculate position relative to ColLabels container
            // Cell is inside GridPanel which has 4px padding
            float x = 4 + cellLocalPos.X + (cellSize.X / 2) - (_colLabels[col].Size.X / 2);
            _colLabels[col].Position = new Vector2(x, 0);
        }

        // Position row labels (1-9) centered beside each cell
        for (int row = 0; row < gridSize; row++)
        {
            if (_rowLabels[row] == null) continue;

            var cell = _cellButtons[row, 0];
            var cellLocalPos = cell.Position;
            var cellSize = cell.Size;

            // Calculate position relative to RowLabels container
            // Cell is inside GridPanel which has 4px padding
            float y = 4 + cellLocalPos.Y + (cellSize.Y / 2) - (_rowLabels[row].Size.Y / 2);
            _rowLabels[row].Position = new Vector2(0, y);
        }
    }

    private void LoadGame()
    {
        CleanupHistoryNavBar();
        _gameState = _appState.CurrentGame;
        _isHistoryReplay = _appState.IsHistoryReplay;

        if (_gameState == null)
        {
            GD.PrintErr("Kein Spielstand gefunden!");
            _appState.GoToMainMenu();
            return;
        }

        if (_isHistoryReplay)
        {
            _isGameOver = true;   // lock out normal interactions
            _isPaused = true;
        }

        _elapsedTime = _gameState.ElapsedSeconds;
        _lastTimerSecond = -1; // Force immediate timer display update

        // Grid und NumberPad für aktuelle GridSize erstellen
        RecreateGridForGameState();
        RecreateNumberPadForGameState();

        // UI aktualisieren
        UpdateDifficultyLabel();
        UpdateMistakesLabel();
        UpdateGrid();
        UpdateNumberCounts();

        ApplyChallengeUi();

        if (_isHistoryReplay)
        {
            BuildHistoryReplay();
            if (_historySnapshots.Count > 0)
            {
                CreateHistoryNavButtons();
                ApplyHistoryStep(0);
                DisableInteractiveControlsForReplay();
            }
            else
            {
                GD.Print("[Replay] No history snapshots available, skipping replay controls.");
            }
        }
    }

    private void ApplyChallengeUi()
    {
        if (_gameState == null) return;
        if (_isHistoryReplay)
        {
            if (_hintButton != null) _hintButton.Disabled = true;
            if (_notesButton != null) _notesButton.Disabled = true;
            if (_houseAutoFillButton != null) _houseAutoFillButton.Disabled = true;
            if (_autoCandidatesButton != null) _autoCandidatesButton.Disabled = true;
            if (_solutionPathButton != null) _solutionPathButton.Disabled = true;
            return;
        }
        if (_notesButton != null)
        {
            _notesButton.Visible = !_gameState.ChallengeNoNotes;
        }

        if (_houseAutoFillButton != null)
        {
            _houseAutoFillButton.Visible = !_gameState.ChallengeNoNotes && _saveService.Settings.HouseAutoFillEnabled;
            _houseAutoFillButton.Disabled = _gameState.ChallengeNoNotes;
        }

        if (_hintButton != null)
        {
            bool is9x9 = _gameState.GridSize == 9;
            int limit = _gameState.ChallengeHintLimit;
            int used = _gameState.HintsUsed;
            int remaining = limit > 0 ? Math.Max(0, limit - used) : -1;

            _hintButton.Visible = is9x9;
            _hintButton.Disabled = !is9x9;

            _hintButton.TooltipText = !is9x9
                ? _localizationService.Get("game.hint.9x9_only")
                : (limit > 0
                    ? $"{_localizationService.Get("game.hint.tooltip")}\n{_localizationService.Get("game.hints")}: {used}/{limit}"
                    : _localizationService.Get("game.hint.tooltip"));

            // Visueller Hint wenn Limit erreicht
            var colors = _themeService.CurrentColors;
            _hintButton.AddThemeColorOverride("font_color", (limit > 0 && remaining == 0) ? colors.TextSecondary : colors.TextPrimary);
        }
    }

    private void UpdateDifficultyLabel()
    {
        if (_gameState == null) return;

        string diffText = _localizationService.GetDifficultyName(_gameState.Difficulty);
        string diffLabel = _localizationService.Get("game.difficulty");

        _difficultyLabel.Text = $"{diffLabel}: {diffText}";

        if (_gameState.IsDeadlyMode)
        {
            _difficultyLabel.Text += $" {_localizationService.Get("game.deadly")}";
        }

        if (_gameState.IsDaily)
        {
            _difficultyLabel.Text += $" {_localizationService.Get("game.daily")}";
        }

        // Szenario-Technik anzeigen
        if (!string.IsNullOrEmpty(_gameState.ScenarioTechnique) &&
            TechniqueInfo.Techniques.TryGetValue(_gameState.ScenarioTechnique, out var technique))
        {
            _difficultyLabel.Text += $" 🎯 {_localizationService.GetTechniqueName(_gameState.ScenarioTechnique)}";
        }

        var tags = new List<string>();
        if (_gameState.ChallengeNoNotes) tags.Add(_localizationService.Get("settings.challenge.no_notes"));
        if (_gameState.ChallengePerfectRun) tags.Add(_localizationService.Get("settings.challenge.perfect"));
        if (_gameState.ChallengeHintLimit > 0) tags.Add(_localizationService.Get("game.challenge.tag.hints", _gameState.ChallengeHintLimit));
        if (_gameState.ChallengeTimeAttackSeconds > 0) tags.Add(_localizationService.Get("game.challenge.tag.time", _gameState.ChallengeTimeAttackSeconds / 60));
        if (tags.Count > 0)
        {
            _difficultyLabel.Text += " [" + string.Join(", ", tags) + "]";
        }
    }

    private void UpdateTimerDisplay()
    {
        int currentSecond = (int)_elapsedTime;
        if (currentSecond == _lastTimerSecond) return;
        _lastTimerSecond = currentSecond;

        var ts = TimeSpan.FromSeconds(_elapsedTime);
        string elapsedText = ts.Hours > 0
            ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";

        if (_gameState != null && _gameState.ChallengeTimeAttackSeconds > 0)
        {
            double remaining = Math.Max(0, _gameState.ChallengeTimeAttackSeconds - _elapsedTime);
            var rt = TimeSpan.FromSeconds(remaining);
            string remainingText = rt.Hours > 0
                ? $"{rt.Hours:D2}:{rt.Minutes:D2}:{rt.Seconds:D2}"
                : $"{rt.Minutes:D2}:{rt.Seconds:D2}";
            _timerLabel.Text = _localizationService.Get("game.timer.remaining", elapsedText, remainingText);
        }
        else
        {
            _timerLabel.Text = elapsedText;
        }
    }

    private void UpdateMistakesLabel()
    {
        if (_gameState == null) return;

        string mistakesText = _gameState.IsDeadlyMode
            ? $"{_gameState.Mistakes}/3"
            : _gameState.Mistakes.ToString();

        _mistakesLabel.Text = _localizationService.Get("game.mistakes.label", mistakesText);
    }

    #region Tutorial Support

    private void SetupTutorialOverlay()
    {
        if (_tutorialService == null) return;

        // Create and add overlay
        _tutorialOverlay = new TutorialOverlay();
        _overlayContainer.AddChild(_tutorialOverlay);

        // Provide UI element references
        _tutorialOverlay.GridContainer = _gridContainer;
        _tutorialOverlay.NumberPad = _numberPad;
        _tutorialOverlay.BackButton = _backButton;
        _tutorialOverlay.TimerLabel = _timerLabel;
        _tutorialOverlay.MistakesLabel = _mistakesLabel;
        _tutorialOverlay.DifficultyLabel = _difficultyLabel;
        _tutorialOverlay.NotesToggle = _notesButton;
        _tutorialOverlay.HintButton = _hintButton;
        _tutorialOverlay.AutoNotesButton = _autoCandidatesButton;
        _tutorialOverlay.EraseButton = _numberButtons[0];
        _tutorialOverlay.HouseAutoFillButton = _houseAutoFillButton;

        // Provide callback to get cell rectangles
        _tutorialOverlay.GetCellRect = GetCellGlobalRect;

        // Provide axis label references
        _tutorialOverlay.ColLabels = _colLabels;
        _tutorialOverlay.RowLabels = _rowLabels;

        // Start tutorial if this is a tutorial game
        if (_gameState?.IsTutorial == true && !string.IsNullOrEmpty(_gameState.TutorialId))
        {
            GD.Print($"[GameScene] Starting tutorial: {_gameState.TutorialId}");
            // Capture tutorialId to avoid race condition (gameState could change)
            var tutorialId = _gameState.TutorialId;
            // Delay start slightly to ensure UI is fully ready
            GetTree().CreateTimer(0.3).Timeout += () =>
            {
                // Check if we're still on this scene and tutorial service is available
                if (IsInsideTree() && _tutorialService != null)
                {
                    _tutorialService.StartTutorial(tutorialId);
                }
            };
        }
    }

    /// <summary>
    /// Returns the global rectangle for a cell at the given row and column.
    /// Used by TutorialOverlay to draw highlights and arrows.
    /// </summary>
    private Rect2 GetCellGlobalRect(int row, int col)
    {
        if (_cellButtons == null || _gameState == null) return new Rect2();

        int gridSize = _gameState.GridSize;
        if (row < 0 || row >= gridSize || col < 0 || col >= gridSize) return new Rect2();

        var cellButton = _cellButtons[row, col];
        return new Rect2(cellButton.GlobalPosition, cellButton.Size);
    }

    /// <summary>
    /// Notifies the tutorial overlay when a user action occurs (for WaitForAction steps).
    /// </summary>
    private void NotifyTutorialAction(ExpectedAction action, (int Row, int Col)? cell = null, int? number = null)
    {
        _tutorialOverlay?.NotifyUserAction(action, cell, number);
    }

    /// <summary>
    /// Notifies the tutorial overlay when multiple cells are selected.
    /// </summary>
    private void NotifyTutorialMultiSelect(int? number = null)
    {
        _tutorialOverlay?.NotifyMultiSelectAction(_selectedCells, number);
    }

    #endregion

    private void UpdateGrid()
    {
        if (_gameState == null || _cellButtons == null) return;

        int gridSize = _gameState.GridSize;
        bool highlightRelatedCells = _saveService.Settings.HighlightRelatedCells;
        bool[] emptyCandidates = gridSize == 4 ? _emptyCandidates4 : _emptyCandidates9;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                var cell = _gameState.Grid[row, col];
                var button = _cellButtons[row, col];

                button.SetValue(cell.Value, cell.IsGiven);

                // Multi-Select: Prüfe ob Zelle in der Auswahl ist
                bool isSelected = _selectedCells.Contains((row, col));
                bool isPrimarySelection = row == _selectedRow && col == _selectedCol;
                button.SetSelected(isPrimarySelection);
                button.SetMultiSelected(isSelected && !isPrimarySelection);

                button.SetHighlighted(cell.Value != 0 && cell.Value == _highlightedNumber);

                // Related highlighting - NUR Zeile und Spalte, NICHT 3x3-Block
                bool isRelated = false;
                if (_selectedRow >= 0 && _selectedCol >= 0)
                {
                    if (highlightRelatedCells)
                    {
                        // Nur gleiche Zeile ODER gleiche Spalte (nicht die ausgewählte Zelle selbst)
                        isRelated = (row == _selectedRow || col == _selectedCol) &&
                            !(row == _selectedRow && col == _selectedCol) &&
                            !isSelected; // Nicht related wenn multi-selected
                    }
                }
                button.SetRelated(isRelated);

                // Notizen setzen (blau - vom Spieler gesetzt)
                button.SetNotes(cell.Notes, true);

                // Auto-Kandidaten berechnen und setzen (grau - automatisch)
                if (_showAutoCandidates && cell.Value == 0)
                {
                    bool[] candidates = CalculateCandidates(row, col);
                    button.SetCandidates(candidates, true);
                }
                else
                {
                    button.SetCandidates(emptyCandidates, false);
                }

                bool isHistoryEntry = _isHistoryReplay && cell.Value != 0 && !cell.IsGiven;
                button.SetHistoryEntry(isHistoryEntry);
                bool isCurrentStepCell = _isHistoryReplay && _historyCurrentStepCells.Contains((row, col));
                button.SetHistoryCurrent(isCurrentStepCell);
            }
        }
    }

    private bool[] CalculateCandidates(int row, int col)
    {
        if (_gameState == null) return _candidatesPool;

        int gridSize = _gameState.GridSize;
        int blockSize = _gameState.BlockSize;

        // Reset pool array (use only up to gridSize elements)
        for (int i = 0; i < gridSize; i++)
            _candidatesPool[i] = true;

        // Zeile prüfen
        for (int c = 0; c < gridSize; c++)
        {
            int val = _gameState.Grid[row, c].Value;
            if (val > 0) _candidatesPool[val - 1] = false;
        }

        // Spalte prüfen
        for (int r = 0; r < gridSize; r++)
        {
            int val = _gameState.Grid[r, col].Value;
            if (val > 0) _candidatesPool[val - 1] = false;
        }

        // Block prüfen (2x2 für Kids, 3x3 für Standard)
        int blockRow = (row / blockSize) * blockSize;
        int blockCol = (col / blockSize) * blockSize;
        for (int r = blockRow; r < blockRow + blockSize; r++)
        {
            for (int c = blockCol; c < blockCol + blockSize; c++)
            {
                int val = _gameState.Grid[r, c].Value;
                if (val > 0) _candidatesPool[val - 1] = false;
            }
        }

        return _candidatesPool;
    }

    private void UpdateNumberCounts()
    {
        if (_gameState == null) return;

        var colors = _themeService.CurrentColors;
        bool hideCompleted = _saveService.Settings.HideCompletedNumbers;
        int gridSize = _gameState.GridSize;

        if (_isHistoryReplay)
        {
            for (int i = 1; i <= gridSize; i++)
            {
                if (_numberButtons[i] == null) continue;
                _numberButtons[i].Visible = true;
                _numberButtons[i].Disabled = true;
                _numberButtons[i].AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
                _numberButtons[i].AddThemeColorOverride("font_color", colors.TextPrimary);
            }

            if (_numberButtons[0] != null)
            {
                _numberButtons[0].Disabled = true;
            }

            return;
        }

        for (int i = 1; i <= gridSize; i++)
        {
            if (_numberButtons[i] == null) continue;

            int count = _gameState.CountNumber(i);
            bool isComplete = count >= gridSize;
            bool isHighlighted = i == _highlightedNumber;

            if (isComplete)
            {
                if (hideCompleted)
                {
                    _numberButtons[i].Visible = false;
                }
                else
                {
                    _numberButtons[i].Visible = true;
                    _numberButtons[i].Disabled = true;
                }
            }
            else
            {
                _numberButtons[i].Visible = true;
                _numberButtons[i].Disabled = false;
            }

            // Highlight aktive Zahl im Numpad
            if (isHighlighted && !isComplete)
            {
                var highlightStyle = _themeService.CreateButtonStyleBox();
                highlightStyle.BgColor = colors.CellBackgroundHighlighted;
                _numberButtons[i].AddThemeStyleboxOverride("normal", highlightStyle);
                _numberButtons[i].AddThemeColorOverride("font_color", colors.TextPrimary);
            }
            else if (!isComplete)
            {
                _numberButtons[i].AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
                _numberButtons[i].AddThemeColorOverride("font_color", colors.TextPrimary);
            }
        }
    }

    private void OnCellClicked(int row, int col)
    {
        if (_isGameOver || _gameState == null) return;

        // During tutorials, block grid input unless specifically waiting for it
        if (_tutorialService != null && !_tutorialService.IsGridInputAllowed)
        {
            GD.Print("[Game] Grid input blocked during tutorial");
            return;
        }

        _audioService.PlayCellSelect();

        var cell = _gameState.Grid[row, col];
        bool ctrlPressed = Input.IsKeyPressed(Key.Ctrl);
        bool shiftPressed = Input.IsKeyPressed(Key.Shift);

        string cellRef = ToCellRef(row, col);

        // Ctrl+Klick: Zur Mehrfachauswahl hinzufügen/entfernen
        if (ctrlPressed)
        {
            if (_selectedCells.Contains((row, col)))
            {
                _selectedCells.Remove((row, col));
                GD.Print($"[Game] Cell {cellRef} removed from multi-selection (Ctrl+click), selected={_selectedCells.Count}");
                // Wenn die Hauptauswahl entfernt wurde, neue setzen
                if (row == _selectedRow && col == _selectedCol)
                {
                    if (_selectedCells.Count > 0)
                    {
                        var first = _selectedCells.First();
                        _selectedRow = first.row;
                        _selectedCol = first.col;
                    }
                    else
                    {
                        _selectedRow = -1;
                        _selectedCol = -1;
                    }
                }
            }
            else
            {
                _selectedCells.Add((row, col));
                _selectedRow = row;
                _selectedCol = col;
                GD.Print($"[Game] Cell {cellRef} added to multi-selection (Ctrl+click), selected={_selectedCells.Count}");
            }
        }
        // Shift+Klick: Bereich auswählen
        else if (shiftPressed && _selectedRow >= 0 && _selectedCol >= 0)
        {
            SelectRange(_selectedRow, _selectedCol, row, col);
            _selectedRow = row;
            _selectedCol = col;
            GD.Print($"[Game] Range selected (Shift+click) to {cellRef}, selected={_selectedCells.Count}");
        }
        // Normaler Klick: Einzelauswahl
        else
        {
            _selectedCells.Clear();
            _selectedCells.Add((row, col));
            _selectedRow = row;
            _selectedCol = col;
            GD.Print($"[Game] Cell {cellRef} selected (value={cell.Value}, isGiven={cell.IsGiven})");

            // Drag starten
            _isDragging = true;
            _dragStart = (row, col);
        }

        // Wenn auf eine Zahl geklickt wird, highlight diese
        if (cell.Value != 0)
        {
            _highlightedNumber = cell.Value;
        }
        else
        {
            _highlightedNumber = 0;
        }

        UpdateGrid();
        UpdateNumberCounts(); // Highlight im Numpad aktualisieren

        // Tutorial notification
        NotifyTutorialAction(ExpectedAction.SelectCell, (row, col));

        // Multi-select notification for tutorial
        if (_selectedCells.Count > 1)
        {
            NotifyTutorialMultiSelect();
        }
    }

    private void OnCellHovered(int row, int col)
    {
        // During tutorials, block grid input unless specifically waiting for it
        if (_tutorialService != null && !_tutorialService.IsGridInputAllowed) return;

        // Drag-Auswahl: Wenn Maus gedrückt und über andere Zelle
        if (_isDragging && _dragStart.HasValue && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            // Bereich von Drag-Start bis aktuelle Zelle auswählen
            SelectRange(_dragStart.Value.row, _dragStart.Value.col, row, col);
            _selectedRow = row;
            _selectedCol = col;
            UpdateGrid();
        }
    }

    private void SelectRange(int startRow, int startCol, int endRow, int endCol)
    {
        int minRow = Math.Min(startRow, endRow);
        int maxRow = Math.Max(startRow, endRow);
        int minCol = Math.Min(startCol, endCol);
        int maxCol = Math.Max(startCol, endCol);

        _selectedCells.Clear();
        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                _selectedCells.Add((r, c));
            }
        }
    }

    private void OnNumberPadPressed(int number)
    {
        if (_selectedRow >= 0 && _selectedCol >= 0)
        {
            string cellRef = ToCellRef(_selectedRow, _selectedCol);
            GD.Print($"[Game] NumberPad pressed: {(number == 0 ? "Erase" : number.ToString())} at {cellRef}, notesMode={_isNotesMode}, multiSelect={_selectedCells.Count}");
            TrySetNumberOnSelection(number);
        }
    }

    private void TrySetNumber(int number)
    {
        if (_gameState == null || _isGameOver) return;
        if (_selectedRow < 0 || _selectedCol < 0) return;

        var cell = _gameState.Grid[_selectedRow, _selectedCol];

        // Given-Zellen können nicht geändert werden
        if (cell.IsGiven) return;

        // Wenn bereits ein Wert gesetzt ist, kann nur gelöscht werden (nicht Notizen)
        if (cell.Value != 0 && number != 0 && !_isNotesMode)
        {
            // Zelle ist bereits gefüllt – ignorieren (nur Löschen oder Notizen erlaubt)
            return;
        }

        // Notizen-Modus
        if (_isNotesMode && number > 0)
        {
            // Nur wenn die Zelle leer ist
            if (cell.Value != 0) return;

            // Toggle die Notiz (bounds check for Kids mode)
            int idx = number - 1;
            if (idx < 0 || idx >= cell.Notes.Length) return;
            bool wasSet = cell.Notes[idx];
            cell.Notes[idx] = !cell.Notes[idx];
            GD.Print($"[Game] Note {number} {(wasSet ? "removed" : "added")} at {ToCellRef(_selectedRow, _selectedCol)}");
            _audioService.PlayNotePlaceOrRemove(!wasSet);
            SaveAndUpdate();

            // Tutorial notification for note toggle
            NotifyTutorialAction(ExpectedAction.ToggleNote, (_selectedRow, _selectedCol), number);
            return;
        }

        // Löschen erlauben
        if (number == 0)
        {
            GD.Print($"[Game] Number removed at {ToCellRef(_selectedRow, _selectedCol)} (was {cell.Value})");
            cell.Value = 0;
            // Auch alle Notizen löschen
            for (int i = 0; i < 9; i++)
                cell.Notes[i] = false;
            _highlightedNumber = 0;
            _audioService.PlayNumberRemove();
            SaveAndUpdate();

            // Tutorial notification for erase
            NotifyTutorialAction(ExpectedAction.EraseCell, (_selectedRow, _selectedCol));
            return;
        }

        // Im normalen Modus: Notizen löschen wenn ein Wert gesetzt wird
        for (int i = 0; i < 9; i++)
            cell.Notes[i] = false;

        // Prüfen ob die Zahl korrekt ist
        if (number != cell.Solution)
        {
            // Fehler!
            GD.Print($"[Game] WRONG number {number} at {ToCellRef(_selectedRow, _selectedCol)} (expected {cell.Solution})");
            _audioService.PlayError();
            RecordMistakeForHeatmap(_selectedRow, _selectedCol);

            // Perfect Run => sofort verloren
            if (_gameState.ChallengePerfectRun)
            {
                _isGameOver = true;
                _gameState.ElapsedSeconds = _elapsedTime;
                _appState.EndGame(GameStatus.Lost);
                ShowPerfectRunFailedOverlay();
                return;
            }
            bool gameOver = _appState.RegisterMistake();

            UpdateMistakesLabel();

            // Visuelles Feedback
            if (_cellButtons != null)
            {
                _cellButtons[_selectedRow, _selectedCol].FlashError();
            }

            if (gameOver)
            {
                // Game Over!
                _isGameOver = true;
                _gameState.ElapsedSeconds = _elapsedTime;
                _appState.EndGame(GameStatus.Lost);
                ShowGameOverOverlay();
                return;
            }
            else
            {
                // Game still ongoing - show tutor for feedback
                MaybeShowTutorOverlay(number);
            }

            // Tutorial notification for wrong number (also matches EnterAnyNumber)
            NotifyTutorialAction(ExpectedAction.EnterWrongNumber, (_selectedRow, _selectedCol), number);
            NotifyTutorialAction(ExpectedAction.EnterAnyNumber, (_selectedRow, _selectedCol), number);
        }
        else
        {
            // Korrekte Zahl
            GD.Print($"[Game] CORRECT number {number} placed at {ToCellRef(_selectedRow, _selectedCol)}");
            _audioService.PlayNumberPlace();
            cell.Value = number;
            _highlightedNumber = number;

            MaybeSmartCleanupNotesAfterPlacement(_selectedRow, _selectedCol, number);

            // Tutorial notification for correct number (also matches EnterAnyNumber)
            NotifyTutorialAction(ExpectedAction.EnterCorrectNumber, (_selectedRow, _selectedCol), number);
            NotifyTutorialAction(ExpectedAction.EnterAnyNumber, (_selectedRow, _selectedCol), number);

            // Technique tracking: applied? (only placement hints)
            if (_lastHintTracking.HasValue)
            {
                var h = _lastHintTracking.Value;
                if (h.row == _selectedRow && h.col == _selectedCol && h.value == number)
                {
                    _saveService.Settings.IncrementTechniqueApplied(h.techniqueId);
                    _saveService.SaveSettings();
                    _lastHintTracking = null;
                }
            }

            // Prüfen ob gewonnen
            if (_gameState.IsComplete())
            {
                _isGameOver = true;
                _gameState.ElapsedSeconds = _elapsedTime;
                _appState.EndGame(GameStatus.Won);
                ShowWinOverlay();
                return;
            }
        }

        SaveAndUpdate();
    }

    private void MaybeSmartCleanupNotesAfterPlacement(int row, int col, int placedNumber)
    {
        if (_gameState == null) return;
        if (placedNumber <= 0) return;

        if (!_saveService.Settings.SmartNoteCleanupEnabled) return;

        int size = _gameState.GridSize;
        int blockSize = _gameState.BlockSize;

        int idx = placedNumber - 1;
        if (idx < 0 || idx >= 9) return;

        // Row
        for (int c = 0; c < size; c++)
        {
            if (c == col) continue;
            var peer = _gameState.Grid[row, c];
            if (peer.Value != 0) continue;
            peer.Notes[idx] = false;
        }

        // Column
        for (int r = 0; r < size; r++)
        {
            if (r == row) continue;
            var peer = _gameState.Grid[r, col];
            if (peer.Value != 0) continue;
            peer.Notes[idx] = false;
        }

        // Block
        int startRow = (row / blockSize) * blockSize;
        int startCol = (col / blockSize) * blockSize;
        for (int r = startRow; r < startRow + blockSize; r++)
        {
            for (int c = startCol; c < startCol + blockSize; c++)
            {
                if (r == row && c == col) continue;
                var peer = _gameState.Grid[r, c];
                if (peer.Value != 0) continue;
                peer.Notes[idx] = false;
            }
        }
    }

    private void RecordMistakeForHeatmap(int row, int col)
    {
        if (_gameState == null) return;
        _saveService.Settings.RecordMistake(_gameState.GridSize, row, col);
        _saveService.SaveSettings();
    }

    private void MaybeShowTutorOverlay(int attemptedNumber)
    {
        if (_gameState == null) return;
        if (!_saveService.Settings.LearnModeEnabled) return;

        // Don't show tutor during tutorials - the tutorial has its own explanations
        if (_gameState.IsTutorial) return;

        var cell = _gameState.Grid[_selectedRow, _selectedCol];
        string cellRef = ToCellRef(_selectedRow, _selectedCol);
        bool violatesRules = !_gameState.IsValidPlacement(_selectedRow, _selectedCol, attemptedNumber);

        string body;
        if (violatesRules)
        {
            var conflicts = FindRuleConflicts(_selectedRow, _selectedCol, attemptedNumber);
            body = $"Cell {cellRef}: {attemptedNumber} conflicts with {string.Join(", ", conflicts)}.";
        }
        else
        {
            body = _localizationService.Get("game.learn.wrong_solution", cellRef, attemptedNumber);
        }

        body += _localizationService.Get("game.learn.hint", cellRef, cell.Solution);

        ShowDismissableOverlay("📘 Tutor", body);
    }

    private List<string> FindRuleConflicts(int row, int col, int number)
    {
        var conflicts = new List<string>();
        if (_gameState == null) return conflicts;

        int size = _gameState.GridSize;
        int blockSize = _gameState.BlockSize;

        // Use HashSet for deduplication (avoid LINQ Distinct allocation)
        var seen = new HashSet<string>();

        // Check row
        for (int c = 0; c < size; c++)
        {
            if (c == col) continue;
            if (_gameState.Grid[row, c].Value == number)
            {
                string cellRef = ToCellRef(row, c);
                if (seen.Add(cellRef))
                    conflicts.Add(cellRef);
            }
        }

        // Check column
        for (int r = 0; r < size; r++)
        {
            if (r == row) continue;
            if (_gameState.Grid[r, col].Value == number)
            {
                string cellRef = ToCellRef(r, col);
                if (seen.Add(cellRef))
                    conflicts.Add(cellRef);
            }
        }

        // Check block
        int br = (row / blockSize) * blockSize;
        int bc = (col / blockSize) * blockSize;
        for (int r = br; r < br + blockSize; r++)
        {
            for (int c = bc; c < bc + blockSize; c++)
            {
                if (r == row && c == col) continue;
                if (_gameState.Grid[r, c].Value == number)
                {
                    string cellRef = ToCellRef(r, c);
                    if (seen.Add(cellRef))
                        conflicts.Add(cellRef);
                }
            }
        }

        if (conflicts.Count == 0) conflicts.Add(_localizationService.Get("game.learn.sudoku_rules"));
        return conflicts;
    }

    private void ShowPerfectRunFailedOverlay()
    {
        var overlay = CreateOverlay(
            _localizationService.Get("game.challenge.perfect.title"),
            _localizationService.Get("game.challenge.perfect.message"),
            new Color("f44336")
        );
        _overlayContainer.AddChild(overlay);
    }

    private void ShowTimeAttackOverlay()
    {
        var overlay = CreateOverlay(
            _localizationService.Get("game.challenge.time.title"),
            _localizationService.Get("game.challenge.time.message"),
            new Color("f44336")
        );
        _overlayContainer.AddChild(overlay);
    }

    private void ShowSimpleOverlay(string title, string message)
    {
        var overlay = CreateOverlay(title, message, new Color("64b5f6"));
        _overlayContainer.AddChild(overlay);
    }

    private void ShowDismissableOverlay(string title, string message)
    {
        var overlay = CreateDismissableOverlay(title, message, new Color("64b5f6"));
        _overlayContainer.AddChild(overlay);
    }

    private void SaveAndUpdate()
    {
        if (_gameState == null) return;

        _gameState.ElapsedSeconds = _elapsedTime;
        _appState.SaveGame();

        UpdateGrid();
        UpdateNumberCounts();
    }

    private void ShowWinOverlay()
    {
        _audioService.PlayWin();
        var overlay = CreateOverlay(_localizationService.Get("dialog.win.title"),
            _localizationService.Get("game.win.message", _timerLabel.Text, _gameState?.Mistakes ?? 0),
            new Color("4caf50"));
        _overlayContainer.AddChild(overlay);
    }

    private void ShowGameOverOverlay()
    {
        var overlay = CreateOverlay(_localizationService.Get("dialog.lose.title"),
            _localizationService.Get("dialog.lose.message"),
            new Color("f44336"));
        _overlayContainer.AddChild(overlay);
    }

    private Control CreateOverlay(string title, string message, Color accentColor)
    {
        var colors = _themeService.CurrentColors;

        // Hintergrund
        var bgRect = new ColorRect();
        bgRect.Color = new Color(0, 0, 0, 0.7f);
        bgRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        // Panel
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bgRect.AddChild(centerContainer);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(350, 250);
        var panelStyle = _themeService.CreatePanelStyleBox(16, 32);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        centerContainer.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(vbox);

        // Title
        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        titleLabel.AddThemeColorOverride("font_color", accentColor);
        vbox.AddChild(titleLabel);

        // Message
        var messageLabel = new Label();
        messageLabel.Text = message;
        messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
        messageLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        vbox.AddChild(messageLabel);

        // Spacer
        var spacer = new Control();
        spacer.SizeFlagsVertical = Control.SizeFlags.Expand;
        vbox.AddChild(spacer);

        // Button
        var button = new Button();
        button.Text = _localizationService.Get("common.back_to_menu");
        button.CustomMinimumSize = new Vector2(0, 50);
        ApplyButtonStyle(button, includeDisabled: false);
        button.Pressed += () => {
            _appState.GoToMainMenu();
        };
        vbox.AddChild(button);

        return bgRect;
    }

    private Control CreateDismissableOverlay(string title, string message, Color accentColor)
    {
        var colors = _themeService.CurrentColors;

        // Hintergrund
        var bgRect = new ColorRect();
        bgRect.Color = new Color(0, 0, 0, 0.7f);
        bgRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        // Panel
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bgRect.AddChild(centerContainer);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(350, 250);
        var panelStyle = _themeService.CreatePanelStyleBox(16, 32);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        centerContainer.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(vbox);

        // Title
        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        titleLabel.AddThemeColorOverride("font_color", accentColor);
        vbox.AddChild(titleLabel);

        // Message
        var messageLabel = new Label();
        messageLabel.Text = message;
        messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
        messageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        messageLabel.CustomMinimumSize = new Vector2(300, 0);
        messageLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        vbox.AddChild(messageLabel);

        // Spacer
        var spacer = new Control();
        spacer.SizeFlagsVertical = Control.SizeFlags.Expand;
        vbox.AddChild(spacer);

        // Button
        var button = new Button();
        button.Text = _localizationService.Get("common.close");
        button.CustomMinimumSize = new Vector2(0, 50);
        ApplyButtonStyle(button, includeDisabled: false);
        button.Pressed += () => {
            bgRect.QueueFree();
        };
        vbox.AddChild(button);

        return bgRect;
    }

    private void OnBackPressed()
    {
        GD.Print("[Game] Back button pressed - returning to menu");
        _audioService.PlayClick();

        // Spiel speichern
        if (_gameState != null && !_isGameOver && !_isHistoryReplay)
        {
            _gameState.ElapsedSeconds = _elapsedTime;
            _appState.SaveGame();
        }

        // Prefer returning to the scene we came from (e.g. Puzzles/Scenarios/Tutorial overview)
        if (_appState.TryReturnToCapturedScene())
            return;

        // Fallback: main menu
        _appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void OnLanguageChanged(int languageIndex)
    {
        ApplyLocalization();

        // Also refresh dynamic labels/tooltips that include localized pieces
        UpdateDifficultyLabel();
        UpdateMistakesLabel();
        ApplyChallengeUi();

        // Force a timer refresh so "remaining" label updates immediately
        _lastTimerSecond = -1;
        UpdateTimerDisplay();
        UpdateHistoryNavButtonState();
    }

    private void ApplyLocalization()
    {
        var loc = _localizationService;
        _backButton.Text = loc.Get("menu.back");
        _backButton.TooltipText = loc.Get("game.back.tooltip");

        if (_solutionPathButton != null)
            _solutionPathButton.TooltipText = loc.Get("game.solutionpath.tooltip");
        if (_solutionPathHeader != null && _solutionPath != null)
            _solutionPathHeader.Text = loc.Get("game.solutionpath.title") + $" — {_solutionPath.Status} ({_solutionPath.Steps.Count} steps)";
    }

    private void BuildHistoryReplay()
    {
        if (_gameState == null)
            return;

        if (_gameState.GridSize != 9)
        {
            GD.Print("[Replay] History replay is available for 9x9 games only.");
            return;
        }

        try
        {
            var path = SolutionPathService.BuildPath(_gameState.Clone());
            _historySnapshots.Clear();
            _historySnapshots.Add(SnapshotGrid(_gameState));
            foreach (var step in path.Steps)
            {
                _historySnapshots.Add(step.GridSnapshot);
            }
            _historyStepIndex = 0;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Replay] Failed to build replay path: {ex.Message}");
            _historySnapshots.Clear();
        }
    }

    private void ApplyHistoryStep(int index)
    {
        if (_gameState == null) return;
        if (_historySnapshots.Count == 0) return;

        _historyStepIndex = Math.Clamp(index, 0, _historySnapshots.Count - 1);
        var snap = _historySnapshots[_historyStepIndex];
        var prevSnap = _historyStepIndex > 0 ? _historySnapshots[_historyStepIndex - 1] : null;

        int size = _gameState.GridSize;
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                _gameState.Grid[r, c].Value = snap[r, c];
            }
        }

        CaptureHistoryStepChanges(prevSnap, snap);
        UpdateGrid();
        UpdateNumberCounts();
        UpdateHistoryNavButtonState();
    }

    private void CleanupHistoryNavBar()
    {
        if (_historyNavBar != null)
        {
            _historyNavBar.QueueFree();
            _historyNavBar = null;
        }

        if (_historyJumpDialog != null)
        {
            _historyJumpDialog.QueueFree();
            _historyJumpDialog = null;
            _historyJumpInput = null;
        }

        _historyFirstButton = null;
        _historyPrevButton = null;
        _historyNextButton = null;
        _historyLastButton = null;
        _historyStepsButton = null;
        ResetHistoryHighlightState();
    }

    private void CreateHistoryNavButtons()
    {
        if (_gridWrapper == null) return;

        CleanupHistoryNavBar();

        var navBar = new HBoxContainer();
        navBar.Name = "HistoryReplayBar";
        navBar.Alignment = BoxContainer.AlignmentMode.Center;
        navBar.AddThemeConstantOverride("separation", 20);
        navBar.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        navBar.CustomMinimumSize = new Vector2(0, 52);

        _historyFirstButton = new Button();
        _historyFirstButton.Text = "<<";
        _historyFirstButton.CustomMinimumSize = new Vector2(64, 48);
        _historyFirstButton.FocusMode = FocusModeEnum.None;
        ApplyButtonStyle(_historyFirstButton, includeDisabled: true);
        _historyFirstButton.Pressed += () => ApplyHistoryStep(0);

        _historyPrevButton = new Button();
        _historyPrevButton.Text = "<";
        _historyPrevButton.CustomMinimumSize = new Vector2(64, 48);
        _historyPrevButton.FocusMode = FocusModeEnum.None;
        ApplyButtonStyle(_historyPrevButton, includeDisabled: true);
        _historyPrevButton.Pressed += () => ApplyHistoryStep(_historyStepIndex - 1);

        _historyStepsButton = new Button();
        _historyStepsButton.FocusMode = FocusModeEnum.None;
        _historyStepsButton.CustomMinimumSize = new Vector2(120, 48);
        ApplyButtonStyle(_historyStepsButton, includeDisabled: false);
        _historyStepsButton.Pressed += ShowHistoryJumpDialog;

        _historyNextButton = new Button();
        _historyNextButton.Text = ">";
        _historyNextButton.CustomMinimumSize = new Vector2(64, 48);
        _historyNextButton.FocusMode = FocusModeEnum.None;
        ApplyButtonStyle(_historyNextButton, includeDisabled: true);
        _historyNextButton.Pressed += () => ApplyHistoryStep(_historyStepIndex + 1);

        _historyLastButton = new Button();
        _historyLastButton.Text = ">>";
        _historyLastButton.CustomMinimumSize = new Vector2(64, 48);
        _historyLastButton.FocusMode = FocusModeEnum.None;
        ApplyButtonStyle(_historyLastButton, includeDisabled: true);
        _historyLastButton.Pressed += () => ApplyHistoryStep(_historySnapshots.Count - 1);

        navBar.AddChild(_historyFirstButton);
        navBar.AddChild(_historyPrevButton);
        navBar.AddChild(_historyStepsButton);
        navBar.AddChild(_historyNextButton);
        navBar.AddChild(_historyLastButton);

        _gridWrapper.AddChild(navBar);
        _gridWrapper.MoveChild(navBar, 0);
        _historyNavBar = navBar;

        BuildHistoryJumpDialog();
        UpdateHistoryNavButtonState();
    }

    private void UpdateHistoryNavButtonState()
    {
        bool atStart = _historyStepIndex <= 0;
        bool atEnd = _historyStepIndex >= _historySnapshots.Count - 1;

        if (_historyFirstButton != null)
            _historyFirstButton.Disabled = atStart;
        if (_historyPrevButton != null)
            _historyPrevButton.Disabled = atStart;
        if (_historyNextButton != null)
            _historyNextButton.Disabled = atEnd;
        if (_historyLastButton != null)
            _historyLastButton.Disabled = atEnd;

        UpdateHistoryStepsLabel();
    }

    private void UpdateHistoryStepsLabel()
    {
        if (_historyStepsButton == null || _historySnapshots.Count == 0) return;

        int totalSteps = Math.Max(1, _historySnapshots.Count);
        int currentStep = Math.Clamp(_historyStepIndex + 1, 1, totalSteps);
        _historyStepsButton.Text = _localizationService.Get("history.replay.steps", currentStep, totalSteps);
        if (_historyJumpInput != null)
        {
            _historyJumpInput.PlaceholderText = $"1-{totalSteps}";
        }
    }

    private void BuildHistoryJumpDialog()
    {
        if (_overlayContainer == null) return;

        var dialog = new AcceptDialog();
        dialog.Title = _localizationService.Get("history.replay.jump.title", "Jump to step");
        dialog.MinSize = new Vector2I(360, 140);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        dialog.AddChild(vbox);

        var label = new Label();
        label.Text = _localizationService.Get("history.replay.jump.prompt", "Enter step number:");
        vbox.AddChild(label);

        _historyJumpInput = new LineEdit();
        _historyJumpInput.PlaceholderText = "1-1";
        _historyJumpInput.ExpandToTextLength = false;
        _historyJumpInput.TextSubmitted += OnHistoryJumpSubmitted;
        vbox.AddChild(_historyJumpInput);

        dialog.Confirmed += OnHistoryJumpConfirmed;

        _overlayContainer.AddChild(dialog);
        _historyJumpDialog = dialog;
    }

    private void ShowHistoryJumpDialog()
    {
        if (_historyJumpDialog == null || _historyJumpInput == null) return;

        _historyJumpInput.Text = (_historyStepIndex + 1).ToString();
        _historyJumpDialog.PopupCentered(new Vector2I(360, 140));
        _historyJumpInput.GrabFocus();
        _historyJumpInput.CaretColumn = _historyJumpInput.Text.Length;
    }

    private void OnHistoryJumpSubmitted(string text)
    {
        ConfirmHistoryJump(text);
    }

    private void OnHistoryJumpConfirmed()
    {
        ConfirmHistoryJump(_historyJumpInput?.Text ?? "");
    }

    private void ConfirmHistoryJump(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!int.TryParse(text, out int step)) return;

        int total = Math.Max(1, _historySnapshots.Count);
        int targetIndex = Math.Clamp(step - 1, 0, total - 1);
        ApplyHistoryStep(targetIndex);
        _historyJumpDialog?.Hide();
    }

    private void CaptureHistoryStepChanges(int[,]? previous, int[,] current)
    {
        _historyCurrentStepCells.Clear();
        if (previous == null) return;

        int size = current.GetLength(0);
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (current[row, col] != previous[row, col])
                {
                    _historyCurrentStepCells.Add((row, col));
                }
            }
        }
    }

    private void ResetHistoryHighlightState()
    {
        _historyCurrentStepCells.Clear();
    }

    private void DisableInteractiveControlsForReplay()
    {
        if (_numberPad != null)
        {
            foreach (var child in _numberPad.GetChildren())
            {
                if (child is Button btn) btn.Disabled = true;
            }
        }
        if (_gridPanel != null)
        {
            _gridPanel.MouseFilter = MouseFilterEnum.Ignore;
        }

        if (_cellButtons != null)
        {
            int rows = _cellButtons.GetLength(0);
            int cols = _cellButtons.GetLength(1);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    _cellButtons[r, c].Disabled = true;
                }
            }
        }
    }

    private static int[,] SnapshotGrid(SudokuGameState state)
    {
        int size = state.GridSize;
        var snap = new int[size, size];
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                snap[r, c] = state.Grid[r, c].Value;
            }
        }
        return snap;
    }

    private void ApplyTheme()
    {
        var colors = _themeService.CurrentColors;

        // Header
        _difficultyLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        _timerLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        _mistakesLabel.AddThemeColorOverride("font_color", colors.TextPrimary);

        // Back Button
        ApplyButtonStyle(_backButton, includeDisabled: false);

        // Grid Panel
        var gridStyle = new StyleBoxFlat();
        gridStyle.BgColor = colors.GridLineThick;
        gridStyle.ContentMarginLeft = 4;
        gridStyle.ContentMarginRight = 4;
        gridStyle.ContentMarginTop = 4;
        gridStyle.ContentMarginBottom = 4;
        gridStyle.CornerRadiusTopLeft = 8;
        gridStyle.CornerRadiusTopRight = 8;
        gridStyle.CornerRadiusBottomLeft = 8;
        gridStyle.CornerRadiusBottomRight = 8;
        _gridPanel.AddThemeStyleboxOverride("panel", gridStyle);

        // Grid Zellen
        if (_cellButtons != null && _gameState != null)
        {
            int gridSize = _gameState.GridSize;
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    _cellButtons[row, col].ApplyTheme(_themeService);
                }
            }
        }

        // Number Pad
        foreach (var button in _numberButtons)
        {
            if (button == null) continue;
            ApplyButtonStyle(button);
            button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
            button.AddThemeFontSizeOverride("font_size", 24);
        }

        // Hint Button
        if (_hintButton != null)
        {
            ApplyButtonStyle(_hintButton, includeDisabled: false);
        }

        // Auto-Candidates Button
        if (_autoCandidatesButton != null)
        {
            UpdateAutoCandidatesButtonAppearance();
        }

        // Notes Button
        if (_notesButton != null)
        {
            UpdateNotesButtonAppearance();
        }

        // House Auto-Fill Button
        if (_houseAutoFillButton != null)
        {
            _houseAutoFillButton.AddThemeFontSizeOverride("font_size", 14);
            UpdateHouseAutoFillButtonAppearance();
        }

        // Axis Labels
        foreach (var label in _colLabels)
        {
            if (label != null)
                label.AddThemeColorOverride("font_color", colors.TextSecondary);
        }
        foreach (var label in _rowLabels)
        {
            if (label != null)
                label.AddThemeColorOverride("font_color", colors.TextSecondary);
        }

        if (_solutionPathHeader != null)
            _solutionPathHeader.AddThemeColorOverride("font_color", colors.TextPrimary);
    }

    #region Hint System

    private void CreateHintButton()
    {
        var header = GetNode<HBoxContainer>("VBoxContainer/HeaderMargin/Header");
        var colors = _themeService.CurrentColors;

        _hintButton = new Button();
        _hintButton.Text = "💡";
        _hintButton.CustomMinimumSize = new Vector2(50, 40);
        _hintButton.TooltipText = _localizationService.Get("game.hint.tooltip");
        _hintButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _hintButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _hintButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _hintButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _hintButton.AddThemeFontSizeOverride("font_size", 20);
        _hintButton.Pressed += OnHintButtonPressed;

        // Füge den Button vor MistakesLabel ein
        header.AddChild(_hintButton);
        header.MoveChild(_hintButton, header.GetChildCount() - 1);
    }

    private void CreateSolutionPathButton()
    {
        var header = GetNode<HBoxContainer>("VBoxContainer/HeaderMargin/Header");
        var colors = _themeService.CurrentColors;

        _solutionPathButton = new Button();
        _solutionPathButton.Text = "🧭";
        _solutionPathButton.CustomMinimumSize = new Vector2(50, 40);
        _solutionPathButton.TooltipText = _localizationService.Get("game.solutionpath.tooltip");
        _solutionPathButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _solutionPathButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _solutionPathButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _solutionPathButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _solutionPathButton.AddThemeFontSizeOverride("font_size", 18);
        _solutionPathButton.Pressed += OnSolutionPathPressed;

        header.AddChild(_solutionPathButton);
        // Place it next to the hint button
        var hintIndex = _hintButton.GetIndex();
        header.MoveChild(_solutionPathButton, hintIndex + 1);
    }

    private void CreateAutoCandidatesButton()
    {
        var header = GetNode<HBoxContainer>("VBoxContainer/HeaderMargin/Header");
        var colors = _themeService.CurrentColors;

        _autoCandidatesButton = new Button();
        _autoCandidatesButton.Text = "📋";
        _autoCandidatesButton.CustomMinimumSize = new Vector2(50, 40);
        _autoCandidatesButton.TooltipText = _localizationService.Get("game.autocandidates.tooltip");
        _autoCandidatesButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _autoCandidatesButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _autoCandidatesButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _autoCandidatesButton.AddThemeColorOverride("font_color", colors.TextSecondary);
        _autoCandidatesButton.AddThemeFontSizeOverride("font_size", 18);
        _autoCandidatesButton.Pressed += OnAutoCandidatesButtonPressed;

        // Füge vor dem Hint-Button ein
        header.AddChild(_autoCandidatesButton);
        var hintIndex = _hintButton.GetIndex();
        header.MoveChild(_autoCandidatesButton, hintIndex);
    }

    private void CreateNotesButton()
    {
        // Füge hinter dem Eraser-Button im NumberPad ein
        var colors = _themeService.CurrentColors;

        _notesButton = new Button();
        _notesButton.Text = "✏️";
        _notesButton.CustomMinimumSize = new Vector2(50, 60);
        _notesButton.TooltipText = _localizationService.Get("game.notes.tooltip");
        _notesButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _notesButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _notesButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _notesButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _notesButton.AddThemeFontSizeOverride("font_size", 18);
        _notesButton.Pressed += OnNotesButtonPressed;

        _numberPad.AddChild(_notesButton);
    }

    private void OnSolutionPathPressed()
    {
        _audioService.PlayClick();
        if (_solutionPathOverlay != null && _solutionPathOverlay.Visible)
        {
            CloseSolutionPathOverlay();
        }
        else
        {
            ShowSolutionPathOverlay();
        }
    }

    private void CreateHouseAutoFillButton()
    {
        // Single button: click = apply, right-click or shift+click = cycle mode
        var colors = _themeService.CurrentColors;

        _houseAutoFillButton = new Button();
        _houseAutoFillButton.CustomMinimumSize = new Vector2(70, 60);
        _houseAutoFillButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _houseAutoFillButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _houseAutoFillButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _houseAutoFillButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _houseAutoFillButton.AddThemeFontSizeOverride("font_size", 14);
        _houseAutoFillButton.GuiInput += OnHouseAutoFillGuiInput;
        _numberPad.AddChild(_houseAutoFillButton);

        UpdateHouseAutoFillButtonAppearance();
    }

    private void ShowSolutionPathOverlay()
    {
        if (_gameState == null) return;

        // Build all variant paths lazily (different starting cells)
        if (_solutionPathVariants == null)
        {
            _solutionPathVariants = SolutionPathService.BuildPathsWithDifferentStarts(_gameState, maxSteps: 512);
            _solutionPathSelectedIndex = 0;
        }

        _solutionPath = _solutionPathVariants[_solutionPathSelectedIndex];
        _solutionPathOriginalSnapshot ??= SnapshotCurrentGrid();
        _solutionPathActiveIndex = null;

        // Create overlay once
        if (_solutionPathOverlay == null)
        {
            var overlay = new PanelContainer();
            overlay.Name = "SolutionPathOverlay";
            overlay.AddThemeStyleboxOverride("panel", _themeService.CreatePanelStyleBox(12, 2));
            overlay.CustomMinimumSize = new Vector2(320, 450);

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 16);
            margin.AddThemeConstantOverride("margin_right", 16);
            margin.AddThemeConstantOverride("margin_top", 12);
            margin.AddThemeConstantOverride("margin_bottom", 12);
            margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            margin.SizeFlagsVertical = SizeFlags.ExpandFill;
            overlay.AddChild(margin);

            var vbox = new VBoxContainer();
            vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
            margin.AddChild(vbox);

            _solutionPathHeader = new Label();
            _solutionPathHeader.AddThemeColorOverride("font_color", _themeService.CurrentColors.TextPrimary);
            _solutionPathHeader.AddThemeFontSizeOverride("font_size", 16);
            vbox.AddChild(_solutionPathHeader);

            _solutionPathSelector = new OptionButton();
            _solutionPathSelector.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _solutionPathSelector.ItemSelected += idx =>
            {
                _solutionPathSelectedIndex = (int)idx;
                _solutionPath = _solutionPathVariants?[_solutionPathSelectedIndex];
                if (_solutionPathOriginalSnapshot != null)
                    ApplySnapshot(_solutionPathOriginalSnapshot);
                _solutionPathActiveIndex = null;
                RenderSolutionPath();
            };
            vbox.AddChild(_solutionPathSelector);

            var scroll = new ScrollContainer();
            scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
            scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            scroll.CustomMinimumSize = new Vector2(0, 340);
            vbox.AddChild(scroll);

            _solutionPathList = new VBoxContainer();
            _solutionPathList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _solutionPathList.AddThemeConstantOverride("separation", 4);
            scroll.AddChild(_solutionPathList);

            var closeButton = new Button();
            closeButton.Text = _localizationService.Get("dialog.close");
            closeButton.CustomMinimumSize = new Vector2(100, 36);
            ApplyButtonStyle(closeButton);
            closeButton.Pressed += CloseSolutionPathOverlay;
            vbox.AddChild(closeButton);

            _solutionPathOverlay = overlay;
            _overlayContainer.AddChild(overlay);
        }

        // Position to the right of the grid
        var gridRect = _gridPanel.GetGlobalRect();
        _solutionPathOverlay.Position = new Vector2(
            gridRect.End.X + 20,  // 20px gap to the right of the grid
            gridRect.Position.Y   // Align top with grid
        );

        // Populate selector items
        if (_solutionPathSelector != null && _solutionPathVariants != null)
        {
            _solutionPathSelector.Clear();
            for (int i = 0; i < _solutionPathVariants.Count; i++)
            {
                var path = _solutionPathVariants[i];
                if (path.Steps.Count == 0)
                {
                    _solutionPathSelector.AddItem($"Path {i + 1} (empty)");
                    continue;
                }
                var first = path.Steps[0];
                char colLetter = (char)('A' + first.Col);
                int rowNumber = first.Row + 1;
                string label = $"{colLetter}{rowNumber} = {first.Value} ({first.TechniqueName})";
                _solutionPathSelector.AddItem(label);
            }
            _solutionPathSelector.Selected = _solutionPathSelectedIndex;
        }

        RenderSolutionPath();
        _solutionPathOverlay.Visible = true;
    }

    private void CloseSolutionPathOverlay()
    {
        if (_solutionPathOverlay == null || !_solutionPathOverlay.Visible)
            return;
        if (_solutionPathOriginalSnapshot != null)
            ApplySnapshot(_solutionPathOriginalSnapshot);
        _solutionPathActiveIndex = null;
        _solutionPathDetailSelectedIndex = null;
        _solutionPathOverlay.Visible = false;
        if (_solutionPathDetailPanel != null)
            _solutionPathDetailPanel.Visible = false;
    }

    private void RenderSolutionPath()
    {
        if (_solutionPath == null || _solutionPathList == null || _solutionPathHeader == null) return;

        _solutionPathHeader.Text = _localizationService.Get("game.solutionpath.title") + $" — {_solutionPath.Status} ({_solutionPath.Steps.Count} steps)";

        foreach (var child in _solutionPathList.GetChildren())
            child.QueueFree();

        var colors = _themeService.CurrentColors;
        foreach (var step in _solutionPath.Steps)
        {
            // Use a Button for the entire row to make it clickable
            var rowButton = new Button();
            rowButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            rowButton.CustomMinimumSize = new Vector2(0, 28);
            rowButton.ClipText = false;
            rowButton.Alignment = HorizontalAlignment.Left;

            // Style: highlight if this row is selected for detail view
            bool isDetailSelected = _solutionPathDetailSelectedIndex == step.Index;
            if (isDetailSelected)
            {
                rowButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox(hover: true));
            }
            else
            {
                rowButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
            }
            rowButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
            rowButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
            rowButton.AddThemeStyleboxOverride("focus", _themeService.CreateButtonStyleBox());
            rowButton.AddThemeColorOverride("font_color", colors.TextPrimary);

            // Build row text
            char colLetter = (char)('A' + step.Col);
            int rowNumber = step.Row + 1;
            string detailText;
            if (step.IsPlacement)
            {
                detailText = _localizationService.Get("game.solutionpath.placement", colLetter, rowNumber, step.Value);
            }
            else
            {
                detailText = _localizationService.Get("game.solutionpath.elimination", colLetter, rowNumber, string.Join(",", step.EliminatedCandidates));
            }
            rowButton.Text = $"{step.Index}   {step.TechniqueName,-18} {detailText}";

            // Click to toggle detail selection
            var capturedStep = step; // capture for closure
            rowButton.Pressed += () => OnSolutionPathRowClicked(capturedStep);

            _solutionPathList.AddChild(rowButton);

            // Add the Set/Unset button as a separate row element
            var actionRow = new HBoxContainer();
            actionRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var spacer = new Control();
            spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            actionRow.AddChild(spacer);

            var jumpButton = new Button();
            bool isActive = _solutionPathActiveIndex == step.Index;
            jumpButton.Text = isActive
                ? _localizationService.Get("game.solutionpath.unset")
                : _localizationService.Get("game.solutionpath.set");
            jumpButton.CustomMinimumSize = new Vector2(70, 28);
            ApplyButtonStyle(jumpButton);
            var snapshot = step.GridSnapshot; // capture
            jumpButton.Pressed += () =>
            {
                if (_solutionPathOriginalSnapshot == null)
                    _solutionPathOriginalSnapshot = SnapshotCurrentGrid();

                if (_solutionPathActiveIndex == step.Index)
                {
                    if (_solutionPathOriginalSnapshot != null)
                        ApplySnapshot(_solutionPathOriginalSnapshot);
                    _solutionPathActiveIndex = null;
                }
                else
                {
                    ApplySnapshot(snapshot);
                    _solutionPathActiveIndex = step.Index;
                }
                RenderSolutionPath();
            };
            actionRow.AddChild(jumpButton);

            // Only show the action row if this step is selected for detail
            actionRow.Visible = isDetailSelected;
            _solutionPathList.AddChild(actionRow);
        }

        // Update the detail panel
        UpdateSolutionPathDetailPanel();
    }

    private void OnSolutionPathRowClicked(SolutionPathService.SolutionPathStep step)
    {
        // Toggle selection
        if (_solutionPathDetailSelectedIndex == step.Index)
        {
            _solutionPathDetailSelectedIndex = null;
        }
        else
        {
            _solutionPathDetailSelectedIndex = step.Index;
        }
        RenderSolutionPath();
    }

    private void UpdateSolutionPathDetailPanel()
    {
        // Create detail panel if needed
        if (_solutionPathDetailPanel == null)
        {
            _solutionPathDetailPanel = new PanelContainer();
            _solutionPathDetailPanel.Name = "SolutionPathDetailPanel";
            _solutionPathDetailPanel.AddThemeStyleboxOverride("panel", _themeService.CreatePanelStyleBox(12, 2));
            _solutionPathDetailPanel.CustomMinimumSize = new Vector2(260, 0);
            _solutionPathDetailPanel.SizeFlagsHorizontal = 0; // shrink to content
            _solutionPathDetailPanel.SizeFlagsVertical = 0;

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 12);
            margin.AddThemeConstantOverride("margin_right", 12);
            margin.AddThemeConstantOverride("margin_top", 8);
            margin.AddThemeConstantOverride("margin_bottom", 8);
            _solutionPathDetailPanel.AddChild(margin);

            _solutionPathDetailLabel = new RichTextLabel();
            _solutionPathDetailLabel.BbcodeEnabled = true;
            _solutionPathDetailLabel.FitContent = true;
            _solutionPathDetailLabel.ScrollActive = false;
            _solutionPathDetailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _solutionPathDetailLabel.SizeFlagsHorizontal = 0;
            _solutionPathDetailLabel.SizeFlagsVertical = 0;
            _solutionPathDetailLabel.CustomMinimumSize = new Vector2(230, 0);
            _solutionPathDetailLabel.AddThemeColorOverride("default_color", _themeService.CurrentColors.TextPrimary);
            _solutionPathDetailLabel.AddThemeFontSizeOverride("normal_font_size", 14);
            margin.AddChild(_solutionPathDetailLabel);

            _overlayContainer.AddChild(_solutionPathDetailPanel);
        }

        // Find the selected step
        SolutionPathService.SolutionPathStep? selectedStep = null;
        if (_solutionPathDetailSelectedIndex != null && _solutionPath != null)
        {
            selectedStep = _solutionPath.Steps.FirstOrDefault(s => s.Index == _solutionPathDetailSelectedIndex);
        }

        if (selectedStep == null)
        {
            _solutionPathDetailPanel.Visible = false;
            return;
        }

        // Position to the left of the grid
        var gridRect = _gridPanel.GetGlobalRect();
        var backRect = _backButton.GetGlobalRect();
        float desiredLeft = backRect.Position.X; // align with Back button left edge
        float maxLeft = gridRect.Position.X - _solutionPathDetailPanel.Size.X - 16; // keep clear of grid
        float left = Mathf.Min(desiredLeft, maxLeft);
        _solutionPathDetailPanel.Position = new Vector2(
            Mathf.Max(8, left),  // keep inside viewport
            gridRect.Position.Y   // Align top with grid
        );

        // Build detail text
        string cellRef = ToCellRef(selectedStep.Row, selectedStep.Col);
        string action = selectedStep.IsPlacement
            ? _localizationService.Get("game.solutionpath.tooltip.placement", cellRef, selectedStep.Value)
            : _localizationService.Get("game.solutionpath.tooltip.elimination", cellRef, string.Join(", ", selectedStep.EliminatedCandidates));

        string related = selectedStep.RelatedCells != null && selectedStep.RelatedCells.Count > 0
            ? string.Join(", ", selectedStep.RelatedCells.Select(rc => ToCellRef(rc.row, rc.col)))
            : _localizationService.Get("game.solutionpath.tooltip.no_related");

        string why = selectedStep.Explanation;

        var colors = _themeService.CurrentColors;
        string accentHex = colors.Accent.ToHtml();

        string bbcode = $"[b][color=#{accentHex}]Step {selectedStep.Index}: {selectedStep.TechniqueName}[/color][/b]\n\n" +
                        $"{action}\n\n" +
                        $"[b]Why:[/b] {why}\n\n" +
                        $"[b]Related:[/b] {related}";

        _solutionPathDetailLabel!.Text = bbcode;
        _solutionPathDetailPanel.ResetSize();
        _solutionPathDetailPanel.Visible = true;

        // Reposition after content changes size
        CallDeferred(nameof(RepositionSolutionPathDetailPanel));
    }

    private void RepositionSolutionPathDetailPanel()
    {
        if (_solutionPathDetailPanel == null || !_solutionPathDetailPanel.Visible) return;

        var gridRect = _gridPanel.GetGlobalRect();
        var backRect = _backButton.GetGlobalRect();
        float desiredLeft = backRect.Position.X;
        float maxLeft = gridRect.Position.X - _solutionPathDetailPanel.Size.X - 16;
        float left = Mathf.Min(desiredLeft, maxLeft);
        _solutionPathDetailPanel.Position = new Vector2(
            Mathf.Max(8, left),
            gridRect.Position.Y
        );
    }

    private string BuildSolutionPathTooltip(SolutionPathService.SolutionPathStep step)
    {
        string cellRef = ToCellRef(step.Row, step.Col);
        string action = step.IsPlacement
            ? _localizationService.Get("game.solutionpath.tooltip.placement", cellRef, step.Value)
            : _localizationService.Get("game.solutionpath.tooltip.elimination", cellRef, string.Join(", ", step.EliminatedCandidates));

        string related = step.RelatedCells != null && step.RelatedCells.Count > 0
            ? string.Join(", ", step.RelatedCells.Select(rc => ToCellRef(rc.row, rc.col)))
            : _localizationService.Get("game.solutionpath.tooltip.no_related");

        // Explanation already contains the why from the underlying hint; surface it clearly.
        string why = step.Explanation;

        return _localizationService.Get(
            "game.solutionpath.tooltip.template",
            step.Index,
            step.TechniqueName,
            action,
            why,
            related
        );
    }

    private int[,] SnapshotCurrentGrid()
    {
        int[,] snap = new int[9, 9];
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                snap[r, c] = _gameState?.Grid[r, c].Value ?? 0;
            }
        }
        return snap;
    }

    private void ApplySnapshot(int[,] snapshot)
    {
        if (_gameState == null) return;
        int size = _gameState.GridSize;
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                var cell = _gameState.Grid[r, c];
                if (cell.IsGiven) continue;
                cell.Value = snapshot[r, c];
            }
        }
        UpdateGrid();
        UpdateNumberCounts();
    }

    private void OnHouseAutoFillGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            // Right-click or Shift+Left-click = cycle mode
            if (mb.ButtonIndex == MouseButton.Right ||
                (mb.ButtonIndex == MouseButton.Left && mb.ShiftPressed))
            {
                _houseAutoFillMode = _houseAutoFillMode switch
                {
                    HouseAutoFillMode.Row => HouseAutoFillMode.Column,
                    HouseAutoFillMode.Column => HouseAutoFillMode.Block,
                    HouseAutoFillMode.Block => HouseAutoFillMode.Row,
                    _ => HouseAutoFillMode.Row
                };
                GD.Print($"[Game] HouseAutoFill mode cycled to {_houseAutoFillMode}");
                UpdateHouseAutoFillButtonAppearance();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Left-click = apply
            if (mb.ButtonIndex == MouseButton.Left)
            {
                OnHouseAutoFillApply();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void UpdateHouseAutoFillButtonAppearance()
    {
        if (_houseAutoFillButton == null) return;

        var colors = _themeService.CurrentColors;

        // Full descriptive labels
        string modeLabel = _houseAutoFillMode switch
        {
            HouseAutoFillMode.Row => "▶ Row",
            HouseAutoFillMode.Column => "▶ Col",
            HouseAutoFillMode.Block => "▶ Box",
            _ => "▶ Row"
        };

        string modeText = _houseAutoFillMode switch
        {
            HouseAutoFillMode.Row => _localizationService.Get("game.autofill.row"),
            HouseAutoFillMode.Column => _localizationService.Get("game.autofill.col"),
            HouseAutoFillMode.Block => _localizationService.Get("game.autofill.block"),
            _ => _localizationService.Get("game.autofill.row")
        };

        _houseAutoFillButton.Text = modeLabel;
        _houseAutoFillButton.TooltipText = _localizationService.Get("game.autofill.tooltip", modeText);

        // Style like other number pad buttons (not highlighted)
        _houseAutoFillButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _houseAutoFillButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _houseAutoFillButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _houseAutoFillButton.AddThemeColorOverride("font_color", colors.TextPrimary);
    }

    private void OnHouseAutoFillApply()
    {
        if (_gameState == null || _isGameOver) return;
        if (_selectedRow < 0 || _selectedCol < 0) return;
        if (_gameState.ChallengeNoNotes) return;

        if (!_saveService.Settings.HouseAutoFillEnabled) return;

        GD.Print($"[Game] HouseAutoFill applied: mode={_houseAutoFillMode}, cell={ToCellRef(_selectedRow, _selectedCol)}");
        AutoFillNotesForSelectedHouse();
    }

    private void AutoFillNotesForSelectedHouse()
    {
        if (_gameState == null) return;

        int size = _gameState.GridSize;
        int blockSize = _gameState.BlockSize;

        // Use direct iteration instead of LINQ to avoid allocations
        IEnumerable<(int r, int c)> cells = _houseAutoFillMode switch
        {
            HouseAutoFillMode.Row => GetRowCells(_selectedRow, size),
            HouseAutoFillMode.Column => GetColumnCells(_selectedCol, size),
            HouseAutoFillMode.Block => GetBlockCells(_selectedRow, _selectedCol, blockSize, size),
            _ => GetRowCells(_selectedRow, size)
        };

        foreach (var (r, c) in cells)
        {
            var cell = _gameState.Grid[r, c];
            if (cell.Value != 0) continue;
            if (cell.IsGiven) continue;
            if (HasAnyNotes(cell, size)) continue; // nicht überschreiben

            bool[] candidates = CalculateCandidates(r, c);
            for (int i = 0; i < 9; i++)
            {
                cell.Notes[i] = i < size && i < candidates.Length && candidates[i];
            }
        }

        SaveAndUpdate();
    }

    private static IEnumerable<(int r, int c)> GetRowCells(int row, int size)
    {
        for (int c = 0; c < size; c++)
            yield return (row, c);
    }

    private static IEnumerable<(int r, int c)> GetColumnCells(int col, int size)
    {
        for (int r = 0; r < size; r++)
            yield return (r, col);
    }

    private static IEnumerable<(int r, int c)> GetBlockCells(int row, int col, int blockSize, int size)
    {
        int startRow = (row / blockSize) * blockSize;
        int startCol = (col / blockSize) * blockSize;
        for (int r = startRow; r < startRow + blockSize && r < size; r++)
        {
            for (int c = startCol; c < startCol + blockSize && c < size; c++)
            {
                yield return (r, c);
            }
        }
    }

    private static bool HasAnyNotes(SudokuCell cell, int size)
    {
        int count = Math.Min(size, cell.Notes.Length);
        for (int i = 0; i < count; i++)
        {
            if (cell.Notes[i]) return true;
        }
        return false;
    }

    private void OnAutoCandidatesButtonPressed()
    {
        _showAutoCandidates = !_showAutoCandidates;
        GD.Print($"[Game] Auto-Candidates toggled = {_showAutoCandidates}");
        UpdateAutoCandidatesButtonAppearance();
        UpdateGrid();
    }

    private void OnNotesButtonPressed()
    {
        _isNotesMode = !_isNotesMode;
        GD.Print($"[Game] Notes mode toggled = {_isNotesMode}");
        UpdateNotesButtonAppearance();

        // Tutorial notification
        NotifyTutorialAction(ExpectedAction.ToggleNotesMode);
    }

    private void UpdateAutoCandidatesButtonAppearance()
    {
        var colors = _themeService.CurrentColors;

        if (_showAutoCandidates)
        {
            // Aktiv - Accent-Farbe
            var activeStyle = _themeService.CreateButtonStyleBox();
            activeStyle.BgColor = colors.Accent;
            _autoCandidatesButton.AddThemeStyleboxOverride("normal", activeStyle);
            _autoCandidatesButton.AddThemeColorOverride("font_color", colors.Background);
        }
        else
        {
            // Inaktiv - Normal
            _autoCandidatesButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
            _autoCandidatesButton.AddThemeColorOverride("font_color", colors.TextSecondary);
        }
    }

    private void UpdateNotesButtonAppearance()
    {
        var colors = _themeService.CurrentColors;

        if (_isNotesMode)
        {
            // Aktiv - Accent-Farbe mit verstärktem Stil für Touch-Geräte
            var activeStyle = _themeService.CreateButtonStyleBox();
            activeStyle.BgColor = colors.Accent;
            activeStyle.BorderColor = colors.Accent.Lightened(0.2f);
            activeStyle.SetBorderWidthAll(2);
            _notesButton.AddThemeStyleboxOverride("normal", activeStyle);
            _notesButton.AddThemeStyleboxOverride("focus", activeStyle); // Prevent white border on click

            // Auch hover/pressed für konsistente Touch-Erfahrung
            var hoverStyle = _themeService.CreateButtonStyleBox();
            hoverStyle.BgColor = colors.Accent.Lightened(0.1f);
            hoverStyle.BorderColor = colors.Accent.Lightened(0.3f);
            hoverStyle.SetBorderWidthAll(2);
            _notesButton.AddThemeStyleboxOverride("hover", hoverStyle);

            var pressedStyle = _themeService.CreateButtonStyleBox();
            pressedStyle.BgColor = colors.Accent.Darkened(0.1f);
            pressedStyle.BorderColor = colors.Accent;
            pressedStyle.SetBorderWidthAll(2);
            _notesButton.AddThemeStyleboxOverride("pressed", pressedStyle);

            _notesButton.AddThemeColorOverride("font_color", colors.Background);
        }
        else
        {
            // Inaktiv - Normal
            var normalStyle = _themeService.CreateButtonStyleBox();
            _notesButton.AddThemeStyleboxOverride("normal", normalStyle);
            _notesButton.AddThemeStyleboxOverride("focus", normalStyle); // Prevent white border on click
            _notesButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
            _notesButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
            _notesButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        }
    }

    private void OnHintButtonPressed()
    {
        if (_gameState == null || _isGameOver) return;

        // Tutorial notification for hint button click
        NotifyTutorialAction(ExpectedAction.ClickButton);

        // Hints sind nur für 9x9 verfügbar
        if (_gameState.GridSize != 9)
        {
            GD.Print("[Game] Hint requested but grid is not 9x9");
            return;
        }

        // Challenge: hint limit
        if (_gameState.ChallengeHintLimit > 0 && _gameState.HintsUsed >= _gameState.ChallengeHintLimit)
        {
            GD.Print($"[Game] Hint limit reached ({_gameState.HintsUsed}/{_gameState.ChallengeHintLimit})");
            ApplyChallengeUi();
            ShowHintLimitOverlay();
            return;
        }

        // Finde einen Hinweis
        _currentHint = HintService.FindHint(_gameState);

        if (_currentHint == null)
        {
            GD.Print("[Game] No hint available");
            ShowNoHintOverlay();
            return;
        }

        _gameState.HintsUsed++;
        _appState.SaveGame();
        ApplyChallengeUi();

        GD.Print($"[Game] Hint shown: techniqueId={_currentHint.TechniqueId}, techniqueName={_currentHint.TechniqueName}, cell={ToCellRef(_currentHint.Row, _currentHint.Col)}, value={_currentHint.Value}, hintsUsed={_gameState.HintsUsed}");

        // Technique progression (use stable TechniqueId)
        if (!string.IsNullOrWhiteSpace(_currentHint.TechniqueId))
        {
            _saveService.Settings.IncrementTechniqueShown(_currentHint.TechniqueId);
            _saveService.SaveSettings();
        }

        // Only placement hints can be "applied" by placing the number.
        _lastHintTracking = _currentHint.IsPlacement && _currentHint.Value > 0 && !string.IsNullOrWhiteSpace(_currentHint.TechniqueId)
            ? (_currentHint.Row, _currentHint.Col, _currentHint.Value, _currentHint.TechniqueId)
            : null;

        _isPaused = true;
        _hintPage = 0;
        _hintHighlightedCells.Clear();

        ShowHintOverlay();
    }

    private void ShowHintLimitOverlay()
    {
        if (_gameState == null) return;
        int limit = _gameState.ChallengeHintLimit;
        var overlay = CreateOverlay(
            _localizationService.Get("game.hint_limit.title"),
            _localizationService.Get("game.hint_limit.message", limit),
            new Color("f44336")
        );
        _overlayContainer.AddChild(overlay);
    }

    private void ShowNoHintOverlay()
    {
        var overlay = CreateOverlay(_localizationService.Get("game.hint.no_hint.title"), _localizationService.Get("game.hint.no_hint.message"), new Color("64b5f6"));
        _overlayContainer.AddChild(overlay);
    }

    private void ShowHintOverlay()
    {
        if (_currentHint == null || _gameState == null) return;

        // Entferne vorheriges Overlay
        CloseHintOverlay();

        var colors = _themeService.CurrentColors;

        // Hintergrund (semi-transparent)
        _hintOverlay = new ColorRect();
        ((ColorRect)_hintOverlay).Color = new Color(0, 0, 0, 0.8f);
        _hintOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlayContainer.AddChild(_hintOverlay);

        // Content Panel
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _hintOverlay.AddChild(centerContainer);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(500, 580);
        var panelStyle = _themeService.CreatePanelStyleBox(16, 24);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        centerContainer.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        // Header mit Schließen-Button
        var headerBox = new HBoxContainer();
        vbox.AddChild(headerBox);

        var titleLabel = new Label();
        titleLabel.Text = GetHintPageTitle();
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        titleLabel.AddThemeColorOverride("font_color", colors.Accent);
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        headerBox.AddChild(titleLabel);

        var closeButton = new Button();
        closeButton.Text = "✕";
        closeButton.CustomMinimumSize = new Vector2(36, 36);
        ApplyButtonStyle(closeButton, includeDisabled: false);
        closeButton.Pressed += OnHintClosePressed;
        headerBox.AddChild(closeButton);

        // Mini-Grid Vorschau
        var gridCenter = new CenterContainer();
        vbox.AddChild(gridCenter);

        var miniGrid = CreateHintMiniGrid(colors);
        gridCenter.AddChild(miniGrid);

        // Seiten-Inhalt (Text)
        var contentLabel = new RichTextLabel();
        contentLabel.BbcodeEnabled = true;
        contentLabel.FitContent = true;
        contentLabel.CustomMinimumSize = new Vector2(450, 80);
        contentLabel.AddThemeColorOverride("default_color", colors.TextPrimary);
        contentLabel.AddThemeFontSizeOverride("normal_font_size", 14);
        contentLabel.Text = GetHintPageContent();
        vbox.AddChild(contentLabel);

        // Spacer
        var spacer = new Control();
        spacer.SizeFlagsVertical = Control.SizeFlags.Expand;
        vbox.AddChild(spacer);

        // Navigation
        var navBox = new HBoxContainer();
        navBox.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(navBox);

        var prevButton = new Button();
        prevButton.Text = _localizationService.Get("game.hint.prev");
        prevButton.CustomMinimumSize = new Vector2(100, 36);
        prevButton.Disabled = _hintPage == 0;
        ApplyButtonStyle(prevButton);
        prevButton.Pressed += OnHintPrevPressed;
        navBox.AddChild(prevButton);

        var pageLabel = new Label();
        pageLabel.Text = _localizationService.Get("game.hint.page", _hintPage + 1, 4);
        pageLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        pageLabel.HorizontalAlignment = HorizontalAlignment.Center;
        pageLabel.AddThemeColorOverride("font_color", colors.TextSecondary);
        navBox.AddChild(pageLabel);

        var nextButton = new Button();
        nextButton.Text = _hintPage == 3 ? _localizationService.Get("common.close") : _localizationService.Get("common.next");
        nextButton.CustomMinimumSize = new Vector2(100, 36);
        ApplyButtonStyle(nextButton, includeDisabled: false);
        nextButton.Pressed += OnHintNextPressed;
        navBox.AddChild(nextButton);
    }

    private Control CreateHintMiniGrid(ThemeService.ThemeColors colors)
    {
        if (_gameState == null || _currentHint == null) return new Control();
        var currentHint = _currentHint;

        int[,] values = new int[9, 9];
        bool[,] isGiven = new bool[9, 9];

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var cell = _gameState.Grid[row, col];
                values[row, col] = cell.Value;
                isGiven[row, col] = cell.IsGiven;
            }
        }

        var highlightedCells = new HashSet<(int row, int col)> { (currentHint.Row, currentHint.Col) };
        var relatedCells = new HashSet<(int row, int col)>();

        if (_hintPage == 1)
        {
            foreach (var cell in currentHint.RelatedCells)
            {
                if (cell.row == currentHint.Row && cell.col == currentHint.Col) continue;
                relatedCells.Add(cell);
            }
        }

        (int row, int col, int value)? solutionCell = null;
        bool showSolution = _hintPage >= 2 && currentHint.IsPlacement;
        if (showSolution && values[currentHint.Row, currentHint.Col] == 0)
        {
            solutionCell = (currentHint.Row, currentHint.Col, currentHint.Value);
        }

        return MiniGridRenderer.CreateMiniGridWithLegends(
            values,
            isGiven,
            highlightedCells,
            relatedCells,
            _themeService,
            colors,
            solutionCell,
            candidates: null,
            cellSize: 28
        );
    }

    private string GetHintPageTitle()
    {
        return _hintPage switch
        {
            0 => _localizationService.Get("game.hint.title.hint", _currentHint?.TechniqueName ?? ""),
            1 => _localizationService.Get("game.hint.title.related"),
            2 => _currentHint != null && !_currentHint.IsPlacement
                ? _localizationService.Get("game.hint.title.elimination")
                : _localizationService.Get("game.hint.title.solution"),
            3 => _localizationService.Get("game.hint.title.explanation"),
            _ => _localizationService.Get("game.hint.title.default")
        };
    }

    private string GetHintPageContent()
    {
        if (_currentHint == null) return "";

           string cellRef = ToCellRef(_currentHint.Row, _currentHint.Col);

        return _hintPage switch
        {
            0 => _localizationService.Get("game.hint.page0", _currentHint.TechniqueName, _currentHint.TechniqueDescription, cellRef),

            1 => _localizationService.Get("game.hint.page1", _currentHint.RelatedCells.Count),

            2 => _currentHint.IsPlacement
                ? _localizationService.Get("game.hint.page2", _currentHint.Value)
                : _localizationService.Get(
                    "game.hint.page2.elimination",
                    cellRef,
                    string.Join(", ", _currentHint.EliminatedCandidates)
                ),

            3 => _localizationService.Get("game.hint.page3", _currentHint.Explanation),

            _ => ""
        };
    }

    private static string ToCellRef(int row, int col)
    {
        char colChar = (char)('A' + Math.Clamp(col, 0, 25));
        return $"{colChar}{row + 1}";
    }

    private void OnHintPrevPressed()
    {
        if (_hintPage > 0)
        {
            _hintPage--;
            GD.Print($"[Game] Hint page: prev -> {_hintPage + 1}/4");
            ShowHintOverlay();
        }
    }

    private void OnHintNextPressed()
    {
        if (_hintPage < 3)
        {
            _hintPage++;
            GD.Print($"[Game] Hint page: next -> {_hintPage + 1}/4");
            ShowHintOverlay();
        }
        else
        {
            GD.Print("[Game] Hint overlay closed (finished)");
            OnHintClosePressed();
        }
    }

    private void OnHintClosePressed()
    {
        GD.Print("[Game] Hint overlay closed");
        CloseHintOverlay();
        _isPaused = false;
        _currentHint = null;
        _hintHighlightedCells.Clear();
    }

    private void CloseHintOverlay()
    {
        if (_hintOverlay != null)
        {
            _hintOverlay.QueueFree();
            _hintOverlay = null;
        }
    }

    #endregion
}
