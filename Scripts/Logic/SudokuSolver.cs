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
    public static bool Solve(int[,] grid, int size = 9, int blockSize = 3)
    {
        return SolveRecursive(grid, size, blockSize);
    }

    private static bool SolveRecursive(int[,] grid, int size = 9, int blockSize = 3)
    {
        // Finde die nächste leere Zelle
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (grid[row, col] == 0)
                {
                    // Versuche Zahlen 1-size
                    for (int num = 1; num <= size; num++)
                    {
                        if (IsValidMove(grid, row, col, num, size, blockSize))
                        {
                            grid[row, col] = num;

                            if (SolveRecursive(grid, size, blockSize))
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
    public static int CountSolutions(int[,] grid, int maxSolutions = 2, int size = 9, int blockSize = 3)
    {
        int count = 0;
        CountSolutionsRecursive(grid, ref count, maxSolutions, size, blockSize);
        return count;
    }

    private static bool CountSolutionsRecursive(int[,] grid, ref int count, int maxSolutions, int size = 9, int blockSize = 3)
    {
        // Finde die nächste leere Zelle
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (grid[row, col] == 0)
                {
                    for (int num = 1; num <= size; num++)
                    {
                        if (IsValidMove(grid, row, col, num, size, blockSize))
                        {
                            grid[row, col] = num;

                            if (CountSolutionsRecursive(grid, ref count, maxSolutions, size, blockSize))
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
    public static bool IsValidMove(int[,] grid, int row, int col, int num, int size = 9, int blockSize = 3)
    {
        // Prüfe Zeile
        for (int c = 0; c < size; c++)
        {
            if (grid[row, c] == num)
                return false;
        }

        // Prüfe Spalte
        for (int r = 0; r < size; r++)
        {
            if (grid[r, col] == num)
                return false;
        }

        // Prüfe Block
        int blockRow = (row / blockSize) * blockSize;
        int blockCol = (col / blockSize) * blockSize;
        for (int r = blockRow; r < blockRow + blockSize; r++)
        {
            for (int c = blockCol; c < blockCol + blockSize; c++)
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
    public static int[,] CopyGrid(int[,] grid, int size = 9)
    {
        int[,] copy = new int[size, size];
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                copy[row, col] = grid[row, col];
            }
        }
        return copy;
    }
}
