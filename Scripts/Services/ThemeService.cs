namespace SudokuSen.Services;

/// <summary>
/// Autoload: Verwaltet das UI-Theme
/// </summary>
public partial class ThemeService : Node
{
    public static ThemeService? Instance { get; private set; }

    [Signal]
    public delegate void ThemeChangedEventHandler(int themeIndex);

    // Theme-Farben
    public struct ThemeColors
    {
        public Color Background;
        public Color PanelBackground;
        public Color CellBackground;
        public Color CellBackgroundGiven;
        public Color CellBackgroundSelected;
        public Color CellBackgroundHighlighted;
        public Color CellBackgroundRelated;
        public Color CellBackgroundError;
        public Color TextPrimary;
        public Color TextSecondary;
        public Color TextGiven;
        public Color TextUser;
        public Color TextError;
        public Color GridLine;
        public Color GridLineThick;
        public Color ButtonNormal;
        public Color ButtonHover;
        public Color ButtonPressed;
        public Color ButtonDisabled;
        public Color Accent;
    }

    public ThemeColors CurrentColors { get; private set; }

    private int _themeIndex = 0;
    private bool _colorblindEnabled = false;

    private SaveService _saveService = null!;

    // Light Theme
    private readonly ThemeColors _lightTheme = new ThemeColors
    {
        Background = new Color("f5f5f5"),
        PanelBackground = new Color("ffffff"),
        CellBackground = new Color("ffffff"),
        CellBackgroundGiven = new Color("e8e8e8"),
        CellBackgroundSelected = new Color("bbdefb"),
        CellBackgroundHighlighted = new Color("c8e6c9"),
        CellBackgroundRelated = new Color("f0f0f0"),
        CellBackgroundError = new Color("ffcdd2"),
        TextPrimary = new Color("212121"),
        TextSecondary = new Color("757575"),
        TextGiven = new Color("1a1a1a"),
        TextUser = new Color("1976d2"),
        TextError = new Color("d32f2f"),
        GridLine = new Color("bdbdbd"),
        GridLineThick = new Color("424242"),
        ButtonNormal = new Color("e0e0e0"),
        ButtonHover = new Color("bdbdbd"),
        ButtonPressed = new Color("9e9e9e"),
        ButtonDisabled = new Color("f5f5f5"),
        Accent = new Color("1976d2")
    };

    // Dark Theme
    private readonly ThemeColors _darkTheme = new ThemeColors
    {
        Background = new Color("121212"),
        PanelBackground = new Color("1e1e1e"),
        CellBackground = new Color("2d2d2d"),
        CellBackgroundGiven = new Color("3d3d3d"),
        CellBackgroundSelected = new Color("1565c0"),
        CellBackgroundHighlighted = new Color("2e7d32"),
        CellBackgroundRelated = new Color("383838"),
        CellBackgroundError = new Color("c62828"),
        TextPrimary = new Color("ffffff"),
        TextSecondary = new Color("b0b0b0"),
        TextGiven = new Color("ffffff"),
        TextUser = new Color("64b5f6"),
        TextError = new Color("ef5350"),
        GridLine = new Color("484848"),
        GridLineThick = new Color("808080"),
        ButtonNormal = new Color("2d2d2d"),
        ButtonHover = new Color("3d3d3d"),
        ButtonPressed = new Color("4d4d4d"),
        ButtonDisabled = new Color("1a1a1a"),
        Accent = new Color("1e88e5") // Darker blue for better toggle visibility
    };

    public override void _Ready()
    {
        Instance = this;

        _saveService = GetNode<SaveService>("/root/SaveService");
        _saveService.EnsureLoaded();
        _saveService.SettingsChanged += OnSettingsChanged;

        ApplySettings(_saveService.Settings);
    }

    public override void _ExitTree()
    {
        if (_saveService != null)
            _saveService.SettingsChanged -= OnSettingsChanged;

        if (Instance == this)
            Instance = null;
    }

    private void OnSettingsChanged()
    {
        GD.Print("[ThemeService] SettingsChanged received -> reapplying theme/colorblind/ui scale");
        ApplySettings(_saveService.Settings);
    }

    private void ApplySettings(SettingsData settings)
    {
        _themeIndex = settings.ThemeIndex;
        _colorblindEnabled = settings.ColorblindPaletteEnabled;
        CurrentColors = BuildColors(_themeIndex, _colorblindEnabled);
        ApplyUiScale(settings.UiScalePercent);
        GD.Print($"[ThemeService] Applied settings | theme={_themeIndex}, colorblind={_colorblindEnabled}, uiScale={settings.UiScalePercent}%");
        EmitSignal(SignalName.ThemeChanged, _themeIndex);
    }

    public void SetTheme(int index)
    {
        _themeIndex = index;
        CurrentColors = BuildColors(_themeIndex, _colorblindEnabled);
        EmitSignal(SignalName.ThemeChanged, _themeIndex);
    }

    public void SetColorblindPalette(bool enabled)
    {
        _colorblindEnabled = enabled;
        CurrentColors = BuildColors(_themeIndex, _colorblindEnabled);
        EmitSignal(SignalName.ThemeChanged, _themeIndex);
    }

    /// <summary>
    /// Base viewport dimensions for reference (design resolution)
    /// </summary>
    public const int BaseViewportWidth = 1280;
    public const int BaseViewportHeight = 820;

    /// <summary>
    /// Calculates the recommended UI scale bounds based on screen size.
    /// Returns (minScale, maxScale, recommendedScale) as percentages.
    /// </summary>
    public (int Min, int Max, int Recommended) GetUiScaleBounds()
    {
        var screenSize = DisplayServer.ScreenGetSize();
        var windowSize = GetTree().Root.Size;

        // Use the actual window size for calculations
        int width = (int)windowSize.X;
        int height = (int)windowSize.Y;

        // Calculate scale factors based on how much screen space we have
        float widthRatio = width / (float)BaseViewportWidth;
        float heightRatio = height / (float)BaseViewportHeight;

        // The limiting factor is the smaller ratio
        float limitingRatio = Math.Min(widthRatio, heightRatio);

        // Fixed bounds: min 50%, max 100%
        int minScale = 50;
        int maxScale = 100;

        // Recommended scale based on screen size
        // For smaller screens: 75-100%
        // For larger screens: 100%
        int recommended = limitingRatio >= 1.0f ? 100 : 75;

        return (minScale, maxScale, recommended);
    }

    /// <summary>
    /// Applies UI scaling with automatic bounds validation.
    /// </summary>
    public void ApplyUiScale(int uiScalePercent)
    {
        var bounds = GetUiScaleBounds();

        // Clamp to valid bounds
        int clampedScale = Math.Clamp(uiScalePercent, bounds.Min, bounds.Max);

        float scale = clampedScale / 100f;
        var root = GetTree().Root;
        root.ContentScaleFactor = scale;

        GD.Print($"[ThemeService] UI Scale applied: {clampedScale}% (requested: {uiScalePercent}%, bounds: {bounds.Min}%-{bounds.Max}%)");
    }

    /// <summary>
    /// Gets the current content scale factor.
    /// </summary>
    public float GetCurrentScale()
    {
        return GetTree().Root.ContentScaleFactor;
    }

    private ThemeColors BuildColors(int themeIndex, bool colorblind)
    {
        var baseColors = themeIndex == 0 ? _lightTheme : _darkTheme;
        if (!colorblind) return baseColors;

        // Slightly adjust accent/highlight away from red/green confusion.
        // Keep everything else identical to the current theme.
        baseColors.Accent = themeIndex == 0 ? new Color("0d47a1") : new Color("90caf9");
        baseColors.CellBackgroundHighlighted = themeIndex == 0 ? new Color("ffe082") : new Color("ffb300");
        return baseColors;
    }

    public void ApplyToControl(Control control)
    {
        // Basis-Styling für einen Control
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = CurrentColors.PanelBackground;
        styleBox.CornerRadiusTopLeft = 8;
        styleBox.CornerRadiusTopRight = 8;
        styleBox.CornerRadiusBottomLeft = 8;
        styleBox.CornerRadiusBottomRight = 8;
        styleBox.ContentMarginLeft = 16;
        styleBox.ContentMarginRight = 16;
        styleBox.ContentMarginTop = 16;
        styleBox.ContentMarginBottom = 16;

        control.AddThemeStyleboxOverride("panel", styleBox);
    }

    /// <summary>
    /// Erstellt einen StyleBox für Buttons
    /// </summary>
    public StyleBoxFlat CreateButtonStyleBox(bool hover = false, bool pressed = false, bool disabled = false)
    {
        var style = new StyleBoxFlat();

        if (disabled)
            style.BgColor = CurrentColors.ButtonDisabled;
        else if (pressed)
            style.BgColor = CurrentColors.ButtonPressed;
        else if (hover)
            style.BgColor = CurrentColors.ButtonHover;
        else
            style.BgColor = CurrentColors.ButtonNormal;

        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;

        return style;
    }

    /// <summary>
    /// Erstellt einen StyleBox für Panels
    /// </summary>
    public StyleBoxFlat CreatePanelStyleBox(int cornerRadius = 8, int margin = 16)
    {
        var style = new StyleBoxFlat();
        style.BgColor = CurrentColors.PanelBackground;
        style.CornerRadiusTopLeft = cornerRadius;
        style.CornerRadiusTopRight = cornerRadius;
        style.CornerRadiusBottomLeft = cornerRadius;
        style.CornerRadiusBottomRight = cornerRadius;
        style.ContentMarginLeft = margin;
        style.ContentMarginRight = margin;
        style.ContentMarginTop = margin;
        style.ContentMarginBottom = margin;

        return style;
    }

    /// <summary>
    /// Erstellt einen StyleBox für Sudoku-Zellen
    /// </summary>
    public StyleBoxFlat CreateCellStyleBox(bool isGiven = false, bool isSelected = false,
        bool isHighlighted = false, bool isRelated = false, bool isError = false,
        int row = 0, int col = 0)
    {
        var style = new StyleBoxFlat();

        // Hintergrundfarbe bestimmen - Related-Zellen ändern NICHT den Hintergrund
        if (isError)
            style.BgColor = CurrentColors.CellBackgroundError;
        else if (isSelected)
            style.BgColor = CurrentColors.CellBackgroundSelected;
        else if (isHighlighted)
            style.BgColor = CurrentColors.CellBackgroundHighlighted;
        else if (isGiven)
            style.BgColor = CurrentColors.CellBackgroundGiven;
        else
            style.BgColor = CurrentColors.CellBackground;

        style.CornerRadiusTopLeft = 2;
        style.CornerRadiusTopRight = 2;
        style.CornerRadiusBottomLeft = 2;
        style.CornerRadiusBottomRight = 2;

        return style;
    }
}
