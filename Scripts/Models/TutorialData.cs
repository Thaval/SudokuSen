namespace SudokuSen.Models;

/// <summary>
/// Represents a single tutorial with its metadata and steps.
/// </summary>
public class TutorialData
{
    /// <summary>Unique identifier for the tutorial</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name shown in the menu</summary>
    public string Name { get; set; } = "";

    /// <summary>Short description</summary>
    public string Description { get; set; } = "";

    /// <summary>Difficulty category: Easy, Medium, Hard</summary>
    public TutorialDifficulty Difficulty { get; set; } = TutorialDifficulty.Easy;

    /// <summary>Estimated duration in minutes</summary>
    public int EstimatedMinutes { get; set; } = 3;

    /// <summary>Pre-defined puzzle state (serialized grid)</summary>
    public string PuzzleData { get; set; } = "";

    /// <summary>The ordered list of tutorial steps</summary>
    public List<TutorialStep> Steps { get; set; } = new();
}

public enum TutorialDifficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Base class for tutorial steps. Each step represents one interaction/animation.
/// </summary>
public abstract class TutorialStep
{
    /// <summary>Step type identifier for serialization</summary>
    public abstract string StepType { get; }

    /// <summary>Optional delay before this step starts (ms)</summary>
    public int DelayMs { get; set; } = 0;

    /// <summary>Duration for animations (ms)</summary>
    public int DurationMs { get; set; } = 500;

    /// <summary>Whether to wait for user interaction before continuing</summary>
    public bool WaitForClick { get; set; } = true;
}

/// <summary>
/// Shows a speech bubble / tooltip with text, optionally pointing to a UI element.
/// </summary>
public class ShowMessageStep : TutorialStep
{
    public override string StepType => "ShowMessage";

    /// <summary>The message text to display</summary>
    public string Message { get; set; } = "";

    /// <summary>Optional title for the message box</summary>
    public string? Title { get; set; }

    /// <summary>Position of the message bubble</summary>
    public MessagePosition Position { get; set; } = MessagePosition.Center;

    /// <summary>Target element to point to (null = no arrow)</summary>
    public TutorialTarget? PointTo { get; set; }

    /// <summary>Multiple targets to point to with arrows (for showing multiple elements at once)</summary>
    public List<TutorialTarget>? PointToMultiple { get; set; }

    /// <summary>Optional cells to highlight while message is shown</summary>
    public List<(int Row, int Col)>? HighlightCells { get; set; }

    /// <summary>Style for highlighted cells</summary>
    public HighlightStyle HighlightStyle { get; set; } = HighlightStyle.Pulse;

    /// <summary>Optional: wait for a specific action instead of button click</summary>
    public ExpectedAction? WaitForAction { get; set; }

    /// <summary>Expected cell for the action (if WaitForAction is set)</summary>
    public (int Row, int Col)? ExpectedCell { get; set; }

    /// <summary>Expected cells for multi-select action (if WaitForAction is SelectMultipleCells)</summary>
    public List<(int Row, int Col)>? ExpectedCells { get; set; }

    /// <summary>Expected number for the action (if WaitForAction is set)</summary>
    public int? ExpectedNumber { get; set; }
}

/// <summary>
/// Highlights one or more cells on the grid.
/// </summary>
public class HighlightCellsStep : TutorialStep
{
    public override string StepType => "HighlightCells";

    /// <summary>List of cell positions to highlight (row, col)</summary>
    public List<(int Row, int Col)> Cells { get; set; } = new();

    /// <summary>Highlight style</summary>
    public HighlightStyle Style { get; set; } = HighlightStyle.Pulse;

    /// <summary>Optional color override</summary>
    public string? Color { get; set; }
}

/// <summary>
/// Highlights an entire row, column, or block.
/// </summary>
public class HighlightHouseStep : TutorialStep
{
    public override string StepType => "HighlightHouse";

    /// <summary>Type of house to highlight</summary>
    public HouseType HouseType { get; set; }

    /// <summary>Index of the house (0-8 for rows/cols, block index for blocks)</summary>
    public int Index { get; set; }

    /// <summary>Highlight style</summary>
    public HighlightStyle Style { get; set; } = HighlightStyle.Glow;
}

/// <summary>
/// Points an arrow to a specific UI element.
/// </summary>
public class PointToElementStep : TutorialStep
{
    public override string StepType => "PointToElement";

    /// <summary>Target to point to</summary>
    public TutorialTarget Target { get; set; } = new();

    /// <summary>Optional message to show alongside the arrow</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Simulates user input (clicking a cell, entering a number).
/// </summary>
public class SimulateInputStep : TutorialStep
{
    public override string StepType => "SimulateInput";

    /// <summary>Type of input to simulate</summary>
    public SimulatedInputType InputType { get; set; }

    /// <summary>Target cell (for cell selection or number entry)</summary>
    public (int Row, int Col)? Cell { get; set; }

    /// <summary>Number to enter (1-9, or 0 for erase)</summary>
    public int? Number { get; set; }

    /// <summary>Whether to enter as a note</summary>
    public bool AsNote { get; set; } = false;
}

/// <summary>
/// Waits for user to perform a specific action.
/// </summary>
public class WaitForActionStep : TutorialStep
{
    public override string StepType => "WaitForAction";

    /// <summary>The action to wait for</summary>
    public ExpectedAction Action { get; set; }

    /// <summary>Expected cell (if action involves a cell)</summary>
    public (int Row, int Col)? ExpectedCell { get; set; }

    /// <summary>Expected number (if action involves entering a number)</summary>
    public int? ExpectedNumber { get; set; }

    /// <summary>Message to show while waiting</summary>
    public string? HintMessage { get; set; }

    /// <summary>Message to show if user does wrong action</summary>
    public string? WrongActionMessage { get; set; }
}

/// <summary>
/// Shows a pause/transition screen.
/// </summary>
public class PauseStep : TutorialStep
{
    public override string StepType => "Pause";

    /// <summary>How long to pause (ms)</summary>
    public int PauseDurationMs { get; set; } = 1000;
}

/// <summary>
/// Clears all current highlights.
/// </summary>
public class ClearHighlightsStep : TutorialStep
{
    public override string StepType => "ClearHighlights";

    public ClearHighlightsStep()
    {
        WaitForClick = false;
        DurationMs = 200;
    }
}

// Supporting enums and classes

public enum MessagePosition
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public enum HighlightStyle
{
    Pulse,      // Pulsing border
    Glow,       // Glowing background
    Arrow,      // Arrow pointing to it
    Outline,    // Thick outline
    Fill        // Filled background
}

public enum HouseType
{
    Row,
    Column,
    Block
}

public enum SimulatedInputType
{
    SelectCell,
    EnterNumber,
    ToggleNote,
    ClickButton,
    PressKey
}

public enum ExpectedAction
{
    SelectCell,
    SelectMultipleCells,
    EnterCorrectNumber,
    EnterAnyNumber,
    EnterWrongNumber,
    AddNote,
    RemoveNote,
    ToggleNote,
    ToggleNoteMultiSelect,
    ToggleNotesMode,
    EraseCell,
    ClickButton,
    PressKey
}

/// <summary>
/// Identifies a target UI element for pointing/highlighting.
/// </summary>
public class TutorialTarget
{
    /// <summary>Type of target element</summary>
    public TargetType Type { get; set; }

    /// <summary>For cells: the cell position</summary>
    public (int Row, int Col)? CellPosition { get; set; }

    /// <summary>For buttons: the button identifier</summary>
    public string? ButtonId { get; set; }

    /// <summary>For UI elements: the node path</summary>
    public string? NodePath { get; set; }
}

public enum TargetType
{
    Cell,
    NumberPadButton,
    BackButton,
    Timer,
    MistakesLabel,
    DifficultyLabel,
    NotesToggle,
    HintButton,
    UndoButton,
    RedoButton,
    AutoNotesButton,
    EraseButton,
    HouseAutoFillButton, // R/C/B button for filling notes in row/column/block
    Grid,
    GridEdge,      // Points to the outer edge of the grid (top-left corner)
    AxisLabels,    // Points to the axis labels (A-I, 1-9)
    ColumnLabel,   // Points to a specific column label (A-I)
    RowLabel,      // Points to a specific row label (1-9)
    Custom
}
