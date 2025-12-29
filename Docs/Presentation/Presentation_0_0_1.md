# 🧩 MySudoku v0.0.1

**Ein modernes Sudoku-Spiel für Desktop – entwickelt mit Godot 4 & C#**

> Release: 2025-12-29

---

## 🏠 Willkommen

MySudoku bietet ein elegantes, benutzerfreundliches Sudoku-Erlebnis mit mehreren Schwierigkeitsstufen, intelligenten Hinweisen und umfangreichen Statistiken.

![Homescreen](screenshots/0.0.1/home.png)

### Hauptmenü-Features:
- 🆕 **Neues Spiel** – Starte ein frisches Sudoku
- 🗓️ **Daily Sudoku** – Tägliches Rätsel mit Streak (einmal pro Tag „offiziell“)
- ▶️ **Fortsetzen** – Setze dein laufendes Spiel fort
- 📊 **Statistik** – Verfolge deine Fortschritte (inkl. Daily/Techniken/Heatmap)
- 📜 **Verlauf** – Siehe alle gespielten Partien
- 💡 **Tipps & Tricks** – Lerne Sudoku-Strategien
- ⚙️ **Einstellungen** – Passe das Spiel an deine Vorlieben an

Wenn eine bestehende Partie offen ist, kannst du diese mit `Spiel fortsetzen` weiterspielen.

![MainMenu Details](screenshots/0.0.1/main_menu.png)

---

## 🎯 Schwierigkeitsstufen

Wähle aus vier verschiedenen Schwierigkeitsgraden – vom kinderfreundlichen 4×4 bis zum anspruchsvollen 9×9.

![DifficultyScreen](screenshots/0.0.1/difficulty.png)

| Stufe | Raster | Beschreibung |
|-------|--------|--------------|
| 👶 **Kids** | 4×4 | Perfekt für Einsteiger und Kinder (Zahlen 1-4) |
| 🟢 **Leicht** | 9×9 | Naked Single, Hidden Single |
| 🟠 **Mittel** | 9×9 | + Naked Pair, Pointing Pair |
| 🔴 **Schwer** | 9×9 | + X-Wing, Swordfish, XY-Wing |

---

## 👶 Kids-Modus

Ein vereinfachtes 4×4-Raster mit großen Zellen – ideal für Kinder und Sudoku-Neulinge.

![KidsGame](screenshots/0.0.1/kids_game.png)

### Kids-Features:
- Übersichtliches 4×4-Gitter mit 2×2-Blöcken
- Nur Zahlen 1-4
- Extra große, gut lesbare Zellen
- Sanfter Einstieg in die Sudoku-Logik

---

## 🎮 Spieloberfläche

Die klassische 9×9-Spielansicht mit allen wichtigen Funktionen auf einen Blick.

![Game](screenshots/0.0.1/game.png)

### Spielfunktionen:
- ⏱️ **Timer** – Messe deine Zeit (Time Attack zeigt Restzeit)
- ❌ **Fehlerzähler** – Behalte deine Fehler im Blick (inkl. Perfect Run Challenge)
- ✏️ **Notizen-Modus** – Markiere mögliche Kandidaten (blau)
- 🧹 **Notizen bereinigen** – Entfernt automatisch die gesetzte Zahl aus Notizen in Zeile/Spalte/Block (optional)
- 📋 **Auto-Kandidaten** – Automatische Anzeige aller Möglichkeiten (grau)
- 🧠 **Auto-Notizen (House)** – Button `R/C/B` füllt Kandidaten als Notizen für Zeile/Spalte/Block (optional)
- 💡 **Hinweise** – Intelligente Tipps mit visueller Erklärung (mit Hint-Limit Challenge möglich)
- 🔢 **Zahlenpad** – Intuitive Eingabe per Klick oder Tastatur

### Steuerung:
- **Pfeiltasten** – Navigation im Grid
- **Zifferntasten 1-9** – Zahl eingeben
- **N** – Notizen-Modus umschalten
- **Entf/Backspace** – Zahl löschen
- **Ctrl+Klick** – Mehrfachauswahl
- **Shift+Klick** – Bereichsauswahl

### In-Game Features:

- **Zellen hervorheben** – Auswahl + Zeile/Spalte (optional)
  ![SelektiereZelle](screenshots/0.0.1/select_cell.png)
- **Mehrere Zellen markieren** – via Dragging oder Ctrl+Klick / Arrow+Shift
  ![MultiSelect](screenshots/0.0.1/multi_select.png)
- **On-the-fly Tipps** – `💡` zeigt Hinweise in mehreren Seiten (Kontext → Lösung → Erklärung)
  ![Hint](screenshots/0.0.1/hint_overlay.png)

---

## 🗓️ Daily Sudoku & Streak

Jeden Tag gibt es ein **Daily** (deterministisch pro Datum). Beim Lösen wird dein **Streak** aktualisiert.

![Daily](screenshots/0.0.1/daily.png)

---

## 🎯 Challenge Modes

Challenge Modes gelten für **neue Spiele**:
- **Keine Notizen**
- **Perfect Run** (1 Fehler = verloren)
- **Hint-Limit**
- **Time Attack**

![Challenges](screenshots/0.0.1/challenges.png)

---

## 📈 Statistik & Fortschritt

MySudoku trackt u.a.:
- Spielzeiten, Siege/Niederlagen
- Daily Streak (aktuell/best)
- Technik-Fortschritt (Hinweis gezeigt / angewandt)
- Fehler-Heatmap (wo du am häufigsten Fehler machst)

![Stats](screenshots/0.0.1/stats.png)

---

## ⚙️ Einstellungen

Passe MySudoku an deinen Spielstil an.

![Settings](screenshots/0.0.1/settings.png)

### Optionen (Auszug):
- 🎨 Theme (Hell/Dunkel)
- ♿ UI-Skalierung
- 🎨 Farbblind-Palette
- 📘 Lernmodus (Erklärung bei Fehlern)
- 🧹 Notizen bereinigen (Smart Cleanup)
- 🧠 Auto-Notizen Button (R/C/B)
- 💀 Deadly Mode
- Challenge Modes

---

## 🛠️ Technische Details

| Eigenschaft | Wert |
|-------------|------|
| **Engine** | Godot 4.5.x |
| **Sprache** | C# / .NET 8 |
| **Plattform** | Windows (Desktop) |
| **Version** | 0.0.1 |
| **Speicherung** | Lokale JSON-Dateien |

---

## 📥 Installation

1. Lade die passende Version herunter
2. Entpacke das Archiv
3. Starte `MySudoku.exe`

**Keine Installation erforderlich – einfach spielen!**

---

## 📸 Screenshots aktualisieren (v0.0.1)

Lege neue Screenshots im Ordner `Docs/Presentation/screenshots/0.0.1/` ab und benutze diese Dateinamen, damit die Links oben stimmen:

- `home.png`
- `main_menu.png`
- `difficulty.png`
- `kids_game.png`
- `game.png`
- `select_cell.png`
- `multi_select.png`
- `hint_overlay.png`
- `daily.png`
- `challenges.png`
- `stats.png`
- `settings.png`

Tipp (Windows): `Win + Shift + S` (Ausschnitt) oder `Alt + PrtScn` (aktives Fenster).
