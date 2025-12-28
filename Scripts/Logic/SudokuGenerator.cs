using System;
using System.Collections.Generic;
using MySudoku.Models;

namespace MySudoku.Logic;

/// <summary>
/// Generiert gültige Sudoku-Rätsel mit eindeutiger Lösung
/// </summary>
public static class SudokuGenerator
{
    private static Random _random = new Random();

    /// <summary>
    /// Generiert ein neues Sudoku-Spiel
    /// </summary>
    public static SudokuGameState Generate(Difficulty difficulty)
    {
        var state = new SudokuGameState
        {
            Difficulty = difficulty,
            StartTime = DateTime.Now,
            Status = GameStatus.InProgress
        };

        // Generiere vollständiges Grid
        int[,] fullGrid = GenerateFullGrid();

        // Speichere Lösung
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                state.Grid[row, col].Solution = fullGrid[row, col];
            }
        }

        // Entferne Zahlen basierend auf Schwierigkeit
        int cellsToRemove = GetCellsToRemove(difficulty);
        int[,] puzzleGrid = RemoveCells(fullGrid, cellsToRemove);

        // Setze das Puzzle
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int value = puzzleGrid[row, col];
                state.Grid[row, col].Value = value;
                state.Grid[row, col].IsGiven = value != 0;
            }
        }

        return state;
    }

    /// <summary>
    /// Generiert ein vollständig gelöstes Sudoku-Grid
    /// </summary>
    private static int[,] GenerateFullGrid()
    {
        int[,] grid = new int[9, 9];
        FillGrid(grid);
        return grid;
    }

    private static bool FillGrid(int[,] grid)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (grid[row, col] == 0)
                {
                    // Erstelle eine zufällige Reihenfolge von 1-9
                    List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    ShuffleList(numbers);

                    foreach (int num in numbers)
                    {
                        if (SudokuSolver.IsValidMove(grid, row, col, num))
                        {
                            grid[row, col] = num;

                            if (FillGrid(grid))
                                return true;

                            grid[row, col] = 0;
                        }
                    }
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Entfernt Zahlen aus dem Grid und stellt sicher, dass eine eindeutige Lösung existiert
    /// </summary>
    private static int[,] RemoveCells(int[,] fullGrid, int cellsToRemove)
    {
        int[,] puzzle = SudokuSolver.CopyGrid(fullGrid);

        // Erstelle Liste aller Positionen
        List<(int row, int col)> positions = new List<(int, int)>();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                positions.Add((row, col));
            }
        }
        ShuffleList(positions);

        int removed = 0;
        foreach (var (row, col) in positions)
        {
            if (removed >= cellsToRemove)
                break;

            int backup = puzzle[row, col];
            puzzle[row, col] = 0;

            // Prüfe ob noch eindeutige Lösung existiert
            int[,] testGrid = SudokuSolver.CopyGrid(puzzle);
            int solutions = SudokuSolver.CountSolutions(testGrid, 2);

            if (solutions != 1)
            {
                // Wiederherstellen wenn nicht eindeutig lösbar
                puzzle[row, col] = backup;
            }
            else
            {
                removed++;
            }
        }

        return puzzle;
    }

    /// <summary>
    /// Bestimmt wie viele Zellen basierend auf Schwierigkeit entfernt werden
    /// </summary>
    private static int GetCellsToRemove(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => 35,    // ~46 Givens
            Difficulty.Medium => 45,  // ~36 Givens
            Difficulty.Hard => 55,    // ~26 Givens
            _ => 40
        };
    }

    private static void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
