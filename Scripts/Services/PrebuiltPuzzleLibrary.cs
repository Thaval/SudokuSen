namespace SudokuSen.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using SudokuSen.Logic;
using SudokuSen.Models;

/// <summary>
/// Provides the static library of prebuilt Sudoku puzzles.
/// </summary>
public static class PrebuiltPuzzleLibrary
{
    private record PuzzleMeta(string Id, Difficulty Difficulty, string SolutionStr, string PuzzleStr, int GivensCount);
    private record PuzzleJson(string id, string difficulty, string solution, string puzzle, int givens);

    private const string PuzzleJsonPath = "res://Scripts/Services/prebuilt_puzzles.json";

    private static readonly List<PuzzleMeta> Metadata = BuildMetadata();
    private static readonly Dictionary<string, PrebuiltPuzzle> Cache = new();

    /// <summary>
    /// Set of solution hashes to detect duplicates when generating dynamic puzzles.
    /// </summary>
    public static IReadOnlySet<string> SolutionHashes { get; } = BuildSolutionHashes();

    public record PrebuiltPuzzleMetadata(string Id, Difficulty Difficulty, int GivensCount);

    /// <summary>
    /// Returns lightweight metadata for listing puzzles without building them.
    /// </summary>
    public static IReadOnlyList<PrebuiltPuzzleMetadata> GetMetadataByDifficulty(Difficulty difficulty)
        => Metadata.Where(m => m.Difficulty == difficulty)
                   .Select(m => new PrebuiltPuzzleMetadata(m.Id, m.Difficulty, m.GivensCount))
                   .ToList();

    /// <summary>
    /// Gets all puzzles for a specific difficulty (builds on demand).
    /// </summary>
    public static IEnumerable<PrebuiltPuzzle> GetByDifficulty(Difficulty difficulty)
        => Metadata.Where(m => m.Difficulty == difficulty).Select(BuildPuzzle);

    /// <summary>
    /// Gets a puzzle by its ID.
    /// </summary>
    public static PrebuiltPuzzle? GetById(string id)
    {
        if (Cache.TryGetValue(id, out var cached))
            return cached;

        var meta = Metadata.FirstOrDefault(m => m.Id == id);
        if (meta == null) return null;

        var puzzle = BuildPuzzle(meta);
        Cache[id] = puzzle;
        return puzzle;
    }

    private static IReadOnlySet<string> BuildSolutionHashes()
    {
        var hashes = new HashSet<string>();
        foreach (var meta in Metadata)
            hashes.Add(meta.SolutionStr);
        return hashes;
    }

    private static List<PuzzleMeta> BuildMetadata()
    {
        var jsonText = ReadAllText(PuzzleJsonPath);
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] JSON file is empty: {PuzzleJsonPath}");

        var json = JsonSerializer.Deserialize<List<PuzzleJson>>(jsonText);
        if (json == null || json.Count == 0)
            throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Failed to deserialize puzzles from {PuzzleJsonPath}");

        ValidatePuzzleJson(json);

        var ordered = json
            .Select(ToMeta)
            .OrderBy(m => GetDifficultyRank(m.Difficulty))
            .ThenBy(m => ExtractIndex(m.Id))
            .ToList();

        ValidateOrderingAndUniqueness(ordered);

        // Build-time validation (parallel): ensure puzzle strings are consistent and uniquely solvable.
        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
        System.Threading.Tasks.Parallel.ForEach(ordered, meta =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(meta.PuzzleStr) || meta.PuzzleStr.Length != 81)
                    throw new InvalidOperationException("Puzzle string must be 81 characters.");

                int actualGivens = meta.PuzzleStr.Count(ch => ch != '.');
                if (actualGivens != meta.GivensCount)
                    throw new InvalidOperationException($"Givens mismatch: expected {meta.GivensCount}, got {actualGivens}.");

                for (int i = 0; i < 81; i++)
                {
                    char pch = meta.PuzzleStr[i];
                    if (pch == '.') continue;
                    if (pch < '1' || pch > '9')
                        throw new InvalidOperationException($"Invalid puzzle char '{pch}' at index {i}.");
                    if (pch != meta.SolutionStr[i])
                        throw new InvalidOperationException($"Puzzle digit disagrees with solution at index {i}.");
                }

                if (!HasUniqueSolution(meta.PuzzleStr))
                    throw new InvalidOperationException("Puzzle does not have a unique solution.");
            }
            catch (Exception ex)
            {
                errors.Add($"{meta.Id}: {ex.Message}");
            }
        });

        if (!errors.IsEmpty)
            throw new InvalidOperationException("[PrebuiltPuzzleLibrary] Puzzle validation failed:\n" + string.Join("\n", errors));

        return ordered;
    }

    private static PuzzleMeta ToMeta(PuzzleJson json)
    {
        if (!Enum.TryParse(json.difficulty, true, out Difficulty difficulty))
            throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Unknown difficulty '{json.difficulty}' for puzzle {json.id}");

        return new PuzzleMeta(json.id, difficulty, json.solution, json.puzzle, json.givens);
    }

    private static void ValidatePuzzleJson(IEnumerable<PuzzleJson> puzzles)
    {
        var ids = new HashSet<string>();
        var solutions = new HashSet<string>();
        var puzzlesSet = new HashSet<string>();

        foreach (var p in puzzles)
        {
            if (string.IsNullOrWhiteSpace(p.id))
                throw new InvalidOperationException("[PrebuiltPuzzleLibrary] Puzzle id is missing.");

            if (!ids.Add(p.id))
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Duplicate puzzle id: {p.id}");

            if (string.IsNullOrWhiteSpace(p.solution) || p.solution.Length != 81 || !p.solution.All(char.IsDigit))
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Invalid solution string for {p.id}.");

            if (string.IsNullOrWhiteSpace(p.puzzle) || p.puzzle.Length != 81)
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Invalid puzzle string for {p.id}.");

            if (!solutions.Add(p.solution))
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Duplicate solution string detected for {p.id}.");

            if (!puzzlesSet.Add(p.puzzle))
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Duplicate puzzle string detected for {p.id}.");

            if (p.givens < 17 || p.givens > 81)
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Givens count out of range for {p.id}: {p.givens}");

            if (!Enum.TryParse(p.difficulty, true, out Difficulty _))
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Unknown difficulty '{p.difficulty}' for puzzle {p.id}");

            if (!IsValidSolutionString(p.solution))
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Solution is not a valid Sudoku for {p.id}");

            // Ensure givens count matches puzzle string
            int actualGivens = p.puzzle.Count(ch => ch != '.');
            if (actualGivens != p.givens)
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Givens count mismatch for {p.id}: expected {p.givens}, got {actualGivens}");

            // Ensure puzzle digits match solution
            for (int i = 0; i < 81; i++)
            {
                char ch = p.puzzle[i];
                if (ch == '.') continue;
                if (ch < '1' || ch > '9')
                    throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Invalid puzzle char '{ch}' for {p.id} at index {i}");
                if (ch != p.solution[i])
                    throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Puzzle digit disagrees with solution for {p.id} at index {i}");
            }
        }
    }

    private static void ValidateOrderingAndUniqueness(IReadOnlyList<PuzzleMeta> metas)
    {
        int prevRank = -1;
        int? prevGivens = null;
        Difficulty? currentDifficulty = null;

        foreach (var meta in metas)
        {
            int rank = GetDifficultyRank(meta.Difficulty);
            if (rank < prevRank)
                throw new InvalidOperationException("[PrebuiltPuzzleLibrary] Difficulty is not non-decreasing across puzzles.");

            if (currentDifficulty != meta.Difficulty)
            {
                currentDifficulty = meta.Difficulty;
                prevGivens = null;
            }
            else if (prevGivens.HasValue && meta.GivensCount > prevGivens.Value)
            {
                throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Givens should not increase within difficulty {meta.Difficulty} (puzzle {meta.Id}).");
            }

            prevRank = rank;
            prevGivens = meta.GivensCount;
        }
    }

    private static int GetDifficultyRank(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 0,
        Difficulty.Medium => 1,
        Difficulty.Hard => 2,
        Difficulty.Insane => 3,
        _ => 4
    };

    private static int ExtractIndex(string id)
    {
        var parts = id.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[1], out int idx))
            return idx;
        return 0;
    }

    private static PrebuiltPuzzle BuildPuzzle(PuzzleMeta meta)
    {
        return CreatePuzzle(meta.Id, meta.Difficulty, meta.SolutionStr, meta.PuzzleStr);
    }

    private static string ReadAllText(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException($"[PrebuiltPuzzleLibrary] Unable to open puzzle file: {path}");

        return file.GetAsText();
    }

    private static bool HasUniqueSolution(string givensStr)
    {
        var grid = new int[9, 9];
        for (int i = 0; i < 81; i++)
        {
            char ch = givensStr[i];
            if (ch != '.')
            {
                int r = i / 9;
                int c = i % 9;
                grid[r, c] = ch - '0';
            }
        }

        // CountSolutions stops early at 2, so 1 means unique.
        return SudokuSolver.CountSolutions(grid, maxSolutions: 2) == 1;
    }

    private static bool IsValidSolutionString(string solution)
    {
        // Verify each row, column, and 3x3 box contains digits 1..9 exactly once.
        int[] rowMask = new int[9];
        int[] colMask = new int[9];
        int[] boxMask = new int[9];

        for (int i = 0; i < 81; i++)
        {
            int r = i / 9;
            int c = i % 9;
            int box = (r / 3) * 3 + (c / 3);
            int digit = solution[i] - '0';

            if (digit < 1 || digit > 9)
                return false;

            int bit = 1 << digit;

            if ((rowMask[r] & bit) != 0) return false;
            if ((colMask[c] & bit) != 0) return false;
            if ((boxMask[box] & bit) != 0) return false;

            rowMask[r] |= bit;
            colMask[c] |= bit;
            boxMask[box] |= bit;
        }

        // Ensure all masks are complete (digits 1..9 set)
        const int full = 0b_1111111110; // bits 1-9 set
        return rowMask.All(m => m == full) && colMask.All(m => m == full) && boxMask.All(m => m == full);
    }

    /// <summary>
    /// Helper to create a puzzle from solution/givens strings (81 chars each, '.' = empty).
    /// </summary>
    private static PrebuiltPuzzle CreatePuzzle(string id, Difficulty difficulty, string solutionStr, string givensStr)
    {
        var solution = new int[9, 9];
        var givens = new bool[9, 9];

        for (int i = 0; i < 81; i++)
        {
            int r = i / 9;
            int c = i % 9;
            solution[r, c] = solutionStr[i] - '0';
            givens[r, c] = givensStr[i] != '.';
        }

        return new PrebuiltPuzzle(id, difficulty, solution, givens);
    }
}
