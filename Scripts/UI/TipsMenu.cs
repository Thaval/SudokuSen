namespace MySudoku.UI;

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
        // General Tips
        CreateGeneralStrategiesTip(),
        CreateKeyboardShortcutsTip(),
        CreateMultiSelectTip(),
        CreatePracticeTip(),
        CreateAvoidMistakesTip()
    };

    private record TipData(
        string Title,
        string ContentBefore,
        MiniGridData? Grid,
        string ContentAfter
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
            "Naked Single (Einziger Kandidat)",
            "[b][color=#4fc3f7]Was ist ein Naked Single?[/color][/b]\n\n" +
            "Eine Zelle hat nur [b]eine einzige mögliche Zahl[/b], weil alle anderen 8 Zahlen bereits in der gleichen Zeile, Spalte oder im 3x3-Block vorkommen.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, (1, 1, 5)),
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
            "4. Bleibt nur eine Zahl? Eintragen!"
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
            "Hidden Single (Versteckter Einzelner)",
            "[b][color=#4fc3f7]Was ist ein Hidden Single?[/color][/b]\n\n" +
            "Eine Zahl kann in einer Zeile, Spalte oder Block nur noch an [b]einer einzigen Position[/b] platziert werden.\n\n" +
            "[b][color=#ffb74d]Beispiel: Wo kann die 7 hin?[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, (2, 1, 7)),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Die 7 muss irgendwo in diesem Block platziert werden.\n\n" +
            "Pruefe jede leere Zelle:\n" +
            "[color=#f44336]-[/color] A1: Hat andere Einschraenkungen\n" +
            "[color=#f44336]-[/color] B2, C2: 7 in Spalte blockiert\n" +
            "[color=#4caf50]✓[/color] B3: Einzige Moeglichkeit fuer 7!\n\n" +
            "[b]Unterschied zum Naked Single:[/b]\n" +
            "Naked: Zelle hat nur 1 Kandidat\n" +
            "Hidden: Zahl hat nur 1 Position"
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
            "Scanning-Technik",
            "[b][color=#4fc3f7]Die Scanning-Technik[/color][/b]\n\n" +
            "Gehe systematisch jede Zahl durch und scanne das gesamte Spielfeld nach Positionen wo diese Zahl noch fehlt.\n\n" +
            "[b][color=#ffb74d]Beispiel: Scanning fuer die 5[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "[color=#81c784]✓[/color] Perfekt fuer Zahlen 7-9 (die oft vorkommen)"
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
            "Kandidaten notieren",
            "[b][color=#4fc3f7]Kandidaten notieren[/color][/b]\n\n" +
            "Die Grundlage fuer fortgeschrittene Techniken: Notiere in jeder leeren Zelle alle noch moeglichen Zahlen.\n\n" +
            "[b][color=#ffb74d]Beispiel mit Kandidaten:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
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
            "→ Entferne die Zahl aus allen betroffenen Kandidaten!"
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
            "Naked Pair (Nacktes Paar)",
            "[b][color=#4fc3f7]Naked Pair[/color][/b]\n\n" +
            "Zwei Zellen mit [b]exakt denselben zwei Kandidaten[/b] bilden ein Paar.\n\n" +
            "[b][color=#ffb74d]Beispiel (Zeile):[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            "[b][color=#81c784]Schritt-fuer-Schritt:[/color][/b]\n\n" +
            "[b]1. Finde das Paar:[/b]\n" +
            "   Zelle A1: {3,7}\n" +
            "   Zelle B1: {3,7}\n" +
            "   → Beide haben EXAKT die gleichen Kandidaten!\n\n" +
            "[b]2. Analysiere die Logik:[/b]\n" +
            "   Eine Zelle wird 3, die andere 7.\n" +
            "   Beide Zahlen sind in diesen 2 Zellen 'gefangen'.\n\n" +
            "[b]3. Finde betroffene Zellen:[/b]\n" +
            "   Suche alle anderen Zellen in der GLEICHEN Einheit\n" +
            "   (hier: gleiche Zeile)\n\n" +
            "[b]4. Eliminiere Kandidaten:[/b]\n" +
            "   Zelle C1: {2,3,4} → {2,4} (3 entfernt!)\n" +
            "   Zelle E1: {2,3,7} → {2} (3 und 7 entfernt!)\n\n" +
            "[b][color=#4caf50]Merke:[/color][/b] Naked Pairs funktionieren in Zeilen, Spalten UND Bloecken!"
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
            "Hidden Pair (Verstecktes Paar)",
            "[b][color=#4fc3f7]Hidden Pair[/color][/b]\n\n" +
            "Zwei Zahlen, die in einer Einheit nur in [b]zwei bestimmten Zellen[/b] vorkommen koennen - auch wenn diese Zellen mehr Kandidaten haben.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
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
            "[b]Tipp:[/b] Suche nach Zahlen die nur 2 Positionen haben."
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
            "Naked Triple (Nacktes Tripel)",
            "[b][color=#4fc3f7]Naked Triple[/color][/b]\n\n" +
            "Drei Zellen mit insgesamt [b]maximal drei Kandidaten[/b].\n" +
            "Nicht alle Zellen muessen alle drei haben!\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Zelle A: {2,3}\n" +
            "Zelle B: {3,7}\n" +
            "Zelle C: {2,7}\n\n" +
            "Zusammen: nur {2,3,7} = Naked Triple!\n\n" +
            "[b]Logik:[/b]\n" +
            "Diese 3 Zahlen MUESSEN in diesen 3 Zellen sein.\n" +
            "Also koennen sie aus anderen Zellen der Einheit entfernt werden.\n\n" +
            "[b]Tipp:[/b] Achte auf Zellen mit nur 2 Kandidaten - sie bilden oft Triples!"
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
            "Naked Quad (Nacktes Quartett)",
            "[b][color=#4fc3f7]Naked Quad[/color][/b]\n\n" +
            "Vier Zellen mit insgesamt [b]maximal vier Kandidaten[/b].\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Zelle A: {1,2}\n" +
            "Zelle B: {2,3}\n" +
            "Zelle C: {3,4}\n" +
            "Zelle D: {1,4}\n\n" +
            "Zusammen: nur {1,2,3,4} = Naked Quad!\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "1,2,3,4 aus allen anderen Zellen der Einheit entfernen.\n\n" +
            "[b]Hinweis:[/b]\n" +
            "Quads sind selten, aber wenn du 3 Zellen mit 4+ gemeinsamen Kandidaten siehst, pruefe auf ein Quad!"
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
            "Hidden Triple (Verstecktes Tripel)",
            "[b][color=#4fc3f7]Hidden Triple[/color][/b]\n\n" +
            "Drei Zahlen, die nur in [b]genau drei Zellen[/b] vorkommen koennen.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Wo koennen 1, 2, 3 in dieser Zeile stehen?\n" +
            "1: nur in A oder B\n" +
            "2: nur in A oder C\n" +
            "3: nur in A, B oder C\n\n" +
            "[b]Hidden Triple {1,2,3}![/b]\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "Alle ANDEREN Kandidaten aus A, B, C entfernen!\n" +
            "Die Zellen behalten nur {1,2,3}.\n\n" +
            "[b]Tipp:[/b] Hidden Triples sind schwer zu finden - suche nach Zahlen die nur 2-3 Optionen haben!"
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
            "Jellyfish Technik",
            "[b][color=#4fc3f7]Jellyfish[/color][/b]\n\n" +
            "Erweiterung von Swordfish auf [b]4 Zeilen und 4 Spalten[/b].\n" +
            "Sehr selten, aber aeusserst maechtig!\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 6:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Die 6 kann in 4 Zeilen nur in bestimmten Spalten stehen:\n" +
            "Zeile 1: Spalten A, C, F, H\n" +
            "Zeile 3: Spalten A, C\n" +
            "Zeile 6: Spalten F, H\n" +
            "Zeile 8: Spalten A, H\n\n" +
            "Alle 4 Spalten sind 'abgedeckt' von diesen 4 Zeilen.\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "Die 6 aus allen anderen Zellen in Spalten A, C, F, H entfernen!\n\n" +
            "[b]Hinweis:[/b] Jellyfish sind extrem selten - konzentriere dich erst auf X-Wing und Swordfish!"
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
            "XYZ-Wing Technik",
            "[b][color=#4fc3f7]XYZ-Wing[/color][/b]\n\n" +
            "Wie Y-Wing, aber der Pivot hat [b]drei Kandidaten[/b] statt zwei.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
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
            "Hier: Zelle B1 verliert die 3!"
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
            { (1, 7), new[] { 7 } },          // Strong link cell (nur 7 hier)
            { (5, 7), new[] { 3, 7 } },      // Bi-value cell 2
            { (5, 1), new[] { 3, 5, 8 } }    // Affected cell
        };

        return new TipData(
            "W-Wing Technik",
            "[b][color=#4fc3f7]W-Wing[/color][/b]\n\n" +
            "Zwei Bi-Value Zellen mit [b]identischen Kandidaten[/b],\n" +
            "verbunden durch einen [b]Strong Link[/b].\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Zelle B2: {3,7}\n" +
            "Zelle H6: {3,7} (gleiche Kandidaten!)\n\n" +
            "Diese Zellen sehen sich NICHT direkt.\n" +
            "Aber: Es gibt einen Strong Link auf 7 in Spalte H.\n" +
            "(7 kann in Spalte H nur an 2 Stellen sein)\n\n" +
            "[b]Logik:[/b]\n" +
            "Wenn B2 = 3, dann H6 = 7\n" +
            "Wenn B2 = 7, dann durch Strong Link: auch H6 verbunden\n" +
            "→ Eine der beiden ist immer 3!\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "3 aus Zellen entfernen, die beide Bi-Value Zellen sehen."
        );
    }

    private static TipData CreateSkyscraperTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (1, 2), (1, 6), (5, 2), (7, 6) };
        var related = new HashSet<(int, int)> { (5, 6), (7, 2) };

        return new TipData(
            "Skyscraper Technik",
            "[b][color=#4fc3f7]Skyscraper[/color][/b]\n\n" +
            "Zwei Spalten mit je [b]genau 2 Kandidaten[/b] einer Zahl,\n" +
            "die eine [b]gemeinsame Zeile teilen[/b].\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 4:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "4 aus Zellen entfernen, die BEIDE Spitzen sehen."
        );
    }

    private static TipData CreateTwoStringKiteTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (0, 3), (0, 7), (5, 3), (7, 7) };
        var related = new HashSet<(int, int)> { (5, 7) };

        return new TipData(
            "2-String Kite Technik",
            "[b][color=#4fc3f7]2-String Kite[/color][/b]\n\n" +
            "Ein Kandidat bildet Konjugat-Paare in einer [b]Zeile[/b]\n" +
            "UND einer [b]Spalte[/b], die sich in einem Block treffen.\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 5:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "5 aus Zellen entfernen, die beide Enden sehen."
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
            "Empty Rectangle Technik",
            "[b][color=#4fc3f7]Empty Rectangle[/color][/b]\n\n" +
            "Ein Kandidat bildet eine [b]L-Form[/b] in einem Block\n" +
            "und interagiert mit einem [b]Konjugat-Paar[/b].\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 8:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "[b]Tipp:[/b] Suche nach Bloecken wo ein Kandidat nur in einer L-Form vorkommt!"
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
            "Simple Coloring (Ketten-Faerbung)",
            "[b][color=#4fc3f7]Simple Coloring[/color][/b]\n\n" +
            "Konjugat-Paare werden [b]abwechselnd gefaerbt[/b]\n" +
            "um Widersprueche oder Eliminierungen zu finden.\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 9:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "→ Alle Zellen dieser Farbe sind nicht diese Zahl."
        );
    }

    private static TipData CreatePointingPairTip()
    {
        var values = new int[,] {
            { 0, 0, 0, 5, 0, 0 },
            { 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 5, 0 }
        };
        var isGiven = new bool[,] {
            { false, false, false, true, false, false },
            { false, false, false, false, false, false },
            { false, false, false, false, true, false }
        };
        var highlighted = new HashSet<(int, int)> { (1, 0), (1, 1) };
        var related = new HashSet<(int, int)> { (1, 3), (1, 4), (1, 5) };

        return new TipData(
            "Pointing Pair",
            "[b][color=#4fc3f7]Pointing Pair[/color][/b]\n\n" +
            "Wenn eine Zahl in einem Block nur in einer Zeile oder Spalte moeglich ist, dann 'zeigt' sie darauf.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "[b][color=#4caf50]Tipp:[/color][/b] Auch umgekehrt: Pointing Triple (3 Zellen) funktioniert genauso!"
        );
    }

    private static TipData CreateBoxLineTip()
    {
        var values = new int[,] {
            { 0, 0, 0, 3, 0, 0, 0, 0, 3 }
        };
        var isGiven = new bool[,] {
            { false, false, false, true, false, false, false, false, true }
        };
        var highlighted = new HashSet<(int, int)> { (0, 4), (0, 5) };
        var related = new HashSet<(int, int)> { (0, 0), (0, 1), (0, 2) };

        return new TipData(
            "Box/Line Reduction",
            "[b][color=#4fc3f7]Box/Line Reduction[/color][/b]\n\n" +
            "Das Gegenteil von Pointing Pair: Eine Zahl in einer Zeile/Spalte kann nur in einem Block stehen.\n\n" +
            "[b][color=#ffb74d]Beispiel (Zeile 1):[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "   (Hier: Zellen A2, B2, C2)\n\n" +
            "[b][color=#4caf50]Unterschied zu Pointing Pair:[/color][/b]\n" +
            "Pointing: Block → Zeile/Spalte\n" +
            "Box/Line: Zeile/Spalte → Block"
        );
    }

    private static TipData CreateXWingTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (1, 1), (1, 6), (5, 1), (5, 6) };
        var related = new HashSet<(int, int)> { (2, 1), (4, 6) };

        return new TipData(
            "X-Wing Technik",
            "[b][color=#4fc3f7]X-Wing[/color][/b]\n\n" +
            "Fortgeschrittene Technik: Eine Zahl bildet ein Rechteck-Muster in genau 2 Zeilen und 2 Spalten.\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 7:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "[b][color=#4caf50]Hinweis:[/color][/b] X-Wing funktioniert auch umgekehrt: 2 Spalten mit je 2 Kandidaten in gleichen Zeilen!"
        );
    }

    private static TipData CreateSwordfishTip()
    {
        var values = new int[9, 9];
        var isGiven = new bool[9, 9];
        var highlighted = new HashSet<(int, int)> { (0, 1), (0, 4), (3, 1), (3, 7), (6, 4), (6, 7) };
        var related = new HashSet<(int, int)> { (2, 1), (5, 4), (8, 7) };

        return new TipData(
            "Swordfish Technik",
            "[b][color=#4fc3f7]Swordfish[/color][/b]\n\n" +
            "X-Wing erweitert auf [b]3 Zeilen und 3 Spalten[/b]. Selten, aber sehr maechtig!\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 4:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "[b][color=#f44336]Hinweis:[/color][/b] Swordfish zu finden ist schwierig! Pruefe zuerst immer einfachere Techniken."
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
            "Y-Wing (XY-Wing)",
            "[b][color=#4fc3f7]Y-Wing[/color][/b]\n\n" +
            "Eine Ketten-Technik: 3 Zellen mit je 2 Kandidaten bilden eine 'Y'-Form.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
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
            "[b][color=#4caf50]Merke:[/color][/b] Der Pivot ist immer die Zelle die beide anderen 'verbindet'."
        );
    }

    private static TipData CreateGeneralStrategiesTip()
    {
        return new TipData(
            "Allgemeine Strategien",
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
            null,
            ""
        );
    }

    private static TipData CreateKeyboardShortcutsTip()
    {
        return new TipData(
            "Tastenkuerzel",
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
            null,
            ""
        );
    }

    private static TipData CreateMultiSelectTip()
    {
        return new TipData(
            "Mehrfachauswahl",
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
            null,
            ""
        );
    }

    private static TipData CreatePracticeTip()
    {
        return new TipData(
            "Uebung macht den Meister",
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
            null,
            ""
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
            "Fehler vermeiden",
            "[b][color=#4fc3f7]Haeufige Fehler[/color][/b]\n\n" +
            "[b][color=#f44336]Fehler #1: Raten[/color][/b]\n" +
            "Wenn du nicht sicher bist, ist es keine Loesung!\n\n" +
            "[b][color=#f44336]Fehler #2: Block uebersehen[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
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
            "[ ] Keine andere Zahl moeglich?"
        );
    }

    #endregion

    public override void _Ready()
    {
        _themeService = GetNode<ThemeService>("/root/ThemeService");
        _audioService = GetNode<AudioService>("/root/AudioService");
        _appState = GetNode<AppState>("/root/AppState");

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
        _themeService.ThemeChanged += OnThemeChanged;

        ShowTip(_currentTip);
    }

    public override void _ExitTree()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
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
        _tipTitle.Text = tip.Title;
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
        if (!string.IsNullOrEmpty(tip.ContentBefore))
        {
            var beforeLabel = new RichTextLabel();
            beforeLabel.BbcodeEnabled = true;
            beforeLabel.FitContent = true;
            beforeLabel.ScrollActive = false;
            beforeLabel.Text = tip.ContentBefore;
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
        if (!string.IsNullOrEmpty(tip.ContentAfter))
        {
            var afterLabel = new RichTextLabel();
            afterLabel.BbcodeEnabled = true;
            afterLabel.FitContent = true;
            afterLabel.ScrollActive = false;
            afterLabel.Text = tip.ContentAfter;
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
