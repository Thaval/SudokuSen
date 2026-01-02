# 🧩 SudokuSen - Benutzerhandbuch

**Ein modernes Sudoku-Spiel für Desktop – entwickelt mit Godot 4 & C#**

*Version 0.0.5*

---

## 🏠 Willkommen

SudokuSen bietet ein elegantes, benutzerfreundliches Sudoku-Erlebnis mit mehreren Schwierigkeitsstufen, intelligenten Hinweisen und umfangreichen Statistiken.

![Homescreen](screenshots/0.0.5/HomeScreen.png)

### Hauptmenü

Das Hauptmenü bietet alle wichtigen Funktionen auf einen Blick – jetzt mit Icons für bessere Übersicht:

| Menüpunkt | Icon | Beschreibung |
|-----------|------|--------------|
| Weiterspielen | ▶️ | Setze dein laufendes Spiel fort |
| Neues Spiel | 🆕 | Starte ein frisches Sudoku |
| Tägliches Rätsel | 📅 | Ein neues Puzzle jeden Tag |
| Szenarien | 🎯 | Trainiere spezifische Techniken |
| Tipps & Tutorials | 💡 | Lerne Sudoku-Strategien |
| Puzzles | 🧩 | Vorgefertigte Rätsel spielen |
| Historie | 📜 | Siehe alle gespielten Partien |
| Statistik | 📊 | Verfolge deine Fortschritte |
| Einstellungen | ⚙️ | Passe das Spiel an |
| Beenden | 🚪 | Spiel schließen |

Falls eine bestehende Partie offen ist, kannst du diese mit **Weiterspielen** direkt fortsetzen.

---

## 🎯 Schwierigkeitsstufen

Wähle aus fünf verschiedenen Schwierigkeitsgraden – vom kinderfreundlichen 4×4 bis zum herausfordernden Insane-Modus.

| Stufe | Raster | Hinweise | Beschreibung |
|-------|--------|----------|--------------|
| 👶 **Kids** | 4×4 | 8 | Perfekt für Einsteiger und Kinder (Zahlen 1-4) |
| 🟢 **Leicht** | 9×9 | 46 | Naked Single, Hidden Single |
| 🟠 **Mittel** | 9×9 | 36 | + Naked Pair, Pointing Pair |
| 🔴 **Schwer** | 9×9 | 26 | + X-Wing, Swordfish, XY-Wing |
| 💀 **Insane** | 9×9 | 21 | Alle Techniken erforderlich |

---

## 👶 Kids-Modus

Ein vereinfachtes 4×4-Raster mit großen Zellen – ideal für Kinder und Sudoku-Neulinge.

### Kids-Features:
- Übersichtliches 4×4-Gitter mit 2×2-Blöcken
- Nur Zahlen 1-4
- Extra große, gut lesbare Zellen
- Sanfter Einstieg in die Sudoku-Logik

---

## 🎮 Spieloberfläche

Die klassische 9×9-Spielansicht mit allen wichtigen Funktionen auf einen Blick.

![In-Game](screenshots/0.0.5/IngameReplay.png)

### Spielelemente:
- ⏱️ **Timer** – Messe deine Zeit
- ❌ **Fehlerzähler** – Behalte deine Fehler im Blick
- ✏️ **Notizen-Modus** – Markiere mögliche Kandidaten (blau)
- 📋 **Auto-Kandidaten** – Automatische Anzeige aller Möglichkeiten (grau)
- 💡 **Hinweise** – Intelligente Tipps mit visueller Erklärung
- 🔢 **Zahlenpad** – Intuitive Eingabe per Klick oder Tastatur
- 🛤️ **Lösungspfad** – Zeigt alle Schritte zur Lösung

### Steuerung:

| Aktion | Eingabe |
|--------|---------|
| Zelle auswählen | Mausklick |
| Zahl eingeben | 1-9 (Tastatur oder Numpad) |
| Zahl löschen | Entf, Backspace oder Radierer |
| Notizen-Modus | N |
| Mehrfachauswahl | Ctrl + Klick |
| Bereichsauswahl | Shift + Klick |
| Navigation | Pfeiltasten |
| Zurück | ESC |

### Hervorhebungen:
- **Ausgewählte Zelle** – Die aktive Zelle wird hervorgehoben
- **Verwandte Zellen** – Zeile, Spalte und Block werden markiert
- **Gleiche Zahlen** – Alle identischen Ziffern werden hervorgehoben

### Notizen-Modus:
Der Notizen-Modus (Taste **N** oder Bleistift-Button) ermöglicht das Eintragen von Kandidaten:
- Aktiviert: Zahlen werden als kleine Notizen eingetragen
- Bei Mehrfachauswahl: Notiz wird in alle ausgewählten Zellen eingetragen

**Hinweis**: Bei deaktiviertem Notizen-Modus und Mehrfachauswahl wird die Zahl nur in die zuletzt gewählte Zelle (dunkelblau) eingetragen.

---

## 💡 Hinweis-System

Das intelligente Hinweis-System hilft dir, ohne die Lösung direkt zu verraten.

### Hinweise in 4 Schritten:

1. **Zelle zeigen** – Welche Zelle ist relevant? (Du kannst noch selbst knobeln!)
2. **Kontext zeigen** – Relevante Zellen werden hervorgehoben
3. **Lösung zeigen** – Die korrekte Zahl wird angezeigt
4. **Erklärung** – Warum ist diese Lösung korrekt?

### Menschenfreundliche Erklärungen

Die Hinweise zeigen jetzt **warum** eine Zahl an einer Stelle steht:

> "Die 6 kann nur in A2 stehen, weil die 6en bei B6, C9, F3 alle anderen Zellen blockieren."

Die Erklärungen:
- Referenzieren die **blockierenden Zahlen** im Raster
- Verwenden **A1-Notation** (wie beim Schach)
- Machen die Logik **nachvollziehbar**

### A1-Notation:

| | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
|--|---|---|---|---|---|---|---|---|---|
| **A** | A1 | A2 | A3 | A4 | A5 | A6 | A7 | A8 | A9 |
| **B** | B1 | B2 | B3 | B4 | B5 | B6 | B7 | B8 | B9 |
| **...** | | | | | | | | | |
| **I** | I1 | I2 | I3 | I4 | I5 | I6 | I7 | I8 | I9 |

---

## 🛤️ Lösungspfad

Der Lösungspfad zeigt dir alle Schritte, um das aktuelle Puzzle zu lösen.

### Funktionen:
- **Toggle-Button** – Ein Klick öffnet, ein weiterer schließt den Lösungspfad
- **Klickbare Schritte** – Klicke auf einen Schritt für Details
- **Detail-Panel** – Zeigt Technik, Zelle und ausführliche Erklärung

### Verwendung:
1. Klicke auf den **Lösungspfad-Button** (rechts oben)
2. Das Overlay erscheint mit allen Lösungsschritten
3. Klicke auf einen Schritt für die detaillierte Erklärung
4. Das Detail-Panel erscheint links neben dem Raster

Das Detail-Panel zeigt:
- **Technik-Name** (z.B. "Hidden Single")
- **Betroffene Zelle** (z.B. "A2 = 6")
- **Warum** diese Lösung korrekt ist
- **Verwandte Zellen** die zur Lösung beitragen

---

## 🎯 Szenarien & Tutorials

Trainiere spezifische Sudoku-Techniken mit vorbereiteten Szenarien.

![Szenarien - Tutorials](screenshots/0.0.5/ScenariosTutorials.png)

### Tutorial-Szenarien:
Lerne die Grundlagen mit geführten Tutorials:
- Einführung in Sudoku-Regeln
- Erste Schritte mit Notizen
- Grundlegende Lösungstechniken

![Szenarien - Techniken Easy](screenshots/0.0.5/ScenariosTechniquesEasy.png)

### Technik-Szenarien:
Übe spezifische Techniken isoliert:
- **Level 1** – Naked Single, Hidden Single
- **Level 2** – Naked Pair, Hidden Pair, Pointing Pair
- **Level 3** – X-Wing, Swordfish, Box/Line Reduction
- **Level 4** – Fortgeschrittene Techniken

---

## 🧩 Vorgefertigte Puzzles

Spiele handverlesene Puzzles mit bekannter Schwierigkeit.

![Vorgefertigte Puzzles](screenshots/0.0.5/PreBuiltPuzzles.png)

### Features:
- Sortiert nach Schwierigkeit
- Fortschritt wird gespeichert
- Perfekt zum gezielten Üben

---

## 📜 Spielverlauf

Behalte den Überblick über alle deine gespielten Partien.

![Historie](screenshots/0.0.5/GameHistory.png)

### Verlauf-Features:
- Chronologische Auflistung aller Spiele
- Schwierigkeit, Zeit und Ergebnis auf einen Blick
- Farbcodierung: ✅ Gewonnen | ❌ Verloren | ⏸️ Abgebrochen
- **Replay-Funktion** – Spiele alte Partien erneut

---

## 💡 Tipps & Tricks

Lerne fortgeschrittene Sudoku-Techniken mit interaktiven Erklärungen.

### Enthaltene Techniken:

| Technik | Beschreibung |
|---------|--------------|
| **Naked Single** | Nur eine Zahl möglich in einer Zelle |
| **Hidden Single** | Zahl nur an einer Stelle in Zeile/Spalte/Block |
| **Naked Pair** | Zwei Zellen mit gleichen Kandidaten |
| **Hidden Pair** | Zwei Kandidaten nur in zwei Zellen |
| **Pointing Pair** | Kandidaten zeigen auf eine Richtung |
| **Box/Line Reduction** | Block-Zeilen-Interaktion |
| **X-Wing** | Fortgeschrittene Eliminierungstechnik |
| **Swordfish** | Erweiterte X-Wing-Variante |
| **XY-Wing** | Drei-Zellen-Kette |
| **Unique Rectangle** | Verhindert mehrdeutige Lösungen |
| **Finned X-Wing** | X-Wing mit zusätzlicher "Flosse" |
| **Remote Pair** | Ketten identischer Kandidaten-Paare |
| **BUG+1** | Bivalue Universal Grave |
| **ALS-XZ Rule** | Almost Locked Sets |
| **Forcing Chains** | Wenn-Dann-Ketten |

Jede Technik wird mit einem visuellen Mini-Board erklärt!

---

## ⚙️ Einstellungen

Passe SudokuSen an deinen Spielstil an.

![Einstellungen](screenshots/0.0.5/Settings.png)

### Optionen:

| Einstellung | Beschreibung |
|-------------|--------------|
| 🎨 **Theme** | Hell, Dunkel oder System |
| 🌍 **Sprache** | Deutsch, English |
| 🔊 **Soundeffekte** | An/Aus |
| 🎵 **Musik** | An/Aus |
| 💀 **Deadly-Modus** | Bei 3 Fehlern Game Over |
| 🔦 **Verwandte Zellen** | Zeile/Spalte hervorheben |
| 🔢 **Gleiche Zahlen** | Identische Ziffern markieren |
| 👁️ **Abgeschlossene ausblenden** | Vollständige Zahlen im Numpad verstecken |
| 📏 **UI-Skalierung** | Interface-Größe anpassen |

---

## 📊 Statistiken

Verfolge deinen Fortschritt über alle Schwierigkeitsgrade.

### Angezeigte Werte:
- Gespielte Partien pro Schwierigkeit
- Gewinnrate
- Durchschnittliche Zeit
- Durchschnittliche Fehler
- Beste Zeit
- Aktuelle Gewinnsträhne

---

## 📅 Tägliches Rätsel

Jeden Tag ein neues Puzzle – alle Spieler weltweit bekommen das gleiche!

### Features:
- Neues Puzzle jeden Tag um Mitternacht
- Streak-Tracking für tägliche Herausforderungen
- Vergleiche deine Zeit mit anderen

---

## 🛠️ Technische Details

| Eigenschaft | Wert |
|-------------|------|
| **Engine** | Godot 4.5.1 |
| **Sprache** | C# / .NET 8 |
| **Plattform** | Windows (Desktop) |
| **Speicherung** | Lokale JSON-Dateien |

### Speicherorte:
- **Einstellungen**: `%APPDATA%/Godot/app_userdata/SudokuSen/settings.json`
- **Spielstand**: `%APPDATA%/Godot/app_userdata/SudokuSen/savegame.json`
- **Historie**: `%APPDATA%/Godot/app_userdata/SudokuSen/history.json`

---

## 📥 Installation

1. Lade die neueste Version von [GitHub Releases](https://github.com/Thaval/SudokuSen/releases) herunter
2. Entpacke das ZIP-Archiv in einen beliebigen Ordner
3. Starte `SudokuSen.exe`

**Keine Installation erforderlich – einfach spielen!**

---

## ❓ FAQ

**Q: Mein Spielstand ist weg!**
A: Spielstände werden im AppData-Ordner gespeichert. Prüfe `%APPDATA%/Godot/app_userdata/SudokuSen/`.

**Q: Das Spiel startet nicht.**
A: Stelle sicher, dass .NET 8 Runtime installiert ist.

**Q: Kann ich das Spiel portabel nutzen?**
A: Ja! Der gesamte Ordner kann kopiert werden. Spielstände bleiben jedoch im AppData.

---

## 🆕 Neu in Version 0.0.5

- **Menü-Icons** – Alle Hauptmenü-Einträge haben jetzt Icons
- **Lösungspfad-Toggle** – Button öffnet/schließt per Klick
- **Klickbare Lösungsschritte** – Detail-Panel statt Hover-Tooltips
- **Menschenfreundliche Erklärungen** – Hinweise zeigen blockierende Zellen
- **Responsive UI** – Overlays passen sich der Fenstergröße an
- **Bugfixes** – 15 fehlende Übersetzungen, doppelte Keys entfernt

---

<div align="center">

### 🎮 Viel Spaß beim Knobeln!

*SudokuSen – Dein persönlicher Sudoku-Begleiter*

[GitHub](https://github.com/Thaval/SudokuSen) | [Releases](https://github.com/Thaval/SudokuSen/releases) | [Issues](https://github.com/Thaval/SudokuSen/issues)

</div>
