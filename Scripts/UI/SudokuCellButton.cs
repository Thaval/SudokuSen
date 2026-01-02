namespace SudokuSen.UI;

/// <summary>
/// Custom Button für Sudoku-Zellen mit Unterstützung für Notizen, Kandidaten und visuelle Zustände.
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
    private bool _isHistoryEntry;
    private bool _isHistoryCurrent;

    // Grid-Konfiguration (dynamisch für Kids vs. Standard)
    private int _gridSize = 9;
    private int _blockSize = 3;

    // Notes/Candidates Display
    private GridContainer? _notesGrid;
    private readonly Label[] _noteLabels = new Label[9];
    private bool[] _notes = new bool[9];
    private bool[] _candidates = new bool[9];
    private bool _showNotes;
    private bool _showCandidates;
    private int _notesMask;
    private int _candidatesMask;

    public SudokuCellButton(int row, int col)
    {
        Row = row;
        Col = col;
        FocusMode = FocusModeEnum.None;
        ClipText = false;
        // Disable processing by default - only enable during flash animation
        SetProcess(false);
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
        if (!_isFlashingError) return;

        _flashTimer -= delta;
        if (_flashTimer <= 0)
        {
            _isFlashingError = false;
            SetProcess(false); // Disable processing when flash ends
            UpdateAppearance();
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
        // Grid für Notizen/Kandidaten: 2x2 für Kids (4), 3x3 für Standard (9)
        int notesColumns = _gridSize == 4 ? 2 : 3;
        int notesCount = _gridSize;

        _notesGrid = new GridContainer
        {
            Columns = notesColumns,
            SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill,
            Visible = false
        };
        _notesGrid.SetAnchorsPreset(LayoutPreset.FullRect);
        _notesGrid.GrowHorizontal = GrowDirection.Both;
        _notesGrid.GrowVertical = GrowDirection.Both;
        _notesGrid.AddThemeConstantOverride("h_separation", 0);
        _notesGrid.AddThemeConstantOverride("v_separation", 0);

        int fontSize = _gridSize == 4 ? 14 : 10;
        for (int i = 0; i < notesCount; i++)
        {
            var label = new Label
            {
                Text = "",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            _noteLabels[i] = label;
            _notesGrid.AddChild(label);
        }

        AddChild(_notesGrid);
    }

    public void SetNotes(bool[] notes, bool showNotes)
    {
        int newMask = ComputeMask(notes, _gridSize);
        if (_showNotes == showNotes && _notesMask == newMask)
        {
            _notes = notes;
            return;
        }

        _notes = notes;
        _showNotes = showNotes;
        _notesMask = newMask;
        UpdateNotesDisplay();
    }

    public void SetCandidates(bool[] candidates, bool showCandidates)
    {
        int newMask = ComputeMask(candidates, _gridSize);
        if (_showCandidates == showCandidates && _candidatesMask == newMask)
        {
            _candidates = candidates;
            return;
        }

        _candidates = candidates;
        _showCandidates = showCandidates;
        _candidatesMask = newMask;
        UpdateNotesDisplay();
    }

    private static int ComputeMask(bool[] values, int gridSize)
    {
        int count = Math.Min(gridSize, Math.Min(9, values.Length));
        int mask = 0;
        for (int i = 0; i < count; i++)
        {
            if (values[i]) mask |= 1 << i;
        }
        return mask;
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
                _noteLabels[i].AddThemeColorOverride("font_color", colors.Accent);
                hasAnyToShow = true;
            }
            else if (showCandidate)
            {
                _noteLabels[i].Text = (i + 1).ToString();
                _noteLabels[i].AddThemeColorOverride("font_color", colors.TextSecondary);
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
        if (_value == value && _isGiven == isGiven) return;
        _value = value;
        _isGiven = isGiven;
        Text = value > 0 ? value.ToString() : "";
        UpdateAppearance();
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected) return;
        _isSelected = selected;
        UpdateAppearance();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (_isHighlighted == highlighted) return;
        _isHighlighted = highlighted;
        UpdateAppearance();
    }

    public void SetRelated(bool related)
    {
        if (_isRelated == related) return;
        _isRelated = related;
        UpdateAppearance();
    }

    public void SetMultiSelected(bool multiSelected)
    {
        if (_isMultiSelected == multiSelected) return;
        _isMultiSelected = multiSelected;
        UpdateAppearance();
    }

    public void SetHistoryEntry(bool historyEntry)
    {
        if (_isHistoryEntry == historyEntry) return;
        _isHistoryEntry = historyEntry;
        UpdateAppearance();
    }

    public void SetHistoryCurrent(bool historyCurrent)
    {
        if (_isHistoryCurrent == historyCurrent) return;
        _isHistoryCurrent = historyCurrent;
        UpdateAppearance();
    }

    public void FlashError()
    {
        _isFlashingError = true;
        _flashTimer = 0.5;
        SetProcess(true); // Enable processing for flash animation
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
            style.BgColor = colors.Accent.Lerp(colors.CellBackground, 0.6f);
        }

        // Related-Zellen bekommen subtilen blauen Hintergrund-Tint
        if (_isRelated && !_isSelected && !_isMultiSelected)
        {
            style.BgColor = colors.Accent.Lerp(colors.CellBackground, 0.85f);
        }

        // Block-Grenzen berechnen
        bool isRightBlockBorder = (Col + 1) % _blockSize == 0 && Col < _gridSize - 1;
        bool isBottomBlockBorder = (Row + 1) % _blockSize == 0 && Row < _gridSize - 1;
        bool isLeftBlockBorder = Col % _blockSize == 0 && Col > 0;
        bool isTopBlockBorder = Row % _blockSize == 0 && Row > 0;

        style.ContentMarginRight = isRightBlockBorder ? 3 : 1;
        style.ContentMarginBottom = isBottomBlockBorder ? 3 : 1;
        style.ContentMarginLeft = isLeftBlockBorder ? 3 : 1;
        style.ContentMarginTop = isTopBlockBorder ? 3 : 1;

        // Block-Grenzen mit spezieller Farbe
        var blockBorderColor = colors.GridLineThick;
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

        // History replay highlighting
        if (_isHistoryCurrent)
        {
            // Orange background for current step cell
            style.BgColor = new Color("ff9800").Lerp(colors.CellBackground, 0.5f);
            style.BorderColor = new Color("ff9800");
            style.BorderWidthTop = Math.Max(style.BorderWidthTop, 3);
            style.BorderWidthBottom = Math.Max(style.BorderWidthBottom, 3);
            style.BorderWidthLeft = Math.Max(style.BorderWidthLeft, 3);
            style.BorderWidthRight = Math.Max(style.BorderWidthRight, 3);
        }
        else if (_isHistoryEntry)
        {
            // Blue tint for all replayed entries
            style.BgColor = colors.Accent.Lerp(colors.CellBackground, 0.6f);
        }

        AddThemeStyleboxOverride("normal", style);
        AddThemeStyleboxOverride("hover", style);
        AddThemeStyleboxOverride("pressed", style);
        AddThemeStyleboxOverride("focus", style);
        AddThemeStyleboxOverride("disabled", style);

        // Textfarbe basierend auf Zustand
        Color textColor = _isFlashingError ? colors.TextError
            : _isGiven ? colors.TextGiven
            : colors.TextUser;

        if (_isHistoryCurrent)
        {
            textColor = new Color("ff9800"); // Orange for current step
        }
        else if (_isHistoryEntry && !_isGiven)
        {
            textColor = colors.Accent; // Blue for all replayed entries
        }

        AddThemeColorOverride("font_color", textColor);
        AddThemeColorOverride("font_hover_color", textColor);
        AddThemeColorOverride("font_pressed_color", textColor);
        AddThemeColorOverride("font_disabled_color", textColor);

        // Schriftgröße: größer für Kids-Modus
        AddThemeFontSizeOverride("font_size", _gridSize == 4 ? 36 : 24);
    }
}
