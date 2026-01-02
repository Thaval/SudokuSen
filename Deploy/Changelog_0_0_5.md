# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.5] - 2026-01-02

### Added

- **Menu Icons**: All main menu entries now display emoji icons for better visual recognition
  - ▶️ Continue, 🆕 New Game, 📅 Daily, 🎯 Scenarios, 💡 Tips & Tutorials
  - 🧩 Puzzles, 📜 History, 📊 Statistics, ⚙️ Settings, 🚪 Quit
- **Solution Path Toggle**: Clicking the solution path button again now closes the overlay
- **Solution Path Detail Panel**: Clicking a solution step shows detailed explanation in a panel left of the grid
  - Panel positioned to align with the Back button
  - Automatically sizes to content
  - Displays human-friendly explanations
- **Human-Friendly Hidden Single Explanations**: Hints now show which placed numbers block other cells
  - Example: "6 can only go in A2 because the 6s at B6, C9, F3 block all other cells"
  - Works for row, column, and block-based Hidden Singles

### Changed

- Solution path entries are now clickable rows instead of hover-only tooltips
- Hidden Single hints provide more educational explanations referencing blocking cells

### Fixed

- **Localization**: Added 15 missing hint translation keys
- **Localization**: Removed duplicate `dialog.cancel` translation key
- **MainMenu**: Daily button no longer duplicates the 📅 icon

### Technical

- Added `BuildHumanFriendlyHiddenSingleExplanation()` helper in HintService
- Added `ToCellRef(row, col)` helper for "A1"-style cell references
- Added `_solutionPathDetailPanel` and `_solutionPathDetailLabel` fields in GameScene
- Added `_solutionPathDetailSelectedIndex` for tracking selected solution step
- Added `OnSolutionPathRowClicked()` and `UpdateSolutionPathDetailPanel()` methods

[0.0.5]: https://github.com/Thaval/SudokuSen/releases/tag/v0.0.5
