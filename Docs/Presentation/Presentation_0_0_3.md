# 🧩 SudokuSen v0.0.3 – User Guide

**A modern Sudoku game for Desktop – built with Godot 4 & C#**

SudokuSen offers an elegant, beginner-friendly Sudoku experience with multiple difficulty levels, intelligent hints that teach you solving techniques, and comprehensive statistics to track your progress. Whether you're a complete beginner or a seasoned puzzle solver, SudokuSen adapts to your skill level.

> **Release:** December 31, 2025

---

## 📑 Table of Contents

1. [What's New in v0.0.3](#whats-new-in-v003)
2. [Quick Start](#quick-start)
3. [Interactive Tutorials](#interactive-tutorials)
4. [Practice Scenarios](#practice-scenarios)
5. [Playing the Game](#playing-the-game)
6. [Notes & Candidates](#notes--candidates)
7. [The Hint System](#the-hint-system)
8. [Daily Sudoku & Streaks](#daily-sudoku--streaks)
9. [Challenge Modes](#challenge-modes)
10. [Statistics & Progress](#statistics--progress)
11. [Settings](#settings)
12. [Tips & Shortcuts](#tips--shortcuts)
13. [FAQ / Troubleshooting](#faq--troubleshooting)
14. [Technical Details](#technical-details)

---

## 🆕 What's New in v0.0.3

### 📚 Complete Tutorial System

Five interactive tutorials guide you from complete beginner to advanced techniques:

| Tutorial | Topic | What You'll Learn |
| -------- | ----- | ----------------- |
| 1 | Getting Started | Cell selection, number entry, basic controls |
| 2 | Using Notes | Candidate notation and note management |
| 3 | Basic Techniques | Naked Singles and Hidden Singles |
| 4 | Intermediate Techniques | Naked Pairs and Pointing Pairs |
| 5 | Challenge Modes | Time Attack, Perfect Run, Hint Limit, No Notes |

Each tutorial is fully interactive – the grid is protected so you can't accidentally make mistakes while learning!

### 🎯 Practice Scenarios

New "Szenarien" menu lets you practice specific Sudoku techniques:

- Choose a technique to focus on (Naked Single, Hidden Single, Naked Pair, etc.)
- Play puzzles designed to showcase that technique
- Track your scenario statistics separately from regular games
- See visual badges in history: 📚 Tutorial, 🎯 Scenario, 📅 Daily

### 📊 Separate Scenario Statistics

- Tutorials and scenarios appear in your game history
- Main statistics only count regular games
- New dedicated section shows scenario performance by technique
- Track your improvement in each solving technique

### 🛡️ Grid Protection During Tutorials

- Grid input is automatically blocked during tutorial explanations
- Only enabled when the tutorial specifically asks for input
- Prevents accidental cell changes while learning

### 🐛 Bug Fixes

- Fixed: Tutorial/scenario games no longer overwrite your save file
- Fixed: Corrupted save files no longer crash the game
- Fixed: Timer race conditions in tutorial system
- Fixed: Hint system properly rejects Kids mode (4×4 grids)

---

## 🚀 Quick Start

Get playing in under a minute:

1. **Launch SudokuSen** – Double-click `SudokuSen.exe`
2. **New to Sudoku?** Click "Szenarien" → "📚 Tutorials" to start learning
3. **Ready to play?** Click "Neues Spiel" (New Game) and select a difficulty
4. **Click a cell** to select it
5. **Enter a number** using the number pad or keyboard (1-9)
6. **Stuck?** Press the 💡 hint button for guidance
7. **Complete the puzzle** – Fill all cells correctly to win!

---

## 📚 Interactive Tutorials

Access tutorials from the main menu: **Szenarien** → **📚 Tutorials**

### Tutorial 1: Getting Started

Learn the absolute basics:
- How to select cells with mouse or keyboard
- Entering numbers with the number pad or keyboard
- Deleting numbers with eraser, Delete, or Backspace
- Understanding given cells vs. empty cells

### Tutorial 2: Using Notes

Master the candidate notation system:
- Toggle notes mode with the ✏️ button or N key
- Add/remove note candidates
- Multi-select cells to add notes in bulk
- Use Shift+Click for range selection
- Clear notes with the eraser

### Tutorial 3: Basic Techniques

Learn your first solving techniques:
- **Naked Single**: When a cell has only one possible candidate
- **Hidden Single**: When a number can only go in one place in a row/column/block
- Practice identifying these patterns in real puzzles

### Tutorial 4: Intermediate Techniques

Build on the basics:
- **Naked Pairs**: Two cells with the same two candidates
- **Pointing Pairs**: Candidates aligned in a block pointing to eliminations
- Use the hint system to find these patterns

### Tutorial 5: Challenge Modes

Push your skills to the limit:
- **⏱️ Time Attack**: Race against the clock
- **🎯 Perfect Run**: No mistakes allowed
- **💡 Hint Limit**: Limited hints per game
- **📝 No Notes**: Pure mental calculation

---

## 🎯 Practice Scenarios

Access scenarios from the main menu: **Szenarien** → Select a technique

Practice specific techniques with targeted puzzles:

| Difficulty | Available Techniques |
| ---------- | -------------------- |
| Leicht (Easy) | Naked Single, Hidden Single |
| Mittel (Medium) | Naked Pair, Pointing Pair, Box/Line Reduction |
| Schwer (Hard) | X-Wing, Swordfish, XY-Wing |

**How Scenarios Work:**
1. Select a technique you want to practice
2. A puzzle requiring that technique is generated
3. Use hints to learn how to apply the technique
4. Your scenario games appear in history with the 🎯 badge
5. View scenario-specific statistics in the Stats menu

**Note:** Scenario games don't affect your main statistics – practice freely without worry!

---

## 🎮 Playing the Game

### The Game Screen

The game screen shows:

- **Timer** (top) – Tracks your solving time
- **Error counter** – Shows mistakes made (important in Deadly Mode!)
- **The Sudoku grid** – Your puzzle
- **Control buttons** – Notes, hints, and special features
- **Number pad** – For entering digits

---

### Controls Overview

| Control | Function |
| ------- | -------- |
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

---

## ✏️ Notes & Candidates

Notes (also called "pencil marks" or "candidates") help you track which numbers could go in each cell.

### Adding Notes

1. Click the **✏️ Notes** button to enter notes mode (button shows as active)
2. Click a cell
3. Click numbers to toggle them as candidates
4. Click ✏️ again to exit notes mode

### Quick Note Entry

- Press **N** to toggle notes mode
- Use **Shift+Click** to select a range of cells
- Enter a number to add/remove that candidate from all selected cells

### Auto-Notes

Use the **R/C/B** buttons to automatically fill candidates:
- **R** – Fill valid candidates for the selected Row
- **C** – Fill valid candidates for the selected Column
- **B** – Fill valid candidates for the selected Block

---

## 💡 The Hint System

When you're stuck, press the **💡 Hint** button (or H key) for intelligent assistance.

### How Hints Work

1. The system analyzes the puzzle for the easiest next move
2. It identifies which technique solves the cell
3. It highlights relevant cells and explains the logic
4. You can accept the hint to fill the cell, or try to solve it yourself

### Hint Techniques (by difficulty)

| Level | Techniques |
| ----- | ---------- |
| Easy | Naked Single, Hidden Single |
| Medium | Naked Pair, Pointing Pair, Box/Line Reduction |
| Hard | X-Wing, Swordfish, XY-Wing |

**Tip:** Use hints to learn new techniques! The explanations teach you the logic.

---

## 📅 Daily Sudoku & Streaks

Play the **Daily Sudoku** from the main menu for a consistent challenge:

- Same puzzle for everyone on the same day
- Difficulty rotates: Easy → Medium → Hard → Easy...
- Build streaks by completing daily puzzles on consecutive days
- Track your longest streak in Statistics

---

## 🏆 Challenge Modes

Enable challenge modes in **Settings** → **Challenge-Modi**:

| Mode | Description |
| ---- | ----------- |
| **⏱️ Time Attack** | Complete the puzzle before time runs out |
| **🎯 Perfect Run** | One mistake = game over |
| **💡 Hint Limit** | Limited number of hints available |
| **📝 No Notes** | Notes feature is disabled |

Combine multiple challenges for the ultimate test!

---

## 📊 Statistics & Progress

View your statistics from the main menu → **Statistik**

### Main Statistics

- Total games played and completed
- Win rate percentage
- Average completion time
- Best time per difficulty
- Current and longest daily streak
- Mistake heatmap showing problem areas

### Scenario Statistics

New in v0.0.3! A dedicated section shows:

- Scenario games by technique
- Win rate for each technique
- Best times for scenario puzzles
- Practice progress tracking

**Note:** Tutorials and scenarios are shown in history but excluded from main statistics.

---

## ⚙️ Settings

Access settings from the main menu → **Einstellungen**

### Appearance

- **Theme**: Light or Dark mode
- **UI Scale**: Adjust interface size (adapts to your screen resolution)

### Gameplay

- **Deadly Mode**: 3 mistakes = game over
- **Number Highlighting**: Highlight matching numbers across the grid
- **Highlight Errors**: Show incorrect entries in red
- **Enable Techniques**: Customize which solving techniques appear in puzzles

### Challenge Modes

- **Challenge Difficulty**: Auto, Easy, Medium, or Hard
- **No Notes**: Disable notes feature
- **Perfect Run**: One mistake ends the game
- **Hint Limit**: Maximum hints per game (0 = unlimited)
- **Time Attack**: Time limit in minutes (0 = no limit)

---

## ⌨️ Tips & Shortcuts

### Keyboard Shortcuts

| Key | Action |
| --- | ------ |
| **1-9** | Enter number |
| **0, Del, Backspace** | Delete number/clear notes |
| **N** | Toggle notes mode |
| **H** | Request hint |
| **Arrow keys** | Navigate cells |
| **Ctrl+Click** | Multi-select cells |
| **Shift+Click** | Select cell range |
| **Escape** | Clear selection / Go back |

### Pro Tips

1. **Start with scanning**: Look for rows/columns/blocks missing few numbers
2. **Use notes liberally**: Track all candidates to spot patterns
3. **Learn the techniques**: Each hint explains how to find the solution
4. **Practice with scenarios**: Focus on specific techniques to improve
5. **Use the tutorials**: Even experienced players can learn new patterns

---

## ❓ FAQ / Troubleshooting

**Q: The game runs slowly**
A: Try reducing the UI scale in Settings

**Q: I can't enter numbers in a cell**
A: Check if the cell is "given" (starting number) – those can't be changed

**Q: My save game was lost**
A: Tutorial and scenario games don't overwrite your main save. Check if you were playing a tutorial.

**Q: Hints aren't available**
A: Hints are disabled for 4×4 Kids mode puzzles

**Q: Where are my scenario statistics?**
A: Scroll down in the Statistics screen to see the dedicated Scenario section

---

## 🔧 Technical Details

- **Engine**: Godot 4.5.1 with C# / .NET 8
- **Platform**: Windows (64-bit)
- **Save Location**: `%APPDATA%\Godot\app_userdata\SudokuSen\`
- **Languages**: German UI (English planned for future release)

---

*Happy puzzling! 🧩*
