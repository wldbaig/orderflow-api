# Token Efficiency

> Rules for AI tools to minimize token usage while keeping quality.

## Reading & searching

- **Search, don't scan.** Use narrow grep/glob patterns; open only the relevant span of large files.
- Start from the use-case folder (`Application/<Feature>/Commands|Queries/<UseCase>/`) — it usually holds everything you need.
- **Don't re-read** files already in context or that you just edited.
- Explore just enough to act; stop when you can make the change.
- Never decompile or reflect over a package to learn its API. Copy the closest in-repo call site; if none exists, say so.

## Tool calls

- Batch independent calls into one step.
- Delegate broad multi-file searches to a sub-agent when available; keep the conclusion, not the dump.
- Trust that an edit applied unless there's reason to doubt it.

## Editing

- Minimal diffs; don't rewrite whole files or reformat unrelated code.
- Generate only what was asked: no speculative interfaces, unused DTOs, or scaffolding.

## Responding

- Be concise; answer first.
- Reference `path:line` instead of pasting code back.
- Report outcomes, not play-by-play.

## Docs & context

- Load only the docs relevant to the task.
- Prefer links over duplicating content (`docs/architecture.md` links to `ARCHITECTURE.md` rather than copying it).

## Model routing

- **Planning/design:** strongest model, high effort.
- **Implementation:** mid-tier model, medium effort.
- Concrete model names live in the tool-specific entrypoint (`CLAUDE.md`, etc.).
