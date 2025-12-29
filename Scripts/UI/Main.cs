namespace MySudoku.UI;

/// <summary>
/// Haupt-Szene mit Scene-Switching
/// </summary>
public partial class Main : Control
{
    private Control _sceneContainer = null!;
    private ColorRect _background = null!;
    private Node? _currentScene;

    public override void _Ready()
    {
        _sceneContainer = GetNode<Control>("SceneContainer");
        _background = GetNode<ColorRect>("Background");

        // Verbinde mit AppState für Szenenwechsel
        var appState = GetNode<AppState>("/root/AppState");
        appState.SceneChangeRequested += OnSceneChangeRequested;

        // Verbinde mit ThemeService
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;

        // Initiales Theme anwenden
        ApplyTheme();

        // Lade Startszene (MainMenu)
        LoadScene(AppState.SCENE_MAIN_MENU);
    }

    public override void _ExitTree()
    {
        var appState = GetNode<AppState>("/root/AppState");
        appState.SceneChangeRequested -= OnSceneChangeRequested;

        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged -= OnThemeChanged;
    }

    private void OnSceneChangeRequested(string scenePath)
    {
        LoadScene(scenePath);
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        _background.Color = themeService.CurrentColors.Background;
    }

    private void LoadScene(string scenePath)
    {
        // Entferne aktuelle Szene
        if (_currentScene != null)
        {
            _sceneContainer.RemoveChild(_currentScene);
            _currentScene.QueueFree();
        }

        // Lade neue Szene
        var scene = GD.Load<PackedScene>(scenePath);
        if (scene != null)
        {
            _currentScene = scene.Instantiate();
            _sceneContainer.AddChild(_currentScene);
        }
        else
        {
            GD.PrintErr($"Konnte Szene nicht laden: {scenePath}");
        }
    }
}
