# SudokuSen v0.0.4 - Changelog

## Neue Features

### 💀 Neuer Schwierigkeitsgrad: "Insane"
- **Extremer Schwierigkeitsgrad** für absolute Sudoku-Experten
- Nur ~21 gegebene Zahlen (60 Zellen werden entfernt)
- Erfordert fortgeschrittene Techniken wie:
  - Unique Rectangle
  - Finned X-Wing & Finned Swordfish
  - Remote Pair
  - BUG+1 (Bivalue Universal Grave)
  - ALS-XZ Rule
  - Forcing Chains
- Lila Button-Design zur Unterscheidung

### 🧠 Neue Lösungstechniken (Level 4)
7 neue fortgeschrittene Techniken wurden hinzugefügt:

| Technik | Beschreibung |
|---------|-------------|
| **Unique Rectangle** | Vermeidet 'Deadly Pattern' Rechtecke die zu mehreren Lösungen führen würden |
| **Finned X-Wing** | X-Wing Muster mit zusätzlichen "Flossen" für begrenzte Eliminierungen |
| **Finned Swordfish** | Swordfish Muster mit Flossen |
| **Remote Pair** | Kette von Zellen mit identischen Kandidaten-Paaren |
| **BUG+1** | Bivalue Universal Grave - wenn alle Zellen nur zwei Kandidaten hätten außer einer |
| **ALS-XZ Rule** | Almost Locked Sets die durch gemeinsame Kandidaten verbunden sind |
| **Forcing Chain** | Wenn beide möglichen Werte zum gleichen Ergebnis führen |

### 📊 Statistik-Erweiterung
- Insane-Statistiken in der Statistik-Übersicht
- Durchschnittliche Zeit und Fehler für Insane
- Progression berücksichtigt jetzt alle 5 Schwierigkeitsgrade

## Technische Verbesserungen

### Code-Änderungen
- `SudokuGameState.cs`: Neuer `Difficulty.Insane` Enum-Wert
- `SudokuGenerator.cs`: 60 Zellen werden bei Insane entfernt
- `TechniqueInfo.cs`: 7 neue Techniken (Level 4), aktualisierte Technique-Mappings
- `SettingsData.cs`: Neue `TechniquesInsane` Konfiguration
- `DifficultyMenu.cs/tscn`: Neuer Button mit lila Design
- `StatsMenu.cs/tscn`: Insane-Statistiken
- `HistoryEntry.cs`: Insane-Display mit 💀 Emoji
- `GameScene.cs`: Insane-Anzeige im Spiel
- `SettingsMenu.cs`: Insane in Technik-Konfiguration
- `SaveService.cs`: Insane in Progression-Pfad
- `AppState.cs`: Level 4 Techniken starten Insane-Spiele
- `MainMenu.cs`: Challenge-Modus unterstützt Insane

## Gesamtübersicht Techniken

### Level 1 - Kids & Easy (4 Techniken)
- Naked Single
- Hidden Single (Zeile/Spalte/Block)

### Level 2 - Medium (7 Techniken)
- Naked Pair, Triple, Quad
- Hidden Pair, Triple
- Pointing Pair
- Box/Line Reduction

### Level 3 - Hard (10 Techniken)
- X-Wing, Swordfish, Jellyfish
- XY-Wing, XYZ-Wing, W-Wing
- Skyscraper, Two-String Kite
- Empty Rectangle
- Simple Coloring

### Level 4 - Insane (7 neue Techniken)
- Unique Rectangle
- Finned X-Wing, Finned Swordfish
- Remote Pair
- BUG+1
- ALS-XZ Rule
- Forcing Chain

**Gesamtzahl definierter Techniken: 28**

## Hinweise

- Die neuen Level-4-Techniken sind im HintService noch nicht implementiert
- Insane-Puzzles sind für erfahrene Spieler gedacht
- Die Puzzles sind garantiert eindeutig lösbar (unique solution)
