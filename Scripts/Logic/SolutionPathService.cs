using System.Linq;
using SudokuSen.Models;

namespace SudokuSen.Logic;

/// <summary>
/// Builds a HoDoKu-style logical solution path using existing hint techniques.
/// </summary>
public static class SolutionPathService
{
    public enum PathStatus
    {
        Solved,
        Stuck,
        HitStepLimit,
        RepeatedElimination
    }

    public record SolutionPathStep
    (
        int Index,
        string TechniqueId,
        string TechniqueName,
        bool IsPlacement,
        int Row,
        int Col,
        int Value,
        List<int> EliminatedCandidates,
        List<(int row, int col)> RelatedCells,
        string Explanation,
        int[,] GridSnapshot
    );

    public record SolutionPath(PathStatus Status, List<SolutionPathStep> Steps, string Message);

    /// <summary>
    /// Attempts to solve the given puzzle logically, recording each applied hint as a step.
    /// </summary>
    public static SolutionPath BuildPath(SudokuGameState originalState, int maxSteps = 512)
    {
        var workState = originalState.Clone();
        SeedNotesWithAllCandidates(workState);
        var steps = new List<SolutionPathStep>(maxSteps);

        // Track eliminations to avoid repeating the exact same move without progress.
        var seenEliminations = new HashSet<string>();

        for (int idx = 1; idx <= maxSteps; idx++)
        {
            var hint = HintService.FindHint(workState, respectNotes: true);
            if (hint is null)
            {
                return new SolutionPath(
                    workState.IsComplete() ? PathStatus.Solved : PathStatus.Stuck,
                    steps,
                    workState.IsComplete() ? "Solved" : "No further logical hint found"
                );
            }

            // Skip duplicate eliminations to avoid infinite loops when no placement is made.
            if (!hint.IsPlacement)
            {
                string elimKey = $"r{hint.Row}c{hint.Col}:{string.Join(',', hint.EliminatedCandidates.OrderBy(v => v))}";
                if (!seenEliminations.Add(elimKey))
                {
                    return new SolutionPath(PathStatus.RepeatedElimination, steps, "Repeated elimination without progress");
                }
            }

            var candidates = HintService.CalculateAllCandidates(workState, respectNotes: true);
            var verbose = BuildVerboseExplanation(workState, hint, candidates);
            if (!string.IsNullOrEmpty(verbose))
            {
                hint = hint with { Explanation = verbose };
            }

            ApplyHint(workState, hint);

            var snapshot = SnapshotGrid(workState);
            steps.Add(new SolutionPathStep(
                Index: idx,
                TechniqueId: hint.TechniqueId,
                TechniqueName: hint.TechniqueName,
                IsPlacement: hint.IsPlacement,
                Row: hint.Row,
                Col: hint.Col,
                Value: hint.Value,
                EliminatedCandidates: hint.EliminatedCandidates,
                RelatedCells: hint.RelatedCells,
                Explanation: hint.Explanation,
                GridSnapshot: snapshot
            ));

            if (workState.IsComplete())
            {
                return new SolutionPath(PathStatus.Solved, steps, "Solved");
            }
        }

        return new SolutionPath(PathStatus.HitStepLimit, steps, $"Stopped after {maxSteps} steps");
    }

    /// <summary>
    /// Build multiple solution paths that differ by the first applied hint (distinct start cell).
    /// Only the earliest-available technique is enumerated; subsequent steps follow normal logic.
    /// </summary>
    public static List<SolutionPath> BuildPathsWithDifferentStarts(SudokuGameState originalState, int maxSteps = 512)
    {
        var hints = HintService.FindAllFirstHints(originalState, respectNotes: true);
        if (hints.Count == 0)
            return new List<SolutionPath> { BuildPath(originalState, maxSteps) };

        var paths = new List<SolutionPath>(hints.Count);
        foreach (var hint in hints)
        {
            paths.Add(BuildPathStartingWithHint(originalState, hint, maxSteps));
        }
        return paths;
    }

    /// <summary>
    /// Build a path but force a specific first hint, then continue with normal logic.
    /// </summary>
    public static SolutionPath BuildPathStartingWithHint(SudokuGameState originalState, HintService.Hint forcedHint, int maxSteps = 512)
    {
        var workState = originalState.Clone();
        SeedNotesWithAllCandidates(workState);
        var steps = new List<SolutionPathStep>(maxSteps);

        {
            var candidates = HintService.CalculateAllCandidates(workState, respectNotes: true);
            var verbose = BuildVerboseExplanation(workState, forcedHint, candidates);
            if (!string.IsNullOrEmpty(verbose))
            {
                forcedHint = forcedHint with { Explanation = verbose };
            }
        }

        ApplyHint(workState, forcedHint);
        var firstSnapshot = SnapshotGrid(workState);
        steps.Add(new SolutionPathStep(
            Index: 1,
            TechniqueId: forcedHint.TechniqueId,
            TechniqueName: forcedHint.TechniqueName,
            IsPlacement: forcedHint.IsPlacement,
            Row: forcedHint.Row,
            Col: forcedHint.Col,
            Value: forcedHint.Value,
            EliminatedCandidates: forcedHint.EliminatedCandidates,
            RelatedCells: forcedHint.RelatedCells,
            Explanation: forcedHint.Explanation,
            GridSnapshot: firstSnapshot
        ));

        var seenEliminations = new HashSet<string>();
        if (!forcedHint.IsPlacement)
        {
            string elimKey = $"r{forcedHint.Row}c{forcedHint.Col}:{string.Join(',', forcedHint.EliminatedCandidates.OrderBy(v => v))}";
            seenEliminations.Add(elimKey);
        }

        for (int idx = 2; idx <= maxSteps; idx++)
        {
            var hint = HintService.FindHint(workState, respectNotes: true);
            if (hint is null)
            {
                return new SolutionPath(
                    workState.IsComplete() ? PathStatus.Solved : PathStatus.Stuck,
                    steps,
                    workState.IsComplete() ? "Solved" : "No further logical hint found"
                );
            }

            if (!hint.IsPlacement)
            {
                string elimKey = $"r{hint.Row}c{hint.Col}:{string.Join(',', hint.EliminatedCandidates.OrderBy(v => v))}";
                if (!seenEliminations.Add(elimKey))
                {
                    return new SolutionPath(PathStatus.RepeatedElimination, steps, "Repeated elimination without progress");
                }
            }

            var candidates = HintService.CalculateAllCandidates(workState, respectNotes: true);
            var verbose = BuildVerboseExplanation(workState, hint, candidates);
            if (!string.IsNullOrEmpty(verbose))
            {
                hint = hint with { Explanation = verbose };
            }

            ApplyHint(workState, hint);

            var snapshot = SnapshotGrid(workState);
            steps.Add(new SolutionPathStep(
                Index: idx,
                TechniqueId: hint.TechniqueId,
                TechniqueName: hint.TechniqueName,
                IsPlacement: hint.IsPlacement,
                Row: hint.Row,
                Col: hint.Col,
                Value: hint.Value,
                EliminatedCandidates: hint.EliminatedCandidates,
                RelatedCells: hint.RelatedCells,
                Explanation: hint.Explanation,
                GridSnapshot: snapshot
            ));

            if (workState.IsComplete())
            {
                return new SolutionPath(PathStatus.Solved, steps, "Solved");
            }
        }

        return new SolutionPath(PathStatus.HitStepLimit, steps, $"Stopped after {maxSteps} steps");
    }

    private static void ApplyHint(SudokuGameState state, HintService.Hint hint)
    {
        var cell = state.Grid[hint.Row, hint.Col];

        if (hint.IsPlacement)
        {
            cell.Value = hint.Value;
            cell.IsGiven = cell.IsGiven; // preserve given flag
            // Clear notes on placement
            for (int i = 0; i < cell.Notes.Length; i++)
                cell.Notes[i] = false;
        }
        else
        {
            foreach (var cand in hint.EliminatedCandidates)
            {
                if (cand >= 1 && cand <= 9)
                    cell.Notes[cand - 1] = false;
            }
        }
    }

    private static void SeedNotesWithAllCandidates(SudokuGameState state)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var cell = state.Grid[r, c];

                // Clear notes for solved cells and givens
                if (cell.Value != 0)
                {
                    for (int i = 0; i < cell.Notes.Length; i++)
                        cell.Notes[i] = false;
                    continue;
                }

                for (int i = 0; i < cell.Notes.Length; i++)
                    cell.Notes[i] = false;

                // Mark all legally placeable numbers as allowed notes
                for (int num = 1; num <= 9; num++)
                {
                    if (state.IsValidPlacement(r, c, num))
                    {
                        cell.Notes[num - 1] = true;
                    }
                }
            }
        }
    }

    private static int[,] SnapshotGrid(SudokuGameState state)
    {
        int[,] snap = new int[9, 9];
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                snap[r, c] = state.Grid[r, c].Value;
            }
        }
        return snap;
    }

    private static string BuildVerboseExplanation(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        try
        {
            return hint.TechniqueId switch
            {
                "NakedSingle" or "ForcedSingle" => ExplainNakedSingle(state, hint, candidates),
                "HiddenSingleRow" or "HiddenSingleCol" or "HiddenSingleBlock" => ExplainHiddenSingle(state, hint, candidates),
                "NakedPair" or "NakedTriple" or "NakedQuad" => ExplainNakedSet(state, hint, candidates),
                "HiddenPair" or "HiddenTriple" => ExplainHiddenSet(state, hint, candidates),
                "PointingPair" => ExplainPointing(state, hint, candidates),
                "BoxLineReduction" => ExplainBoxLine(state, hint, candidates),
                "XWing" or "Swordfish" or "Jellyfish" => ExplainFish(state, hint, candidates),
                "XYWing" => ExplainXYWing(state, hint),
                "XYZWing" => ExplainXYZWing(state, hint),
                "BUGPlus1" => ExplainBugPlusOne(state, hint, candidates),
                "UniqueRectangle" => ExplainUniqueRectangle(state, hint),
                "RemotePair" => ExplainRemotePair(state, hint),
                "FinnedXWing" or "FinnedSwordfish" => ExplainFinnedFish(state, hint),
                "ALSXZRule" => ExplainAlsXz(state, hint),
                "ForcingChain" => ExplainForcingChain(state, hint),
                _ => ExplainGeneric(state, hint, candidates)
            };
        }
        catch
        {
            // Fallback to existing explanation if anything unexpected happens
            return hint.Explanation;
        }
    }

    private static string ExplainNakedSingle(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        int r = hint.Row;
        int c = hint.Col;
        int v = hint.Value;
        string cellRef = ToCellRef(r, c);

        var cellCands = candidates[r, c].ToList();
        cellCands.Sort();

        var rowHits = new List<string>();
        for (int col = 0; col < state.GridSize; col++)
        {
            if (col == c) continue;
            if (state.Grid[r, col].Value == v) rowHits.Add(ToCellRef(r, col));
        }

        var colHits = new List<string>();
        for (int row = 0; row < state.GridSize; row++)
        {
            if (row == r) continue;
            if (state.Grid[row, c].Value == v) colHits.Add(ToCellRef(row, c));
        }

        var boxHits = new List<string>();
        int br = (r / 3) * 3;
        int bc = (c / 3) * 3;
        for (int row = br; row < br + 3; row++)
        {
            for (int col = bc; col < bc + 3; col++)
            {
                if (row == r && col == c) continue;
                if (state.Grid[row, col].Value == v) boxHits.Add(ToCellRef(row, col));
            }
        }

        string rowPart = rowHits.Count > 0
            ? $"Row {r + 1} already has {v} at {string.Join(", ", rowHits)}, so {v} is removed from other cells in the row."
            : $"Row {r + 1} has no other {v}.";
        string colPart = colHits.Count > 0
            ? $"Column {c + 1} already has {v} at {string.Join(", ", colHits)}, so {v} is removed from other cells in the column."
            : $"Column {c + 1} has no other {v}.";
        string boxPart = boxHits.Count > 0
            ? $"The block already has {v} at {string.Join(", ", boxHits)}, so {v} is removed from other cells in the block."
            : $"The block has no other {v}.";

        return $"Naked Single at {cellRef}: start with candidates [{string.Join(", ", cellCands)}]."
             + $" {rowPart}"
             + $" {colPart}"
             + $" {boxPart}"
             + $" After these eliminations only {v} remains for {cellRef}, so place {v}.";
    }

    private static string ExplainHiddenSingle(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        int r = hint.Row;
        int c = hint.Col;
        int v = hint.Value;
        string cellRef = ToCellRef(r, c);
        var cellCands = candidates[r, c].ToList(); cellCands.Sort();

        if (hint.TechniqueId == "HiddenSingleRow")
        {
            var positions = new List<string>();
            for (int col = 0; col < state.GridSize; col++)
            {
                if (state.Grid[r, col].Value != 0) continue;
                if (candidates[r, col].Contains(v)) positions.Add(ToCellRef(r, col));
            }
            return $"Hidden Single (row {r + 1}): start candidates at {cellRef}: [{string.Join(", ", cellCands)}]. Candidate {v} appears only at {string.Join(", ", positions)}, so all other row cells lack {v}. Therefore {cellRef} = {v}.";
        }

        if (hint.TechniqueId == "HiddenSingleCol")
        {
            var positions = new List<string>();
            for (int row = 0; row < state.GridSize; row++)
            {
                if (state.Grid[row, c].Value != 0) continue;
                if (candidates[row, c].Contains(v)) positions.Add(ToCellRef(row, c));
            }
            return $"Hidden Single (column {c + 1}): start candidates at {cellRef}: [{string.Join(", ", cellCands)}]. Candidate {v} appears only at {string.Join(", ", positions)}, so all other column cells lack {v}. Therefore {cellRef} = {v}.";
        }

        int br = (r / 3) * 3;
        int bc = (c / 3) * 3;
        var blockPositions = new List<string>();
        for (int row = br; row < br + 3; row++)
        {
            for (int col = bc; col < bc + 3; col++)
            {
                if (state.Grid[row, col].Value != 0) continue;
                if (candidates[row, col].Contains(v)) blockPositions.Add(ToCellRef(row, col));
            }
        }
        return $"Hidden Single (block {br / 3 + 1},{bc / 3 + 1}): start candidates at {cellRef}: [{string.Join(", ", cellCands)}]. Candidate {v} appears only at {string.Join(", ", blockPositions)}, so all other block cells lack {v}. Therefore {cellRef} = {v}.";
    }

    private static string ExplainNakedPair(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pairCells = FormatCells(hint.RelatedCells);
        string lockedDigits = DigitsForCells(hint.RelatedCells, candidates);
        var cellCands = candidates[hint.Row, hint.Col].ToList();
        cellCands.Sort();

        return $"{hint.TechniqueName}: cells {pairCells} hold only [{lockedDigits}] in this house, so those digits clear from every other cell here. After those clears, {cellRef} has [{string.Join(", ", cellCands)}] and is forced to {hint.Value}.";
    }

    private static string ExplainNakedSet(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string members = FormatCells(hint.RelatedCells);
        string lockedDigits = DigitsForCells(hint.RelatedCells, candidates);
        var cellCands = candidates[hint.Row, hint.Col].ToList();
        cellCands.Sort();

        return $"{hint.TechniqueName}: cells {members} are limited to [{lockedDigits}] in this house, so those digits drop from every other cell here. {cellRef} then has [{string.Join(", ", cellCands)}] and must be {hint.Value}.";
    }

    private static string ExplainHiddenSet(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string members = FormatCells(hint.RelatedCells);
        string lockedDigits = DigitsForCells(hint.RelatedCells, candidates);
        var cellCands = candidates[hint.Row, hint.Col].ToList();
        cellCands.Sort();

        return $"{hint.TechniqueName}: only {members} can take digits [{lockedDigits}] in this house, so every other candidate in those cells is removed. {cellRef} shrinks to [{string.Join(", ", cellCands)}] ⇒ {hint.Value}.";
    }

    private static string ExplainPointing(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        var startCands = candidates[hint.Row, hint.Col].ToList(); startCands.Sort();
        if (hint.IsPlacement)
        {
            var remaining = RemoveDigits(startCands, hint.EliminatedCandidates);
            return $"Pointing: inside one block the digit is confined to {pattern}, so the same digit clears from the rest of that line. After those clears, {cellRef} has [{string.Join(", ", remaining)}] ⇒ {hint.Value}.";
        }
        var after = RemoveDigits(startCands, hint.EliminatedCandidates);
        return $"Pointing: the digit is confined to {pattern} inside the block, so eliminate {elim} from {cellRef} along the shared line (candidates now [{string.Join(", ", after)}]).";
    }

    private static string ExplainBoxLine(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        var startCands = candidates[hint.Row, hint.Col].ToList(); startCands.Sort();
        if (hint.IsPlacement)
        {
            var remaining = RemoveDigits(startCands, hint.EliminatedCandidates);
            return $"Box/Line: all line occurrences sit in one block ({pattern}), so other cells in that block drop the digit. After those drops, {cellRef} has [{string.Join(", ", remaining)}] ⇒ {hint.Value}.";
        }
        var after = RemoveDigits(startCands, hint.EliminatedCandidates);
        return $"Box/Line: the line's candidates sit in one block ({pattern}); remove {elim} from {cellRef} inside that block (candidates now [{string.Join(", ", after)}]).";
    }

    private static string ExplainFish(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        var startCands = candidates[hint.Row, hint.Col].ToList(); startCands.Sort();
        if (hint.IsPlacement)
        {
            var remaining = RemoveDigits(startCands, hint.EliminatedCandidates);
            return $"{hint.TechniqueName}: base and cover lines at {pattern} trap the digit at their intersections. Clearing it from cover lines leaves {cellRef} with [{string.Join(", ", remaining)}] ⇒ {hint.Value}.";
        }
        var after = RemoveDigits(startCands, hint.EliminatedCandidates);
        return $"{hint.TechniqueName}: intersections at {pattern} define the fish; eliminate {elim} from {cellRef} on the cover lines (candidates now [{string.Join(", ", after)}]).";
    }

    private static string ExplainXYWing(SudokuGameState state, HintService.Hint hint)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        if (hint.IsPlacement)
        {
            return $"XY-Wing: pivot and pincers ({pattern}) each see {cellRef}; their shared value is removed there, leaving one candidate ⇒ {hint.Value}.";
        }
        return $"XY-Wing: pivot and pincers ({pattern}) share one value; remove {elim} from {cellRef}.";
    }

    private static string ExplainXYZWing(SudokuGameState state, HintService.Hint hint)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        if (hint.IsPlacement)
        {
            return $"XYZ-Wing: pivot plus two bi-value cells ({pattern}) hold all three digits; only {cellRef} can take {hint.Value}.";
        }
        return $"XYZ-Wing: all three digits appear in {pattern}; eliminate {elim} from {cellRef}.";
    }

    private static string ExplainBugPlusOne(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        var startCands = candidates[hint.Row, hint.Col].OrderBy(x => x).ToList();
        return $"BUG+1: every other unsolved cell is bi-value; {cellRef} alone has an extra candidate among [{string.Join(", ", startCands)}]. That forces {cellRef} = {hint.Value}.";
    }

    private static string ExplainUniqueRectangle(SudokuGameState state, HintService.Hint hint)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        if (hint.IsPlacement)
        {
            return $"Unique Rectangle: to avoid the deadly rectangle {pattern}, {cellRef} must be {hint.Value}.";
        }
        return $"Unique Rectangle: rectangle {pattern} would be deadly; eliminate {elim} from {cellRef}.";
    }

    private static string ExplainRemotePair(SudokuGameState state, HintService.Hint hint)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        return $"Remote Pair: an alternating chain on {pattern} forces the same digit along the links. Since {cellRef} sees two links, remove {elim}.";
    }

    private static string ExplainFinnedFish(SudokuGameState state, HintService.Hint hint)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        return $"{hint.TechniqueName}: the fin with its base pattern at {pattern} restricts the digit; eliminate {elim} from {cellRef}.";
    }

    private static string ExplainAlsXz(SudokuGameState state, HintService.Hint hint)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string pattern = FormatCells(hint.RelatedCells);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        return $"ALS-XZ: two almost-locked sets share restricted candidates at {pattern}; therefore remove {elim} from {cellRef}.";
    }

    private static string ExplainForcingChain(SudokuGameState state, HintService.Hint hint)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string elim = FormatDigitList(hint.EliminatedCandidates);
        if (hint.IsPlacement)
        {
            return $"Forcing Chain: following each assumption in the chain ends at {cellRef} = {hint.Value}; every alternate value contradicts.";
        }
        return $"Forcing Chain: every branch of the chain forbids {elim} at {cellRef}.";
    }

    private static string ExplainGeneric(SudokuGameState state, HintService.Hint hint, HashSet<int>[,] candidates)
    {
        string cellRef = ToCellRef(hint.Row, hint.Col);
        string related = FormatCells(hint.RelatedCells);
        var cellCands = candidates[hint.Row, hint.Col].ToList();
        cellCands.Sort();
        if (hint.IsPlacement)
        {
            return $"{hint.TechniqueName}: set {cellRef} to {hint.Value}. Candidates were [{string.Join(", ", cellCands)}]. Pattern cells: {related}. {hint.Explanation}";
        }
        var after = RemoveDigits(cellCands, hint.EliminatedCandidates);
        return $"{hint.TechniqueName}: eliminate {FormatDigitList(hint.EliminatedCandidates)} from {cellRef} (candidates now [{string.Join(", ", after)}]). Pattern cells: {related}. {hint.Explanation}";
    }

    private static string FormatCells(List<(int row, int col)>? cells)
    {
        if (cells == null || cells.Count == 0) return "(none)";
        return string.Join(", ", cells.Select(rc => ToCellRef(rc.row, rc.col)));
    }

    private static string DigitsForCells(List<(int row, int col)>? cells, HashSet<int>[,] candidates)
    {
        if (cells == null || cells.Count == 0) return "(none)";
        var set = new HashSet<int>();
        foreach (var rc in cells)
        {
            set.UnionWith(candidates[rc.row, rc.col]);
        }
        return FormatDigitList(set);
    }

    private static string FormatDigitList(IEnumerable<int> digits)
    {
        var list = digits.Distinct().OrderBy(x => x).ToList();
        return list.Count == 0 ? "(none)" : string.Join(", ", list);
    }

    private static List<int> RemoveDigits(IEnumerable<int> start, IEnumerable<int> toRemove)
    {
        var set = new HashSet<int>(start);
        foreach (var d in toRemove) set.Remove(d);
        return set.OrderBy(x => x).ToList();
    }

    private static string ToCellRef(int row, int col)
    {
        char colChar = (char)('A' + col);
        return $"{colChar}{row + 1}";
    }
}
