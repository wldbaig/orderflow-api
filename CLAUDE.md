# CLAUDE.md — OrderFlow API

> Stack: .NET 8 / C# · Clean Architecture + MediatR CQRS + EF Core (SQL Server)

> Instructions for Claude in this repository. Keep this file concise and use the linked docs as the
> source of truth. Technical guidance only; the narrative design lives in
> [ARCHITECTURE.md](ARCHITECTURE.md) and [PERFORMANCE.md](PERFORMANCE.md).

## Read first

- [AI Rules](docs/ai-rules.md) — what to do / avoid when generating code
- [Token Efficiency](docs/token-efficiency.md) — keep AI runs focused and efficient

## Docs

- [Architecture](docs/architecture.md) — layers, boundaries, dependency direction
- [Conventions](docs/conventions.md) — structure, naming, error handling, CQRS
- [Coding Style](docs/coding-style.md) — formatting, functions, DI, async
- [Dependencies](docs/dependencies.md) — dependency-management rules

## Prompts

- [Implement](docs/prompts/implement.md) — add a feature / use case
- [Review](docs/prompts/review.md) — review checklist
- [Refactor](docs/prompts/refactor.md) — refactor guidance

## Working rules

- Before changing code, inspect the relevant project, the nearest existing use case, and the applicable doc.
- Follow existing repository patterns instead of introducing a new architectural style.
- Keep changes scoped to the requested task; avoid unrelated refactors.
- Prefer extending existing abstractions (`IOrderRepository`, `IOrderReadService`, `ICacheService`, `IOrderIntelligenceService`) over creating parallel ones.
- Preserve dependency direction and project boundaries defined in `docs/architecture.md`.
- Do not introduce a new NuGet package unless the task genuinely requires it and no existing dependency already provides the capability.
- Do not change public contracts, database schema, migrations, API routes, the `ApiResponse` envelope, or configuration keys (`ConnectionStrings:Default`, `Ai:*`, `Database:*`) unless required by the task.
- When changing DTOs, request contracts, or shared models, search for all consumers before editing them.
- When adding or changing behavior, add or update unit tests in `tests/OrderFlow.UnitTests` (mirror the `src` folder of the code under test).
- Do not weaken or delete tests merely to make a change pass.
- Never add secrets, API keys, passwords, or connection strings with credentials to source control. Use user-secrets or environment variables.
- If requirements conflict with the current architecture or existing code behavior, call out the conflict before making a broad structural change.
- For multi-file or architectural work, form a short implementation plan before editing.
- After implementation, review the diff for accidental changes and run the narrowest relevant verification first, then solution-level verification.

## Project facts

- **Solution:** `OrderFlow.sln` (SDK pinned in `global.json`, 8.0.x)
- **Restore:** `dotnet restore OrderFlow.sln`
- **Build:** `dotnet build OrderFlow.sln -c Release`
- **Test:** `dotnet test OrderFlow.sln -c Release` (xUnit + NSubstitute + EF Core InMemory, project `tests/OrderFlow.UnitTests`)
- **Format check:** `dotnet format OrderFlow.sln --verify-no-changes` (no `.editorconfig` yet; not enforced in CI)
- **Run API:** `dotnet run --project src/OrderFlow.Api` (applies migrations + seeds on startup)
- **Benchmark:** `dotnet run -c Release --project tools/OrderFlow.Benchmark`
- **New migration:** `dotnet ef migrations add <Name> -p src/OrderFlow.Infrastructure -s src/OrderFlow.Api`
- **CI:** `.github/workflows/ci.yml` (job `build-and-test`) — restore, Release build and unit tests on every PR and on every push/merge to `main`. `main` is protected: the job must pass before a PR can be merged.

## Implementation guidance

1. Identify the affected project and layer.
2. Inspect the closest existing use case (e.g. `CreateOrder`, `CancelOrder`, `GetOrders`, `SearchOrdersNl`).
3. Read only the relevant linked documentation.
4. Make the smallest coherent change.
5. Preserve existing contracts unless the request explicitly requires changing them.
6. Add or update unit tests for the changed behavior.
7. Build the solution (`dotnet build OrderFlow.sln -c Release`).
8. Run `dotnet test OrderFlow.sln -c Release`.
9. Add a sample request to `src/OrderFlow.Api/OrderFlow.Api.http` for new endpoints.
10. Review the final diff and report any validation that could not be completed.

## Review guidance

Prioritize:

1. Correctness and regression risk.
2. Architecture and dependency-boundary violations.
3. API contract / `ApiResponse` envelope compatibility.
4. Data integrity and migration impact.
5. Error handling (exception → HTTP mapping) and logging.
6. Async, cancellation, and DbContext/HttpClient lifetime issues.
7. Security and accidental secret exposure.
8. AI resilience: an AI failure must never fail order capture.
9. Test coverage and consistency with existing patterns.

Prefer concrete findings with file/line references over general style commentary.

## Refactoring guidance

- Preserve externally observable behavior unless the task says otherwise.
- Refactor incrementally; build after each meaningful step.
- Avoid combining broad cleanup with feature work.
- Keep API routes, DTO shapes, and config keys stable.
- If a refactor reveals a behavioral change, surface it explicitly.
- If you touch `OrderReadService`, indexes, or caching, re-run the benchmark and update `PERFORMANCE.md`.

## Definition of done

- The requested behavior is implemented.
- The change follows the repository architecture and conventions.
- No unrelated files were changed.
- The solution builds in Release and all unit tests pass.
- Docs updated (`README.md`, `ARCHITECTURE.md`, `PERFORMANCE.md`) when endpoints or behavior change.
- No secrets introduced.
- The final response summarizes the change and clearly states what was validated.

## Instruction precedence

- Follow direct user instructions first.
- Then the closest applicable `CLAUDE.md`. Nested `CLAUDE.md` files may override rules for their folder.
- Keep detailed rules in `docs/`; keep `CLAUDE.md` as the navigation and execution entrypoint.

## Claude notes

- Read only the `docs/` files relevant to the task.
- Use sub-agents for broad multi-file searches; keep conclusions, not file dumps.
- Model routing: planning/design on Opus (high effort); mechanical implementation on Sonnet (medium effort).
- Before opening a PR: Release build green and `.github/pull_request_template.md` filled in.
