namespace SudokuSen.Logic;

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
        CountSolutionsRecursiveMrv(grid, ref count, maxSolutions, size, blockSize);
        return count;
    }

    private static bool CountSolutionsRecursiveMrv(int[,] grid, ref int count, int maxSolutions, int size = 9, int blockSize = 3)
    {
        // Pick the next empty cell with the fewest candidates (MRV heuristic)
        int bestRow = -1;
        int bestCol = -1;
        Span<int> bestCandidates = stackalloc int[9];
        int bestCandidateCount = int.MaxValue;
        Span<int> candidates = stackalloc int[9];

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (grid[row, col] != 0) continue;

                int candCount = 0;
                for (int num = 1; num <= size; num++)
                {
                    if (IsValidMove(grid, row, col, num, size, blockSize))
                        candidates[candCount++] = num;
                }

                // Dead end
                if (candCount == 0)
                    return false;

                if (candCount < bestCandidateCount)
                {
                    bestCandidateCount = candCount;
                    bestRow = row;
                    bestCol = col;
                    for (int i = 0; i < candCount; i++)
                        bestCandidates[i] = candidates[i];

                    if (bestCandidateCount == 1)
                        goto FOUND_BEST; // can't do better than 1
                }
            }
        }

    FOUND_BEST:
        // No empties => one solution found
        if (bestRow == -1)
        {
            count++;
            return count >= maxSolutions;
        }

        for (int i = 0; i < bestCandidateCount; i++)
        {
            int num = bestCandidates[i];
            grid[bestRow, bestCol] = num;

            if (CountSolutionsRecursiveMrv(grid, ref count, maxSolutions, size, blockSize))
            {
                grid[bestRow, bestCol] = 0;
                return true;
            }

            grid[bestRow, bestCol] = 0;
        }

        return false;
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
