using System;
using System.Collections.Generic;
using System.Linq;
using MySudoku.Models;

namespace MySudoku.Logic;

/// <summary>
/// Service für Sudoku-Hinweise mit verschiedenen Techniken
/// </summary>
public static class HintService
{
    /// <summary>
    /// Repräsentiert einen Hinweis für eine Zelle
    /// </summary>
    public class Hint
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Value { get; set; }
        public string TechniqueName { get; set; } = "";
        public string TechniqueDescription { get; set; } = "";
        public List<(int row, int col)> RelatedCells { get; set; } = new();
        public string Explanation { get; set; } = "";
    }

    /// <summary>
    /// Findet einen Hinweis für das aktuelle Spielfeld
    /// </summary>
    public static Hint? FindHint(SudokuGameState gameState)
    {
        // Versuche verschiedene Techniken in aufsteigender Schwierigkeit

        // 1. Naked Single - Eine Zelle hat nur eine mögliche Zahl
        var nakedSingle = FindNakedSingle(gameState);
        if (nakedSingle != null) return nakedSingle;

        // 2. Hidden Single - Eine Zahl kann nur an einer Stelle in Zeile/Spalte/Block
        var hiddenSingle = FindHiddenSingle(gameState);
        if (hiddenSingle != null) return hiddenSingle;

        // 3. Naked Pair
        var nakedPair = FindNakedPair(gameState);
        if (nakedPair != null) return nakedPair;

        // 4. Pointing Pair
        var pointingPair = FindPointingPair(gameState);
        if (pointingPair != null) return pointingPair;

        // 5. Box/Line Reduction
        var boxLine = FindBoxLineReduction(gameState);
        if (boxLine != null) return boxLine;

        // 6. X-Wing
        var xwing = FindXWing(gameState);
        if (xwing != null) return xwing;

        // Fallback: Finde einfach die nächste lösbare Zelle
        return FindSimpleHint(gameState);
    }

    /// <summary>
    /// Berechnet die Kandidaten für alle Zellen
    /// </summary>
    public static HashSet<int>[,] CalculateAllCandidates(SudokuGameState gameState)
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
                        if (CanPlaceNumber(gameState, row, col, num))
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
    private static Hint? FindNakedSingle(SudokuGameState gameState)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (gameState.Grid[row, col].Value != 0) continue;

                var possibleNumbers = GetPossibleNumbers(gameState, row, col);

                if (possibleNumbers.Count == 1)
                {
                    int value = possibleNumbers[0];
                    var relatedCells = GetAllRelatedCellsWithValue(gameState, row, col);

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = value,
                        TechniqueName = "Naked Single",
                        TechniqueDescription = "Diese Zelle hat nur eine mögliche Zahl.",
                        RelatedCells = relatedCells,
                        Explanation = $"In dieser Zelle kann nur die {value} stehen, da alle anderen Zahlen (1-9) bereits in der gleichen Zeile, Spalte oder im 3x3-Block vorkommen."
                    };
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Hidden Single: Eine Zahl kann nur an einer Stelle in Zeile/Spalte/Block
    /// </summary>
    private static Hint? FindHiddenSingle(SudokuGameState gameState)
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
                        CanPlaceNumber(gameState, row, col, num))
                    {
                        possibleCols.Add(col);
                    }
                }

                if (possibleCols.Count == 1)
                {
                    int col = possibleCols[0];
                    var relatedCells = GetRowCells(row, col);

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = num,
                        TechniqueName = "Hidden Single (Zeile)",
                        TechniqueDescription = $"Die {num} kann in dieser Zeile nur hier stehen.",
                        RelatedCells = relatedCells,
                        Explanation = $"In Zeile {row + 1} gibt es nur eine Zelle, in der die {num} platziert werden kann. Alle anderen Zellen in der Zeile sind entweder gefüllt oder die {num} wird durch andere Zahlen in deren Spalte oder Block blockiert."
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
                        CanPlaceNumber(gameState, row, col, num))
                    {
                        possibleRows.Add(row);
                    }
                }

                if (possibleRows.Count == 1)
                {
                    int row = possibleRows[0];
                    var relatedCells = GetColCells(row, col);

                    return new Hint
                    {
                        Row = row,
                        Col = col,
                        Value = num,
                        TechniqueName = "Hidden Single (Spalte)",
                        TechniqueDescription = $"Die {num} kann in dieser Spalte nur hier stehen.",
                        RelatedCells = relatedCells,
                        Explanation = $"In Spalte {col + 1} gibt es nur eine Zelle, in der die {num} platziert werden kann. Alle anderen Zellen in der Spalte sind entweder gefüllt oder die {num} wird durch andere Zahlen in deren Zeile oder Block blockiert."
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
                                CanPlaceNumber(gameState, row, col, num))
                            {
                                possiblePositions.Add((row, col));
                            }
                        }
                    }

                    if (possiblePositions.Count == 1)
                    {
                        var (row, col) = possiblePositions[0];
                        var relatedCells = GetBlockCells(row, col);

                        return new Hint
                        {
                            Row = row,
                            Col = col,
                            Value = num,
                            TechniqueName = "Hidden Single (Block)",
                            TechniqueDescription = $"Die {num} kann in diesem 3x3-Block nur hier stehen.",
                            RelatedCells = relatedCells,
                            Explanation = $"Im 3x3-Block gibt es nur eine Zelle, in der die {num} platziert werden kann. Alle anderen Zellen im Block sind entweder gefüllt oder die {num} wird durch andere Zahlen in deren Zeile oder Spalte blockiert."
                        };
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Naked Pair: Zwei Zellen mit genau denselben zwei Kandidaten
    /// </summary>
    private static Hint? FindNakedPair(SudokuGameState gameState)
    {
        var candidates = CalculateAllCandidates(gameState);

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
                                        int value = remainingCands.First();
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
                                            TechniqueName = "Naked Pair",
                                            TechniqueDescription = $"Die Zahlen {pairNums[0]} und {pairNums[1]} bilden ein Naked Pair.",
                                            RelatedCells = relatedCells,
                                            Explanation = $"Die Zellen in Spalte {pairCells[i].col + 1} und {pairCells[j].col + 1} können nur {pairNums[0]} oder {pairNums[1]} enthalten. Daher können diese Zahlen aus anderen Zellen der Zeile eliminiert werden, was hier nur {value} übrig lässt."
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
    private static Hint? FindPointingPair(SudokuGameState gameState)
    {
        var candidates = CalculateAllCandidates(gameState);

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
                        if (positions.All(p => p.row == positions[0].row))
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
                                        int value = remainingCands.First();
                                        return new Hint
                                        {
                                            Row = targetRow,
                                            Col = col,
                                            Value = value,
                                            TechniqueName = "Pointing Pair",
                                            TechniqueDescription = $"Die {num} im Block zeigt auf diese Zeile.",
                                            RelatedCells = positions,
                                            Explanation = $"Im 3x3-Block kommt die {num} nur in Zeile {targetRow + 1} vor. Daher kann die {num} aus anderen Zellen dieser Zeile (außerhalb des Blocks) eliminiert werden."
                                        };
                                    }
                                }
                            }
                        }

                        // Prüfe ob alle in derselben Spalte
                        if (positions.All(p => p.col == positions[0].col))
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
                                        int value = remainingCands.First();
                                        return new Hint
                                        {
                                            Row = row,
                                            Col = targetCol,
                                            Value = value,
                                            TechniqueName = "Pointing Pair",
                                            TechniqueDescription = $"Die {num} im Block zeigt auf diese Spalte.",
                                            RelatedCells = positions,
                                            Explanation = $"Im 3x3-Block kommt die {num} nur in Spalte {targetCol + 1} vor. Daher kann die {num} aus anderen Zellen dieser Spalte (außerhalb des Blocks) eliminiert werden."
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
    private static Hint? FindBoxLineReduction(SudokuGameState gameState)
    {
        var candidates = CalculateAllCandidates(gameState);

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
                                        int value = remainingCands.First();
                                        var relatedCells = cols.Select(c => (row, c)).ToList();

                                        return new Hint
                                        {
                                            Row = r,
                                            Col = c,
                                            Value = value,
                                            TechniqueName = "Box/Line Reduction",
                                            TechniqueDescription = $"Die {num} in Zeile {row + 1} ist auf diesen Block beschränkt.",
                                            RelatedCells = relatedCells,
                                            Explanation = $"In Zeile {row + 1} kommt die {num} nur im Block vor. Daher kann die {num} aus anderen Zellen des Blocks eliminiert werden."
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
    private static Hint? FindXWing(SudokuGameState gameState)
    {
        var candidates = CalculateAllCandidates(gameState);

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
                                        int value = remainingCands.First();
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
                                            TechniqueName = "X-Wing",
                                            TechniqueDescription = $"Ein X-Wing Muster für die {num}.",
                                            RelatedCells = relatedCells,
                                            Explanation = $"Die {num} bildet ein Rechteck-Muster (X-Wing) in den Zeilen {rowPairs[i].row + 1} und {rowPairs[j].row + 1}. In jeder dieser Zeilen muss die {num} in einer der beiden Spalten {col1 + 1} oder {col2 + 1} stehen. Daher kann die {num} aus anderen Zellen dieser Spalten eliminiert werden."
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
                        TechniqueName = "Logische Analyse",
                        TechniqueDescription = "Diese Zelle kann durch Analyse gelöst werden.",
                        RelatedCells = relatedCells,
                        Explanation = $"Durch Überprüfen aller möglichen Zahlen und Eliminieren der unmöglichen Kandidaten ergibt sich die {cell.Solution} als einzig mögliche Lösung für diese Zelle."
                    };
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Holt alle möglichen Zahlen für eine Zelle
    /// </summary>
    private static List<int> GetPossibleNumbers(SudokuGameState gameState, int row, int col)
    {
        var possible = new List<int>();
        for (int num = 1; num <= 9; num++)
        {
            if (CanPlaceNumber(gameState, row, col, num))
            {
                possible.Add(num);
            }
        }
        return possible;
    }

    /// <summary>
    /// Prüft ob eine Zahl an der Position platziert werden kann
    /// </summary>
    private static bool CanPlaceNumber(SudokuGameState gameState, int row, int col, int num)
    {
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
