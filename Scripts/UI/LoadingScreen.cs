using Godot;
using System.Collections.Generic;

namespace SudokuSen.UI;

/// <summary>
/// Simple loading screen with a 3x3 blinking cell animation (row by row).
/// Replaces the Godot face splash by showing immediately at startup and
/// swaps to the real main scene once loaded.
/// </summary>
public partial class LoadingScreen : Control
{
    private readonly List<ColorRect> _cells = new();
    private readonly Color _offColor = new Color(0.18f, 0.2f, 0.24f, 0.55f);
    private readonly Color _onColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    private readonly Color _bgColor = new Color(0.07f, 0.08f, 0.1f, 1f);

    private int _activeIndex = 0;
    private double _stepTimer = 0;
    private const double StepDuration = 0.12; // seconds per cell blink

    public override void _Ready()
    {
        BuildLayout();
        UpdateCells();
        StartLoadAsync();
    }

    public override void _Process(double delta)
    {
        _stepTimer += delta;
        if (_stepTimer >= StepDuration)
        {
            _stepTimer = 0;
            _activeIndex = (_activeIndex + 1) % _cells.Count;
            UpdateCells();
        }
    }

    private void BuildLayout()
    {
        // Fullscreen background
        var bg = new ColorRect
        {
            Color = _bgColor,
            AnchorRight = 1,
            AnchorBottom = 1
        };
        AddChild(bg);

        // Centered grid container for 3x3 cells
        var center = new CenterContainer
        {
            AnchorRight = 1,
            AnchorBottom = 1
        };
        AddChild(center);

        var grid = new GridContainer
        {
            Columns = 3,
            CustomMinimumSize = new Vector2(180, 180)
        };
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 10);
        center.AddChild(grid);

        for (int i = 0; i < 9; i++)
        {
            var cell = new ColorRect
            {
                Color = _offColor,
                CustomMinimumSize = new Vector2(50, 50)
            };
            _cells.Add(cell);
            grid.AddChild(cell);
        }
    }

    private void UpdateCells()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].Color = i == _activeIndex ? _onColor : _offColor;
        }
    }

    private async void StartLoadAsync()
    {
        // Ensure the animation is visible for at least a couple of frames
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);

        var mainPacked = GD.Load<PackedScene>("res://Scenes/Main.tscn");
        if (mainPacked == null)
        {
            GD.PrintErr("[Loading] Failed to load Main.tscn");
            return;
        }

        var main = mainPacked.Instantiate();
        var root = GetTree().Root;
        root.AddChild(main);
        GetTree().CurrentScene = main;

        QueueFree();
    }
}
