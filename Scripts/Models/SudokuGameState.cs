using Godot;
using System;
using System.Text.Json.Serialization;

namespace MySudoku.Models;

/// <summary>
/// Schwierigkeitsgrade
/// </summary>
public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Status eines Spiels
/// </summary>
public enum GameStatus
{
    InProgress,
    Won,
    Lost,
    Abandoned
}

/// <summary>
/// Repräsentiert den aktuellen Spielzustand
/// </summary>
public class SudokuGameState
{
    /// <summary>9x9 Grid</summary>
    public SudokuCell[,] Grid { get; set; } = new SudokuCell[9, 9];

    /// <summary>Schwierigkeitsgrad</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Startzeitpunkt</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Verstrichene Zeit in Sekunden</summary>
    public double ElapsedSeconds { get; set; }

    /// <summary>Anzahl der Fehler</summary>
    public int Mistakes { get; set; }

    /// <summary>Deadly Modus aktiv</summary>
    public bool IsDeadlyMode { get; set; }

    /// <summary>Aktueller Status</summary>
    public GameStatus Status { get; set; }

    public SudokuGameState()
    {
        InitializeEmptyGrid();
    }

    private void InitializeEmptyGrid()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                Grid[row, col] = new SudokuCell();
            }
        }
    }

    /// <summary>
    /// Zählt wie oft eine Zahl im Grid vorkommt
    /// </summary>
    public int CountNumber(int number)
    {
        int count = 0;
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (Grid[row, col].Value == number)
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Prüft ob das Grid vollständig und korrekt ist
    /// </summary>
    public bool IsComplete()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (Grid[row, col].IsEmpty || !Grid[row, col].IsCorrect)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Prüft ob eine Zahl an einer Position gültig ist (Sudoku-Regeln)
    /// </summary>
    public bool IsValidPlacement(int row, int col, int number)
    {
        // Prüfe Zeile
        for (int c = 0; c < 9; c++)
        {
            if (c != col && Grid[row, c].Value == number)
                return false;
        }

        // Prüfe Spalte
        for (int r = 0; r < 9; r++)
        {
            if (r != row && Grid[r, col].Value == number)
                return false;
        }

        // Prüfe 3x3 Block
        int blockRow = (row / 3) * 3;
        int blockCol = (col / 3) * 3;
        for (int r = blockRow; r < blockRow + 3; r++)
        {
            for (int c = blockCol; c < blockCol + 3; c++)
            {
                if ((r != row || c != col) && Grid[r, c].Value == number)
                    return false;
            }
        }

        return true;
    }

    public SudokuGameState Clone()
    {
        var clone = new SudokuGameState
        {
            Difficulty = Difficulty,
            StartTime = StartTime,
            ElapsedSeconds = ElapsedSeconds,
            Mistakes = Mistakes,
            IsDeadlyMode = IsDeadlyMode,
            Status = Status
        };

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                clone.Grid[row, col] = Grid[row, col].Clone();
            }
        }

        return clone;
    }
}
