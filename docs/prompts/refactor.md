# Prompt: Refactor

> Read with [ai-rules.md](../ai-rules.md), [conventions.md](../conventions.md), and
> [coding-style.md](../coding-style.md).

## Principles

- **Behavior-preserving by default.** Same routes, status codes, DTO shapes, and persisted data. If that can't hold, stop and flag it.
- **Small, reversible steps.** Build after each step.
- **Safety net first.** With no test project yet, add characterization tests (or document manual `.http` checks) before touching risky code.
- **Stay in scope.** No feature work bundled in.
- **Respect boundaries.** Keep changes within the right layer; don't hand-edit migration snapshots.

## Good targets

- Logic leaking out of the `Order` aggregate into handlers → move it back into the aggregate.
- Duplicated mapping → a single extension in `<Feature>/Mapping`.
- Queries going through write repositories → `IOrderReadService`-style projections.
- Deep nesting → guard clauses.
- Naming/structure that breaks [conventions.md](../conventions.md).

## Checklist

- [ ] Public API and behavior unchanged (or the change is intentional and flagged).
- [ ] Build green before and after; tests green where they exist.
- [ ] No new duplication; encapsulation preserved or improved.
- [ ] All references, DI registrations, and `.http` samples updated after renames/moves.
- [ ] Read-path changes re-benchmarked (`tools/OrderFlow.Benchmark`).
- [ ] No scope creep, dead code, or suppressed warnings.

## When a "refactor" is really a change

Changing status codes or error messages, altering a DTO shape, changing AI fallback behavior, changing
defaults or config keys, or touching the persisted schema are **behavior changes** — surface them for
explicit approval.
