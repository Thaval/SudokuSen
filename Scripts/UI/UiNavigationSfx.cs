namespace SudokuSen.UI;

using Godot;
using SudokuSen.Services;

/// <summary>
/// Wires consistent UI navigation SFX (keyboard/controller focus movement).
/// Many menus only play SFX on Pressed; focus navigation needs explicit hooks.
/// </summary>
public static class UiNavigationSfx
{
    /// <summary>
    /// Plays a click when focus enters any focusable interactive control under <paramref name="root"/>.
    /// Deferred to skip initial focus churn on scene load.
    /// Only fires on keyboard/controller navigation, not on mouse clicks (those already have Pressed handlers).
    /// </summary>
    public static void Wire(Control root, AudioService audioService)
    {
        if (root == null || audioService == null) return;

        // Store the tree reference immediately - it might not be available later.
        var tree = root.GetTree();
        if (tree == null) return;

        // Defer wiring until after the first frame so initial auto-focus doesn't trigger clicks.
        tree.ProcessFrame += WireOnce;

        void WireOnce()
        {
            // Guard against disposed objects during testing or rapid scene changes.
            // Check both tree and root validity BEFORE any property access.
            if (!GodotObject.IsInstanceValid(tree)) return;

            tree.ProcessFrame -= WireOnce;

            if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree()) return;
            WireInternal(root, audioService);
        }
    }

    private static void WireInternal(Control root, AudioService audioService)
    {
        foreach (var node in EnumerateDescendants(root))
        {
            if (node is not Control control) continue;

            // Only focusable controls can be navigated to.
            if (control.FocusMode == Control.FocusModeEnum.None) continue;

            // Limit to controls users actually navigate between.
            if (!IsInteractive(control)) continue;

            control.FocusEntered += () => OnFocusEntered(control, audioService);
        }
    }

    private static void OnFocusEntered(Control control, AudioService audioService)
    {
        // Skip if mouse button is pressed — the Pressed handler will play the click.
        // This avoids double-clicks when clicking a button (focus + press both fire).
        if (Input.IsMouseButtonPressed(MouseButton.Left) ||
            Input.IsMouseButtonPressed(MouseButton.Right))
        {
            return;
        }

        audioService.PlayClick();
    }

    private static bool IsInteractive(Control control)
    {
        return control is BaseButton
            || control is OptionButton
            || control is LineEdit
            || control is TextEdit
            || control is Range; // sliders
    }

    private static System.Collections.Generic.IEnumerable<Node> EnumerateDescendants(Node root)
    {
        var stack = new System.Collections.Generic.Stack<Node>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var child in current.GetChildren())
            {
                if (child is Node n)
                {
                    yield return n;
                    stack.Push(n);
                }
            }
        }
    }
}
