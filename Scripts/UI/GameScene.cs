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
    private HBoxContainer _numberPad = null!;
    private Control _overlayContainer = null!;

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

    private (int row, int col, int value, string technique)? _lastHintTracking = null;

    public override void _Ready()
    {
        // Cache service references
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _appState = GetNode<AppState>("/root/AppState");
        _audioService = GetNode<AudioService>("/root/AudioService");

        _saveService.SettingsChanged += OnSettingsChanged;

        // Start game music
        _audioService.StartGameMusic();

        // UI-Referenzen holen
        _backButton = GetNode<Button>("VBoxContainer/HeaderMargin/Header/BackButton");
        _difficultyLabel = GetNode<Label>("VBoxContainer/HeaderMargin/Header/DifficultyLabel");
        _timerLabel = GetNode<Label>("VBoxContainer/HeaderMargin/Header/TimerLabel");
        _mistakesLabel = GetNode<Label>("VBoxContainer/HeaderMargin/Header/MistakesLabel");
        _gridPanel = GetNode<PanelContainer>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/GridPanel");
        _gridContainer = GetNode<GridContainer>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/GridPanel/GridContainer");
        _numberPad = GetNode<HBoxContainer>("VBoxContainer/NumberPadMargin/NumberPadContainer/NumberPad");
        _overlayContainer = GetNode<Control>("OverlayContainer");

        // Achsen-Labels erstellen
        CreateAxisLabels();

        // Hint-Button erstellen und zum Header hinzufügen
        CreateHintButton();
        CreateAutoCandidatesButton();
        CreateNotesButton();
        CreateHouseAutoFillButton();

        // Events
        _backButton.Pressed += OnBackPressed;
        _backButton.TooltipText = "Zurück zum Hauptmenü (ESC)\nSpiel wird automatisch gespeichert";

        // Grid erstellen
        CreateGrid();
        CreateNumberPad();

        // Theme anwenden
        ApplyTheme();
        _themeService.ThemeChanged += OnThemeChanged;

        // Spiel laden
        LoadGame();
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;

        _saveService.SettingsChanged -= OnSettingsChanged;

        // Spiel speichern beim Verlassen
        if (_gameState != null && !_isGameOver)
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
        button.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
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

                // Multi-select: set note to true (idempotent). Single-select keeps toggle behavior.
                if (_selectedCells.Count > 1)
                {
                    foreach (var (row, col) in _selectedCells)
                    {
                        var cell = _gameState.Grid[row, col];
                        if (cell.IsGiven) continue;
                        if (cell.Value != 0) continue;
                        if (idx >= cell.Notes.Length) continue;
                        cell.Notes[idx] = true;
                    }
                    SaveAndUpdate();
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
            button.TooltipText = $"Zahl {i} setzen (Taste {i})";
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
        eraserButton.TooltipText = "Zahl löschen (Entf/Backspace)";
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

        // Spalten-Labels (A-I) oben - erstelle alle 9, verstecke je nach GridSize
        var colLabelsContainer = GetNode<HBoxContainer>("VBoxContainer/GridCenterContainer/GridWrapper/ColLabelsContainer/ColLabels");
        string[] colNames = { "A", "B", "C", "D", "E", "F", "G", "H", "I" };

        for (int i = 0; i < 9; i++)
        {
            var label = new Label();
            label.Text = colNames[i];
            label.CustomMinimumSize = new Vector2(50, 20);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 14);
            label.AddThemeColorOverride("font_color", colors.TextSecondary);
            _colLabels[i] = label;
            colLabelsContainer.AddChild(label);
        }

        // Zeilen-Labels (1-9) links - erstelle alle 9, verstecke je nach GridSize
        var rowLabelsContainer = GetNode<VBoxContainer>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/RowLabels");

        for (int i = 0; i < 9; i++)
        {
            var label = new Label();
            label.Text = (i + 1).ToString();
            label.CustomMinimumSize = new Vector2(24, 50);
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
        int cellSize = gridSize == 4 ? 110 : 50; // Größere Zellen für Kids (passend zu Grid)

        // Spalten-Labels anpassen
        for (int i = 0; i < 9; i++)
        {
            if (_colLabels[i] != null)
            {
                _colLabels[i].Visible = i < gridSize;
                _colLabels[i].CustomMinimumSize = new Vector2(cellSize, 20);
            }
        }

        // Zeilen-Labels anpassen
        for (int i = 0; i < 9; i++)
        {
            if (_rowLabels[i] != null)
            {
                _rowLabels[i].Visible = i < gridSize;
                _rowLabels[i].CustomMinimumSize = new Vector2(24, cellSize);
            }
        }
    }

    private void LoadGame()
    {
        _gameState = _appState.CurrentGame;

        if (_gameState == null)
        {
            GD.PrintErr("Kein Spielstand gefunden!");
            _appState.GoToMainMenu();
            return;
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
    }

    private void ApplyChallengeUi()
    {
        if (_gameState == null) return;
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
                ? "Hinweise nur im 9x9 verfügbar"
                : (limit > 0
                    ? $"Tipp anzeigen\nHinweise: {used}/{limit} (rest: {remaining})"
                    : "Tipp anzeigen\nZeigt einen Hinweis für den nächsten Zug");

            // Visueller Hint wenn Limit erreicht
            var colors = _themeService.CurrentColors;
            _hintButton.AddThemeColorOverride("font_color", (limit > 0 && remaining == 0) ? colors.TextSecondary : colors.TextPrimary);
        }
    }

    private void UpdateDifficultyLabel()
    {
        if (_gameState == null) return;

        string diffText = _gameState.Difficulty switch
        {
            Difficulty.Kids => "Kids",
            Difficulty.Easy => "Leicht",
            Difficulty.Medium => "Mittel",
            Difficulty.Hard => "Schwer",
            _ => "?"
        };

        _difficultyLabel.Text = $"Schwierigkeit: {diffText}";

        if (_gameState.IsDeadlyMode)
        {
            _difficultyLabel.Text += " (Deadly)";
        }

        if (_gameState.IsDaily)
        {
            _difficultyLabel.Text += " (Daily)";
        }

        // Szenario-Technik anzeigen
        if (!string.IsNullOrEmpty(_gameState.ScenarioTechnique) &&
            TechniqueInfo.Techniques.TryGetValue(_gameState.ScenarioTechnique, out var technique))
        {
            _difficultyLabel.Text += $" 🎯 {technique.Name}";
        }

        var tags = new List<string>();
        if (_gameState.ChallengeNoNotes) tags.Add("NoNotes");
        if (_gameState.ChallengePerfectRun) tags.Add("Perfect");
        if (_gameState.ChallengeHintLimit > 0) tags.Add($"Hints≤{_gameState.ChallengeHintLimit}");
        if (_gameState.ChallengeTimeAttackSeconds > 0) tags.Add($"Time≤{_gameState.ChallengeTimeAttackSeconds / 60}m");
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
            _timerLabel.Text = $"{elapsedText} (Rest {remainingText})";
        }
        else
        {
            _timerLabel.Text = elapsedText;
        }
    }

    private void UpdateMistakesLabel()
    {
        if (_gameState == null) return;

        string text = $"Fehler: {_gameState.Mistakes}";
        if (_gameState.IsDeadlyMode)
        {
            text += "/3";
        }
        _mistakesLabel.Text = text;
    }

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
    }

    private void OnCellHovered(int row, int col)
    {
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
        }
        else
        {
            // Korrekte Zahl
            GD.Print($"[Game] CORRECT number {number} placed at {ToCellRef(_selectedRow, _selectedCol)}");
            _audioService.PlayNumberPlace();
            cell.Value = number;
            _highlightedNumber = number;

            MaybeSmartCleanupNotesAfterPlacement(_selectedRow, _selectedCol, number);

            // Technique tracking: applied?
            if (_lastHintTracking.HasValue)
            {
                var h = _lastHintTracking.Value;
                if (h.row == _selectedRow && h.col == _selectedCol && h.value == number)
                {
                    _saveService.Settings.IncrementTechniqueApplied(h.technique);
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

        var cell = _gameState.Grid[_selectedRow, _selectedCol];
        string cellRef = ToCellRef(_selectedRow, _selectedCol);
        bool violatesRules = !_gameState.IsValidPlacement(_selectedRow, _selectedCol, attemptedNumber);

        string body;
        if (violatesRules)
        {
            var conflicts = FindRuleConflicts(_selectedRow, _selectedCol, attemptedNumber);
            body = $"Zelle {cellRef}: {attemptedNumber} passt nicht, weil sie mit {string.Join(", ", conflicts)} kollidiert.";
        }
        else
        {
            body = $"Zelle {cellRef}: {attemptedNumber} ist regelkonform, aber nicht die Lösung dieses Rätsels.";
        }

        body += $"\n\nTipp: In {cellRef} gehört {cell.Solution}.";

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

        if (conflicts.Count == 0) conflicts.Add("den Sudoku-Regeln");
        return conflicts;
    }

    private void ShowPerfectRunFailedOverlay()
    {
        var overlay = CreateOverlay("🎯 Perfect Run", "Ein Fehler beendet diesen Modus.\nDu hast verloren.", new Color("f44336"));
        _overlayContainer.AddChild(overlay);
    }

    private void ShowTimeAttackOverlay()
    {
        var overlay = CreateOverlay("⏱️ Time Attack", "Zeit abgelaufen.\nDu hast verloren.", new Color("f44336"));
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
        var overlay = CreateOverlay("🎉 Gratulation!",
            $"Du hast das Sudoku gelöst!\n\n⏱️ Zeit: {_timerLabel.Text}\n❌ Fehler: {_gameState?.Mistakes ?? 0}",
            new Color("4caf50"));
        _overlayContainer.AddChild(overlay);
    }

    private void ShowGameOverOverlay()
    {
        var overlay = CreateOverlay("💀 Game Over",
            "Du hast 3 Fehler gemacht.\nDas Spiel ist beendet.",
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
        button.Text = "Zurück zum Menü";
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
        button.Text = "Schließen";
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
        if (_gameState != null && !_isGameOver)
        {
            _gameState.ElapsedSeconds = _elapsedTime;
            _appState.SaveGame();
        }

        _appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
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
    }

    #region Hint System

    private void CreateHintButton()
    {
        var header = GetNode<HBoxContainer>("VBoxContainer/HeaderMargin/Header");
        var colors = _themeService.CurrentColors;

        _hintButton = new Button();
        _hintButton.Text = "💡";
        _hintButton.CustomMinimumSize = new Vector2(50, 40);
        _hintButton.TooltipText = "Tipp anzeigen\nZeigt einen Hinweis für den nächsten Zug";
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

    private void CreateAutoCandidatesButton()
    {
        var header = GetNode<HBoxContainer>("VBoxContainer/HeaderMargin/Header");
        var colors = _themeService.CurrentColors;

        _autoCandidatesButton = new Button();
        _autoCandidatesButton.Text = "📋";
        _autoCandidatesButton.CustomMinimumSize = new Vector2(50, 40);
        _autoCandidatesButton.TooltipText = "Auto-Kandidaten anzeigen/verbergen\nZeigt alle möglichen Zahlen (grau)";
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
        _notesButton.TooltipText = "Notizen-Modus (N)\nEigene Notizen setzen (blau)\nCtrl+Klick für Mehrfachauswahl";
        _notesButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _notesButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _notesButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _notesButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _notesButton.AddThemeFontSizeOverride("font_size", 18);
        _notesButton.Pressed += OnNotesButtonPressed;

        _numberPad.AddChild(_notesButton);
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
            HouseAutoFillMode.Row => "Zeile",
            HouseAutoFillMode.Column => "Spalte",
            HouseAutoFillMode.Block => "Block",
            _ => "Zeile"
        };

        _houseAutoFillButton.Text = modeLabel;
        _houseAutoFillButton.TooltipText = $"Klick: Auto-Notizen für {modeText} einfügen\nRechtsklick/Shift+Klick: Modus wechseln";

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
            activeStyle.BorderColor = colors.Accent.Lightened(0.3f);
            activeStyle.SetBorderWidthAll(3);
            _notesButton.AddThemeStyleboxOverride("normal", activeStyle);

            // Auch hover/pressed für konsistente Touch-Erfahrung
            var hoverStyle = _themeService.CreateButtonStyleBox();
            hoverStyle.BgColor = colors.Accent.Lightened(0.1f);
            hoverStyle.BorderColor = colors.Accent.Lightened(0.4f);
            hoverStyle.SetBorderWidthAll(3);
            _notesButton.AddThemeStyleboxOverride("hover", hoverStyle);

            var pressedStyle = _themeService.CreateButtonStyleBox();
            pressedStyle.BgColor = colors.Accent.Darkened(0.1f);
            pressedStyle.BorderColor = colors.Accent;
            pressedStyle.SetBorderWidthAll(3);
            _notesButton.AddThemeStyleboxOverride("pressed", pressedStyle);

            _notesButton.AddThemeColorOverride("font_color", colors.Background);
        }
        else
        {
            // Inaktiv - Normal
            _notesButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
            _notesButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
            _notesButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
            _notesButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        }
    }

    private void OnHintButtonPressed()
    {
        if (_gameState == null || _isGameOver) return;

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

        GD.Print($"[Game] Hint shown: technique={_currentHint.TechniqueName}, cell={ToCellRef(_currentHint.Row, _currentHint.Col)}, value={_currentHint.Value}, hintsUsed={_gameState.HintsUsed}");

        // Technique progression
        _saveService.Settings.IncrementTechniqueShown(_currentHint.TechniqueName);
        _saveService.SaveSettings();
        _lastHintTracking = (_currentHint.Row, _currentHint.Col, _currentHint.Value, _currentHint.TechniqueName);

        _isPaused = true;
        _hintPage = 0;
        _hintHighlightedCells.Clear();

        ShowHintOverlay();
    }

    private void ShowHintLimitOverlay()
    {
        if (_gameState == null) return;
        int limit = _gameState.ChallengeHintLimit;
        var overlay = CreateOverlay("💡 Hint-Limit", $"Du hast das Hint-Limit erreicht ({limit}).", new Color("f44336"));
        _overlayContainer.AddChild(overlay);
    }

    private void ShowNoHintOverlay()
    {
        var overlay = CreateOverlay("💡 Kein Tipp", "Aktuell ist kein sinnvoller Hinweis verfügbar.", new Color("64b5f6"));
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
        prevButton.Text = "← Zurück";
        prevButton.CustomMinimumSize = new Vector2(100, 36);
        prevButton.Disabled = _hintPage == 0;
        ApplyButtonStyle(prevButton);
        prevButton.Pressed += OnHintPrevPressed;
        navBox.AddChild(prevButton);

        var pageLabel = new Label();
        pageLabel.Text = $"{_hintPage + 1} / 4";
        pageLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        pageLabel.HorizontalAlignment = HorizontalAlignment.Center;
        pageLabel.AddThemeColorOverride("font_color", colors.TextSecondary);
        navBox.AddChild(pageLabel);

        var nextButton = new Button();
        nextButton.Text = _hintPage == 3 ? "Schließen" : "Weiter →";
        nextButton.CustomMinimumSize = new Vector2(100, 36);
        ApplyButtonStyle(nextButton, includeDisabled: false);
        nextButton.Pressed += OnHintNextPressed;
        navBox.AddChild(nextButton);
    }

    private Control CreateHintMiniGrid(ThemeService.ThemeColors colors)
    {
        if (_gameState == null || _currentHint == null) return new Control();

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

        var highlightedCells = new HashSet<(int row, int col)> { (_currentHint.Row, _currentHint.Col) };
        var relatedCells = new HashSet<(int row, int col)>();

        if (_hintPage == 1)
        {
            foreach (var cell in _currentHint.RelatedCells)
            {
                if (cell.row == _currentHint.Row && cell.col == _currentHint.Col) continue;
                relatedCells.Add(cell);
            }
        }

        (int row, int col, int value)? solutionCell = null;
        bool showSolution = _hintPage >= 2;
        if (showSolution && values[_currentHint.Row, _currentHint.Col] == 0)
        {
            solutionCell = (_currentHint.Row, _currentHint.Col, _currentHint.Value);
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
            0 => $"💡 Tipp: {_currentHint?.TechniqueName ?? ""}",
            1 => "🔍 Relevante Zellen",
            2 => "✓ Lösung",
            3 => "📖 Erklärung",
            _ => "Tipp"
        };
    }

    private string GetHintPageContent()
    {
        if (_currentHint == null) return "";

           string cellRef = ToCellRef(_currentHint.Row, _currentHint.Col);

        return _hintPage switch
        {
            0 => $"[b]Technik:[/b] {_currentHint.TechniqueName}\n\n" +
                 $"{_currentHint.TechniqueDescription}\n\n" +
                 $"Schaue dir die [color=#64b5f6]blau markierte Zelle[/color] im Spielfeld an.\n" +
                  $"[i](Zelle {cellRef})[/i]",

              1 => $"Die markierten Zellen sind relevant für diesen Hinweis.\n\n" +
                 $"Diese Zellen befinden sich in der gleichen Zeile, Spalte oder im gleichen 3x3-Block " +
                 $"wie die Zielzelle und beeinflussen, welche Zahlen dort möglich sind.\n\n" +
                 $"[i]Anzahl relevanter Zellen: {_currentHint.RelatedCells.Count}[/i]",

            2 => $"Die Lösung für diese Zelle ist:\n\n" +
                 $"[center][font_size=48][color=#4caf50][b]{_currentHint.Value}[/b][/color][/font_size][/center]\n\n" +
                 $"[i]Klicke auf \"Weiter\" für eine detaillierte Erklärung.[/i]",

            3 => $"[b]Erklärung:[/b]\n\n{_currentHint.Explanation}",

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
