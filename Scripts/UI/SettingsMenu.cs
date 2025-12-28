using Godot;
using System;
using MySudoku.Services;

namespace MySudoku.UI;

/// <summary>
/// Einstellungsmenü
/// </summary>
public partial class SettingsMenu : Control
{
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private OptionButton _themeOption = null!;
    private CheckButton _deadlyCheck = null!;
    private CheckButton _hideCheck = null!;
    private CheckButton _highlightCheck = null!;
    private Button _backButton = null!;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _title = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBoxContainer/Title");

        var settingsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/SettingsContainer");
        _themeOption = settingsContainer.GetNode<OptionButton>("ThemeRow/ThemeOption");
        _deadlyCheck = settingsContainer.GetNode<CheckButton>("DeadlyRow/DeadlyCheck");
        _hideCheck = settingsContainer.GetNode<CheckButton>("HideRow/HideCheck");
        _highlightCheck = settingsContainer.GetNode<CheckButton>("HighlightRow/HighlightCheck");
        _backButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBoxContainer/BackButton");

        // Theme-Optionen
        _themeOption.AddItem("Hell", 0);
        _themeOption.AddItem("Dunkel", 1);

        // Werte laden
        LoadSettings();

        // Events
        _themeOption.ItemSelected += OnThemeSelected;
        _deadlyCheck.Toggled += OnDeadlyToggled;
        _hideCheck.Toggled += OnHideToggled;
        _highlightCheck.Toggled += OnHighlightToggled;
        _backButton.Pressed += OnBackPressed;

        ApplyTheme();
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;
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

    private void LoadSettings()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        var settings = saveService.Settings;

        _themeOption.Selected = settings.ThemeIndex;
        _deadlyCheck.ButtonPressed = settings.DeadlyModeEnabled;
        _hideCheck.ButtonPressed = settings.HideCompletedNumbers;
        _highlightCheck.ButtonPressed = settings.HighlightRelatedCells;
    }

    private void SaveSettings()
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.SaveSettings();
    }

    private void OnThemeSelected(long index)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.ThemeIndex = (int)index;
        SaveSettings();

        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.SetTheme((int)index);
    }

    private void OnDeadlyToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.DeadlyModeEnabled = pressed;
        SaveSettings();
    }

    private void OnHideToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.HideCompletedNumbers = pressed;
        SaveSettings();
    }

    private void OnHighlightToggled(bool pressed)
    {
        var saveService = GetNode<SaveService>("/root/SaveService");
        saveService.Settings.HighlightRelatedCells = pressed;
        SaveSettings();
    }

    private void OnBackPressed()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.GoToMainMenu();
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

        // Labels
        foreach (var child in GetTree().GetNodesInGroup(""))
        {
            // Alle Labels im Settings-Container
        }

        var settingsContainer = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer/SettingsContainer");
        foreach (var row in settingsContainer.GetChildren())
        {
            if (row is HBoxContainer hbox)
            {
                foreach (var item in hbox.GetChildren())
                {
                    if (item is Label label)
                    {
                        label.AddThemeColorOverride("font_color", colors.TextPrimary);
                    }
                }
            }
        }

        // Back Button
        _backButton.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        _backButton.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        _backButton.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        _backButton.AddThemeColorOverride("font_color", colors.TextPrimary);
    }
}
