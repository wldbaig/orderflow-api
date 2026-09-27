# Architecture

OrderFlow is a thin vertical slice built with **Clean Architecture**. The point of the
structure is that business rules have exactly one home and the outer layers depend inward —
so features can be added for years without the core rotting into a ball of mud.

## Layers and the dependency rule

```
OrderFlow.Api            →  OrderFlow.Application  →  OrderFlow.Domain
OrderFlow.Infrastructure →  OrderFlow.Application  →  OrderFlow.Domain
```

Dependencies only ever point **inward**. The Domain knows nothing about EF, HTTP, or AI.
The Application knows the Domain and defines *ports* (interfaces). Infrastructure and Api
sit on the outside and depend on the Application — never the other way around.

| Project | Responsibility | Knows about |
|---|---|---|
| **Domain** | Entities, value objects, enums, business rules. No framework code. | Nothing |
| **Application** | Use cases (MediatR commands/queries), DTOs, validation, ports (interfaces). | Domain |
| **Infrastructure** | EF Core, repositories, read models, caching, AI clients, seeding. Implements the ports. | Application, Domain |
| **Api** | Controllers, HTTP concerns, DI wiring, Swagger, exception handling. | Application, Infrastructure |

## Where the business logic lives — and why

**All order rules live in the `Order` aggregate** (`OrderFlow.Domain/Entities/Order.cs`):

- Totals (`TotalAmount`, `TotalUnits`) are computed inside the aggregate, never in a
  controller or a query.
- Status transitions are guarded: `Cancel()` throws unless the order is `Pending`,
  `Confirm()` only moves from `Pending`, lines can only be added while `Pending`.
- The aggregate is created through a `Create(...)` factory that enforces invariants
  (non-empty lines, required customer) so an `Order` can never exist in an invalid state.
- AI results are applied through explicit methods (`ApplyIntelligence`,
  `MarkIntelligenceFailed`) rather than by setting loose properties.

Because those rules are *inside* the entity, there is no way to bypass them from a
controller, a handler, or a future feature. This is the single most important property of
the design: **the rules cannot leak or drift.**

## The boundaries you can see

- **DTOs at the edge, entities inside.** The Api accepts request contracts
  (`Api/Contracts`), the Application returns DTOs (`Application/Orders/Dtos`), and mapping
  from the domain happens in one explicit place (`Application/Orders/Mapping/OrderMappings`).
  The `Order` entity is never serialized to the wire.
- **CQRS via MediatR.** Writes are *commands* (`CreateOrder`, `CancelOrder`, `CreateProduct`);
  reads are *queries* (`GetOrders`, `GetOrderById`, `SearchOrdersNl`, `GetProductCatalog`).
  Controllers only build a request and `Send` it.
- **Ports & adapters.** The Application declares interfaces —
  `IOrderRepository`, `IOrderReadService`, `IOrderNumberGenerator`, `ICacheService`,
  `IOrderIntelligenceService` — and Infrastructure implements them. Swapping SQL Server,
  the cache, or the AI provider touches only Infrastructure.
- **Cross-cutting concerns as pipeline behaviors.** Validation (`ValidationBehavior`) and
  logging/timing (`LoggingBehavior`) wrap every request once, so controllers and handlers
  never repeat that code.

## Request flow: `POST /api/orders`

1. `OrdersController.Create` maps the request DTO to a `CreateOrderCommand` and sends it.
2. `LoggingBehavior` → `ValidationBehavior` (FluentValidation) run first.
3. `CreateOrderCommandHandler`:
   - gets an order number from `IOrderNumberGenerator` (a SQL sequence),
   - builds the `Order` aggregate (invariants enforced),
   - calls `IOrderIntelligenceService.AnalyzeOrderAsync` — **best-effort**: on any failure it
     returns a non-succeeded result and the order is still saved (`MarkIntelligenceFailed`),
   - persists through `IOrderRepository`,
   - maps to `OrderDetailDto`.
4. The controller returns `201 Created` with the standard `ApiResponse<T>` envelope.

## Read vs write separation

- **Writes** load the full tracked aggregate (`IOrderRepository`) so domain methods run
  against a consistent object graph.
- **Reads** never load aggregates. `IOrderReadService` projects straight to DTOs with
  `AsNoTracking`, filters/sorts on indexed columns, and pages server-side. See
  [PERFORMANCE.md](PERFORMANCE.md).

## Error handling

Handlers throw meaningful exceptions; a single `GlobalExceptionHandler` (the .NET 8
`IExceptionHandler`) maps each to a status code and the standard error envelope:

| Exception | HTTP |
|---|---|
| `ValidationException` | 400 |
| `NotFoundException` | 404 |
| `DomainException` (rule violation) | 409 |
| `AiUnavailableException` | 503 |
| anything else | 500 |

## Why this won't rot

- New use cases are new command/query slices — they don't modify existing ones (open/closed).
- New rules go into the aggregate, where they're enforced for every caller automatically.
- Infrastructure choices (DB, cache, AI vendor) are swappable behind interfaces.
- Validation and logging are centralized, so they can't be forgotten on a new endpoint.
