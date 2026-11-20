# Prompt: Code Review

> Checklist for reviewing a change, keyed to the docs in this folder.

## Correctness

- [ ] Does what it claims; handles edge cases and failure paths.
- [ ] Input validated by a FluentValidation validator; errors use the existing exception types.
- [ ] Every write path calls `SaveChangesAsync`.
- [ ] No null / off-by-one / paging-bound issues; nullability annotations honest.

## Design & architecture

- [ ] Right feature folder and layer; dependency direction respected ([architecture.md](../architecture.md)).
- [ ] Business rules live in the aggregate, not the handler or controller.
- [ ] Reads use `IOrderReadService`-style `AsNoTracking` projections; writes use the repository.
- [ ] Reuses existing ports; no parallel implementations or speculative interfaces.
- [ ] Registrations only in the layer's `DependencyInjection.cs`.

## Contracts & data

- [ ] API routes, DTO shapes, `ApiResponse` envelope, and config keys unchanged unless intended.
- [ ] Schema changes come with a migration; generated SQL reviewed (indexes, nullability, data loss).
- [ ] Cache keys invalidated on related writes.

## AI integration

- [ ] AI failure / timeout cannot fail or block order capture.
- [ ] AI output is parsed and validated before reaching the domain.
- [ ] No API keys or sensitive payloads logged.

## Conventions & style

- [ ] Naming and structure follow [conventions.md](../conventions.md).
- [ ] Async + `CancellationToken ct` propagated; no `.Result`/`.Wait()`.
- [ ] Structured logging templates; no string interpolation in log calls.
- [ ] No dead or commented-out code; summaries explain why.

## Tests & performance

- [ ] Changed behavior covered by unit tests in `tests/OrderFlow.UnitTests`.
- [ ] Read-path / index / cache changes re-benchmarked and `PERFORMANCE.md` updated.

## Scope & safety

- [ ] Focused change; no unrelated refactors.
- [ ] No new packages without justification; no secrets committed.
- [ ] Docs updated if behavior or structure changed.

## Flag, don't silently pass

Call out behavior changes disguised as refactors, weakened types, or anything that can't be verified
from the diff.
