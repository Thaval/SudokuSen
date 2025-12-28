using Godot;
using System;

namespace MySudoku.Services;

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
        Accent = new Color("64b5f6")
    };

    public override void _Ready()
    {
        Instance = this;
        // Theme aus Settings laden
        var saveService = GetNode<SaveService>("/root/SaveService");
        CurrentColors = saveService.Settings.ThemeIndex == 0 ? _lightTheme : _darkTheme;
    }

    public void SetTheme(int index)
    {
        CurrentColors = index == 0 ? _lightTheme : _darkTheme;
        EmitSignal(SignalName.ThemeChanged, index);
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
