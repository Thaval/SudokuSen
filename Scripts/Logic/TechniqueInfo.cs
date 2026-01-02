namespace SudokuSen.Logic;

using SudokuSen.Services;
using SudokuSen.Models;

/// <summary>
/// Informationen über Sudoku-Lösungstechniken
/// </summary>
public static class TechniqueInfo
{
    public record TechniqueGroup(string CategoryKey, string[] TechniqueIds);

    public static readonly Difficulty[] DifficultyOrder =
    {
        Difficulty.Kids,
        Difficulty.Easy,
        Difficulty.Medium,
        Difficulty.Hard,
        Difficulty.Insane
    };

    /// <summary>
    /// Beschreibung einer Technik
    /// </summary>
    public record Technique
    {
        public string Id { get; init; } = "";
        public int DifficultyLevel { get; init; } // 1 = Easy, 2 = Medium, 3 = Hard
        public int DefaultDifficulty { get; init; } // Standard-Schwierigkeit für diese Technik

        public string Name
        {
            get
            {
                var loc = LocalizationService.Instance;
                return loc != null ? loc.GetTechniqueName(Id) : Id;
            }
        }

        public string Description
        {
            get
            {
                var loc = LocalizationService.Instance;
                return loc != null ? loc.GetTechniqueDescription(Id) : "";
            }
        }

        public string ShortDescription
        {
            get
            {
                var loc = LocalizationService.Instance;
                return loc != null ? loc.GetTechniqueShort(Id) : Name;
            }
        }
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
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },
        ["HiddenSingleRow"] = new Technique
        {
            Id = "HiddenSingleRow",
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },
        ["HiddenSingleCol"] = new Technique
        {
            Id = "HiddenSingleCol",
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },
        ["HiddenSingleBlock"] = new Technique
        {
            Id = "HiddenSingleBlock",
            DifficultyLevel = 1,
            DefaultDifficulty = 1
        },

        // === MEDIUM TECHNIQUES (Level 2) ===
        ["NakedPair"] = new Technique
        {
            Id = "NakedPair",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["NakedTriple"] = new Technique
        {
            Id = "NakedTriple",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["NakedQuad"] = new Technique
        {
            Id = "NakedQuad",
            DifficultyLevel = 2,
            DefaultDifficulty = 3
        },
        ["HiddenPair"] = new Technique
        {
            Id = "HiddenPair",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["HiddenTriple"] = new Technique
        {
            Id = "HiddenTriple",
            DifficultyLevel = 2,
            DefaultDifficulty = 3
        },
        ["PointingPair"] = new Technique
        {
            Id = "PointingPair",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },
        ["BoxLineReduction"] = new Technique
        {
            Id = "BoxLineReduction",
            DifficultyLevel = 2,
            DefaultDifficulty = 2
        },

        // === HARD TECHNIQUES (Level 3) ===
        ["XWing"] = new Technique
        {
            Id = "XWing",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["Swordfish"] = new Technique
        {
            Id = "Swordfish",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["Jellyfish"] = new Technique
        {
            Id = "Jellyfish",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["XYWing"] = new Technique
        {
            Id = "XYWing",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["XYZWing"] = new Technique
        {
            Id = "XYZWing",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["WWing"] = new Technique
        {
            Id = "WWing",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["Skyscraper"] = new Technique
        {
            Id = "Skyscraper",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["TwoStringKite"] = new Technique
        {
            Id = "TwoStringKite",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["EmptyRectangle"] = new Technique
        {
            Id = "EmptyRectangle",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        ["SimpleColoring"] = new Technique
        {
            Id = "SimpleColoring",
            DifficultyLevel = 3,
            DefaultDifficulty = 3
        },
        // Level 4 - Insane/Insane
        ["UniqueRectangle"] = new Technique
        {
            Id = "UniqueRectangle",
            DifficultyLevel = 4,
            DefaultDifficulty = 4
        },
        ["FinnedXWing"] = new Technique
        {
            Id = "FinnedXWing",
            DifficultyLevel = 4,
            DefaultDifficulty = 4
        },
        ["FinnedSwordfish"] = new Technique
        {
            Id = "FinnedSwordfish",
            DifficultyLevel = 4,
            DefaultDifficulty = 4
        },
        ["RemotePair"] = new Technique
        {
            Id = "RemotePair",
            DifficultyLevel = 4,
            DefaultDifficulty = 4
        },
        ["BUGPlus1"] = new Technique
        {
            Id = "BUGPlus1",
            DifficultyLevel = 4,
            DefaultDifficulty = 4
        },
        ["ALSXZRule"] = new Technique
        {
            Id = "ALSXZRule",
            DifficultyLevel = 4,
            DefaultDifficulty = 4
        },
        ["ForcingChain"] = new Technique
        {
            Id = "ForcingChain",
            DifficultyLevel = 4,
            DefaultDifficulty = 4
        }
    };

    /// <summary>
    /// Liste aller Technik-IDs in Reihenfolge
    /// </summary>
    public static readonly string[] AllTechniqueIds = new[]
    {
        // Easy (Level 1)
        "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock",
        // Medium (Level 2)
        "NakedPair", "NakedTriple", "NakedQuad", "HiddenPair", "HiddenTriple", "PointingPair", "BoxLineReduction",
        // Hard (Level 3)
        "XWing", "Swordfish", "Jellyfish", "XYWing", "XYZWing", "WWing", "Skyscraper", "TwoStringKite", "EmptyRectangle", "SimpleColoring",
        // Insane (Level 4)
        "UniqueRectangle", "FinnedXWing", "FinnedSwordfish", "RemotePair", "BUGPlus1", "ALSXZRule", "ForcingChain"
    };

    /// <summary>
    /// Standard-Techniken pro Schwierigkeit
    /// </summary>
    public static readonly Dictionary<Difficulty, HashSet<string>> DefaultTechniquesPerDifficulty = new()
    {
        [Difficulty.Kids] = new HashSet<string> { "NakedSingle" },
        [Difficulty.Easy] = new HashSet<string> { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock" },
        [Difficulty.Medium] = new HashSet<string> { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock", "NakedPair", "NakedTriple", "HiddenPair", "PointingPair", "BoxLineReduction" },
        [Difficulty.Hard] = new HashSet<string> { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock", "NakedPair", "NakedTriple", "NakedQuad", "HiddenPair", "HiddenTriple", "PointingPair", "BoxLineReduction", "XWing", "Swordfish", "XYWing" },
        [Difficulty.Insane] = new HashSet<string> {
            // Alle vorherigen Techniken
            "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock",
            "NakedPair", "NakedTriple", "NakedQuad", "HiddenPair", "HiddenTriple", "PointingPair", "BoxLineReduction",
            "XWing", "Swordfish", "Jellyfish", "XYWing", "XYZWing", "WWing", "Skyscraper", "TwoStringKite", "EmptyRectangle", "SimpleColoring",
            // Plus Level 4 Techniken
            "UniqueRectangle", "FinnedXWing", "FinnedSwordfish", "RemotePair", "BUGPlus1", "ALSXZRule", "ForcingChain"
        }
    };

    /// <summary>
    /// Groups used by practice/scenario menus (single source of truth for technique buttons).
    /// </summary>
    public static readonly TechniqueGroup[] PracticeGroups = new TechniqueGroup[]
    {
        new("scenarios.category.easy", new[] { "NakedSingle", "HiddenSingleRow", "HiddenSingleCol", "HiddenSingleBlock" }),
        new("scenarios.category.medium", new[] { "NakedPair", "NakedTriple", "HiddenPair", "PointingPair", "BoxLineReduction" }),
        new("scenarios.category.hard", new[] { "XWing", "Swordfish", "XYWing", "Skyscraper", "SimpleColoring" }),
        new("scenarios.category.insane", new[] { "UniqueRectangle", "FinnedXWing", "FinnedSwordfish", "RemotePair", "BUGPlus1", "ALSXZRule", "ForcingChain" })
    };

    public static HashSet<string> GetDefaultTechniques(Difficulty difficulty)
    {
        return DefaultTechniquesPerDifficulty.TryGetValue(difficulty, out var set)
            ? new HashSet<string>(set)
            : new HashSet<string>();
    }

    public static HashSet<string> GetConfiguredTechniques(SettingsData settings, Difficulty difficulty)
    {
        return settings.GetTechniquesForDifficulty(difficulty) ?? GetDefaultTechniques(difficulty);
    }

    public static HashSet<string> GetCumulativeTechniques(SettingsData settings, Difficulty upToDifficulty, bool includeUpTo = true)
    {
        var result = new HashSet<string>();

        foreach (var diff in DifficultyOrder)
        {
            if (!includeUpTo && diff == upToDifficulty)
                break;

            result.UnionWith(GetConfiguredTechniques(settings, diff));

            if (diff == upToDifficulty)
                break;
        }

        return result;
    }

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
        var loc = LocalizationService.Instance;
        if (loc == null)
        {
            // Fallback: keep output stable even if localization isn't ready yet.
            return difficulty.ToString();
        }

        var techIds = enabledTechniques ?? DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>());
        var lines = new List<string>();

        lines.Add(loc.GetDifficultyDisplay(difficulty));
        lines.Add("━━━━━━━━━━━━━━━");
        lines.Add(loc.Get("settings.techniques.active"));
        lines.Add("");

        foreach (var id in AllTechniqueIds)
        {
            if (techIds.Contains(id) && Techniques.TryGetValue(id, out var tech))
            {
                lines.Add($"• {loc.GetTechniqueName(id)}");
                lines.Add($"  {loc.GetTechniqueShort(id)}");
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
        var loc = LocalizationService.Instance;
        if (loc == null) return difficulty.ToString();

        if (difficulty == Difficulty.Kids) return loc.Get("difficulty.kids.desc");

        var techIds = enabledTechniques ?? DefaultTechniquesPerDifficulty.GetValueOrDefault(difficulty, new HashSet<string>());

        // Zeige nur die "besonderen" Techniken für diese Schwierigkeit (nicht die vererbten)
        var previousTechIds = difficulty switch
        {
            Difficulty.Easy => new HashSet<string>(),
            Difficulty.Medium => DefaultTechniquesPerDifficulty[Difficulty.Easy],
            Difficulty.Hard => DefaultTechniquesPerDifficulty[Difficulty.Medium],
            Difficulty.Insane => DefaultTechniquesPerDifficulty[Difficulty.Hard],
            _ => new HashSet<string>()
        };

        var uniqueTechs = new List<string>();
        foreach (var id in AllTechniqueIds)
        {
            if (techIds.Contains(id) && !previousTechIds.Contains(id))
            {
                uniqueTechs.Add(loc.GetTechniqueName(id));
            }
        }

        if (uniqueTechs.Count == 0)
        {
            return difficulty switch
            {
                Difficulty.Easy => loc.Get("difficulty.easy.desc"),
                Difficulty.Medium => loc.Get("difficulty.medium.desc"),
                Difficulty.Hard => loc.Get("difficulty.hard.desc"),
                Difficulty.Insane => loc.Get("difficulty.insane.desc"),
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
