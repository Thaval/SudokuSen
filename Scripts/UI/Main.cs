namespace SudokuSen.UI;

/// <summary>
/// Haupt-Szene mit Scene-Switching
/// </summary>
public partial class Main : Control
{
    // Cached Service References
    private ThemeService _themeService = null!;
    private AppState _appState = null!;

    private Control _sceneContainer = null!;
    private ColorRect _background = null!;
    private Node? _currentScene;

    public override void _Ready()
    {
        // Cache service references
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _appState = GetNode<AppState>("/root/AppState");

        _sceneContainer = GetNode<Control>("SceneContainer");
        _background = GetNode<ColorRect>("Background");

        // Verbinde mit AppState für Szenenwechsel
        _appState.SceneChangeRequested += OnSceneChangeRequested;

        // Verbinde mit ThemeService
        _themeService.ThemeChanged += OnThemeChanged;

        // Initiales Theme anwenden
        ApplyTheme();

        // Lade Startszene (MainMenu)
        LoadScene(AppState.SCENE_MAIN_MENU);
    }

    public override void _ExitTree()
    {
        _appState.SceneChangeRequested -= OnSceneChangeRequested;
        _themeService.ThemeChanged -= OnThemeChanged;
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
        _background.Color = _themeService.CurrentColors.Background;
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
