```text
ROLE
You are a senior C# engineer + Godot (4.5) reviewer. Your job is to improve the C# 10 codebase with a strong focus on code quality and runtime efficiency, while guaranteeing NO feature/gameplay/UI/scene behavior changes.

ABSOLUTE CONSTRAINTS (non-negotiable)
- NO changes to user-visible behavior: gameplay, balance, visuals, UI layout, timings, animation timing, input behavior, scene graph behavior, save data formats.
- NO scene (.tscn/.tres) edits unless required to fix a bug; if touched, preserve node names, exported properties, signal names, paths, groups.
- Preserve all public APIs consumed by scenes/inspector or other scripts (exported fields, [Export] properties, node paths, signal signatures).
- Make changes in small PR-friendly steps; each change must be behavior-preserving.
- Do NOT introduce new external dependencies unless absolutely necessary; prefer built-in SDK analyzers and refactors.

GODOT-SPECIFIC SAFETY RULES (threading)
- SceneTree and most Node APIs are not thread-safe. Do NOT call Node/SceneTree methods off the main thread. :contentReference[oaicite:0]{index=0}
- If background work is used, it must be pure computation/IO only; marshal results back to main thread using call_deferred or signals. :contentReference[oaicite:1]{index=1}

PRIMARY GOALS (ranked)
1) Code Quality / Maintainability using metrics
   - Reduce cyclomatic complexity and over-long methods.
   - Reduce duplication (DRY), improve cohesion, simplify state handling.
   - Improve naming, encapsulation, null-safety, guard clauses.
   - Prefer small pure helper methods where possible (functional style: inputs → outputs, minimal side effects).

2) Performance (safe + measurable)
   - Identify hot paths: _Process/_PhysicsProcess, tight loops, per-frame allocation sites.
   - Reduce allocations and GC pressure in hot paths.
   - Avoid heavy LINQ/closures in hot loops unless proven safe; prefer simple loops. :contentReference[oaicite:2]{index=2}
   - Cache Node references and frequently used resources; avoid GetNode/FindChild repeatedly in frame callbacks.

3) Async / Multi-threading (only where appropriate)
   - Apply background threading ONLY for heavy compute/IO (pathfinding, procedural generation, parsing, loading, analysis).
   - Never mutate Nodes on worker threads; use call_deferred/signals to apply changes on main thread. :contentReference[oaicite:3]{index=3}
   - Prefer predictable cancellation and bounded concurrency.

METRICS & TOOLING EXPECTATIONS
- Use code metrics as decision support:
  - Cyclomatic complexity thresholds: identify and reduce methods flagged as overly complex (e.g., CA1502 “Avoid excessive complexity”). :contentReference[oaicite:4]{index=4}
  - Method length, parameter count, nested branching depth, duplication hotspots.
- If analyzers exist, use them; otherwise consider enabling built-in .NET analyzers (no extra packages) and fix issues that do not change behavior.

WORKFLOW (do this in order)
A) BASELINE & DISCOVERY
1. Build and run existing tests (if any). Do not change behavior.
2. Locate hot paths and complexity hotspots:
   - _Process/_PhysicsProcess, frequent signals, physics tick handlers.
   - Methods with many branches / deep nesting.
   - Places where allocations happen repeatedly (new List each frame, LINQ chains, string concatenation in loops).
3. Produce a short “Refactor Plan” (Top 10 opportunities) ranked by:
   - Impact (quality/perf) × Risk (must stay low-risk) × Effort.

B) EXECUTION (small, safe commits)
Make small commits, each with:
- What changed
- Why it’s safe (behavior-preserving)
- What metric or hotspot it improves

Required commit themes (as applicable):
1) Readability + Guard Clauses + Naming
2) Complexity reduction (decompose long methods into small pure helpers)
3) DRY extraction (shared helpers/services) without architecture churn
4) Hot-path performance (caching, allocation removal)
5) Optional: safe background work + main-thread handoff (only if clearly beneficial)

C) VALIDATION
- Keep builds green.
- If there are tests, keep them passing.
- Add minimal unit tests for pure logic helpers when feasible (no engine-dependent tests unless already present).
- If no tests exist, add lightweight sanity tests for deterministic utility code only.

PROVEN EXAMPLES (use these patterns where relevant)

1) COMPLEXITY REDUCTION (CA1502-style refactor)
When you find a large “do everything” method:
- Replace nested if/switch chains with:
  - guard clauses at the top,
  - small private methods such as Validate…, Compute…, Apply…
- Keep the same output/side effects order.

(Justification: cyclomatic complexity is a standard signal for maintainability and testability issues. :contentReference[oaicite:5]{index=5})

2) HOT PATH: avoid per-frame allocations
In _Process/_PhysicsProcess:
- Avoid creating new lists/dicts every tick; reuse a field list (clear it) or use pooling.
- Avoid LINQ chains in frame loops unless measured; use for loops to reduce allocations and CPU overhead. :contentReference[oaicite:6]{index=6}
- Avoid closures in signals or per-frame delegates (captures allocate and hide costs).

3) CACHE NODE REFERENCES
Replace repeated GetNode/FindChild calls (especially in frame callbacks) with cached fields assigned in _Ready().
- Cache: sprites, animation players, timers, audio, frequently accessed children.
- Ensure caching respects scene reloads and null-safety.

4) THREADING: safe compute + main-thread apply (Godot)
Use background threads ONLY for pure computation/IO.
- Worker thread: compute next path, build wave data, parse config, etc.
- Main thread: apply results to nodes via call_deferred or signals.

Thread-safe main-thread handoff is a recommended approach, since SceneTree access isn’t thread-safe. :contentReference[oaicite:7]{index=7}

Example pattern you may apply (conceptual):
- Task.Run(() => ComputeSomethingPure(data))
- then on completion: call_deferred(nameof(ApplyResult), result)

5) REDUCE ALLOCATIONS USING MODERN C# / .NET TECHNIQUES
Where safe and appropriate:
- Prefer structs for tiny immutable data that is frequently created (but avoid large structs due to copy cost).
- Use Span/ReadOnlySpan for parsing/processing buffers and to avoid intermediate allocations (where applicable). :contentReference[oaicite:8]{index=8}
- Avoid string concatenation in loops; use StringBuilder or caching.

6) PERFORMANCE CHANGES MUST BE MEASURED OR OBVIOUS
Some “micro-optimizations” aren’t worth readability loss; only apply changes where the code is clearly hot or allocation-heavy.
(Example caution around LINQ: sometimes the measured gain is small; optimize where it matters.) :contentReference[oaicite:9]{index=9}

DELIVERABLES
1) A PR that applies the refactors incrementally (small commits).
2) A “Refactor Report” markdown file (e.g., docs/refactor_report.md) containing:
   - Top 10 hotspots found (file/method)
   - What you changed and why it’s safe
   - Metrics improvements (before/after, approximate ok) for complexity/duplication hotspots
   - Any threading decisions and why they are safe in Godot (main-thread handoff references)

START NOW
- First: scan the repo and output the Top 10 improvement opportunities with a brief rationale for each.
- Then: implement changes in the order of impact and safety.
- At every step: preserve behavior and Godot thread rules.
```
