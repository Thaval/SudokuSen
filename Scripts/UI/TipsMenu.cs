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

    private int _currentTip = 0;

    // Tip-Daten mit Board-Konfigurationen
    private static readonly TipData[] Tips = new[]
    {
        CreateNakedSingleTip(),
        CreateHiddenSingleTip(),
        CreateScanningTip(),
        CreateCandidatesTip(),
        CreateNakedPairTip(),
        CreateHiddenPairTip(),
        CreatePointingPairTip(),
        CreateBoxLineTip(),
        CreateXWingTip(),
        CreateGeneralStrategiesTip(),
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
            "[color=#f44336]-[/color] (0,0): Hat andere Einschraenkungen\n" +
            "[color=#f44336]-[/color] (1,1), (1,2): 7 in Spalte blockiert\n" +
            "[color=#4caf50]-[/color] (2,1): Einzige Moeglichkeit fuer 7!\n\n" +
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
            { 0, 5, 0, 0, 0, 0, 0, 0, 0 },
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
        var related = new HashSet<(int, int)> { (0, 0), (1, 3), (2, 8), (4, 1), (5, 5) };

        return new TipData(
            "Scanning-Technik",
            "[b][color=#4fc3f7]Die Scanning-Technik[/color][/b]\n\n" +
            "Gehe systematisch jede Zahl durch und scanne das gesamte Spielfeld.\n\n" +
            "[b][color=#ffb74d]Beispiel: Scanning fuer die 5[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
            "[b][color=#81c784]So scannst du:[/color][/b]\n\n" +
            "1. Waehle eine Zahl (hier: 5)\n" +
            "2. Markiere mental alle 5en (orange)\n" +
            "3. Zeichne gedanklich Linien durch alle 5en\n" +
            "4. Finde Bloecke wo nur 1 Platz bleibt\n\n" +
            "[b]Vorteile:[/b]\n" +
            "[color=#81c784]OK[/color] Schnell erlernbar\n" +
            "[color=#81c784]OK[/color] Findet viele einfache Loesungen\n" +
            "[color=#81c784]OK[/color] Keine Notizen noetig"
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
            "Notiere in jeder leeren Zelle alle noch moeglichen Zahlen.\n\n" +
            "[b][color=#ffb74d]Beispiel mit Kandidaten:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            "[b][color=#81c784]Anleitung:[/color][/b]\n\n" +
            "1. Waehle eine leere Zelle\n" +
            "2. Pruefe Zahlen 1-9\n" +
            "3. Notiere alle uebrigen als Kandidaten\n\n" +
            "[b][color=#4fc3f7]Im Spiel:[/color][/b]\n" +
            "Druecke [b]N[/b] fuer den Notiz-Modus.\n" +
            "Dann Zahlen eingeben zum Toggeln.\n\n" +
            "[color=#f44336][b]Wichtig:[/b][/color] Halte Kandidaten aktuell!"
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
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Zellen [A] und [B] haben beide nur {3,7}.\n" +
            "Eine ist 3, die andere 7.\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "3 und 7 koennen aus allen anderen Zellen der Zeile entfernt werden!\n\n" +
            "[b]Ergebnis:[/b]\n" +
            "[C]: 2,4 (3 entfernt)\n" +
            "[D]: 2 (3,7 entfernt)"
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
            "Zwei Zahlen, die nur in [b]zwei bestimmten Zellen[/b] vorkommen koennen.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related, null, candidates),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Wo koennen 1 und 4 in dieser Zeile stehen?\n" +
            "1: nur in [A] oder [C]\n" +
            "4: nur in [A] oder [C]\n\n" +
            "[b]Hidden Pair {1,4}![/b]\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "Alle anderen Kandidaten aus [A] und [C] entfernen!\n" +
            "[A]: nur {1,4}\n" +
            "[C]: nur {1,4}"
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
            "Wenn eine Zahl im Block nur in einer Zeile/Spalte moeglich ist.\n\n" +
            "[b][color=#ffb74d]Beispiel:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Die 5 in Block 1 (links) kann nur in Zeile 2 stehen.\n" +
            "(Zeile 1 und 3 haben bereits 5 in anderen Bloecken)\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "Die 5 fuer diesen Block MUSS in Zeile 2 sein.\n" +
            "Also: 5 aus dem Rest von Zeile 2 entfernen!\n\n" +
            "[b]Merke:[/b] Block-Logik beeinflusst Zeile/Spalte"
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
            "Das Gegenteil von Pointing Pair.\n\n" +
            "[b][color=#ffb74d]Beispiel (Zeile 1):[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Die 3 in Zeile 1 kann nur in Block 2 (Mitte) stehen.\n" +
            "(Block 1 und 3 haben X oder bereits 3)\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "Die 3 fuer diese Zeile ist definitiv in Block 2.\n" +
            "Also: 3 aus anderen Zellen von Block 2 entfernen!\n\n" +
            "[b]Merke:[/b] Zeile/Spalte-Logik beeinflusst Block"
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
            "Fortgeschritten: Eine Zahl in 2 Zeilen nur in denselben 2 Spalten.\n\n" +
            "[b][color=#ffb74d]Beispiel mit der 7:[/color][/b]",
            new MiniGridData(values, isGiven, highlighted, related),
            "[b][color=#81c784]Analyse:[/color][/b]\n\n" +
            "Zeile 2: 7 nur in Spalte 2 oder 7\n" +
            "Zeile 6: 7 nur in Spalte 2 oder 7\n\n" +
            "Das bildet ein Rechteck (X)!\n\n" +
            "[b]Logik:[/b]\n" +
            "Entweder diagonal A oder diagonal B.\n" +
            "In beiden Faellen: je eine 7 pro Spalte.\n\n" +
            "[b]Konsequenz:[/b]\n" +
            "7 aus Spalten 2 und 7 entfernen (ausser X-Wing-Zellen)"
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
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged += OnThemeChanged;

        ShowTip(_currentTip);
    }

    public override void _ExitTree()
    {
        var themeService = GetNode<ThemeService>("/root/ThemeService");
        themeService.ThemeChanged -= OnThemeChanged;
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

        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

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
                theme,
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
        var appState = GetNode<AppState>("/root/AppState");
        appState.GoToMainMenu();
    }

    private void OnThemeChanged(int themeIndex)
    {
        ApplyTheme();
        ShowTip(_currentTip);
    }

    private void ApplyTheme()
    {
        var theme = GetNode<ThemeService>("/root/ThemeService");
        var colors = theme.CurrentColors;

        _title.AddThemeColorOverride("font_color", colors.TextPrimary);
        _tipTitle.AddThemeColorOverride("font_color", colors.Accent);
        _pageLabel.AddThemeColorOverride("font_color", colors.TextSecondary);

        var panelStyle = theme.CreatePanelStyleBox(12, 0);
        _panel.AddThemeStyleboxOverride("panel", panelStyle);

        ApplyButtonTheme(_backButton, theme);
        ApplyButtonTheme(_prevButton, theme);
        ApplyButtonTheme(_nextButton, theme);
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
