namespace MySudoku.Logic;

/// <summary>
/// Informationen über Sudoku-Lösungstechniken
/// </summary>
public static class TechniqueInfo
{
    /// <summary>
    /// Beschreibung einer Technik
    /// </summary>
    public record Technique
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string ShortDescription { get; init; } = "";
        public int DifficultyLevel { get; init; } // 1 = Easy, 2 = Medium, 3 = Hard
        public int DefaultDifficulty { get; init; } // Standard-Schwierigkeit für diese Technik
    }

    /// <summary>
    /// Alle verfügbaren Techniken
    /// </summary>
    public static readonly Dictionary<string, Technique> Techniques = new()
    {
        // === EASY TECHNIQUES (Level 1) ===
        ["NakedSingle"] = new Technique
        {
            Id = "NakedSingle",
            Name = "Naked Single",
            ShortDescription = "Nur eine Zahl möglich",
            Description = "Eine Zelle hat nur eine mögliche Zahl, da alle anderen durch Zeile, Spalte oder Block ausgeschlossen sind.",
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },
        ["HiddenSingleRow"] = new Technique
        {
            Id = "HiddenSingleRow",
            Name = "Hidden Single (Zeile)",
            ShortDescription = "Zahl nur an einer Stelle in Zeile",
            Description = "Eine Zahl kann in einer Zeile nur an einer einzigen Position platziert werden.",
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },
        ["HiddenSingleCol"] = new Technique
        {
            Id = "HiddenSingleCol",
            Name = "Hidden Single (Spalte)",
            ShortDescription = "Zahl nur an einer Stelle in Spalte",
            Description = "Eine Zahl kann in einer Spalte nur an einer einzigen Position platziert werden.",
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },
        ["HiddenSingleBlock"] = new Technique
        {
            Id = "HiddenSingleBlock",
            Name = "Hidden Single (Block)",
            ShortDescription = "Zahl nur an einer Stelle im Block",
            Description = "Eine Zahl kann in einem 3x3-Block nur an einer einzigen Position platziert werden.",
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },

        // === MEDIUM TECHNIQUES (Level 2) ===
        ["NakedPair"] = new Technique
        {
            Id = "NakedPair",
            Name = "Naked Pair",
            ShortDescription = "Zwei Zellen mit gleichen zwei Kandidaten",
            Description = "Zwei Zellen in einer Einheit haben genau dieselben zwei Kandidaten. Diese Zahlen können aus anderen Zellen der Einheit eliminiert werden.",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["NakedTriple"] = new Technique
        {
            Id = "NakedTriple",
            Name = "Naked Triple",
            ShortDescription = "Drei Zellen mit drei gemeinsamen Kandidaten",
            Description = "Drei Zellen in einer Einheit teilen sich maximal drei Kandidaten. Diese können aus anderen Zellen eliminiert werden.",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["NakedQuad"] = new Technique
        {
            Id = "NakedQuad",
            Name = "Naked Quad",
            ShortDescription = "Vier Zellen mit vier gemeinsamen Kandidaten",
            Description = "Vier Zellen in einer Einheit teilen sich maximal vier Kandidaten. Diese können aus anderen Zellen eliminiert werden.",
            DifficultyLevel = 2,
            DefaultDifficulty = 3
        },
        ["HiddenPair"] = new Technique
        {
            Id = "HiddenPair",
            Name = "Hidden Pair",
            ShortDescription = "Zwei Zahlen nur in zwei Zellen",
            Description = "Zwei Zahlen kommen in einer Einheit nur in genau zwei Zellen vor. Andere Kandidaten in diesen Zellen können eliminiert werden.",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["HiddenTriple"] = new Technique
        {
            Id = "HiddenTriple",
            Name = "Hidden Triple",
            ShortDescription = "Drei Zahlen nur in drei Zellen",
            Description = "Drei Zahlen kommen in einer Einheit nur in genau drei Zellen vor. Andere Kandidaten in diesen Zellen können eliminiert werden.",
            DifficultyLevel = 2,
            DefaultDifficulty = 3
        },
        ["PointingPair"] = new Technique
        {
            Id = "PointingPair",
            Name = "Pointing Pair",
            ShortDescription = "Block-Zeile/Spalte Interaktion",
            Description = "Wenn eine Zahl in einem Block nur in einer Zeile/Spalte vorkommt, kann sie aus dem Rest dieser Zeile/Spalte eliminiert werden.",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["BoxLineReduction"] = new Technique
        {
            Id = "BoxLineReduction",
            Name = "Box/Line Reduction",
            ShortDescription = "Zeile/Spalte-Block Interaktion",
            Description = "Wenn eine Zahl in einer Zeile/Spalte nur in einem Block vorkommt, kann sie aus dem Rest des Blocks eliminiert werden.",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },

        // === HARD TECHNIQUES (Level 3) ===
        ["XWing"] = new Technique
        {
            Id = "XWing",
            Name = "X-Wing",
            ShortDescription = "Rechteck-Muster in zwei Zeilen/Spalten",
            Description = "Wenn eine Zahl in zwei Zeilen nur in den gleichen zwei Spalten vorkommt, kann sie aus diesen Spalten in anderen Zeilen eliminiert werden.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["Swordfish"] = new Technique
        {
            Id = "Swordfish",
            Name = "Swordfish",
            ShortDescription = "Erweitertes X-Wing mit drei Linien",
            Description = "Eine Erweiterung von X-Wing mit drei Zeilen und drei Spalten.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["Jellyfish"] = new Technique
        {
            Id = "Jellyfish",
            Name = "Jellyfish",
            ShortDescription = "Erweitertes Swordfish mit vier Linien",
            Description = "Eine Erweiterung von Swordfish mit vier Zeilen und vier Spalten.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["XYWing"] = new Technique
        {
            Id = "XYWing",
            Name = "XY-Wing",
            ShortDescription = "Drei Zellen mit Pivot",
            Description = "Drei Zellen mit je zwei Kandidaten bilden ein Y-Muster. Die gemeinsame Zahl kann aus Zellen eliminiert werden, die alle drei sehen.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["XYZWing"] = new Technique
        {
            Id = "XYZWing",
            Name = "XYZ-Wing",
            ShortDescription = "XY-Wing mit drei Kandidaten im Pivot",
            Description = "Wie XY-Wing, aber der Pivot hat drei Kandidaten. Eliminierungen nur in Zellen, die alle drei sehen.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["WWing"] = new Technique
        {
            Id = "WWing",
            Name = "W-Wing",
            ShortDescription = "Zwei Bi-Value Zellen mit Strong Link",
            Description = "Zwei Zellen mit identischen zwei Kandidaten, verbunden durch einen Strong Link auf einem Kandidaten.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["Skyscraper"] = new Technique
        {
            Id = "Skyscraper",
            Name = "Skyscraper",
            ShortDescription = "Zwei Konjugierte Paare mit gemeinsamer Basis",
            Description = "Zwei Spalten mit je genau zwei Kandidaten einer Zahl, die eine Zeile teilen.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["TwoStringKite"] = new Technique
        {
            Id = "TwoStringKite",
            Name = "2-String Kite",
            ShortDescription = "Zeile+Spalte Konjugate im Block",
            Description = "Ein Kandidat bildet ein Konjugat-Paar in einer Zeile UND einer Spalte, die sich in einem Block treffen.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["EmptyRectangle"] = new Technique
        {
            Id = "EmptyRectangle",
            Name = "Empty Rectangle",
            ShortDescription = "L-Form im Block mit Konjugat-Paar",
            Description = "Ein Kandidat bildet eine L-Form in einem Block und interagiert mit einem Konjugat-Paar.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["SimpleColoring"] = new Technique
        {
            Id = "SimpleColoring",
            Name = "Simple Coloring",
            ShortDescription = "Ketten-Färbung für Eliminierungen",
            Description = "Konjugat-Paare werden abwechselnd gefärbt um Widersprüche oder Eliminierungen zu finden.",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        }
    };

    /// <summary>
    /// Liste aller Technik-IDs in Reihenfolge
    /// </summary>
    public static readonly string[] AllTechniqueIds = new[]
    {
        // Easy
        "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock",
        // Medium
        "NakedPair", "NakedTriple", "NakedQuad", "HiddenPair", "HiddenTriple", "PointingPair", "BoxLineReduction",
        // Hard
        "XWing", "Swordfish", "Jellyfish", "XYWing", "XYZWing", "WWing", "Skyscraper", "TwoStringKite", "EmptyRectangle", "SimpleColoring"
    };

    /// <summary>
    /// Standard-Techniken pro Schwierigkeit
    /// </summary>
    public static readonly Dictionary<Difficulty, HashSet<string>> DefaultTechniquesPerDifficulty = new()
    {
        [Difficulty.Kids] = new HashSet<string> { "NakedSingle" },
        [Difficulty.Easy] = new HashSet<string> { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock" },
        [Difficulty.Medium] = new HashSet<string> { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock", "NakedPair", "NakedTriple", "HiddenPair", "PointingPair", "BoxLineReduction" },
        [Difficulty.Hard] = new HashSet<string> { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock", "NakedPair", "NakedTriple", "NakedQuad", "HiddenPair", "HiddenTriple", "PointingPair", "BoxLineReduction", "XWing", "Swordfish", "XYWing" }
    };

    /// <summary>
    /// Holt die Techniken für eine Schwierigkeit (aus Einstellungen oder Standard)
    /// </summary>
    public static List<Technique> GetTechniquesForDifficulty(Difficulty difficulty, HashSet<string>? enabledTechniques = null)
    {
        var techIds = enabledTechniques ?? DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>());
        var result = new List<Technique>();

        foreach (var id in AllTechniqueIds)
        {
            if (techIds.Contains(id) && Techniques.TryGetValue(id, out var tech))
            {
                result.Add(tech);
            }
        }

        return result;
    }

    /// <summary>
    /// Holt alle Techniken einer bestimmten Schwierigkeitsstufe
    /// </summary>
    public static List<Technique> GetTechniquesByLevel(int level)
    {
        return Techniques.Values.Where(t => t.DifficultyLevel == level).ToList();
    }

    /// <summary>
    /// Erstellt einen Tooltip-Text für eine Schwierigkeit basierend auf konfigurierten Techniken
    /// </summary>
    public static string GetDifficultyTooltip(Difficulty difficulty, HashSet<string>? enabledTechniques = null)
    {
        var techIds = enabledTechniques ?? DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>());
        var lines = new List<string>();

        string diffName = difficulty switch
        {
            Difficulty.Kids => "🟦 KIDS",
            Difficulty.Easy => "🟢 LEICHT",
            Difficulty.Medium => "🟠 MITTEL",
            Difficulty.Hard => "🔴 SCHWER",
            _ => "UNBEKANNT"
        };

        lines.Add(diffName);
        lines.Add("━━━━━━━━━━━━━━━");
        lines.Add("Aktive Techniken:");
        lines.Add("");

        foreach (var id in AllTechniqueIds)
        {
            if (techIds.Contains(id) && Techniques.TryGetValue(id, out var tech))
            {
                lines.Add($"• {tech.Name}");
                lines.Add($"  {tech.ShortDescription}");
                lines.Add("");
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Erstellt eine kurze Technik-Liste für Anzeige unter den Buttons
    /// </summary>
    public static string GetShortTechniqueList(Difficulty difficulty, HashSet<string>? enabledTechniques = null)
    {
        if (difficulty == Difficulty.Kids)
        {
            return "4x4 Raster, Zahlen 1-4";
        }

        var techIds = enabledTechniques ?? DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>());

        // Zeige nur die "besonderen" Techniken für diese Schwierigkeit (nicht die vererbten)
        var previousTechIds = difficulty switch
        {
            Difficulty.Easy => new HashSet<string>(),
            Difficulty.Medium => DefaultTechniquesPerDifficulty[Difficulty.Easy],
            Difficulty.Hard => DefaultTechniquesPerDifficulty[Difficulty.Medium],
            _ => new HashSet<string>()
        };

        var uniqueTechs = new List<string>();
        foreach (var id in AllTechniqueIds)
        {
            if (techIds.Contains(id) && !previousTechIds.Contains(id) && Techniques.TryGetValue(id, out var tech))
            {
                uniqueTechs.Add(tech.Name);
            }
        }

        if (uniqueTechs.Count == 0)
        {
            return difficulty switch
            {
                Difficulty.Easy => "Naked Single, Hidden Single",
                Difficulty.Medium => "Naked Pair, Pointing Pair, Box/Line",
                Difficulty.Hard => "X-Wing, Swordfish, XY-Wing",
                _ => ""
            };
        }

        // Zeige max 3 Techniken
        if (uniqueTechs.Count > 3)
        {
            return string.Join(", ", uniqueTechs.Take(3)) + "...";
        }

        return string.Join(", ", uniqueTechs);
    }
}
