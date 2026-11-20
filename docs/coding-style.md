# Coding Style

> How code should read and behave in this repo. Match the surrounding file first.

## Formatting

- No `.editorconfig` yet; follow the existing files (4-space indent, Allman braces, file-scoped namespaces).
- `dotnet format OrderFlow.sln --verify-no-changes` is the check to run; it's not enforced in CI yet.
- Don't reformat untouched code.

## Language settings

- `Nullable` and `ImplicitUsings` are enabled in every project. Annotate nullability honestly (`Order?` for not-found); don't silence warnings with `!` unless the value is provably non-null.
- No `GlobalUsings.cs` files exist; imports are per file. Only import what the file uses.

## Functions & files

- Small, single-purpose methods; guard clauses over deep nesting.
- Keep files focused; split when a file gathers unrelated concerns.
- Group many parameters into a type (`OrderQueryParameters`, `OrderIntelligenceRequest`).

## State & data

- Prefer immutability: commands, queries, and request types are `record`s; DTOs use `init` setters.
- Encapsulate aggregates: private setters, read-only collection views.
- No static mutable state; pass dependencies via constructors.

## Object construction

- Target-typed `new()` is used where the type is obvious from the declaration (field initializers, `ToDetailDto() => new() { ... }`). Use an explicit type when the target isn't visible on the same line.
- New entity ids use `Guid.NewGuid()`. (`Guid.CreateVersion7()` needs .NET 9+; revisit when the SDK is upgraded.)

## Layout

- Single line by default for type declarations and method signatures.
- Constructor parameter lists go one per line when there is more than one parameter (see handlers).
- One-parameter constructors may use an expression body (`public OrdersController(ISender sender) => _sender = sender;`).
- LINQ chains and long builder chains (EF configurations, `AddHttpClient(...).AddPolicyHandler(...)`) go one call per line.

## Comments

- Public types and non-obvious members carry a short `/// <summary>` explaining **why** (the codebase does this consistently).
- Inline comments only for non-obvious intent (ordering of pipeline behaviors, Polly timeout ownership).
- No commented-out code, no TODOs left in code.

## Dependencies (DI)

- Depend on interfaces across layer boundaries; constructor injection only.
- Register only in the layer's `DependencyInjection.cs`.
- Use the framework's own services (`ILogger<T>`, `IMemoryCache` behind `ICacheService`, `IHttpClientFactory` via `AddHttpClient`) — don't wrap them again.

## Async & concurrency

- Async all the way; never `.Result` / `.Wait()`.
- Every async method takes and forwards a `CancellationToken ct`.
- `OrderFlowDbContext` is scoped; never share it across threads or cache entities.
- `ConfigureAwait(false)` isn't used in this codebase; don't add it piecemeal.

## Logging

- `ILogger<T>` with structured message templates (`"Created order {OrderNumber}"`), never string interpolation.
- Warnings for degraded-but-handled paths (AI unavailable); errors for unexpected failures (handled centrally).

## Warnings

- The Release build must stay warning-free. Fix causes, don't suppress. If a suppression is unavoidable, scope it narrowly and give a reason.
