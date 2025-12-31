# 🧩 MySudoku v0.0.1 – User Guide

**A modern Sudoku game for Desktop – built with Godot 4 & C#**

MySudoku offers an elegant, beginner-friendly Sudoku experience with multiple difficulty levels, intelligent hints that teach you solving techniques, and comprehensive statistics to track your progress. Whether you're a complete beginner or a seasoned puzzle solver, MySudoku adapts to your skill level.

> **Release:** December 29, 2025

---

## 📑 Table of Contents

1. [Quick Start](#quick-start)
2. [Main Screens](#main-screens)
3. [Playing the Game](#playing-the-game)
4. [Notes & Candidates](#notes--candidates)
5. [The Hint System](#the-hint-system)
6. [Daily Sudoku & Streaks](#daily-sudoku--streaks)
7. [Challenge Modes](#challenge-modes)
8. [Statistics & Progress](#statistics--progress)
9. [Settings](#settings)
10. [Tips & Shortcuts](#tips--shortcuts)
11. [FAQ / Troubleshooting](#faq--troubleshooting)
12. [Technical Details](#technical-details)

---

## 🚀 Quick Start

Get playing in under a minute:

1. **Launch MySudoku** – Double-click `MySudoku.exe`
2. **Click "Neues Spiel"** (New Game) in the main menu
3. **Select a difficulty** – Start with "Leicht" (Easy) if you're new
4. **Click a cell** to select it
5. **Enter a number** using the number pad or keyboard (1-9)
6. **Stuck?** Press the 💡 hint button for guidance
7. **Complete the puzzle** – Fill all cells correctly to win!

![Main Menu](screenshots/0.0.1/HomeScreen.png)

*The main menu – click "Neues Spiel" to start a new game or "Spiel fortsetzen" to continue.*

---

## 🖥️ Main Screens

### Home Screen

The home screen is your starting point. Here's what each button does:

| Button | Function |
|--------|----------|
| **Neues Spiel** | Start a fresh puzzle |
| **📅 Daily Sudoku** | Play today's daily puzzle (tracks streak) |
| **Spiel fortsetzen** | Continue your saved game (only visible if you have one) |
| **Statistik** | View your statistics and progress |
| **Verlauf** | Browse your game history |
| **Tipps & Tricks** | Learn Sudoku strategies |
| **Einstellungen** | Customize the game |
| **Beenden** | Exit the application |

---

### Difficulty Selection

Choose from four difficulty levels to match your skill:

![Difficulty Selection](screenshots/0.0.1/SelectDifficulty.png)

*Select your preferred difficulty – Kids mode uses a smaller 4×4 grid.*

| Level | Grid | Techniques |
|-------|------|------------|
| 👶 **Kids** | 4×4 | 4×4 grid with numbers 1-4 (simplified logic) |
| 🟢 **Leicht** (Easy) | 9×9 | Naked Single, Hidden Single |
| 🟠 **Mittel** (Medium) | 9×9 | + Naked Pair, Pointing Pair, Box/Line |
| 🔴 **Schwer** (Hard) | 9×9 | + X-Wing, Swordfish, XY-Wing |

**Tip:** Each puzzle has exactly one solution – if you're stuck, there's always a logical path forward!

---

## 🎮 Playing the Game

### The Game Screen

![9x9 Game Screen](screenshots/0.0.1/9x9Ingame.png)

*The main game view – a 9×9 grid divided into nine 3×3 blocks.*

The game screen shows:

- **Timer** (top) – Tracks your solving time
- **Error counter** – Shows mistakes made (important in Deadly Mode!)
- **The Sudoku grid** – Your puzzle
- **Control buttons** – Notes, hints, and special features
- **Number pad** – For entering digits

---

### Controls Overview

![Grid Controls](screenshots/0.0.1/GridControls.png)

*The number pad and control buttons at the bottom of the screen.*

| Control | Function |
|---------|----------|
| **1-9 buttons** | Enter that digit in the selected cell |
| **⌫ (Eraser)** | Clear the selected cell |
| **✏️ (Notes)** | Toggle notes mode – enter candidates instead of answers |
| **💡 (Hint)** | Get a smart hint with explanation |
| **R/C/B** | Auto-fill notes for Row/Column/Block |

---

### Selecting Cells

Click any empty cell to select it. The game highlights:

- The **selected cell** (accent color)
- The **row and column** containing the selection
- All cells with the **same number** (if highlighting is enabled)

![Cell Highlighting](screenshots/0.0.1/IngameHighlightDigitsRowsAndCols.png)

*When you select a cell, related cells are highlighted to help you spot patterns.*

#### Multi-Select

You can select multiple cells at once:

- **Ctrl + Click** – Add/remove individual cells from selection
- **Shift + Click** – Select a range of cells
- **Drag** – Draw a selection across multiple cells

This is useful for entering the same note in multiple cells at once!

---

### Entering Numbers

**Method 1: Number Pad**
Click a cell, then click a number (1-9) on the number pad.

**Method 2: Keyboard**
Click a cell, then press a number key (1-9).

**To delete:** Press `Delete`, `Backspace`, or click the ⌫ eraser button.

![Grid with Labels](screenshots/0.0.1/GridLabels.png)

*The grid shows given numbers (fixed) and your entries. Wrong answers appear in red.*

---

### Kids Mode (4×4)

Perfect for young players or Sudoku beginners:

![Kids Mode](screenshots/0.0.1/KidsIngame.png)

*The simplified 4×4 grid uses only numbers 1-4 and has larger cells.*

- Smaller 4×4 grid with 2×2 blocks
- Only digits 1-4
- Extra-large, easy-to-read cells
- Same controls as the full game

---

## ✏️ Notes & Candidates

Notes (also called "pencil marks" or "candidates") help you track which numbers could go in each cell.

### Adding Notes

1. Click the **✏️ Notes** button to enter notes mode (button shows as active)
2. Click a cell
3. Click numbers 1-9 to toggle them as notes
4. Click the Notes button again to return to normal mode

![Adding Notes](screenshots/0.0.1/IngameAddNote.png)

*In notes mode, clicking numbers adds small candidate markers instead of filling the cell.*

![Notes Display](screenshots/0.0.1/IngameShowNotes.png)

*Notes appear as small numbers in the corners of cells.*

---

### Auto-Notes (House Fill)

The **R/C/B button** lets you automatically fill notes for an entire row, column, or block:

![Row/Column/Block Toggle](screenshots/0.0.1/RowColBlockNoteToggle.png)

*The R/C/B button fills candidates automatically – right-click or Shift+click to cycle modes.*

**How to use:**

1. Select a cell in the row/column/block you want to fill
2. Click the R/C/B button to apply auto-notes
3. Right-click or Shift+click to cycle between Row → Column → Block modes

**Tip:** Enable "Notizen bereinigen" (Smart Cleanup) in Settings to automatically remove notes when you place a number!

---

## 💡 The Hint System

Stuck? The hint system doesn't just give you the answer – it teaches you *how* to find it.

**Note:** Hints are available for **9×9** puzzles only. (If your screen differs, check the selected difficulty.)

### Using Hints

1. Click the **💡 Hint** button
2. A popup appears showing the technique to use
3. Click through the pages to see:
   - **Context:** What pattern to look for
   - **Solution:** The cell and number to place
   - **Explanation:** Why this works

If **Hint-Limit** is enabled, the hint button stops working after the limit is reached.

![Hint Step 1](screenshots/0.0.1/IngameTipp1.png)

*Step 1: The hint identifies which technique applies to your current puzzle.*

![Hint Step 2](screenshots/0.0.1/IngameTipp2.png)

*Step 2: The relevant cells are highlighted on the grid.*

![Hint Step 3](screenshots/0.0.1/IngameTipp3.png)

*Step 3: The solution is revealed with the logical reasoning.*

![Hint Step 4](screenshots/0.0.1/IngameTipp4.png)

*Step 4: A detailed explanation helps you learn the technique for future puzzles.*

**Tip:** Use hints to learn! The Statistics screen tracks which techniques you've learned and applied.

---

## 🗓️ Daily Sudoku & Streaks

Every day brings a new **Daily Sudoku** (Medium difficulty) generated deterministically from today’s date.

### How Streaks Work

- Complete the Daily puzzle to extend your streak
- Your streak shows consecutive days solved
- Miss a day? Your streak resets to zero
- Track your current and best streak in Statistics

**Tip:** You can replay today’s Daily, but it won’t grant an extra streak after it’s already marked as completed.

---

## 🎯 Challenge Modes

Want more difficulty? Enable Challenge Modes before starting a new game:

| Challenge | Description |
|-----------|-------------|
| **Keine Notizen** | Notes are disabled – pure mental solving |
| **Perfect Run** | One mistake = game over |
| **Hint-Limit** | Limited number of hints allowed |
| **Time Attack** | Beat the clock before time runs out |

Configure these in **Settings** (Challenge Modes section).

**Note:** Challenge settings are applied when a game starts (including Daily). Changing Settings won’t modify an already running game.

---

## 📊 Statistics & Progress

Track your improvement over time:

![Statistics Screen](screenshots/0.0.1/Statistics.png)

*The statistics screen shows your overall progress, daily streaks, and technique mastery.*

### What's Tracked

- **Games played/won/lost** per difficulty
- **Average and best times**
- **Daily streak** (current and best)
- **Techniques learned** – Which solving methods you've used
- **Error heatmap** – See which cells give you trouble

---

### Game History

![History Screen](screenshots/0.0.1/History.png)

*Browse all your past games with date, difficulty, time, and result.*

View your complete game history including:

- Date and time played
- Difficulty level
- Completion time
- Win/loss status

---

## ⚙️ Settings

Customize MySudoku to your preferences:

![Settings Screen](screenshots/0.0.1/Settings.png)

*The settings menu – adjust visuals, gameplay, and accessibility options.*

### Available Options

| Setting | Description |
|---------|-------------|
| **Theme** | Switch between Light and Dark mode |
| **UI-Skalierung** | Adjust interface size for your screen |
| **Farbblind-Modus** | Alternative color palette for colorblind players |
| **Lernmodus** | Show explanations when you make mistakes |
| **Notizen bereinigen** | Automatically remove notes when placing numbers |
| **Auto-Notizen Button** | Show/hide the R/C/B button |
| **Deadly Mode** | 3 mistakes = game over |
| **Challenge Modes** | Enable/disable various challenges |

---

## 💡 Tips & Shortcuts

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **Arrow keys** | Navigate the grid |
| **1-9** | Enter number / toggle note |
| **N** | Toggle notes mode |
| **Delete / Backspace** | Clear cell |
| **Ctrl + Click** | Multi-select cells |
| **Shift + Arrow** | Extend selection |
| **Escape** | Return to menu |

### Strategy Tips

The Tips & Tricks menu teaches you essential solving techniques:

![Tips Menu](screenshots/0.0.1/TipsMenu.png)

*Browse 26 comprehensive tips – from basic techniques to advanced patterns like X-Wing and Swordfish.*

**Beginner techniques (Easy):**

- **Naked Single** – Only one number fits in a cell
- **Hidden Single** – A number can only go in one place within a row/column/block
- **Scanning** – Systematically check rows, columns, and blocks
- **Candidates** – Using pencil marks to track possibilities

**Intermediate techniques (Medium):**

- **Naked Pair** – Two cells share the same two candidates
- **Naked Triple** – Three cells share up to three candidates
- **Hidden Pair** – Two numbers appear only in two cells of a unit
- **Pointing Pair** – Block candidates point to row/column eliminations
- **Box/Line Reduction** – Row/column candidates confined to one block

**Advanced techniques (Hard):**

- **X-Wing** – Rectangle pattern across two rows/columns
- **Swordfish** – Extended X-Wing with three rows/columns
- **XY-Wing** – Three-cell pivot pattern for eliminations

**Additional tips in the menu:**

- General Strategies, Keyboard Shortcuts, Multi-Select, Practice Tips, Avoiding Mistakes

---

## ❓ FAQ / Troubleshooting

### The game shows an error when I enter a number

That number conflicts with another in the same row, column, or block. Check the highlighted cells!

### My notes disappeared

If "Notizen bereinigen" is enabled, notes are automatically removed when you place a number. You can disable this in Settings.

### The Daily Sudoku didn't count for my streak

You must complete the Daily on the correct calendar day. Check that your system date is correct.

### I want to restart the current puzzle

Go to the menu (Escape key or menu button) and select "Neues Spiel" with the same difficulty.

### The UI is too small/large

Adjust **UI-Skalierung** in Settings to scale the interface.

*(If your screen differs from these screenshots, check Settings → Theme or UI-Skalierung)*

---

## 🛠️ Technical Details

| Property | Value |
|----------|-------|
| **Engine** | Godot 4.5 |
| **Language** | C# / .NET 8 |
| **Platform** | Windows (Desktop) |
| **Version** | 0.0.1 |
| **Save Location** | Local JSON files |

### Installation

1. Download the release archive
2. Extract to any folder
3. Run `MySudoku.exe`

**No installation required – portable and ready to play!**

---

## 📸 Screenshots Used

| Filename | Section |
|----------|---------|
| `HomeScreen.png` | Quick Start, Main Screens |
| `SelectDifficulty.png` | Difficulty Selection |
| `9x9Ingame.png` | Playing the Game |
| `GridControls.png` | Controls Overview |
| `GridLabels.png` | Entering Numbers |
| `KidsIngame.png` | Kids Mode |
| `IngameHighlightDigitsRowsAndCols.png` | Selecting Cells |
| `IngameAddNote.png` | Notes & Candidates |
| `IngameShowNotes.png` | Notes & Candidates |
| `RowColBlockNoteToggle.png` | Auto-Notes |
| `IngameTipp1.png` | Hint System (Step 1) |
| `IngameTipp2.png` | Hint System (Step 2) |
| `IngameTipp3.png` | Hint System (Step 3) |
| `IngameTipp4.png` | Hint System (Step 4) |
| `Statistics.png` | Statistics & Progress |
| `History.png` | Game History |
| `Settings.png` | Settings |
| `TipsMenu.png` | Tips & Shortcuts |

---

*MySudoku v0.0.1 – Happy solving! 🧩*
