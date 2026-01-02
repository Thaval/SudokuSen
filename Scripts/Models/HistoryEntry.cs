using SudokuSen.Services;

namespace SudokuSen.Models;

/// <summary>
/// Ein Eintrag im Spielverlauf
/// </summary>
public class HistoryEntry
{
    /// <summary>Eindeutige ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Datum und Uhrzeit des Spielstarts</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Datum und Uhrzeit des Spielendes</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Schwierigkeitsgrad</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Spieldauer in Sekunden</summary>
    public double DurationSeconds { get; set; }

    /// <summary>Anzahl der Fehler</summary>
    public int Mistakes { get; set; }

    /// <summary>Spielstatus</summary>
    public GameStatus Status { get; set; }

    /// <summary>War Deadly Mode aktiv</summary>
    public bool WasDeadlyMode { get; set; }

    /// <summary>War es ein Daily-Sudoku?</summary>
    public bool IsDaily { get; set; }

    /// <summary>Daily-Datum (yyyy-MM-dd) wenn IsDaily</summary>
    public string? DailyDate { get; set; }

    /// <summary>War es ein Tutorial?</summary>
    public bool IsTutorial { get; set; }

    /// <summary>Tutorial-ID wenn IsTutorial</summary>
    public string? TutorialId { get; set; }

    /// <summary>War es ein Szenario/Übungspuzzle?</summary>
    public bool IsScenario { get; set; }

    /// <summary>Technik-ID wenn IsScenario</summary>
    public string? ScenarioTechnique { get; set; }

    /// <summary>Referenz auf vorgefertigtes Puzzle (falls verwendet)</summary>
    public string? PrebuiltPuzzleId { get; set; }

    // Puzzle reconstruction for history replay
    public List<int> SolutionDigits { get; set; } = new(); // flattened row-major, 81 entries for 9x9 or 16 for 4x4
    public List<bool> Givens { get; set; } = new();        // same length as SolutionDigits

    /// <summary>
    /// Erstellt einen HistoryEntry aus einem GameState
    /// </summary>
    public static HistoryEntry FromGameState(SudokuGameState state, GameStatus finalStatus)
    {
        return new HistoryEntry
        {
            StartTime = state.StartTime,
            EndTime = DateTime.Now,
            Difficulty = state.Difficulty,
            DurationSeconds = state.ElapsedSeconds,
            Mistakes = state.Mistakes,
            Status = finalStatus,
            WasDeadlyMode = state.IsDeadlyMode,
            IsDaily = state.IsDaily,
            DailyDate = state.DailyDate,
            IsTutorial = state.IsTutorial,
            TutorialId = state.TutorialId,
            IsScenario = state.IsScenario || !string.IsNullOrEmpty(state.ScenarioTechnique),
            ScenarioTechnique = state.ScenarioTechnique,
            PrebuiltPuzzleId = state.PrebuiltPuzzleId,
            SolutionDigits = FlattenSolutions(state),
            Givens = FlattenGivens(state)
        };
    }

    private static List<int> FlattenSolutions(SudokuGameState state)
    {
        var list = new List<int>(state.GridSize * state.GridSize);
        int size = state.GridSize;
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                list.Add(state.Grid[r, c].Solution);
            }
        }
        return list;
    }

    private static List<bool> FlattenGivens(SudokuGameState state)
    {
        var list = new List<bool>(state.GridSize * state.GridSize);
        int size = state.GridSize;
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                list.Add(state.Grid[r, c].IsGiven);
            }
        }
        return list;
    }

    public bool HasReplayData =>
        (SolutionDigits.Count > 0 && SolutionDigits.Count == Givens.Count)
        || !string.IsNullOrWhiteSpace(PrebuiltPuzzleId);

    /// <summary>
    /// Rebuilds the original puzzle state (givens + solution) for history replay.
    /// </summary>
    public SudokuGameState ToPuzzleState()
    {
        if (!HasReplayData)
            throw new InvalidOperationException("History entry lacks replay data");

        // 1) If solution digits are present, rebuild directly.
        if (SolutionDigits.Count > 0 && SolutionDigits.Count == Givens.Count)
        {
            int count = SolutionDigits.Count;
            int size = count == 16 ? 4 : 9;

            var state = new SudokuGameState
            {
                Difficulty = Difficulty,
                StartTime = StartTime,
                Status = GameStatus.InProgress,
                IsDeadlyMode = WasDeadlyMode,
                IsDaily = IsDaily,
                DailyDate = DailyDate,
                IsScenario = IsScenario,
                ScenarioTechnique = ScenarioTechnique,
                IsTutorial = IsTutorial,
                TutorialId = TutorialId,
                PrebuiltPuzzleId = PrebuiltPuzzleId
            };

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int idx = r * size + c;
                    int sol = SolutionDigits[idx];
                    bool given = Givens[idx];
                    state.Grid[r, c] = new SudokuCell
                    {
                        Solution = sol,
                        Value = given ? sol : 0,
                        IsGiven = given,
                        Notes = new bool[size == 4 ? 4 : 9]
                    };
                }
            }

            return state;
        }

        // 2) Otherwise, try prebuilt puzzle reconstruction.
        if (!string.IsNullOrWhiteSpace(PrebuiltPuzzleId))
        {
            var puzzle = PrebuiltPuzzleLibrary.GetById(PrebuiltPuzzleId);
            if (puzzle == null)
                throw new InvalidOperationException($"Prebuilt puzzle not found: {PrebuiltPuzzleId}");

            var state = puzzle.ToGameState();
            state.Difficulty = Difficulty;
            state.IsDaily = IsDaily;
            state.DailyDate = DailyDate;
            state.IsScenario = IsScenario;
            state.ScenarioTechnique = ScenarioTechnique;
            state.IsTutorial = IsTutorial;
            state.TutorialId = TutorialId;
            state.IsDeadlyMode = WasDeadlyMode;
            return state;
        }

        throw new InvalidOperationException("History entry lacks usable replay data");
    }

    public string GetFormattedDuration()
    {
        var ts = TimeSpan.FromSeconds(DurationSeconds);
        if (ts.Hours > 0)
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    public string GetStatusText()
    {
        var loc = LocalizationService.Instance;
        return Status switch
        {
            GameStatus.Won => $"✓ {loc.Get("history.won")}",
            GameStatus.Lost => $"✗ {loc.Get("history.lost")}",
            GameStatus.Abandoned => $"⏸ {loc.Get("history.abandoned")}",
            GameStatus.InProgress => $"▶ {loc.Get("history.in_progress")}",
            _ => loc.Get("common.unknown")
        };
    }

    public string GetDifficultyText()
    {
        return LocalizationService.Instance.GetDifficultyDisplay(Difficulty);
    }
}
