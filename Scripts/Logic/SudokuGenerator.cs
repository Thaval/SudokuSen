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

        int size = state.GridSize;
        int blockSize = state.BlockSize;

        // Generiere vollständiges Grid
        int[,] fullGrid = GenerateFullGrid(size, blockSize);

        // Speichere Lösung
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                state.Grid[row, col].Solution = fullGrid[row, col];
            }
        }

        // Entferne Zahlen basierend auf Schwierigkeit
        int cellsToRemove = GetCellsToRemove(difficulty);
        int[,] puzzleGrid = RemoveCells(fullGrid, cellsToRemove, size, blockSize);

        // Setze das Puzzle
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
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
    private static int[,] GenerateFullGrid(int size = 9, int blockSize = 3)
    {
        int[,] grid = new int[size, size];
        FillGrid(grid, size, blockSize);
        return grid;
    }

    private static bool FillGrid(int[,] grid, int size = 9, int blockSize = 3)
    {
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (grid[row, col] == 0)
                {
                    // Erstelle eine zufällige Reihenfolge von 1-size
                    List<int> numbers = new List<int>();
                    for (int i = 1; i <= size; i++)
                        numbers.Add(i);
                    ShuffleList(numbers);

                    foreach (int num in numbers)
                    {
                        if (SudokuSolver.IsValidMove(grid, row, col, num, size, blockSize))
                        {
                            grid[row, col] = num;

                            if (FillGrid(grid, size, blockSize))
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
    private static int[,] RemoveCells(int[,] fullGrid, int cellsToRemove, int size = 9, int blockSize = 3)
    {
        int[,] puzzle = SudokuSolver.CopyGrid(fullGrid, size);

        // Erstelle Liste aller Positionen
        List<(int row, int col)> positions = new List<(int, int)>();
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
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
            int[,] testGrid = SudokuSolver.CopyGrid(puzzle, size);
            int solutions = SudokuSolver.CountSolutions(testGrid, 2, size, blockSize);

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
            Difficulty.Kids => 8,     // ~8 Givens in 4x4 (16 cells - 8 = 8 givens)
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
