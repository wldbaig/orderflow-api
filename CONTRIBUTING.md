# Contributing

## Workflow
1. Branch from `main`: `feature/<short-name>`, `fix/<short-name>` or `docs/<short-name>`.
2. Keep commits small and messages imperative ("Add order priority filter").
3. Run `dotnet build OrderFlow.sln -c Release` locally.
4. Open a PR against `main` using the template; CI must be green before merge.

## Adding a use case
1. Domain: add or extend behaviour on the aggregate (with invariants).
2. Application: `<Feature>/Commands|Queries/<UseCase>/` with command, handler, validator.
3. Infrastructure: implement any new port; add an EF migration if the schema changes.
4. Api: thin controller action + request contract in `Contracts/`.
5. Add a sample call to `src/OrderFlow.Api/OrderFlow.Api.http`.

## Performance-sensitive changes
If you touch `OrderReadService`, indexes or caching, re-run `tools/OrderFlow.Benchmark`
and update `PERFORMANCE.md` with the new numbers.

## Working with AI agents
Coding agents follow `AGENTS.md` and the rules in `docs/` (`CLAUDE.md` defers to it).
