namespace PrebuiltPuzzlesValidator;

/// <summary>
/// High-performance Sudoku solver using bitwise constraint propagation.
/// Uses bitmasks to track available candidates per cell, row, column, and box.
/// </summary>
public static class SudokuSolver
{
    private const int AllBits = 0x1FF; // bits 0-8 set = digits 1-9

    public static int CountSolutions(int[,] grid, int maxSolutions = 2, int size = 9, int blockSize = 3)
    {
        // Convert grid to flat array for speed
        Span<int> cells = stackalloc int[81];
        Span<int> rowMask = stackalloc int[9];
        Span<int> colMask = stackalloc int[9];
        Span<int> boxMask = stackalloc int[9];

        // Initialize all masks to have all candidates available
        for (int i = 0; i < 9; i++)
        {
            rowMask[i] = AllBits;
            colMask[i] = AllBits;
            boxMask[i] = AllBits;
        }

        // Place initial values and update masks
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int idx = r * 9 + c;
                int val = grid[r, c];
                cells[idx] = val;
                if (val != 0)
                {
                    int bit = 1 << (val - 1);
                    int box = (r / 3) * 3 + (c / 3);
                    rowMask[r] &= ~bit;
                    colMask[c] &= ~bit;
                    boxMask[box] &= ~bit;
                }
            }
        }

        int count = 0;
        SolveRecursive(cells, rowMask, colMask, boxMask, ref count, maxSolutions);
        return count;
    }

    private static bool SolveRecursive(
        Span<int> cells,
        Span<int> rowMask,
        Span<int> colMask,
        Span<int> boxMask,
        ref int count,
        int maxSolutions)
    {
        // Find cell with minimum remaining values (MRV heuristic)
        int bestIdx = -1;
        int bestCandidates = 0;
        int bestCount = 10;

        for (int idx = 0; idx < 81; idx++)
        {
            if (cells[idx] != 0) continue;

            int r = idx / 9;
            int c = idx % 9;
            int box = (r / 3) * 3 + (c / 3);

            int candidates = rowMask[r] & colMask[c] & boxMask[box];
            if (candidates == 0)
                return false; // No valid moves - dead end

            int popCount = BitCount(candidates);
            if (popCount < bestCount)
            {
                bestCount = popCount;
                bestIdx = idx;
                bestCandidates = candidates;

                if (bestCount == 1)
                    break; // Can't do better than 1
            }
        }

        if (bestIdx == -1)
        {
            // All cells filled - found a solution
            count++;
            return count >= maxSolutions;
        }

        int bestR = bestIdx / 9;
        int bestC = bestIdx % 9;
        int bestBox = (bestR / 3) * 3 + (bestC / 3);

        // Try each candidate
        while (bestCandidates != 0)
        {
            int bit = bestCandidates & -bestCandidates; // Lowest set bit
            bestCandidates &= ~bit; // Remove it
            int digit = BitToDigit(bit);

            // Place digit
            cells[bestIdx] = digit;
            rowMask[bestR] &= ~bit;
            colMask[bestC] &= ~bit;
            boxMask[bestBox] &= ~bit;

            if (SolveRecursive(cells, rowMask, colMask, boxMask, ref count, maxSolutions))
            {
                // Restore and return early
                cells[bestIdx] = 0;
                rowMask[bestR] |= bit;
                colMask[bestC] |= bit;
                boxMask[bestBox] |= bit;
                return true;
            }

            // Restore
            cells[bestIdx] = 0;
            rowMask[bestR] |= bit;
            colMask[bestC] |= bit;
            boxMask[bestBox] |= bit;
        }

        return false;
    }

    private static int BitCount(int n)
    {
        // Fast population count
        n = n - ((n >> 1) & 0x55555555);
        n = (n & 0x33333333) + ((n >> 2) & 0x33333333);
        return (((n + (n >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
    }

    private static int BitToDigit(int bit)
    {
        // Convert single-bit mask to digit 1-9
        return bit switch
        {
            0x001 => 1,
            0x002 => 2,
            0x004 => 3,
            0x008 => 4,
            0x010 => 5,
            0x020 => 6,
            0x040 => 7,
            0x080 => 8,
            0x100 => 9,
            _ => 0
        };
    }

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
