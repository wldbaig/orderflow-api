# CLAUDE.md

Guidance for Claude (and other coding agents) working in this repository.

## What this is
OrderFlow API — a .NET 8 Clean Architecture proof-of-concept for B2B order intake.
Three pillars: clean domain logic, measured performance, AI integrated into order intake.
Read `ARCHITECTURE.md` and `PERFORMANCE.md` before non-trivial changes.

## Layout
```
src/OrderFlow.Domain          Entities (Order aggregate), enums, DomainException. No framework refs.
src/OrderFlow.Application     MediatR commands/queries, validators, DTOs, ports (interfaces).
src/OrderFlow.Infrastructure  EF Core (SQL Server), repositories, read service, caching, AI providers.
src/OrderFlow.Api             Controllers, Program.cs, exception handling, Swagger.
tools/OrderFlow.Benchmark     50k-order read benchmark.
```

## Commands
```bash
dotnet restore OrderFlow.sln
dotnet build OrderFlow.sln -c Release
dotnet run --project src/OrderFlow.Api            # migrates + seeds on startup
dotnet run -c Release --project tools/OrderFlow.Benchmark
dotnet ef migrations add <Name> -p src/OrderFlow.Infrastructure -s src/OrderFlow.Api
```
SDK is pinned in `global.json` (8.0.x, rollForward latestFeature).

## Rules
- Dependencies point inward only: Api/Infrastructure → Application → Domain. Never reference
  EF, HTTP or AI SDKs from Domain or Application.
- Business rules live in the `Order` aggregate. Mutate state through its methods
  (`Create`, `Confirm`, `Cancel`, `ApplyIntelligence`, `MarkIntelligenceFailed`), never via setters
  from handlers or controllers.
- Each use case = one MediatR command/query + handler + FluentValidation validator in its own folder
  under `Application/<Feature>/Commands|Queries/<UseCase>/`.
- Controllers stay thin: map request → command, send, wrap in `ApiResponse`.
- Read paths go through `IOrderReadService` (projection, `AsNoTracking`), not repositories.
- AI work goes behind `IOrderIntelligenceService`. AI failure must never fail order creation —
  fall back via `MarkIntelligenceFailed`. Default provider is `Heuristic` (offline).
- Secrets (API keys, connection strings) go in user-secrets or env vars, never in `appsettings*.json`.
- Schema changes need an EF migration in `Infrastructure/Persistence/Migrations`.

## PR checklist
- `dotnet build -c Release` passes with no new warnings.
- Docs updated (`README.md`, `ARCHITECTURE.md`, `PERFORMANCE.md`) when behaviour or endpoints change.
- Fill in `.github/pull_request_template.md`.
