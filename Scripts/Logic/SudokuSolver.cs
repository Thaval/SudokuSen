using System;
using System.Collections.Generic;
using MySudoku.Models;

namespace MySudoku.Logic;

/// <summary>
/// Sudoku Solver mit Backtracking-Algorithmus
/// </summary>
public static class SudokuSolver
{
    /// <summary>
    /// Löst das Sudoku-Grid
    /// </summary>
    /// <returns>True wenn lösbar</returns>
    public static bool Solve(int[,] grid)
    {
        return SolveRecursive(grid);
    }

    private static bool SolveRecursive(int[,] grid)
    {
        // Finde die nächste leere Zelle
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (grid[row, col] == 0)
                {
                    // Versuche Zahlen 1-9
                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsValidMove(grid, row, col, num))
                        {
                            grid[row, col] = num;

                            if (SolveRecursive(grid))
                                return true;

                            grid[row, col] = 0; // Backtrack
                        }
                    }
                    return false; // Keine gültige Zahl gefunden
                }
            }
        }
        return true; // Alle Zellen gefüllt
    }

    /// <summary>
    /// Zählt die Anzahl der Lösungen (stoppt bei maxSolutions)
    /// </summary>
    public static int CountSolutions(int[,] grid, int maxSolutions = 2)
    {
        int count = 0;
        CountSolutionsRecursive(grid, ref count, maxSolutions);
        return count;
    }

    private static bool CountSolutionsRecursive(int[,] grid, ref int count, int maxSolutions)
    {
        // Finde die nächste leere Zelle
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (grid[row, col] == 0)
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        if (IsValidMove(grid, row, col, num))
                        {
                            grid[row, col] = num;

                            if (CountSolutionsRecursive(grid, ref count, maxSolutions))
                            {
                                grid[row, col] = 0;
                                return true; // Frühzeitig abbrechen
                            }

                            grid[row, col] = 0;
                        }
                    }
                    return false;
                }
            }
        }

        count++;
        return count >= maxSolutions; // True = stoppen
    }

    /// <summary>
    /// Prüft ob eine Zahl an einer Position gültig ist
    /// </summary>
    public static bool IsValidMove(int[,] grid, int row, int col, int num)
    {
        // Prüfe Zeile
        for (int c = 0; c < 9; c++)
        {
            if (grid[row, c] == num)
                return false;
        }

        // Prüfe Spalte
        for (int r = 0; r < 9; r++)
        {
            if (grid[r, col] == num)
                return false;
        }

        // Prüfe 3x3 Block
        int blockRow = (row / 3) * 3;
        int blockCol = (col / 3) * 3;
        for (int r = blockRow; r < blockRow + 3; r++)
        {
            for (int c = blockCol; c < blockCol + 3; c++)
            {
                if (grid[r, c] == num)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Kopiert ein Grid
    /// </summary>
    public static int[,] CopyGrid(int[,] grid)
    {
        int[,] copy = new int[9, 9];
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                copy[row, col] = grid[row, col];
            }
        }
        return copy;
    }
}
