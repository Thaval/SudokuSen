# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.3] - 2025-12-31

### Added

- **Complete Tutorial System**: 5 interactive tutorials guiding players from basics to advanced techniques
  - Tutorial 1: Getting Started - Cell selection, number entry, and basic controls
  - Tutorial 2: Using Notes - Candidate notation and note management
  - Tutorial 3: Basic Techniques - Naked Singles and Hidden Singles
  - Tutorial 4: Intermediate Techniques - Naked Pairs and Pointing Pairs
  - Tutorial 5: Challenge Modes - Time Attack, Perfect Run, Hint Limit, and No Notes
- **Scenario/Practice Mode**: Practice specific Sudoku techniques with focused puzzles
- **Separate Statistics for Scenarios**: Dedicated statistics section for practice scenarios
- **Tutorial/Scenario Badges in History**: Visual indicators (📚/🎯/📅) in game history
- **Grid Input Protection During Tutorials**: Grid is locked during tutorials unless specifically waiting for input
- **IsScenario Property**: Proper tracking of scenario games in game state

### Changed

- Tutorials and scenarios now appear in history but are excluded from main statistics
- Scenario games no longer overwrite regular save files
- Improved Clone() method to include all tutorial/scenario properties

### Fixed

- **Critical**: Tutorial/scenario games no longer overwrite regular save files
- **Critical**: Corrupted save files with invalid cell coordinates no longer crash the game
- **Race Condition**: Tutorial start timer now safely handles scene changes
- **Race Condition**: Tutorial delayed step timer now validates state before execution
- **Bounds Check**: HintService now properly rejects non-9x9 grids (Kids mode)

### Technical

- Added `IsScenario` property to `SudokuGameState`
- Added `IsGridInputAllowed` property to `TutorialService`
- Added bounds validation in `SaveService.ToGameState()`
- Improved timer callback safety with captured variables and state validation

[0.0.3]: https://github.com/Thaval/SudokuSen/releases/tag/v0.0.3
