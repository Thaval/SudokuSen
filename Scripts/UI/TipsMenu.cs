namespace SudokuSen.UI;

/// <summary>
/// Tipps und Tricks Menü mit visuellen Sudoku-Board Beispielen
/// </summary>
public partial class TipsMenu : Control
{
    private Button _backButton = null!;
    private Label _title = null!;
    private PanelContainer _panel = null!;
    private Label _tipTitle = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _scrollContent = null!;
    private Button _prevButton = null!;
    private Button _nextButton = null!;
    private Label _pageLabel = null!;

    // Cached service references
    private ThemeService _themeService = null!;
    private AudioService _audioService = null!;
    private AppState _appState = null!;
    private LocalizationService _localizationService = null!;

    private int _currentTip = 0;

    // Tip-Daten mit Board-Konfigurationen
    private static readonly TipData[] Tips = new[]
    {
        // Basic Techniques (Easy)
        CreateNakedSingleTip(),
        CreateHiddenSingleTip(),
        CreateScanningTip(),
        CreateCandidatesTip(),
        // Medium Techniques
        CreateNakedPairTip(),
        CreateNakedTripleTip(),
        CreateNakedQuadTip(),
        CreateHiddenPairTip(),
        CreateHiddenTripleTip(),
        CreatePointingPairTip(),
        CreateBoxLineTip(),
        // Hard Techniques
        CreateXWingTip(),
        CreateSwordfishTip(),
        CreateJellyfishTip(),
        CreateYWingTip(),
        CreateXYZWingTip(),
        CreateWWingTip(),
        CreateSkyscraperTip(),
        CreateTwoStringKiteTip(),
        CreateEmptyRectangleTip(),
        CreateSimpleColoringTip(),
        // Insane Techniques (Level 4)
        CreateUniqueRectangleTip(),
        CreateFinnedXWingTip(),
        CreateFinnedSwordfishTip(),
        CreateRemotePairTip(),
        CreateBugPlus1Tip(),
        CreateAlsXzRuleTip(),
        CreateForcingChainTip(),
        // General Tips
        CreateGeneralStrategiesTip(),
        CreateKeyboardShortcutsTip(),
        CreateMultiSelectTip(),
        CreatePracticeTip(),
        CreateAvoidMistakesTip()
    };

    private record LocalizedText(string German, string English)
    {
        public string Get(Language language) => language == Language.German ? German : English;
    }

    private static LocalizedText LT(string german, string english) => new(german, english);

    private record TipData(
        LocalizedText Title,
        LocalizedText ContentBefore,
        MiniGridData? Grid,
        LocalizedText ContentAfter
    );

    private record MiniGridData(
        int[,] Values,
        bool[,] IsGiven,
        HashSet<(int row, int col)> HighlightedCells,
        HashSet<(int row, int col)> RelatedCells,
        (int row, int col, int value)? SolutionCell = null,
        Dictionary<(int row, int col), int[]>? Candidates = null
    );

    #region Tip Data Creation

    private static TipData CreateNakedSingleTip()
    {
        var values = new int[,] {
            { 1, 2, 3 },
            { 4, 0, 6 },
            { 7, 8, 9 }
        };
        var isGiven = new bool[,] {
            { true, true, true },
            { true, false, true },
            { true, true, true }
        };
        var highlighted = new HashSet<(int, int)> { (1, 1) };
        var related = new HashSet<(int, int)> { (0, 1), (2, 1), (1, 0), (1, 2) };

        return new TipData(
            LT("Naked Single (Einziger Kandidat)", "Naked Single (Only Candidate)"),
            LT(
                "[b][color=#4fc3f7]Was ist ein Naked Single?[/color][/b]\n\n" +
                "Eine Zelle hat nur [b]eine einzige mögliche Zahl[/b], weil alle anderen 8 Zahlen bereits in der gleichen Zeile, Spalte oder im 3x3-Block vorkommen.\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]What is a Naked Single?[/color][/b]\n\n" +
                "A cell has [b]only one possible number[/b] because the other 8 numbers already appear in the same row, column, or 3x3 box.\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, (1, 1, 5)),
            LT(
                "[b][color=#81c784]Analyse der markierten Zelle:[/color][/b]\n\n" +
                "Die Zeile enthaelt: 4, 6\n" +
                "Die Spalte enthaelt: 2, 8\n" +
                "Der Block enthaelt: 1, 2, 3, 4, 6, 7, 8, 9\n\n" +
                "[b]Ausgeschlossen:[/b] 1, 2, 3, 4, 6, 7, 8, 9\n" +
                "[b]Loesung:[/b] [color=#4caf50][b]5[/b][/color]\n\n" +
                "[b][color=#4fc3f7]So gehst du vor:[/color][/b]\n" +
                "1. Waehle eine leere Zelle\n" +
                "2. Pruefe alle Zahlen 1-9\n" +
                "3. Streiche aus, was in Zeile/Spalte/Block vorkommt\n" +
                "4. Bleibt nur eine Zahl? Eintragen!",
                "[b][color=#81c784]Analysis of the highlighted cell:[/color][/b]\n\n" +
                "The row contains: 4, 6\n" +
                "The column contains: 2, 8\n" +
                "The box contains: 1, 2, 3, 4, 6, 7, 8, 9\n\n" +
                "[b]Excluded:[/b] 1, 2, 3, 4, 6, 7, 8, 9\n" +
                "[b]Solution:[/b] [color=#4caf50][b]5[/b][/color]\n\n" +
                "[b][color=#4fc3f7]How to use it:[/color][/b]\n" +
                "1. Select an empty cell\n" +
                "2. Check numbers 1-9\n" +
                "3. Cross out what appears in row/column/box\n" +
                "4. Only one left? Place it!"
            )
        );
    }

    private static TipData CreateHiddenSingleTip()
    {
        var values = new int[,] {
            { 0, 2, 4 },
            { 1, 0, 0 },
            { 8, 0, 0 }
        };
        var isGiven = new bool[,] {
            { false, true, true },
            { true, false, false },
            { true, false, false }
        };
        var highlighted = new HashSet<(int, int)> { (2, 1) };
        var related = new HashSet<(int, int)> { (0, 0), (1, 1), (1, 2), (2, 2) };

        return new TipData(
            LT("Hidden Single (Versteckter Einzelner)", "Hidden Single"),
            LT(
                "[b][color=#4fc3f7]Was ist ein Hidden Single?[/color][/b]\n\n" +
                "Eine Zahl kann in einer Zeile, Spalte oder Block nur noch an [b]einer einzigen Position[/b] platziert werden.\n\n" +
                "[b][color=#ffb74d]Beispiel: Wo kann die 7 hin?[/color][/b]",
                "[b][color=#4fc3f7]What is a Hidden Single?[/color][/b]\n\n" +
                "A number can be placed in a row, column, or box in [b]only one position[/b].\n\n" +
                "[b][color=#ffb74d]Example: Where can 7 go?[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, (2, 1, 7)),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "Die 7 muss irgendwo in diesem Block platziert werden.\n\n" +
                "Pruefe jede leere Zelle:\n" +
                "[color=#f44336]-[/color] A1: Hat andere Einschraenkungen\n" +
                "[color=#f44336]-[/color] B2, C2: 7 in Spalte blockiert\n" +
                "[color=#4caf50]✓[/color] B3: Einzige Moeglichkeit fuer 7!\n\n" +
                "[b]Unterschied zum Naked Single:[/b]\n" +
                "Naked: Zelle hat nur 1 Kandidat\n" +
                "Hidden: Zahl hat nur 1 Position",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "The 7 must be placed somewhere in this box.\n\n" +
                "Check each empty cell:\n" +
                "[color=#f44336]-[/color] A1: Has other constraints\n" +
                "[color=#f44336]-[/color] B2, C2: 7 is blocked by the column\n" +
                "[color=#4caf50]✓[/color] B3: Only possible spot for 7!\n\n" +
                "[b]Difference vs. Naked Single:[/b]\n" +
                "Naked: the cell has only 1 candidate\n" +
                "Hidden: the number has only 1 position"
            )
        );
    }

    private static TipData CreateScanningTip()
    {
        var values = new int[,] {
            { 5, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 5, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 5 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 5, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 5, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        var isGiven = new bool[9, 9];
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                isGiven[r, c] = values[r, c] != 0;

        var highlighted = new HashSet<(int, int)>();
        var related = new HashSet<(int, int)> { (0, 0), (1, 3), (2, 8), (4, 2), (5, 5) };

        return new TipData(
            LT("Scanning-Technik", "Scanning Technique"),
            LT(
                "[b][color=#4fc3f7]Die Scanning-Technik[/color][/b]\n\n" +
                "Gehe systematisch jede Zahl durch und scanne das gesamte Spielfeld nach Positionen wo diese Zahl noch fehlt.\n\n" +
                "[b][color=#ffb74d]Beispiel: Scanning fuer die 5[/color][/b]",
                "[b][color=#4fc3f7]The Scanning Technique[/color][/b]\n\n" +
                "Systematically pick a number and scan the entire grid for places where that number is still missing.\n\n" +
                "[b][color=#ffb74d]Example: scanning for 5[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
                "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
                "[b]1.[/b] Waehle eine Zahl (z.B. 5)\n" +
                "[b]2.[/b] Markiere mental alle vorhandenen 5en im Grid\n" +
                "[b]3.[/b] Schaue auf jeden 3x3-Block einzeln:\n" +
                "   → Welche Bloecke haben noch keine 5?\n" +
                "[b]4.[/b] Zeichne gedanklich Linien durch die 5en:\n" +
                "   → Horizontale Linien durch Zeilen mit 5\n" +
                "   → Vertikale Linien durch Spalten mit 5\n" +
                "[b]5.[/b] Finde Bloecke wo nur noch 1 Platz frei ist\n" +
                "[b]6.[/b] Trage die 5 dort ein!\n\n" +
                "[b][color=#4caf50]Vorteile:[/color][/b]\n" +
                "[color=#81c784]✓[/color] Schnell erlernbar fuer Anfaenger\n" +
                "[color=#81c784]✓[/color] Findet viele einfache Loesungen\n" +
                "[color=#81c784]✓[/color] Keine Notizen noetig\n" +
                "[color=#81c784]✓[/color] Perfekt fuer Zahlen 7-9 (die oft vorkommen)",
                "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
                "[b]1.[/b] Pick a number (e.g. 5)\n" +
                "[b]2.[/b] Mentally mark all existing 5s\n" +
                "[b]3.[/b] Look at each 3x3 box:\n" +
                "   → Which boxes do not contain a 5 yet?\n" +
                "[b]4.[/b] Draw imaginary lines through the 5s:\n" +
                "   → Horizontal lines through rows with a 5\n" +
                "   → Vertical lines through columns with a 5\n" +
                "[b]5.[/b] Find boxes where only 1 spot remains\n" +
                "[b]6.[/b] Place the 5 there!\n\n" +
                "[b][color=#4caf50]Benefits:[/color][/b]\n" +
                "[color=#81c784]✓[/color] Easy to learn for beginners\n" +
                "[color=#81c784]✓[/color] Finds many simple placements\n" +
                "[color=#81c784]✓[/color] No notes required\n" +
                "[color=#81c784]✓[/color] Great for numbers that appear often"
            )
        );
    }

    private static TipData CreateCandidatesTip()
    {
        var values = new int[,] {
            { 5, 0, 0 },
            { 0, 0, 3 },
            { 0, 9, 0 }
        };
        var isGiven = new bool[,] {
            { true, false, false },
            { false, false, true },
            { false, true, false }
        };
        var highlighted = new HashSet<(int, int)>();
        var related = new HashSet<(int, int)>();
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 1), new[] { 1, 2, 4 } },
            { (0, 2), new[] { 2, 4 } },
            { (1, 0), new[] { 1, 2, 6 } },
            { (1, 1), new[] { 1, 2, 4, 6 } },
            { (2, 0), new[] { 1, 2, 4 } },
            { (2, 2), new[] { 1, 2, 4 } }
        };

        return new TipData(
            LT("Kandidaten notieren", "Write Down Candidates"),
            LT(
                "[b][color=#4fc3f7]Kandidaten notieren[/color][/b]\n\n" +
                "Die Grundlage fuer fortgeschrittene Techniken: Notiere in jeder leeren Zelle alle noch moeglichen Zahlen.\n\n" +
                "[b][color=#ffb74d]Beispiel mit Kandidaten:[/color][/b]",
                "[b][color=#4fc3f7]Writing Candidates[/color][/b]\n\n" +
                "The foundation for advanced techniques: write down all still-possible numbers in each empty cell.\n\n" +
                "[b][color=#ffb74d]Example with candidates:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
                "[b]1. Waehle eine leere Zelle:[/b]\n" +
                "   Z.B. Zelle B2 - mittlere Zelle\n\n" +
                "[b]2. Pruefe Zeile 2:[/b]\n" +
                "   Welche Zahlen sind bereits da? → 3, 9\n" +
                "   Koennte sein: 1,2,4,5,6,7,8\n\n" +
                "[b]3. Pruefe Spalte B:[/b]\n" +
                "   Welche Zahlen sind bereits da? → 5, 9\n" +
                "   Koennte sein: 1,2,3,4,6,7,8\n\n" +
                "[b]4. Pruefe den 3x3-Block:[/b]\n" +
                "   Welche Zahlen sind bereits da? → 3,5,9\n" +
                "   Koennte sein: 1,2,4,6,7,8\n\n" +
                "[b]5. Kombiniere alles:[/b]\n" +
                "   Schnittmenge: {1,2,4,6} ← Diese notieren!\n\n" +
                "[b][color=#4fc3f7]Im Spiel:[/color][/b]\n" +
                "[b]1.[/b] Druecke [b]N[/b] fuer den Notiz-Modus\n" +
                "[b]2.[/b] Waehle Zelle(n) aus\n" +
                "[b]3.[/b] Druecke Zahlen 1-9 zum Toggeln\n" +
                "[b]4.[/b] Druecke [b]N[/b] erneut um in Normal-Modus zu wechseln\n\n" +
                "[b][color=#f44336]WICHTIG:[/color][/b]\n" +
                "Halte Kandidaten aktuell! Nach jeder gefundenen Zahl:\n" +
                "→ Entferne die Zahl aus allen betroffenen Kandidaten!",
                "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
                "[b]1. Pick an empty cell:[/b]\n" +
                "   For example: cell B2 (the center)\n\n" +
                "[b]2. Check row 2:[/b]\n" +
                "   Which numbers are already there? → 3, 9\n" +
                "   Possible: 1,2,4,5,6,7,8\n\n" +
                "[b]3. Check column B:[/b]\n" +
                "   Which numbers are already there? → 5, 9\n" +
                "   Possible: 1,2,3,4,6,7,8\n\n" +
                "[b]4. Check the 3x3 box:[/b]\n" +
                "   Which numbers are already there? → 3,5,9\n" +
                "   Possible: 1,2,4,6,7,8\n\n" +
                "[b]5. Combine everything:[/b]\n" +
                "   Intersection: {1,2,4,6} ← write these down!\n\n" +
                "[b][color=#4fc3f7]In the game:[/color][/b]\n" +
                "[b]1.[/b] Press [b]N[/b] for Notes Mode\n" +
                "[b]2.[/b] Select cell(s)\n" +
                "[b]3.[/b] Press 1-9 to toggle candidates\n" +
                "[b]4.[/b] Press [b]N[/b] again to return to normal mode\n\n" +
                "[b][color=#f44336]IMPORTANT:[/color][/b]\n" +
                "Keep candidates up to date! After every placement:\n" +
                "→ Remove that number from all affected candidates."
            )
        );
    }

    private static TipData CreateNakedPairTip()
    {
        var values = new int[,] {
            { 0, 0, 0, 5, 0, 1 }
        };
        var isGiven = new bool[,] {
            { false, false, false, true, false, true }
        };
        var highlighted = new HashSet<(int, int)> { (0, 0), (0, 1) };
        var related = new HashSet<(int, int)> { (0, 2), (0, 4) };
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 0), new[] { 3, 7 } },
            { (0, 1), new[] { 3, 7 } },
            { (0, 2), new[] { 2, 3, 4 } },
            { (0, 4), new[] { 2, 3, 7 } }
        };

        return new TipData(
            LT("Naked Pair (Nacktes Paar)", "Naked Pair"),
            LT(
                "[b][color=#4fc3f7]Naked Pair[/color][/b]\n\n" +
                "Zwei Zellen mit [b]exakt denselben zwei Kandidaten[/b] bilden ein Paar.\n\n" +
                "[b][color=#ffb74d]Beispiel (Zeile):[/color][/b]",
                "[b][color=#4fc3f7]Naked Pair[/color][/b]\n\n" +
                "Two cells with [b]exactly the same two candidates[/b] form a pair.\n\n" +
                "[b][color=#ffb74d]Example (row):[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
                "[b]1. Finde das Paar:[/b]\n" +
                "   A1: {3,7} | B1: {3,7} (exakt gleich)\n\n" +
                "[b]2. Logik checken:[/b]\n" +
                "   Eine Zelle wird 3, die andere 7.\n" +
                "   3 und 7 koennen deshalb nirgendwo sonst in dieser Zeile stehen.\n\n" +
                "[b]3. Betroffene Zellen finden:[/b]\n" +
                "   Gleiche Zeile, aber NICHT Teil des Paars: C1, E1\n\n" +
                "[b]4. Eliminieren:[/b]\n" +
                "   C1: {2,3,4} → {2,4} (3 entfernen)\n" +
                "   E1: {2,3,7} → {2} (3 und 7 entfernen)\n\n" +
                "[b][color=#4caf50]Merke:[/color][/b] Naked Pairs funktionieren in Zeilen, Spalten UND Bloecken!",
                "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
                "[b]1. Spot the pair:[/b]\n" +
                "   A1: {3,7} | B1: {3,7} (exact match)\n\n" +
                "[b]2. Check the logic:[/b]\n" +
                "   One will be 3, the other 7.\n" +
                "   So 3 and 7 cannot appear elsewhere in this row.\n\n" +
                "[b]3. Find affected cells:[/b]\n" +
                "   Same row, not part of the pair: C1, E1\n\n" +
                "[b]4. Eliminate:[/b]\n" +
                "   C1: {2,3,4} → {2,4} (remove 3)\n" +
                "   E1: {2,3,7} → {2} (remove 3 and 7)\n\n" +
                "[b][color=#4caf50]Remember:[/color][/b] Naked Pairs work in rows, columns, AND boxes!"
            )
        );
    }

    private static TipData CreateHiddenPairTip()
    {
        var values = new int[,] {
            { 0, 3, 0, 4, 6, 0 }
        };
        var isGiven = new bool[,] {
            { false, true, false, true, true, false }
        };
        var highlighted = new HashSet<(int, int)> { (0, 0), (0, 2) };
        var related = new HashSet<(int, int)> { (0, 5) };
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 0), new[] { 1, 2, 4, 5 } },
            { (0, 2), new[] { 1, 2, 5, 8 } },
            { (0, 5), new[] { 2, 5, 7, 8 } }
        };

        return new TipData(
            LT("Hidden Pair (Verstecktes Paar)", "Hidden Pair"),
            LT(
                "[b][color=#4fc3f7]Hidden Pair[/color][/b]\n\n" +
                "Zwei Zahlen, die in einer Einheit nur in [b]zwei bestimmten Zellen[/b] vorkommen koennen - auch wenn diese Zellen mehr Kandidaten haben.\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]Hidden Pair[/color][/b]\n\n" +
                "Two numbers that, in a unit, can appear in [b]only two specific cells[/b] even if those cells currently have more candidates.\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
                "[b]1. Untersuche die Kandidaten:[/b]\n" +
                "   Zelle A1: {1,2,4,5}\n" +
                "   Zelle C1: {1,2,5,8}\n" +
                "   Zelle F1: {2,5,7,8}\n\n" +
                "[b]2. Pruefe jede Zahl einzeln:[/b]\n" +
                "   Wo kann die 1 in dieser Zeile stehen?\n" +
                "   → Nur in Zelle A1 oder C1!\n" +
                "   Wo kann die 4 in dieser Zeile stehen?\n" +
                "   → Nur in Zelle A1 oder C1!\n\n" +
                "[b]3. Erkenne das Hidden Pair:[/b]\n" +
                "   1 und 4 sind 'versteckt' in A1 und C1!\n" +
                "   Diese beiden Zahlen MUESSEN in diesen 2 Zellen sein.\n\n" +
                "[b]4. Analysiere die Logik:[/b]\n" +
                "   Eine Zelle wird 1, die andere 4.\n" +
                "   Alle ANDEREN Kandidaten in A1 und C1 sind unmöglich!\n\n" +
                "[b]5. Reduziere die Kandidaten:[/b]\n" +
                "   Zelle A1: {1,2,4,5} → {1,4} (2,5 entfernt!)\n" +
                "   Zelle C1: {1,2,5,8} → {1,4} (2,5,8 entfernt!)\n\n" +
                "[b][color=#f44336]Wichtig:[/color][/b] Hidden Pairs sind schwerer zu finden als Naked Pairs!\n" +
                "[b]Tipp:[/b] Suche nach Zahlen die nur 2 Positionen haben.",
                "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
                "[b]1. Inspect the candidates:[/b]\n" +
                "   Cell A1: {1,2,4,5}\n" +
                "   Cell C1: {1,2,5,8}\n" +
                "   Cell F1: {2,5,7,8}\n\n" +
                "[b]2. Check each number:[/b]\n" +
                "   Where can 1 go in this row?\n" +
                "   → Only in A1 or C1!\n" +
                "   Where can 4 go in this row?\n" +
                "   → Only in A1 or C1!\n\n" +
                "[b]3. Spot the Hidden Pair:[/b]\n" +
                "   1 and 4 are 'hidden' in A1 and C1.\n" +
                "   Those two numbers MUST be in these two cells.\n\n" +
                "[b]4. Apply the logic:[/b]\n" +
                "   One cell will be 1, the other 4.\n" +
                "   All OTHER candidates in A1 and C1 are impossible.\n\n" +
                "[b]5. Reduce candidates:[/b]\n" +
                "   Cell A1: {1,2,4,5} → {1,4} (remove 2,5)\n" +
                "   Cell C1: {1,2,5,8} → {1,4} (remove 2,5,8)\n\n" +
                "[b][color=#f44336]Important:[/color][/b] Hidden Pairs are harder to spot than Naked Pairs.\n" +
                "[b]Tip:[/b] Look for numbers that have only 2 possible positions."
            )
        );
    }

    private static TipData CreateNakedTripleTip()
    {
        var values = new int[,] {
            { 0, 0, 0, 5, 1, 9 }
        };
        var isGiven = new bool[,] {
            { false, false, false, true, true, true }
        };
        var highlighted = new HashSet<(int, int)> { (0, 0), (0, 1), (0, 2) };
        var related = new HashSet<(int, int)>();
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 0), new[] { 2, 3 } },
            { (0, 1), new[] { 3, 7 } },
            { (0, 2), new[] { 2, 7 } }
        };

        return new TipData(
            LT("Naked Triple (Nacktes Tripel)", "Naked Triple"),
            LT(
                "[b][color=#4fc3f7]Naked Triple[/color][/b]\n\n" +
                "Drei Zellen mit insgesamt [b]maximal drei Kandidaten[/b].\n" +
                "Nicht alle Zellen muessen alle drei haben!\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]Naked Triple[/color][/b]\n\n" +
                "Three cells that together contain [b]at most three candidates[/b].\n" +
                "Not every cell has to contain all three.\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "A: {2,3}\n" +
                "B: {3,7}\n" +
                "C: {2,7}\n\n" +
                "Summe der Kandidaten = {2,3,7} → Naked Triple\n\n" +
                "[b]Logik:[/b]\n" +
                "Diese 3 Zahlen MUESSEN in diesen 3 Zellen sein.\n" +
                "Also koennen sie aus anderen Zellen der Einheit entfernt werden.\n\n" +
                "[b]Tipp:[/b] Achte auf Zellen mit nur 2 Kandidaten - sie bilden oft Triples!",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "A: {2,3}\n" +
                "B: {3,7}\n" +
                "C: {2,7}\n\n" +
                "Combined candidates = {2,3,7} → Naked Triple\n\n" +
                "[b]Logic:[/b]\n" +
                "These three numbers MUST occupy these three cells.\n" +
                "So they can be removed from other cells in the same unit.\n\n" +
                "[b]Tip:[/b] Watch for cells with only 2 candidates — they often form triples."
            )
        );
    }

    private static TipData CreateNakedQuadTip()
    {
        var values = new int[,] {
            { 0, 0, 0, 0, 5, 6, 7, 8, 9 }
        };
        var isGiven = new bool[,] {
            { false, false, false, false, true, true, true, true, true }
        };
        var highlighted = new HashSet<(int, int)> { (0, 0), (0, 1), (0, 2), (0, 3) };
        var related = new HashSet<(int, int)>();
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 0), new[] { 1, 2 } },
            { (0, 1), new[] { 2, 3 } },
            { (0, 2), new[] { 3, 4 } },
            { (0, 3), new[] { 1, 4 } }
        };

        return new TipData(
            LT("Naked Quad (Nacktes Quartett)", "Naked Quad"),
            LT(
                "[b][color=#4fc3f7]Naked Quad[/color][/b]\n\n" +
                "Vier Zellen mit insgesamt [b]maximal vier Kandidaten[/b].\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]Naked Quad[/color][/b]\n\n" +
                "Four cells that together contain [b]at most four candidates[/b].\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "Zelle A: {1,2}\n" +
                "Zelle B: {2,3}\n" +
                "Zelle C: {3,4}\n" +
                "Zelle D: {1,4}\n\n" +
                "Zusammen: nur {1,2,3,4} = Naked Quad!\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "1,2,3,4 aus allen anderen Zellen der Einheit entfernen.\n\n" +
                "[b]Hinweis:[/b]\n" +
                "Quads sind selten, aber wenn du 3 Zellen mit 4+ gemeinsamen Kandidaten siehst, pruefe auf ein Quad!",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "Cell A: {1,2}\n" +
                "Cell B: {2,3}\n" +
                "Cell C: {3,4}\n" +
                "Cell D: {1,4}\n\n" +
                "Together: only {1,2,3,4} → a Naked Quad.\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove 1,2,3,4 from all other cells in the unit.\n\n" +
                "[b]Note:[/b]\n" +
                "Quads are rare, but if you see 3 cells sharing 4+ candidates, check for a quad."
            )
        );
    }

    private static TipData CreateHiddenTripleTip()
    {
        var values = new int[,] {
            { 0, 0, 0, 4, 5, 6, 7, 8, 9 }
        };
        var isGiven = new bool[,] {
            { false, false, false, true, true, true, true, true, true }
        };
        var highlighted = new HashSet<(int, int)> { (0, 0), (0, 1), (0, 2) };
        var related = new HashSet<(int, int)>();
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 0), new[] { 1, 2, 3 } },
            { (0, 1), new[] { 1, 3 } },
            { (0, 2), new[] { 2, 3 } }
        };

        return new TipData(
            LT("Hidden Triple (Verstecktes Tripel)", "Hidden Triple"),
            LT(
                "[b][color=#4fc3f7]Hidden Triple[/color][/b]\n\n" +
                "Drei Zahlen, die nur in [b]genau drei Zellen[/b] vorkommen koennen.\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]Hidden Triple[/color][/b]\n\n" +
                "Three numbers that, in a unit, can appear in [b]exactly three cells[/b].\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "Wo koennen 1, 2, 3 in dieser Zeile stehen?\n" +
                "1: nur in A oder B\n" +
                "2: nur in A oder C\n" +
                "3: nur in A, B oder C\n\n" +
                "[b]Hidden Triple {1,2,3}![/b]\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "Alle ANDEREN Kandidaten aus A, B, C entfernen!\n" +
                "Die Zellen behalten nur {1,2,3}.\n\n" +
                "[b]Tipp:[/b] Hidden Triples sind schwer zu finden - suche nach Zahlen die nur 2-3 Optionen haben!",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "Where can 1, 2, 3 go in this row?\n" +
                "1: only in A or B\n" +
                "2: only in A or C\n" +
                "3: only in A, B, or C\n\n" +
                "[b]Hidden Triple {1,2,3}![/b]\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove all OTHER candidates from A, B, C.\n" +
                "Those cells keep only {1,2,3}.\n\n" +
                "[b]Tip:[/b] Hidden Triples are hard to spot — look for numbers with only 2–3 possible positions."
            )
        );
    }

    private static TipData CreateJellyfishTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> {
            (0, 0), (0, 2), (0, 5), (0, 7),
            (2, 0), (2, 2),
            (5, 5), (5, 7),
            (7, 0), (7, 7)
        };
        var related = new HashSet<(int, int)> { (4, 0), (4, 2), (6, 5), (8, 7) };

        return new TipData(
            LT("Jellyfish Technik", "Jellyfish"),
            LT(
                "[b][color=#4fc3f7]Jellyfish[/color][/b]\n\n" +
                "Erweiterung von Swordfish auf [b]4 Zeilen und 4 Spalten[/b].\n" +
                "Sehr selten, aber aeusserst maechtig!\n\n" +
                "[b][color=#ffb74d]Beispiel mit der 6:[/color][/b]",
                "[b][color=#4fc3f7]Jellyfish[/color][/b]\n\n" +
                "An extension of Swordfish to [b]4 rows and 4 columns[/b].\n" +
                "Very rare, but extremely powerful.\n\n" +
                "[b][color=#ffb74d]Example with 6:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "Die 6 kann in 4 Zeilen nur in bestimmten Spalten stehen:\n" +
                "Zeile 1: Spalten A, C, F, H\n" +
                "Zeile 3: Spalten A, C\n" +
                "Zeile 6: Spalten F, H\n" +
                "Zeile 8: Spalten A, H\n\n" +
                "Alle 4 Spalten sind 'abgedeckt' von diesen 4 Zeilen.\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "Die 6 aus allen anderen Zellen in Spalten A, C, F, H entfernen!\n\n" +
                "[b]Hinweis:[/b] Jellyfish sind extrem selten - konzentriere dich erst auf X-Wing und Swordfish!",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "The 6 can appear in these 4 rows only in certain columns:\n" +
                "Row 1: columns A, C, F, H\n" +
                "Row 3: columns A, C\n" +
                "Row 6: columns F, H\n" +
                "Row 8: columns A, H\n\n" +
                "All 4 columns are covered by these 4 rows.\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove 6 from all other cells in columns A, C, F, H.\n\n" +
                "[b]Note:[/b] Jellyfish are extremely rare — focus on X-Wing and Swordfish first."
            )
        );
    }

    private static TipData CreateXYZWingTip()
    {
        var values = new int[,] {
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 0, 0, 0 }
        };
        var isGiven = new bool[,] {
            { false, false, false },
            { false, false, false },
            { false, false, false }
        };
        var highlighted = new HashSet<(int, int)> { (0, 0), (0, 2), (2, 0) };
        var related = new HashSet<(int, int)> { (0, 1) };
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 0), new[] { 1, 2, 3 } },   // Pivot: ABC (3 Kandidaten!)
            { (0, 2), new[] { 1, 3 } },      // Wing 1: AC
            { (2, 0), new[] { 2, 3 } },      // Wing 2: BC
            { (0, 1), new[] { 3, 5, 7 } }    // Affected cell
        };

        return new TipData(
            LT("XYZ-Wing Technik", "XYZ-Wing"),
            LT(
                "[b][color=#4fc3f7]XYZ-Wing[/color][/b]\n\n" +
                "Wie Y-Wing, aber der Pivot hat [b]drei Kandidaten[/b] statt zwei.\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]XYZ-Wing[/color][/b]\n\n" +
                "Like a Y-Wing, but the pivot has [b]three candidates[/b] instead of two.\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "[b]Pivot[/b] A1: Kandidaten {1,2,3} ← DREI!\n" +
                "[b]Wing 1[/b] C1: Kandidaten {1,3}\n" +
                "[b]Wing 2[/b] A3: Kandidaten {2,3}\n\n" +
                "[b]Unterschied zu Y-Wing:[/b]\n" +
                "Der Pivot hat alle 3 Kandidaten.\n" +
                "Eliminierungen nur in Zellen die ALLE DREI sehen!\n\n" +
                "[b]Logik:[/b]\n" +
                "Egal welchen Wert der Pivot hat, eine der 3 Zellen wird 3.\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "3 aus Zellen entfernen, die Pivot UND beide Wings sehen.\n" +
                "Hier: Zelle B1 verliert die 3!",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "[b]Pivot[/b] A1: candidates {1,2,3} (three!)\n" +
                "[b]Wing 1[/b] C1: candidates {1,3}\n" +
                "[b]Wing 2[/b] A3: candidates {2,3}\n\n" +
                "[b]Difference vs. Y-Wing:[/b]\n" +
                "The pivot contains all 3 candidates.\n" +
                "Eliminations apply only to cells that can see pivot AND both wings.\n\n" +
                "[b]Logic:[/b]\n" +
                "No matter what the pivot is, one of the three cells will be 3.\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove 3 from cells that see the pivot AND both wings.\n" +
                "Here: cell B1 loses candidate 3."
            )
        );
    }

    private static TipData CreateWWingTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (1, 1), (1, 7), (5, 7) };
        var related = new HashSet<(int, int)> { (5, 1) };
        var candidates = new Dictionary<(int, int), int[]> {
            { (1, 1), new[] { 3, 7 } },      // Bi-value cell 1
            { (1, 7), new[] { 3, 7 } },      // Strong link cell (H2)
            { (5, 7), new[] { 3, 7 } },      // Bi-value cell 2
            { (5, 1), new[] { 3, 5, 8 } }    // Affected cell
        };

        return new TipData(
            LT("W-Wing Technik", "W-Wing"),
            LT(
                "[b][color=#4fc3f7]W-Wing[/color][/b]\n\n" +
                "Zwei Bi-Value Zellen mit [b]identischen Kandidaten[/b],\n" +
                "verbunden durch einen [b]Strong Link[/b].\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]W-Wing[/color][/b]\n\n" +
                "Two bi-value cells with [b]identical candidates[/b],\n" +
                "connected by a [b]strong link[/b].\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "B2: {3,7}\n" +
                "H2: {3,7}\n" +
                "H6: {3,7}\n" +
                "Starker Link auf 7 in Spalte H (nur H2 oder H6 moeglich).\n\n" +
                "[b]Logik:[/b]\n" +
                "Wenn B2 = 3, erzwingt der Strong Link H6 = 7 (H2 wird 3).\n" +
                "Wenn B2 = 7, erzwingt der Strong Link H2 = 7 → H6 wird 3.\n" +
                "→ In JEDEM Fall ist entweder B2 oder H6 die 3.\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "Kandidat 3 aus Zellen entfernen, die B2 und H6 beide sehen.",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "B2: {3,7}\n" +
                "H2: {3,7}\n" +
                "H6: {3,7}\n" +
                "Strong link on 7 in column H (only H2 or H6 possible).\n\n" +
                "[b]Logic:[/b]\n" +
                "If B2 = 3, the strong link forces H6 = 7 (so H2 becomes 3).\n" +
                "If B2 = 7, the strong link forces H2 = 7 → H6 becomes 3.\n" +
                "→ Either B2 or H6 ends up as 3.\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove candidate 3 from any cell that sees both B2 and H6."
            )
        );
    }

    private static TipData CreateSkyscraperTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (1, 2), (1, 6), (5, 2), (7, 6) };
        var related = new HashSet<(int, int)> { (5, 6), (7, 2) };

        return new TipData(
            LT("Skyscraper Technik", "Skyscraper"),
            LT(
                "[b][color=#4fc3f7]Skyscraper[/color][/b]\n\n" +
                "Zwei Spalten mit je [b]genau 2 Kandidaten[/b] einer Zahl,\n" +
                "die eine [b]gemeinsame Zeile teilen[/b].\n\n" +
                "[b][color=#ffb74d]Beispiel mit der 4:[/color][/b]",
                "[b][color=#4fc3f7]Skyscraper[/color][/b]\n\n" +
                "Two columns each with [b]exactly two candidates[/b] for a number,\n" +
                "sharing one [b]common row[/b].\n\n" +
                "[b][color=#ffb74d]Example with 4:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "Spalte C: 4 nur in Zeile 2 und 6\n" +
                "Spalte G: 4 nur in Zeile 2 und 8\n\n" +
                "[b]Basis:[/b] Zeile 2 (gemeinsam)\n" +
                "[b]Spitzen:[/b] C6 und G8\n\n" +
                "[b]Logik:[/b]\n" +
                "Eine der 4en in Zeile 2 ist korrekt.\n" +
                "Je nachdem welche, wandert die andere 4 zur entsprechenden Spitze.\n" +
                "→ Mindestens eine Spitze wird 4!\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "4 aus Zellen entfernen, die BEIDE Spitzen sehen.",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "Column C: 4 only in rows 2 and 6\n" +
                "Column G: 4 only in rows 2 and 8\n\n" +
                "[b]Base:[/b] row 2 (common)\n" +
                "[b]Tops:[/b] C6 and G8\n\n" +
                "[b]Logic:[/b]\n" +
                "One of the 4s in row 2 is true.\n" +
                "Depending on which, the other 4 is forced to the corresponding top.\n" +
                "→ At least one top is 4.\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove 4 from cells that can see BOTH tops."
            )
        );
    }

    private static TipData CreateTwoStringKiteTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (0, 3), (0, 7), (5, 3), (7, 7) };
        var related = new HashSet<(int, int)> { (5, 7) };

        return new TipData(
            LT("2-String Kite Technik", "2-String Kite"),
            LT(
                "[b][color=#4fc3f7]2-String Kite[/color][/b]\n\n" +
                "Ein Kandidat bildet Konjugat-Paare in einer [b]Zeile[/b]\n" +
                "UND einer [b]Spalte[/b], die sich in einem Block treffen.\n\n" +
                "[b][color=#ffb74d]Beispiel mit der 5:[/color][/b]",
                "[b][color=#4fc3f7]2-String Kite[/color][/b]\n\n" +
                "A candidate forms conjugate pairs in a [b]row[/b]\n" +
                "AND a [b]column[/b] that meet in a box.\n\n" +
                "[b][color=#ffb74d]Example with 5:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "Zeile 1: 5 nur in Spalte D und H (Konjugat-Paar)\n" +
                "Spalte D: 5 nur in Zeile 1 und 6 (Konjugat-Paar)\n\n" +
                "Die Paare treffen sich bei D1 im Block!\n" +
                "Das bildet einen 'Drachen' (Kite).\n\n" +
                "[b]Enden des Kites:[/b]\n" +
                "H1 und D6\n\n" +
                "[b]Logik:[/b]\n" +
                "Durch die Konjugat-Paare ist einer der Enden immer 5.\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "5 aus Zellen entfernen, die beide Enden sehen.",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "Row 1: 5 only in columns D and H (conjugate pair)\n" +
                "Column D: 5 only in rows 1 and 6 (conjugate pair)\n\n" +
                "The pairs meet at D1 inside the box — that forms a kite.\n\n" +
                "[b]Ends of the kite:[/b]\n" +
                "H1 and D6\n\n" +
                "[b]Logic:[/b]\n" +
                "Because of the conjugate pairs, one of the ends must be 5.\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove 5 from cells that can see both ends."
            )
        );
    }

    private static TipData CreateEmptyRectangleTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        // Empty Rectangle in Block (Positionen wo Kandidat NICHT ist)
        var highlighted = new HashSet<(int, int)> { (3, 0), (3, 1), (4, 0), (4, 1), (5, 2) };
        var related = new HashSet<(int, int)> { (0, 2), (0, 8), (5, 8) };

        return new TipData(
            LT("Empty Rectangle Technik", "Empty Rectangle"),
            LT(
                "[b][color=#4fc3f7]Empty Rectangle[/color][/b]\n\n" +
                "Ein Kandidat bildet eine [b]L-Form[/b] in einem Block\n" +
                "und interagiert mit einem [b]Konjugat-Paar[/b].\n\n" +
                "[b][color=#ffb74d]Beispiel mit der 8:[/color][/b]",
                "[b][color=#4fc3f7]Empty Rectangle[/color][/b]\n\n" +
                "A candidate forms an [b]L-shape[/b] inside a box\n" +
                "and interacts with a [b]conjugate pair[/b].\n\n" +
                "[b][color=#ffb74d]Example with 8:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
                "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
                "Im Block links-Mitte bildet die 8 eine L-Form:\n" +
                "- Zeilen 4-5 haben keine 8 in Spalten A-B\n" +
                "- Das 'Empty Rectangle' zeigt wohin die 8 NICHT kann\n\n" +
                "Es gibt ein Konjugat-Paar fuer 8 in Zeile 1:\n" +
                "C1 und I1\n\n" +
                "[b]Logik:[/b]\n" +
                "Die L-Form und das Paar bilden eine Kette.\n" +
                "Am Ende der Kette kann 8 eliminiert werden.\n\n" +
                "[b]Konsequenz:[/b]\n" +
                "8 aus der Ziel-Zelle entfernen.\n\n" +
                "[b]Tipp:[/b] Suche nach Bloecken wo ein Kandidat nur in einer L-Form vorkommt!",
                "[b][color=#81c784]Analysis:[/color][/b]\n\n" +
                "In the middle-left box, candidate 8 forms an L-shape:\n" +
                "- Rows 4-5 have no 8 in columns A-B\n" +
                "- The empty rectangle shows where 8 cannot go\n\n" +
                "There is a conjugate pair for 8 in row 1:\n" +
                "C1 and I1\n\n" +
                "[b]Logic:[/b]\n" +
                "The L-shape and the pair form a chain.\n" +
                "At the end of the chain, 8 can be eliminated.\n\n" +
                "[b]Consequence:[/b]\n" +
                "Remove 8 from the target cell.\n\n" +
                "[b]Tip:[/b] Look for boxes where a candidate appears only in an L-shape."
            )
        );
    }

    private static TipData CreateSimpleColoringTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        // Colored cells (alternating)
        var highlighted = new HashSet<(int, int)> { (0, 2), (4, 2), (4, 7), (8, 7) }; // Color A
        var related = new HashSet<(int, int)> { (0, 5), (2, 2), (4, 4), (6, 7) };      // Color B

        return new TipData(
            LT("Simple Coloring (Ketten-Faerbung)", "Simple Coloring"),
            LT(
                "[b][color=#4fc3f7]Simple Coloring[/color][/b]\n\n" +
                "Konjugat-Paare werden [b]abwechselnd gefaerbt[/b]\n" +
                "um Widersprueche oder Eliminierungen zu finden.\n\n" +
                "[b][color=#ffb74d]Beispiel mit der 9:[/color][/b]",
                "[b][color=#4fc3f7]Simple Coloring[/color][/b]\n\n" +
                "Alternate coloring of [b]conjugate pairs[/b] to find\n" +
                "contradictions or eliminations.\n\n" +
                "[b][color=#ffb74d]Example with 9:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
                "[b][color=#81c784]So geht's:[/color][/b]\n\n" +
                "[b]Schritt 1:[/b] Finde alle Konjugat-Paare fuer eine Zahl\n" +
                "(Einheiten wo die Zahl nur 2x vorkommt)\n\n" +
                "[b]Schritt 2:[/b] Faerbe abwechselnd\n" +
                "Wenn A [color=#4fc3f7]BLAU[/color] ist, ist sein Partner [color=#ffb74d]ORANGE[/color]\n\n" +
                "[b]Schritt 3:[/b] Suche Widersprueche\n\n" +
                "[b][color=#f44336]Regel 1 (Color Trap):[/color][/b]\n" +
                "Sieht eine ungefaerbte Zelle BEIDE Farben?\n" +
                "→ Diese Zelle kann die Zahl NICHT haben!\n\n" +
                "[b][color=#f44336]Regel 2 (Color Wrap):[/color][/b]\n" +
                "Sehen sich zwei Zellen der GLEICHEN Farbe?\n" +
                "→ Diese Farbe ist FALSCH!\n" +
                "→ Alle Zellen dieser Farbe sind nicht diese Zahl.",
                "[b][color=#81c784]How it works:[/color][/b]\n\n" +
                "[b]Step 1:[/b] Find all conjugate pairs for a number\n" +
                "(units where the number appears only twice)\n\n" +
                "[b]Step 2:[/b] Color alternately\n" +
                "If A is [color=#4fc3f7]BLUE[/color], its partner is [color=#ffb74d]ORANGE[/color]\n\n" +
                "[b]Step 3:[/b] Look for contradictions\n\n" +
                "[b][color=#f44336]Rule 1 (Color Trap):[/color][/b]\n" +
                "Does an uncolored cell see BOTH colors?\n" +
                "→ Then that cell cannot be the number.\n\n" +
                "[b][color=#f44336]Rule 2 (Color Wrap):[/color][/b]\n" +
                "Do two cells of the SAME color see each other?\n" +
                "→ That color is wrong.\n" +
                "→ All cells of that color are not the number."
            )
        );
    }

    private static TipData CreateUniqueRectangleTip()
    {
        return new TipData(
            LT("Unique Rectangle (Deadly Pattern vermeiden)", "Unique Rectangle (Avoid Deadly Patterns)"),
            LT(
                "[b][color=#4fc3f7]Unique Rectangle[/color][/b]\n\n" +
                "Ein [b]Unique Rectangle[/b] ist ein 2x2-Rechteck aus vier Zellen,\n" +
                "in dem zwei Kandidaten (z.B. {a,b}) ein gefährliches Muster bilden würden.\n\n" +
                "Wenn alle vier Zellen nur {a,b} hätten, gäbe es oft [b]mehrere Lösungen[/b].\n" +
                "Um die Eindeutigkeit zu erhalten, muss irgendwo ein Kandidat eliminiert werden.\n\n" +
                "[b]Merke:[/b] Suche nach 2x2-Rechtecken mit denselben zwei Kandidaten.",
                "[b][color=#4fc3f7]Unique Rectangle[/color][/b]\n\n" +
                "A [b]Unique Rectangle[/b] is a 2x2 rectangle of four cells where two candidates\n" +
                "(e.g., {a,b}) would form a dangerous pattern.\n\n" +
                "If all four cells had only {a,b}, the puzzle often becomes [b]non-unique[/b].\n" +
                "To preserve uniqueness, a candidate must be eliminated somewhere.\n\n" +
                "[b]Tip:[/b] Look for 2x2 rectangles sharing the same two candidates."
            ),
            null,
            LT(
                "[b][color=#81c784]Praxis-Tipp:[/color][/b]\n\n" +
                "- Markiere die vier Zellen (2 Zeilen × 2 Spalten).\n" +
                "- Prüfe, ob genau zwei Kandidaten dominieren.\n" +
                "- Falls eine der vier Zellen zusätzliche Kandidaten hat,\n" +
                "  kann das oft zu einer Eliminierung führen.",
                "[b][color=#81c784]Practical tip:[/color][/b]\n\n" +
                "- Mark the four cells (2 rows × 2 columns).\n" +
                "- Check whether exactly two candidates dominate.\n" +
                "- If one of the four cells has extra candidates,\n" +
                "  you can often derive an elimination."
            )
        );
    }

    private static TipData CreateFinnedXWingTip()
    {
        return new TipData(
            LT("Finned X-Wing (X-Wing mit Flosse)", "Finned X-Wing"),
            LT(
                "[b][color=#4fc3f7]Finned X-Wing[/color][/b]\n\n" +
                "Ein [b]Finned X-Wing[/b] ist eine Variante von X-Wing:\n" +
                "Zwei Zeilen (oder Spalten) haben fast ein X-Wing-Rechteck,\n" +
                "aber eine Seite hat eine zusätzliche Kandidaten-Zelle: die [b]Flosse[/b].\n\n" +
                "Die Flosse begrenzt die Eliminierungen typischerweise auf einen Block.",
                "[b][color=#4fc3f7]Finned X-Wing[/color][/b]\n\n" +
                "A [b]Finned X-Wing[/b] is a variant of X-Wing:\n" +
                "Two rows (or columns) almost form an X-Wing rectangle,\n" +
                "but one side has an extra candidate cell: the [b]fin[/b].\n\n" +
                "The fin typically restricts eliminations to a single box."
            ),
            null,
            LT(
                "[b][color=#81c784]Merke:[/color][/b]\n\n" +
                "- Finde ein fast perfektes X-Wing.\n" +
                "- Suche die zusätzliche Kandidaten-Zelle (Flosse).\n" +
                "- Eliminierungen passieren in der Regel im Block der Flosse.",
                "[b][color=#81c784]Remember:[/color][/b]\n\n" +
                "- Find an almost perfect X-Wing.\n" +
                "- Identify the extra candidate cell (the fin).\n" +
                "- Eliminations usually occur inside the fin’s box."
            )
        );
    }

    private static TipData CreateFinnedSwordfishTip()
    {
        return new TipData(
            LT("Finned Swordfish (Swordfish mit Flosse)", "Finned Swordfish"),
            LT(
                "[b][color=#4fc3f7]Finned Swordfish[/color][/b]\n\n" +
                "Wie beim Swordfish, aber mit einer [b]Flosse[/b] (zusätzlicher Kandidat)\n" +
                "in einem der beteiligten Häuser.\n\n" +
                "Die Flosse schränkt die Eliminierungen meist auf einen Block ein.",
                "[b][color=#4fc3f7]Finned Swordfish[/color][/b]\n\n" +
                "Like Swordfish, but with a [b]fin[/b] (an extra candidate)\n" +
                "in one of the involved houses.\n\n" +
                "The fin usually restricts eliminations to a single box."
            ),
            null,
            LT(
                "[b][color=#81c784]Praxis:[/color][/b]\n\n" +
                "Wenn ein Swordfish fast passt, prüfe, ob die zusätzliche Zelle\n" +
                "in einem Block liegt, der die Eliminierung ermöglicht.",
                "[b][color=#81c784]Practice:[/color][/b]\n\n" +
                "If a Swordfish almost fits, check whether the extra cell sits in a box\n" +
                "that enables a restricted elimination."
            )
        );
    }

    private static TipData CreateRemotePairTip()
    {
        return new TipData(
            LT("Remote Pair (Kette von Bivalue-Zellen)", "Remote Pair"),
            LT(
                "[b][color=#4fc3f7]Remote Pair[/color][/b]\n\n" +
                "Ein [b]Remote Pair[/b] ist eine Kette aus Zellen, die alle genau\n" +
                "dieselben zwei Kandidaten besitzen (z.B. {a,b}).\n\n" +
                "Durch die Kette alternieren die Kandidaten logisch.\n" +
                "Eine Zelle, die beide Enden der Kette sieht, kann oft einen Kandidaten eliminieren.",
                "[b][color=#4fc3f7]Remote Pair[/color][/b]\n\n" +
                "A [b]Remote Pair[/b] is a chain of cells where each cell has exactly\n" +
                "the same two candidates (e.g., {a,b}).\n\n" +
                "Along the chain, the candidates alternate logically.\n" +
                "A cell that sees both ends can often eliminate a candidate."
            ),
            null,
            LT(
                "[b][color=#81c784]Merke:[/color][/b]\n\n" +
                "- Kette muss aus ausschließlich bivalue-Zellen bestehen.\n" +
                "- Kandidaten müssen identisch sein.\n" +
                "- Prüfe Zellen, die beide Ketten-Enden sehen.",
                "[b][color=#81c784]Remember:[/color][/b]\n\n" +
                "- The chain must consist only of bivalue cells.\n" +
                "- The candidate pair must be identical.\n" +
                "- Check cells that see both chain endpoints."
            )
        );
    }

    private static TipData CreateBugPlus1Tip()
    {
        return new TipData(
            LT("BUG+1 (Bivalue Universal Grave)", "BUG+1 (Bivalue Universal Grave)"),
            LT(
                "[b][color=#4fc3f7]BUG+1[/color][/b]\n\n" +
                "In einer BUG-Situation sind fast alle leeren Zellen [b]bivalue[/b] (genau 2 Kandidaten).\n" +
                "Bei [b]BUG+1[/b] gibt es genau eine Ausnahme: eine Zelle mit 3 Kandidaten.\n\n" +
                "Der 'Extra'-Kandidat in dieser Zelle ist dann oft die Lösung.",
                "[b][color=#4fc3f7]BUG+1[/color][/b]\n\n" +
                "In a BUG state, almost all empty cells are [b]bivalue[/b] (exactly 2 candidates).\n" +
                "In [b]BUG+1[/b], there is exactly one exception: a cell with 3 candidates.\n\n" +
                "The extra candidate in that cell is often the solution."
            ),
            null,
            LT(
                "[b][color=#81c784]Hinweis:[/color][/b]\n\n" +
                "BUG+1 kommt selten vor, ist aber sehr stark in Endspielen.\n" +
                "Wenn du fast überall nur Paare siehst, suche nach der einen 3-Kandidaten-Zelle.",
                "[b][color=#81c784]Tip:[/color][/b]\n\n" +
                "BUG+1 is rare but powerful in endgames.\n" +
                "If you see pairs almost everywhere, look for the one cell with 3 candidates."
            )
        );
    }

    private static TipData CreateAlsXzRuleTip()
    {
        return new TipData(
            LT("ALS-XZ Rule (Almost Locked Sets)", "ALS-XZ Rule"),
            LT(
                "[b][color=#4fc3f7]ALS-XZ Rule[/color][/b]\n\n" +
                "[b]ALS[/b] = Almost Locked Set: Eine Zellmenge mit (n+1) Kandidaten über n Zellen.\n\n" +
                "Bei ALS-XZ werden zwei ALS über gemeinsame Kandidaten (X und Z) verknüpft,\n" +
                "um Kandidaten in anderen Zellen zu eliminieren.",
                "[b][color=#4fc3f7]ALS-XZ Rule[/color][/b]\n\n" +
                "[b]ALS[/b] = Almost Locked Set: a group of n cells containing (n+1) candidates.\n\n" +
                "ALS-XZ links two ALS via shared candidates (X and Z)\n" +
                "to eliminate candidates elsewhere."
            ),
            null,
            LT(
                "[b][color=#81c784]Praxis-Tipp:[/color][/b]\n\n" +
                "Das ist eine fortgeschrittene Ketten-Technik.\n" +
                "Wenn du viele Kandidaten-Notizen nutzt, kann ALS-XZ überraschende Eliminierungen liefern.",
                "[b][color=#81c784]Practical tip:[/color][/b]\n\n" +
                "This is an advanced chaining technique.\n" +
                "With full candidate notes, ALS-XZ can yield surprising eliminations."
            )
        );
    }

    private static TipData CreateForcingChainTip()
    {
        return new TipData(
            LT("Forcing Chain (Widerspruchsbeweis)", "Forcing Chain (Proof by contradiction)"),
            LT(
                "[b][color=#4fc3f7]Forcing Chain[/color][/b]\n\n" +
                "Bei einer [b]Forcing Chain[/b] testest du Annahmen (Kandidaten)\n" +
                "und verfolgst die logischen Konsequenzen.\n\n" +
                "Führt eine Annahme zu einem [b]Widerspruch[/b], ist sie falsch.\n" +
                "Bleibt nur eine Annahme übrig, ist sie die Lösung.",
                "[b][color=#4fc3f7]Forcing Chain[/color][/b]\n\n" +
                "In a [b]Forcing Chain[/b], you test assumptions (candidates)\n" +
                "and follow their logical consequences.\n\n" +
                "If an assumption leads to a [b]contradiction[/b], it is false.\n" +
                "If only one assumption remains, it must be the solution."
            ),
            null,
            LT(
                "[b][color=#81c784]Wichtig:[/color][/b]\n\n" +
                "Forcing Chains sind mächtig, aber zeitaufwändig.\n" +
                "Nutze sie, wenn klassische Techniken nicht weiterhelfen.",
                "[b][color=#81c784]Important:[/color][/b]\n\n" +
                "Forcing chains are powerful but time-consuming.\n" +
                "Use them when simpler techniques are exhausted."
            )
        );
    }

    private static TipData CreatePointingPairTip()
    {
        var values = new int[,] {
            { 0, 0, 0, 5, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 5 }
        };
        var isGiven = new bool[,] {
            { false, false, false, true, false, false, false, false, false },
            { false, false, false, false, false, false, false, false, false },
            { false, false, false, false, false, false, false, false, true }
        };
        var highlighted = new HashSet<(int, int)> { (1, 0), (1, 1) };
        var related = new HashSet<(int, int)> { (1, 3), (1, 4), (1, 5) };

        return new TipData(
            LT("Pointing Pair", "Pointing Pair"),
            LT(
                "[b][color=#4fc3f7]Pointing Pair[/color][/b]\n\n" +
                "Wenn eine Zahl in einem Block nur in einer Zeile oder Spalte moeglich ist, dann 'zeigt' sie darauf.\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]Pointing Pair[/color][/b]\n\n" +
                "If a number in a box can only go in one row or one column, it 'points' to that line.\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
                "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
                "[b]1. Untersuche einen Block:[/b]\n" +
                "   Block 1 (oben-links): Wo kann die 5 stehen?\n\n" +
                "[b]2. Pruefe jede Zeile im Block:[/b]\n" +
                "   Zeile 1: Hat bereits 5 in Spalte D → blockiert\n" +
                "   Zeile 2: Zellen A2 und B2 sind moeglich! ✓\n" +
                "   Zeile 3: Hat bereits 5 in Spalte I → blockiert\n\n" +
                "[b]3. Erkenne das Muster:[/b]\n" +
                "   Die 5 fuer diesen Block MUSS in Zeile 2 sein!\n" +
                "   Die beiden Zellen 'zeigen' auf diese Zeile.\n\n" +
                "[b]4. Eliminiere in der Zeile:[/b]\n" +
                "   Entferne 5 aus ALLEN anderen Zellen von Zeile 2!\n" +
                "   (Hier: Zellen D2, E2, F2)\n\n" +
                "[b][color=#4caf50]Tipp:[/color][/b] Auch umgekehrt: Pointing Triple (3 Zellen) funktioniert genauso!",
                "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
                "[b]1. Inspect a box:[/b]\n" +
                "   Box 1 (top-left): where can 5 go?\n\n" +
                "[b]2. Check each row in the box:[/b]\n" +
                "   Row 1: already has a 5 → blocked\n" +
                "   Row 2: cells A2 and B2 are possible ✓\n" +
                "   Row 3: already has a 5 → blocked\n\n" +
                "[b]3. Spot the pattern:[/b]\n" +
                "   The 5 for this box MUST be in row 2.\n" +
                "   So the candidates point to that row.\n\n" +
                "[b]4. Eliminate in the row:[/b]\n" +
                "   Remove 5 from ALL other cells in row 2.\n" +
                "   (Here: D2, E2, F2)\n\n" +
                "[b][color=#4caf50]Tip:[/color][/b] The same idea works for a Pointing Triple (3 cells)."
            )
        );
    }

    private static TipData CreateBoxLineTip()
    {
        var values = new int[,] {
            { 3, 0, 0, 0, 0, 0, 0, 0, 3 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        var isGiven = new bool[,] {
            { true, false, false, false, false, false, false, false, true },
            { false, false, false, false, false, false, false, false, false },
            { false, false, false, false, false, false, false, false, false }
        };
        var highlighted = new HashSet<(int, int)> { (0, 4), (0, 5) };
        var related = new HashSet<(int, int)> { (1, 3), (1, 4), (1, 5), (2, 3), (2, 4), (2, 5) };

        return new TipData(
            LT("Box/Line Reduction", "Box/Line Reduction"),
            LT(
                "[b][color=#4fc3f7]Box/Line Reduction[/color][/b]\n\n" +
                "Das Gegenteil von Pointing Pair: Eine Zahl in einer Zeile/Spalte kann nur in einem Block stehen.\n\n" +
                "[b][color=#ffb74d]Beispiel (Zeile 1):[/color][/b]",
                "[b][color=#4fc3f7]Box/Line Reduction[/color][/b]\n\n" +
                "The opposite of a Pointing Pair: in a row/column, a number can only be placed within one box.\n\n" +
                "[b][color=#ffb74d]Example (row 1):[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
            "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
            "[b]1. Untersuche eine Zeile:[/b]\n" +
            "   Zeile 1: Wo kann die 3 stehen?\n\n" +
            "[b]2. Pruefe jeden Block:[/b]\n" +
            "   Block 1 (Spalten A-C): Hat bereits 3 → blockiert\n" +
            "   Block 2 (Spalten D-F): Zellen E1 und F1 moeglich! ✓\n" +
            "   Block 3 (Spalten G-I): Hat bereits 3 → blockiert\n\n" +
            "[b]3. Erkenne das Muster:[/b]\n" +
            "   Die 3 fuer diese Zeile MUSS in Block 2 sein!\n" +
            "   Alle moeglichen Positionen sind in einem Block.\n\n" +
            "[b]4. Eliminiere im Block:[/b]\n" +
            "   Entferne 3 aus ALLEN anderen Zellen von Block 2!\n" +
            "   Beispiel: D2, E2, F2 (und ebenso D3, E3, F3).\n\n" +
            "[b][color=#4caf50]Unterschied zu Pointing Pair:[/color][/b]\n" +
            "Pointing: Block → Zeile/Spalte\n" +
            "Box/Line: Zeile/Spalte → Block",
            "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
            "[b]1. Inspect a row:[/b]\n" +
            "   Row 1: where can 3 go?\n\n" +
            "[b]2. Check each box:[/b]\n" +
            "   Box 1 (columns A-C): already has a 3 → blocked\n" +
            "   Box 2 (columns D-F): cells E1 and F1 are possible ✓\n" +
            "   Box 3 (columns G-I): already has a 3 → blocked\n\n" +
            "[b]3. Spot the pattern:[/b]\n" +
            "   The 3 for this row MUST be in box 2.\n" +
            "   All candidates are inside one box.\n\n" +
            "[b]4. Eliminate in the box:[/b]\n" +
            "   Remove 3 from ALL other cells in box 2.\n" +
            "   Example: D2, E2, F2 (and D3, E3, F3).\n\n" +
            "[b][color=#4caf50]Difference vs. Pointing Pair:[/color][/b]\n" +
            "Pointing: box → row/column\n" +
            "Box/Line: row/column → box"
            )
        );
    }

    private static TipData CreateXWingTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (1, 1), (1, 6), (5, 1), (5, 6) };
        var related = new HashSet<(int, int)> { (2, 1), (4, 6) };

        return new TipData(
            LT("X-Wing Technik", "X-Wing"),
            LT(
                "[b][color=#4fc3f7]X-Wing[/color][/b]\n\n" +
                "Fortgeschrittene Technik: Eine Zahl bildet ein Rechteck-Muster in genau 2 Zeilen und 2 Spalten.\n\n" +
                "[b][color=#ffb74d]Beispiel mit der 7:[/color][/b]",
                "[b][color=#4fc3f7]X-Wing[/color][/b]\n\n" +
                "Advanced technique: a number forms a rectangle pattern in exactly 2 rows and 2 columns.\n\n" +
                "[b][color=#ffb74d]Example with 7:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
            "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
            "[b]1. Finde Zeilen mit nur 2 Kandidaten:[/b]\n" +
            "   Zeile 2: 7 nur in Spalte B und Spalte G\n" +
            "   Zeile 6: 7 nur in Spalte B und Spalte G\n\n" +
            "[b]2. Pruefe die Spalten:[/b]\n" +
            "   Beide Zeilen verwenden die GLEICHEN 2 Spalten!\n" +
            "   Das bildet ein Rechteck → X-Wing! ✓\n\n" +
            "[b]3. Analysiere die Moeglichkeiten:[/b]\n" +
            "   Szenario A: B2=7 und G6=7 (Diagonale 1)\n" +
            "   Szenario B: G2=7 und B6=7 (Diagonale 2)\n\n" +
            "[b]4. Erkenne die Logik:[/b]\n" +
            "   In BEIDEN Szenarien hat jede Spalte genau eine 7!\n" +
            "   → Spalte B bekommt definitiv eine 7\n" +
            "   → Spalte G bekommt definitiv eine 7\n\n" +
            "[b]5. Eliminiere Kandidaten:[/b]\n" +
            "   Entferne 7 aus ALLEN anderen Zellen in Spalte B und G!\n" +
            "   (Ausser den 4 X-Wing Eckpunkten)\n\n" +
            "[b][color=#4caf50]Hinweis:[/color][/b] X-Wing funktioniert auch umgekehrt: 2 Spalten mit je 2 Kandidaten in gleichen Zeilen!",
            "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
            "[b]1. Find rows with only 2 candidates:[/b]\n" +
            "   Row 2: 7 only in columns B and G\n" +
            "   Row 6: 7 only in columns B and G\n\n" +
            "[b]2. Check the columns:[/b]\n" +
            "   Both rows use the SAME 2 columns → rectangle → X-Wing.\n\n" +
            "[b]3. Two possible diagonals:[/b]\n" +
            "   Scenario A: B2=7 and G6=7\n" +
            "   Scenario B: G2=7 and B6=7\n\n" +
            "[b]4. Key idea:[/b]\n" +
            "   In both scenarios, column B has exactly one 7 and column G has exactly one 7.\n\n" +
            "[b]5. Eliminate candidates:[/b]\n" +
            "   Remove 7 from all other cells in columns B and G\n" +
            "   (except the four X-Wing corners).\n\n" +
            "[b][color=#4caf50]Note:[/color][/b] X-Wing also works the other way: columns first, then rows."
            )
        );
    }

    private static TipData CreateSwordfishTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (0, 1), (0, 4), (3, 1), (3, 7), (6, 4), (6, 7) };
        var related = new HashSet<(int, int)> { (2, 1), (5, 4), (8, 7) };

        return new TipData(
            LT("Swordfish Technik", "Swordfish"),
            LT(
                "[b][color=#4fc3f7]Swordfish[/color][/b]\n\n" +
                "X-Wing erweitert auf [b]3 Zeilen und 3 Spalten[/b]. Selten, aber sehr maechtig!\n\n" +
                "[b][color=#ffb74d]Beispiel mit der 4:[/color][/b]",
                "[b][color=#4fc3f7]Swordfish[/color][/b]\n\n" +
                "X-Wing extended to [b]3 rows and 3 columns[/b]. Rare, but powerful.\n\n" +
                "[b][color=#ffb74d]Example with 4:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
            "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
            "[b]1. Finde Zeilen mit 2-3 Kandidaten:[/b]\n" +
            "   Zeile 1: 4 in Spalte B und E\n" +
            "   Zeile 4: 4 in Spalte B und H\n" +
            "   Zeile 7: 4 in Spalte E und H\n\n" +
            "[b]2. Pruefe die Spalten:[/b]\n" +
            "   Alle Kandidaten verteilen sich auf 3 Spalten: B, E, H\n" +
            "   Jede Spalte wird von mindestens 2 Zeilen abgedeckt! ✓\n\n" +
            "[b]3. Analysiere die Verteilung:[/b]\n" +
            "   Spalte B: In Zeilen 1 und 4\n" +
            "   Spalte E: In Zeilen 1 und 7\n" +
            "   Spalte H: In Zeilen 4 und 7\n\n" +
            "[b]4. Erkenne die Logik:[/b]\n" +
            "   Diese 3 Zeilen MUESSEN die 4 in diesen 3 Spalten haben.\n" +
            "   Egal wie verteilt: Jede Spalte bekommt genau eine 4!\n\n" +
            "[b]5. Eliminiere Kandidaten:[/b]\n" +
            "   Entferne 4 aus ALLEN anderen Zellen in Spalten B, E, H!\n" +
            "   (Ausser den Swordfish-Positionen)\n\n" +
            "[b][color=#f44336]Hinweis:[/color][/b] Swordfish zu finden ist schwierig! Pruefe zuerst immer einfachere Techniken.",
            "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
            "[b]1. Find rows with 2-3 candidates:[/b]\n" +
            "   Row 1: 4 in columns B and E\n" +
            "   Row 4: 4 in columns B and H\n" +
            "   Row 7: 4 in columns E and H\n\n" +
            "[b]2. Check the columns:[/b]\n" +
            "   All candidates are within 3 columns: B, E, H.\n\n" +
            "[b]3. Key idea:[/b]\n" +
            "   These 3 rows must place the 4 within these 3 columns, so\n" +
            "   columns B, E, H will each contain one 4.\n\n" +
            "[b]4. Eliminate candidates:[/b]\n" +
            "   Remove 4 from all other cells in columns B, E, H\n" +
            "   (except the Swordfish positions).\n\n" +
            "[b][color=#f44336]Note:[/color][/b] Swordfish is hard to spot — always try simpler techniques first."
            )
        );
    }

    private static TipData CreateYWingTip()
    {
        var values = new int[,] {
            { 0, 0, 0 },
            { 0, 0, 0 },
            { 0, 0, 0 }
        };
        var isGiven = new bool[,] {
            { false, false, false },
            { false, false, false },
            { false, false, false }
        };
        var highlighted = new HashSet<(int, int)> { (0, 0), (0, 2), (2, 0) };
        var related = new HashSet<(int, int)> { (2, 2) };
        var candidates = new Dictionary<(int, int), int[]> {
            { (0, 0), new[] { 1, 2 } },      // Pivot: AB
            { (0, 2), new[] { 1, 3 } },      // Wing 1: AC
            { (2, 0), new[] { 2, 3 } },      // Wing 2: BC
            { (2, 2), new[] { 1, 3, 5 } }    // Affected cell
        };

        return new TipData(
            LT("Y-Wing (XY-Wing)", "Y-Wing (XY-Wing)"),
            LT(
                "[b][color=#4fc3f7]Y-Wing[/color][/b]\n\n" +
                "Eine Ketten-Technik: 3 Zellen mit je 2 Kandidaten bilden eine 'Y'-Form.\n\n" +
                "[b][color=#ffb74d]Beispiel:[/color][/b]",
                "[b][color=#4fc3f7]Y-Wing[/color][/b]\n\n" +
                "A chaining technique: 3 bi-value cells form a 'Y' structure.\n\n" +
                "[b][color=#ffb74d]Example:[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            LT(
            "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
            "[b]1. Finde den Pivot:[/b]\n" +
            "   Zelle A1: {1,2} ← Der 'Stamm' des Y\n\n" +
            "[b]2. Finde die Wings:[/b]\n" +
            "   Wing 1 (C1): {1,3} ← Teilt '1' mit Pivot\n" +
            "   Wing 2 (A3): {2,3} ← Teilt '2' mit Pivot\n\n" +
            "[b]3. Pruefe die Struktur:[/b]\n" +
            "   - Pivot und Wing 1 sehen sich (gleiche Zeile) ✓\n" +
            "   - Pivot und Wing 2 sehen sich (gleiche Spalte) ✓\n" +
            "   - Wings sehen sich NICHT direkt ✓\n" +
            "   - Wings teilen einen Kandidaten: {3} ✓\n\n" +
            "[b]4. Analysiere die Logik:[/b]\n" +
            "   Wenn Pivot = 1 → Wing 2 muss 3 sein\n" +
            "   Wenn Pivot = 2 → Wing 1 muss 3 sein\n" +
            "   → In JEDEM Fall wird eine der Wings die 3!\n\n" +
            "[b]5. Finde Ziel-Zellen:[/b]\n" +
            "   Suche Zellen die BEIDE Wings sehen koennen\n" +
            "   Hier: Zelle C3\n\n" +
            "[b]6. Eliminiere:[/b]\n" +
            "   Entferne 3 aus Zelle C3!\n\n" +
            "[b][color=#4caf50]Merke:[/color][/b] Der Pivot ist immer die Zelle die beide anderen 'verbindet'.",
            "[b][color=#81c784]Step by step:[/color][/b]\n\n" +
            "[b]1. Find the pivot:[/b]\n" +
            "   Cell A1: {1,2} ← the stem of the Y\n\n" +
            "[b]2. Find the wings:[/b]\n" +
            "   Wing 1 (C1): {1,3} ← shares 1 with the pivot\n" +
            "   Wing 2 (A3): {2,3} ← shares 2 with the pivot\n\n" +
            "[b]3. Structure check:[/b]\n" +
            "   - Pivot sees wing 1 ✓\n" +
            "   - Pivot sees wing 2 ✓\n" +
            "   - Wings do NOT see each other ✓\n" +
            "   - Wings share candidate 3 ✓\n\n" +
            "[b]4. Logic:[/b]\n" +
            "   If pivot = 1 → wing 2 must be 3\n" +
            "   If pivot = 2 → wing 1 must be 3\n" +
            "   → In any case, one wing is 3.\n\n" +
            "[b]5. Eliminate:[/b]\n" +
            "   Remove 3 from any cell that can see BOTH wings.\n" +
            "   Here: cell C3.\n\n" +
            "[b][color=#4caf50]Remember:[/color][/b] The pivot is the cell that connects the other two."
            )
        );
    }

    private static TipData CreateGeneralStrategiesTip()
    {
        return new TipData(
            LT("Allgemeine Strategien", "General Strategies"),
            LT(
            "[b][color=#4fc3f7]Allgemeine Strategien[/color][/b]\n\n" +
            "[b][color=#ffb74d]1. Starte mit dem Offensichtlichen[/color][/b]\n" +
            "- Bloecke/Zeilen mit vielen Zahlen\n" +
            "- Zahlen die 6-7 mal vorkommen\n" +
            "- Zellen mit wenigen Moeglichkeiten\n\n" +
            "[b][color=#ffb74d]2. Arbeite systematisch[/color][/b]\n" +
            "- Zeile fuer Zeile\n" +
            "- Spalte fuer Spalte\n" +
            "- Block fuer Block\n" +
            "- Zahl fuer Zahl (1-9)\n\n" +
            "[b][color=#ffb74d]3. Ketten-Reaktion[/color][/b]\n" +
            "Nach jeder gefundenen Zahl:\n" +
            "-> Kandidaten in Zeile aktualisieren\n" +
            "-> Kandidaten in Spalte aktualisieren\n" +
            "-> Kandidaten in Block aktualisieren\n" +
            "-> Neue Naked Singles suchen!\n\n" +
            "[b][color=#ffb74d]4. Wenn du feststeckst[/color][/b]\n" +
            "- Mache eine Pause\n" +
            "- Pruefe Kandidaten erneut\n" +
            "- Nutze den Hinweis-Button\n\n" +
            "[b][color=#4caf50]Goldene Regel: NIEMALS RATEN![/color][/b]",
            "[b][color=#4fc3f7]General Strategies[/color][/b]\n\n" +
            "[b][color=#ffb74d]1. Start with the obvious[/color][/b]\n" +
            "- Boxes/rows with many givens\n" +
            "- Numbers that already appear 6-7 times\n" +
            "- Cells with few possibilities\n\n" +
            "[b][color=#ffb74d]2. Work systematically[/color][/b]\n" +
            "- Row by row\n" +
            "- Column by column\n" +
            "- Box by box\n" +
            "- Number by number (1-9)\n\n" +
            "[b][color=#ffb74d]3. Chain reaction[/color][/b]\n" +
            "After every placement:\n" +
            "-> Update candidates in the row\n" +
            "-> Update candidates in the column\n" +
            "-> Update candidates in the box\n" +
            "-> Look for new Naked Singles\n\n" +
            "[b][color=#ffb74d]4. If you get stuck[/color][/b]\n" +
            "- Take a short break\n" +
            "- Re-check candidates\n" +
            "- Use the Hint button\n\n" +
            "[b][color=#4caf50]Golden rule: NEVER GUESS![/color][/b]"
            ),
            null,
            LT("", "")
        );
    }

    private static TipData CreateKeyboardShortcutsTip()
    {
        return new TipData(
            LT("Tastenkuerzel", "Keyboard Shortcuts"),
            LT(
            "[b][color=#4fc3f7]Tastenkuerzel[/color][/b]\n\n" +
            "[b][color=#ffb74d]Navigation:[/color][/b]\n" +
            "[b]Pfeiltasten[/b] – Zelle wechseln\n" +
            "[b]Shift + Pfeiltasten[/b] – Mehrfachauswahl erweitern\n" +
            "[b]ESC[/b] – Auswahl aufheben / Zurueck\n\n" +
            "[b][color=#ffb74d]Eingabe:[/color][/b]\n" +
            "[b]1-9[/b] – Zahl eingeben\n" +
            "[b]Entf / Rueck[/b] – Zahl loeschen\n" +
            "[b]N[/b] – Notizen-Modus umschalten\n\n" +
            "[b][color=#ffb74d]Im Notizen-Modus:[/color][/b]\n" +
            "[b]1-9[/b] – Notiz hinzufuegen (bei Mehrfachauswahl)\n" +
            "[b]1-9[/b] – Notiz toggeln (bei Einzelauswahl)\n" +
            "[b]Entf / Rueck[/b] – Alle Notizen loeschen\n\n" +
            "[b][color=#ffb74d]Extras:[/color][/b]\n" +
            "[b]Rechtsklick[/b] auf ▶Row/Col/Box – Modus wechseln\n\n" +
            "[b][color=#4caf50]Tipp:[/color][/b] Mit Tastatur bist du schneller!",
            "[b][color=#4fc3f7]Keyboard Shortcuts[/color][/b]\n\n" +
            "[b][color=#ffb74d]Navigation:[/color][/b]\n" +
            "[b]Arrow keys[/b] – move selection\n" +
            "[b]Shift + arrow keys[/b] – expand multi-select\n" +
            "[b]ESC[/b] – clear selection / back\n\n" +
            "[b][color=#ffb74d]Input:[/color][/b]\n" +
            "[b]1-9[/b] – enter number\n" +
            "[b]Del / Backspace[/b] – erase\n" +
            "[b]N[/b] – toggle Notes Mode\n\n" +
            "[b][color=#ffb74d]In Notes Mode:[/color][/b]\n" +
            "[b]1-9[/b] – add notes (multi-select)\n" +
            "[b]1-9[/b] – toggle note (single cell)\n" +
            "[b]Del / Backspace[/b] – clear all notes\n\n" +
            "[b][color=#ffb74d]Extras:[/color][/b]\n" +
            "[b]Right click[/b] on ▶Row/Col/Box – change mode\n\n" +
            "[b][color=#4caf50]Tip:[/color][/b] The keyboard is faster once you get used to it!"
            ),
            null,
            LT("", "")
        );
    }

    private static TipData CreateMultiSelectTip()
    {
        return new TipData(
            LT("Mehrfachauswahl", "Multi-Select"),
            LT(
            "[b][color=#4fc3f7]Mehrfachauswahl[/color][/b]\n\n" +
            "[b][color=#ffb74d]So waehlst du mehrere Zellen:[/color][/b]\n\n" +
            "[b]Mit Maus:[/b]\n" +
            "• [b]Ctrl + Klick[/b] – Einzelne Zellen hinzufuegen\n" +
            "• [b]Ziehen[/b] – Rechteck auswaehlen\n\n" +
            "[b]Mit Tastatur:[/b]\n" +
            "• [b]Shift + Pfeiltasten[/b] – Auswahl erweitern\n\n" +
            "[b][color=#ffb74d]Was du damit machen kannst:[/color][/b]\n\n" +
            "[b]Im Notizen-Modus:[/b]\n" +
            "• Zahl druecken → Notiz in ALLE Zellen\n" +
            "• Entf/Rueck → Notizen aus ALLEN loeschen\n\n" +
            "[b]Im normalen Modus:[/b]\n" +
            "• Zahl druecken → Alle Zellen fuellen (wenn korrekt)\n" +
            "• Entf/Rueck → Alle Zellen leeren\n\n" +
            "[b][color=#4caf50]Perfekt fuer:[/color][/b]\n" +
            "• Schnelles Notizen-Setzen\n" +
            "• Kandidaten in einem Haus loeschen\n" +
            "• Mehrere gleiche Zahlen eintragen",
            "[b][color=#4fc3f7]Multi-Select[/color][/b]\n\n" +
            "[b][color=#ffb74d]How to select multiple cells:[/color][/b]\n\n" +
            "[b]With mouse:[/b]\n" +
            "• [b]Ctrl + click[/b] – add individual cells\n" +
            "• [b]Drag[/b] – select a rectangle\n\n" +
            "[b]With keyboard:[/b]\n" +
            "• [b]Shift + arrow keys[/b] – expand selection\n\n" +
            "[b][color=#ffb74d]What you can do with it:[/color][/b]\n\n" +
            "[b]In Notes Mode:[/b]\n" +
            "• Press a number → add that note to ALL selected cells\n" +
            "• Del/Backspace → clear notes from ALL selected cells\n\n" +
            "[b]In normal mode:[/b]\n" +
            "• Press a number → fill all selected cells (if allowed)\n" +
            "• Del/Backspace → clear all selected cells\n\n" +
            "[b][color=#4caf50]Great for:[/color][/b]\n" +
            "• Fast note entry\n" +
            "• Clearing candidates in a house\n" +
            "• Entering the same number in multiple places"
            ),
            null,
            LT("", "")
        );
    }

    private static TipData CreatePracticeTip()
    {
        return new TipData(
            LT("Uebung macht den Meister", "Practice Makes Perfect"),
            LT(
            "[b][color=#4fc3f7]Uebung macht den Meister[/color][/b]\n\n" +
            "[b][color=#ffb74d]Trainingsplan:[/color][/b]\n\n" +
            "[b]Woche 1-2: Grundlagen[/b]\n" +
            "- 3-5 leichte Raetsel pro Tag\n" +
            "- Fokus: Naked und Hidden Single\n" +
            "- Zeit ist egal - Genauigkeit zaehlt!\n\n" +
            "[b]Woche 3-4: Aufbau[/b]\n" +
            "- 2-3 mittlere Raetsel pro Tag\n" +
            "- Kandidaten-Notizen nutzen\n" +
            "- Naked Pairs suchen\n\n" +
            "[b]Woche 5+: Fortgeschritten[/b]\n" +
            "- Mix aus allen Schwierigkeiten\n" +
            "- Zeit messen und verbessern\n" +
            "- Neue Techniken lernen\n\n" +
            "[b][color=#ffb74d]Mentale Tipps:[/color][/b]\n" +
            "[color=#81c784]OK[/color] Kurze Sessions (15-20 min)\n" +
            "[color=#81c784]OK[/color] Regelmaessigkeit ist wichtig\n" +
            "[color=#81c784]OK[/color] Fehler analysieren\n\n" +
            "[b][color=#4caf50]Du schaffst das![/color][/b]",
            "[b][color=#4fc3f7]Practice Makes Perfect[/color][/b]\n\n" +
            "[b][color=#ffb74d]Training plan:[/color][/b]\n\n" +
            "[b]Weeks 1-2: Basics[/b]\n" +
            "- 3-5 easy puzzles per day\n" +
            "- Focus: Naked and Hidden Singles\n" +
            "- Time doesn't matter — accuracy does\n\n" +
            "[b]Weeks 3-4: Build up[/b]\n" +
            "- 2-3 medium puzzles per day\n" +
            "- Use candidate notes\n" +
            "- Start looking for pairs\n\n" +
            "[b]Week 5+: Advanced[/b]\n" +
            "- Mix of all difficulties\n" +
            "- Track your time and improve\n" +
            "- Learn new techniques\n\n" +
            "[b][color=#ffb74d]Mindset tips:[/color][/b]\n" +
            "[color=#81c784]OK[/color] Short sessions (15-20 min)\n" +
            "[color=#81c784]OK[/color] Consistency matters\n" +
            "[color=#81c784]OK[/color] Review your mistakes\n\n" +
            "[b][color=#4caf50]You've got this![/color][/b]"
            ),
            null,
            LT("", "")
        );
    }

    private static TipData CreateAvoidMistakesTip()
    {
        var values = new int[,] {
            { 5, 0, 0 },
            { 0, 0, 5 },
            { 0, 0, 0 }
        };
        var isGiven = new bool[,] {
            { true, false, false },
            { false, false, true },
            { false, false, false }
        };
        var highlighted = new HashSet<(int, int)> { (1, 1) };
        var related = new HashSet<(int, int)>();

        return new TipData(
            LT("Fehler vermeiden", "Avoid Mistakes"),
            LT(
            "[b][color=#4fc3f7]Haeufige Fehler[/color][/b]\n\n" +
            "[b][color=#f44336]Fehler #1: Raten[/color][/b]\n" +
            "Wenn du nicht sicher bist, ist es keine Loesung!\n\n" +
            "[b][color=#f44336]Fehler #2: Block uebersehen[/color][/b]",
            "[b][color=#4fc3f7]Common Mistakes[/color][/b]\n\n" +
            "[b][color=#f44336]Mistake #1: Guessing[/color][/b]\n" +
            "If you're not sure, it's not a logical move.\n\n" +
            "[b][color=#f44336]Mistake #2: Forgetting the box[/color][/b]"
            ),
            new MiniGridData(values, isGiven, highlighted, related),
            LT(
            "Kann hier eine 5 hin? [color=#f44336]NEIN![/color]\n" +
            "Die 5 ist bereits im Block!\n\n" +
            "[b][color=#f44336]Fehler #3: Kandidaten nicht aktualisieren[/color][/b]\n" +
            "Nach jeder Zahl sofort alle betroffenen Kandidaten entfernen!\n\n" +
            "[b][color=#f44336]Fehler #4: Zu schnell[/color][/b]\n" +
            "Lieber langsam und richtig als schnell und falsch.\n\n" +
            "[b][color=#4fc3f7]Checkliste vor jedem Eintrag:[/color][/b]\n" +
            "[ ] Zahl nicht in Zeile?\n" +
            "[ ] Zahl nicht in Spalte?\n" +
            "[ ] Zahl nicht im Block?\n" +
            "[ ] Keine andere Zahl moeglich?",
            "Can a 5 go here? [color=#f44336]NO![/color]\n" +
            "There is already a 5 in this box.\n\n" +
            "[b][color=#f44336]Mistake #3: Not updating candidates[/color][/b]\n" +
            "After every placement, remove that number from all affected candidates.\n\n" +
            "[b][color=#f44336]Mistake #4: Going too fast[/color][/b]\n" +
            "Better slow and correct than fast and wrong.\n\n" +
            "[b][color=#4fc3f7]Checklist before every entry:[/color][/b]\n" +
            "[ ] Not in the row?\n" +
            "[ ] Not in the column?\n" +
            "[ ] Not in the box?\n" +
            "[ ] No other number possible?"
            )
        );
    }

    #endregion

    public override void _Ready()
    {
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _appState = GetNode<AppState>("/root/AppState");
        _localizationService = GetNode<LocalizationService>("/root/LocalizationService");

        UiNavigationSfx.Wire(this, _audioService);

        _backButton = GetNode<Button>("BackButton");
        _title = GetNode<Label>("Title");
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");

        var contentContainer = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBoxContainer");
        _tipTitle = contentContainer.GetNode<Label>("TipTitle");
        _scrollContainer = contentContainer.GetNode<ScrollContainer>("ScrollContainer");
        _scrollContent = _scrollContainer.GetNode<VBoxContainer>("ScrollContent");

        var navContainer = contentContainer.GetNode<HBoxContainer>("NavigationContainer");
        _prevButton = navContainer.GetNode<Button>("PrevButton");
        _pageLabel = navContainer.GetNode<Label>("PageLabel");
        _nextButton = navContainer.GetNode<Button>("NextButton");

        _backButton.Pressed += OnBackPressed;
        _prevButton.Pressed += OnPrevPressed;
        _nextButton.Pressed += OnNextPressed;

        ApplyTheme();
        ApplyLocalization();
        _themeService.ThemeChanged += OnThemeChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;

        ShowTip(_currentTip);
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _localizationService.LanguageChanged -= OnLanguageChanged;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            OnBackPressed();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_left"))
        {
            OnPrevPressed();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_right"))
        {
            OnNextPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ShowTip(int index)
    {
        if (index < 0 || index >= Tips.Length) return;

        var tip = Tips[index];
        var language = _localizationService.CurrentLanguage;

        _tipTitle.Text = tip.Title.Get(language);
        _pageLabel.Text = $"{index + 1} / {Tips.Length}";

        _prevButton.Disabled = index == 0;
        _nextButton.Disabled = index == Tips.Length - 1;

        // Clear previous content
        foreach (var child in _scrollContent.GetChildren())
        {
            child.QueueFree();
        }

        var colors = _themeService.CurrentColors;

        // Content Before
        var contentBefore = tip.ContentBefore.Get(language);
        if (!string.IsNullOrEmpty(contentBefore))
        {
            var beforeLabel = new RichTextLabel();
            beforeLabel.BbcodeEnabled = true;
            beforeLabel.FitContent = true;
            beforeLabel.ScrollActive = false;
            beforeLabel.Text = contentBefore;
            beforeLabel.AddThemeColorOverride("default_color", colors.TextPrimary);
            beforeLabel.AddThemeFontSizeOverride("normal_font_size", 15);
            _scrollContent.AddChild(beforeLabel);
        }

        // Mini Grid
        if (tip.Grid != null)
        {
            var gridCenter = new CenterContainer();
            _scrollContent.AddChild(gridCenter);

            var miniGrid = MiniGridRenderer.CreateMiniGridWithLegends(
                tip.Grid.Values,
                tip.Grid.IsGiven,
                tip.Grid.HighlightedCells,
                tip.Grid.RelatedCells,
                _themeService,
                colors,
                tip.Grid.SolutionCell,
                tip.Grid.Candidates
            );
            gridCenter.AddChild(miniGrid);
        }

        // Content After
        var contentAfter = tip.ContentAfter.Get(language);
        if (!string.IsNullOrEmpty(contentAfter))
        {
            var afterLabel = new RichTextLabel();
            afterLabel.BbcodeEnabled = true;
            afterLabel.FitContent = true;
            afterLabel.ScrollActive = false;
            afterLabel.Text = contentAfter;
            afterLabel.AddThemeColorOverride("default_color", colors.TextPrimary);
            afterLabel.AddThemeFontSizeOverride("normal_font_size", 15);
            _scrollContent.AddChild(afterLabel);
        }

        // Scroll to top
        _scrollContainer.ScrollVertical = 0;
    }

    private void OnPrevPressed()
    {
        if (_currentTip > 0)
        {
            _currentTip--;
            ShowTip(_currentTip);
        }
    }

    private void OnNextPressed()
    {
        if (_currentTip < Tips.Length - 1)
        {
            _currentTip++;
            ShowTip(_currentTip);
        }
    }

    private void OnBackPressed()
    {
        _audioService.PlayClick();
        _appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
        ShowTip(_currentTip);
    }

    private void OnLanguageChanged(int languageIndex)
    {
        ApplyLocalization();
        ShowTip(_currentTip);
    }

    private void ApplyLocalization()
    {
        _title.Text = _localizationService.Get("tips.title");
        _backButton.Text = _localizationService.Get("menu.back");
        _backButton.TooltipText = _localizationService.Get("settings.back.tooltip");
        _prevButton.Text = _localizationService.Get("tips.nav.prev");
        _prevButton.TooltipText = _localizationService.Get("tips.nav.prev.tooltip");
        _nextButton.Text = _localizationService.Get("tips.nav.next");
        _nextButton.TooltipText = _localizationService.Get("tips.nav.next.tooltip");
    }

    private void ApplyTheme()
    {
        var colors = _themeService.CurrentColors;

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _tipTitle.AddThemeColorOverride("font_color", colors.Accent);
        _pageLabel.AddThemeColorOverride("font_color", colors.TextSecondary);

        var panelStyle = _themeService.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        ApplyButtonTheme(_backButton, _themeService);
        ApplyButtonTheme(_prevButton, _themeService);
        ApplyButtonTheme(_nextButton, _themeService);
    }

    private void ApplyButtonTheme(Button button, ThemeService theme)
    {
        var colors = theme.CurrentColors;
        button.AddThemeStyleboxOverride("normal", theme.CreateButtonStyleBox());
        button.AddThemeStyleboxOverride("hover", theme.CreateButtonStyleBox(hover: true));
        button.AddThemeStyleboxOverride("pressed", theme.CreateButtonStyleBox(pressed: true));
        button.AddThemeStyleboxOverride("disabled", theme.CreateButtonStyleBox(disabled: true));
        button.AddThemeColorOverride("font_color", colors.TextPrimary);
        button.AddThemeColorOverride("font_disabled_color", colors.TextSecondary);
    }
}
