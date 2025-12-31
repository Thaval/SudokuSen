# SudokuSen - Godot 4.5 C# Sudoku Game

Ein vollständiges klassisches 9x9 Sudoku-Spiel, entwickelt mit Godot 4.5 und C# 10.

## 📁 Projektstruktur

```text
SudokuSen/
├── project.godot              # Godot-Projektkonfiguration
├── SudokuSen.csproj           # C# Projektdatei
├── SudokuSen.sln              # Visual Studio Solution
├── icon.svg                  # App-Icon
│
├── Scenes/                   # Godot-Szenen (.tscn)
│   ├── Main.tscn             # Haupt-Szene mit Scene-Switching
│   ├── MainMenu.tscn         # Hauptmenü
│   ├── DifficultyMenu.tscn   # Schwierigkeitsauswahl
│   ├── GameScene.tscn        # Spiel-Szene
│   ├── SettingsMenu.tscn     # Einstellungen
│   ├── HistoryMenu.tscn      # Spielverlauf
│   ├── StatsMenu.tscn        # Statistiken
│   └── TipsMenu.tscn         # Tipps & Tricks
│
├── Scripts/                  # C# Scripts
│   ├── Models/               # Datenmodelle
│   │   ├── SudokuCell.cs
│   │   ├── SudokuGameState.cs
│   │   ├── HistoryEntry.cs
│   │   └── SettingsData.cs
│   │
│   ├── Logic/                # Spiellogik
│   │   ├── SudokuGenerator.cs
│   │   └── SudokuSolver.cs
│   │
│   ├── Services/             # Autoload-Services
│   │   ├── AppState.cs       # Navigation & Spielzustand
│   │   ├── SaveService.cs    # Persistenz
│   │   ├── ThemeService.cs   # UI-Themes
│   │   └── IconFactory.cs    # Icon-Generierung
│   │
│   └── UI/                   # UI-Controller
│       ├── Main.cs
│       ├── MainMenu.cs
│       ├── DifficultyMenu.cs
│       ├── GameScene.cs
│       ├── SettingsMenu.cs
│       ├── HistoryMenu.cs
│       ├── StatsMenu.cs
│       └── TipsMenu.cs
│
└── Examples/                 # Beispiel-JSON-Dateien
    ├── settings.json
    ├── savegame.json
    └── history.json
```

## 📣 Präsentation (pro Version)

- Aktuell: `Docs/Presentation/Presentation_0_0_1.md`
- Vorlage/initial: `Docs/Presentation/Presentation.md`

## 🎮 Features

### Hauptmenü

- **Spiel fortsetzen** - Nur sichtbar bei vorhandenem Spielstand
- **Neues Spiel** - Startet Schwierigkeitsauswahl
- **Einstellungen** - Theme, Deadly Mode, etc.
- **Verlauf** - Liste aller gespielten Spiele
- **Statistik** - Aggregierte Spielstatistiken
- **Tipps & Tricks** - 12 Sudoku-Strategien
- **Beenden** - Schließt das Spiel
- Vollständige Tastaturnavigation

### Schwierigkeitsauswahl

- **Leicht** - ~46 vorgegebene Zahlen
- **Mittel** - ~36 vorgegebene Zahlen
- **Schwer** - ~26 vorgegebene Zahlen
- Alle Rätsel haben eine **eindeutige Lösung**

### Spiel-Szene

- 9x9 Grid mit klaren 3x3-Block-Trennungen
- Vorgegebene Zahlen (Givens) sind nicht editierbar
- **Eingabe:**
  - Mausklick auf Zelle → Auswahl
  - Zahlenleiste 1-9 oder Tastatur → Zahl setzen
  - Entf/Backspace oder Radiergummi → Löschen
- **Highlighting:**
  - Ausgewählte Zelle
  - Gleiche Zahlen im Grid
  - Zeile/Spalte/Block (optional)
- **Fehlerlogik:**
  - Visuelles Feedback (rot blinkend)
  - Fehlerzähler
  - Deadly Mode: 3 Fehler = Game Over
- **Zahlenleiste:**
  - Zahlen bei 9x Platzierung ausgrauen ODER ausblenden (einstellbar)
- **Timer** - Zeigt verstrichene Zeit

### Einstellungen

- Theme-Auswahl (Hell/Dunkel)
- Deadly Mode Toggle
- Vollständige Zahlen ausblenden/ausgrauen
- Zeile/Spalte/Block Highlighting
- Persistent gespeichert in `user://settings.json`

### Verlauf

- Liste aller Spiele mit:
  - Datum/Uhrzeit
  - Schwierigkeit
  - Dauer
  - Fehleranzahl
  - Ergebnis (Gewonnen/Verloren/Abgebrochen)
- Farbige Status-Indikatoren

### Statistik

- Spiele gesamt
- Wins/Losses
- Gewinnrate mit Fortschrittsbalken
- Beste/Längste Zeit
- Durchschnittliche Zeit pro Schwierigkeit
- Durchschnittliche Fehler pro Schwierigkeit

### Tipps & Tricks

- 12 Sudoku-Techniken
- Carousel-Navigation (Zurück/Weiter)
- Tastatursteuerung (Links/Rechts)

## 🔧 Technische Details

### Architektur

- **Models:** Reine Datenklassen (SudokuCell, SudokuGameState, etc.)
- **Logic:** Spiellogik ohne Godot-Abhängigkeiten (Generator, Solver)
- **Services:** Autoload-Singletons für globalen Zustand
- **UI:** Control-basierte Scene-Controller

### Sudoku-Generator

1. Erstellt vollständiges, gültiges 9x9 Grid (Backtracking)
2. Entfernt Zahlen basierend auf Schwierigkeit
3. Prüft Eindeutigkeit per Solver (CountSolutions ≤ 1)

### Persistenz

- `user://settings.json` - Einstellungen
- `user://savegame.json` - Aktuelles Spiel
- `user://history.json` - Spielverlauf

### Theme-System

- Programmatisches UI-Styling
- Hell/Dunkel Theme
- StyleBoxFlat für alle UI-Elemente
- Farben zentral in ThemeService definiert

## 🚀 Setup in Godot

### 1. Projekt öffnen

Öffne das Projekt in Godot 4.5.

### 2. C# Build

Build das C#-Projekt:

```bash
dotnet build
```

Oder in Godot: Projekt → C# Lösung erstellen

### 3. Autoloads prüfen

Die Autoloads sollten bereits konfiguriert sein:

- `SaveService` → `res://Scripts/Services/SaveService.cs`
- `AppState` → `res://Scripts/Services/AppState.cs`
- `ThemeService` → `res://Scripts/Services/ThemeService.cs`

### 4. Main Scene

Main Scene ist gesetzt auf: `res://Scenes/Main.tscn`

### 5. Starten

F5 oder Play-Button drücken.

## 🎮 Steuerung

| Aktion           | Eingabe                            |
| ---------------- | ---------------------------------- |
| Zelle auswählen  | Mausklick                          |
| Zahl setzen      | 1-9 (Tastatur oder Numpad)         |
| Zahl löschen     | Entf, Backspace, oder Radiergummi  |
| Zurück           | ESC                                |
| Navigation       | Pfeiltasten, Tab                   |
| Bestätigen       | Enter, Space                       |

## 📝 Node-Hierarchien

### Main.tscn

```text
Main (Control)
├── Background (ColorRect)
└── SceneContainer (Control)
```

### MainMenu.tscn

```text
MainMenu (Control)
└── CenterContainer
    └── Panel (PanelContainer)
        └── MarginContainer
            └── VBoxContainer
                ├── Title (Label)
                ├── Subtitle (Label)
                ├── HSeparator
                └── ButtonContainer (VBoxContainer)
                    ├── ContinueButton
                    ├── StartButton
                    ├── SettingsButton
                    ├── HistoryButton
                    ├── StatsButton
                    ├── TipsButton
                    ├── HSeparator2
                    └── QuitButton
```

### GameScene.tscn

```text
GameScene (Control)
├── VBoxContainer
│   ├── Header (HBoxContainer)
│   │   ├── BackButton
│   │   ├── DifficultyLabel
│   │   ├── TimerLabel
│   │   └── MistakesLabel
│   ├── GridCenterContainer
│   │   └── GridPanel (PanelContainer)
│   │       └── GridContainer (9x9 Buttons)
│   └── NumberPadContainer
│       └── NumberPad (HBoxContainer, 1-9 + Eraser)
└── OverlayContainer (für Win/GameOver)
```

## 📄 Lizenz

MIT License

## 🎯 Akzeptanzkriterien (alle erfüllt)

- ✅ Neues Spiel starten → Sudoku erscheint
- ✅ Eingabe per Maus + Tastatur funktioniert
- ✅ Fortsetzen nur bei existierendem Savegame
- ✅ Deadly Modus: 3 Fehler = Game Over
- ✅ Highlighting funktioniert
- ✅ Zahlen werden bei 9x platziert ausgegraut/ausgeblendet
- ✅ Win speichert HistoryEntry + löscht Savegame
- ✅ Einstellungen bleiben nach Neustart erhalten
