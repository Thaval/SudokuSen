using System.Collections.Generic;
using MySudoku.Models;

namespace MySudoku.Logic;

/// <summary>
/// Informationen über Sudoku-Lösungstechniken
/// </summary>
public static class TechniqueInfo
{
    /// <summary>
    /// Beschreibung einer Technik
    /// </summary>
    public class Technique
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public int DifficultyLevel { get; set; } // 1 = Easy, 2 = Medium, 3 = Hard
    }

    /// <summary>
    /// Alle verfügbaren Techniken
    /// </summary>
    public static readonly Dictionary<string, Technique> Techniques = new()
    {
        ["NakedSingle"] = new Technique
        {
            Name = "Naked Single",
            ShortDescription = "Nur eine Zahl möglich",
            Description = "Eine Zelle hat nur eine mögliche Zahl, da alle anderen durch Zeile, Spalte oder Block ausgeschlossen sind.",
            DifficultyLevel = 1
        },
        ["HiddenSingleRow"] = new Technique
        {
            Name = "Hidden Single (Zeile)",
            ShortDescription = "Zahl nur an einer Stelle in Zeile",
            Description = "Eine Zahl kann in einer Zeile nur an einer einzigen Position platziert werden.",
            DifficultyLevel = 1
        },
        ["HiddenSingleCol"] = new Technique
        {
            Name = "Hidden Single (Spalte)",
            ShortDescription = "Zahl nur an einer Stelle in Spalte",
            Description = "Eine Zahl kann in einer Spalte nur an einer einzigen Position platziert werden.",
            DifficultyLevel = 1
        },
        ["HiddenSingleBlock"] = new Technique
        {
            Name = "Hidden Single (Block)",
            ShortDescription = "Zahl nur an einer Stelle im Block",
            Description = "Eine Zahl kann in einem 3x3-Block nur an einer einzigen Position platziert werden.",
            DifficultyLevel = 1
        },
        ["NakedPair"] = new Technique
        {
            Name = "Naked Pair",
            ShortDescription = "Zwei Zellen mit gleichen zwei Kandidaten",
            Description = "Zwei Zellen in einer Einheit haben genau dieselben zwei Kandidaten. Diese Zahlen können aus anderen Zellen der Einheit eliminiert werden.",
            DifficultyLevel = 2
        },
        ["NakedTriple"] = new Technique
        {
            Name = "Naked Triple",
            ShortDescription = "Drei Zellen mit drei gemeinsamen Kandidaten",
            Description = "Drei Zellen in einer Einheit teilen sich maximal drei Kandidaten. Diese können aus anderen Zellen eliminiert werden.",
            DifficultyLevel = 2
        },
        ["HiddenPair"] = new Technique
        {
            Name = "Hidden Pair",
            ShortDescription = "Zwei Zahlen nur in zwei Zellen",
            Description = "Zwei Zahlen kommen in einer Einheit nur in genau zwei Zellen vor. Andere Kandidaten in diesen Zellen können eliminiert werden.",
            DifficultyLevel = 2
        },
        ["PointingPair"] = new Technique
        {
            Name = "Pointing Pair",
            ShortDescription = "Block-Zeile/Spalte Interaktion",
            Description = "Wenn eine Zahl in einem Block nur in einer Zeile/Spalte vorkommt, kann sie aus dem Rest dieser Zeile/Spalte eliminiert werden.",
            DifficultyLevel = 2
        },
        ["BoxLineReduction"] = new Technique
        {
            Name = "Box/Line Reduction",
            ShortDescription = "Zeile/Spalte-Block Interaktion",
            Description = "Wenn eine Zahl in einer Zeile/Spalte nur in einem Block vorkommt, kann sie aus dem Rest des Blocks eliminiert werden.",
            DifficultyLevel = 2
        },
        ["XWing"] = new Technique
        {
            Name = "X-Wing",
            ShortDescription = "Rechteck-Muster in zwei Zeilen/Spalten",
            Description = "Wenn eine Zahl in zwei Zeilen nur in den gleichen zwei Spalten vorkommt, kann sie aus diesen Spalten in anderen Zeilen eliminiert werden.",
            DifficultyLevel = 3
        },
        ["Swordfish"] = new Technique
        {
            Name = "Swordfish",
            ShortDescription = "Erweitertes X-Wing mit drei Linien",
            Description = "Eine Erweiterung von X-Wing mit drei Zeilen und drei Spalten.",
            DifficultyLevel = 3
        },
        ["XYWing"] = new Technique
        {
            Name = "XY-Wing",
            ShortDescription = "Drei Zellen mit Pivot",
            Description = "Drei Zellen mit je zwei Kandidaten bilden ein Y-Muster. Die gemeinsame Zahl kann aus Zellen eliminiert werden, die alle drei sehen.",
            DifficultyLevel = 3
        }
    };

    /// <summary>
    /// Holt die Techniken für eine Schwierigkeit
    /// </summary>
    public static List<Technique> GetTechniquesForDifficulty(Difficulty difficulty)
    {
        var result = new List<Technique>();
        int maxLevel = difficulty switch
        {
            Difficulty.Easy => 1,
            Difficulty.Medium => 2,
            Difficulty.Hard => 3,
            _ => 1
        };

        foreach (var tech in Techniques.Values)
        {
            if (tech.DifficultyLevel <= maxLevel)
            {
                result.Add(tech);
            }
        }

        return result;
    }

    /// <summary>
    /// Erstellt einen Tooltip-Text für eine Schwierigkeit
    /// </summary>
    public static string GetDifficultyTooltip(Difficulty difficulty)
    {
        var lines = new List<string>();

        switch (difficulty)
        {
            case Difficulty.Easy:
                lines.Add("🟢 LEICHT");
                lines.Add("━━━━━━━━━━━━━━━");
                lines.Add("Benötigte Techniken:");
                lines.Add("");
                lines.Add("• Naked Single");
                lines.Add("  Eine Zelle hat nur eine mögliche Zahl");
                lines.Add("");
                lines.Add("• Hidden Single");
                lines.Add("  Eine Zahl passt nur an eine Stelle");
                lines.Add("");
                lines.Add("━━━━━━━━━━━━━━━");
                lines.Add("~35-40 vorgegebene Zahlen");
                break;

            case Difficulty.Medium:
                lines.Add("🟠 MITTEL");
                lines.Add("━━━━━━━━━━━━━━━");
                lines.Add("Benötigte Techniken:");
                lines.Add("");
                lines.Add("• Alle Easy-Techniken");
                lines.Add("");
                lines.Add("• Naked Pair/Triple");
                lines.Add("  Kandidaten-Eliminierung");
                lines.Add("");
                lines.Add("• Pointing Pair");
                lines.Add("  Block-Zeile Interaktion");
                lines.Add("");
                lines.Add("• Box/Line Reduction");
                lines.Add("  Zeile-Block Interaktion");
                lines.Add("");
                lines.Add("━━━━━━━━━━━━━━━");
                lines.Add("~28-34 vorgegebene Zahlen");
                break;

            case Difficulty.Hard:
                lines.Add("🔴 SCHWER");
                lines.Add("━━━━━━━━━━━━━━━");
                lines.Add("Benötigte Techniken:");
                lines.Add("");
                lines.Add("• Alle Medium-Techniken");
                lines.Add("");
                lines.Add("• X-Wing");
                lines.Add("  Rechteck-Muster Eliminierung");
                lines.Add("");
                lines.Add("• Swordfish");
                lines.Add("  Erweitertes X-Wing");
                lines.Add("");
                lines.Add("• XY-Wing");
                lines.Add("  Drei-Zellen-Pivot-Muster");
                lines.Add("");
                lines.Add("━━━━━━━━━━━━━━━");
                lines.Add("~22-27 vorgegebene Zahlen");
                break;
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Erstellt eine kurze Technik-Liste für Anzeige unter den Buttons
    /// </summary>
    public static string GetShortTechniqueList(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Naked Single, Hidden Single",
            Difficulty.Medium => "Naked Pair, Pointing Pair, Box/Line",
            Difficulty.Hard => "X-Wing, Swordfish, XY-Wing",
            _ => ""
        };
    }
}
