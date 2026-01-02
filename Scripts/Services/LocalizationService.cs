namespace SudokuSen.Services;

using System.Text.RegularExpressions;

/// <summary>
/// Supported languages
/// </summary>
public enum Language
{
    German,  // Deutsch (default)
    English  // English
}

/// <summary>
/// Service for internationalization (i18n) - manages translations for German and English
/// </summary>
public partial class LocalizationService : Node
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance!;

    private Language _currentLanguage = Language.English;
    public Language CurrentLanguage => _currentLanguage;

    [Signal]
    public delegate void LanguageChangedEventHandler(int languageIndex);

    // Translation dictionaries
    private static readonly Dictionary<string, Dictionary<Language, string>> Translations = new()
    {
        // ===========================================
        // MAIN MENU
        // ===========================================
        ["menu.title"] = new() { { Language.German, "SudokuSen" }, { Language.English, "SudokuSen" } },
        ["menu.subtitle"] = new() { { Language.German, "Ein klassisches Zahlenrätsel" }, { Language.English, "A classic number puzzle" } },
        ["menu.continue"] = new() { { Language.German, "▶️ Fortsetzen" }, { Language.English, "▶️ Continue" } },
        ["menu.continue.tooltip"] = new() { { Language.German, "Setzt dein letztes Spiel fort" }, { Language.English, "Continue your last game" } },
        ["menu.new_game"] = new() { { Language.German, "🆕 Neues Spiel" }, { Language.English, "🆕 New Game" } },
        ["menu.new_game.tooltip"] = new() { { Language.German, "Startet ein neues Sudoku-Rätsel mit wählbarer Schwierigkeit" }, { Language.English, "Start a new Sudoku puzzle with selectable difficulty" } },
        ["menu.daily"] = new() { { Language.German, "📅 Daily Challenge" }, { Language.English, "📅 Daily Challenge" } },
        ["menu.scenarios"] = new() { { Language.German, "🎯 Szenarien" }, { Language.English, "🎯 Scenarios" } },
        ["menu.scenarios.tooltip"] = new() { { Language.German, "Vordefinierte Puzzle-Situationen zum Üben bestimmter Techniken" }, { Language.English, "Predefined puzzle situations to practice specific techniques" } },
        ["menu.tips"] = new() { { Language.German, "💡 Tipps & Tricks" }, { Language.English, "💡 Tips & Tricks" } },
        ["menu.tips.tooltip"] = new() { { Language.German, "Lerne Sudoku-Strategien und Lösungstechniken" }, { Language.English, "Learn Sudoku strategies and solving techniques" } },
        ["menu.puzzles"] = new() { { Language.German, "🧩 Puzzles" }, { Language.English, "🧩 Puzzles" } },
        ["menu.puzzles.tooltip"] = new() { { Language.German, "Durchstöbere vorgefertigte Sudoku-Rätsel" }, { Language.English, "Browse prebuilt Sudoku puzzles" } },
        ["menu.history"] = new() { { Language.German, "📜 Verlauf" }, { Language.English, "📜 History" } },
        ["menu.history.tooltip"] = new() { { Language.German, "Zeigt alle gespielten Partien und deren Ergebnisse" }, { Language.English, "Shows all played games and their results" } },
        ["menu.stats"] = new() { { Language.German, "📊 Statistik" }, { Language.English, "📊 Statistics" } },
        ["menu.stats.tooltip"] = new() { { Language.German, "Deine Spielstatistik pro Schwierigkeit" }, { Language.English, "Your game statistics by difficulty" } },
        ["menu.settings"] = new() { { Language.German, "⚙️ Einstellungen" }, { Language.English, "⚙️ Settings" } },
        ["menu.settings.tooltip"] = new() { { Language.German, "Spieleinstellungen, Audio und Darstellung anpassen" }, { Language.English, "Adjust game settings, audio, and appearance" } },
        ["menu.quit"] = new() { { Language.German, "🚪 Beenden" }, { Language.English, "🚪 Quit" } },
        ["menu.quit.tooltip"] = new() { { Language.German, "Beendet das Spiel (aktuelles Spiel wird automatisch gespeichert)" }, { Language.English, "Quit the game (current game is saved automatically)" } },
        ["menu.back"] = new() { { Language.German, "← Zurück" }, { Language.English, "← Back" } },

        // ===========================================
        // DIFFICULTY MENU
        // ===========================================
        ["difficulty.title"] = new() { { Language.German, "Schwierigkeit wählen" }, { Language.English, "Choose Difficulty" } },
        ["difficulty.description"] = new() { { Language.German, "Wähle den Schwierigkeitsgrad für dein neues Spiel" }, { Language.English, "Choose the difficulty level for your new game" } },
        ["difficulty.kids"] = new() { { Language.German, "👶 Kids" }, { Language.English, "👶 Kids" } },
        ["difficulty.kids.desc"] = new() { { Language.German, "4x4 Raster, Zahlen 1-4" }, { Language.English, "4x4 grid, numbers 1-4" } },
        ["difficulty.kids.tooltip"] = new() { { Language.German, "Perfekt für Anfänger - vereinfachtes 4x4 Raster" }, { Language.English, "Perfect for beginners - simplified 4x4 grid" } },
        ["difficulty.easy"] = new() { { Language.German, "🟢 Leicht" }, { Language.English, "🟢 Easy" } },
        ["difficulty.easy.desc"] = new() { { Language.German, "Naked Single, Hidden Single" }, { Language.English, "Naked Single, Hidden Single" } },
        ["difficulty.easy.tooltip"] = new() { { Language.German, "Grundlegende Techniken - ideal zum Einstieg ins 9x9 Sudoku" }, { Language.English, "Basic techniques - ideal for getting started with 9x9 Sudoku" } },
        ["difficulty.medium"] = new() { { Language.German, "🟠 Mittel" }, { Language.English, "🟠 Medium" } },
        ["difficulty.medium.desc"] = new() { { Language.German, "+ Naked Pair, Pointing Pair" }, { Language.English, "+ Naked Pair, Pointing Pair" } },
        ["difficulty.medium.tooltip"] = new() { { Language.German, "Fortgeschrittene Techniken - erfordert Notizen und Paar-Erkennung" }, { Language.English, "Advanced techniques - requires notes and pair recognition" } },
        ["difficulty.hard"] = new() { { Language.German, "🔴 Schwer" }, { Language.English, "🔴 Hard" } },
        ["difficulty.hard.desc"] = new() { { Language.German, "+ X-Wing, Box/Line Reduction" }, { Language.English, "+ X-Wing, Box/Line Reduction" } },
        ["difficulty.hard.tooltip"] = new() { { Language.German, "Experten-Techniken - X-Wing, Swordfish und mehr" }, { Language.English, "Expert techniques - X-Wing, Swordfish and more" } },
        ["difficulty.insane"] = new() { { Language.German, "💀 Insane" }, { Language.English, "💀 Insane" } },
        ["difficulty.insane.desc"] = new() { { Language.German, "Alle Techniken, ~21 Hinweise" }, { Language.English, "All techniques, ~21 clues" } },
        ["difficulty.insane.tooltip"] = new() { { Language.German, "Für absolute Experten - Minimale Hinweise, alle Techniken erforderlich" }, { Language.English, "For absolute experts - Minimal clues, all techniques required" } },

        // ===========================================
        // GAME SCENE
        // ===========================================
        ["game.difficulty"] = new() { { Language.German, "Schwierigkeit" }, { Language.English, "Difficulty" } },
        ["game.difficulty.kids"] = new() { { Language.German, "Kids" }, { Language.English, "Kids" } },
        ["game.difficulty.easy"] = new() { { Language.German, "Leicht" }, { Language.English, "Easy" } },
        ["game.difficulty.medium"] = new() { { Language.German, "Mittel" }, { Language.English, "Medium" } },
        ["game.difficulty.hard"] = new() { { Language.German, "Schwer" }, { Language.English, "Hard" } },
        ["game.difficulty.insane"] = new() { { Language.German, "Insane" }, { Language.English, "Insane" } },
        ["game.deadly"] = new() { { Language.German, "(Deadly)" }, { Language.English, "(Deadly)" } },
        ["game.daily"] = new() { { Language.German, "(Daily)" }, { Language.English, "(Daily)" } },
        ["game.mistakes"] = new() { { Language.German, "Fehler" }, { Language.English, "Mistakes" } },
        ["game.mistakes.label"] = new() { { Language.German, "Fehler: {0}" }, { Language.English, "Mistakes: {0}" } },
        ["game.hints"] = new() { { Language.German, "Hinweise" }, { Language.English, "Hints" } },
        ["game.pause"] = new() { { Language.German, "Pause" }, { Language.English, "Pause" } },
        ["game.resume"] = new() { { Language.German, "Fortsetzen" }, { Language.English, "Resume" } },
        ["game.new_game"] = new() { { Language.German, "Neues Spiel" }, { Language.English, "New Game" } },
        ["game.give_up"] = new() { { Language.German, "Aufgeben" }, { Language.English, "Give Up" } },
        ["game.hint"] = new() { { Language.German, "Hinweis" }, { Language.English, "Hint" } },
        ["game.notes"] = new() { { Language.German, "Notizen" }, { Language.English, "Notes" } },
        ["game.erase"] = new() { { Language.German, "Löschen" }, { Language.English, "Erase" } },
        ["game.undo"] = new() { { Language.German, "Rückgängig" }, { Language.English, "Undo" } },
        ["game.redo"] = new() { { Language.German, "Wiederholen" }, { Language.English, "Redo" } },
        ["game.solutionpath.title"] = new() { { Language.German, "Lösungsweg" }, { Language.English, "Solution Path" } },
        ["game.solutionpath.tooltip"] = new() { { Language.German, "Zeigt die logischen Schritte an und springt zu jedem Schritt" }, { Language.English, "Show the logical steps and jump to any step" } },
        ["game.solutionpath.placement"] = new() { { Language.German, "{0}{1} = {2}" }, { Language.English, "{0}{1} = {2}" } },
        ["game.solutionpath.elimination"] = new() { { Language.German, "{0}{1} ≠ {2}" }, { Language.English, "{0}{1} ≠ {2}" } },
        ["game.solutionpath.set"] = new() { { Language.German, "Setzen" }, { Language.English, "Set" } },
        ["game.solutionpath.unset"] = new() { { Language.German, "Zurück" }, { Language.English, "Unset" } },
        ["game.solutionpath.tooltip.placement"] = new() { { Language.German, "Setze {1} auf {0}" }, { Language.English, "Place {1} at {0}" } },
        ["game.solutionpath.tooltip.elimination"] = new() { { Language.German, "Streiche {1} aus {0}" }, { Language.English, "Eliminate {1} from {0}" } },
        ["game.solutionpath.tooltip.no_related"] = new() { { Language.German, "Keine verwandten Zellen" }, { Language.English, "No related cells" } },
        ["game.solutionpath.tooltip.template"] = new() { { Language.German, "Schritt {0}: {1}\n{2}\nWarum: {3}\nVerwandt: {4}" }, { Language.English, "Step {0}: {1}\n{2}\nWhy: {3}\nRelated: {4}" } },

        // ===========================================
        // WIN/LOSE DIALOGS
        // ===========================================
        ["dialog.win.title"] = new() { { Language.German, "🎉 Gewonnen!" }, { Language.English, "🎉 You Won!" } },
        ["dialog.win.message"] = new() { { Language.German, "Herzlichen Glückwunsch!" }, { Language.English, "Congratulations!" } },
        ["dialog.win.time"] = new() { { Language.German, "Zeit" }, { Language.English, "Time" } },
        ["dialog.win.mistakes"] = new() { { Language.German, "Fehler" }, { Language.English, "Mistakes" } },
        ["dialog.win.hints"] = new() { { Language.German, "Hinweise" }, { Language.English, "Hints" } },
        ["dialog.lose.title"] = new() { { Language.German, "💀 Verloren!" }, { Language.English, "💀 Game Over!" } },
        ["dialog.lose.message"] = new() { { Language.German, "Zu viele Fehler!" }, { Language.English, "Too many mistakes!" } },
        ["dialog.giveup.title"] = new() { { Language.German, "Aufgeben?" }, { Language.English, "Give Up?" } },
        ["dialog.giveup.message"] = new() { { Language.German, "Möchtest du wirklich aufgeben?" }, { Language.English, "Do you really want to give up?" } },
        ["dialog.yes"] = new() { { Language.German, "Ja" }, { Language.English, "Yes" } },
        ["dialog.no"] = new() { { Language.German, "Nein" }, { Language.English, "No" } },
        ["dialog.ok"] = new() { { Language.German, "OK" }, { Language.English, "OK" } },
        ["dialog.close"] = new() { { Language.German, "Schließen" }, { Language.English, "Close" } },
        ["dialog.cancel"] = new() { { Language.German, "Abbrechen" }, { Language.English, "Cancel" } },

        // ===========================================
        // SETTINGS MENU
        // ===========================================
        ["settings.title"] = new() { { Language.German, "Einstellungen" }, { Language.English, "Settings" } },
        ["settings.back.tooltip"] = new() { { Language.German, "Zurück zum Hauptmenü (Einstellungen werden automatisch gespeichert)" }, { Language.English, "Return to the main menu (settings are saved automatically)" } },

        ["settings.storage.title"] = new() { { Language.German, "📁 Speicherort" }, { Language.English, "📁 Storage Location" } },
        ["settings.storage.path"] = new() { { Language.German, "Pfad" }, { Language.English, "Path" } },
        ["settings.storage.placeholder"] = new() { { Language.German, "Standard (user://)" }, { Language.English, "Default (user://)" } },
        ["settings.storage.tooltip"] = new() { { Language.German, "Ändert den Speicherort für Spielstände und Einstellungen" }, { Language.English, "Changes the storage location for saves and settings" } },
        ["settings.storage.browse.tooltip"] = new() { { Language.German, "Ordner auswählen" }, { Language.English, "Choose folder" } },
        ["settings.storage.info.placeholder"] = new() { { Language.German, "Aktueller Speicherort wird unten angezeigt" }, { Language.English, "Current storage location is shown below" } },
        ["settings.storage.info"] = new() { { Language.German, "📂 {0}" }, { Language.English, "📂 {0}" } },
        ["settings.storage.invalid"] = new() { { Language.German, "❌ Ungültiger Pfad - konnte nicht erstellt werden" }, { Language.English, "❌ Invalid path - could not be created" } },
        ["settings.storage.select_title"] = new() { { Language.German, "Speicherort auswählen" }, { Language.English, "Select storage location" } },

        ["settings.appearance.title"] = new() { { Language.German, "🎨 Darstellung" }, { Language.English, "🎨 Appearance" } },
        ["settings.language"] = new() { { Language.German, "Sprache" }, { Language.English, "Language" } },
        ["settings.language.label"] = new() { { Language.German, "Sprache / Language" }, { Language.English, "Language" } },
        ["settings.language.tooltip"] = new() { { Language.German, "Wählt die Sprache der Benutzeroberfläche" }, { Language.English, "Selects the UI language" } },
        ["settings.language.german"] = new() { { Language.German, "Deutsch" }, { Language.English, "German" } },
        ["settings.language.english"] = new() { { Language.German, "Englisch" }, { Language.English, "English" } },

        ["settings.theme"] = new() { { Language.German, "Design" }, { Language.English, "Theme" } },
        ["settings.theme.light"] = new() { { Language.German, "Hell" }, { Language.English, "Light" } },
        ["settings.theme.dark"] = new() { { Language.German, "Dunkel" }, { Language.English, "Dark" } },
        ["settings.theme.tooltip"] = new() { { Language.German, "Wählt das Farbschema der Benutzeroberfläche" }, { Language.English, "Selects the UI color theme" } },

        ["settings.deadly_mode"] = new() { { Language.German, "Deadly Mode (3 Fehler = Game Over)" }, { Language.English, "Deadly Mode (3 mistakes = Game Over)" } },
        ["settings.deadly_mode.tooltip"] = new() { { Language.German, "Bei aktiviertem Deadly Modus endet das Spiel nach 3 Fehlern" }, { Language.English, "When enabled, the game ends after 3 mistakes" } },
        ["settings.hide_completed"] = new() { { Language.German, "Vollständige Zahlen ausblenden" }, { Language.English, "Hide completed numbers" } },
        ["settings.hide_completed.tooltip"] = new() { { Language.German, "Zahlen die 9x im Raster vorkommen werden ausgeblendet oder ausgegraut" }, { Language.English, "Numbers that appear 9x in the grid are hidden or dimmed" } },
        ["settings.highlight_cells"] = new() { { Language.German, "Zeile/Spalte/Block highlighten" }, { Language.English, "Highlight row/column/block" } },
        ["settings.highlight_cells.tooltip"] = new() { { Language.German, "Hebt die aktuelle Zeile, Spalte und Block hervor" }, { Language.English, "Highlights the current row, column, and block" } },
        ["settings.learn_mode"] = new() { { Language.German, "Lernmodus (Erklärung bei Fehlern)" }, { Language.English, "Learn mode (explain mistakes)" } },
        ["settings.learn_mode.tooltip"] = new() { { Language.German, "Zeigt eine kurze Erklärung bei Fehlern an" }, { Language.English, "Shows a short explanation when you make a mistake" } },
        ["settings.colorblind"] = new() { { Language.German, "Farbblind-Palette" }, { Language.English, "Colorblind palette" } },
        ["settings.colorblind.tooltip"] = new() { { Language.German, "Verwendet kontrastreiche Farben für bessere Sichtbarkeit" }, { Language.English, "Uses high-contrast colors for better visibility" } },
        ["settings.ui_scale.label"] = new() { { Language.German, "UI Größe" }, { Language.English, "UI Size" } },
        ["settings.ui_scale.tooltip"] = new() { { Language.German, "UI-Skalierung ({0}% - {1}%)\nEmpfohlen: {2}%" }, { Language.English, "UI scale ({0}% - {1}%)\nRecommended: {2}%" } },

        // Puzzles settings
        ["settings.puzzles.title"] = new() { { Language.German, "🧩 Puzzle-Quelle" }, { Language.English, "🧩 Puzzle Source" } },
        ["settings.puzzles.mode"] = new() { { Language.German, "Puzzle-Modus" }, { Language.English, "Puzzle Mode" } },
        ["settings.puzzles.mode.tooltip"] = new() { { Language.German, "Wähle ob du vorgebaute Puzzles, dynamisch erzeugte Puzzles oder beide haben möchtest" }, { Language.English, "Choose whether you want prebuilt puzzles, dynamically generated puzzles, or both" } },
        ["settings.puzzles.mode.both"] = new() { { Language.German, "Beide (vorgebaut + dynamisch)" }, { Language.English, "Both (prebuilt + dynamic)" } },
        ["settings.puzzles.mode.prebuilt"] = new() { { Language.German, "Nur vorgebaute Puzzles" }, { Language.English, "Prebuilt only" } },
        ["settings.puzzles.mode.dynamic"] = new() { { Language.German, "Nur dynamisch erzeugt" }, { Language.English, "Dynamic only" } },

        ["settings.sound"] = new() { { Language.German, "Sound" }, { Language.English, "Sound" } },
        ["settings.sound.effects"] = new() { { Language.German, "Sound-Effekte" }, { Language.English, "Sound Effects" } },
        ["settings.sound.effects.tooltip"] = new() { { Language.German, "Aktiviert oder deaktiviert alle Sound-Effekte" }, { Language.English, "Enables or disables all sound effects" } },
        ["settings.sound.volume"] = new() { { Language.German, "SFX-Lautstärke" }, { Language.English, "SFX Volume" } },
        ["settings.sound.volume.tooltip"] = new() { { Language.German, "Lautstärke der Sound-Effekte (0-100%)" }, { Language.English, "Volume of sound effects (0-100%)" } },

        ["settings.music"] = new() { { Language.German, "Musik" }, { Language.English, "Music" } },
        ["settings.music.enabled"] = new() { { Language.German, "Musik aktiviert" }, { Language.English, "Music Enabled" } },
        ["settings.music.tooltip"] = new() { { Language.German, "Aktiviert oder deaktiviert die Hintergrundmusik" }, { Language.English, "Enables or disables background music" } },
        ["settings.music.volume"] = new() { { Language.German, "Musik-Lautstärke" }, { Language.English, "Music Volume" } },
        ["settings.music.volume.tooltip"] = new() { { Language.German, "Lautstärke der Hintergrundmusik (0-100%)" }, { Language.English, "Volume of background music (0-100%)" } },
        ["settings.music.menu"] = new() { { Language.German, "Menü-Musik" }, { Language.English, "Menu Music" } },
        ["settings.music.game"] = new() { { Language.German, "Spiel-Musik" }, { Language.English, "Game Music" } },

        ["settings.notes.title"] = new() { { Language.German, "✏️ Notiz-Assistent" }, { Language.English, "✏️ Notes Assistant" } },
        ["settings.notes.smart_cleanup"] = new() { { Language.German, "Notizen bereinigen (nach Zahl setzen)" }, { Language.English, "Clean up notes (after placing number)" } },
        ["settings.notes.smart_cleanup.tooltip"] = new() { { Language.German, "Entfernt automatisch Notizen aus Zeile/Spalte/Block wenn eine Zahl gesetzt wird" }, { Language.English, "Automatically removes notes in row/column/block when a number is placed" } },
        ["settings.notes.house_autofill"] = new() { { Language.German, "Auto-Notizen Button im Spiel" }, { Language.English, "Auto-notes button in game" } },
        ["settings.notes.house_autofill.tooltip"] = new() { { Language.German, "Zeigt einen Button zum automatischen Ausfüllen von Notizen im Spiel" }, { Language.English, "Shows a button to auto-fill notes during the game" } },

        ["settings.challenge.title"] = new() { { Language.German, "🏆 Challenge Modes (für neue Spiele)" }, { Language.English, "🏆 Challenge modes (for new games)" } },
        ["settings.challenge.difficulty"] = new() { { Language.German, "Schwierigkeit" }, { Language.English, "Difficulty" } },
        ["settings.challenge.difficulty.tooltip"] = new() { { Language.German, "Schwierigkeit für Challenge-Spiele. Auto wählt basierend auf deiner Spielhistorie." }, { Language.English, "Difficulty for challenge games. Auto picks based on your play history." } },
        ["settings.challenge.auto"] = new() { { Language.German, "Auto" }, { Language.English, "Auto" } },
        ["settings.challenge.easy"] = new() { { Language.German, "Leicht" }, { Language.English, "Easy" } },
        ["settings.challenge.medium"] = new() { { Language.German, "Mittel" }, { Language.English, "Medium" } },
        ["settings.challenge.hard"] = new() { { Language.German, "Schwer" }, { Language.English, "Hard" } },
        ["settings.challenge.no_notes"] = new() { { Language.German, "Keine Notizen" }, { Language.English, "No Notes" } },
        ["settings.challenge.no_notes.tooltip"] = new() { { Language.German, "Deaktiviert das Notizen-Feature für neue Spiele" }, { Language.English, "Disables notes for new games" } },
        ["settings.challenge.perfect"] = new() { { Language.German, "Perfect Run (1 Fehler = verloren)" }, { Language.English, "Perfect Run (1 mistake = lost)" } },
        ["settings.challenge.perfect.tooltip"] = new() { { Language.German, "Nur ein einziger Fehler beendet das Spiel" }, { Language.English, "A single mistake ends the game" } },
        ["settings.challenge.hints"] = new() { { Language.German, "Hint-Limit" }, { Language.English, "Hint Limit" } },
        ["settings.challenge.hints.tooltip"] = new() { { Language.German, "Begrenzt die Anzahl der verfügbaren Hinweise pro Spiel" }, { Language.English, "Limits the number of hints per game" } },
        ["settings.challenge.time"] = new() { { Language.German, "Time Attack" }, { Language.English, "Time Attack" } },
        ["settings.challenge.time.tooltip"] = new() { { Language.German, "Setzt ein Zeitlimit für das Spiel" }, { Language.English, "Sets a time limit for the game" } },

        ["settings.techniques.title"] = new() { { Language.German, "🧠 Techniken pro Schwierigkeit" }, { Language.English, "🧠 Techniques per difficulty" } },
        ["settings.techniques.description"] = new() { { Language.German, "Wähle welche Lösungstechniken pro Schwierigkeit benötigt werden." }, { Language.English, "Choose which solving techniques are required per difficulty." } },
        ["settings.techniques.active"] = new() { { Language.German, "Aktive Techniken:" }, { Language.English, "Active techniques:" } },
        ["settings.techniques.reset"] = new() { { Language.German, "🔄 Auf Standard zurücksetzen" }, { Language.English, "🔄 Reset to default" } },
        ["settings.techniques.reset.tooltip"] = new() { { Language.German, "Setzt alle Techniken-Einstellungen auf die Standardwerte zurück" }, { Language.English, "Reset all technique settings to their defaults" } },
        ["settings.technique.level.easy"] = new() { { Language.German, "Leicht" }, { Language.English, "Easy" } },
        ["settings.technique.level.medium"] = new() { { Language.German, "Mittel" }, { Language.English, "Medium" } },
        ["settings.technique.level.hard"] = new() { { Language.German, "Schwer" }, { Language.English, "Hard" } },

        ["settings.technique.tooltip"] = new() { { Language.German, "{0}\n\nSchwierigkeit: {1}" }, { Language.English, "{0}\n\nDifficulty: {1}" } },

        ["settings.common.off"] = new() { { Language.German, "Aus" }, { Language.English, "Off" } },
        ["settings.time.minutes"] = new() { { Language.German, "{0} min" }, { Language.English, "{0} min" } },

        // ===========================================
        // STATS MENU
        // ===========================================
        ["stats.title"] = new() { { Language.German, "Statistik" }, { Language.English, "Statistics" } },
        ["stats.overview"] = new() { { Language.German, "Übersicht" }, { Language.English, "Overview" } },
        ["stats.total_games"] = new() { { Language.German, "Gespielte Spiele" }, { Language.English, "Games Played" } },
        ["stats.wins"] = new() { { Language.German, "Gewonnen" }, { Language.English, "Won" } },
        ["stats.losses"] = new() { { Language.German, "Verloren" }, { Language.English, "Lost" } },
        ["stats.win_rate"] = new() { { Language.German, "Gewinnrate" }, { Language.English, "Win Rate" } },
        ["stats.times"] = new() { { Language.German, "⏱️ Zeiten" }, { Language.English, "⏱️ Times" } },
        ["stats.best_time"] = new() { { Language.German, "🏆 Beste Zeit" }, { Language.English, "🏆 Best Time" } },
        ["stats.worst_time"] = new() { { Language.German, "🐢 Längste Zeit" }, { Language.English, "🐢 Longest Time" } },
        ["stats.avg_time"] = new() { { Language.German, "Ø Zeit" }, { Language.English, "Avg Time" } },
        ["stats.mistakes_title"] = new() { { Language.German, "⚠️ Fehler" }, { Language.English, "⚠️ Mistakes" } },
        ["stats.avg_mistakes"] = new() { { Language.German, "Ø Fehler" }, { Language.English, "Avg Mistakes" } },
        ["stats.daily"] = new() { { Language.German, "📅 Daily Challenge" }, { Language.English, "📅 Daily Challenge" } },
        ["stats.streak"] = new() { { Language.German, "🔥 Streak" }, { Language.English, "🔥 Streak" } },
        ["stats.best_streak"] = new() { { Language.German, "⭐ Best" }, { Language.English, "⭐ Best" } },
        ["stats.today"] = new() { { Language.German, "📅 Heute" }, { Language.English, "📅 Today" } },
        ["stats.today.done"] = new() { { Language.German, "✅ erledigt" }, { Language.English, "✅ completed" } },
        ["stats.today.open"] = new() { { Language.German, "⏳ offen" }, { Language.English, "⏳ open" } },
        ["stats.techniques"] = new() { { Language.German, "🧠 Techniken" }, { Language.English, "🧠 Techniques" } },
        ["stats.scenarios"] = new() { { Language.German, "🎯 Szenarien" }, { Language.English, "🎯 Scenarios" } },
        ["stats.heatmap"] = new() { { Language.German, "🔥 Fehler-Heatmap" }, { Language.English, "🔥 Mistake Heatmap" } },
        ["stats.total_games_line"] = new() { { Language.German, "🎮  Spiele gesamt: {0}" }, { Language.English, "🎮  Games played: {0}" } },
        ["stats.wins_losses_line"] = new() { { Language.German, "✅ Gewonnen: {0}   ❌ Verloren: {1}" }, { Language.English, "✅ Won: {0}   ❌ Lost: {1}" } },
        ["stats.win_rate_line"] = new() { { Language.German, "📊 Gewinnrate: {0:F1}%" }, { Language.English, "📊 Win rate: {0:F1}%" } },
        ["stats.best_time_line"] = new() { { Language.German, "🏆 Beste Zeit: {0}" }, { Language.English, "🏆 Best time: {0}" } },
        ["stats.worst_time_line"] = new() { { Language.German, "🐢 Längste Zeit: {0}" }, { Language.English, "🐢 Longest time: {0}" } },
        ["stats.avg_time_difficulty"] = new() { { Language.German, "    {0}: {1}" }, { Language.English, "    {0}: {1}" } },
        ["stats.avg_mistakes_difficulty"] = new() { { Language.German, "    {0}: {1} Fehler" }, { Language.English, "    {0}: {1} mistakes" } },
        ["stats.daily.streak_line"] = new() { { Language.German, "🔥 Streak: {0}   ⭐ Best: {1}" }, { Language.English, "🔥 Streak: {0}   ⭐ Best: {1}" } },
        ["stats.daily.today_done_line"] = new() { { Language.German, "📅 Heute: ✅ erledigt" }, { Language.English, "📅 Today: ✅ done" } },
        ["stats.daily.today_open_line"] = new() { { Language.German, "📅 Heute: ⏳ offen" }, { Language.English, "📅 Today: ⏳ open" } },
        ["stats.techniques.none"] = new() { { Language.German, "[i]Noch keine Technik-Daten.[/i]\n\nNutze 💡 Hinweise im Spiel, um Techniken zu lernen!" }, { Language.English, "[i]No technique data yet.[/i]\n\nUse 💡 hints in-game to learn techniques!" } },
        ["stats.techniques.header"] = new() { { Language.German, "[b]🎓 Meist gesehene Hinweise:[/b]\n\n" }, { Language.English, "[b]🎓 Most viewed hints:[/b]\n\n" } },
        ["stats.techniques.line"] = new() { { Language.German, "• [b]{0}[/b]\n   Gesehen: {1}x  Angewendet: {2}x  ({3:F0}%)\n   {4}\n\n" }, { Language.English, "• [b]{0}[/b]\n   Seen: {1}x  Applied: {2}x  ({3:F0}%)\n   {4}\n\n" } },
        ["stats.scenario.none"] = new() { { Language.German, "[i]Noch keine Szenarien gespielt.[/i]\n\nSpiele 🎯 Übungs-Szenarien im Szenarien-Menü!" }, { Language.English, "[i]No scenarios played yet.[/i]\n\nPlay 🎯 practice scenarios from the Scenarios menu!" } },
        ["stats.week_label"] = new() { { Language.German, "KW{0}" }, { Language.English, "W{0}" } },
        ["stats.daily_recent.header"] = new() { { Language.German, "[b]Letzte 2 Wochen:[/b]\n\n" }, { Language.English, "[b]Last 2 weeks:[/b]\n\n" } },
        ["stats.scenario.overview_header"] = new() { { Language.German, "[b]📊 Übersicht:[/b]\n" }, { Language.English, "[b]📊 Overview:[/b]\n" } },
        ["stats.scenario.overview_line"] = new() { { Language.German, "   Gespielt: {0}   Gewonnen: {1}   ({2:F0}%)\n\n" }, { Language.English, "   Played: {0}   Won: {1}   ({2:F0}%)\n\n" } },
        ["stats.scenario.by_tech_header"] = new() { { Language.German, "[b]🎯 Pro Technik:[/b]\n\n" }, { Language.English, "[b]🎯 By technique:[/b]\n\n" } },
        ["stats.scenario.line"] = new() { { Language.German, "• [b]{0}[/b]\n   {1}/{2} gewonnen ({3:F0}%)  ⏱️ Beste: {4}\n   {5}\n\n" }, { Language.English, "• [b]{0}[/b]\n   {1}/{2} won ({3:F0}%)  ⏱️ Best: {4}\n   {5}\n\n" } },

        // ===========================================
        // HISTORY MENU
        // ===========================================
        ["history.title"] = new() { { Language.German, "Spielverlauf" }, { Language.English, "Game History" } },
        ["history.no_games"] = new() { { Language.German, "Noch keine Spiele gespielt" }, { Language.English, "No games played yet" } },
        ["history.won"] = new() { { Language.German, "Gewonnen" }, { Language.English, "Won" } },
        ["history.lost"] = new() { { Language.German, "Verloren" }, { Language.English, "Lost" } },
        ["history.abandoned"] = new() { { Language.German, "Aufgegeben" }, { Language.English, "Abandoned" } },
        ["history.in_progress"] = new() { { Language.German, "Läuft" }, { Language.English, "In Progress" } },
        ["history.badge.tutorial"] = new() { { Language.German, "📚 Tutorial" }, { Language.English, "📚 Tutorial" } },
        ["history.badge.scenario"] = new() { { Language.German, "🎯 {0}" }, { Language.English, "🎯 {0}" } },
        ["history.badge.daily"] = new() { { Language.German, "📅 Daily" }, { Language.English, "📅 Daily" } },
        ["history.time"] = new() { { Language.German, "⏱️ Zeit: {0}" }, { Language.English, "⏱️ Time: {0}" } },
        ["history.mistakes"] = new() { { Language.German, "⚠️ Fehler: {0}" }, { Language.English, "⚠️ Mistakes: {0}" } },
        ["history.replay.steps"] = new() { { Language.German, "Schritt {0}/{1}" }, { Language.English, "Step {0}/{1}" } },
        ["history.replay.jump.title"] = new() { { Language.German, "Zu Schritt springen" }, { Language.English, "Jump to Step" } },
        ["history.replay.jump.prompt"] = new() { { Language.German, "Schrittnummer eingeben:" }, { Language.English, "Enter step number:" } },

        // ===========================================
        // SCENARIOS MENU
        // ===========================================
        ["scenarios.title"] = new() { { Language.German, "Szenarien & Tutorials" }, { Language.English, "Scenarios & Tutorials" } },
        ["scenarios.tutorials"] = new() { { Language.German, "📚 Tutorials" }, { Language.English, "📚 Tutorials" } },
        ["scenarios.practice"] = new() { { Language.German, "🎯 Technik-Training" }, { Language.English, "🎯 Technique Practice" } },
        ["scenarios.minutes"] = new() { { Language.German, "Min." }, { Language.English, "min" } },
        ["scenarios.completed"] = new() { { Language.German, "✓ Abgeschlossen" }, { Language.English, "✓ Completed" } },

        // ===========================================
        // PUZZLES MENU
        // ===========================================
        ["puzzles.title"] = new() { { Language.German, "Vorgebaute Puzzles" }, { Language.English, "Prebuilt Puzzles" } },
        ["puzzles.empty"] = new() { { Language.German, "Keine Puzzles für diese Schwierigkeit verfügbar." }, { Language.English, "No puzzles available for this difficulty." } },
        ["puzzles.play"] = new() { { Language.German, "Spielen" }, { Language.English, "Play" } },
        ["puzzles.completed"] = new() { { Language.German, "✅ Gelöst" }, { Language.English, "✅ Completed" } },
        ["puzzles.confirm.title"] = new() { { Language.German, "Puzzle starten?" }, { Language.English, "Start Puzzle?" } },
        ["puzzles.confirm.start"] = new() { { Language.German, "Starten" }, { Language.English, "Start" } },
        ["puzzles.confirm.new"] = new() { { Language.German, "Dieses Puzzle wurde noch nicht gelöst." }, { Language.English, "This puzzle has not been completed yet." } },
        ["puzzles.confirm.replay"] = new() { { Language.German, "Du hast dieses Puzzle bereits gelöst. Erneut spielen?" }, { Language.English, "You've already completed this puzzle. Play again?" } },

        // ===========================================
        // TIPS MENU
        // ===========================================
        ["tips.title"] = new() { { Language.German, "Tipps & Tricks" }, { Language.English, "Tips & Tricks" } },

        // ===========================================
        // HINTS
        // ===========================================
        ["hint.naked_single"] = new() { { Language.German, "Naked Single" }, { Language.English, "Naked Single" } },
        ["hint.naked_single.desc"] = new() { { Language.German, "Diese Zelle hat nur eine mögliche Zahl." }, { Language.English, "This cell has only one possible number." } },
        ["hint.naked_single.explanation"] = new()
        {
            { Language.German, "In dieser Zelle kann nur die {0} stehen, da alle anderen Zahlen (1-9) bereits in der gleichen Zeile, Spalte oder im gleichen 3x3-Block vorkommen." },
            { Language.English, "Only {0} can go in this cell, because all other numbers (1-9) already appear in the same row, column, or 3x3 block." }
        },
        ["hint.hidden_single.row"] = new() { { Language.German, "Hidden Single (Zeile)" }, { Language.English, "Hidden Single (Row)" } },
        ["hint.hidden_single.row.desc"] = new() { { Language.German, "Die {0} kann in dieser Zeile nur hier stehen." }, { Language.English, "{0} can only be placed here in this row." } },
        ["hint.hidden_single.row.explanation"] = new()
        {
            { Language.German, "In Zeile {0} gibt es nur eine Zelle, in der die {1} platziert werden kann. Alle anderen Zellen in der Zeile sind entweder gefüllt oder die {1} wird durch andere Zahlen in deren Spalte oder Block blockiert." },
            { Language.English, "In row {0}, there is only one cell where {1} can be placed. All other cells in the row are either filled, or {1} is blocked by other numbers in their column or block." }
        },
        ["hint.hidden_single.col"] = new() { { Language.German, "Hidden Single (Spalte)" }, { Language.English, "Hidden Single (Column)" } },
        ["hint.hidden_single.col.desc"] = new() { { Language.German, "Die {0} kann in dieser Spalte nur hier stehen." }, { Language.English, "{0} can only be placed here in this column." } },
        ["hint.hidden_single.col.explanation"] = new()
        {
            { Language.German, "In Spalte {0} gibt es nur eine Zelle, in der die {1} platziert werden kann. Alle anderen Zellen in der Spalte sind entweder gefüllt oder die {1} wird durch andere Zahlen in deren Zeile oder Block blockiert." },
            { Language.English, "In column {0}, there is only one cell where {1} can be placed. All other cells in the column are either filled, or {1} is blocked by other numbers in their row or block." }
        },
        ["hint.hidden_single.block"] = new() { { Language.German, "Hidden Single (Block)" }, { Language.English, "Hidden Single (Block)" } },
        ["hint.hidden_single.block.desc"] = new() { { Language.German, "Die {0} kann in diesem 3x3-Block nur hier stehen." }, { Language.English, "{0} can only be placed here in this 3x3 block." } },
        ["hint.hidden_single.block.explanation"] = new()
        {
            { Language.German, "Im 3x3-Block gibt es nur eine Zelle, in der die {0} platziert werden kann. Alle anderen Zellen im Block sind entweder gefüllt oder die {0} wird durch andere Zahlen in deren Zeile oder Spalte blockiert." },
            { Language.English, "In this 3x3 block, there is only one cell where {0} can be placed. All other cells in the block are either filled, or {0} is blocked by other numbers in their row or column." }
        },
        ["hint.naked_pair"] = new() { { Language.German, "Naked Pair" }, { Language.English, "Naked Pair" } },
        ["hint.naked_pair.desc"] = new() { { Language.German, "Die Zahlen {0} und {1} bilden ein Naked Pair." }, { Language.English, "The numbers {0} and {1} form a Naked Pair." } },
        ["hint.naked_pair.explanation"] = new()
        {
            { Language.German, "Die Zellen in Spalte {0} und {1} können nur {2} oder {3} enthalten. Daher können diese Zahlen aus anderen Zellen der Zeile eliminiert werden, was hier nur {4} übrig lässt." },
            { Language.English, "The cells in columns {0} and {1} can only contain {2} or {3}. Therefore, these numbers can be eliminated from other cells in the row, leaving only {4} here." }
        },
        ["hint.pointing_pair"] = new() { { Language.German, "Pointing Pair" }, { Language.English, "Pointing Pair" } },
        ["hint.pointing_pair.row.desc"] = new() { { Language.German, "Die {0} im Block zeigt auf diese Zeile." }, { Language.English, "{0} in the block points to this row." } },
        ["hint.pointing_pair.row.explanation"] = new()
        {
            { Language.German, "Im 3x3-Block kommt die {0} nur in Zeile {1} vor. Daher kann die {0} aus anderen Zellen dieser Zeile (außerhalb des Blocks) eliminiert werden." },
            { Language.English, "In the 3x3 block, {0} appears only in row {1}. Therefore, {0} can be eliminated from other cells in this row (outside the block)." }
        },
        ["hint.pointing_pair.col.desc"] = new() { { Language.German, "Die {0} im Block zeigt auf diese Spalte." }, { Language.English, "{0} in the block points to this column." } },
        ["hint.pointing_pair.col.explanation"] = new()
        {
            { Language.German, "Im 3x3-Block kommt die {0} nur in Spalte {1} vor. Daher kann die {0} aus anderen Zellen dieser Spalte (außerhalb des Blocks) eliminiert werden." },
            { Language.English, "In the 3x3 block, {0} appears only in column {1}. Therefore, {0} can be eliminated from other cells in this column (outside the block)." }
        },
        ["hint.box_line"] = new() { { Language.German, "Box/Line Reduction" }, { Language.English, "Box/Line Reduction" } },
        ["hint.box_line.desc"] = new() { { Language.German, "Die {0} in Zeile {1} ist auf diesen Block beschränkt." }, { Language.English, "{0} in row {1} is restricted to this block." } },
        ["hint.box_line.explanation"] = new()
        {
            { Language.German, "In Zeile {0} kommt die {1} nur im Block vor. Daher kann die {1} aus anderen Zellen des Blocks eliminiert werden." },
            { Language.English, "In row {0}, {1} appears only in this block. Therefore, {1} can be eliminated from other cells of the block." }
        },
        ["hint.x_wing"] = new() { { Language.German, "X-Wing" }, { Language.English, "X-Wing" } },
        ["hint.x_wing.desc"] = new() { { Language.German, "Ein X-Wing Muster für die {0}." }, { Language.English, "An X-Wing pattern for {0}." } },
        ["hint.x_wing.explanation"] = new()
        {
            { Language.German, "Die {0} bildet ein Rechteck-Muster (X-Wing) in den Zeilen {1} und {2}. In jeder dieser Zeilen muss die {0} in einer der beiden Spalten {3} oder {4} stehen. Daher kann die {0} aus anderen Zellen dieser Spalten eliminiert werden." },
            { Language.English, "{0} forms a rectangle pattern (X-Wing) in rows {1} and {2}. In each of these rows, {0} must be in one of the two columns {3} or {4}. Therefore, {0} can be eliminated from other cells in these columns." }
        },
        ["hint.hidden_single"] = new() { { Language.German, "Hidden Single" }, { Language.English, "Hidden Single" } },
        ["hint.hidden_single.desc"] = new() { { Language.German, "Diese Zahl kann nur an dieser Stelle stehen." }, { Language.English, "This number can only go in this position." } },
        ["hint.hidden_single.explanation"] = new()
        {
            { Language.German, "Die {0} kann in dieser Einheit (Zeile, Spalte oder Block) nur in dieser Zelle platziert werden, da sie überall sonst durch andere Zahlen blockiert ist." },
            { Language.English, "{0} can only be placed in this cell within this unit (row, column, or block), as it is blocked everywhere else by other numbers." }
        },
        ["hint.hidden_pair"] = new() { { Language.German, "Hidden Pair" }, { Language.English, "Hidden Pair" } },
        ["hint.hidden_pair.desc"] = new() { { Language.German, "Die Zahlen {0} und {1} bilden ein Hidden Pair." }, { Language.English, "The numbers {0} and {1} form a Hidden Pair." } },
        ["hint.hidden_pair.explanation"] = new()
        {
            { Language.German, "Die Zahlen kommen in dieser Einheit nur in genau zwei Zellen vor. Andere Kandidaten in diesen Zellen können eliminiert werden, was nur {0} übrig lässt." },
            { Language.English, "These numbers appear in exactly two cells within this unit. Other candidates in those cells can be eliminated, leaving only {0}." }
        },
        ["hint.hidden_triple"] = new() { { Language.German, "Hidden Triple" }, { Language.English, "Hidden Triple" } },
        ["hint.hidden_triple.desc"] = new() { { Language.German, "Die Zahlen {0}, {1} und {2} bilden ein Hidden Triple." }, { Language.English, "The numbers {0}, {1}, and {2} form a Hidden Triple." } },
        ["hint.hidden_triple.explanation"] = new()
        {
            { Language.German, "Diese drei Zahlen kommen in dieser Einheit nur in genau drei Zellen vor. Andere Kandidaten in diesen Zellen können eliminiert werden, was nur {0} übrig lässt." },
            { Language.English, "These three numbers appear in exactly three cells within this unit. Other candidates in those cells can be eliminated, leaving only {0}." }
        },
        ["hint.xy_wing"] = new() { { Language.German, "XY-Wing" }, { Language.English, "XY-Wing" } },
        ["hint.xy_wing.desc"] = new() { { Language.German, "Ein XY-Wing Muster für die {0}." }, { Language.English, "An XY-Wing pattern for {0}." } },
        ["hint.xy_wing.explanation"] = new()
        {
            { Language.German, "Drei Zellen mit je zwei Kandidaten bilden ein Y-Muster. Die gemeinsame Zahl {0} kann aus Zellen eliminiert werden, die alle drei sehen." },
            { Language.English, "Three bi-value cells form a Y pattern. The shared candidate {0} can be eliminated from cells that see all three." }
        },
        ["hint.xyz_wing"] = new() { { Language.German, "XYZ-Wing" }, { Language.English, "XYZ-Wing" } },
        ["hint.xyz_wing.desc"] = new() { { Language.German, "Ein XYZ-Wing Muster für die {0}." }, { Language.English, "An XYZ-Wing pattern for {0}." } },
        ["hint.xyz_wing.explanation"] = new()
        {
            { Language.German, "Wie XY-Wing, aber der Pivot hat drei Kandidaten. Die {0} kann aus Zellen eliminiert werden, die alle drei Zellen sehen." },
            { Language.English, "Like XY-Wing, but the pivot has three candidates. {0} can be eliminated from cells that see all three cells." }
        },
        ["hint.logical_analysis"] = new() { { Language.German, "Logische Analyse" }, { Language.English, "Logical Analysis" } },
        ["hint.logical_analysis.desc"] = new() { { Language.German, "Diese Zelle kann durch Analyse gelöst werden." }, { Language.English, "This cell can be solved by analysis." } },
        ["hint.logical_analysis.explanation"] = new()
        {
            { Language.German, "Durch Überprüfen aller möglichen Zahlen und Eliminieren der unmöglichen Kandidaten ergibt sich die {0} als einzig mögliche Lösung für diese Zelle." },
            { Language.English, "By checking all possible numbers and eliminating impossible candidates, {0} remains as the only possible solution for this cell." }
        },
        ["hint.apply"] = new() { { Language.German, "Anwenden" }, { Language.English, "Apply" } },
        ["hint.close"] = new() { { Language.German, "Schließen" }, { Language.English, "Close" } },
        ["hint.no_hint"] = new() { { Language.German, "Kein Hinweis verfügbar" }, { Language.English, "No hint available" } },

        // ===========================================
        // TECHNIQUES
        // ===========================================
        ["technique.naked_single"] = new() { { Language.German, "Naked Single" }, { Language.English, "Naked Single" } },
        ["technique.naked_single.short"] = new() { { Language.German, "Nur eine Möglichkeit in Zelle" }, { Language.English, "Only one possibility in cell" } },
        ["technique.naked_single.desc"] = new()
        {
            { Language.German, "Eine Zelle hat nur eine mögliche Zahl, da alle anderen durch Zeile, Spalte oder Block ausgeschlossen sind." },
            { Language.English, "A cell has only one possible number because all others are excluded by its row, column, or block." }
        },

        ["technique.hidden_single_row"] = new() { { Language.German, "Hidden Single (Zeile)" }, { Language.English, "Hidden Single (Row)" } },
        ["technique.hidden_single_row.desc"] = new()
        {
            { Language.German, "Eine Zahl kann in einer Zeile nur an einer einzigen Position platziert werden." },
            { Language.English, "A number can be placed in only one position in a row." }
        },
        ["technique.hidden_single_col"] = new() { { Language.German, "Hidden Single (Spalte)" }, { Language.English, "Hidden Single (Column)" } },
        ["technique.hidden_single_col.desc"] = new()
        {
            { Language.German, "Eine Zahl kann in einer Spalte nur an einer einzigen Position platziert werden." },
            { Language.English, "A number can be placed in only one position in a column." }
        },
        ["technique.hidden_single_block"] = new() { { Language.German, "Hidden Single (Block)" }, { Language.English, "Hidden Single (Block)" } },
        ["technique.hidden_single_block.desc"] = new()
        {
            { Language.German, "Eine Zahl kann in einem 3x3-Block nur an einer einzigen Position platziert werden." },
            { Language.English, "A number can be placed in only one position in a 3x3 block." }
        },

        ["technique.hidden_single"] = new() { { Language.German, "Hidden Single" }, { Language.English, "Hidden Single" } },
        ["technique.hidden_single.short"] = new() { { Language.German, "Zahl nur an einer Stelle möglich" }, { Language.English, "Number only possible in one place" } },
        ["technique.naked_pair"] = new() { { Language.German, "Naked Pair" }, { Language.English, "Naked Pair" } },
        ["technique.naked_pair.short"] = new() { { Language.German, "Zwei Zellen mit gleichen zwei Kandidaten" }, { Language.English, "Two cells with same two candidates" } },
        ["technique.naked_pair.desc"] = new()
        {
            { Language.German, "Zwei Zellen in einer Einheit haben genau dieselben zwei Kandidaten. Diese Zahlen können aus anderen Zellen der Einheit eliminiert werden." },
            { Language.English, "Two cells in a unit have exactly the same two candidates. Those candidates can be eliminated from other cells in the unit." }
        },
        ["technique.naked_triple"] = new() { { Language.German, "Naked Triple" }, { Language.English, "Naked Triple" } },
        ["technique.naked_triple.desc"] = new()
        {
            { Language.German, "Drei Zellen in einer Einheit teilen sich maximal drei Kandidaten. Diese können aus anderen Zellen eliminiert werden." },
            { Language.English, "Three cells in a unit share at most three candidates. These candidates can be eliminated from other cells in the unit." }
        },
        ["technique.naked_quad"] = new() { { Language.German, "Naked Quad" }, { Language.English, "Naked Quad" } },
        ["technique.naked_quad.desc"] = new()
        {
            { Language.German, "Vier Zellen in einer Einheit teilen sich maximal vier Kandidaten. Diese können aus anderen Zellen eliminiert werden." },
            { Language.English, "Four cells in a unit share at most four candidates. These candidates can be eliminated from other cells in the unit." }
        },
        ["technique.hidden_pair"] = new() { { Language.German, "Hidden Pair" }, { Language.English, "Hidden Pair" } },
        ["technique.hidden_pair.desc"] = new()
        {
            { Language.German, "Zwei Zahlen kommen in einer Einheit nur in genau zwei Zellen vor. Andere Kandidaten in diesen Zellen können eliminiert werden." },
            { Language.English, "Two numbers appear in a unit in exactly two cells. Other candidates in those cells can be eliminated." }
        },
        ["technique.hidden_triple"] = new() { { Language.German, "Hidden Triple" }, { Language.English, "Hidden Triple" } },
        ["technique.hidden_triple.desc"] = new()
        {
            { Language.German, "Drei Zahlen kommen in einer Einheit nur in genau drei Zellen vor. Andere Kandidaten in diesen Zellen können eliminiert werden." },
            { Language.English, "Three numbers appear in a unit in exactly three cells. Other candidates in those cells can be eliminated." }
        },
        ["technique.pointing_pair"] = new() { { Language.German, "Pointing Pair" }, { Language.English, "Pointing Pair" } },
        ["technique.pointing_pair.short"] = new() { { Language.German, "Kandidat im Block zeigt auf Zeile/Spalte" }, { Language.English, "Candidate in block points to row/column" } },
        ["technique.pointing_pair.desc"] = new()
        {
            { Language.German, "Wenn eine Zahl in einem Block nur in einer Zeile/Spalte vorkommt, kann sie aus dem Rest dieser Zeile/Spalte eliminiert werden." },
            { Language.English, "If a number in a block appears only in one row/column, it can be eliminated from the rest of that row/column." }
        },

        // Note: TechniqueInfo uses the ID "BoxLineReduction" => key "technique.box_line_reduction".
        ["technique.box_line"] = new() { { Language.German, "Box/Line Reduction" }, { Language.English, "Box/Line Reduction" } },
        ["technique.box_line_reduction"] = new() { { Language.German, "Box/Line Reduction" }, { Language.English, "Box/Line Reduction" } },
        ["technique.box_line_reduction.desc"] = new()
        {
            { Language.German, "Wenn eine Zahl in einer Zeile/Spalte nur in einem Block vorkommt, kann sie aus dem Rest des Blocks eliminiert werden." },
            { Language.English, "If a number in a row/column appears only within one block, it can be eliminated from the rest of that block." }
        },
        ["technique.x_wing"] = new() { { Language.German, "X-Wing" }, { Language.English, "X-Wing" } },
        ["technique.x_wing.short"] = new() { { Language.German, "Rechteck-Muster für Eliminierungen" }, { Language.English, "Rectangle pattern for eliminations" } },
        ["technique.x_wing.desc"] = new()
        {
            { Language.German, "Wenn eine Zahl in zwei Zeilen nur in den gleichen zwei Spalten vorkommt, kann sie aus diesen Spalten in anderen Zeilen eliminiert werden." },
            { Language.English, "If a number appears in two rows only in the same two columns, it can be eliminated from those columns in other rows." }
        },
        ["technique.swordfish"] = new() { { Language.German, "Swordfish" }, { Language.English, "Swordfish" } },
        ["technique.swordfish.desc"] = new()
        {
            { Language.German, "Eine Erweiterung von X-Wing mit drei Zeilen und drei Spalten." },
            { Language.English, "An extension of X-Wing using three rows and three columns." }
        },
        ["technique.jellyfish"] = new() { { Language.German, "Jellyfish" }, { Language.English, "Jellyfish" } },
        ["technique.jellyfish.desc"] = new()
        {
            { Language.German, "Eine Erweiterung von Swordfish mit vier Zeilen und vier Spalten." },
            { Language.English, "An extension of Swordfish using four rows and four columns." }
        },
        ["technique.xy_wing"] = new() { { Language.German, "XY-Wing" }, { Language.English, "XY-Wing" } },
        ["technique.xy_wing.desc"] = new()
        {
            { Language.German, "Drei Zellen mit je zwei Kandidaten bilden ein Y-Muster. Die gemeinsame Zahl kann aus Zellen eliminiert werden, die alle drei sehen." },
            { Language.English, "Three bi-value cells form a Y pattern. The shared candidate can be eliminated from cells that see all three." }
        },
        ["technique.xyz_wing"] = new() { { Language.German, "XYZ-Wing" }, { Language.English, "XYZ-Wing" } },
        ["technique.xyz_wing.desc"] = new()
        {
            { Language.German, "Wie XY-Wing, aber der Pivot hat drei Kandidaten. Eliminierungen nur in Zellen, die alle drei sehen." },
            { Language.English, "Like XY-Wing, but the pivot has three candidates. Eliminations only in cells that see all three." }
        },
        ["technique.w_wing"] = new() { { Language.German, "W-Wing" }, { Language.English, "W-Wing" } },
        ["technique.w_wing.desc"] = new()
        {
            { Language.German, "Zwei Zellen mit identischen zwei Kandidaten, verbunden durch einen Strong Link auf einem Kandidaten." },
            { Language.English, "Two identical bi-value cells connected by a strong link on a candidate." }
        },
        ["technique.skyscraper"] = new() { { Language.German, "Skyscraper" }, { Language.English, "Skyscraper" } },
        ["technique.skyscraper.desc"] = new()
        {
            { Language.German, "Zwei Spalten mit je genau zwei Kandidaten einer Zahl, die eine Zeile teilen." },
            { Language.English, "Two columns with exactly two candidates of a digit that share a row." }
        },
        ["technique.two_string_kite"] = new() { { Language.German, "Two-String Kite" }, { Language.English, "Two-String Kite" } },
        ["technique.two_string_kite.desc"] = new()
        {
            { Language.German, "Ein Kandidat bildet ein Konjugat-Paar in einer Zeile UND einer Spalte, die sich in einem Block treffen." },
            { Language.English, "A candidate forms a conjugate pair in a row and a column that meet in a block." }
        },
        ["technique.empty_rectangle"] = new() { { Language.German, "Empty Rectangle" }, { Language.English, "Empty Rectangle" } },
        ["technique.empty_rectangle.desc"] = new()
        {
            { Language.German, "Ein Kandidat bildet eine L-Form in einem Block und interagiert mit einem Konjugat-Paar." },
            { Language.English, "A candidate forms an L-shape in a block and interacts with a conjugate pair." }
        },
        ["technique.simple_coloring"] = new() { { Language.German, "Simple Coloring" }, { Language.English, "Simple Coloring" } },
        ["technique.simple_coloring.desc"] = new()
        {
            { Language.German, "Konjugat-Paare werden abwechselnd gefärbt um Widersprüche oder Eliminierungen zu finden." },
            { Language.English, "Conjugate pairs are alternately colored to find contradictions or eliminations." }
        },
        ["technique.unique_rectangle"] = new() { { Language.German, "Unique Rectangle" }, { Language.English, "Unique Rectangle" } },
        ["technique.unique_rectangle.short"] = new() { { Language.German, "Vermeidet 'Deadly Pattern' Rechtecke" }, { Language.English, "Avoids 'Deadly Pattern' rectangles" } },
        ["technique.unique_rectangle.desc"] = new()
        {
            { Language.German, "Erkennt Rechteck-Muster die zu mehreren Lösungen führen würden und eliminiert die entsprechenden Kandidaten." },
            { Language.English, "Detects rectangle patterns that would lead to multiple solutions and eliminates the relevant candidates." }
        },
        ["technique.finned_x_wing"] = new() { { Language.German, "Finned X-Wing" }, { Language.English, "Finned X-Wing" } },
        ["technique.finned_x_wing.desc"] = new()
        {
            { Language.German, "Ein X-Wing Muster mit zusätzlichen Kandidaten (Flossen) die begrenzte Eliminierungen ermöglichen." },
            { Language.English, "An X-Wing pattern with extra candidates (fins) that enable limited eliminations." }
        },
        ["technique.finned_swordfish"] = new() { { Language.German, "Finned Swordfish" }, { Language.English, "Finned Swordfish" } },
        ["technique.finned_swordfish.desc"] = new()
        {
            { Language.German, "Ein Swordfish Muster mit zusätzlichen Kandidaten (Flossen) die begrenzte Eliminierungen ermöglichen." },
            { Language.English, "A Swordfish pattern with extra candidates (fins) that enable limited eliminations." }
        },
        ["technique.remote_pair"] = new() { { Language.German, "Remote Pair" }, { Language.English, "Remote Pair" } },
        ["technique.remote_pair.desc"] = new()
        {
            { Language.German, "Eine Kette von Zellen mit identischen Kandidaten-Paaren, die Eliminierungen in Zellen ermöglicht die beide Enden sehen." },
            { Language.English, "A chain of cells with identical candidate pairs that enables eliminations in cells that see both ends." }
        },
        // Note: TechniqueInfo uses the ID "BUGPlus1" => key "technique.bug_plus1".
        ["technique.bug_plus_1"] = new() { { Language.German, "BUG+1" }, { Language.English, "BUG+1" } },
        ["technique.bug_plus1"] = new() { { Language.German, "BUG+1" }, { Language.English, "BUG+1" } },
        ["technique.bug_plus1.desc"] = new()
        {
            { Language.German, "Wenn alle Zellen nur zwei Kandidaten hätten außer einer, muss der Extra-Kandidat in dieser Zelle die Lösung sein." },
            { Language.English, "If all cells are bi-value except one, the extra candidate in that cell must be the solution." }
        },

        // Note: TechniqueInfo uses the ID "ALSXZRule" => key "technique.alsxz_rule".
        ["technique.als_xz"] = new() { { Language.German, "ALS-XZ Rule" }, { Language.English, "ALS-XZ Rule" } },
        ["technique.alsxz_rule"] = new() { { Language.German, "ALS-XZ Rule" }, { Language.English, "ALS-XZ Rule" } },
        ["technique.alsxz_rule.desc"] = new()
        {
            { Language.German, "Zwei Almost Locked Sets die durch einen gemeinsamen Kandidaten verbunden sind ermöglichen Eliminierungen." },
            { Language.English, "Two almost locked sets connected by a shared candidate allow eliminations." }
        },
        ["technique.forcing_chain"] = new() { { Language.German, "Forcing Chain" }, { Language.English, "Forcing Chain" } },
        ["technique.forcing_chain.desc"] = new()
        {
            { Language.German, "Wenn beide möglichen Werte einer Zelle zum gleichen Ergebnis führen, muss dieses Ergebnis wahr sein." },
            { Language.English, "If both possible values of a cell lead to the same conclusion, that conclusion must be true." }
        },

        // ===========================================
        // COMMON
        // ===========================================
        ["common.unknown"] = new() { { Language.German, "Unbekannt" }, { Language.English, "Unknown" } },
        ["minigrid.invalid_data"] = new() { { Language.German, "(Mini-Grid: ungültige Daten)" }, { Language.English, "(Mini-grid: invalid data)" } },
        ["common.row"] = new() { { Language.German, "Zeile" }, { Language.English, "Row" } },
        ["common.column"] = new() { { Language.German, "Spalte" }, { Language.English, "Column" } },
        ["common.block"] = new() { { Language.German, "Block" }, { Language.English, "Block" } },
        ["common.cell"] = new() { { Language.German, "Zelle" }, { Language.English, "Cell" } },
        ["common.difficulty"] = new() { { Language.German, "Schwierigkeit" }, { Language.English, "Difficulty" } },
        ["common.back"] = new() { { Language.German, "← Zurück" }, { Language.English, "← Back" } },
        ["common.next"] = new() { { Language.German, "Weiter →" }, { Language.English, "Next →" } },
        ["common.close"] = new() { { Language.German, "Schließen" }, { Language.English, "Close" } },
        ["common.waiting"] = new() { { Language.German, "Warten..." }, { Language.English, "Waiting..." } },
        ["common.back_to_menu"] = new() { { Language.German, "Zurück zum Menü" }, { Language.English, "Back to Menu" } },

        // ===========================================
        // GAME SCENE - TOOLTIPS & BUTTONS
        // ===========================================
        ["game.number.tooltip"] = new() { { Language.German, "Zahl {0} setzen (Taste {0})" }, { Language.English, "Place number {0} (key {0})" } },
        ["game.eraser.tooltip"] = new() { { Language.German, "Zahl löschen (Entf/Backspace)" }, { Language.English, "Erase number (Del/Backspace)" } },
        ["game.hint.tooltip"] = new() { { Language.German, "Tipp anzeigen\nZeigt einen Hinweis für den nächsten Zug" }, { Language.English, "Show hint\nDisplays a hint for the next move" } },
        ["game.autocandidates.tooltip"] = new() { { Language.German, "Auto-Kandidaten anzeigen/verbergen\nZeigt alle möglichen Zahlen (grau)" }, { Language.English, "Show/hide auto-candidates\nDisplays all possible numbers (grey)" } },
        ["game.notes.tooltip"] = new() { { Language.German, "Notizen-Modus (N)\nEigene Notizen setzen (blau)\nCtrl+Klick für Mehrfachauswahl" }, { Language.English, "Notes mode (N)\nSet your own notes (blue)\nCtrl+Click for multi-select" } },
        ["game.hint.prev"] = new() { { Language.German, "← Zurück" }, { Language.English, "← Back" } },
        ["game.hint.page"] = new() { { Language.German, "Seite {0} / {1}" }, { Language.English, "Page {0} / {1}" } },
        ["game.hint.title.hint"] = new() { { Language.German, "💡 Tipp: {0}" }, { Language.English, "💡 Hint: {0}" } },
        ["game.hint.title.related"] = new() { { Language.German, "🔍 Relevante Zellen" }, { Language.English, "🔍 Related Cells" } },
        ["game.hint.title.solution"] = new() { { Language.German, "✓ Lösung" }, { Language.English, "✓ Solution" } },
        ["game.hint.title.elimination"] = new() { { Language.German, "✂ Eliminierungen" }, { Language.English, "✂ Eliminations" } },
        ["game.hint.title.explanation"] = new() { { Language.German, "📖 Erklärung" }, { Language.English, "📖 Explanation" } },
        ["game.hint.title.default"] = new() { { Language.German, "Tipp" }, { Language.English, "Hint" } },
        ["game.hint.page0"] = new() { { Language.German, "[b]Technik:[/b] {0}\n\n{1}\n\nSchaue dir die [color=#64b5f6]blau markierte Zelle[/color] im Spielfeld an.\n[i](Zelle {2})[/i]" }, { Language.English, "[b]Technique:[/b] {0}\n\n{1}\n\nLook at the [color=#64b5f6]blue highlighted cell[/color] on the board.\n[i](Cell {2})[/i]" } },
        ["game.hint.page1"] = new() { { Language.German, "Die markierten Zellen sind relevant für diesen Hinweis.\n\nDiese Zellen befinden sich in der gleichen Zeile, Spalte oder im gleichen 3x3-Block wie die Zielzelle und beeinflussen, welche Zahlen dort möglich sind.\n\n[i]Anzahl relevanter Zellen: {0}[/i]" }, { Language.English, "The highlighted cells are relevant for this hint.\n\nThese cells are in the same row, column, or 3x3 block as the target cell and influence which numbers are possible there.\n\n[i]Number of relevant cells: {0}[/i]" } },
        ["game.hint.page2"] = new() { { Language.German, "Die Lösung für diese Zelle ist:\n\n[center][font_size=48][color=#4caf50][b]{0}[/b][/color][/font_size][/center]\n\n[i]Klicke auf \"Weiter\" für eine detaillierte Erklärung.[/i]" }, { Language.English, "The solution for this cell is:\n\n[center][font_size=48][color=#4caf50][b]{0}[/b][/color][/font_size][/center]\n\n[i]Click \"Next\" for a detailed explanation.[/i]" } },
        ["game.hint.page2.elimination"] = new() { { Language.German, "In Zelle {0} können folgende Kandidaten eliminiert werden:\n\n[center][font_size=36][color=#ffcc80][b]{1}[/b][/color][/font_size][/center]\n\n[i]Klicke auf \"Weiter\" für eine detaillierte Erklärung.[/i]" }, { Language.English, "In cell {0}, the following candidate(s) can be eliminated:\n\n[center][font_size=36][color=#ffcc80][b]{1}[/b][/color][/font_size][/center]\n\n[i]Click \"Next\" for a detailed explanation.[/i]" } },
        ["game.hint.page3"] = new() { { Language.German, "[b]Erklärung:[/b]\n\n{0}" }, { Language.English, "[b]Explanation:[/b]\n\n{0}" } },
        ["game.hint.9x9_only"] = new() { { Language.German, "Hinweise nur im 9x9 verfügbar" }, { Language.English, "Hints only available in 9x9" } },
        ["game.hint.no_hint.title"] = new() { { Language.German, "💡 Kein Tipp" }, { Language.English, "💡 No Hint" } },
        ["game.hint.no_hint.message"] = new() { { Language.German, "Aktuell ist kein sinnvoller Hinweis verfügbar." }, { Language.English, "No helpful hint is currently available." } },

        ["game.hint_limit.title"] = new() { { Language.German, "💡 Hinweis-Limit" }, { Language.English, "💡 Hint Limit" } },
        ["game.hint_limit.message"] = new() { { Language.German, "Du hast das Hinweis-Limit erreicht ({0})." }, { Language.English, "You have reached the hint limit ({0})." } },
        ["game.back.tooltip"] = new() { { Language.German, "Zurück zum Hauptmenü (ESC)\nSpiel wird automatisch gespeichert" }, { Language.English, "Return to main menu (ESC)\nGame is saved automatically" } },
        ["game.back.tooltip.nosave"] = new() { { Language.German, "Zurück zum Hauptmenü" }, { Language.English, "Return to main menu" } },
        ["game.win.message"] = new() { { Language.German, "Du hast das Sudoku gelöst!\n\n⏱️ Zeit: {0}\n❌ Fehler: {1}" }, { Language.English, "You solved the Sudoku!\n\n⏱️ Time: {0}\n❌ Mistakes: {1}" } },
        ["game.learn.wrong_solution"] = new() { { Language.German, "Zelle {0}: {1} ist regelkonform, aber nicht die Lösung dieses Rätsels." }, { Language.English, "Cell {0}: {1} follows the rules, but is not the solution for this puzzle." } },
        ["game.learn.hint"] = new() { { Language.German, "\n\nTipp: In {0} gehört {1}." }, { Language.English, "\n\nHint: {0} should contain {1}." } },
        ["game.learn.sudoku_rules"] = new() { { Language.German, "den Sudoku-Regeln" }, { Language.English, "the Sudoku rules" } },
        ["game.autofill.tooltip"] = new() { { Language.German, "Klick: Auto-Notizen für {0} einfügen\nRechtsklick/Shift+Klick: Modus wechseln" }, { Language.English, "Click: Auto-fill notes for {0}\nRight-click/Shift+click: Change mode" } },
        ["game.autofill.cell"] = new() { { Language.German, "Zelle" }, { Language.English, "cell" } },
        ["game.autofill.row"] = new() { { Language.German, "Zeile" }, { Language.English, "row" } },
        ["game.autofill.col"] = new() { { Language.German, "Spalte" }, { Language.English, "column" } },
        ["game.autofill.block"] = new() { { Language.German, "Block" }, { Language.English, "block" } },

        ["game.timer.remaining"] = new() { { Language.German, "{0} (Rest {1})" }, { Language.English, "{0} (Remaining {1})" } },

        ["game.challenge.perfect.title"] = new() { { Language.German, "🎯 Perfect Run" }, { Language.English, "🎯 Perfect Run" } },
        ["game.challenge.perfect.message"] = new() { { Language.German, "Ein Fehler beendet diesen Modus.\nDu hast verloren." }, { Language.English, "One mistake ends this mode.\nYou lost." } },
        ["game.challenge.time.title"] = new() { { Language.German, "⏱️ Time Attack" }, { Language.English, "⏱️ Time Attack" } },
        ["game.challenge.time.message"] = new() { { Language.German, "Zeit abgelaufen.\nDu hast verloren." }, { Language.English, "Time's up.\nYou lost." } },

        ["game.challenge.tag.hints"] = new() { { Language.German, "Hinweise≤{0}" }, { Language.English, "Hints≤{0}" } },
        ["game.challenge.tag.time"] = new() { { Language.German, "Zeit≤{0}m" }, { Language.English, "Time≤{0}m" } },

        // ===========================================
        // MAIN MENU
        // ===========================================
        ["menu.daily.tooltip.done"] = new() { { Language.German, "Daily für heute ist bereits erledigt.\nDu kannst es trotzdem erneut spielen (ohne extra Streak)." }, { Language.English, "Today's daily is already completed.\nYou can still play it again (without extra streak)." } },
        ["menu.daily.tooltip.open"] = new() { { Language.German, "Tägliches Sudoku (deterministisch)." }, { Language.English, "Daily Sudoku (deterministic)." } },

        // ===========================================
        // SCENARIOS MENU
        // ===========================================
        ["scenarios.techniques.title"] = new() { { Language.German, "🎯 Technik-Szenarien" }, { Language.English, "🎯 Technique Scenarios" } },
        ["scenarios.techniques.desc"] = new() { { Language.German, "Übe spezifische Lösungstechniken mit passenden Puzzles" }, { Language.English, "Practice specific solving techniques with matching puzzles" } },
        ["scenarios.tutorials.title"] = new() { { Language.German, "📚 Tutorials" }, { Language.English, "📚 Tutorials" } },
        ["scenarios.tutorials.desc"] = new() { { Language.German, "Interaktive Anleitungen mit animierten Hinweisen" }, { Language.English, "Interactive guides with animated hints" } },
        ["scenarios.description"] = new() { { Language.German, "Starte ein interaktives Tutorial oder übe spezifische Techniken." }, { Language.English, "Start an interactive tutorial or practice specific techniques." } },
        ["scenarios.category.easy"] = new() { { Language.German, "🟢 Leicht" }, { Language.English, "🟢 Easy" } },
        ["scenarios.category.medium"] = new() { { Language.German, "🟠 Mittel" }, { Language.English, "🟠 Medium" } },
        ["scenarios.category.hard"] = new() { { Language.German, "🔴 Schwer" }, { Language.English, "🔴 Hard" } },
        ["scenarios.category.insane"] = new() { { Language.German, "💀 Insane" }, { Language.English, "💀 Insane" } },
        ["scenarios.technique.tooltip"] = new() { { Language.German, "{0}\n\nKlicke um ein Puzzle zu starten, das diese Technik erfordert." }, { Language.English, "{0}\n\nClick to start a puzzle that requires this technique." } },
        ["scenarios.minutes"] = new() { { Language.German, "⏱️ Ca. {0} Minuten" }, { Language.English, "⏱️ About {0} minutes" } },

        // ===========================================
        // TUTORIAL OVERLAY
        // ===========================================
        ["tutorial.back"] = new() { { Language.German, "← Zurück" }, { Language.English, "← Back" } },
        ["tutorial.next"] = new() { { Language.German, "Weiter →" }, { Language.English, "Next →" } },
        ["tutorial.waiting"] = new() { { Language.German, "Warten..." }, { Language.English, "Waiting..." } },
        ["tutorial.getting_started"] = new() { { Language.German, "Erste Schritte" }, { Language.English, "Getting Started" } },
        ["tutorial.getting_started.desc"] = new() { { Language.German, "Lerne die Benutzeroberfläche, Steuerung und Notizen kennen." }, { Language.English, "Learn the user interface, controls, and notes." } },
        ["tutorial.basic_techniques"] = new() { { Language.German, "Grundtechniken" }, { Language.English, "Basic Techniques" } },
        ["tutorial.basic_techniques.desc"] = new() { { Language.German, "Naked Single, Hidden Single und mehr." }, { Language.English, "Naked Single, Hidden Single, and more." } },
        ["tutorial.advanced_features"] = new() { { Language.German, "Erweiterte Funktionen" }, { Language.English, "Advanced Features" } },
        ["tutorial.advanced_features.desc"] = new() { { Language.German, "Auto-Notes, Mehrfachauswahl, R/C/B und Shortcuts." }, { Language.English, "Auto-notes, multi-select, R/C/B and shortcuts." } },
        ["tutorial.advanced_techniques"] = new() { { Language.German, "Fortgeschrittene Techniken" }, { Language.English, "Advanced Techniques" } },
        ["tutorial.advanced_techniques.desc"] = new() { { Language.German, "Pairs, Pointing, Box/Line, X-Wing und mehr." }, { Language.English, "Pairs, Pointing, Box/Line, X-Wing and more." } },
        ["tutorial.challenge_modes"] = new() { { Language.German, "Challenge-Modi" }, { Language.English, "Challenge Modes" } },
        ["tutorial.challenge_modes.desc"] = new() { { Language.German, "Deadly Mode, Statistiken und persönliche Bestzeiten." }, { Language.English, "Deadly Mode, statistics, and personal best times." } },

        // ===========================================
        // TIPS MENU - TITLES
        // ===========================================
        ["tips.nav.prev"] = new() { { Language.German, "← Zurück" }, { Language.English, "← Back" } },
        ["tips.nav.next"] = new() { { Language.German, "Weiter →" }, { Language.English, "Next →" } },
        ["tips.nav.page"] = new() { { Language.German, "{0} / {1}" }, { Language.English, "{0} / {1}" } },
        ["tips.nav.prev.tooltip"] = new() { { Language.German, "Vorheriger Tipp (oder Pfeiltaste Links)" }, { Language.English, "Previous tip (or Left arrow key)" } },
        ["tips.nav.next.tooltip"] = new() { { Language.German, "Nächster Tipp (oder Pfeiltaste Rechts)" }, { Language.English, "Next tip (or Right arrow key)" } },
    };

    public override void _Ready()
    {
        _instance = this;

        var saveService = GetNodeOrNull<SaveService>("/root/SaveService");
        saveService?.EnsureLoaded();

        if (saveService != null)
        {
            _currentLanguage = (Language)saveService.Settings.LanguageIndex;
        }

        GD.Print($"[Localization] Initialized with language: {_currentLanguage}");
    }

    /// <summary>
    /// Get a translated string by key
    /// </summary>
    public string Get(string key)
    {
        if (Translations.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(_currentLanguage, out var text))
            {
                return text;
            }
            // Fallback to German
            if (translations.TryGetValue(Language.German, out var fallback))
            {
                return fallback;
            }
        }
        GD.PrintErr($"[Localization] Missing translation for key: {key}");
        return $"[{key}]";
    }

    /// <summary>
    /// Get a translated string with format arguments
    /// </summary>
    public string Get(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // Handle acronym boundaries: "XYWing" => "XY_Wing"
        string s = Regex.Replace(input, "([A-Z]+)([A-Z][a-z])", "$1_$2");
        // Handle normal boundaries: "BoxLine" => "Box_Line"
        s = Regex.Replace(s, "([a-z0-9])([A-Z])", "$1_$2");
        return s.ToLowerInvariant();
    }

    public string GetTechniqueName(string techniqueId)
    {
        if (string.IsNullOrWhiteSpace(techniqueId)) return Get("common.unknown");
        string key = $"technique.{ToSnakeCase(techniqueId)}";
        var translated = Get(key);
        return translated != $"[{key}]" ? translated : techniqueId;
    }

    public string GetTechniqueDescription(string techniqueId)
    {
        if (string.IsNullOrWhiteSpace(techniqueId)) return Get("common.unknown");
        string key = $"technique.{ToSnakeCase(techniqueId)}.desc";
        var translated = Get(key);
        return translated != $"[{key}]" ? translated : Get("common.unknown");
    }

    public string GetTechniqueShort(string techniqueId)
    {
        if (string.IsNullOrWhiteSpace(techniqueId)) return Get("common.unknown");
        string key = $"technique.{ToSnakeCase(techniqueId)}.short";
        var translated = Get(key);
        return translated != $"[{key}]" ? translated : GetTechniqueName(techniqueId);
    }

    public string GetTutorialName(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId)) return Get("common.unknown");
        string key = $"tutorial.{tutorialId}";
        var translated = Get(key);
        return translated != $"[{key}]" ? translated : tutorialId;
    }

    public string GetTutorialDescription(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId)) return Get("common.unknown");
        string key = $"tutorial.{tutorialId}.desc";
        var translated = Get(key);
        return translated != $"[{key}]" ? translated : Get("common.unknown");
    }

    /// <summary>
    /// Set the current language
    /// </summary>
    public void SetLanguage(Language language)
    {
        if (_currentLanguage == language) return;

        _currentLanguage = language;

        // Save to settings
        var saveService = GetNodeOrNull<SaveService>("/root/SaveService");
        if (saveService != null)
        {
            saveService.Settings.LanguageIndex = (int)language;
            saveService.SaveSettings();
        }

        GD.Print($"[Localization] Language changed to: {language} (Settings.LanguageIndex={(int)language})");
        EmitSignal(SignalName.LanguageChanged, (int)language);
    }

    /// <summary>
    /// Set language by index
    /// </summary>
    public void SetLanguage(int index)
    {
        SetLanguage((Language)index);
    }

    /// <summary>
    /// Get difficulty name in current language
    /// </summary>
    public string GetDifficultyName(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Kids => Get("game.difficulty.kids"),
            Difficulty.Easy => Get("game.difficulty.easy"),
            Difficulty.Medium => Get("game.difficulty.medium"),
            Difficulty.Hard => Get("game.difficulty.hard"),
            Difficulty.Insane => Get("game.difficulty.insane"),
            _ => Get("common.unknown")
        };
    }

    /// <summary>
    /// Get difficulty display text with emoji
    /// </summary>
    public string GetDifficultyDisplay(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Kids => Get("difficulty.kids"),
            Difficulty.Easy => Get("difficulty.easy"),
            Difficulty.Medium => Get("difficulty.medium"),
            Difficulty.Hard => Get("difficulty.hard"),
            Difficulty.Insane => Get("difficulty.insane"),
            _ => Get("common.unknown")
        };
    }

    /// <summary>
    /// Get available language names
    /// </summary>
    public static string[] GetLanguageNames()
    {
        return new[] { "Deutsch", "English" };
    }
}
