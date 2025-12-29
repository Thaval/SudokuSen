namespace MySudoku.UI;

/// <summary>
/// Die Haupt-Spielszene mit dem Sudoku-Grid
/// </summary>
public partial class GameScene : Control
{
    // UI-Elemente
    private Button _backButton = null!;
    private Button _hintButton = null!;
    private Button _notesButton = null!;
    private Button _autoCandidatesButton = null!;
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
    private bool _isGameOver = false;
    private bool _isPaused = false;
    private bool _isNotesMode = false;
    private bool _showAutoCandidates = false;

    // Multi-Select
    private HashSet<(int row, int col)> _selectedCells = new();
    private bool _isDragging = false;
    private (int row, int col)? _dragStart = null;

    // Hint-System
    private HintService.Hint? _currentHint = null;
    private int _hintPage = 0;
    private Control? _hintOverlay = null;
    private HashSet<(int row, int col)> _hintHighlightedCells = new();

    public override void _Ready()
    {
        // UI-Referenzen holen
        _backButton = GetNode<Button>("VBoxContainer/Header/BackButton");
        _difficultyLabel = GetNode<Label>("VBoxContainer/Header/DifficultyLabel");
        _timerLabel = GetNode<Label>("VBoxContainer/Header/TimerLabel");
        _mistakesLabel = GetNode<Label>("VBoxContainer/Header/MistakesLabel");
        _gridPanel = GetNode<PanelContainer>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/GridPanel");
        _gridContainer = GetNode<GridContainer>("VBoxContainer/GridCenterContainer/GridWrapper/GridRowContainer/GridPanel/GridContainer");
        _numberPad = GetNode<HBoxContainer>("VBoxContainer/NumberPadContainer/NumberPad");
        _overlayContainer = GetNode<Control>("OverlayContainer");

        // Achsen-Labels erstellen
        CreateAxisLabels();

        // Hint-Button erstellen und zum Header hinzufügen
        CreateHintButton();
        CreateAutoCandidatesButton();
        CreateNotesButton();

        // Events
        _backButton.Pressed += OnBackPressed;
        _backButton.TooltipText = "Zurück zum Hauptmenü (ESC)\nSpiel wird automatisch gespeichert";

        // Grid erstellen
        CreateGrid();
        CreateNumberPad();

        // Theme anwenden
        ApplyTheme();
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;

        // Spiel laden
        LoadGame();
    }

    public override void _ExitTree()
    {
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged -= OnThemeChanged;

        // Spiel speichern beim Verlassen
        if (_gameState != null && !_isGameOver)
        {
            _gameState.ElapsedSeconds = _elapsedTime;
            var appState = GetNode<AppState>("/root/AppState");
            appState.SaveGame();
        }
    }

    public override void _Process(double delta)
    {
        if (_gameState == null || _isGameOver || _isPaused) return;

        _elapsedTime += delta;
        UpdateTimerDisplay();
    }

    public override void _Input(InputEvent @event)
    {
        if (_isGameOver || _gameState == null || _isPaused) return;

        // Maus-Events für Drag-Select
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (!mouseButton.Pressed)
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
                OnNotesButtonPressed();
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

    private void TrySetNumberOnSelection(int number)
    {
        // Wenn Mehrfachauswahl, auf alle anwenden
        if (_selectedCells.Count > 1)
        {
            foreach (var (row, col) in _selectedCells)
            {
                _selectedRow = row;
                _selectedCol = col;
                TrySetNumber(number);
            }
            // Zurück zur letzten Zelle
            if (_selectedCells.Count > 0)
            {
                var last = _selectedCells.Last();
                _selectedRow = last.row;
                _selectedCol = last.col;
            }
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
        var theme = GetNode<ThemeService>("/root/ThemeService");
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                _cellButtons[row, col].ApplyTheme(theme);
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

        // Alte Buttons entfernen (außer Notes-Button, der wird am Ende wieder hinzugefügt)
        foreach (var child in _numberPad.GetChildren().ToList())
        {
            if (child != _notesButton)
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

        // Notes-Button ans Ende verschieben
        if (_notesButton != null)
        {
            _numberPad.MoveChild(_notesButton, _numberPad.GetChildCount() - 1);
        }

        // Theme anwenden
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;
        foreach (var button in _numberButtons)
        {
            if (button == null) continue;
            button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
            button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
            button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
            button.AddThemeStyleboxOverride("disabled", theme.CreateButtonStyleBox(disabled: true));
            button.AddThemeColorOverride("font_color", colors.TextPrimary);
            button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
            button.AddThemeFontSizeOverride("font_size", 24);
        }
    }

    private void CreateAxisLabels()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

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
        var appState = GetNode<AppState>("/root/AppState");
        _gameState = appState.CurrentGame;

        if (_gameState == null)
        {
            GD.PrintErr("Kein Spielstand gefunden!");
            appState.GoToMainMenu();
            return;
        }

        _elapsedTime = _gameState.ElapsedSeconds;

        // Grid und NumberPad für aktuelle GridSize erstellen
        RecreateGridForGameState();
        RecreateNumberPadForGameState();

        // UI aktualisieren
        UpdateDifficultyLabel();
        UpdateMistakesLabel();
        UpdateGrid();
        UpdateNumberCounts();
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
    }

    private void UpdateTimerDisplay()
    {
        var ts = TimeSpan.FromSeconds(_elapsedTime);
        if (ts.Hours > 0)
            _timerLabel.Text = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        else
            _timerLabel.Text = $"{ts.Minutes:D2}:{ts.Seconds:D2}";
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
                    var saveService = GetNode<SaveService>("/root/SaveService");
                    if (saveService.Settings.HighlightRelatedCells)
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
                    bool[] emptyCandidates = new bool[gridSize == 4 ? 4 : 9];
                    button.SetCandidates(emptyCandidates, false);
                }
            }
        }
    }

    private bool[] CalculateCandidates(int row, int col)
    {
        if (_gameState == null) return new bool[9];

        int gridSize = _gameState.GridSize;
        int blockSize = _gameState.BlockSize;
        bool[] candidates = new bool[gridSize];
        for (int i = 0; i < gridSize; i++)
            candidates[i] = true;

        // Zeile prüfen
        for (int c = 0; c < gridSize; c++)
        {
            int val = _gameState.Grid[row, c].Value;
            if (val > 0) candidates[val - 1] = false;
        }

        // Spalte prüfen
        for (int r = 0; r < gridSize; r++)
        {
            int val = _gameState.Grid[r, col].Value;
            if (val > 0) candidates[val - 1] = false;
        }

        // Block prüfen (2x2 für Kids, 3x3 für Standard)
        int blockRow = (row / blockSize) * blockSize;
        int blockCol = (col / blockSize) * blockSize;
        for (int r = blockRow; r < blockRow + blockSize; r++)
        {
            for (int c = blockCol; c < blockCol + blockSize; c++)
            {
                int val = _gameState.Grid[r, c].Value;
                if (val > 0) candidates[val - 1] = false;
            }
        }

        return candidates;
    }

    private void UpdateNumberCounts()
    {
        if (_gameState == null) return;

        var saveService = GetNode<SaveService>("/root/SaveService");
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;
        bool hideCompleted = saveService.Settings.HideCompletedNumbers;
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
                var highlightStyle = theme.CreateButtonStyleBox();
                highlightStyle.BgColor = colors.CellBackgroundHighlighted;
                _numberButtons[i].AddThemeStyleboxOverride("normal", highlightStyle);
                _numberButtons[i].AddThemeColorOverride("font_color", colors.TextPrimary);
            }
            else if (!isComplete)
            {
                _numberButtons[i].AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
                _numberButtons[i].AddThemeColorOverride("font_color", colors.TextPrimary);
            }
        }
    }

    private void OnCellClicked(int row, int col)
    {
        if (_isGameOver || _gameState == null) return;

        var cell = _gameState.Grid[row, col];
        bool ctrlPressed = Input.IsKeyPressed(Key.Ctrl);
        bool shiftPressed = Input.IsKeyPressed(Key.Shift);

        // Ctrl+Klick: Zur Mehrfachauswahl hinzufügen/entfernen
        if (ctrlPressed)
        {
            if (_selectedCells.Contains((row, col)))
            {
                _selectedCells.Remove((row, col));
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
            }
        }
        // Shift+Klick: Bereich auswählen
        else if (shiftPressed && _selectedRow >= 0 && _selectedCol >= 0)
        {
            SelectRange(_selectedRow, _selectedCol, row, col);
            _selectedRow = row;
            _selectedCol = col;
        }
        // Normaler Klick: Einzelauswahl
        else
        {
            _selectedCells.Clear();
            _selectedCells.Add((row, col));
            _selectedRow = row;
            _selectedCol = col;

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
            TrySetNumber(number);
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
            // Im normalen Modus: Überschreibe den Wert wenn er falsch ist, ansonsten blockieren
            // Aber da unsere Logik nur korrekte Werte zulässt, hier nichts tun
        }

        // Notizen-Modus
        if (_isNotesMode && number > 0)
        {
            // Nur wenn die Zelle leer ist
            if (cell.Value != 0) return;

            // Toggle die Notiz
            cell.Notes[number - 1] = !cell.Notes[number - 1];
            SaveAndUpdate();
            return;
        }

        // Löschen erlauben
        if (number == 0)
        {
            cell.Value = 0;
            // Auch alle Notizen löschen
            for (int i = 0; i < 9; i++)
                cell.Notes[i] = false;
            _highlightedNumber = 0;
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
            var appState = GetNode<AppState>("/root/AppState");
            bool gameOver = appState.RegisterMistake();

            UpdateMistakesLabel();

            // Visuelles Feedback
            _cellButtons[_selectedRow, _selectedCol].FlashError();

            if (gameOver)
            {
                // Game Over!
                _isGameOver = true;
                _gameState.ElapsedSeconds = _elapsedTime;
                appState.EndGame(GameStatus.Lost);
                ShowGameOverOverlay();
                return;
            }
        }
        else
        {
            // Korrekte Zahl
            cell.Value = number;
            _highlightedNumber = number;

            // Prüfen ob gewonnen
            if (_gameState.IsComplete())
            {
                _isGameOver = true;
                _gameState.ElapsedSeconds = _elapsedTime;
                var appState = GetNode<AppState>("/root/AppState");
                appState.EndGame(GameStatus.Won);
                ShowWinOverlay();
                return;
            }
        }

        SaveAndUpdate();
    }

    private void SaveAndUpdate()
    {
        if (_gameState == null) return;

        _gameState.ElapsedSeconds = _elapsedTime;
        var appState = GetNode<AppState>("/root/AppState");
        appState.SaveGame();

        UpdateGrid();
        UpdateNumberCounts();
    }

    private void ShowWinOverlay()
    {
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
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

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
        var panelStyle = theme.CreatePanelStyleBox(16, 32);
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
        button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        button.AddThemeColorOverride("font_color", colors.TextPrimary);
        button.Pressed += () => {
            var appState = GetNode<AppState>("/root/AppState");
            appState.GoToMainMenu();
        };
        vbox.AddChild(button);

        return bgRect;
    }

    private void OnBackPressed()
    {
        // Spiel speichern
        if (_gameState != null && !_isGameOver)
        {
            _gameState.ElapsedSeconds = _elapsedTime;
            var appState = GetNode<AppState>("/root/AppState");
            appState.SaveGame();
        }

        var appState2 = GetNode<AppState>("/root/AppState");
        appState2.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        // Header
        _difficultyLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        _timerLabel.AddThemeColorOverride("font_color", colors.TextPrimary);
        _mistakesLabel.AddThemeColorOverride("font_color", colors.TextPrimary);

        // Back Button
        _backButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);

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
                    _cellButtons[row, col].ApplyTheme(theme);
                }
            }
        }

        // Number Pad
        foreach (var button in _numberButtons)
        {
            if (button == null) continue;
            button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
            button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
            button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
            button.AddThemeStyleboxOverride("disabled", theme.CreateButtonStyleBox(disabled: true));
            button.AddThemeColorOverride("font_color", colors.TextPrimary);
            button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
            button.AddThemeFontSizeOverride("font_size", 24);
        }

        // Hint Button
        if (_hintButton != null)
        {
            _hintButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
            _hintButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
            _hintButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
            _hintButton.AddThemeColorOverride("font_color", colors.TextPrimary);
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
        var header = GetNode<HBoxContainer>("VBoxContainer/Header");
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        _hintButton = new Button();
        _hintButton.Text = "💡";
        _hintButton.CustomMinimumSize = new Vector2(50, 40);
        _hintButton.TooltipText = "Tipp anzeigen\nZeigt einen Hinweis für den nächsten Zug";
        _hintButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        _hintButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        _hintButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        _hintButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _hintButton.AddThemeFontSizeOverride("font_size", 20);
        _hintButton.Pressed += OnHintButtonPressed;

        // Füge den Button vor MistakesLabel ein
        header.AddChild(_hintButton);
        header.MoveChild(_hintButton, header.GetChildCount() - 1);
    }

    private void CreateAutoCandidatesButton()
    {
        var header = GetNode<HBoxContainer>("VBoxContainer/Header");
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        _autoCandidatesButton = new Button();
        _autoCandidatesButton.Text = "📋";
        _autoCandidatesButton.CustomMinimumSize = new Vector2(50, 40);
        _autoCandidatesButton.TooltipText = "Auto-Kandidaten anzeigen/verbergen\nZeigt alle möglichen Zahlen (grau)";
        _autoCandidatesButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        _autoCandidatesButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        _autoCandidatesButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
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
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        _notesButton = new Button();
        _notesButton.Text = "✏️";
        _notesButton.CustomMinimumSize = new Vector2(50, 60);
        _notesButton.TooltipText = "Notizen-Modus (N)\nEigene Notizen setzen (blau)\nCtrl+Klick für Mehrfachauswahl";
        _notesButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        _notesButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        _notesButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        _notesButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _notesButton.AddThemeFontSizeOverride("font_size", 18);
        _notesButton.Pressed += OnNotesButtonPressed;

        _numberPad.AddChild(_notesButton);
    }

    private void OnAutoCandidatesButtonPressed()
    {
        _showAutoCandidates = !_showAutoCandidates;
        UpdateAutoCandidatesButtonAppearance();
        UpdateGrid();
    }

    private void OnNotesButtonPressed()
    {
        _isNotesMode = !_isNotesMode;
        UpdateNotesButtonAppearance();
    }

    private void UpdateAutoCandidatesButtonAppearance()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        if (_showAutoCandidates)
        {
            // Aktiv - Accent-Farbe
            var activeStyle = theme.CreateButtonStyleBox();
            activeStyle.BgColor = colors.Accent;
            _autoCandidatesButton.AddThemeStyleboxOverride("normal", activeStyle);
            _autoCandidatesButton.AddThemeColorOverride("font_color", colors.Background);
        }
        else
        {
            // Inaktiv - Normal
            _autoCandidatesButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
            _autoCandidatesButton.AddThemeColorOverride("font_color", colors.TextSecondary);
        }
    }

    private void UpdateNotesButtonAppearance()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        if (_isNotesMode)
        {
            // Aktiv - Accent-Farbe (Theme-aware)
            var activeStyle = theme.CreateButtonStyleBox();
            activeStyle.BgColor = colors.Accent;
            _notesButton.AddThemeStyleboxOverride("normal", activeStyle);
            _notesButton.AddThemeColorOverride("font_color", colors.Background);
        }
        else
        {
            // Inaktiv - Normal
            _notesButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
            _notesButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        }
    }

    private void OnHintButtonPressed()
    {
        if (_gameState == null || _isGameOver) return;

        // Hints sind nur für 9x9 verfügbar
        if (_gameState.GridSize != 9)
        {
            return;
        }

        // Finde einen Hinweis
        _currentHint = HintService.FindHint(_gameState);

        if (_currentHint == null)
        {
            // Kein Hinweis verfügbar (Spiel ist gelöst?)
            return;
        }

        _isPaused = true;
        _hintPage = 0;
        _hintHighlightedCells.Clear();

        ShowHintOverlay();
    }

    private void ShowHintOverlay()
    {
        if (_currentHint == null || _gameState == null) return;

        // Entferne vorheriges Overlay
        CloseHintOverlay();

        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

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
        var panelStyle = theme.CreatePanelStyleBox(16, 24);
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
        closeButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        closeButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        closeButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        closeButton.Pressed += OnHintClosePressed;
        headerBox.AddChild(closeButton);

        // Mini-Grid Vorschau
        var gridCenter = new CenterContainer();
        vbox.AddChild(gridCenter);

        var miniGridPanel = new PanelContainer();
        miniGridPanel.CustomMinimumSize = new Vector2(270, 270);
        var miniGridStyle = new StyleBoxFlat();
        miniGridStyle.BgColor = colors.GridLineThick;
        miniGridStyle.ContentMarginLeft = 2;
        miniGridStyle.ContentMarginRight = 2;
        miniGridStyle.ContentMarginTop = 2;
        miniGridStyle.ContentMarginBottom = 2;
        miniGridStyle.CornerRadiusTopLeft = 4;
        miniGridStyle.CornerRadiusTopRight = 4;
        miniGridStyle.CornerRadiusBottomLeft = 4;
        miniGridStyle.CornerRadiusBottomRight = 4;
        miniGridPanel.AddThemeStyleboxOverride("panel", miniGridStyle);
        gridCenter.AddChild(miniGridPanel);

        var miniGrid = new GridContainer();
        miniGrid.Columns = 9;
        miniGridPanel.AddChild(miniGrid);

        // Erstelle Mini-Grid Zellen
        CreateMiniGridCells(miniGrid, theme, colors);

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
        prevButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        prevButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        prevButton.AddThemeStyleboxOverride("disabled", theme.CreateButtonStyleBox(disabled: true));
        prevButton.AddThemeColorOverride("font_color", colors.TextPrimary);
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
        nextButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        nextButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        nextButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        nextButton.Pressed += OnHintNextPressed;
        navBox.AddChild(nextButton);
    }

    private void CreateMiniGridCells(GridContainer miniGrid, ThemeService theme, ThemeService.ThemeColors colors)
    {
        if (_gameState == null || _currentHint == null) return;

        // Bestimme welche Zellen hervorgehoben werden sollen
        var highlightedCells = new HashSet<(int row, int col)>();
        bool showSolution = _hintPage >= 2;

        switch (_hintPage)
        {
            case 0: // Nur Zielzelle
                highlightedCells.Add((_currentHint.Row, _currentHint.Col));
                break;
            case 1: // Zielzelle + Related Cells
                highlightedCells.Add((_currentHint.Row, _currentHint.Col));
                foreach (var cell in _currentHint.RelatedCells)
                {
                    highlightedCells.Add(cell);
                }
                break;
            case 2: // Nur Zielzelle mit Lösung
            case 3:
                highlightedCells.Add((_currentHint.Row, _currentHint.Col));
                break;
        }

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var cell = _gameState.Grid[row, col];
                bool isTarget = row == _currentHint.Row && col == _currentHint.Col;
                bool isRelated = highlightedCells.Contains((row, col)) && !isTarget;

                var cellPanel = new PanelContainer();
                cellPanel.CustomMinimumSize = new Vector2(28, 28);

                // Bestimme Hintergrundfarbe
                Color bgColor;
                if (isTarget)
                    bgColor = colors.CellBackgroundSelected;
                else if (isRelated && _hintPage == 1)
                    bgColor = new Color("ffb74d").Lerp(colors.CellBackground, 0.4f);
                else if (cell.IsGiven)
                    bgColor = colors.CellBackgroundGiven;
                else
                    bgColor = colors.CellBackground;

                var cellStyle = new StyleBoxFlat();
                cellStyle.BgColor = bgColor;

                // Margins für 3x3-Block-Grenzen
                bool isRightBlockBorder = (col + 1) % 3 == 0 && col < 8;
                bool isBottomBlockBorder = (row + 1) % 3 == 0 && row < 8;

                cellStyle.ContentMarginRight = isRightBlockBorder ? 2 : 1;
                cellStyle.ContentMarginBottom = isBottomBlockBorder ? 2 : 1;
                cellStyle.ContentMarginLeft = 1;
                cellStyle.ContentMarginTop = 1;

                // Border für Zielzelle
                if (isTarget)
                {
                    cellStyle.BorderColor = colors.Accent;
                    cellStyle.BorderWidthLeft = 2;
                    cellStyle.BorderWidthRight = 2;
                    cellStyle.BorderWidthTop = 2;
                    cellStyle.BorderWidthBottom = 2;
                }

                cellPanel.AddThemeStyleboxOverride("panel", cellStyle);

                // Label für Zahl
                var label = new Label();
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                label.AddThemeFontSizeOverride("font_size", 14);

                // Bestimme anzuzeigenden Wert
                int displayValue = cell.Value;
                if (isTarget && showSolution && cell.Value == 0)
                {
                    displayValue = _currentHint.Value;
                }

                if (displayValue > 0)
                {
                    label.Text = displayValue.ToString();

                    // Textfarbe
                    if (isTarget && showSolution && cell.Value == 0)
                        label.AddThemeColorOverride("font_color", new Color("4caf50")); // Grün für Lösung
                    else if (cell.IsGiven)
                        label.AddThemeColorOverride("font_color", colors.TextGiven);
                    else
                        label.AddThemeColorOverride("font_color", colors.TextUser);
                }
                else
                {
                    label.Text = "";
                }

                cellPanel.AddChild(label);
                miniGrid.AddChild(cellPanel);
            }
        }
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

        return _hintPage switch
        {
            0 => $"[b]Technik:[/b] {_currentHint.TechniqueName}\n\n" +
                 $"{_currentHint.TechniqueDescription}\n\n" +
                 $"Schaue dir die [color=#64b5f6]blau markierte Zelle[/color] im Spielfeld an.\n" +
                 $"[i](Zeile {_currentHint.Row + 1}, Spalte {_currentHint.Col + 1})[/i]",

            1 => $"Die [color=#ffb74d]orange markierten Zellen[/color] sind relevant für diesen Hinweis.\n\n" +
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

    private void OnHintPrevPressed()
    {
        if (_hintPage > 0)
        {
            _hintPage--;
            ShowHintOverlay();
        }
    }

    private void OnHintNextPressed()
    {
        if (_hintPage < 3)
        {
            _hintPage++;
            ShowHintOverlay();
        }
        else
        {
            OnHintClosePressed();
        }
    }

    private void OnHintClosePressed()
    {
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

/// <summary>
/// Custom Button für Sudoku-Zellen
/// </summary>
public partial class SudokuCellButton : Button
{
    [Signal]
    public delegate void CellClickedEventHandler(int row, int col);

    [Signal]
    public delegate void CellHoveredEventHandler(int row, int col);

    public int Row { get; }
    public int Col { get; }

    private int _value;
    private bool _isGiven;
    private bool _isSelected;
    private bool _isMultiSelected;
    private bool _isHighlighted;
    private bool _isRelated;
    private bool _isFlashingError;
    private double _flashTimer;

    // Grid-Konfiguration (dynamisch für Kids vs. Standard)
    private int _gridSize = 9;
    private int _blockSize = 3;

    // Notes/Candidates Display
    private GridContainer? _notesGrid;
    private Label[] _noteLabels = new Label[9];
    private bool[] _notes = new bool[9];
    private bool[] _candidates = new bool[9];
    private bool _showNotes;
    private bool _showCandidates;

    public SudokuCellButton(int row, int col)
    {
        Row = row;
        Col = col;
        FocusMode = FocusModeEnum.None;
        ClipText = false;
    }

    public void SetGridConfig(int gridSize, int blockSize)
    {
        _gridSize = gridSize;
        _blockSize = blockSize;
    }

    public override void _Ready()
    {
        Pressed += OnPressed;
        MouseEntered += OnMouseEntered;
        CreateNotesGrid();
    }

    public override void _Process(double delta)
    {
        if (_isFlashingError)
        {
            _flashTimer -= delta;
            if (_flashTimer <= 0)
            {
                _isFlashingError = false;
                UpdateAppearance();
            }
        }
    }

    private void OnPressed()
    {
        EmitSignal(SignalName.CellClicked, Row, Col);
    }

    private void OnMouseEntered()
    {
        EmitSignal(SignalName.CellHovered, Row, Col);
    }

    private void CreateNotesGrid()
    {
        // Erstelle ein Grid für die Notizen/Kandidaten
        // Größe basiert auf _gridSize: 2x2 für Kids (4), 3x3 für Standard (9)
        int notesColumns = _gridSize == 4 ? 2 : 3;
        int notesCount = _gridSize;

        _notesGrid = new GridContainer();
        _notesGrid.Columns = notesColumns;
        _notesGrid.SetAnchorsPreset(LayoutPreset.FullRect);
        _notesGrid.GrowHorizontal = GrowDirection.Both;
        _notesGrid.GrowVertical = GrowDirection.Both;
        _notesGrid.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill;
        _notesGrid.SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill;
        _notesGrid.AddThemeConstantOverride("h_separation", 0);
        _notesGrid.AddThemeConstantOverride("v_separation", 0);
        _notesGrid.Visible = false;

        for (int i = 0; i < notesCount; i++)
        {
            var label = new Label();
            label.Text = "";
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill;
            label.SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill;
            // Größere Schrift für Kids (weniger Zahlen, mehr Platz)
            label.AddThemeFontSizeOverride("font_size", _gridSize == 4 ? 14 : 10);
            _noteLabels[i] = label;
            _notesGrid.AddChild(label);
        }

        AddChild(_notesGrid);
    }

    public void SetNotes(bool[] notes, bool showNotes)
    {
        _notes = notes;
        _showNotes = showNotes;
        UpdateNotesDisplay();
    }

    public void SetCandidates(bool[] candidates, bool showCandidates)
    {
        _candidates = candidates;
        _showCandidates = showCandidates;
        UpdateNotesDisplay();
    }

    private void UpdateNotesDisplay()
    {
        if (_notesGrid == null) return;

        var theme = ThemeService.Instance;
        if (theme == null) return;

        var colors = theme.CurrentColors;

        // Wenn ein Wert gesetzt ist, keine Notizen anzeigen
        if (_value != 0)
        {
            _notesGrid.Visible = false;
            return;
        }

        bool hasAnyToShow = false;
        int notesCount = _gridSize;

        for (int i = 0; i < notesCount; i++)
        {
            bool showNote = _showNotes && i < _notes.Length && _notes[i];
            bool showCandidate = _showCandidates && i < _candidates.Length && _candidates[i] && !(i < _notes.Length && _notes[i]);

            if (showNote)
            {
                _noteLabels[i].Text = (i + 1).ToString();
                _noteLabels[i].AddThemeColorOverride("font_color", colors.Accent); // Blau für Notizen (Theme-aware)
                hasAnyToShow = true;
            }
            else if (showCandidate)
            {
                _noteLabels[i].Text = (i + 1).ToString();
                _noteLabels[i].AddThemeColorOverride("font_color", colors.TextSecondary); // Grau für Kandidaten (Theme-aware)
                hasAnyToShow = true;
            }
            else
            {
                _noteLabels[i].Text = "";
            }
        }

        _notesGrid.Visible = hasAnyToShow;
    }

    public void SetValue(int value, bool isGiven)
    {
        _value = value;
        _isGiven = isGiven;
        Text = value > 0 ? value.ToString() : "";
        UpdateAppearance();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateAppearance();
    }

    public void SetHighlighted(bool highlighted)
    {
        _isHighlighted = highlighted;
        UpdateAppearance();
    }

    public void SetRelated(bool related)
    {
        _isRelated = related;
        UpdateAppearance();
    }

    public void SetMultiSelected(bool multiSelected)
    {
        _isMultiSelected = multiSelected;
        UpdateAppearance();
    }

    public void FlashError()
    {
        _isFlashingError = true;
        _flashTimer = 0.5;
        UpdateAppearance();
    }

    public void ApplyTheme(ThemeService theme)
    {
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        var theme = ThemeService.Instance;
        if (theme == null) return;

        var colors = theme.CurrentColors;

        // StyleBox basierend auf Zustand
        var style = theme.CreateCellStyleBox(
            _isGiven,
            _isSelected,
            _isHighlighted,
            _isRelated,
            _isFlashingError,
            Row,
            Col
        );

        // Multi-Select Styling überschreibt normale Hintergrundfarbe
        if (_isMultiSelected && !_isSelected)
        {
            var multiSelectColor = colors.Accent.Lerp(colors.CellBackground, 0.6f);
            style.BgColor = multiSelectColor;
        }

        // Related-Zellen bekommen subtilen blauen Hintergrund-Tint (statt Border)
        if (_isRelated && !_isSelected && !_isMultiSelected)
        {
            var relatedBgColor = colors.Accent.Lerp(colors.CellBackground, 0.85f);
            style.BgColor = relatedBgColor;
        }

        // Margins für Grid-Linien
        // Normale Zellen-Grenzen: 1px
        // Block-Grenzen: 3px mit spezieller Farbe (2x2 für Kids, 3x3 für Standard)
        bool isRightBlockBorder = (Col + 1) % _blockSize == 0 && Col < _gridSize - 1;
        bool isBottomBlockBorder = (Row + 1) % _blockSize == 0 && Row < _gridSize - 1;
        bool isLeftBlockBorder = Col % _blockSize == 0 && Col > 0;
        bool isTopBlockBorder = Row % _blockSize == 0 && Row > 0;

        style.ContentMarginRight = isRightBlockBorder ? 3 : 1;
        style.ContentMarginBottom = isBottomBlockBorder ? 3 : 1;
        style.ContentMarginLeft = isLeftBlockBorder ? 3 : 1;
        style.ContentMarginTop = isTopBlockBorder ? 3 : 1;

        // 3x3-Block-Grenzen mit eigener Farbe hervorheben
        var blockBorderColor = colors.GridLineThick;

        // Block-Grenzen setzen (für alle Zellen)
        if (isRightBlockBorder)
        {
            style.BorderWidthRight = 3;
            style.BorderColor = blockBorderColor;
        }
        if (isBottomBlockBorder)
        {
            style.BorderWidthBottom = 3;
            style.BorderColor = blockBorderColor;
        }
        if (isLeftBlockBorder)
        {
            style.BorderWidthLeft = 3;
            style.BorderColor = blockBorderColor;
        }
        if (isTopBlockBorder)
        {
            style.BorderWidthTop = 3;
            style.BorderColor = blockBorderColor;
        }

        AddThemeStyleboxOverride("normal", style);
        AddThemeStyleboxOverride("hover", style);
        AddThemeStyleboxOverride("pressed", style);
        AddThemeStyleboxOverride("focus", style);

        // Textfarbe
        Color textColor;
        if (_isFlashingError)
            textColor = colors.TextError;
        else if (_isGiven)
            textColor = colors.TextGiven;
        else
            textColor = colors.TextUser;

        AddThemeColorOverride("font_color", textColor);
        AddThemeColorOverride("font_hover_color", textColor);
        AddThemeColorOverride("font_pressed_color", textColor);

        // Größere Schrift für Kids-Modus (größere Zellen)
        int fontSize = _gridSize == 4 ? 36 : 24;
        AddThemeFontSizeOverride("font_size", fontSize);
    }
}
