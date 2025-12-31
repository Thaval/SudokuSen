namespace SudokuSen.Logic;

/// <summary>
/// Generiert gültige Sudoku-Rätsel mit eindeutiger Lösung
/// </summary>
public static class SudokuGenerator
{
    /// <summary>
    /// Generiert ein neues Sudoku-Spiel
    /// </summary>
    public static SudokuGameState Generate(Difficulty difficulty, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var state = new SudokuGameState
        {
            Difficulty = difficulty,
            StartTime = DateTime.Now,
            Status = GameStatus.InProgress
        };

        int size = state.GridSize;
        int blockSize = state.BlockSize;

        // Generiere vollständiges Grid
        int[,] fullGrid = GenerateFullGrid(rng, size, blockSize);

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
        int[,] puzzleGrid = RemoveCells(fullGrid, cellsToRemove, rng, size, blockSize);

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
    /// Generiert ein Puzzle, das gezielt eine bestimmte Technik erfordert
    /// </summary>
    public static SudokuGameState GenerateForTechnique(string techniqueId, Difficulty difficulty, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        // Generiere mehrere Puzzles und wähle das beste für die Technik
        SudokuGameState? bestPuzzle = null;
        int bestScore = -1;
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var puzzle = Generate(difficulty, seed.HasValue ? seed.Value + attempt : null);
            int score = EvaluateTechniqueScore(puzzle, techniqueId);

            if (score > bestScore)
            {
                bestScore = score;
                bestPuzzle = puzzle;
            }

            // Früh abbrechen wenn ein gutes Puzzle gefunden wurde
            if (score >= 3) break;
        }

        return bestPuzzle ?? Generate(difficulty, seed);
    }

    /// <summary>
    /// Bewertet wie gut ein Puzzle für eine bestimmte Technik geeignet ist
    /// </summary>
    private static int EvaluateTechniqueScore(SudokuGameState puzzle, string techniqueId)
    {
        // Simplifizierte Bewertung basierend auf Puzzle-Eigenschaften
        // Ein komplexeres System würde den Solver mit Technik-Tracking nutzen
        int score = 0;
        int size = puzzle.GridSize;
        int emptyCells = 0;

        // Zähle leere Zellen
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (puzzle.Grid[row, col].Value == 0)
                    emptyCells++;
            }
        }

        // Basis-Score basierend auf Komplexität
        score = techniqueId switch
        {
            // Leichte Techniken - weniger leere Zellen sind besser
            "NakedSingle" or "HiddenSingleRow" or "HiddenSingleCol" or "HiddenSingleBlock"
                => Math.Max(0, 50 - emptyCells),

            // Mittlere Techniken - mittlere Anzahl leerer Zellen
            "NakedPair" or "NakedTriple" or "HiddenPair" or "PointingPair" or "BoxLineReduction"
                => Math.Min(emptyCells, 55 - emptyCells) / 5,

            // Schwere Techniken - mehr leere Zellen sind besser
            "XWing" or "Swordfish" or "XYWing" or "Skyscraper" or "SimpleColoring"
                => emptyCells / 10,

            _ => emptyCells / 15
        };

        return Math.Max(0, score);
    }

    /// <summary>
    /// Generiert ein vollständig gelöstes Sudoku-Grid
    /// </summary>
    private static int[,] GenerateFullGrid(Random rng, int size = 9, int blockSize = 3)
    {
        int[,] grid = new int[size, size];
        FillGrid(grid, rng, size, blockSize);
        return grid;
    }

    private static bool FillGrid(int[,] grid, Random rng, int size = 9, int blockSize = 3)
    {
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (grid[row, col] == 0)
                {
                    // Erstelle eine zufällige Reihenfolge von 1-size
                    List<int> numbers = new();
                    for (int i = 1; i <= size; i++)
                        numbers.Add(i);
                    ShuffleList(numbers, rng);

                    foreach (int num in numbers)
                    {
                        if (SudokuSolver.IsValidMove(grid, row, col, num, size, blockSize))
                        {
                            grid[row, col] = num;

                            if (FillGrid(grid, rng, size, blockSize))
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
    private static int[,] RemoveCells(int[,] fullGrid, int cellsToRemove, Random rng, int size = 9, int blockSize = 3)
    {
        int[,] puzzle = SudokuSolver.CopyGrid(fullGrid, size);

        // Erstelle Liste aller Positionen
        List<(int row, int col)> positions = new();
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                positions.Add((row, col));
            }
        }
        ShuffleList(positions, rng);

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
        ShuffleList(list, new Random());
    }

    private static void ShuffleList<T>(List<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
