# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.2] - 2024-12-31

### Added

- **Challenge Difficulty Selection**: New setting to choose difficulty for challenge modes (Auto/Easy/Medium/Hard)
- **Recommended Difficulty System**: Auto mode calculates recommended difficulty based on player's history (win rate and average mistakes)
- **Tooltips on all UI elements**: Helpful descriptions for all buttons and settings throughout the game
- **Dynamic UI Scale Bounds**: UI scaling now adapts to screen resolution with proper min/max limits
- **Background Click to Clear Selection**: Click outside the grid to deselect cells

### Changed

- Improved Settings Menu scrolling behavior
- UI scale slider now shows valid range based on screen resolution
- Challenge modes can now skip difficulty selection when a specific difficulty is set

### Fixed

- Settings panel now properly scrolls on smaller screens
- UI scale no longer allows values outside supported range

## [0.0.1] - 2024-12-29

### Added

- Multiple difficulty levels: Kids (4×4 grid), Easy, Medium, Hard (9×9 grid)
- Classic Sudoku game mode
- Daily Sudoku with streak tracking
- Deadly Mode (3 mistakes = game over)
- Challenge modes: No Notes, Perfect Run, Hint Limit, Time Attack
- Smart hint system with step-by-step explanations
- Multiple solving techniques support for hints
- Learn mode for beginners
- Manual note-taking system
- Auto-candidates feature
- Smart note cleanup
- House auto-fill (Row/Column/Block)
- Light and Dark theme options
- Colorblind-friendly palette
- UI scaling options
- Sound effects and music with volume controls
- Game history tracking
- Statistics per difficulty level
- Mistake heatmap visualization
- Technique progression tracking
- Clean, modern user interface
- Full keyboard and mouse support
- Row/column/block highlighting
- Automatic conflict detection
- Menu and game music tracks
- Sound effects for all interactions

[0.0.2]: https://github.com/Thaval/SudokuSen/releases/tag/v0.0.2
[0.0.1]: https://github.com/Thaval/SudokuSen/releases/tag/v0.0.1
