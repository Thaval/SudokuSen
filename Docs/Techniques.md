# Sudoku Solving Techniques

A comprehensive guide to all Sudoku solving techniques, categorized by difficulty.

---

## Table of Contents

1. [Easy Techniques](#easy-techniques)
   - [Naked Single](#naked-single)
   - [Hidden Single](#hidden-single)
2. [Medium Techniques](#medium-techniques)
   - [Naked Pair](#naked-pair)
   - [Naked Triple](#naked-triple)
   - [Naked Quad](#naked-quad)
   - [Hidden Pair](#hidden-pair)
   - [Hidden Triple](#hidden-triple)
   - [Pointing Pair/Triple](#pointing-pairtriple)
   - [Box/Line Reduction](#boxline-reduction)
3. [Hard Techniques](#hard-techniques)
   - [X-Wing](#x-wing)
   - [Swordfish](#swordfish)
   - [Jellyfish](#jellyfish)
   - [XY-Wing](#xy-wing)
   - [XYZ-Wing](#xyz-wing)
   - [W-Wing](#w-wing)
   - [Skyscraper](#skyscraper)
   - [2-String Kite](#2-string-kite)
   - [Empty Rectangle](#empty-rectangle)
   - [Simple Coloring](#simple-coloring)

---

## Easy Techniques

These techniques are sufficient to solve most easy puzzles and form the foundation of Sudoku solving.

### Naked Single

**Also known as:** Sole Candidate, Forced Digit

**Difficulty:** Easy

**Description:**
A cell has only one possible candidate because all other digits (1-9) are already present in its row, column, or 3×3 box.

**How to detect:**
1. Select an empty cell
2. Check which numbers 1-9 are already in the same row
3. Check which numbers 1-9 are already in the same column
4. Check which numbers 1-9 are already in the same 3×3 box
5. If only ONE number is not present in any of these, that's the Naked Single

**How to apply:**
Place the only remaining candidate in the cell.

**Example:**
```
Row has: 1, 2, 3, 4, 5, 7, 8, 9
Column has: 1, 3, 6
Box has: 2, 4, 5, 8

Combined exclusions: 1, 2, 3, 4, 5, 6, 7, 8, 9 except 6? No...
Let's recalculate: Missing from all = only 6 remains
→ Cell must be 6
```

**Visual Pattern:**
```
┌───────┬───────┬───────┐
│ 1 2 3 │ . . . │ . . . │
│ 4 [?] 6 │ . . . │ . . . │  ← Cell [?] sees 1,2,3,4,6,7,8,9
│ 7 8 9 │ . . . │ . . . │     Only 5 is missing → [?] = 5
└───────┴───────┴───────┘
```

---

### Hidden Single

**Also known as:** Unique Candidate, Pinned Digit

**Difficulty:** Easy

**Description:**
A digit can only go in one cell within a row, column, or 3×3 box, even though that cell might have other candidates.

**Variants:**
- **Hidden Single in Row:** A digit can only go in one cell of a row
- **Hidden Single in Column:** A digit can only go in one cell of a column
- **Hidden Single in Box:** A digit can only go in one cell of a 3×3 box

**How to detect:**
1. Pick a digit (1-9)
2. Pick a unit (row, column, or box)
3. Find all cells in that unit where this digit could go
4. If only ONE cell can contain this digit, it's a Hidden Single

**How to apply:**
Place the digit in the only cell where it can go.

**Example (Hidden Single in Box):**
```
┌───────────────────┐
│ 5  .  . │         │  In this box, where can 7 go?
│ .  .  7 │  →      │  Cell (1,2) has 7 in its column
│ .  7  . │         │  Cell (2,1) has 7 in its row
└─────────┘         │  Only cell (0,1) or (0,2) remain
                    │  If (0,2) sees a 7 in its column → (0,1) = 7
```

**Visual Pattern:**
```
Box analysis for digit 7:
┌───────┐
│ . ✓ X │  X = blocked by column
│ X . . │  X = blocked by row
│ . . . │  ✓ = only place for 7
└───────┘
```

---

## Medium Techniques

These techniques require tracking candidates (pencil marks) and involve eliminating possibilities.

### Naked Pair

**Also known as:** Conjugate Pair, Obvious Pair

**Difficulty:** Medium

**Description:**
Two cells in the same unit (row, column, or box) contain only the same two candidates. These two numbers can be eliminated from all other cells in that unit.

**How to detect:**
1. Find two cells in the same unit
2. Both cells must have exactly 2 candidates
3. Both cells must have the SAME 2 candidates (e.g., both have {3,7})

**How to apply:**
Remove those two candidates from all OTHER cells in the same unit.

**Example:**
```
Row: [37] [37] [1379] [139] [1359] ...
      ↑    ↑
    Naked Pair on {3,7}

Result: Remove 3 and 7 from cells 3, 4, 5...
Row: [37] [37] [19] [19] [159] ...
```

**Why it works:**
The two cells MUST contain 3 and 7 (one each). So no other cell in the unit can have 3 or 7.

---

### Naked Triple

**Also known as:** Obvious Triple

**Difficulty:** Medium

**Description:**
Three cells in a unit contain only candidates from a set of three numbers. The three cells don't all need to have all three candidates – they just can't have any candidates outside the set of three.

**How to detect:**
1. Find three cells in the same unit
2. Together they contain AT MOST 3 different candidates
3. Valid patterns: {12}{23}{13}, {123}{12}{23}, {123}{123}{123}

**How to apply:**
Remove those three candidates from all other cells in the unit.

**Example:**
```
Cell A: {1,2}
Cell B: {2,3}
Cell C: {1,3}

These form a Naked Triple on {1,2,3}
→ Remove 1, 2, 3 from all other cells in the unit
```

---

### Naked Quad

**Difficulty:** Medium

**Description:**
Four cells in a unit contain only candidates from a set of four numbers.

**How to detect:**
Same as Naked Triple, but with 4 cells and 4 candidates.

**How to apply:**
Remove those four candidates from all other cells in the unit.

---

### Hidden Pair

**Also known as:** Unique Pair

**Difficulty:** Medium

**Description:**
Two candidates appear in only two cells within a unit. All other candidates can be removed from those two cells.

**How to detect:**
1. In a unit, find two candidates that appear in exactly the same two cells
2. These two cells may have other candidates too

**How to apply:**
Remove all OTHER candidates from those two cells, leaving only the pair.

**Example:**
```
Before:
Cell A: {1,2,5,7,9}  ← contains 2 and 7
Cell B: {2,3,7,8}    ← contains 2 and 7
Cell C: {1,3,5,8,9}  ← no 2 or 7
Cell D: {1,3,5,9}    ← no 2 or 7

2 and 7 only appear in cells A and B → Hidden Pair!

After:
Cell A: {2,7}  ← reduced to just the pair
Cell B: {2,7}  ← reduced to just the pair
```

---

### Hidden Triple

**Difficulty:** Medium

**Description:**
Three candidates appear in only three cells within a unit. All other candidates can be removed from those three cells.

**How to detect:**
1. Find three candidates that appear ONLY in three specific cells
2. Other candidates may be present in those cells

**How to apply:**
Remove all other candidates from those three cells.

---

### Pointing Pair/Triple

**Also known as:** Locked Candidates Type 1, Box/Line Intersection

**Difficulty:** Medium

**Description:**
When a candidate in a box is restricted to a single row or column, that candidate can be eliminated from that row/column outside the box.

**How to detect:**
1. In a 3×3 box, find a candidate
2. Check if that candidate appears only in one row (or column) within the box
3. If yes, it's a Pointing Pair/Triple

**How to apply:**
Remove that candidate from the rest of the row/column (outside the box).

**Example:**
```
┌─────────────┬─────────────┬─────────────┐
│  .   .   .  │  .   .   .  │  .   .   .  │
│  .   .   .  │  .   .   .  │  .   .   .  │
│ [5] [5]  .  │  5   .   5  │  .   5   .  │ ← Row 3
└─────────────┴─────────────┴─────────────┘
     Box 1         Box 2         Box 3

In Box 1, the 5s are only in row 3 (Pointing Pair)
→ Remove 5 from row 3 in Box 2 and Box 3
```

---

### Box/Line Reduction

**Also known as:** Locked Candidates Type 2, Line/Box Intersection

**Difficulty:** Medium

**Description:**
When a candidate in a row/column is restricted to a single box, that candidate can be eliminated from the rest of that box.

**How to detect:**
1. In a row or column, find a candidate
2. Check if that candidate appears only within one box
3. If yes, it's a Box/Line Reduction

**How to apply:**
Remove that candidate from the rest of the box (cells not in the original row/column).

**Example:**
```
┌─────────────┐
│  .   .   .  │
│  .  [3]  .  │ ← In this row, 3 only appears in Box 1
│  .  [3]  .  │
├─────────────┤
│  3   .   3  │ ← These 3s can be eliminated
│  .   .   .  │
│  .   .   .  │
└─────────────┘
```

---

## Hard Techniques

These are advanced techniques that require pattern recognition across multiple units.

### X-Wing

**Difficulty:** Hard

**Description:**
When a candidate appears in exactly two cells in each of two rows, AND these cells are in the same two columns, the candidate can be eliminated from those columns (in other rows).

**How to detect:**
1. Find a candidate that appears exactly twice in a row
2. Find another row where the same candidate appears exactly twice in the SAME columns
3. This forms a rectangle pattern

**How to apply:**
Eliminate the candidate from the two columns, except in the four X-Wing cells.

**Example:**
```
       Col A    Col B
Row 1:  [7]      [7]     ← 7 only in cols A and B
Row 2:   .        .
Row 3:   7        7      ← Can eliminate these!
Row 4:  [7]      [7]     ← 7 only in cols A and B
Row 5:   7        .      ← Can eliminate this!

The [7]s form an X-Wing → Remove 7 from cols A,B in rows 2,3,5
```

**Why it works:**
In the X-Wing rectangle, the 7s must be placed diagonally (either top-left + bottom-right, OR top-right + bottom-left). Either way, both columns are "covered."

---

### Swordfish

**Difficulty:** Hard

**Description:**
An extension of X-Wing to three rows and three columns. When a candidate appears 2-3 times in each of three rows, and all occurrences are confined to the same three columns, eliminations can be made.

**How to detect:**
1. Find three rows where a candidate appears 2-3 times each
2. All occurrences must be in the same three columns
3. Each column must have at least 2 of the 6-9 cells

**How to apply:**
Eliminate the candidate from the three columns, except in the Swordfish cells.

**Example:**
```
       Col A   Col B   Col C
Row 1:  [4]     [4]      .
Row 2:   4       .       4    ← Eliminate
Row 3:  [4]      .      [4]
Row 4:   .       4       .    ← Eliminate
Row 5:   .      [4]     [4]
```

---

### Jellyfish

**Difficulty:** Hard

**Description:**
Extension of Swordfish to four rows and four columns.

**How to detect & apply:**
Same logic as Swordfish, but with 4 rows and 4 columns.

---

### XY-Wing

**Also known as:** Y-Wing, Bent Triple

**Difficulty:** Hard

**Description:**
Three cells with two candidates each, forming a "Y" pattern. One cell (pivot) sees two other cells (pincers). The pivot shares one candidate with each pincer.

**Structure:**
- Pivot: {A,B}
- Pincer 1: {A,C} - sees pivot
- Pincer 2: {B,C} - sees pivot
- Pincers don't need to see each other

**How to detect:**
1. Find a cell with exactly 2 candidates {A,B} (pivot)
2. Find a cell it sees with candidates {A,C}
3. Find another cell it sees with candidates {B,C}
4. C is the common elimination candidate

**How to apply:**
Eliminate C from all cells that see BOTH pincers.

**Example:**
```
Pivot at (1,1): {3,7}
Pincer 1 at (1,5): {3,9}  ← shares 3 with pivot
Pincer 2 at (4,1): {7,9}  ← shares 7 with pivot

Common candidate: 9
→ Eliminate 9 from cells that see both (1,5) and (4,1)
```

**Why it works:**
- If pivot = 3 → Pincer 2 = 9
- If pivot = 7 → Pincer 1 = 9
- Either way, one pincer is 9, so cells seeing both can't be 9

---

### XYZ-Wing

**Difficulty:** Hard

**Description:**
Similar to XY-Wing, but the pivot has three candidates {A,B,C}.

**Structure:**
- Pivot: {A,B,C}
- Pincer 1: {A,C}
- Pincer 2: {B,C}

**How to apply:**
Eliminate C from cells that see ALL THREE cells (pivot and both pincers).

---

### W-Wing

**Difficulty:** Hard

**Description:**
Two cells with the same two candidates {A,B} connected by a strong link on one of the candidates.

**How to detect:**
1. Find two cells with identical candidates {A,B}
2. These cells don't see each other
3. There's a strong link (conjugate pair) on candidate A connecting them

**How to apply:**
Eliminate B from cells that see both {A,B} cells.

---

### Skyscraper

**Difficulty:** Hard

**Description:**
Two conjugate pairs on the same candidate, sharing one end point in a row/column.

**How to detect:**
1. Find two columns where a candidate appears exactly twice
2. One cell from each column is in the same row (the "base")
3. The other two cells are the "tops"

**How to apply:**
Eliminate the candidate from cells that see both "top" cells.

**Example:**
```
     Col A   Col B
Row 1: [7]     .
Row 2:  .      .
Row 3: [7]    [7]    ← Base (same row)
Row 4:  .     [7]
       ↑       ↑
    Tops: (1,A) and (4,B)

→ Eliminate 7 from cells seeing both (1,A) and (4,B)
```

---

### 2-String Kite

**Difficulty:** Hard

**Description:**
A candidate forms a conjugate pair in a row AND a column, with the pairs sharing a cell in a box.

**How to detect:**
1. Find a row where candidate X appears exactly twice
2. Find a column where X appears exactly twice
3. One cell is common to both (in the same box)
4. This creates a "kite" pattern

**How to apply:**
Eliminate X from cells that see both "ends" of the kite.

---

### Empty Rectangle

**Difficulty:** Hard

**Description:**
In a box, a candidate forms an "L" shape (empty rectangle), intersecting with a conjugate pair.

**How to detect:**
1. In a box, find a candidate that appears only in an L-shape (one row + one column within the box)
2. The intersection cell is the "hinge"
3. Find a conjugate pair on the same candidate in a row/column through one arm

**How to apply:**
Eliminate the candidate from the cell at the other end of the pattern.

---

### Simple Coloring

**Also known as:** Single's Chains

**Difficulty:** Hard

**Description:**
Using the "either/or" nature of conjugate pairs to color cells and find contradictions or eliminations.

**How to apply:**
1. Pick a candidate and find all its conjugate pairs
2. Color cells alternately (if A is blue, its conjugate pair partner is green)
3. **Rule 1 (Color Trap):** If an uncolored cell sees both colors, eliminate the candidate
4. **Rule 2 (Color Wrap):** If two cells of the same color see each other, that color is false

---

## Technique Summary Table

| Technique | Difficulty | Type | Brief Description |
|-----------|------------|------|-------------------|
| Naked Single | Easy | Placement | Only one candidate in cell |
| Hidden Single | Easy | Placement | Candidate unique in unit |
| Naked Pair | Medium | Elimination | Two cells, same two candidates |
| Naked Triple | Medium | Elimination | Three cells, three candidates |
| Naked Quad | Medium | Elimination | Four cells, four candidates |
| Hidden Pair | Medium | Elimination | Two candidates in only two cells |
| Hidden Triple | Medium | Elimination | Three candidates in only three cells |
| Pointing Pair | Medium | Elimination | Box restricts to row/column |
| Box/Line Reduction | Medium | Elimination | Row/column restricts to box |
| X-Wing | Hard | Elimination | 2×2 rectangle pattern |
| Swordfish | Hard | Elimination | 3×3 pattern |
| Jellyfish | Hard | Elimination | 4×4 pattern |
| XY-Wing | Hard | Elimination | Y-shaped three-cell pattern |
| XYZ-Wing | Hard | Elimination | XY-Wing with 3-candidate pivot |
| W-Wing | Hard | Elimination | Two bi-value cells + strong link |
| Skyscraper | Hard | Elimination | Two conjugate pairs, shared base |
| 2-String Kite | Hard | Elimination | Row + column pairs in box |
| Empty Rectangle | Hard | Elimination | L-shape in box + conjugate pair |
| Simple Coloring | Hard | Elimination | Chain coloring contradictions |

---

## Implementation Notes

### For Puzzle Generation
- **Easy puzzles:** Only require Naked Single + Hidden Single to solve
- **Medium puzzles:** May require techniques up to Box/Line Reduction
- **Hard puzzles:** May require any technique including X-Wing, Swordfish, XY-Wing

### For Hint System
Each technique should provide:
1. **Detection:** Which cells/candidates are involved
2. **Explanation:** Why the technique applies
3. **Result:** What can be placed or eliminated
4. **Visualization:** Highlight relevant cells

### For Settings
Allow users to configure which techniques are used per difficulty:
- Checkboxes for each technique
- Grouped by difficulty category
- Changes affect puzzle generation
