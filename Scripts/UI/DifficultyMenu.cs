using Godot;
using System;
using MySudoku.Models;
using MySudoku.Services;
using MySudoku.Logic;

namespace MySudoku.UI;

/// <summary>
/// Schwierigkeitsauswahl-Menü
/// </summary>
public partial class DifficultyMenu : Control
{
    private Button _easyButton = null!;
    private Button _mediumButton = null!;
    private Button _hardButton = null!;
    private Button _backButton = null!;
    private Label _easyTechniques = null!;
    private Label _mediumTechniques = null!;
    private Label _hardTechniques = null!;
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private Label _description = null!;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Title");
        _description = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Description");

        var buttonContainer = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/ButtonContainer");
        _easyButton = buttonContainer.GetNode<Button>("EasyContainer/EasyButton");
        _mediumButton = buttonContainer.GetNode<Button>("MediumContainer/MediumButton");
        _hardButton = buttonContainer.GetNode<Button>("HardContainer/HardButton");
        _easyTechniques = buttonContainer.GetNode<Label>("EasyContainer/EasyTechniques");
        _mediumTechniques = buttonContainer.GetNode<Label>("MediumContainer/MediumTechniques");
        _hardTechniques = buttonContainer.GetNode<Label>("HardContainer/HardTechniques");
        _backButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBoxContainer/BackButton");

        _easyButton.Pressed += () => OnDifficultySelected(Difficulty.Easy);
        _mediumButton.Pressed += () => OnDifficultySelected(Difficulty.Medium);
        _hardButton.Pressed += () => OnDifficultySelected(Difficulty.Hard);
        _backButton.Pressed += OnBackPressed;

        // Technik-Beschreibungen unter den Buttons
        _easyTechniques.Text = TechniqueInfo.GetShortTechniqueList(Difficulty.Easy);
        _mediumTechniques.Text = TechniqueInfo.GetShortTechniqueList(Difficulty.Medium);
        _hardTechniques.Text = TechniqueInfo.GetShortTechniqueList(Difficulty.Hard);

        ApplyTheme();
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;

        _easyButton.GrabFocus();
    }

    public override void _ExitTree()
    {
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged -= OnThemeChanged;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            OnBackPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        var panelStyle = theme.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _description.AddThemeColorOverride("font_color", colors.TextSecondary);

        // Technik-Labels stylen
        _easyTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);
        _mediumTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);
        _hardTechniques.AddThemeColorOverride("font_color", colors.TextSecondary);

        ApplyButtonTheme(_easyButton, theme, new Color("4caf50")); // Grün
        ApplyButtonTheme(_mediumButton, theme, new Color("ff9800")); // Orange
        ApplyButtonTheme(_hardButton, theme, new Color("f44336")); // Rot
        ApplyButtonTheme(_backButton, theme);
    }

    private void ApplyButtonTheme(Button button, ThemeService theme, Color? accentColor = null)
    {
        var colors = theme.CurrentColors;

        if (accentColor.HasValue)
        {
            var normalStyle = new StyleBoxFlat();
            normalStyle.BgColor = accentColor.Value.Darkened(0.2f);
            normalStyle.CornerRadiusTopLeft = 6;
            normalStyle.CornerRadiusTopRight = 6;
            normalStyle.CornerRadiusBottomLeft = 6;
            normalStyle.CornerRadiusBottomRight = 6;
            normalStyle.ContentMarginLeft = 16;
            normalStyle.ContentMarginRight = 16;
            normalStyle.ContentMarginTop = 8;
            normalStyle.ContentMarginBottom = 8;

            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = accentColor.Value;
            hoverStyle.CornerRadiusTopLeft = 6;
            hoverStyle.CornerRadiusTopRight = 6;
            hoverStyle.CornerRadiusBottomLeft = 6;
            hoverStyle.CornerRadiusBottomRight = 6;
            hoverStyle.ContentMarginLeft = 16;
            hoverStyle.ContentMarginRight = 16;
            hoverStyle.ContentMarginTop = 8;
            hoverStyle.ContentMarginBottom = 8;

            button.AddThemeStyleboxOverride("normal", normalStyle);
            button.AddThemeStyleboxOverride("hover", hoverStyle);
            button.AddThemeStyleboxOverride("pressed", hoverStyle);
            button.AddThemeStyleboxOverride("focus", hoverStyle);
            button.AddThemeColorOverride("font_color", Colors.White);
            button.AddThemeColorOverride("font_hover_color", Colors.White);
            button.AddThemeColorOverride("font_pressed_color", Colors.White);
        }
        else
        {
            button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
            button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
            button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
            button.AddThemeStyleboxOverride("focus", theme.CreateButtonStyleBox(hover: true));
            button.AddThemeColorOverride("font_color", colors.TextPrimary);
            button.AddThemeColorOverride("font_hover_color", colors.TextPrimary);
            button.AddThemeColorOverride("font_pressed_color", colors.TextPrimary);
        }
    }

    private void OnDifficultySelected(Difficulty difficulty)
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.StartNewGame(difficulty);
    }

    private void OnBackPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.GoToMainMenu();
    }
}
