using SudokuSen.Services;

namespace SudokuSen.Logic;

/// <summary>
/// Service für Sudoku-Hinweise mit verschiedenen Techniken
/// </summary>
public static class HintService
{
    /// <summary>
    /// Repräsentiert einen Hinweis für eine Zelle
    /// </summary>
    public record Hint
    {
        public int Row { get; init; }
        public int Col { get; init; }
        public int Value { get; init; }
        public bool IsPlacement { get; init; } = true;
        public List<int> EliminatedCandidates { get; init; } = new();
        public string TechniqueId { get; init; } = "";
        public string TechniqueName { get; init; } = "";
        public string TechniqueDescription { get; init; } = "";
        public List<(int row, int col)> RelatedCells { get; init; } = new();
        public string Explanation { get; init; } = "";
    }

    /// <summary>
    /// Gets the single element from a HashSet that has exactly one element.
    /// Avoids LINQ .First() allocation. Caller must ensure Count == 1.
    /// </summary>
    private static int GetSingleElement(HashSet<int> set)
    {
        using var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        return enumerator.Current;
    }

    /// <summary>
    /// Checks if all positions share the same row. Avoids LINQ closure allocation.
    /// </summary>
    private static bool AllSameRow(List<(int row, int col)> positions)
    {
        if (positions.Count == 0) return true;
        int firstRow = positions[0].row;
        for (int i = 1; i < positions.Count; i++)
        {
            if (positions[i].row != firstRow) return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if all positions share the same column. Avoids LINQ closure allocation.
    /// </summary>
    private static bool AllSameCol(List<(int row, int col)> positions)
    {
        if (positions.Count == 0) return true;
        int firstCol = positions[0].col;
        for (int i = 1; i < positions.Count; i++)
        {
            if (positions[i].col != firstCol) return false;
        }
        return true;
    }

    /// <summary>
    /// Findet einen Hinweis für das aktuelle Spielfeld
    /// </summary>
    public static Hint? FindHint(SudokuGameState gameState, bool respectNotes = false)
    {
        // Hints are only supported for 9x9 grids
        if (gameState.GridSize != 9)
        {
            GD.Print($"[HintService] Hints not supported for {gameState.GridSize}x{gameState.GridSize} grids");
            return null;
        }

        // Versuche verschiedene Techniken in aufsteigender Schwierigkeit

        // 1. Naked Single - Eine Zelle hat nur eine mögliche Zahl
        var nakedSingle = FindNakedSingle(gameState, respectNotes);
        if (nakedSingle != null) return nakedSingle;

        // 2. Hidden Single - Eine Zahl kann nur an einer Stelle in Zeile/Spalte/Block
        var hiddenSingle = FindHiddenSingle(gameState, respectNotes);
        if (hiddenSingle != null) return hiddenSingle;

        // Calculate candidates once for all advanced techniques
        var candidates = CalculateAllCandidates(gameState, respectNotes);

        // 3. Naked Pair
        var nakedPair = FindNakedPair(gameState, candidates);
        if (nakedPair != null) return nakedPair;

        // 4. Pointing Pair
        var pointingPair = FindPointingPair(gameState, candidates);
        if (pointingPair != null) return pointingPair;

        // 5. Box/Line Reduction
        var boxLine = FindBoxLineReduction(gameState, candidates);
        if (boxLine != null) return boxLine;

        // 6. X-Wing
        var xwing = FindXWing(gameState, candidates);
        if (xwing != null) return xwing;

        // 7. BUG+1 (Insane)
        var bugPlus1 = FindBugPlus1(gameState, candidates);
        if (bugPlus1 != null) return bugPlus1;

        // 8. Unique Rectangle (Insane)
        var uniqueRectangle = FindUniqueRectangle(gameState, candidates);
        if (uniqueRectangle != null) return uniqueRectangle;

        // 9. Remote Pair (Insane)
        var remotePair = FindRemotePair(gameState, candidates);
        if (remotePair != null) return remotePair;

        // 10. Finned X-Wing (Insane)
        var finnedXWing = FindFinnedXWing(gameState, candidates);
        if (finnedXWing != null) return finnedXWing;

        // 11. Finned Swordfish (Insane)
        var finnedSwordfish = FindFinnedSwordfish(gameState, candidates);
        if (finnedSwordfish != null) return finnedSwordfish;

        // 12. ALS-XZ Rule (Insane)
        var alsXz = FindAlsXzRule(gameState, candidates);
        if (alsXz != null) return alsXz;

        // 13. Forcing Chain (Insane): Try candidates and prove contradictions
        var forcingChain = FindForcingChain(gameState, candidates);
        if (forcingChain != null) return forcingChain;

        // Fallback: Finde einfach die nächste lösbare Zelle
        return FindSimpleHint(gameState);
    }

    private static string L(string german, string english)
    {
        var loc = LocalizationService.Instance;
        return loc != null && loc.CurrentLanguage == Language.German ? german : english;
    }

    /// <summary>
    /// Converts row/col (0-indexed) to cell reference like "A1", "B2", etc.
    /// </summary>
    private static string ToCellRef(int row, int col)
    {
        char colChar = (char)('A' + Math.Clamp(col, 0, 8));
        return $"{colChar}{row + 1}";
    }

    private static Hint? FindForcingChain(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        var loc = LocalizationService.Instance;

        // Build base grid from current values
        var baseGrid = new int[9, 9];
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                baseGrid[r, c] = gameState.Grid[r, c].Value;
            }
        }

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (baseGrid[row, col] != 0) continue;

                var cellCands = candidates[row, col];
                if (cellCands.Count < 2) continue;

                int solvableCount = 0;
                int forcedValue = 0;
                var contradictions = new List<int>();

                foreach (int cand in cellCands)
                {
                    var gridCopy = SudokuSolver.CopyGrid(baseGrid);
                    gridCopy[row, col] = cand;

                    // If no solution exists under this assumption, it's a contradiction.
                    int solutions = SudokuSolver.CountSolutions(gridCopy, maxSolutions: 1);
                    if (solutions > 0)
                    {
                        solvableCount++;
                        forcedValue = cand;
                        if (solvableCount > 1)
                        {
                            // Not forced (yet) - early out for this cell.
                            break;
                        }
                    }
                    else
                    {
                        contradictions.Add(cand);
                    }
                }

                if (solvableCount == 1)
                {
                    string cellLabel = $"R{row + 1}C{col + 1}";
                    string contradictionList = contradictions.Count > 0 ? string.Join(", ", contradictions) : "";

                    string explanation = contradictions.Count > 0
                        ? L(
                            $"In {cellLabel} sind mehrere Kandidaten möglich. Wenn man {contradictionList} annimmt, führt das zu einem Widerspruch. Daher muss hier {forcedValue} stehen.",
                            $"In {cellLabel}, multiple candidates are possible. Assuming {contradictionList} leads to a contradiction, so this cell must be {forcedValue}."
                        )
                        : L(
                            $"In {cellLabel} ist nur ein Kandidat mit einer gültigen Fortsetzung vereinbar. Daher muss hier {forcedValue} stehen.",
                            $"In {cellLabel}, only one candidate is compatible with a valid continuation, so this cell must be {forcedValue}."
                        );

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = forcedValue,
                        IsPlacement = true,
                        TechniqueId = "ForcingChain",
                        TechniqueName = loc != null ? loc.GetTechniqueName("ForcingChain") : "Forcing Chain",
                        TechniqueDescription = loc != null ? loc.GetTechniqueDescription("ForcingChain") : "",
                        RelatedCells = GetAllRelatedCellsWithValue(gameState, row, col),
                        Explanation = explanation
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Collect all hints for the first-available technique (so we can branch different starting cells).
    /// Prioritizes truly forced starts: forced singles (naked+unique), then hidden singles, then naked singles.
    /// </summary>
    public static List<Hint> FindAllFirstHints(SudokuGameState gameState, bool respectNotes = false)
    {
        var results = new List<Hint>();

        if (gameState.GridSize != 9)
            return results;

        // 1) Forced singles (naked single AND unique position for that value in its row/col/box)
        CollectForcedSingles(gameState, results, respectNotes);
        if (results.Count > 0) return results;

        // 2) Hidden singles (unique position in row/col/box for a value)
        CollectHiddenSingles(gameState, results, respectNotes);
        if (results.Count > 0) return results;

        // 3) Naked singles (only one candidate in cell) as last resort
        CollectNakedSingles(gameState, results, respectNotes);
        if (results.Count > 0) return results;

        // 4) Difficulty-aware techniques to enumerate more distinct starts
        var candidates = CalculateAllCandidates(gameState, respectNotes);

        // Medium and above: Naked Pair, Pointing Pair
        if (gameState.Difficulty >= Difficulty.Medium)
        {
            CollectNakedPairs(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectPointingPairs(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectHiddenPairs(gameState, candidates, results);
            if (results.Count > 0) return results;

            CollectHiddenTriples(gameState, candidates, results);
            if (results.Count > 0) return results;

            CollectNakedTriples(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectNakedQuads(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;
        }

        // Hard and above: Box/Line Reduction, X-Wing
        if (gameState.Difficulty >= Difficulty.Hard)
        {
            CollectBoxLineReductions(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectXWings(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectSwordfish(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectJellyfish(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectXYWings(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;

            CollectXYZWings(gameState, candidates, results, allowElimination: false);
            if (results.Count > 0) return results;
        }

        // Insane: try all remaining advanced techniques to enumerate possible starts
        if (gameState.Difficulty >= Difficulty.Insane)
        {
            var advancedHint = FindBugPlus1(gameState, candidates);
            if (advancedHint != null) { results.Add(advancedHint); return results; }

            advancedHint = FindUniqueRectangle(gameState, candidates);
            if (advancedHint != null) { results.Add(advancedHint); return results; }

            advancedHint = FindRemotePair(gameState, candidates);
            if (advancedHint != null) { results.Add(advancedHint); return results; }

            advancedHint = FindFinnedXWing(gameState, candidates);
            if (advancedHint != null) { results.Add(advancedHint); return results; }

            advancedHint = FindFinnedSwordfish(gameState, candidates);
            if (advancedHint != null) { results.Add(advancedHint); return results; }

            advancedHint = FindAlsXzRule(gameState, candidates);
            if (advancedHint != null) { results.Add(advancedHint); return results; }

            advancedHint = FindForcingChain(gameState, candidates);
            if (advancedHint != null) { results.Add(advancedHint); return results; }
        }

        // If no placement was found, allow elimination starts (hybrid mode)
        if (results.Count == 0)
        {
            // Medium and above
            if (gameState.Difficulty >= Difficulty.Medium)
            {
                CollectNakedPairs(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectPointingPairs(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectNakedTriples(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectNakedQuads(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;
            }

            if (gameState.Difficulty >= Difficulty.Hard)
            {
                CollectBoxLineReductions(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectXWings(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectSwordfish(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectJellyfish(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectXYWings(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;

                CollectXYZWings(gameState, candidates, results, allowElimination: true);
                if (results.Count > 0) return results;
            }
        }

        // Fallback: single best hint
        var single = FindHint(gameState, respectNotes);
        if (single != null)
            results.Add(single);
        return results;
    }

    private static bool Sees(int row1, int col1, int row2, int col2)
    {
        if (row1 == row2) return true;
        if (col1 == col2) return true;
        return row1 / 3 == row2 / 3 && col1 / 3 == col2 / 3;
    }

    private static int[,] BuildValueGrid(SudokuGameState gameState)
    {
        var grid = new int[9, 9];
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                grid[r, c] = gameState.Grid[r, c].Value;
            }
        }
        return grid;
    }

    private static Hint? FindBugPlus1(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        var loc = LocalizationService.Instance;

        int bugRow = -1;
        int bugCol = -1;
        int bugCount = 0;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (gameState.Grid[r, c].Value != 0) continue;

                int candCount = candidates[r, c].Count;
                if (candCount == 2) continue;
                if (candCount < 2) return null; // invalid state (no candidates)

                bugCount++;
                bugRow = r;
                bugCol = c;
                if (bugCount > 1) return null;
            }
        }

        if (bugCount != 1) return null;

        // For each candidate in the BUG cell, check if it breaks the even-candidate distribution in row/col/block.
        foreach (int cand in candidates[bugRow, bugCol])
        {
            bool isExtra = false;

            int rowOcc = 0;
            for (int c = 0; c < 9; c++)
            {
                if (candidates[bugRow, c].Contains(cand)) rowOcc++;
            }
            if (rowOcc % 2 == 1) isExtra = true;

            int colOcc = 0;
            for (int r = 0; r < 9; r++)
            {
                if (candidates[r, bugCol].Contains(cand)) colOcc++;
            }
            if (colOcc % 2 == 1) isExtra = true;

            int startRow = (bugRow / 3) * 3;
            int startCol = (bugCol / 3) * 3;
            int blockOcc = 0;
            for (int r = startRow; r < startRow + 3; r++)
            {
                for (int c = startCol; c < startCol + 3; c++)
                {
                    if (candidates[r, c].Contains(cand)) blockOcc++;
                }
            }
            if (blockOcc % 2 == 1) isExtra = true;

            if (isExtra)
            {
                string cellLabel = $"R{bugRow + 1}C{bugCol + 1}";
                return new Hint
                {
                    Row = bugRow,
                    Col = bugCol,
                    Value = cand,
                    IsPlacement = true,
                    TechniqueId = "BUGPlus1",
                    TechniqueName = loc != null ? loc.GetTechniqueName("BUGPlus1") : "BUG+1",
                    TechniqueDescription = loc != null ? loc.GetTechniqueDescription("BUGPlus1") : "",
                    RelatedCells = GetAllRelatedCellsWithValue(gameState, bugRow, bugCol),
                    Explanation = L(
                        $"BUG+1: Fast alle offenen Zellen sind bivalue (2 Kandidaten). In {cellLabel} gibt es einen Extra-Kandidaten. Dieser Extra-Kandidat ist {cand}, daher muss hier {cand} stehen.",
                        $"BUG+1: Almost all unsolved cells are bi-value (2 candidates). In {cellLabel} there is one extra candidate. That extra candidate is {cand}, so this cell must be {cand}."
                    )
                };
            }
        }

        return null;
    }

    private static Hint? FindUniqueRectangle(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        var loc = LocalizationService.Instance;

        // Unique Rectangle Type 1 (basic): 4 cells in two rows and two cols. Three are exactly {a,b}, the 4th is {a,b}+extras.
        // Eliminate the extra candidates from the 4th cell (elimination hint).
        for (int r1 = 0; r1 < 8; r1++)
        {
            for (int r2 = r1 + 1; r2 < 9; r2++)
            {
                for (int c1 = 0; c1 < 8; c1++)
                {
                    for (int c2 = c1 + 1; c2 < 9; c2++)
                    {
                        if (gameState.Grid[r1, c1].Value != 0 || gameState.Grid[r1, c2].Value != 0 ||
                            gameState.Grid[r2, c1].Value != 0 || gameState.Grid[r2, c2].Value != 0)
                        {
                            continue;
                        }

                        var a = candidates[r1, c1];
                        var b = candidates[r1, c2];
                        var c = candidates[r2, c1];
                        var d = candidates[r2, c2];

                        // Collect which are bivalue
                        var cells = new[]
                        {
                            (r1, c1, a),
                            (r1, c2, b),
                            (r2, c1, c),
                            (r2, c2, d)
                        };

                        // Find a pair {x,y} that appears as exact bivalue in 3 cells
                        for (int x = 1; x <= 9; x++)
                        {
                            for (int y = x + 1; y <= 9; y++)
                            {
                                int exactCount = 0;
                                int extraIndex = -1;

                                for (int i = 0; i < 4; i++)
                                {
                                    var set = cells[i].Item3;
                                    bool hasXY = set.Contains(x) && set.Contains(y);
                                    if (!hasXY) { extraIndex = -2; break; }

                                    if (set.Count == 2)
                                    {
                                        if (set.SetEquals(new HashSet<int> { x, y })) exactCount++;
                                        else { extraIndex = -2; break; }
                                    }
                                    else
                                    {
                                        extraIndex = i;
                                    }
                                }

                                if (extraIndex >= 0 && exactCount == 3)
                                {
                                    var extraSet = cells[extraIndex].Item3;
                                    var extras = new List<int>();
                                    foreach (int cand in extraSet)
                                    {
                                        if (cand != x && cand != y) extras.Add(cand);
                                    }
                                    if (extras.Count == 0) continue;

                                    int tr = cells[extraIndex].Item1;
                                    int tc = cells[extraIndex].Item2;
                                    string cellLabel = $"R{tr + 1}C{tc + 1}";

                                    var related = new List<(int row, int col)>
                                    {
                                        (r1, c1), (r1, c2), (r2, c1), (r2, c2)
                                    };

                                    return new Hint
                                    {
                                        Row = tr,
                                        Col = tc,
                                        Value = 0,
                                        IsPlacement = false,
                                        EliminatedCandidates = extras,
                                        TechniqueId = "UniqueRectangle",
                                        TechniqueName = loc != null ? loc.GetTechniqueName("UniqueRectangle") : "Unique Rectangle",
                                        TechniqueDescription = loc != null ? loc.GetTechniqueDescription("UniqueRectangle") : "",
                                        RelatedCells = related,
                                        Explanation = L(
                                            $"Unique Rectangle: Die vier markierten Zellen bilden ein Rechteck mit den Kandidaten {x}/{y}. In {cellLabel} können die Extra-Kandidaten ({string.Join(", ", extras)}) ausgeschlossen werden.",
                                            $"Unique Rectangle: The four highlighted cells form a rectangle with candidates {x}/{y}. In {cellLabel}, the extra candidate(s) ({string.Join(", ", extras)}) can be eliminated."
                                        )
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private static Hint? FindRemotePair(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        var loc = LocalizationService.Instance;

        // Remote Pair: chain of bivalue cells with the same pair {a,b}. If two such cells are connected by an odd-length path,
        // any cell that sees both endpoints cannot contain a or b.
        for (int a = 1; a <= 8; a++)
        {
            for (int b = a + 1; b <= 9; b++)
            {
                var nodes = new List<(int row, int col)>();
                for (int r = 0; r < 9; r++)
                {
                    for (int c = 0; c < 9; c++)
                    {
                        if (gameState.Grid[r, c].Value != 0) continue;
                        if (candidates[r, c].Count != 2) continue;
                        if (candidates[r, c].Contains(a) && candidates[r, c].Contains(b))
                        {
                            nodes.Add((r, c));
                        }
                    }
                }

                if (nodes.Count < 2) continue;

                // BFS from each node
                for (int s = 0; s < nodes.Count; s++)
                {
                    var start = nodes[s];
                    var queue = new Queue<(int row, int col)>();
                    var dist = new Dictionary<(int row, int col), int>();
                    var parent = new Dictionary<(int row, int col), (int row, int col)>();

                    queue.Enqueue(start);
                    dist[start] = 0;

                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        int curDist = dist[cur];

                        // Try endpoints at odd distance (>=1)
                        if (curDist >= 1 && (curDist % 2 == 1))
                        {
                            // Any cell seeing both endpoints cannot be a or b
                            for (int tr = 0; tr < 9; tr++)
                            {
                                for (int tc = 0; tc < 9; tc++)
                                {
                                    if (gameState.Grid[tr, tc].Value != 0) continue;
                                    if (!Sees(tr, tc, start.row, start.col)) continue;
                                    if (!Sees(tr, tc, cur.row, cur.col)) continue;
                                    if (tr == start.row && tc == start.col) continue;
                                    if (tr == cur.row && tc == cur.col) continue;

                                    var elim = new List<int>();
                                    if (candidates[tr, tc].Contains(a)) elim.Add(a);
                                    if (candidates[tr, tc].Contains(b)) elim.Add(b);
                                    if (elim.Count == 0) continue;

                                    // Build path cells for visualization
                                    var path = new List<(int row, int col)>();
                                    var p = cur;
                                    path.Add(p);
                                    while (!p.Equals(start))
                                    {
                                        if (!parent.TryGetValue(p, out var pp)) break;
                                        p = pp;
                                        path.Add(p);
                                    }

                                    string cellLabel = $"R{tr + 1}C{tc + 1}";
                                    return new Hint
                                    {
                                        Row = tr,
                                        Col = tc,
                                        Value = 0,
                                        IsPlacement = false,
                                        EliminatedCandidates = elim,
                                        TechniqueId = "RemotePair",
                                        TechniqueName = loc != null ? loc.GetTechniqueName("RemotePair") : "Remote Pair",
                                        TechniqueDescription = loc != null ? loc.GetTechniqueDescription("RemotePair") : "",
                                        RelatedCells = path,
                                        Explanation = L(
                                            $"Remote Pair: Eine Kette von Zellen mit dem Kandidatenpaar {a}/{b} erzwingt eine Alternation. Da {cellLabel} beide Enden sieht, können {string.Join(" und ", elim)} dort ausgeschlossen werden.",
                                            $"Remote Pair: A chain of {a}/{b} bi-value cells forces alternation. Since {cellLabel} sees both ends, {string.Join(" and ", elim)} can be eliminated from that cell."
                                        )
                                    };
                                }
                            }
                        }

                        // Expand neighbors (other nodes that see this node)
                        for (int n = 0; n < nodes.Count; n++)
                        {
                            var next = nodes[n];
                            if (next.Equals(cur)) continue;
                            if (!Sees(cur.row, cur.col, next.row, next.col)) continue;
                            if (dist.ContainsKey(next)) continue;

                            dist[next] = curDist + 1;
                            parent[next] = cur;
                            queue.Enqueue(next);
                        }
                    }
                }
            }
        }

        return null;
    }

    private static Hint? FindFinnedXWing(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        var loc = LocalizationService.Instance;

        for (int num = 1; num <= 9; num++)
        {
            var rowToCols = new List<(int row, List<int> cols)>();
            for (int row = 0; row < 9; row++)
            {
                var cols = new List<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (candidates[row, col].Contains(num)) cols.Add(col);
                }
                if (cols.Count >= 2 && cols.Count <= 3)
                {
                    rowToCols.Add((row, cols));
                }
            }

            for (int i = 0; i < rowToCols.Count; i++)
            {
                for (int j = 0; j < rowToCols.Count; j++)
                {
                    if (i == j) continue;
                    var (rBase, baseCols) = rowToCols[i];
                    var (rFin, finCols) = rowToCols[j];

                    if (baseCols.Count != 2 || finCols.Count != 3) continue;

                    int c1 = baseCols[0];
                    int c2 = baseCols[1];
                    if (!finCols.Contains(c1) || !finCols.Contains(c2)) continue;

                    // fin column is the extra one
                    int cf = finCols[0] != c1 && finCols[0] != c2 ? finCols[0]
                        : finCols[1] != c1 && finCols[1] != c2 ? finCols[1]
                        : finCols[2];

                    // Determine which base column shares a block with the fin
                    int finBlockRow = rFin / 3;
                    int finBlockCol = cf / 3;

                    int cFinBase;
                    if (c1 / 3 == finBlockCol) cFinBase = c1;
                    else if (c2 / 3 == finBlockCol) cFinBase = c2;
                    else continue;

                    int cOther = cFinBase == c1 ? c2 : c1;

                    int startRow = finBlockRow * 3;
                    int startCol = finBlockCol * 3;
                    for (int r = startRow; r < startRow + 3; r++)
                    {
                        if (r == rBase || r == rFin) continue;

                        if (candidates[r, cOther].Contains(num))
                        {
                            string cellLabel = $"R{r + 1}C{cOther + 1}";
                            var related = new List<(int row, int col)>
                            {
                                (rBase, c1), (rBase, c2),
                                (rFin, c1), (rFin, c2),
                                (rFin, cf)
                            };

                            return new Hint
                            {
                                Row = r,
                                Col = cOther,
                                Value = 0,
                                IsPlacement = false,
                                EliminatedCandidates = new List<int> { num },
                                TechniqueId = "FinnedXWing",
                                TechniqueName = loc != null ? loc.GetTechniqueName("FinnedXWing") : "Finned X-Wing",
                                TechniqueDescription = loc != null ? loc.GetTechniqueDescription("FinnedXWing") : "",
                                RelatedCells = related,
                                Explanation = L(
                                    $"Finned X-Wing: Für die {num} gibt es ein X-Wing mit einer Finne. Daher kann {num} in {cellLabel} eliminiert werden.",
                                    $"Finned X-Wing: For digit {num}, there is an X-Wing with a fin. Therefore {num} can be eliminated from {cellLabel}."
                                )
                            };
                        }
                    }
                }
            }
        }

        return null;
    }

    private static Hint? FindFinnedSwordfish(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        var loc = LocalizationService.Instance;

        for (int num = 1; num <= 9; num++)
        {
            var rowCols = new List<(int row, List<int> cols)>();
            for (int r = 0; r < 9; r++)
            {
                var cols = new List<int>();
                for (int c = 0; c < 9; c++)
                {
                    if (candidates[r, c].Contains(num)) cols.Add(c);
                }
                if (cols.Count >= 2 && cols.Count <= 4)
                {
                    rowCols.Add((r, cols));
                }
            }

            // Pick 3 rows
            for (int i = 0; i < rowCols.Count; i++)
            {
                for (int j = i + 1; j < rowCols.Count; j++)
                {
                    for (int k = j + 1; k < rowCols.Count; k++)
                    {
                        var rows = new[] { rowCols[i], rowCols[j], rowCols[k] };

                        // Choose a fin row with 4 candidates
                        for (int finIdx = 0; finIdx < 3; finIdx++)
                        {
                            int finRow = rows[finIdx].row;
                            var finCols = rows[finIdx].cols;
                            if (finCols.Count != 4) continue;

                            // Determine base columns as the 3 columns that are shared across the other rows' candidates
                            // We try every combination of 3 columns from finCols as baseCols.
                            for (int a = 0; a < 4; a++)
                            {
                                var baseCols = new List<int>();
                                for (int idx = 0; idx < 4; idx++)
                                {
                                    if (idx != a) baseCols.Add(finCols[idx]);
                                }
                                baseCols.Sort();
                                int finCol = finCols[a];

                                // Other two rows must have candidates subset of baseCols
                                bool ok = true;
                                var baseRows = new List<int>();
                                for (int rr = 0; rr < 3; rr++)
                                {
                                    if (rr == finIdx) continue;
                                    int r = rows[rr].row;
                                    baseRows.Add(r);
                                    var cols = rows[rr].cols;
                                    if (cols.Count < 2 || cols.Count > 3) { ok = false; break; }
                                    for (int ci = 0; ci < cols.Count; ci++)
                                    {
                                        if (!baseCols.Contains(cols[ci])) { ok = false; break; }
                                    }
                                    if (!ok) break;
                                }
                                if (!ok) continue;

                                // Fin must share a block with at least one base column in finRow
                                int finBlockRow = finRow / 3;
                                int finBlockCol = finCol / 3;
                                int cFinBase = -1;
                                for (int bc = 0; bc < baseCols.Count; bc++)
                                {
                                    if (baseCols[bc] / 3 == finBlockCol)
                                    {
                                        cFinBase = baseCols[bc];
                                        break;
                                    }
                                }
                                if (cFinBase < 0) continue;

                                // Eliminate num from cells in the fin block, in base columns other than cFinBase, excluding the 3 base rows
                                int startRow = finBlockRow * 3;
                                int startCol = finBlockCol * 3;
                                for (int r = startRow; r < startRow + 3; r++)
                                {
                                    if (r == finRow || baseRows.Contains(r)) continue;

                                    for (int bc = 0; bc < baseCols.Count; bc++)
                                    {
                                        int cElim = baseCols[bc];
                                        if (cElim == cFinBase) continue;
                                        if (cElim / 3 != finBlockCol) continue;

                                        if (candidates[r, cElim].Contains(num))
                                        {
                                            string cellLabel = $"R{r + 1}C{cElim + 1}";
                                            var related = new List<(int row, int col)>();
                                            foreach (int bc2 in baseCols)
                                            {
                                                related.Add((finRow, bc2));
                                            }
                                            related.Add((finRow, finCol));
                                            for (int rr = 0; rr < 3; rr++)
                                            {
                                                if (rr == finIdx) continue;
                                                int rBase = rows[rr].row;
                                                foreach (int bc2 in rows[rr].cols)
                                                {
                                                    related.Add((rBase, bc2));
                                                }
                                            }

                                            return new Hint
                                            {
                                                Row = r,
                                                Col = cElim,
                                                Value = 0,
                                                IsPlacement = false,
                                                EliminatedCandidates = new List<int> { num },
                                                TechniqueId = "FinnedSwordfish",
                                                TechniqueName = loc != null ? loc.GetTechniqueName("FinnedSwordfish") : "Finned Swordfish",
                                                TechniqueDescription = loc != null ? loc.GetTechniqueDescription("FinnedSwordfish") : "",
                                                RelatedCells = related,
                                                Explanation = L(
                                                    $"Finned Swordfish: Für die {num} gibt es ein Swordfish mit einer Finne. Daher kann {num} in {cellLabel} eliminiert werden.",
                                                    $"Finned Swordfish: For digit {num}, there is a Swordfish with a fin. Therefore {num} can be eliminated from {cellLabel}."
                                                )
                                            };
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private record Als(List<(int row, int col)> Cells, HashSet<int> Union, string HouseId);

    private static IEnumerable<Als> EnumerateAlsCandidates(HashSet<int>[,] candidates)
    {
        // Rows
        for (int r = 0; r < 9; r++)
        {
            var empties = new List<(int row, int col)>();
            for (int c = 0; c < 9; c++)
            {
                if (candidates[r, c].Count > 0) empties.Add((r, c));
            }
            foreach (var als in EnumerateAlsInHouse(empties, candidates, $"R{r}")) yield return als;
        }

        // Cols
        for (int c = 0; c < 9; c++)
        {
            var empties = new List<(int row, int col)>();
            for (int r = 0; r < 9; r++)
            {
                if (candidates[r, c].Count > 0) empties.Add((r, c));
            }
            foreach (var als in EnumerateAlsInHouse(empties, candidates, $"C{c}")) yield return als;
        }

        // Blocks
        for (int br = 0; br < 3; br++)
        {
            for (int bc = 0; bc < 3; bc++)
            {
                var empties = new List<(int row, int col)>();
                int sr = br * 3;
                int sc = bc * 3;
                for (int r = sr; r < sr + 3; r++)
                {
                    for (int c = sc; c < sc + 3; c++)
                    {
                        if (candidates[r, c].Count > 0) empties.Add((r, c));
                    }
                }
                foreach (var als in EnumerateAlsInHouse(empties, candidates, $"B{br}{bc}")) yield return als;
            }
        }
    }

    private static IEnumerable<Als> EnumerateAlsInHouse(List<(int row, int col)> cells, HashSet<int>[,] candidates, string houseId)
    {
        // Limit ALS sizes for performance
        int n = cells.Count;
        for (int size = 1; size <= 3; size++)
        {
            for (int i = 0; i < n; i++)
            {
                if (size == 1)
                {
                    var union = new HashSet<int>(candidates[cells[i].row, cells[i].col]);
                    if (union.Count == 2)
                    {
                        yield return new Als(new List<(int, int)> { cells[i] }, union, houseId);
                    }
                    continue;
                }

                for (int j = i + 1; j < n; j++)
                {
                    if (size == 2)
                    {
                        var union = new HashSet<int>(candidates[cells[i].row, cells[i].col]);
                        union.UnionWith(candidates[cells[j].row, cells[j].col]);
                        if (union.Count == 3)
                        {
                            yield return new Als(new List<(int, int)> { cells[i], cells[j] }, union, houseId);
                        }
                        continue;
                    }

                    for (int k = j + 1; k < n; k++)
                    {
                        var union = new HashSet<int>(candidates[cells[i].row, cells[i].col]);
                        union.UnionWith(candidates[cells[j].row, cells[j].col]);
                        union.UnionWith(candidates[cells[k].row, cells[k].col]);
                        if (union.Count == 4)
                        {
                            yield return new Als(new List<(int, int)> { cells[i], cells[j], cells[k] }, union, houseId);
                        }
                    }
                }
            }
        }
    }

    private static Hint? FindAlsXzRule(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        var loc = LocalizationService.Instance;

        var alsList = new List<Als>(EnumerateAlsCandidates(candidates));
        if (alsList.Count == 0) return null;

        for (int i = 0; i < alsList.Count; i++)
        {
            for (int j = i + 1; j < alsList.Count; j++)
            {
                var a = alsList[i];
                var b = alsList[j];

                // Disjoint
                bool overlap = false;
                foreach (var cellA in a.Cells)
                {
                    for (int t = 0; t < b.Cells.Count; t++)
                    {
                        if (b.Cells[t].row == cellA.row && b.Cells[t].col == cellA.col) { overlap = true; break; }
                    }
                    if (overlap) break;
                }
                if (overlap) continue;

                // Common candidates
                var common = new List<int>();
                foreach (int cand in a.Union)
                {
                    if (b.Union.Contains(cand)) common.Add(cand);
                }
                if (common.Count < 2) continue;

                foreach (int x in common)
                {
                    // restricted common: appears in exactly 1 cell in each ALS, and those cells see each other
                    var axCells = new List<(int row, int col)>();
                    foreach (var cell in a.Cells)
                    {
                        if (candidates[cell.row, cell.col].Contains(x)) axCells.Add(cell);
                    }
                    if (axCells.Count != 1) continue;

                    var bxCells = new List<(int row, int col)>();
                    foreach (var cell in b.Cells)
                    {
                        if (candidates[cell.row, cell.col].Contains(x)) bxCells.Add(cell);
                    }
                    if (bxCells.Count != 1) continue;

                    if (!Sees(axCells[0].row, axCells[0].col, bxCells[0].row, bxCells[0].col)) continue;

                    foreach (int z in common)
                    {
                        if (z == x) continue;

                        var azCells = new List<(int row, int col)>();
                        foreach (var cell in a.Cells)
                        {
                            if (candidates[cell.row, cell.col].Contains(z)) azCells.Add(cell);
                        }
                        if (azCells.Count == 0) continue;

                        var bzCells = new List<(int row, int col)>();
                        foreach (var cell in b.Cells)
                        {
                            if (candidates[cell.row, cell.col].Contains(z)) bzCells.Add(cell);
                        }
                        if (bzCells.Count == 0) continue;

                        // Find elimination cell that sees all Z instances in both ALS
                        for (int tr = 0; tr < 9; tr++)
                        {
                            for (int tc = 0; tc < 9; tc++)
                            {
                                if (gameState.Grid[tr, tc].Value != 0) continue;
                                if (!candidates[tr, tc].Contains(z)) continue;

                                // not part of either ALS
                                bool inAls = false;
                                for (int m = 0; m < a.Cells.Count; m++)
                                {
                                    if (a.Cells[m].row == tr && a.Cells[m].col == tc) { inAls = true; break; }
                                }
                                if (!inAls)
                                {
                                    for (int m = 0; m < b.Cells.Count; m++)
                                    {
                                        if (b.Cells[m].row == tr && b.Cells[m].col == tc) { inAls = true; break; }
                                    }
                                }
                                if (inAls) continue;

                                bool seesAll = true;
                                for (int m = 0; m < azCells.Count; m++)
                                {
                                    if (!Sees(tr, tc, azCells[m].row, azCells[m].col)) { seesAll = false; break; }
                                }
                                if (!seesAll) continue;
                                for (int m = 0; m < bzCells.Count; m++)
                                {
                                    if (!Sees(tr, tc, bzCells[m].row, bzCells[m].col)) { seesAll = false; break; }
                                }
                                if (!seesAll) continue;

                                string cellLabel = $"R{tr + 1}C{tc + 1}";
                                var related = new List<(int row, int col)>();
                                related.AddRange(a.Cells);
                                related.AddRange(b.Cells);

                                return new Hint
                                {
                                    Row = tr,
                                    Col = tc,
                                    Value = 0,
                                    IsPlacement = false,
                                    EliminatedCandidates = new List<int> { z },
                                    TechniqueId = "ALSXZRule",
                                    TechniqueName = loc != null ? loc.GetTechniqueName("ALSXZRule") : "ALS-XZ Rule",
                                    TechniqueDescription = loc != null ? loc.GetTechniqueDescription("ALSXZRule") : "",
                                    RelatedCells = related,
                                    Explanation = L(
                                        $"ALS-XZ: Zwei Almost Locked Sets sind über einen eingeschränkten Kandidaten {x} verbunden. Daher kann {z} in {cellLabel} eliminiert werden.",
                                        $"ALS-XZ: Two almost locked sets are linked by the restricted common candidate {x}. Therefore {z} can be eliminated from {cellLabel}."
                                    )
                                };
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Berechnet die Kandidaten für alle Zellen
    /// </summary>
    public static HashSet<int>[,] CalculateAllCandidates(SudokuGameState gameState, bool respectNotes = false)
    {
        var candidates = new HashSet<int>[9, 9];

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                candidates[row, col] = new HashSet<int>();

                if (gameState.Grid[row, col].Value == 0)
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        if (CanPlaceNumber(gameState, row, col, num, respectNotes))
                        {
                            candidates[row, col].Add(num);
                        }
                    }
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// Naked Single: Eine Zelle hat nur eine mögliche Zahl
    /// </summary>
    private static Hint? FindNakedSingle(SudokuGameState gameState, bool respectNotes)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (gameState.Grid[row, col].Value != 0) continue;

                var possibleNumbers = GetPossibleNumbers(gameState, row, col, respectNotes);

                if (possibleNumbers.Count == 1)
                {
                    var loc = LocalizationService.Instance;
                    int value = possibleNumbers[0];
                    var relatedCells = GetAllRelatedCellsWithValue(gameState, row, col);

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = value,
                        TechniqueId = "NakedSingle",
                        TechniqueName = loc.Get("hint.naked_single"),
                        TechniqueDescription = loc.Get("hint.naked_single.desc"),
                        RelatedCells = relatedCells,
                        Explanation = loc.Get("hint.naked_single.explanation", value)
                    };
                }
            }
        }
        return null;
    }

    private static void CollectNakedSingles(SudokuGameState gameState, List<Hint> results, bool respectNotes)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (gameState.Grid[row, col].Value != 0) continue;

                var possibleNumbers = GetPossibleNumbers(gameState, row, col, respectNotes);
                if (possibleNumbers.Count == 1)
                {
                    int value = possibleNumbers[0];
                    var relatedCells = GetAllRelatedCellsWithValue(gameState, row, col);
                    results.Add(new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = value,
                        TechniqueId = "NakedSingle",
                        TechniqueName = LocalizationService.Instance.Get("hint.naked_single"),
                        TechniqueDescription = LocalizationService.Instance.Get("hint.naked_single.desc"),
                        RelatedCells = relatedCells,
                        Explanation = LocalizationService.Instance.Get("hint.naked_single.explanation", value)
                    });
                }
            }
        }
    }

    private static void CollectForcedSingles(SudokuGameState gameState, List<Hint> results, bool respectNotes)
    {
        // Precompute candidates for all empty cells
        var allCandidates = CalculateAllCandidates(gameState, respectNotes);

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (gameState.Grid[row, col].Value != 0) continue;

                var cands = allCandidates[row, col];
                if (cands.Count != 1) continue;
                int value = cands.First();

                // Check uniqueness of this value in row/col/box among empty cells
                bool uniqueRow = true;
                for (int c = 0; c < 9; c++)
                {
                    if (c == col) continue;
                    if (gameState.Grid[row, c].Value == 0 && allCandidates[row, c].Contains(value))
                    {
                        uniqueRow = false; break;
                    }
                }

                bool uniqueCol = true;
                for (int r = 0; r < 9; r++)
                {
                    if (r == row) continue;
                    if (gameState.Grid[r, col].Value == 0 && allCandidates[r, col].Contains(value))
                    {
                        uniqueCol = false; break;
                    }
                }

                bool uniqueBox = true;
                int br = (row / 3) * 3;
                int bc = (col / 3) * 3;
                for (int r = br; r < br + 3 && uniqueBox; r++)
                {
                    for (int c = bc; c < bc + 3; c++)
                    {
                        if (r == row && c == col) continue;
                        if (gameState.Grid[r, c].Value == 0 && allCandidates[r, c].Contains(value))
                        {
                            uniqueBox = false; break;
                        }
                    }
                }

                if (!(uniqueRow || uniqueCol || uniqueBox))
                    continue; // not uniquely forced in any house

                var relatedCells = GetAllRelatedCellsWithValue(gameState, row, col);
                results.Add(new Hint
                {
                    Row = row,
                    Col = col,
                    Value = value,
                    TechniqueId = "ForcedSingle",
                    TechniqueName = LocalizationService.Instance.Get("hint.naked_single"),
                    TechniqueDescription = LocalizationService.Instance.Get("hint.naked_single.desc"),
                    RelatedCells = relatedCells,
                    Explanation = LocalizationService.Instance.Get("hint.naked_single.explanation", value)
                });
            }
        }
    }

    private static void CollectNakedPairs(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        // Rows
        for (int row = 0; row < 9; row++)
        {
            var pairCells = new List<(int col, HashSet<int> cands)>();
            for (int col = 0; col < 9; col++)
            {
                if (candidates[row, col].Count == 2)
                {
                    pairCells.Add((col, candidates[row, col]));
                }
            }

            for (int i = 0; i < pairCells.Count; i++)
            {
                for (int j = i + 1; j < pairCells.Count; j++)
                {
                    if (!pairCells[i].cands.SetEquals(pairCells[j].cands)) continue;
                    var pairNums = new List<int>(pairCells[i].cands);

                    for (int col = 0; col < 9; col++)
                    {
                        if (col == pairCells[i].col || col == pairCells[j].col) continue;

                        if (candidates[row, col].Contains(pairNums[0]) || candidates[row, col].Contains(pairNums[1]))
                        {
                            var remainingCands = new HashSet<int>(candidates[row, col]);
                            remainingCands.ExceptWith(pairNums);

                            if (remainingCands.Count == 1)
                            {
                                int value = GetSingleElement(remainingCands);
                                var relatedCells = new List<(int, int)> { (row, pairCells[i].col), (row, pairCells[j].col) };
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = col,
                                    Value = value,
                                    TechniqueId = "NakedPair",
                                    TechniqueName = LocalizationService.Instance.Get("hint.naked_pair"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.naked_pair.desc", pairNums[0], pairNums[1]),
                                    RelatedCells = relatedCells,
                                    Explanation = LocalizationService.Instance.Get("hint.naked_pair.explanation", pairCells[i].col + 1, pairCells[j].col + 1, pairNums[0], pairNums[1], value)
                                });
                            }
                            else if (allowElimination)
                            {
                                var elim = candidates[row, col].Where(n => pairNums.Contains(n)).ToList();
                                if (elim.Count == 0) continue;
                                var relatedCells = new List<(int, int)> { (row, pairCells[i].col), (row, pairCells[j].col) };
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = col,
                                    Value = 0,
                                    IsPlacement = false,
                                    EliminatedCandidates = elim,
                                    TechniqueId = "NakedPair",
                                    TechniqueName = LocalizationService.Instance.Get("hint.naked_pair"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.naked_pair.desc", pairNums[0], pairNums[1]),
                                    RelatedCells = relatedCells,
                                    Explanation = LocalizationService.Instance.Get("hint.naked_pair.explanation", pairCells[i].col + 1, pairCells[j].col + 1, pairNums[0], pairNums[1], string.Join(", ", elim))
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    private static void CollectPointingPairs(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        for (int blockRow = 0; blockRow < 3; blockRow++)
        {
            for (int blockCol = 0; blockCol < 3; blockCol++)
            {
                for (int num = 1; num <= 9; num++)
                {
                    var positions = new List<(int row, int col)>();
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row = blockRow * 3 + r;
                            int col = blockCol * 3 + c;
                            if (candidates[row, col].Contains(num)) positions.Add((row, col));
                        }
                    }

                    if (positions.Count < 2 || positions.Count > 3) continue;

                    if (AllSameRow(positions))
                    {
                        int targetRow = positions[0].row;
                        for (int col = 0; col < 9; col++)
                        {
                            if (col / 3 == blockCol) continue;
                            if (!candidates[targetRow, col].Contains(num)) continue;

                            var remainingCands = new HashSet<int>(candidates[targetRow, col]);
                            remainingCands.Remove(num);
                            if (remainingCands.Count == 1)
                            {
                                int value = GetSingleElement(remainingCands);
                                results.Add(new Hint
                                {
                                    Row = targetRow,
                                    Col = col,
                                    Value = value,
                                    TechniqueId = "PointingPair",
                                    TechniqueName = LocalizationService.Instance.Get("hint.pointing_pair"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.pointing_pair.row.desc", num),
                                    RelatedCells = positions,
                                    Explanation = LocalizationService.Instance.Get("hint.pointing_pair.row.explanation", num, targetRow + 1)
                                });
                            }
                            else if (allowElimination)
                            {
                                results.Add(new Hint
                                {
                                    Row = targetRow,
                                    Col = col,
                                    Value = 0,
                                    IsPlacement = false,
                                    EliminatedCandidates = new List<int> { num },
                                    TechniqueId = "PointingPair",
                                    TechniqueName = LocalizationService.Instance.Get("hint.pointing_pair"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.pointing_pair.row.desc", num),
                                    RelatedCells = positions,
                                    Explanation = LocalizationService.Instance.Get("hint.pointing_pair.row.explanation", num, targetRow + 1)
                                });
                            }
                        }
                    }

                    if (AllSameCol(positions))
                    {
                        int targetCol = positions[0].col;
                        for (int row = 0; row < 9; row++)
                        {
                            if (row / 3 == blockRow) continue;
                            if (!candidates[row, targetCol].Contains(num)) continue;

                            var remainingCands = new HashSet<int>(candidates[row, targetCol]);
                            remainingCands.Remove(num);
                            if (remainingCands.Count == 1)
                            {
                                int value = GetSingleElement(remainingCands);
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = targetCol,
                                    Value = value,
                                    TechniqueId = "PointingPair",
                                    TechniqueName = LocalizationService.Instance.Get("hint.pointing_pair"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.pointing_pair.col.desc", num),
                                    RelatedCells = positions,
                                    Explanation = LocalizationService.Instance.Get("hint.pointing_pair.col.explanation", num, targetCol + 1)
                                });
                            }
                            else if (allowElimination)
                            {
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = targetCol,
                                    Value = 0,
                                    IsPlacement = false,
                                    EliminatedCandidates = new List<int> { num },
                                    TechniqueId = "PointingPair",
                                    TechniqueName = LocalizationService.Instance.Get("hint.pointing_pair"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.pointing_pair.col.desc", num),
                                    RelatedCells = positions,
                                    Explanation = LocalizationService.Instance.Get("hint.pointing_pair.col.explanation", num, targetCol + 1)
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    private static void CollectBoxLineReductions(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int num = 1; num <= 9; num++)
            {
                var cols = new List<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (candidates[row, col].Contains(num)) cols.Add(col);
                }

                if (cols.Count < 2 || cols.Count > 3) continue;

                int blockCol = cols[0] / 3;
                bool sameBlock = true;
                for (int i = 1; i < cols.Count; i++)
                {
                    if (cols[i] / 3 != blockCol) { sameBlock = false; break; }
                }
                if (!sameBlock) continue;

                int blockRow = row / 3;
                for (int r = blockRow * 3; r < blockRow * 3 + 3; r++)
                {
                    if (r == row) continue;
                    for (int c = blockCol * 3; c < blockCol * 3 + 3; c++)
                    {
                        if (!candidates[r, c].Contains(num)) continue;

                        var remainingCands = new HashSet<int>(candidates[r, c]);
                        remainingCands.Remove(num);
                        if (remainingCands.Count == 1)
                        {
                            int value = GetSingleElement(remainingCands);
                            var relatedCells = new List<(int, int)>(cols.Count);
                            foreach (int colIdx in cols) relatedCells.Add((row, colIdx));

                            results.Add(new Hint
                            {
                                Row = r,
                                Col = c,
                                Value = value,
                                TechniqueId = "BoxLineReduction",
                                TechniqueName = LocalizationService.Instance.Get("hint.box_line"),
                                TechniqueDescription = LocalizationService.Instance.Get("hint.box_line.desc", num, row + 1),
                                RelatedCells = relatedCells,
                                Explanation = LocalizationService.Instance.Get("hint.box_line.explanation", row + 1, num)
                            });
                        }
                        else if (allowElimination)
                        {
                            var relatedCells = new List<(int, int)>(cols.Count);
                            foreach (int colIdx in cols) relatedCells.Add((row, colIdx));
                            results.Add(new Hint
                            {
                                Row = r,
                                Col = c,
                                Value = 0,
                                IsPlacement = false,
                                EliminatedCandidates = new List<int> { num },
                                TechniqueId = "BoxLineReduction",
                                TechniqueName = LocalizationService.Instance.Get("hint.box_line"),
                                TechniqueDescription = LocalizationService.Instance.Get("hint.box_line.desc", num, row + 1),
                                RelatedCells = relatedCells,
                                Explanation = LocalizationService.Instance.Get("hint.box_line.explanation", row + 1, num)
                            });
                        }
                    }
                }
            }
        }
    }

    private static void CollectXWings(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        for (int num = 1; num <= 9; num++)
        {
            var rowPairs = new List<(int row, int col1, int col2)>();
            for (int row = 0; row < 9; row++)
            {
                var cols = new List<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (candidates[row, col].Contains(num)) cols.Add(col);
                }
                if (cols.Count == 2)
                {
                    rowPairs.Add((row, cols[0], cols[1]));
                }
            }

            for (int i = 0; i < rowPairs.Count; i++)
            {
                for (int j = i + 1; j < rowPairs.Count; j++)
                {
                    if (rowPairs[i].col1 != rowPairs[j].col1 || rowPairs[i].col2 != rowPairs[j].col2) continue;

                    int col1 = rowPairs[i].col1;
                    int col2 = rowPairs[i].col2;

                    for (int row = 0; row < 9; row++)
                    {
                        if (row == rowPairs[i].row || row == rowPairs[j].row) continue;

                        if (candidates[row, col1].Contains(num))
                        {
                            var remainingCands = new HashSet<int>(candidates[row, col1]);
                            remainingCands.Remove(num);
                            var relatedCells = new List<(int, int)>
                            {
                                (rowPairs[i].row, col1), (rowPairs[i].row, col2),
                                (rowPairs[j].row, col1), (rowPairs[j].row, col2)
                            };

                            if (remainingCands.Count == 1)
                            {
                                int value = GetSingleElement(remainingCands);
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = col1,
                                    Value = value,
                                    TechniqueId = "XWing",
                                    TechniqueName = LocalizationService.Instance.Get("hint.x_wing"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.x_wing.desc", num),
                                    RelatedCells = relatedCells,
                                    Explanation = LocalizationService.Instance.Get("hint.x_wing.explanation", num, rowPairs[i].row + 1, rowPairs[j].row + 1, col1 + 1, col2 + 1)
                                });
                            }
                            else if (allowElimination)
                            {
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = col1,
                                    Value = 0,
                                    IsPlacement = false,
                                    EliminatedCandidates = new List<int> { num },
                                    TechniqueId = "XWing",
                                    TechniqueName = LocalizationService.Instance.Get("hint.x_wing"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.x_wing.desc", num),
                                    RelatedCells = relatedCells,
                                    Explanation = LocalizationService.Instance.Get("hint.x_wing.explanation", num, rowPairs[i].row + 1, rowPairs[j].row + 1, col1 + 1, col2 + 1)
                                });
                            }
                        }

                        if (candidates[row, col2].Contains(num))
                        {
                            var remainingCands = new HashSet<int>(candidates[row, col2]);
                            remainingCands.Remove(num);
                            var relatedCells = new List<(int, int)>
                            {
                                (rowPairs[i].row, col1), (rowPairs[i].row, col2),
                                (rowPairs[j].row, col1), (rowPairs[j].row, col2)
                            };

                            if (remainingCands.Count == 1)
                            {
                                int value = GetSingleElement(remainingCands);
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = col2,
                                    Value = value,
                                    TechniqueId = "XWing",
                                    TechniqueName = LocalizationService.Instance.Get("hint.x_wing"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.x_wing.desc", num),
                                    RelatedCells = relatedCells,
                                    Explanation = LocalizationService.Instance.Get("hint.x_wing.explanation", num, rowPairs[i].row + 1, rowPairs[j].row + 1, col1 + 1, col2 + 1)
                                });
                            }
                            else if (allowElimination)
                            {
                                results.Add(new Hint
                                {
                                    Row = row,
                                    Col = col2,
                                    Value = 0,
                                    IsPlacement = false,
                                    EliminatedCandidates = new List<int> { num },
                                    TechniqueId = "XWing",
                                    TechniqueName = LocalizationService.Instance.Get("hint.x_wing"),
                                    TechniqueDescription = LocalizationService.Instance.Get("hint.x_wing.desc", num),
                                    RelatedCells = relatedCells,
                                    Explanation = LocalizationService.Instance.Get("hint.x_wing.explanation", num, rowPairs[i].row + 1, rowPairs[j].row + 1, col1 + 1, col2 + 1)
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    private static void CollectNakedTriples(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        // Rows and cols and boxes
        CollectNakedSet(gameState, candidates, results, setSize: 3, scope: "row", allowElimination);
        if (results.Count > 0) return;
        CollectNakedSet(gameState, candidates, results, setSize: 3, scope: "col", allowElimination);
        if (results.Count > 0) return;
        CollectNakedSet(gameState, candidates, results, setSize: 3, scope: "box", allowElimination);
    }

    private static void CollectNakedQuads(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        CollectNakedSet(gameState, candidates, results, setSize: 4, scope: "row", allowElimination);
        if (results.Count > 0) return;
        CollectNakedSet(gameState, candidates, results, setSize: 4, scope: "col", allowElimination);
        if (results.Count > 0) return;
        CollectNakedSet(gameState, candidates, results, setSize: 4, scope: "box", allowElimination);
    }

    private static void CollectNakedSet(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, int setSize, string scope, bool allowElimination)
    {
        int houseCount = scope == "box" ? 9 : 9;
        for (int house = 0; house < houseCount; house++)
        {
            var cells = new List<(int row, int col)>();
            if (scope == "row")
            {
                int row = house;
                for (int col = 0; col < 9; col++)
                    if (candidates[row, col].Count > 0) cells.Add((row, col));
            }
            else if (scope == "col")
            {
                int col = house;
                for (int row = 0; row < 9; row++)
                    if (candidates[row, col].Count > 0) cells.Add((row, col));
            }
            else
            {
                int br = (house / 3) * 3;
                int bc = (house % 3) * 3;
                for (int r = br; r < br + 3; r++)
                    for (int c = bc; c < bc + 3; c++)
                        if (candidates[r, c].Count > 0) cells.Add((r, c));
            }

            int n = cells.Count;
            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            for (int k = j + 1; k < n; k++)
            {
                var set = new HashSet<int>(candidates[cells[i].row, cells[i].col]);
                set.UnionWith(candidates[cells[j].row, cells[j].col]);
                set.UnionWith(candidates[cells[k].row, cells[k].col]);
                if (set.Count > setSize) continue;
                if (setSize == 3 && set.Count != 3) continue;

                if (setSize == 4)
                {
                    for (int l = k + 1; l < n; l++)
                    {
                        var set4 = new HashSet<int>(set);
                        set4.UnionWith(candidates[cells[l].row, cells[l].col]);
                        if (set4.Count != 4) continue;
                        TryEliminateFromHouse(gameState, candidates, results, scope, house, set4, new[] { cells[i], cells[j], cells[k], cells[l] }, allowElimination);
                        if (results.Count > 0) return;
                    }
                }
                else
                {
                    TryEliminateFromHouse(gameState, candidates, results, scope, house, set, new[] { cells[i], cells[j], cells[k] }, allowElimination);
                    if (results.Count > 0) return;
                }
            }
        }
    }

    private static void TryEliminateFromHouse(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, string scope, int house, HashSet<int> set, (int row, int col)[] members, bool allowElimination)
    {
        // eliminate set numbers from other cells in house; if any becomes single, create placement
        IEnumerable<(int row, int col)> enumCells()
        {
            if (scope == "row")
            {
                int row = house;
                for (int c = 0; c < 9; c++) yield return (row, c);
            }
            else if (scope == "col")
            {
                int col = house;
                for (int r = 0; r < 9; r++) yield return (r, col);
            }
            else
            {
                int br = (house / 3) * 3;
                int bc = (house % 3) * 3;
                for (int r = br; r < br + 3; r++)
                    for (int c = bc; c < bc + 3; c++)
                        yield return (r, c);
            }
        }

        var memberSet = new HashSet<(int, int)>(members);
        foreach (var cell in enumCells())
        {
            if (memberSet.Contains(cell)) continue;
            if (gameState.Grid[cell.row, cell.col].Value != 0) continue;
            if (!candidates[cell.row, cell.col].Overlaps(set)) continue;

            var remaining = new HashSet<int>(candidates[cell.row, cell.col]);
            remaining.ExceptWith(set);
            if (remaining.Count == 1)
            {
                int value = GetSingleElement(remaining);
                results.Add(new Hint
                {
                    Row = cell.row,
                    Col = cell.col,
                    Value = value,
                    TechniqueId = set.Count == 3 ? "NakedTriple" : "NakedQuad",
                    TechniqueName = LocalizationService.Instance.Get(set.Count == 3 ? "hint.naked_triple" : "hint.naked_quad"),
                    TechniqueDescription = LocalizationService.Instance.Get(set.Count == 3 ? "hint.naked_triple.desc" : "hint.naked_quad.desc"),
                    RelatedCells = members.ToList(),
                    Explanation = LocalizationService.Instance.Get(set.Count == 3 ? "hint.naked_triple.explanation" : "hint.naked_quad.explanation", value)
                });
                return;
            }
            else if (allowElimination)
            {
                var elim = candidates[cell.row, cell.col].Where(n => set.Contains(n)).ToList();
                if (elim.Count == 0) continue;
                results.Add(new Hint
                {
                    Row = cell.row,
                    Col = cell.col,
                    Value = 0,
                    IsPlacement = false,
                    EliminatedCandidates = elim,
                    TechniqueId = set.Count == 3 ? "NakedTriple" : "NakedQuad",
                    TechniqueName = LocalizationService.Instance.Get(set.Count == 3 ? "hint.naked_triple" : "hint.naked_quad"),
                    TechniqueDescription = LocalizationService.Instance.Get(set.Count == 3 ? "hint.naked_triple.desc" : "hint.naked_quad.desc"),
                    RelatedCells = members.ToList(),
                    Explanation = LocalizationService.Instance.Get(set.Count == 3 ? "hint.naked_triple.explanation" : "hint.naked_quad.explanation", string.Join(", ", elim))
                });
                return;
            }
        }
    }

    private static void CollectHiddenPairs(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results)
    {
        CollectHiddenSet(gameState, candidates, results, setSize: 2, scope: "row");
        if (results.Count > 0) return;
        CollectHiddenSet(gameState, candidates, results, setSize: 2, scope: "col");
        if (results.Count > 0) return;
        CollectHiddenSet(gameState, candidates, results, setSize: 2, scope: "box");
    }

    private static void CollectHiddenTriples(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results)
    {
        CollectHiddenSet(gameState, candidates, results, setSize: 3, scope: "row");
        if (results.Count > 0) return;
        CollectHiddenSet(gameState, candidates, results, setSize: 3, scope: "col");
        if (results.Count > 0) return;
        CollectHiddenSet(gameState, candidates, results, setSize: 3, scope: "box");
    }

    private static void CollectHiddenSet(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, int setSize, string scope)
    {
        IEnumerable<(int row, int col)> enumCells(int house)
        {
            if (scope == "row") { int r = house; for (int c = 0; c < 9; c++) yield return (r, c); }
            else if (scope == "col") { int c = house; for (int r = 0; r < 9; r++) yield return (r, c); }
            else { int br = (house / 3) * 3; int bc = (house % 3) * 3; for (int r = br; r < br + 3; r++) for (int c = bc; c < bc + 3; c++) yield return (r, c); }
        }

        for (int house = 0; house < 9; house++)
        {
            var positionsByNum = new Dictionary<int, List<(int row, int col)>>();
            for (int num = 1; num <= 9; num++) positionsByNum[num] = new List<(int, int)>();

            foreach (var cell in enumCells(house))
            {
                if (gameState.Grid[cell.row, cell.col].Value != 0) continue;
                foreach (int num in candidates[cell.row, cell.col])
                    positionsByNum[num].Add(cell);
            }

            var nums = Enumerable.Range(1, 9).ToArray();
            for (int i = 0; i < nums.Length; i++)
            {
                for (int j = i + 1; j < nums.Length; j++)
                {
                    if (setSize == 2)
                    {
                        var posI = positionsByNum[nums[i]];
                        var posJ = positionsByNum[nums[j]];
                        if (posI.Count == 2 && posJ.Count == 2 && posI.SequenceEqual(posJ))
                        {
                            foreach (var cell in posI)
                            {
                                var remaining = new HashSet<int>(candidates[cell.row, cell.col]);
                                remaining.RemoveWhere(n => n != nums[i] && n != nums[j]);
                                if (remaining.Count == 1)
                                {
                                    int value = GetSingleElement(remaining);
                                    results.Add(new Hint
                                    {
                                        Row = cell.row,
                                        Col = cell.col,
                                        Value = value,
                                        TechniqueId = "HiddenPair",
                                        TechniqueName = LocalizationService.Instance.Get("hint.hidden_pair"),
                                        TechniqueDescription = LocalizationService.Instance.Get("hint.hidden_pair.desc", nums[i], nums[j]),
                                        RelatedCells = posI,
                                        Explanation = LocalizationService.Instance.Get("hint.hidden_pair.explanation", value)
                                    });
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int k = j + 1; k < nums.Length; k++)
                        {
                            var posI = positionsByNum[nums[i]];
                            var posJ = positionsByNum[nums[j]];
                            var posK = positionsByNum[nums[k]];
                            var union = posI.Union(posJ).Union(posK).Distinct().ToList();
                            if (union.Count != 3) continue;
                            foreach (var cell in union)
                            {
                                var remaining = new HashSet<int>(candidates[cell.row, cell.col]);
                                remaining.RemoveWhere(n => n != nums[i] && n != nums[j] && n != nums[k]);
                                if (remaining.Count == 1)
                                {
                                    int value = GetSingleElement(remaining);
                                    results.Add(new Hint
                                    {
                                        Row = cell.row,
                                        Col = cell.col,
                                        Value = value,
                                        TechniqueId = "HiddenTriple",
                                        TechniqueName = LocalizationService.Instance.Get("hint.hidden_triple"),
                                        TechniqueDescription = LocalizationService.Instance.Get("hint.hidden_triple.desc", nums[i], nums[j], nums[k]),
                                        RelatedCells = union,
                                        Explanation = LocalizationService.Instance.Get("hint.hidden_triple.explanation", value)
                                    });
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static void CollectSwordfish(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        CollectFish(gameState, candidates, results, size: 3, techniqueId: "Swordfish", locKey: "hint.swordfish", allowElimination);
    }

    private static void CollectJellyfish(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        CollectFish(gameState, candidates, results, size: 4, techniqueId: "Jellyfish", locKey: "hint.jellyfish", allowElimination);
    }

    private static void CollectFish(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, int size, string techniqueId, string locKey, bool allowElimination)
    {
        for (int num = 1; num <= 9; num++)
        {
            var rowCols = new List<(int row, List<int> cols)>();
            for (int r = 0; r < 9; r++)
            {
                var cols = new List<int>();
                for (int c = 0; c < 9; c++) if (candidates[r, c].Contains(num)) cols.Add(c);
                if (cols.Count >= 2 && cols.Count <= size) rowCols.Add((r, cols));
            }

            for (int a = 0; a < rowCols.Count; a++)
            for (int b = a + 1; b < rowCols.Count; b++)
            for (int c = b + 1; c < rowCols.Count && size >= 3; c++)
            {
                if (size == 4)
                {
                    for (int d = c + 1; d < rowCols.Count; d++)
                    {
                        var rows = new[] { rowCols[a], rowCols[b], rowCols[c], rowCols[d] };
                        TryFish(gameState, candidates, results, num, rows, size, techniqueId, locKey, allowElimination);
                        if (results.Count > 0) return;
                    }
                }
                else
                {
                    var rows = new[] { rowCols[a], rowCols[b], rowCols[c] };
                    TryFish(gameState, candidates, results, num, rows, size, techniqueId, locKey, allowElimination);
                    if (results.Count > 0) return;
                }
            }
        }
    }

    private static void TryFish(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, int num, (int row, List<int> cols)[] rows, int size, string techniqueId, string locKey, bool allowElimination)
    {
        var colUnion = new HashSet<int>();
        foreach (var r in rows) foreach (int c in r.cols) colUnion.Add(c);
        if (colUnion.Count != size) return;

        for (int row = 0; row < 9; row++)
        {
            bool isRowInSet = rows.Any(t => t.row == row);
            if (isRowInSet) continue;

            foreach (int col in colUnion)
            {
                if (!candidates[row, col].Contains(num)) continue;
                var remaining = new HashSet<int>(candidates[row, col]);
                remaining.Remove(num);
                var related = rows.SelectMany(r => r.cols.Select(c => (r.row, c))).ToList();
                if (remaining.Count == 1)
                {
                    int value = GetSingleElement(remaining);
                    results.Add(new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = value,
                        TechniqueId = techniqueId,
                        TechniqueName = LocalizationService.Instance.Get(locKey),
                        TechniqueDescription = LocalizationService.Instance.Get(locKey + ".desc", num),
                        RelatedCells = related,
                        Explanation = LocalizationService.Instance.Get(locKey + ".explanation", num)
                    });
                    return;
                }
                else if (allowElimination)
                {
                    results.Add(new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = 0,
                        IsPlacement = false,
                        EliminatedCandidates = new List<int> { num },
                        TechniqueId = techniqueId,
                        TechniqueName = LocalizationService.Instance.Get(locKey),
                        TechniqueDescription = LocalizationService.Instance.Get(locKey + ".desc", num),
                        RelatedCells = related,
                        Explanation = LocalizationService.Instance.Get(locKey + ".explanation", num)
                    });
                    return;
                }
            }
        }
    }

    private static void CollectXYWings(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        for (int pr = 0; pr < 9; pr++)
        for (int pc = 0; pc < 9; pc++)
        {
            if (gameState.Grid[pr, pc].Value != 0) continue;
            var pivot = candidates[pr, pc];
            if (pivot.Count != 2) continue;
            int[] pv = pivot.ToArray();
            int a = pv[0], b = pv[1];

            var pincers = new List<(int row, int col, int candShared, int candOther)>();
            for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                if (r == pr && c == pc) continue;
                if (gameState.Grid[r, c].Value != 0) continue;
                var cand = candidates[r, c];
                if (cand.Count != 2) continue;
                int[] cv = cand.ToArray();
                if (cand.Contains(a) && !cand.Contains(b)) pincers.Add((r, c, a, cv.First(x => x != a)));
                if (cand.Contains(b) && !cand.Contains(a)) pincers.Add((r, c, b, cv.First(x => x != b)));
            }

            foreach (var p1 in pincers)
            {
                foreach (var p2 in pincers)
                {
                    if (p1.Equals(p2)) continue;
                    if (p1.candShared == p2.candShared) continue;
                    int commonElim = p1.candOther == p2.candOther ? p1.candOther : -1;
                    if (commonElim == -1) continue;
                    if (!Sees(pr, pc, p1.row, p1.col) || !Sees(pr, pc, p2.row, p2.col)) continue;

                    for (int r = 0; r < 9; r++)
                    for (int c = 0; c < 9; c++)
                    {
                        if (gameState.Grid[r, c].Value != 0) continue;
                        if (!Sees(r, c, p1.row, p1.col) || !Sees(r, c, p2.row, p2.col)) continue;
                        if (!candidates[r, c].Contains(commonElim)) continue;
                        var remaining = new HashSet<int>(candidates[r, c]);
                        remaining.Remove(commonElim);
                        if (remaining.Count == 1)
                        {
                            int value = GetSingleElement(remaining);
                            results.Add(new Hint
                            {
                                Row = r,
                                Col = c,
                                Value = value,
                                TechniqueId = "XYWing",
                                TechniqueName = LocalizationService.Instance.Get("hint.xy_wing"),
                                TechniqueDescription = LocalizationService.Instance.Get("hint.xy_wing.desc", commonElim),
                                RelatedCells = new List<(int, int)> { (pr, pc), (p1.row, p1.col), (p2.row, p2.col) },
                                Explanation = LocalizationService.Instance.Get("hint.xy_wing.explanation", value)
                            });
                            return;
                        }
                        else if (allowElimination)
                        {
                            results.Add(new Hint
                            {
                                Row = r,
                                Col = c,
                                Value = 0,
                                IsPlacement = false,
                                EliminatedCandidates = new List<int> { commonElim },
                                TechniqueId = "XYWing",
                                TechniqueName = LocalizationService.Instance.Get("hint.xy_wing"),
                                TechniqueDescription = LocalizationService.Instance.Get("hint.xy_wing.desc", commonElim),
                                RelatedCells = new List<(int, int)> { (pr, pc), (p1.row, p1.col), (p2.row, p2.col) },
                                Explanation = LocalizationService.Instance.Get("hint.xy_wing.explanation", commonElim)
                            });
                            return;
                        }
                    }
                }
            }
        }
    }

    private static void CollectXYZWings(SudokuGameState gameState, HashSet<int>[,] candidates, List<Hint> results, bool allowElimination)
    {
        for (int pr = 0; pr < 9; pr++)
        for (int pc = 0; pc < 9; pc++)
        {
            if (gameState.Grid[pr, pc].Value != 0) continue;
            var pivot = candidates[pr, pc];
            if (pivot.Count != 3) continue;
            var pv = pivot.ToArray();
            foreach (int elim in pv)
            {
                var left = pv.Where(x => x != elim).ToArray();
                int a = left[0], b = left[1];
                (int row, int col)? pa = null; (int row, int col)? pb = null;

                for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (gameState.Grid[r, c].Value != 0) continue;
                    if (!Sees(pr, pc, r, c)) continue;
                    var cand = candidates[r, c];
                    if (cand.Count != 2) continue;
                    if (cand.Contains(elim) && cand.Contains(a) && pa == null) pa = (r, c);
                    else if (cand.Contains(elim) && cand.Contains(b) && pb == null) pb = (r, c);
                }

                if (pa == null || pb == null) continue;

                for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (gameState.Grid[r, c].Value != 0) continue;
                    if (!Sees(r, c, pr, pc) || !Sees(r, c, pa.Value.row, pa.Value.col) || !Sees(r, c, pb.Value.row, pb.Value.col)) continue;
                    if (!candidates[r, c].Contains(elim)) continue;
                    var remaining = new HashSet<int>(candidates[r, c]);
                    remaining.Remove(elim);
                    if (remaining.Count == 1)
                    {
                        int value = GetSingleElement(remaining);
                        results.Add(new Hint
                        {
                            Row = r,
                            Col = c,
                            Value = value,
                            TechniqueId = "XYZWing",
                            TechniqueName = LocalizationService.Instance.Get("hint.xyz_wing"),
                            TechniqueDescription = LocalizationService.Instance.Get("hint.xyz_wing.desc", elim),
                            RelatedCells = new List<(int, int)> { (pr, pc), pa.Value, pb.Value },
                            Explanation = LocalizationService.Instance.Get("hint.xyz_wing.explanation", value)
                        });
                        return;
                    }
                    else if (allowElimination)
                    {
                        results.Add(new Hint
                        {
                            Row = r,
                            Col = c,
                            Value = 0,
                            IsPlacement = false,
                            EliminatedCandidates = new List<int> { elim },
                            TechniqueId = "XYZWing",
                            TechniqueName = LocalizationService.Instance.Get("hint.xyz_wing"),
                            TechniqueDescription = LocalizationService.Instance.Get("hint.xyz_wing.desc", elim),
                            RelatedCells = new List<(int, int)> { (pr, pc), pa.Value, pb.Value },
                            Explanation = LocalizationService.Instance.Get("hint.xyz_wing.explanation", elim)
                        });
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Builds a human-friendly explanation for Hidden Single by listing the placed numbers that block other cells.
    /// Returns a tuple: (explanation string, list of blocking cell positions)
    /// </summary>
    private static (string explanation, List<(int row, int col)> blockingCells) BuildHumanFriendlyHiddenSingleExplanation(
        SudokuGameState gameState, int targetRow, int targetCol, int num, string unitType)
    {
        var loc = LocalizationService.Instance;
        var blockingCells = new List<(int row, int col)>();

        // Find all placed instances of 'num' that block other empty cells in the unit
        // For row-based: find 6s in columns that see other empty cells in the same row
        // For col-based: find 6s in rows that see other empty cells in the same column
        // For block-based: find 6s that block other empty cells in the same block

        string targetCellRef = ToCellRef(targetRow, targetCol);

        if (unitType == "row")
        {
            // For this row, which other cells are empty but blocked?
            // Find the placed 'num' values that block them
            for (int col = 0; col < 9; col++)
            {
                if (col == targetCol) continue;
                if (gameState.Grid[targetRow, col].Value != 0) continue; // not empty

                // This cell is empty but can't have 'num' - find why
                // Check column for blocking number
                for (int r = 0; r < 9; r++)
                {
                    if (gameState.Grid[r, col].Value == num && !blockingCells.Contains((r, col)))
                    {
                        blockingCells.Add((r, col));
                    }
                }
                // Check block for blocking number
                int blockStartR = (targetRow / 3) * 3;
                int blockStartC = (col / 3) * 3;
                for (int br = blockStartR; br < blockStartR + 3; br++)
                {
                    for (int bc = blockStartC; bc < blockStartC + 3; bc++)
                    {
                        if (gameState.Grid[br, bc].Value == num && !blockingCells.Contains((br, bc)))
                        {
                            blockingCells.Add((br, bc));
                        }
                    }
                }
            }
        }
        else if (unitType == "col")
        {
            // For this column, which other cells are empty but blocked?
            for (int row = 0; row < 9; row++)
            {
                if (row == targetRow) continue;
                if (gameState.Grid[row, targetCol].Value != 0) continue;

                // Check row for blocking number
                for (int c = 0; c < 9; c++)
                {
                    if (gameState.Grid[row, c].Value == num && !blockingCells.Contains((row, c)))
                    {
                        blockingCells.Add((row, c));
                    }
                }
                // Check block for blocking number
                int blockStartR = (row / 3) * 3;
                int blockStartC = (targetCol / 3) * 3;
                for (int br = blockStartR; br < blockStartR + 3; br++)
                {
                    for (int bc = blockStartC; bc < blockStartC + 3; bc++)
                    {
                        if (gameState.Grid[br, bc].Value == num && !blockingCells.Contains((br, bc)))
                        {
                            blockingCells.Add((br, bc));
                        }
                    }
                }
            }
        }
        else // block
        {
            int blockStartR = (targetRow / 3) * 3;
            int blockStartC = (targetCol / 3) * 3;

            // For this block, which other cells are empty but blocked?
            for (int br = blockStartR; br < blockStartR + 3; br++)
            {
                for (int bc = blockStartC; bc < blockStartC + 3; bc++)
                {
                    if (br == targetRow && bc == targetCol) continue;
                    if (gameState.Grid[br, bc].Value != 0) continue;

                    // Check row for blocking number
                    for (int c = 0; c < 9; c++)
                    {
                        if (gameState.Grid[br, c].Value == num && !blockingCells.Contains((br, c)))
                        {
                            blockingCells.Add((br, c));
                        }
                    }
                    // Check column for blocking number
                    for (int r = 0; r < 9; r++)
                    {
                        if (gameState.Grid[r, bc].Value == num && !blockingCells.Contains((r, bc)))
                        {
                            blockingCells.Add((r, bc));
                        }
                    }
                }
            }
        }

        // Build the explanation string
        if (blockingCells.Count > 0)
        {
            var blockingRefs = blockingCells.Select(bc => ToCellRef(bc.row, bc.col)).ToList();
            string blockingList = string.Join(", ", blockingRefs);

            string explanation = loc.CurrentLanguage == Language.German
                ? $"Die {num} kann nur in {targetCellRef} stehen, weil die {num}en bei {blockingList} alle anderen Zellen blockieren."
                : $"{num} can only go in {targetCellRef} because the {num}s at {blockingList} block all other cells.";

            return (explanation, blockingCells);
        }
        else
        {
            // Fallback to generic explanation
            string explanation = loc.CurrentLanguage == Language.German
                ? $"Die {num} kann in dieser Einheit nur in {targetCellRef} stehen."
                : $"{num} can only go in {targetCellRef} in this unit.";
            return (explanation, blockingCells);
        }
    }

    /// <summary>
    /// Hidden Single: Eine Zahl kann nur an einer Stelle in Zeile/Spalte/Block
    /// </summary>
    private static Hint? FindHiddenSingle(SudokuGameState gameState, bool respectNotes)
    {
        // Prüfe jede Zeile
        for (int row = 0; row < 9; row++)
        {
            for (int num = 1; num <= 9; num++)
            {
                var possibleCols = new List<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (gameState.Grid[row, col].Value == 0 &&
                        CanPlaceNumber(gameState, row, col, num, respectNotes))
                    {
                        possibleCols.Add(col);
                    }
                }

                if (possibleCols.Count == 1)
                {
                    var loc = LocalizationService.Instance;
                    int col = possibleCols[0];
                    var (explanation, blockingCells) = BuildHumanFriendlyHiddenSingleExplanation(gameState, row, col, num, "row");

                    // Include blocking cells in related cells for visual highlighting
                    var relatedCells = new List<(int, int)>(blockingCells);

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = num,
                        TechniqueId = "HiddenSingleRow",
                        TechniqueName = loc.Get("hint.hidden_single.row"),
                        TechniqueDescription = loc.Get("hint.hidden_single.row.desc", num),
                        RelatedCells = relatedCells,
                        Explanation = explanation
                    };
                }
            }
        }

        // Prüfe jede Spalte
        for (int col = 0; col < 9; col++)
        {
            for (int num = 1; num <= 9; num++)
            {
                var possibleRows = new List<int>();
                for (int row = 0; row < 9; row++)
                {
                    if (gameState.Grid[row, col].Value == 0 &&
                        CanPlaceNumber(gameState, row, col, num, respectNotes))
                    {
                        possibleRows.Add(row);
                    }
                }

                if (possibleRows.Count == 1)
                {
                    var loc = LocalizationService.Instance;
                    int row = possibleRows[0];
                    var (explanation, blockingCells) = BuildHumanFriendlyHiddenSingleExplanation(gameState, row, col, num, "col");

                    // Include blocking cells in related cells for visual highlighting
                    var relatedCells = new List<(int, int)>(blockingCells);

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = num,
                        TechniqueId = "HiddenSingleCol",
                        TechniqueName = loc.Get("hint.hidden_single.col"),
                        TechniqueDescription = loc.Get("hint.hidden_single.col.desc", num),
                        RelatedCells = relatedCells,
                        Explanation = explanation
                    };
                }
            }
        }

        // Prüfe jeden 3x3-Block
        for (int blockRow = 0; blockRow < 3; blockRow++)
        {
            for (int blockCol = 0; blockCol < 3; blockCol++)
            {
                for (int num = 1; num <= 9; num++)
                {
                    var possiblePositions = new List<(int row, int col)>();

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row = blockRow * 3 + r;
                            int col = blockCol * 3 + c;

                            if (gameState.Grid[row, col].Value == 0 &&
                                CanPlaceNumber(gameState, row, col, num, respectNotes))
                            {
                                possiblePositions.Add((row, col));
                            }
                        }
                    }

                    if (possiblePositions.Count == 1)
                    {
                        var loc = LocalizationService.Instance;
                        var (row, col) = possiblePositions[0];
                        var (explanation, blockingCells) = BuildHumanFriendlyHiddenSingleExplanation(gameState, row, col, num, "block");

                        // Include blocking cells in related cells for visual highlighting
                        var relatedCells = new List<(int, int)>(blockingCells);

                        return new Hint
                        {
                            Row = row,
                            Col = col,
                            Value = num,
                            TechniqueId = "HiddenSingleBlock",
                            TechniqueName = loc.Get("hint.hidden_single.block"),
                            TechniqueDescription = loc.Get("hint.hidden_single.block.desc", num),
                            RelatedCells = relatedCells,
                            Explanation = explanation
                        };
                    }
                }
            }
        }

        return null;
    }

    private static void CollectHiddenSingles(SudokuGameState gameState, List<Hint> results, bool respectNotes)
    {
        // Rows
        for (int row = 0; row < 9; row++)
        {
            for (int num = 1; num <= 9; num++)
            {
                var possibleCols = new List<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (gameState.Grid[row, col].Value == 0 && CanPlaceNumber(gameState, row, col, num, respectNotes))
                        possibleCols.Add(col);
                }
                if (possibleCols.Count == 1)
                {
                    int col = possibleCols[0];
                    var (explanation, blockingCells) = BuildHumanFriendlyHiddenSingleExplanation(gameState, row, col, num, "row");
                    var relatedCells = new List<(int, int)>(blockingCells);
                    results.Add(new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = num,
                        IsPlacement = true,
                        TechniqueId = "HiddenSingleRow",
                        TechniqueName = LocalizationService.Instance.Get("hint.hidden_single"),
                        TechniqueDescription = LocalizationService.Instance.Get("hint.hidden_single.desc"),
                        RelatedCells = relatedCells,
                        Explanation = explanation
                    });
                }
            }
        }

        // Columns
        for (int col = 0; col < 9; col++)
        {
            for (int num = 1; num <= 9; num++)
            {
                var possibleRows = new List<int>();
                for (int row = 0; row < 9; row++)
                {
                    if (gameState.Grid[row, col].Value == 0 && CanPlaceNumber(gameState, row, col, num, respectNotes))
                        possibleRows.Add(row);
                }
                if (possibleRows.Count == 1)
                {
                    int row = possibleRows[0];
                    var (explanation, blockingCells) = BuildHumanFriendlyHiddenSingleExplanation(gameState, row, col, num, "col");
                    var relatedCells = new List<(int, int)>(blockingCells);
                    results.Add(new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = num,
                        IsPlacement = true,
                        TechniqueId = "HiddenSingleCol",
                        TechniqueName = LocalizationService.Instance.Get("hint.hidden_single"),
                        TechniqueDescription = LocalizationService.Instance.Get("hint.hidden_single.desc"),
                        RelatedCells = relatedCells,
                        Explanation = explanation
                    });
                }
            }
        }

        // Boxes
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                int startRow = boxRow * 3;
                int startCol = boxCol * 3;
                for (int num = 1; num <= 9; num++)
                {
                    var positions = new List<(int r, int c)>();
                    for (int r = startRow; r < startRow + 3; r++)
                    {
                        for (int c = startCol; c < startCol + 3; c++)
                        {
                            if (gameState.Grid[r, c].Value == 0 && CanPlaceNumber(gameState, r, c, num, respectNotes))
                                positions.Add((r, c));
                        }
                    }

                    if (positions.Count == 1)
                    {
                        var (r, c) = positions[0];
                        var (explanation, blockingCells) = BuildHumanFriendlyHiddenSingleExplanation(gameState, r, c, num, "block");
                        var relatedCells = new List<(int, int)>(blockingCells);
                        results.Add(new Hint
                        {
                            Row = r,
                            Col = c,
                            Value = num,
                            IsPlacement = true,
                            TechniqueId = "HiddenSingleBox",
                            TechniqueName = LocalizationService.Instance.Get("hint.hidden_single"),
                            TechniqueDescription = LocalizationService.Instance.Get("hint.hidden_single.desc"),
                            RelatedCells = relatedCells,
                            Explanation = explanation
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Naked Pair: Zwei Zellen mit genau denselben zwei Kandidaten
    /// </summary>
    private static Hint? FindNakedPair(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        // Prüfe jede Zeile
        for (int row = 0; row < 9; row++)
        {
            var pairCells = new List<(int col, HashSet<int> cands)>();

            for (int col = 0; col < 9; col++)
            {
                if (candidates[row, col].Count == 2)
                {
                    pairCells.Add((col, candidates[row, col]));
                }
            }

            // Suche nach zwei Zellen mit gleichen Kandidaten
            for (int i = 0; i < pairCells.Count; i++)
            {
                for (int j = i + 1; j < pairCells.Count; j++)
                {
                    if (pairCells[i].cands.SetEquals(pairCells[j].cands))
                    {
                        var pairNums = new List<int>(pairCells[i].cands);

                        // Prüfe ob diese Kandidaten woanders in der Zeile eliminiert werden können
                        for (int col = 0; col < 9; col++)
                        {
                            if (col != pairCells[i].col && col != pairCells[j].col)
                            {
                                if (candidates[row, col].Contains(pairNums[0]) ||
                                    candidates[row, col].Contains(pairNums[1]))
                                {
                                    // Finde eine Zelle die wir lösen können
                                    var remainingCands = new HashSet<int>(candidates[row, col]);
                                    remainingCands.ExceptWith(pairNums);

                                    if (remainingCands.Count == 1)
                                    {
                                        int value = GetSingleElement(remainingCands);
                                        var relatedCells = new List<(int, int)>
                                        {
                                            (row, pairCells[i].col),
                                            (row, pairCells[j].col)
                                        };

                                        return new Hint
                                        {
                                            Row = row,
                                            Col = col,
                                            Value = value,
                                            TechniqueId = "NakedPair",
                                            TechniqueName = LocalizationService.Instance.Get("hint.naked_pair"),
                                            TechniqueDescription = LocalizationService.Instance.Get("hint.naked_pair.desc", pairNums[0], pairNums[1]),
                                            RelatedCells = relatedCells,
                                            Explanation = LocalizationService.Instance.Get(
                                                "hint.naked_pair.explanation",
                                                pairCells[i].col + 1,
                                                pairCells[j].col + 1,
                                                pairNums[0],
                                                pairNums[1],
                                                value)
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Pointing Pair: Kandidat in Block nur in einer Zeile/Spalte
    /// </summary>
    private static Hint? FindPointingPair(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        for (int blockRow = 0; blockRow < 3; blockRow++)
        {
            for (int blockCol = 0; blockCol < 3; blockCol++)
            {
                for (int num = 1; num <= 9; num++)
                {
                    var positions = new List<(int row, int col)>();

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row = blockRow * 3 + r;
                            int col = blockCol * 3 + c;

                            if (candidates[row, col].Contains(num))
                            {
                                positions.Add((row, col));
                            }
                        }
                    }

                    if (positions.Count >= 2 && positions.Count <= 3)
                    {
                        // Prüfe ob alle in derselben Zeile
                        if (AllSameRow(positions))
                        {
                            int targetRow = positions[0].row;

                            // Prüfe ob der Kandidat außerhalb des Blocks in der Zeile existiert
                            for (int col = 0; col < 9; col++)
                            {
                                if (col / 3 != blockCol && candidates[targetRow, col].Contains(num))
                                {
                                    var remainingCands = new HashSet<int>(candidates[targetRow, col]);
                                    remainingCands.Remove(num);

                                    if (remainingCands.Count == 1)
                                    {
                                        int value = GetSingleElement(remainingCands);
                                        return new Hint
                                        {
                                            Row = targetRow,
                                            Col = col,
                                            Value = value,
                                            TechniqueId = "PointingPair",
                                            TechniqueName = LocalizationService.Instance.Get("hint.pointing_pair"),
                                            TechniqueDescription = LocalizationService.Instance.Get("hint.pointing_pair.row.desc", num),
                                            RelatedCells = positions,
                                            Explanation = LocalizationService.Instance.Get("hint.pointing_pair.row.explanation", num, targetRow + 1)
                                        };
                                    }
                                }
                            }
                        }

                        // Prüfe ob alle in derselben Spalte
                        if (AllSameCol(positions))
                        {
                            int targetCol = positions[0].col;

                            for (int row = 0; row < 9; row++)
                            {
                                if (row / 3 != blockRow && candidates[row, targetCol].Contains(num))
                                {
                                    var remainingCands = new HashSet<int>(candidates[row, targetCol]);
                                    remainingCands.Remove(num);

                                    if (remainingCands.Count == 1)
                                    {
                                        int value = GetSingleElement(remainingCands);
                                        return new Hint
                                        {
                                            Row = row,
                                            Col = targetCol,
                                            Value = value,
                                            TechniqueId = "PointingPair",
                                            TechniqueName = LocalizationService.Instance.Get("hint.pointing_pair"),
                                            TechniqueDescription = LocalizationService.Instance.Get("hint.pointing_pair.col.desc", num),
                                            RelatedCells = positions,
                                            Explanation = LocalizationService.Instance.Get("hint.pointing_pair.col.explanation", num, targetCol + 1)
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Box/Line Reduction: Kandidat in Zeile/Spalte nur in einem Block
    /// </summary>
    private static Hint? FindBoxLineReduction(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        // Prüfe jede Zeile
        for (int row = 0; row < 9; row++)
        {
            for (int num = 1; num <= 9; num++)
            {
                var cols = new List<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (candidates[row, col].Contains(num))
                    {
                        cols.Add(col);
                    }
                }

                if (cols.Count >= 2 && cols.Count <= 3)
                {
                    int blockCol = cols[0] / 3;
                    if (cols.All(c => c / 3 == blockCol))
                    {
                        // Alle Vorkommen sind in einem Block - prüfe ob wir im Block eliminieren können
                        int blockRow = row / 3;

                        for (int r = blockRow * 3; r < blockRow * 3 + 3; r++)
                        {
                            if (r == row) continue;

                            for (int c = blockCol * 3; c < blockCol * 3 + 3; c++)
                            {
                                if (candidates[r, c].Contains(num))
                                {
                                    var remainingCands = new HashSet<int>(candidates[r, c]);
                                    remainingCands.Remove(num);

                                    if (remainingCands.Count == 1)
                                    {
                                        int value = GetSingleElement(remainingCands);
                                        // Build relatedCells without LINQ to avoid closure allocation
                                        var relatedCells = new List<(int, int)>(cols.Count);
                                        foreach (int colIdx in cols)
                                        {
                                            relatedCells.Add((row, colIdx));
                                        }

                                        return new Hint
                                        {
                                            Row = r,
                                            Col = c,
                                            Value = value,
                                            TechniqueId = "BoxLineReduction",
                                            TechniqueName = LocalizationService.Instance.Get("hint.box_line"),
                                            TechniqueDescription = LocalizationService.Instance.Get("hint.box_line.desc", num, row + 1),
                                            RelatedCells = relatedCells,
                                            Explanation = LocalizationService.Instance.Get("hint.box_line.explanation", row + 1, num)
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// X-Wing: Rechteck-Muster
    /// </summary>
    private static Hint? FindXWing(SudokuGameState gameState, HashSet<int>[,] candidates)
    {
        for (int num = 1; num <= 9; num++)
        {
            // Finde Zeilen wo num nur in 2 Spalten vorkommt
            var rowPairs = new List<(int row, int col1, int col2)>();

            for (int row = 0; row < 9; row++)
            {
                var cols = new List<int>();
                for (int col = 0; col < 9; col++)
                {
                    if (candidates[row, col].Contains(num))
                    {
                        cols.Add(col);
                    }
                }

                if (cols.Count == 2)
                {
                    rowPairs.Add((row, cols[0], cols[1]));
                }
            }

            // Suche nach zwei Zeilen mit gleichen Spalten
            for (int i = 0; i < rowPairs.Count; i++)
            {
                for (int j = i + 1; j < rowPairs.Count; j++)
                {
                    if (rowPairs[i].col1 == rowPairs[j].col1 && rowPairs[i].col2 == rowPairs[j].col2)
                    {
                        // X-Wing gefunden! Prüfe ob wir in den Spalten eliminieren können
                        int col1 = rowPairs[i].col1;
                        int col2 = rowPairs[i].col2;

                        for (int row = 0; row < 9; row++)
                        {
                            if (row != rowPairs[i].row && row != rowPairs[j].row)
                            {
                                if (candidates[row, col1].Contains(num) || candidates[row, col2].Contains(num))
                                {
                                    int targetCol = candidates[row, col1].Contains(num) ? col1 : col2;

                                    var remainingCands = new HashSet<int>(candidates[row, targetCol]);
                                    remainingCands.Remove(num);

                                    if (remainingCands.Count == 1)
                                    {
                                        int value = GetSingleElement(remainingCands);
                                        var relatedCells = new List<(int, int)>
                                        {
                                            (rowPairs[i].row, col1),
                                            (rowPairs[i].row, col2),
                                            (rowPairs[j].row, col1),
                                            (rowPairs[j].row, col2)
                                        };

                                        return new Hint
                                        {
                                            Row = row,
                                            Col = targetCol,
                                            Value = value,
                                            TechniqueId = "XWing",
                                            TechniqueName = LocalizationService.Instance.Get("hint.x_wing"),
                                            TechniqueDescription = LocalizationService.Instance.Get("hint.x_wing.desc", num),
                                            RelatedCells = relatedCells,
                                            Explanation = LocalizationService.Instance.Get(
                                                "hint.x_wing.explanation",
                                                num,
                                                rowPairs[i].row + 1,
                                                rowPairs[j].row + 1,
                                                col1 + 1,
                                                col2 + 1)
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Einfacher Fallback: Finde die nächste Zelle mit der Lösung
    /// </summary>
    private static Hint? FindSimpleHint(SudokuGameState gameState)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var cell = gameState.Grid[row, col];
                if (cell.Value == 0)
                {
                    var relatedCells = GetAllRelatedCellsWithValue(gameState, row, col);

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = cell.Solution,
                        TechniqueId = "LogicalAnalysis",
                        TechniqueName = LocalizationService.Instance.Get("hint.logical_analysis"),
                        TechniqueDescription = LocalizationService.Instance.Get("hint.logical_analysis.desc"),
                        RelatedCells = relatedCells,
                        Explanation = LocalizationService.Instance.Get("hint.logical_analysis.explanation", cell.Solution)
                    };
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Holt alle möglichen Zahlen für eine Zelle
    /// </summary>
    private static List<int> GetPossibleNumbers(SudokuGameState gameState, int row, int col, bool respectNotes = false)
    {
        var possible = new List<int>();
        for (int num = 1; num <= 9; num++)
        {
            if (CanPlaceNumber(gameState, row, col, num, respectNotes))
            {
                possible.Add(num);
            }
        }
        return possible;
    }

    /// <summary>
    /// Prüft ob eine Zahl an der Position platziert werden kann
    /// </summary>
    private static bool CanPlaceNumber(SudokuGameState gameState, int row, int col, int num, bool respectNotes = false)
    {
        if (respectNotes)
        {
            var notes = gameState.Grid[row, col].Notes;
            bool anyNotes = false;
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i]) { anyNotes = true; break; }
            }

            if (anyNotes)
            {
                if (num < 1 || num > notes.Length) return false;
                if (!notes[num - 1]) return false;
            }
        }

        // Prüfe Zeile
        for (int c = 0; c < 9; c++)
        {
            if (gameState.Grid[row, c].Value == num) return false;
        }

        // Prüfe Spalte
        for (int r = 0; r < 9; r++)
        {
            if (gameState.Grid[r, col].Value == num) return false;
        }

        // Prüfe 3x3-Block
        int startRow = (row / 3) * 3;
        int startCol = (col / 3) * 3;
        for (int r = startRow; r < startRow + 3; r++)
        {
            for (int c = startCol; c < startCol + 3; c++)
            {
                if (gameState.Grid[r, c].Value == num) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Holt alle Related-Zellen die eine Zahl enthalten (für Visualisierung)
    /// </summary>
    private static List<(int row, int col)> GetAllRelatedCellsWithValue(SudokuGameState gameState, int targetRow, int targetCol)
    {
        var cells = new List<(int row, int col)>();

        // Zeile
        for (int col = 0; col < 9; col++)
        {
            if (col != targetCol && gameState.Grid[targetRow, col].Value != 0)
            {
                cells.Add((targetRow, col));
            }
        }

        // Spalte
        for (int row = 0; row < 9; row++)
        {
            if (row != targetRow && gameState.Grid[row, targetCol].Value != 0)
            {
                cells.Add((row, targetCol));
            }
        }

        // 3x3-Block
        int startRow = (targetRow / 3) * 3;
        int startCol = (targetCol / 3) * 3;
        for (int r = startRow; r < startRow + 3; r++)
        {
            for (int c = startCol; c < startCol + 3; c++)
            {
                if ((r != targetRow || c != targetCol) &&
                    gameState.Grid[r, c].Value != 0 &&
                    !cells.Contains((r, c)))
                {
                    cells.Add((r, c));
                }
            }
        }

        return cells;
    }

    private static List<(int row, int col)> GetRowCells(int targetRow, int excludeCol)
    {
        var cells = new List<(int row, int col)>();
        for (int col = 0; col < 9; col++)
        {
            if (col != excludeCol)
            {
                cells.Add((targetRow, col));
            }
        }
        return cells;
    }

    private static List<(int row, int col)> GetColCells(int excludeRow, int targetCol)
    {
        var cells = new List<(int row, int col)>();
        for (int row = 0; row < 9; row++)
        {
            if (row != excludeRow)
            {
                cells.Add((row, targetCol));
            }
        }
        return cells;
    }

    private static List<(int row, int col)> GetBlockCells(int targetRow, int targetCol)
    {
        var cells = new List<(int row, int col)>();
        int startRow = (targetRow / 3) * 3;
        int startCol = (targetCol / 3) * 3;

        for (int r = startRow; r < startRow + 3; r++)
        {
            for (int c = startCol; c < startCol + 3; c++)
            {
                if (r != targetRow || c != targetCol)
                {
                    cells.Add((r, c));
                }
            }
        }
        return cells;
    }
}
