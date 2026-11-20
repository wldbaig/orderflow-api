# Conventions

> Structure, naming, error handling, and CQRS conventions as they exist in this codebase.
> Consistency with existing code matters more than any single choice.

## Structure

- One folder per use case: `Application/<Feature>/Commands|Queries/<UseCase>/`.
- Namespace mirrors the folder path (`OrderFlow.Application.Orders.Commands.CreateOrder`).
- One public type per file, file name matches the type. Existing exceptions: small companion types
  that only exist for the primary type (`CreateOrderLineInput` next to `CreateOrderCommand`, `ApiError`
  next to `ApiResponse`, nested option classes in `AiOptions`).
- A use case folder contains `<UseCase>Command|Query`, `<UseCase>CommandHandler|QueryHandler`, and
  `<UseCase>CommandValidator|QueryValidator` (when it has input) — one file each, as in `CreateOrder`,
  `CancelOrder`, `GetOrders`, `SearchOrdersNl`.
- Legacy: `GetOrderById`, `GetProductCatalog`, and `CreateProduct` still keep request, validator, and
  handler in a single file. Don't copy that layout for new use cases; split them when you next touch them.

## Naming

| Kind | Convention | Example |
|---|---|---|
| Types, methods, properties | PascalCase | `OrderReadService`, `ListOrdersAsync` |
| Private fields | `_camelCase` | `_repository` |
| Locals, parameters | camelCase | `orderNumber` |
| Interfaces / ports | `I` + PascalCase | `IOrderRepository`, `IOrderIntelligenceService` |
| Async methods | `Async` suffix | `GetByIdAsync` |
| Commands / queries | `<Verb><Noun>Command` / `Get<Noun>Query` | `CancelOrderCommand`, `GetOrdersQuery` |
| Handlers / validators | `<Request>Handler` / `<Request>Validator` | `CreateOrderCommandHandler` |
| Inbound HTTP contract (Api) | `<Verb><Noun>Request` | `CreateOrderRequest` |
| Data returned from Application | `<Noun>Dto` | `OrderDetailDto`, `OrderListItemDto`, `ProductDto` |
| Nested input inside a command | `<Noun>Input` | `CreateOrderLineInput` |
| Write-side data access | `I<Aggregate>Repository` | `IOrderRepository` |
| Read-side data access | `I<Aggregate>ReadService` | `IOrderReadService` |
| Cancellation token parameter | `ct` | `CancellationToken ct` |

- Don't put the use-case verb in a DTO name (`OrderDetailDto`, not `GetOrderDto`).
- Shared DTOs live in `<Feature>/Dtos`; a DTO used by only one use case may live in that use case folder.

## Error handling

The strategy is **exceptions**, mapped to HTTP once in `Api/Common/GlobalExceptionHandler.cs`:

| Exception | Layer | HTTP |
|---|---|---|
| `ValidationException` (from `ValidationBehavior`) | Application | 400 + field errors |
| `NotFoundException` | Application | 404 |
| `DomainException` (invariant / invalid transition) | Domain | 409 |
| `AiUnavailableException` | Application | 503 |
| `OperationCanceledException` | — | 499 |
| anything else | — | 500 (generic message, logged as error) |

- Don't return result/either objects from handlers; throw the matching exception.
- Don't catch-and-swallow. Log with context (structured properties, e.g. `{OrderNumber}`) or let it propagate.
- New error categories get a new exception type in `Application/Common/Exceptions` **and** a mapping line in `GlobalExceptionHandler`.
- Every API response uses the `ApiResponse<T>` / `ApiError` envelope.

## Domain model

- Aggregates are created through a static factory (`Order.Create(...)`) that enforces invariants.
- Private setters; collections exposed read-only (`_lines` → `Lines`).
- State changes only through intent-revealing methods; invalid transitions throw `DomainException`.
- AI-derived fields are written only through `ApplyIntelligence` / `MarkIntelligenceFailed`.

## CQRS: reads vs writes

- **Commands** inject the write repository, load the tracked aggregate (`GetByIdAsync` includes lines), call domain methods, then `await _repository.SaveChangesAsync(ct)`. `AddAsync` stages; it does not persist — every command that adds or mutates must call `SaveChangesAsync` before returning.
- **Queries** inject `IOrderReadService` (or `ICacheService` for reference data) and return DTOs. Read services use `AsNoTracking` and project with `Select` to DTOs in SQL — no full aggregate loads.
- Never run a query through `IOrderRepository` "because it's already there".
- Writes that affect cached reference data must invalidate the key from `Common/CacheKeys.cs` (see `CreateProduct`).

## Mapping

- Domain → DTO mapping is hand-written extension methods in `<Feature>/Mapping` (`OrderMappings.ToDetailDto()`).
- Map every DTO property explicitly; no reflection/convention mappers (no AutoMapper/Mapster).
- Read-side projections in `OrderReadService` map explicitly inside the `Select`.
- Api request → command mapping happens in the controller action, explicitly.

## AI integration

- AI is consumed only through `IOrderIntelligenceService`.
- Implementations return a non-succeeded result instead of throwing on transient failure; handlers branch on `Succeeded`.
- Prompts live in `Infrastructure/Intelligence/IntelligencePrompts.cs`; AI output is parsed into typed results, never passed raw to the domain.
- Provider selection is config-only (`Ai:Provider`); missing key falls back to `Heuristic`.
