namespace SudokuSen.Models;

/// <summary>
/// A static, pre-built Sudoku puzzle.
/// </summary>
public record PrebuiltPuzzle(
    string Id,
    Difficulty Difficulty,
    int[,] Solution,
    bool[,] Givens
)
{
    /// <summary>
    /// Human-readable puzzle name (e.g. "Easy #1").
    /// Derived from Id and difficulty at runtime by LocalizationService.
    /// </summary>
    public string GetDisplayName(LocalizationService loc)
    {
        // Extract number from Id (e.g., "easy_1" → "1")
        string number = Id.Split('_')[^1];
        string difficultyName = loc.GetDifficultyName(Difficulty);
        return $"{difficultyName} #{number}";
    }

    /// <summary>
    /// Creates a SudokuGameState for this puzzle so it can be played.
    /// </summary>
    public SudokuGameState ToGameState()
    {
        var state = new SudokuGameState
        {
            Difficulty = Difficulty,
            StartTime = DateTime.Now,
            Status = GameStatus.InProgress,
            PrebuiltPuzzleId = Id
        };

        int size = state.GridSize;
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                int sol = Solution[r, c];
                bool isGiven = Givens[r, c];
                state.Grid[r, c] = new SudokuCell
                {
                    Value = isGiven ? sol : 0,
                    Solution = sol,
                    IsGiven = isGiven,
                    Notes = new bool[9]
                };
            }
        }

        return state;
    }

    /// <summary>
    /// Computes a hash of the solution for duplicate detection.
    /// </summary>
    public string GetSolutionHash()
    {
        // Simple hash: concatenate all solution digits
        var sb = new System.Text.StringBuilder(81);
        int size = Solution.GetLength(0);
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                sb.Append(Solution[r, c]);
        return sb.ToString();
    }
}
