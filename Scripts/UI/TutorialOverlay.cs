using SudokuSen.Models;
using SudokuSen.Services;

namespace SudokuSen.UI;

/// <summary>
/// Overlay that displays tutorial messages, arrows, and highlights.
/// Should be added as a child of the GameScene.
/// </summary>
public partial class TutorialOverlay : Control
{
    // Visual components
    private PanelContainer? _messagePanel;
    private Label? _messageTitle;
    private Label? _messageText;
    private Button? _continueButton;
    private Button? _skipButton;
    private Control? _arrowContainer;
    private Control? _highlightContainer;

    // Services
    private TutorialService? _tutorialService;
    private ThemeService? _themeService;
    private AudioService? _audioService;

    // Animation state
    private bool _isAnimating = false;
    private double _animationTime = 0;
    private Vector2 _arrowTargetPos = Vector2.Zero;
    private Rect2? _arrowTargetRect = null; // For calculating edge intersection
    private bool _showArrow = false;
    private List<(Vector2 Pos, Rect2? Rect)> _additionalArrowTargets = new();

    // Highlight state
    private readonly List<(Rect2 Rect, Color Color, HighlightStyle Style)> _cellHighlights = new();
    private double _pulseTime = 0;

    // References to game UI elements (set by GameScene)
    public Control? GridContainer { get; set; }
    public Control? NumberPad { get; set; }
    public Button? BackButton { get; set; }
    public Label? TimerLabel { get; set; }
    public Label? MistakesLabel { get; set; }
    public Label? DifficultyLabel { get; set; }
    public Button? NotesToggle { get; set; }
    public Button? HintButton { get; set; }
    public Button? AutoNotesButton { get; set; }
    public Button? EraseButton { get; set; }
    public Button? HouseAutoFillButton { get; set; }
    public Func<int, int, Rect2>? GetCellRect { get; set; }
    public Label[]? ColLabels { get; set; }
    public Label[]? RowLabels { get; set; }

    public override void _Ready()
    {
        _tutorialService = GetNode<TutorialService>("/root/TutorialService");
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _audioService = GetNode<AudioService>("/root/AudioService");

        // Set up to cover the entire screen
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore; // Let clicks through by default

        CreateUI();
        ConnectSignals();
        Hide(); // Hidden by default
    }

    private void CreateUI()
    {
        // Highlight container (drawn behind everything)
        _highlightContainer = new Control();
        _highlightContainer.Name = "HighlightContainer";
        _highlightContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _highlightContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_highlightContainer);

        // Arrow container
        _arrowContainer = new Control();
        _arrowContainer.Name = "ArrowContainer";
        _arrowContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _arrowContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_arrowContainer);

        // Message panel
        _messagePanel = new PanelContainer();
        _messagePanel.Name = "MessagePanel";
        _messagePanel.CustomMinimumSize = new Vector2(280, 280); // Fixed height for consistent button position
        _messagePanel.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_messagePanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        _messagePanel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        _messageTitle = new Label();
        _messageTitle.Name = "Title";
        _messageTitle.AddThemeFontSizeOverride("font_size", 24);
        _messageTitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_messageTitle);

        _messageText = new Label();
        _messageText.Name = "Text";
        _messageText.AddThemeFontSizeOverride("font_size", 16);
        _messageText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _messageText.CustomMinimumSize = new Vector2(230, 0); // Narrower to fit panel
        _messageText.SizeFlagsVertical = SizeFlags.ExpandFill; // Expand to push buttons down
        vbox.AddChild(_messageText);

        var buttonBox = new HBoxContainer();
        buttonBox.AddThemeConstantOverride("separation", 12);
        buttonBox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(buttonBox);

        _skipButton = new Button();
        _skipButton.Text = "← Zurück";
        _skipButton.CustomMinimumSize = new Vector2(120, 40);
        _skipButton.Pressed += OnBackPressed;
        buttonBox.AddChild(_skipButton);

        _continueButton = new Button();
        _continueButton.Text = "Weiter →";
        _continueButton.CustomMinimumSize = new Vector2(120, 40);
        _continueButton.Pressed += OnContinuePressed;
        buttonBox.AddChild(_continueButton);

        _messagePanel.Hide();
        ApplyTheme();
    }

    private void ConnectSignals()
    {
        if (_tutorialService == null) return;

        _tutorialService.TutorialStarted += OnTutorialStarted;
        _tutorialService.TutorialEnded += OnTutorialEnded;
        _tutorialService.MessageRequested += OnMessageRequested;
        _tutorialService.HighlightCellsRequested += OnHighlightCellsRequested;
        _tutorialService.HighlightHouseRequested += OnHighlightHouseRequested;
        _tutorialService.PointToElementRequested += OnPointToElementRequested;
        _tutorialService.ClearHighlightsRequested += OnClearHighlightsRequested;
        _tutorialService.WaitingForAction += OnWaitingForAction;
    }

    public override void _ExitTree()
    {
        if (_tutorialService != null)
        {
            _tutorialService.TutorialStarted -= OnTutorialStarted;
            _tutorialService.TutorialEnded -= OnTutorialEnded;
            _tutorialService.MessageRequested -= OnMessageRequested;
            _tutorialService.HighlightCellsRequested -= OnHighlightCellsRequested;
            _tutorialService.HighlightHouseRequested -= OnHighlightHouseRequested;
            _tutorialService.PointToElementRequested -= OnPointToElementRequested;
            _tutorialService.ClearHighlightsRequested -= OnClearHighlightsRequested;
            _tutorialService.WaitingForAction -= OnWaitingForAction;
        }
    }

    public override void _Process(double delta)
    {
        _pulseTime += delta;
        _animationTime += delta;

        // Request redraw for animated highlights
        if (_cellHighlights.Count > 0 || _showArrow)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        // Draw cell highlights
        // Note: GetCellRect returns global positions, but _Draw uses local coordinates
        // Since the overlay is full-rect anchored at (0,0), we need to convert
        var overlayGlobalPos = GlobalPosition;

        foreach (var (rect, color, style) in _cellHighlights)
        {
            // Convert global rect to local coordinates
            var localRect = new Rect2(rect.Position - overlayGlobalPos, rect.Size);
            DrawHighlight(localRect, color, style);
        }

        // Draw arrows
        if (_showArrow && _messagePanel != null && _messagePanel.Visible)
        {
            var fromGlobal = _messagePanel.GlobalPosition + _messagePanel.Size / 2;
            var from = fromGlobal - overlayGlobalPos;

            // Calculate arrow endpoint at edge of target, not center
            Vector2 toGlobal = _arrowTargetRect.HasValue
                ? GetRectEdgeIntersection(fromGlobal, _arrowTargetRect.Value)
                : _arrowTargetPos;
            var to = toGlobal - overlayGlobalPos;
            DrawArrow(from, to);

            // Draw additional arrows
            foreach (var (additionalPos, additionalRect) in _additionalArrowTargets)
            {
                Vector2 additionalToGlobal = additionalRect.HasValue
                    ? GetRectEdgeIntersection(fromGlobal, additionalRect.Value)
                    : additionalPos;
                var additionalTo = additionalToGlobal - overlayGlobalPos;
                DrawArrow(from, additionalTo);
            }
        }
    }

    private void DrawHighlight(Rect2 rect, Color color, HighlightStyle style)
    {
        float pulse = (float)(0.5 + 0.5 * Math.Sin(_pulseTime * 4));

        switch (style)
        {
            case HighlightStyle.Pulse:
                var pulseColor = new Color(color.R, color.G, color.B, 0.3f + 0.4f * pulse);
                DrawRect(rect, pulseColor);
                DrawRect(rect, new Color(color.R, color.G, color.B, 0.8f), false, 3);
                break;

            case HighlightStyle.Glow:
                // Draw multiple expanding rectangles for glow effect
                for (int i = 3; i >= 0; i--)
                {
                    var glowRect = rect.Grow(i * 4);
                    var alpha = (0.2f - i * 0.04f) * (0.7f + 0.3f * pulse);
                    DrawRect(glowRect, new Color(color.R, color.G, color.B, alpha));
                }
                break;

            case HighlightStyle.Outline:
                DrawRect(rect, new Color(color.R, color.G, color.B, 0.9f), false, 4);
                break;

            case HighlightStyle.Fill:
                DrawRect(rect, new Color(color.R, color.G, color.B, 0.4f));
                break;

            case HighlightStyle.Arrow:
                DrawRect(rect, new Color(color.R, color.G, color.B, 0.3f));
                // Arrow is drawn separately
                break;
        }
    }

    private void DrawArrow(Vector2 from, Vector2 to)
    {
        var colors = _themeService?.CurrentColors;
        var arrowColor = colors?.Accent ?? Colors.Yellow;

        // Draw line (no anti-aliasing to prevent artifacts)
        DrawLine(from, to, arrowColor, 3, false);

        // Draw arrowhead as filled triangle
        var dir = (to - from).Normalized();
        var perpendicular = new Vector2(-dir.Y, dir.X);
        var arrowSize = 15f;

        var arrowHead = new Vector2[]
        {
            to,
            to - dir * arrowSize + perpendicular * arrowSize * 0.5f,
            to - dir * arrowSize - perpendicular * arrowSize * 0.5f
        };

        // Use single color array for solid fill
        DrawPolygon(arrowHead, new Color[] { arrowColor });
    }

    private void OnTutorialStarted(string tutorialId)
    {
        Show();
        GD.Print($"[TutorialOverlay] Tutorial started: {tutorialId}");
    }

    private void OnTutorialEnded(string tutorialId, bool completed)
    {
        _messagePanel?.Hide();
        _cellHighlights.Clear();
        _showArrow = false;
        Hide();
        GD.Print($"[TutorialOverlay] Tutorial ended: {tutorialId}, completed={completed}");
    }

    private void OnMessageRequested(string message, string title, int position, string pointToJson)
    {
        if (_messagePanel == null || _messageTitle == null || _messageText == null) return;

        _messageTitle.Text = string.IsNullOrEmpty(title) ? "" : title;
        _messageTitle.Visible = !string.IsNullOrEmpty(title);
        _messageText.Text = message;

        // Reset button state - check if tutorial is waiting for action
        if (_continueButton != null)
        {
            // Check if the current step requires user action
            bool isWaitingForAction = _tutorialService?.CurrentStep is ShowMessageStep msgStep && msgStep.WaitForAction.HasValue;
            GD.Print($"[TutorialOverlay] OnMessageRequested: isWaitingForAction={isWaitingForAction}");

            if (isWaitingForAction)
            {
                GD.Print($"[TutorialOverlay] Setting button to DISABLED state");
                _continueButton.Text = "Warten...";
                _continueButton.Disabled = true;
                ApplyDisabledButtonStyle();
            }
            else
            {
                GD.Print($"[TutorialOverlay] Setting button to NORMAL state");
                _continueButton.Text = "Weiter →";
                _continueButton.Disabled = false;
                ApplyNormalButtonStyle();
            }
        }

        _messagePanel.Show();
        PositionMessagePanel((MessagePosition)position);

        // Handle pointing
        _showArrow = false;
        _additionalArrowTargets.Clear();
        if (!string.IsNullOrEmpty(pointToJson))
        {
            // Check if it's multiple targets (separated by ;)
            var targetJsons = pointToJson.Split(';');
            bool firstTarget = true;
            foreach (var targetJson in targetJsons)
            {
                if (string.IsNullOrWhiteSpace(targetJson)) continue;
                var pointTo = DeserializeTarget(targetJson.Trim());
                if (pointTo != null)
                {
                    var (targetPos, targetRect) = GetTargetPositionAndRect(pointTo);
                    if (targetPos.HasValue)
                    {
                        if (firstTarget)
                        {
                            _arrowTargetPos = targetPos.Value;
                            _arrowTargetRect = targetRect;
                            _showArrow = true;
                            firstTarget = false;
                        }
                        else
                        {
                            _additionalArrowTargets.Add((targetPos.Value, targetRect));
                        }
                    }
                }
            }
        }

        _audioService?.PlayClick();
        QueueRedraw();
    }

    private void OnHighlightCellsRequested(int[] positions, string styleStr, string colorStr)
    {
        _cellHighlights.Clear();

        var style = Enum.TryParse<HighlightStyle>(styleStr, out var s) ? s : HighlightStyle.Pulse;
        var color = string.IsNullOrEmpty(colorStr)
            ? (_themeService?.CurrentColors.Accent ?? Colors.Yellow)
            : new Color(colorStr);

        GD.Print($"[TutorialOverlay] HighlightCells requested: {positions.Length / 2} cells, style={style}, GetCellRect={(GetCellRect != null ? "set" : "NULL")}");

        // positions is [row1, col1, row2, col2, ...]
        for (int i = 0; i < positions.Length - 1; i += 2)
        {
            int row = positions[i];
            int col = positions[i + 1];

            if (GetCellRect != null)
            {
                var rect = GetCellRect(row, col);
                GD.Print($"[TutorialOverlay] Cell ({row},{col}) rect: pos={rect.Position}, size={rect.Size}");
                _cellHighlights.Add((rect, color, style));
            }
            else
            {
                GD.PrintErr($"[TutorialOverlay] GetCellRect is NULL, cannot highlight cell ({row},{col})");
            }
        }

        GD.Print($"[TutorialOverlay] Total highlights: {_cellHighlights.Count}");
        QueueRedraw();
    }

    private void OnHighlightHouseRequested(string houseTypeStr, int index, string styleStr)
    {
        if (!Enum.TryParse<HouseType>(houseTypeStr, out var houseType)) return;
        var style = Enum.TryParse<HighlightStyle>(styleStr, out var s) ? s : HighlightStyle.Glow;
        var color = _themeService?.CurrentColors.Accent ?? Colors.Yellow;

        // Get all cells in the house
        var cells = new List<(int Row, int Col)>();

        switch (houseType)
        {
            case HouseType.Row:
                for (int c = 0; c < 9; c++) cells.Add((index, c));
                break;
            case HouseType.Column:
                for (int r = 0; r < 9; r++) cells.Add((r, index));
                break;
            case HouseType.Block:
                int startRow = (index / 3) * 3;
                int startCol = (index % 3) * 3;
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        cells.Add((startRow + r, startCol + c));
                break;
        }

        foreach (var (row, col) in cells)
        {
            if (GetCellRect != null)
            {
                var rect = GetCellRect(row, col);
                _cellHighlights.Add((rect, color, style));
            }
        }

        QueueRedraw();
    }

    private void OnPointToElementRequested(string targetJson, string message)
    {
        var target = DeserializeTarget(targetJson);
        if (target == null) return;

        var (targetPos, targetRect) = GetTargetPositionAndRect(target);
        if (targetPos.HasValue)
        {
            _arrowTargetPos = targetPos.Value;
            _arrowTargetRect = targetRect;
            _showArrow = true;

            if (!string.IsNullOrEmpty(message) && _messagePanel != null && _messageTitle != null && _messageText != null)
            {
                _messageTitle.Text = "";
                _messageTitle.Visible = false;
                _messageText.Text = message;
                _messagePanel.Show();
                PositionMessagePanel(MessagePosition.Center);
            }

            QueueRedraw();
        }
    }

    /// <summary>
    /// Deserializes a target from the string format: "Type|CellRow|CellCol|ButtonId"
    /// </summary>
    private TutorialTarget? DeserializeTarget(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        var parts = json.Split('|');
        if (parts.Length < 4) return null;

        if (!Enum.TryParse<TargetType>(parts[0], out var type)) return null;
        int.TryParse(parts[1], out var row);
        int.TryParse(parts[2], out var col);
        var buttonId = parts[3];

        return new TutorialTarget
        {
            Type = type,
            CellPosition = row >= 0 && col >= 0 ? (row, col) : null,
            ButtonId = string.IsNullOrEmpty(buttonId) ? null : buttonId
        };
    }

    private void OnClearHighlightsRequested()
    {
        _cellHighlights.Clear();
        _showArrow = false;
        QueueRedraw();
    }

    private void OnWaitingForAction(string action, string hintMessage)
    {
        GD.Print($"[TutorialOverlay] OnWaitingForAction called: action={action}");
        if (_continueButton != null)
        {
            GD.Print($"[TutorialOverlay] Button BEFORE: Text={_continueButton.Text}, Disabled={_continueButton.Disabled}");
            _continueButton.Text = "Warten...";
            _continueButton.Disabled = true;
            ApplyDisabledButtonStyle();
            GD.Print($"[TutorialOverlay] Button AFTER: Text={_continueButton.Text}, Disabled={_continueButton.Disabled}");
        }

        if (!string.IsNullOrEmpty(hintMessage) && _messageText != null)
        {
            _messageText.Text = hintMessage;
        }
    }

    private void PositionMessagePanel(MessagePosition position)
    {
        if (_messagePanel == null) return;

        var screenSize = GetViewportRect().Size;
        var panelSize = _messagePanel.Size;

        // For CenterLeft, align with the Menu button (starts at ~20px from left)
        float panelX = 20; // Align with Menu button

        // Calculate panel position based on MessagePosition
        Vector2 pos = position switch
        {
            MessagePosition.TopLeft => new Vector2(20, 80),
            MessagePosition.TopCenter => new Vector2((screenSize.X - panelSize.X) / 2, 80),
            MessagePosition.TopRight => new Vector2(screenSize.X - panelSize.X - 20, 80),
            MessagePosition.CenterLeft => new Vector2(panelX, (screenSize.Y - panelSize.Y) / 2),
            MessagePosition.Center => new Vector2((screenSize.X - panelSize.X) / 2, (screenSize.Y - panelSize.Y) / 2),
            MessagePosition.CenterRight => new Vector2(screenSize.X - panelSize.X - 20, (screenSize.Y - panelSize.Y) / 2),
            MessagePosition.BottomLeft => new Vector2(20, screenSize.Y - panelSize.Y - 100),
            MessagePosition.BottomCenter => new Vector2((screenSize.X - panelSize.X) / 2, screenSize.Y - panelSize.Y - 100),
            MessagePosition.BottomRight => new Vector2(screenSize.X - panelSize.X - 20, screenSize.Y - panelSize.Y - 100),
            _ => new Vector2((screenSize.X - panelSize.X) / 2, (screenSize.Y - panelSize.Y) / 2)
        };

        _messagePanel.Position = pos;
    }

    private (Vector2? Position, Rect2? Rect) GetTargetPositionAndRect(TutorialTarget target)
    {
        Control? targetControl = target.Type switch
        {
            TargetType.BackButton => BackButton,
            TargetType.Timer => TimerLabel,
            TargetType.MistakesLabel => MistakesLabel,
            TargetType.DifficultyLabel => DifficultyLabel,
            TargetType.NotesToggle => NotesToggle,
            TargetType.HintButton => HintButton,
            TargetType.AutoNotesButton => AutoNotesButton,
            TargetType.EraseButton => EraseButton,
            TargetType.HouseAutoFillButton => HouseAutoFillButton,
            TargetType.Grid => GridContainer,
            _ => null
        };

        // Special handling for GridEdge - point to top-left corner of grid
        if (target.Type == TargetType.GridEdge && GridContainer != null)
        {
            return (GridContainer.GlobalPosition + new Vector2(20, 20), null);
        }

        // Special handling for AxisLabels - point to column labels area
        if (target.Type == TargetType.AxisLabels && GridContainer != null)
        {
            // Point to the area above/left of grid where labels are
            return (GridContainer.GlobalPosition + new Vector2(-10, -10), null);
        }

        // Special handling for ColumnLabel - point to specific column header (A-I)
        if (target.Type == TargetType.ColumnLabel && ColLabels != null && !string.IsNullOrEmpty(target.ButtonId))
        {
            // ButtonId should be "A"-"I" (0-8)
            int colIndex = target.ButtonId[0] - 'A';
            if (colIndex >= 0 && colIndex < ColLabels.Length && ColLabels[colIndex] != null)
            {
                var label = ColLabels[colIndex];
                var rect = new Rect2(label.GlobalPosition, label.Size);
                return (label.GlobalPosition + label.Size / 2, rect);
            }
        }

        // Special handling for RowLabel - point to specific row label (1-9)
        if (target.Type == TargetType.RowLabel && RowLabels != null && !string.IsNullOrEmpty(target.ButtonId))
        {
            // ButtonId should be "1"-"9" (0-8)
            int rowIndex = int.Parse(target.ButtonId) - 1;
            if (rowIndex >= 0 && rowIndex < RowLabels.Length && RowLabels[rowIndex] != null)
            {
                var label = RowLabels[rowIndex];
                var rect = new Rect2(label.GlobalPosition, label.Size);
                return (label.GlobalPosition + label.Size / 2, rect);
            }
        }

        if (targetControl != null)
        {
            var rect = new Rect2(targetControl.GlobalPosition, targetControl.Size);
            return (targetControl.GlobalPosition + targetControl.Size / 2, rect);
        }

        // For cells
        if (target.Type == TargetType.Cell && target.CellPosition.HasValue && GetCellRect != null)
        {
            var rect = GetCellRect(target.CellPosition.Value.Row, target.CellPosition.Value.Col);
            return (rect.Position + rect.Size / 2, rect);
        }

        // For number pad buttons
        if (target.Type == TargetType.NumberPadButton && NumberPad != null && !string.IsNullOrEmpty(target.ButtonId))
        {
            var btn = NumberPad.FindChild(target.ButtonId, true, false) as Control;
            if (btn != null)
            {
                var rect = new Rect2(btn.GlobalPosition, btn.Size);
                return (btn.GlobalPosition + btn.Size / 2, rect);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Calculate the point where a line from 'from' to the center of 'rect' intersects the rect's edge.
    /// </summary>
    private Vector2 GetRectEdgeIntersection(Vector2 from, Rect2 rect)
    {
        var center = rect.Position + rect.Size / 2;
        var dir = center - from;

        if (dir.LengthSquared() < 0.001f)
            return center;

        // Calculate intersection with each edge and find the closest valid one
        float halfW = rect.Size.X / 2;
        float halfH = rect.Size.Y / 2;

        // Parametric line: P = from + t * dir, where t=1 reaches center
        // We need to find where it intersects the rect edges

        float tMin = 1f; // Start at center

        // Left edge (x = rect.Position.X)
        if (Math.Abs(dir.X) > 0.001f)
        {
            float t = (rect.Position.X - from.X) / dir.X;
            if (t > 0 && t < tMin)
            {
                float y = from.Y + t * dir.Y;
                if (y >= rect.Position.Y && y <= rect.Position.Y + rect.Size.Y)
                    tMin = t;
            }
        }

        // Right edge (x = rect.Position.X + rect.Size.X)
        if (Math.Abs(dir.X) > 0.001f)
        {
            float t = (rect.Position.X + rect.Size.X - from.X) / dir.X;
            if (t > 0 && t < tMin)
            {
                float y = from.Y + t * dir.Y;
                if (y >= rect.Position.Y && y <= rect.Position.Y + rect.Size.Y)
                    tMin = t;
            }
        }

        // Top edge (y = rect.Position.Y)
        if (Math.Abs(dir.Y) > 0.001f)
        {
            float t = (rect.Position.Y - from.Y) / dir.Y;
            if (t > 0 && t < tMin)
            {
                float x = from.X + t * dir.X;
                if (x >= rect.Position.X && x <= rect.Position.X + rect.Size.X)
                    tMin = t;
            }
        }

        // Bottom edge (y = rect.Position.Y + rect.Size.Y)
        if (Math.Abs(dir.Y) > 0.001f)
        {
            float t = (rect.Position.Y + rect.Size.Y - from.Y) / dir.Y;
            if (t > 0 && t < tMin)
            {
                float x = from.X + t * dir.X;
                if (x >= rect.Position.X && x <= rect.Position.X + rect.Size.X)
                    tMin = t;
            }
        }

        return from + tMin * dir;
    }

    private void OnContinuePressed()
    {
        _audioService?.PlayClick();
        _tutorialService?.OnUserClick();
        // Button state is now handled by OnMessageRequested based on the next step's WaitForAction
    }

    private void OnBackPressed()
    {
        _audioService?.PlayClick();
        _tutorialService?.PreviousStep();
    }

    private void ApplyTheme()
    {
        if (_themeService == null || _messagePanel == null) return;

        var colors = _themeService.CurrentColors;
        var panelStyle = _themeService.CreatePanelStyleBox(16, 2);
        _messagePanel.AddThemeStyleboxOverride("panel", panelStyle);

        _messageTitle?.AddThemeColorOverride("font_color", colors.Accent);
        _messageText?.AddThemeColorOverride("font_color", colors.TextPrimary);

        if (_continueButton != null)
        {
            _continueButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
            _continueButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
            _continueButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        }

        if (_skipButton != null)
        {
            _skipButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
            _skipButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
            _skipButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        }
    }

    private void ApplyNormalButtonStyle()
    {
        if (_continueButton == null || _themeService == null) return;

        // Set text to "Weiter →"
        _continueButton.Text = "Weiter →";

        // Restore focus mode
        _continueButton.FocusMode = Control.FocusModeEnum.All;

        var colors = _themeService.CurrentColors;
        _continueButton.AddThemeColorOverride("font_color", colors.TextPrimary);
        _continueButton.AddThemeColorOverride("font_hover_color", colors.TextPrimary);
        _continueButton.AddThemeColorOverride("font_pressed_color", colors.TextPrimary);
        _continueButton.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
        _continueButton.AddThemeColorOverride("font_focus_color", colors.TextPrimary);
        _continueButton.AddThemeStyleboxOverride("normal", _themeService.CreateButtonStyleBox());
        _continueButton.AddThemeStyleboxOverride("hover", _themeService.CreateButtonStyleBox(hover: true));
        _continueButton.AddThemeStyleboxOverride("pressed", _themeService.CreateButtonStyleBox(pressed: true));
        _continueButton.AddThemeStyleboxOverride("disabled", _themeService.CreateButtonStyleBox());
    }

    private void ApplyDisabledButtonStyle()
    {
        if (_continueButton == null) return;

        // Set text to "Warten..."
        _continueButton.Text = "Warten...";

        // Grey text for ALL states
        var greyColor = new Color(0.5f, 0.5f, 0.5f, 1.0f); // Grey text
        _continueButton.AddThemeColorOverride("font_color", greyColor);
        _continueButton.AddThemeColorOverride("font_hover_color", greyColor);
        _continueButton.AddThemeColorOverride("font_pressed_color", greyColor);
        _continueButton.AddThemeColorOverride("font_disabled_color", greyColor);
        _continueButton.AddThemeColorOverride("font_focus_color", greyColor);

        // Create disabled style: transparent background, grey border
        var disabledStyle = new StyleBoxFlat();
        disabledStyle.BgColor = new Color(0, 0, 0, 0); // Fully transparent background
        disabledStyle.BorderColor = new Color(0.5f, 0.5f, 0.5f, 1.0f); // Grey border
        disabledStyle.SetBorderWidthAll(1);
        disabledStyle.SetCornerRadiusAll(8);
        disabledStyle.SetContentMarginAll(8);

        // Apply same style to ALL button states - no hover effect
        _continueButton.AddThemeStyleboxOverride("normal", disabledStyle);
        _continueButton.AddThemeStyleboxOverride("hover", disabledStyle);
        _continueButton.AddThemeStyleboxOverride("pressed", disabledStyle);
        _continueButton.AddThemeStyleboxOverride("disabled", disabledStyle);
        _continueButton.AddThemeStyleboxOverride("focus", disabledStyle);

        // Disable focus to prevent visual changes
        _continueButton.FocusMode = Control.FocusModeEnum.None;
    }

    /// <summary>
    /// Called by GameScene when user performs an action during tutorial.
    /// </summary>
    public void NotifyUserAction(ExpectedAction action, (int Row, int Col)? cell = null, int? number = null)
    {
        if (_tutorialService?.IsPlaying == true)
        {
            bool wasCorrect = _tutorialService.OnUserAction(action, cell, number);
            // Button state is now handled by OnMessageRequested when step advances
            // Don't reset here as the next step may also require waiting
        }
    }

    /// <summary>
    /// Called by GameScene when user selects multiple cells during tutorial.
    /// </summary>
    public void NotifyMultiSelectAction(HashSet<(int row, int col)> selectedCells, int? number = null)
    {
        if (_tutorialService?.IsPlaying == true)
        {
            bool wasCorrect = _tutorialService.OnMultiSelectAction(selectedCells, number);
            // Button state is now handled by OnMessageRequested when step advances
            // Don't reset here as the next step may also require waiting
        }
    }
}
