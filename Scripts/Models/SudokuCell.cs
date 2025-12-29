using System.Text.Json.Serialization;

namespace MySudoku.Models;

/// <summary>
/// Repräsentiert eine einzelne Zelle im Sudoku-Grid
/// </summary>
public class SudokuCell
{
    /// <summary>Aktueller Wert (0 = leer, 1-9 = Zahl)</summary>
    public int Value { get; set; }

    /// <summary>True wenn die Zelle eine vorgegebene Zahl ist (nicht editierbar)</summary>
    public bool IsGiven { get; set; }

    /// <summary>Die korrekte Lösung für diese Zelle</summary>
    public int Solution { get; set; }

    /// <summary>Notizen für diese Zelle (Kandidaten)</summary>
    public bool[] Notes { get; set; } = new bool[9];

    [JsonIgnore]
    public bool IsCorrect => Value == Solution;

    [JsonIgnore]
    public bool IsEmpty => Value == 0;

    [JsonIgnore]
    public bool IsEditable => !IsGiven;

    public SudokuCell()
    {
    }

    public SudokuCell(int value, bool isGiven, int solution)
    {
        Value = value;
        IsGiven = isGiven;
        Solution = solution;
    }

    public SudokuCell Clone()
    {
        var clone = new SudokuCell
        {
            Value = Value,
            IsGiven = IsGiven,
            Solution = Solution,
            Notes = (bool[])Notes.Clone()
        };
        return clone;
    }
}
