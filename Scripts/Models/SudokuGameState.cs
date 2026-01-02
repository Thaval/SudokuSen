using System.Text.Json.Serialization;

namespace SudokuSen.Models;

/// <summary>
/// Schwierigkeitsgrade
/// </summary>
public enum Difficulty
{
    Kids,       // 4x4 Grid (2x2 Blöcke)
    Easy,       // 9x9, einfach (nur Singles)
    Medium,     // 9x9, mittel (+ Pairs, Pointing Pairs)
    Hard,       // 9x9, schwer (+ X-Wing, Swordfish etc.)
    Insane  // 9x9, insane (alle Techniken, minimale Hinweise)
}

/// <summary>
/// Status eines Spiels
/// </summary>
public enum GameStatus
{
    InProgress,
    Won,
    Lost,
    Abandoned
}

/// <summary>
/// Repräsentiert den aktuellen Spielzustand
/// </summary>
public class SudokuGameState
{
    /// <summary>9x9 Grid (oder 4x4 für Kids-Modus)</summary>
    public SudokuCell[,] Grid { get; set; } = new SudokuCell[9, 9];

    /// <summary>Schwierigkeitsgrad</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Grid-Größe (9 für normal, 4 für Kids)</summary>
    public int GridSize => Difficulty == Difficulty.Kids ? 4 : 9;

    /// <summary>Block-Größe (3 für normal, 2 für Kids)</summary>
    public int BlockSize => Difficulty == Difficulty.Kids ? 2 : 3;

    /// <summary>Startzeitpunkt</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Verstrichene Zeit in Sekunden</summary>
    public double ElapsedSeconds { get; set; }

    /// <summary>Anzahl der Fehler</summary>
    public int Mistakes { get; set; }

    /// <summary>Deadly Modus aktiv</summary>
    public bool IsDeadlyMode { get; set; }

    /// <summary>Aktueller Status</summary>
    public GameStatus Status { get; set; }

    // Meta
    public bool IsDaily { get; set; } = false;
    public string? DailyDate { get; set; } // yyyy-MM-dd
    public bool IsScenario { get; set; } = false;
    public string? ScenarioTechnique { get; set; } // Technik-ID für Szenario-Spiele

    // Tutorial
    public bool IsTutorial { get; set; } = false;
    public string? TutorialId { get; set; }

    // Prebuilt puzzles
    public string? PrebuiltPuzzleId { get; set; }

    // Challenge Modes (snapshot at game start)
    public bool ChallengeNoNotes { get; set; } = false;
    public bool ChallengePerfectRun { get; set; } = false;
    public int ChallengeHintLimit { get; set; } = 0;
    public int ChallengeTimeAttackSeconds { get; set; } = 0;
    public int HintsUsed { get; set; } = 0;

    public SudokuGameState()
    {
        InitializeEmptyGrid();
    }

    private void InitializeEmptyGrid()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                Grid[row, col] = new SudokuCell();
            }
        }
    }

    /// <summary>
    /// Zählt wie oft eine Zahl im Grid vorkommt
    /// </summary>
    public int CountNumber(int number)
    {
        int count = 0;
        int size = GridSize;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (Grid[row, col].Value == number)
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Prüft ob das Grid vollständig und korrekt ist
    /// </summary>
    public bool IsComplete()
    {
        int size = GridSize;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (Grid[row, col].IsEmpty || !Grid[row, col].IsCorrect)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Prüft ob eine Zahl an einer Position gültig ist (Sudoku-Regeln)
    /// </summary>
    public bool IsValidPlacement(int row, int col, int number)
    {
        int size = GridSize;
        int blockSize = BlockSize;

        // Prüfe Zeile
        for (int c = 0; c < size; c++)
        {
            if (c != col && Grid[row, c].Value == number)
                return false;
        }

        // Prüfe Spalte
        for (int r = 0; r < size; r++)
        {
            if (r != row && Grid[r, col].Value == number)
                return false;
        }

        // Prüfe Block
        int blockRow = (row / blockSize) * blockSize;
        int blockCol = (col / blockSize) * blockSize;
        for (int r = blockRow; r < blockRow + blockSize; r++)
        {
            for (int c = blockCol; c < blockCol + blockSize; c++)
            {
                if ((r != row || c != col) && Grid[r, c].Value == number)
                    return false;
            }
        }

        return true;
    }

    public SudokuGameState Clone()
    {
        var clone = new SudokuGameState
        {
            Difficulty = Difficulty,
            StartTime = StartTime,
            ElapsedSeconds = ElapsedSeconds,
            Mistakes = Mistakes,
            IsDeadlyMode = IsDeadlyMode,
            Status = Status,
            IsDaily = IsDaily,
            DailyDate = DailyDate,
            IsScenario = IsScenario,
            ScenarioTechnique = ScenarioTechnique,
            IsTutorial = IsTutorial,
            TutorialId = TutorialId,
            PrebuiltPuzzleId = PrebuiltPuzzleId,
            ChallengeNoNotes = ChallengeNoNotes,
            ChallengePerfectRun = ChallengePerfectRun,
            ChallengeHintLimit = ChallengeHintLimit,
            ChallengeTimeAttackSeconds = ChallengeTimeAttackSeconds,
            HintsUsed = HintsUsed
        };

        int size = GridSize;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                clone.Grid[row, col] = Grid[row, col].Clone();
            }
        }

        return clone;
    }
}
