using System.Collections.Concurrent;
using System.Text.Json;

namespace PrebuiltPuzzlesValidator;

internal static class Program
{
    private sealed record PuzzleJson(string id, string difficulty, string solution, string puzzle, int givens);

    private enum Difficulty
    {
        Easy,
        Medium,
        Hard,
        Insane,
        Kids
    }

    public static int Main(string[] args)
    {
        try
        {
            bool fix = args.Any(a => string.Equals(a, "--fix", StringComparison.OrdinalIgnoreCase));
            bool generate = args.Any(a => string.Equals(a, "--generate", StringComparison.OrdinalIgnoreCase));

            string repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            string jsonPath = Path.Combine(repoRoot, "Scripts", "Services", "prebuilt_puzzles.json");

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"prebuilt_puzzles.json not found at: {jsonPath}");

            List<PuzzleJson> puzzles;
            if (generate)
            {
                puzzles = GenerateAllPuzzles();
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(puzzles, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
                Console.WriteLine($"WROTE: {jsonPath}");
                fix = false;
            }
            else
            {
                puzzles = JsonSerializer.Deserialize<List<PuzzleJson>>(File.ReadAllText(jsonPath), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize JSON.");

                if (puzzles.Count == 0)
                    throw new InvalidOperationException("JSON contains no puzzles.");
            }

            SelfTest(puzzles);

            ValidateJsonOrder(puzzles);

            bool changed = ValidateContentAndUniqueness(puzzles, fix);

            if (fix && changed)
            {
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(puzzles, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
                Console.WriteLine($"WROTE: {jsonPath}");

                // Re-validate after writing.
                ValidateJsonOrder(puzzles);
                _ = ValidateContentAndUniqueness(puzzles, fix: false);
            }

            Console.WriteLine($"OK: {puzzles.Count} puzzles validated{(changed ? " (fixed)" : "")}." );
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("VALIDATION FAILED");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void ValidateJsonOrder(IReadOnlyList<PuzzleJson> puzzles)
    {
        int prevRank = -1;
        int? prevGivensInDifficulty = null;
        Difficulty? currentDifficulty = null;
        int prevIndexInDifficulty = -1;

        for (int i = 0; i < puzzles.Count; i++)
        {
            var p = puzzles[i];

            if (!Enum.TryParse<Difficulty>(p.difficulty, true, out var diff))
                throw new InvalidOperationException($"Unknown difficulty '{p.difficulty}' (id={p.id})");

            int rank = GetDifficultyRank(diff);
            if (rank < prevRank)
                throw new InvalidOperationException($"Difficulty decreases at JSON index {i} (id={p.id}).");

            int idx = ExtractIndex(p.id);
            if (currentDifficulty != diff)
            {
                currentDifficulty = diff;
                prevGivensInDifficulty = null;
                prevIndexInDifficulty = -1;
            }
            else
            {
                if (idx < prevIndexInDifficulty)
                    throw new InvalidOperationException($"ID index decreases within difficulty {diff} at JSON index {i} (id={p.id}).");

                if (prevGivensInDifficulty.HasValue && p.givens > prevGivensInDifficulty.Value)
                    throw new InvalidOperationException($"Givens increase within difficulty {diff} at JSON index {i} (id={p.id}).");
            }

            prevRank = rank;
            prevGivensInDifficulty = p.givens;
            prevIndexInDifficulty = idx;
        }
    }

    private static bool ValidateContentAndUniqueness(IList<PuzzleJson> puzzles, bool fix)
    {
        bool changed = false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var solutions = new HashSet<string>(StringComparer.Ordinal);

        var errors = new List<string>();

        for (int pi = 0; pi < puzzles.Count; pi++)
        {
            var p = puzzles[pi];
            if (string.IsNullOrWhiteSpace(p.id))
                throw new InvalidOperationException("Puzzle id is missing.");

            if (!ids.Add(p.id))
                throw new InvalidOperationException($"Duplicate puzzle id: {p.id}");

            if (!Enum.TryParse<Difficulty>(p.difficulty, true, out var diff))
                throw new InvalidOperationException($"Unknown difficulty '{p.difficulty}' (id={p.id})");

            if (string.IsNullOrWhiteSpace(p.solution) || p.solution.Length != 81)
                throw new InvalidOperationException($"Invalid solution length for {p.id}.");

            if (string.IsNullOrWhiteSpace(p.puzzle) || p.puzzle.Length != 81)
                throw new InvalidOperationException($"Invalid puzzle length for {p.id}.");

            bool solutionDigitsOk = p.solution.All(ch => ch is >= '1' and <= '9');
            bool solutionValid = solutionDigitsOk && TryValidateSolutionDetailed(p.solution, out _);
            bool solutionUnique = solutions.Add(p.solution);

            if (!solutionValid || !solutionUnique)
            {
                string reason;
                if (!solutionDigitsOk)
                {
                    reason = "contains non 1-9 digits";
                }
                else if (!solutionUnique)
                {
                    reason = "duplicate solution";
                }
                else
                {
                    _ = TryValidateSolutionDetailed(p.solution, out var details);
                    reason = $"invalid Sudoku grid ({details})";
                }

                if (!fix)
                {
                    errors.Add($"{p.id}: solution {reason}");
                }
                else
                {
                    // Generate a new valid, unique solution that also yields a unique puzzle under our deterministic givens-generation.
                    var fixedSolution = GenerateReplacementSolution(puzzles, p);
                    int baseSeed = SeedFromId(p.id);
                    string fixedPuzzle = MakeValidatedGivens(fixedSolution, p.givens, baseSeed, enforceRotationalSymmetry: true);
                    puzzles[pi] = p with { solution = fixedSolution, puzzle = fixedPuzzle };
                    changed = true;

                    // Re-track uniqueness set: replace old entry.
                    solutions.Remove(p.solution);
                    solutions.Add(fixedSolution);
                    p = puzzles[pi];
                }
            }

            if (p.givens is < 17 or > 81)
                throw new InvalidOperationException($"Givens out of range for {p.id}: {p.givens}");

            // After potential fix, ensure solution is valid.
            if (!p.solution.All(ch => ch is >= '1' and <= '9'))
            {
                errors.Add($"{p.id}: solution invalid even after fix attempt (contains non 1-9 digits)");
            }
            else if (!TryValidateSolutionDetailed(p.solution, out var afterDetails))
            {
                errors.Add($"{p.id}: solution invalid even after fix attempt ({afterDetails})");
            }

            // Deterministic puzzle generation + uniqueness check (matches PrebuiltPuzzleLibrary logic).
            string givensStr = p.puzzle;

            int actualGivens = givensStr.Count(ch => ch != '.');
            if (actualGivens != p.givens)
                errors.Add($"{p.id}: generated givens mismatch: expected {p.givens}, got {actualGivens}");

            for (int i = 0; i < 81; i++)
            {
                char g = givensStr[i];
                if (g == '.') continue;
                if (g != p.solution[i])
                    errors.Add($"{p.id}: generated givens disagree with solution at index {i}");
            }

            if (!HasUniqueSolution(givensStr))
            {
                if (!fix)
                {
                    errors.Add($"{p.id}: generated puzzle is not uniquely solvable");
                }
                else
                {
                    // Regenerate solution + puzzle until this id yields a unique puzzle.
                    var fixedSolution = GenerateReplacementSolution(puzzles, p);
                    int baseSeed = SeedFromId(p.id);
                    string fixedPuzzle = MakeValidatedGivens(fixedSolution, p.givens, baseSeed, enforceRotationalSymmetry: true);
                    puzzles[pi] = p with { solution = fixedSolution, puzzle = fixedPuzzle };
                    changed = true;
                    solutions.Remove(p.solution);
                    solutions.Add(fixedSolution);
                    p = puzzles[pi];

                    // Re-run generation/uniqueness check with the new solution.
                    givensStr = p.puzzle;
                    if (!HasUniqueSolution(givensStr))
                        errors.Add($"{p.id}: generated puzzle is not uniquely solvable (even after fix)");
                }
            }

            // Difficulty sanity: higher rank should not be easier by having more givens than any lower rank puzzle.
            // (Not a strict guarantee of human difficulty, but catches obvious ordering issues.)
            _ = diff;
        }

        // Cross-difficulty monotonicity check by givens (overall difficulty proxy)
        // Ensures top-to-bottom in JSON also trends harder by not increasing givens across the whole file.
        int? prevGivens = null;
        for (int i = 0; i < puzzles.Count; i++)
        {
            int g = puzzles[i].givens;
            if (prevGivens.HasValue && g > prevGivens.Value)
                errors.Add($"{puzzles[i].id}: overall givens increase at JSON index {i}");
            prevGivens = g;
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

        return changed;
    }

    private static string GenerateReplacementSolution(IEnumerable<PuzzleJson> allPuzzles, PuzzleJson puzzle)
    {
        // We generate a fully-solved grid and ensure:
        // - it's Sudoku-valid
        // - it's not a duplicate of another solution
        // - with this puzzle's givensCount and id-seed, we can create a uniquely solvable puzzle
        var existingSolutions = new HashSet<string>(allPuzzles.Select(p => p.solution), StringComparer.Ordinal);
        int baseSeed = SeedFromId(puzzle.id);

        for (int attempt = 0; attempt < 20000; attempt++)
        {
            int seed = unchecked(baseSeed + attempt * 104729); // large prime stride
            string candidate = GenerateFullSolutionString(seed);

            if (!TryValidateSolutionDetailed(candidate, out _))
                continue;
            if (existingSolutions.Contains(candidate))
                continue;

            string givensStr = MakeValidatedGivens(candidate, puzzle.givens, baseSeed, enforceRotationalSymmetry: true);
            if (!HasUniqueSolution(givensStr))
                continue;

            return candidate;
        }

        throw new InvalidOperationException($"Unable to generate a replacement solution for {puzzle.id} after many attempts.");
    }

    private static string GenerateFullSolutionString(int seed)
    {
        var rng = new Random(seed);
        var grid = new int[9, 9];
        if (!FillGrid(grid, rng))
            throw new InvalidOperationException("Failed to generate a full Sudoku grid.");

        var chars = new char[81];
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                chars[r * 9 + c] = (char)('0' + grid[r, c]);
            }
        }
        return new string(chars);
    }

    private static bool FillGrid(int[,] grid, Random rng)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (grid[r, c] != 0) continue;

                Span<int> nums = stackalloc int[9];
                for (int i = 0; i < 9; i++) nums[i] = i + 1;
                Shuffle(nums, rng);

                for (int i = 0; i < 9; i++)
                {
                    int n = nums[i];
                    if (!IsValidMove(grid, r, c, n))
                        continue;

                    grid[r, c] = n;
                    if (FillGrid(grid, rng))
                        return true;
                    grid[r, c] = 0;
                }

                return false;
            }
        }

        return true;
    }

    private static void Shuffle(Span<int> nums, Random rng)
    {
        for (int i = nums.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (nums[i], nums[j]) = (nums[j], nums[i]);
        }
    }

    private static bool IsValidMove(int[,] grid, int row, int col, int num)
    {
        for (int c = 0; c < 9; c++)
            if (grid[row, c] == num) return false;

        for (int r = 0; r < 9; r++)
            if (grid[r, col] == num) return false;

        int br = (row / 3) * 3;
        int bc = (col / 3) * 3;
        for (int r = br; r < br + 3; r++)
            for (int c = bc; c < bc + 3; c++)
                if (grid[r, c] == num) return false;

        return true;
    }

    private static int GetDifficultyRank(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 0,
        Difficulty.Medium => 1,
        Difficulty.Hard => 2,
        Difficulty.Insane => 3,
        Difficulty.Kids => -1,
        _ => 99
    };

    private static int ExtractIndex(string id)
    {
        var parts = id.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 && int.TryParse(parts[1], out int idx) ? idx : 0;
    }

    private static int SeedFromId(string id)
    {
        int seed = 17;
        foreach (var ch in id)
            seed = unchecked(seed * 31 + ch);
        return seed;
    }

    private static string MakeValidatedGivens(string solutionStr, int givensCount, int seed, bool enforceRotationalSymmetry)
    {
        // Reduce attempts - with optimized solver we can try fewer but they're faster
        int maxAttempts = givensCount <= 24 ? 500 : 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int attemptSeed = unchecked(seed + attempt * 9973);
            var candidate = MakeGivens(solutionStr, givensCount, attemptSeed, enforceRotationalSymmetry);
            if (HasUniqueSolution(candidate))
                return candidate;
        }
        // Return last attempt even if not unique - caller will retry with different solution
        return MakeGivens(solutionStr, givensCount, seed, enforceRotationalSymmetry);
    }

    private static string MakeGivens(string solutionStr, int givensCount, int seed, bool enforceRotationalSymmetry)
    {
        int count = Math.Clamp(givensCount, 17, 81);
        var rng = new Random(seed);
        var chars = Enumerable.Repeat('.', 81).ToArray();

        if (enforceRotationalSymmetry)
        {
            var pairs = new List<(int a, int b)>(41);
            for (int i = 0; i < 81; i++)
            {
                int j = 80 - i;
                if (i > j) break;
                pairs.Add((i, j));
            }

            for (int i = pairs.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pairs[i], pairs[j]) = (pairs[j], pairs[i]);
            }

            int remaining = count;
            foreach (var (a, b) in pairs)
            {
                if (remaining <= 0) break;

                if (a == b)
                {
                    if (remaining >= 1)
                    {
                        chars[a] = solutionStr[a];
                        remaining -= 1;
                    }
                }
                else if (remaining >= 2)
                {
                    chars[a] = solutionStr[a];
                    chars[b] = solutionStr[b];
                    remaining -= 2;
                }
                else if (remaining == 1)
                {
                    int idx = rng.Next(2) == 0 ? a : b;
                    chars[idx] = solutionStr[idx];
                    remaining = 0;
                }
            }
        }
        else
        {
            var indices = Enumerable.Range(0, 81).ToArray();
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            foreach (var idx in indices.Take(count))
                chars[idx] = solutionStr[idx];
        }

        return new string(chars);
    }

    private static bool HasUniqueSolution(string givensStr)
    {
        // Inline grid creation to avoid allocation overhead
        Span<int> cells = stackalloc int[81];
        for (int i = 0; i < 81; i++)
        {
            char ch = givensStr[i];
            cells[i] = ch == '.' ? 0 : ch - '0';
        }

        return CountSolutionsFast(cells, maxSolutions: 2) == 1;
    }

    private static int CountSolutionsFast(Span<int> cells, int maxSolutions)
    {
        const int AllBits = 0x1FF;
        Span<int> rowMask = stackalloc int[9];
        Span<int> colMask = stackalloc int[9];
        Span<int> boxMask = stackalloc int[9];

        for (int i = 0; i < 9; i++)
        {
            rowMask[i] = AllBits;
            colMask[i] = AllBits;
            boxMask[i] = AllBits;
        }

        for (int idx = 0; idx < 81; idx++)
        {
            int val = cells[idx];
            if (val != 0)
            {
                int r = idx / 9;
                int c = idx % 9;
                int box = (r / 3) * 3 + (c / 3);
                int bit = 1 << (val - 1);
                rowMask[r] &= ~bit;
                colMask[c] &= ~bit;
                boxMask[box] &= ~bit;
            }
        }

        int count = 0;
        SolveFast(cells, rowMask, colMask, boxMask, ref count, maxSolutions);
        return count;
    }

    private static bool SolveFast(
        Span<int> cells,
        Span<int> rowMask,
        Span<int> colMask,
        Span<int> boxMask,
        ref int count,
        int maxSolutions)
    {
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
                return false;

            int popCount = System.Numerics.BitOperations.PopCount((uint)candidates);
            if (popCount < bestCount)
            {
                bestCount = popCount;
                bestIdx = idx;
                bestCandidates = candidates;
                if (bestCount == 1) break;
            }
        }

        if (bestIdx == -1)
        {
            count++;
            return count >= maxSolutions;
        }

        int bestR = bestIdx / 9;
        int bestC = bestIdx % 9;
        int bestBox = (bestR / 3) * 3 + (bestC / 3);

        while (bestCandidates != 0)
        {
            int bit = bestCandidates & -bestCandidates;
            bestCandidates &= ~bit;
            int digit = System.Numerics.BitOperations.TrailingZeroCount(bit) + 1;

            cells[bestIdx] = digit;
            rowMask[bestR] &= ~bit;
            colMask[bestC] &= ~bit;
            boxMask[bestBox] &= ~bit;

            if (SolveFast(cells, rowMask, colMask, boxMask, ref count, maxSolutions))
            {
                cells[bestIdx] = 0;
                rowMask[bestR] |= bit;
                colMask[bestC] |= bit;
                boxMask[bestBox] |= bit;
                return true;
            }

            cells[bestIdx] = 0;
            rowMask[bestR] |= bit;
            colMask[bestC] |= bit;
            boxMask[bestBox] |= bit;
        }

        return false;
    }

    private static bool TryValidateSolutionDetailed(string solution, out string details)
    {
        details = "ok";

        bool[,] rowSeen = new bool[9, 10];
        bool[,] colSeen = new bool[9, 10];
        bool[,] boxSeen = new bool[9, 10];

        for (int i = 0; i < 81; i++)
        {
            int r = i / 9;
            int c = i % 9;
            int b = (r / 3) * 3 + (c / 3);
            int digit = solution[i] - '0';

            if (digit < 1 || digit > 9)
            {
                details = $"bad digit '{solution[i]}' at i={i} (r={r},c={c})";
                return false;
            }

            if (rowSeen[r, digit])
            {
                details = $"row duplicate digit {digit} at r={r} (i={i})";
                return false;
            }
            if (colSeen[c, digit])
            {
                details = $"col duplicate digit {digit} at c={c} (i={i})";
                return false;
            }
            if (boxSeen[b, digit])
            {
                details = $"box duplicate digit {digit} at b={b} (i={i}, r={r}, c={c})";
                return false;
            }

            rowSeen[r, digit] = true;
            colSeen[c, digit] = true;
            boxSeen[b, digit] = true;
        }

        return true;
    }

    private static void SelfTest(IReadOnlyList<PuzzleJson> puzzles)
    {
        // Sanity-check the solver: with exactly one empty cell, solution count must be 1.
        var p = puzzles.FirstOrDefault(x => x.solution?.Length == 81);
        if (p == null) return;

        var grid = new int[9, 9];
        for (int i = 0; i < 81; i++)
        {
            int r = i / 9;
            int c = i % 9;
            grid[r, c] = p.solution[i] - '0';
        }
        grid[0, 0] = 0;

        int solutions = SudokuSolver.CountSolutions(SudokuSolver.CopyGrid(grid), maxSolutions: 2);
        if (solutions != 1)
            throw new InvalidOperationException($"[SelfTest] Expected 1 solution for 1-empty-cell grid, got {solutions}." );
    }

    private static List<PuzzleJson> GenerateAllPuzzles()
    {
        // Match the existing counts and givens bands.
        var spec = new List<(Difficulty diff, string prefix, int count, int[] givensBands)>
        {
            (Difficulty.Easy, "easy", 30, new[] { 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 40, 40, 40, 40, 40, 40, 40, 40, 40, 40, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36 }),
            (Difficulty.Medium, "medium", 30, new[] { 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }),
            (Difficulty.Hard, "hard", 30, new[] { 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26 }),
            (Difficulty.Insane, "insane", 10, new[] { 25, 25, 25, 25, 25, 25, 25, 25, 25, 25 }),
        };

        var entries = new List<(Difficulty diff, string id, int givens)>();
        foreach (var (diff, prefix, count, givensBands) in spec)
        {
            for (int i = 1; i <= count; i++)
            {
                entries.Add((diff, $"{prefix}_{i}", givensBands[i - 1]));
            }
        }

        var solutions = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        var puzzles = new ConcurrentBag<PuzzleJson>();

        // Use all cores for maximum speed
        Parallel.ForEach(entries, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, entry =>
        {
            var (diff, id, givens) = entry;
            Console.WriteLine($"Generating {id}...");
            var (solution, puzzle) = GenerateOneFast(id, givens, solutions);
            puzzles.Add(new PuzzleJson(id, diff.ToString(), solution, puzzle, givens));
            Console.WriteLine($"  Done: {id}");
        });

        var ordered = puzzles
            .OrderBy(p => GetDifficultyRank(Enum.Parse<Difficulty>(p.difficulty, true)))
            .ThenBy(p => ExtractIndex(p.id))
            .ToList();

        ValidateJsonOrder(ordered);
        _ = ValidateContentAndUniqueness(ordered, fix: false);

        return ordered;
    }

    // Base valid Sudoku - we'll permute this to generate others instantly
    private static readonly string BaseSolution = "123456789456789123789123456214365897365897214897214365531642978642978531978531642";

    private static (string solution, string puzzle) GenerateOneFast(string id, int givens, ConcurrentDictionary<string, byte> solutions)
    {
        int idSeed = SeedFromId(id);

        for (int attempt = 0; attempt < 50000; attempt++)
        {
            int seed = unchecked(idSeed + attempt * 13331);

            // Generate solution by permuting the base - guaranteed valid!
            string solution = PermuteSolution(BaseSolution, seed);

            if (!solutions.TryAdd(solution, 0))
                continue;

            // Generate puzzle with uniqueness check
            string puzzle = MakeUniquePuzzle(solution, givens, seed);
            if (puzzle != null)
                return (solution, puzzle);

            solutions.TryRemove(solution, out _);
        }

        throw new InvalidOperationException($"Unable to generate puzzle for {id} with givens={givens}.");
    }

    private static string PermuteSolution(string baseSol, int seed)
    {
        var rng = new Random(seed);
        var grid = new char[81];
        baseSol.CopyTo(0, grid, 0, 81);

        // 1. Permute digits (relabel 1-9)
        var digitMap = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        for (int i = 8; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (digitMap[i], digitMap[j]) = (digitMap[j], digitMap[i]);
        }
        for (int i = 0; i < 81; i++)
        {
            int oldDigit = grid[i] - '1';
            grid[i] = (char)('0' + digitMap[oldDigit]);
        }

        // 2. Shuffle rows within each band (3 bands of 3 rows each)
        for (int band = 0; band < 3; band++)
        {
            int[] rows = { band * 3, band * 3 + 1, band * 3 + 2 };
            for (int i = 2; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                if (i != j) SwapRows(grid, rows[i], rows[j]);
            }
        }

        // 3. Shuffle columns within each stack (3 stacks of 3 cols each)
        for (int stack = 0; stack < 3; stack++)
        {
            int[] cols = { stack * 3, stack * 3 + 1, stack * 3 + 2 };
            for (int i = 2; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                if (i != j) SwapCols(grid, cols[i], cols[j]);
            }
        }

        // 4. Shuffle bands (groups of 3 rows)
        int[] bands = { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            if (i != j) SwapBands(grid, bands[i], bands[j]);
        }

        // 5. Shuffle stacks (groups of 3 cols)
        int[] stacks = { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            if (i != j) SwapStacks(grid, stacks[i], stacks[j]);
        }

        return new string(grid);
    }

    private static void SwapRows(char[] grid, int r1, int r2)
    {
        for (int c = 0; c < 9; c++)
        {
            (grid[r1 * 9 + c], grid[r2 * 9 + c]) = (grid[r2 * 9 + c], grid[r1 * 9 + c]);
        }
    }

    private static void SwapCols(char[] grid, int c1, int c2)
    {
        for (int r = 0; r < 9; r++)
        {
            (grid[r * 9 + c1], grid[r * 9 + c2]) = (grid[r * 9 + c2], grid[r * 9 + c1]);
        }
    }

    private static void SwapBands(char[] grid, int b1, int b2)
    {
        for (int i = 0; i < 3; i++)
            SwapRows(grid, b1 * 3 + i, b2 * 3 + i);
    }

    private static void SwapStacks(char[] grid, int s1, int s2)
    {
        for (int i = 0; i < 3; i++)
            SwapCols(grid, s1 * 3 + i, s2 * 3 + i);
    }

    private static string? MakeUniquePuzzle(string solution, int givens, int baseSeed)
    {
        // Try more patterns for hard puzzles
        int maxAttempts = givens <= 27 ? 200 : 50;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int seed = unchecked(baseSeed + attempt * 7919);
            string puzzle = MakeGivensSymmetric(solution, givens, seed);
            if (HasUniqueSolution(puzzle))
                return puzzle;
        }
        return null;
    }

    private static string MakeGivensSymmetric(string solution, int givens, int seed)
    {
        var rng = new Random(seed);
        var chars = new char[81];
        for (int i = 0; i < 81; i++) chars[i] = '.';

        // Build symmetric pairs - center cell (40) is its own pair
        var pairs = new List<(int a, int b)>();
        for (int i = 0; i <= 40; i++)
        {
            int j = 80 - i;
            pairs.Add((i, j));
        }

        // Shuffle pairs
        for (int i = pairs.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pairs[i], pairs[j]) = (pairs[j], pairs[i]);
        }

        int placed = 0;
        foreach (var (a, b) in pairs)
        {
            if (placed >= givens) break;

            if (a == b)
            {
                // Center cell
                chars[a] = solution[a];
                placed++;
            }
            else
            {
                // Symmetric pair - place both if we have room for 2, or just one if we need exactly 1 more
                if (givens - placed >= 2)
                {
                    chars[a] = solution[a];
                    chars[b] = solution[b];
                    placed += 2;
                }
                else if (givens - placed == 1)
                {
                    // Need exactly 1 more - pick one of the pair
                    int idx = rng.Next(2) == 0 ? a : b;
                    chars[idx] = solution[idx];
                    placed++;
                }
            }
        }

        return new string(chars);
    }

    private static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SudokuSen.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repo root (SudokuSen.sln)." );
    }
}
