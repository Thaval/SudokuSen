# Refactor Report - MySudoku

## Top 10 Improvement Opportunities

### 1. **GameScene.cs - Monolithic Class (2365 lines)**
- **Impact**: High | **Risk**: Medium | **Effort**: High
- **Issue**: Single class handling UI, game logic, input, overlays, hints, notes, etc.
- **Improvement**: Extract nested `SudokuCellButton` class to separate file (already partial class pattern exists)
- **Status**: ✅ Completed - Extracted SudokuCellButton to separate file

### 2. **Repeated GetNode<ThemeService>/GetNode<SaveService> calls**
- **Impact**: Medium | **Risk**: Low | **Effort**: Low
- **Issue**: `GetNode<ThemeService>("/root/ThemeService")` called 60+ times throughout codebase
- **Improvement**: Cache service references in `_Ready()` for all UI files
- **Status**: ✅ Completed - All 9 UI files now cache services in `_Ready()`
  - GameScene.cs, SettingsMenu.cs, StatsMenu.cs, Main.cs, MainMenu.cs
  - DifficultyMenu.cs, HistoryMenu.cs, ScenariosMenu.cs, TipsMenu.cs

### 3. **ApplyTheme() method complexity (80+ lines)**
- **Impact**: Medium | **Risk**: Low | **Effort**: Medium
- **Issue**: Long method applying theme to multiple UI elements with repeated patterns
- **Improvement**: Extract helper methods: `ApplyButtonStyle()`
- **Status**: ✅ Completed - Extracted ApplyButtonStyle helper method

### 4. **Duplicated button styling code**
- **Impact**: Medium | **Risk**: Low | **Effort**: Low
- **Issue**: Same `AddThemeStyleboxOverride` pattern repeated for every button creation
- **Improvement**: Create `ApplyButtonStyle(Button button)` helper method
- **Status**: ✅ Completed - Added ApplyButtonStyle to GameScene

### 5. **_Input() method complexity (70+ lines)**
- **Impact**: Medium | **Risk**: Medium | **Effort**: Medium
- **Issue**: Long switch/if chains for keyboard input handling
- **Improvement**: Extract methods: `HandleNumberInput()`, `HandleNavigationInput()`, `HandleSpecialKeys()`
- **Status**: ✅ Completed - Extracted HandleKeyboardInput with guard clauses

### 6. **HintService - Repeated candidate calculation**
- **Impact**: Low | **Risk**: Low | **Effort**: Low
- **Issue**: `CalculateAllCandidates()` called 4 times in hint-finding methods
- **Improvement**: Calculate once and pass as parameter
- **Status**: ✅ Completed - Candidates calculated once in FindHint, passed to advanced techniques

### 7. **TipsMenu.cs - Static tip data (1180 lines)**
- **Impact**: Low | **Risk**: Low | **Effort**: Medium
- **Issue**: 26 static `CreateXxxTip()` methods with similar structure
- **Improvement**: Consider data-driven approach with JSON/resource file
- **Status**: ⏭️ Skipped - Would change architecture significantly, low ROI

### 8. **SudokuCellButton._Process() hot path**
- **Impact**: Medium | **Risk**: Low | **Effort**: Low
- **Issue**: `_Process` runs every frame even when not flashing
- **Improvement**: Disable processing when not needed via `SetProcess(false)`
- **Status**: ✅ Completed - Added SetProcess optimization

### 9. **UpdateGrid() - per-frame allocation potential**
- **Impact**: Low | **Risk**: Low | **Effort**: Low
- **Issue**: `CalculateCandidates()` allocates new `bool[9]` array each call
- **Improvement**: Use pooled/reusable arrays for candidates
- **Status**: ✅ Completed - Added reusable array pool

### 10. **Magic strings for node paths**
- **Impact**: Low | **Risk**: Low | **Effort**: Medium
- **Issue**: Hard-coded strings like `"VBoxContainer/HeaderMargin/Header"`
- **Improvement**: Use constants or [Export] node paths
- **Status**: ⏭️ Skipped - Would require scene changes, cosmetic improvement

---

## Changes Made

### Commit 1: Extract SudokuCellButton to separate file
- **What**: Moved `SudokuCellButton` nested class to `Scripts/UI/SudokuCellButton.cs`
- **Why safe**: Class was already self-contained, no external dependencies changed
- **Metrics**: GameScene.cs reduced from 2365 to ~2020 lines

### Commit 2: Cache service references (all UI files)
- **What**: Added `_themeService`, `_saveService`, `_appState` fields cached in `_Ready()` across all UI files
- **Why safe**: Same services accessed, just cached instead of lookup each time
- **Metrics**: Reduced ~60+ GetNode calls to 27 (only in _Ready methods and cross-service calls)
- **Files updated**:
  - GameScene.cs, SettingsMenu.cs, StatsMenu.cs, Main.cs, MainMenu.cs
  - DifficultyMenu.cs, HistoryMenu.cs, ScenariosMenu.cs, TipsMenu.cs

### Commit 3: Extract ApplyButtonStyle helper
- **What**: Created `ApplyButtonStyle(Button, bool includeDisabled)` private method
- **Why safe**: Pure refactor, same styling applied
- **Metrics**: Reduced code duplication across 12+ button styling sites

### Commit 4: Optimize SudokuCellButton._Process
- **What**: Added `SetProcess(false)` when not flashing, `SetProcess(true)` when flash starts
- **Why safe**: Same visual behavior, just skips empty _Process calls
- **Metrics**: 81 cells × 60fps = ~4860 fewer empty method calls per second

### Commit 5: Reusable candidate arrays
- **What**: Added `_candidatesPool` array reused in `CalculateCandidates()`
- **Why safe**: Array contents reset each call, no state leakage
- **Metrics**: Eliminated ~81 bool[9] allocations per UpdateGrid call

### Commit 6: Guard clauses and method extraction in _Input
- **What**: Early returns and extracted `TryHandleNumberKey()` helper
- **Why safe**: Same logic paths, just reorganized
- **Metrics**: Reduced cyclomatic complexity of _Input from ~15 to ~8

### Commit 7: HintService candidate calculation optimization
- **What**: Calculate `CalculateAllCandidates()` once in `FindHint()`, pass to advanced techniques
- **Why safe**: Candidates don't change during hint search; pure data passed between methods
- **Metrics**: Reduced redundant candidate calculations from 4 to 1 in worst case (X-Wing)
- **Methods updated**: FindNakedPair, FindPointingPair, FindBoxLineReduction, FindXWing

### Commit 8: Eliminate per-UpdateGrid allocations
- **What**: Use `_candidatesPool` in `CalculateCandidates()`, cache `_emptyCandidates4/9` arrays
- **Why safe**: Arrays reset/reused each call, same data semantics
- **Metrics**: Eliminated bool[] allocations in hot path (up to 82 arrays per UpdateGrid call)

### Commit 9: Remove unnecessary allocations and LINQ in TrySetNumberOnSelection
- **What**: Replaced `new HashSet<>` with conditional logic, removed redundant `.Last()` LINQ call
- **Why safe**: Same behavior through explicit if/else; loop already sets final values
- **Metrics**: Eliminated HashSet allocation on every notes-mode delete, removed LINQ iterator

### Commit 10: Optimize UpdateTimerDisplay to skip redundant updates
- **What**: Added `_lastTimerSecond` cache; only format and assign timer strings when second changes
- **Why safe**: Display only shows seconds resolution, so sub-second updates are invisible
- **Metrics**: Reduced timer string allocations from ~60/sec to 1/sec (59 fewer allocations per second)

---

## Threading Considerations

No threading changes were made in this refactor. The codebase correctly:
- Uses main thread for all Node/SceneTree operations
- No background tasks or async operations that touch UI
- All _Process callbacks are synchronous

If future async work is needed (e.g., puzzle generation), it should:
1. Run computation on Task.Run()
2. Marshal results back via `CallDeferred()` or signals
3. Never access Node properties from worker thread

---

## Validation

- ✅ Project builds without warnings
- ✅ All existing functionality preserved
- ✅ No scene (.tscn) files modified
- ✅ No public API changes
- ✅ No save data format changes

---

## Refactor Session - December 2025

### Overview
Performed code quality scan per software-quality.prompt.md. Focused on LINQ allocation removal in HintService.cs (cold path but good practice for pattern consistency).

### Changes Made

#### Commit 11: Replace LINQ .First() with GetSingleElement helper (HintService.cs)
- **What**: Added `GetSingleElement(HashSet<int>)` helper; replaced 5 occurrences of `remainingCands.First()`
- **Why safe**: Count==1 always verified before call; same value returned via enumerator
- **Metrics**: Eliminates LINQ iterator allocation per hint-finding pass
- **Methods updated**: FindNakedPair, FindPointingPair, FindBoxLineReduction, FindXWing

#### Commit 12: Replace LINQ .All() with explicit loops (HintService.cs)
- **What**: Added `AllSameRow(List<(int,int)>)` and `AllSameCol(List<(int,int)>)` helpers
- **Why safe**: Same predicate logic, explicit loop avoids closure allocation
- **Metrics**: Eliminates 2 closure allocations in FindPointingPair per hint search

#### Commit 13: Replace LINQ Select().ToList() with explicit loop (HintService.cs)
- **What**: Replaced `cols.Select(c => (row, c)).ToList()` with explicit foreach + Add
- **Why safe**: Same list contents, pre-sized for capacity
- **Metrics**: Eliminates closure + intermediate iterator allocation in FindBoxLineReduction

### Current State
- **Build**: ✅ Green (0 errors, 0 warnings)
- **Behavior**: ✅ Observationally equivalent
- **Threading**: ✅ All main-thread only

### Remaining Opportunities (Low Priority)
| # | File | Issue | Notes |
|---|------|-------|-------|
| 1 | UiNavigationSfx.cs | Stack allocation in EnumerateDescendants | Once per scene load, negligible |
| 2 | SettingsMenu.cs | 800+ lines, high complexity | Architecture change, defer |
| 3 | AudioService.cs | String interpolation in logs | Only affects debug overhead |

