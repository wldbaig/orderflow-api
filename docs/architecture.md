# Architecture

> Structural rules agents must follow. The narrative explanation (why, trade-offs, diagram) lives in
> [ARCHITECTURE.md](../ARCHITECTURE.md) — this file is the short, enforceable version.

## Core rule: dependencies point inward

```
OrderFlow.Api            →  OrderFlow.Application  →  OrderFlow.Domain
OrderFlow.Infrastructure →  OrderFlow.Application  →  OrderFlow.Domain
```

| Project | Holds | May reference |
|---|---|---|
| `OrderFlow.Domain` | Entities (`Order` aggregate, `OrderLine`, `Product`), enums, `DomainException` | nothing project-specific, no NuGet framework packages |
| `OrderFlow.Application` | MediatR commands/queries + handlers, FluentValidation validators, DTOs, mapping extensions, pipeline behaviors, ports (interfaces), application exceptions | Domain |
| `OrderFlow.Infrastructure` | `OrderFlowDbContext`, EF configurations, migrations, seed, repositories, `OrderReadService`, `MemoryCacheService`, AI providers (OpenAI / Gemini / Heuristic) | Application, Domain |
| `OrderFlow.Api` | Controllers, request contracts, `ApiResponse`/`ApiError`, `GlobalExceptionHandler`, `Program.cs`, startup DB initializer | Application, Infrastructure |

- Domain and Application must never reference EF Core, ASP.NET Core, `HttpClient`, Polly, or AI SDKs.
- Ports (interfaces) are declared in Application (`Common/Interfaces`, `Intelligence`) and implemented in Infrastructure.

## Composition roots

- `OrderFlow.Application/DependencyInjection.cs` → `AddApplication()`: MediatR (assembly scan), pipeline behaviors (`LoggingBehavior` → `ValidationBehavior` → handler), validators (assembly scan).
- `OrderFlow.Infrastructure/DependencyInjection.cs` → `AddInfrastructure(configuration)`: DbContext, repositories, read service, cache, AI provider selection + Polly policies.
- `OrderFlow.Api/Program.cs` calls both and wires HTTP concerns.
- Handlers and validators are discovered by assembly scan — never register them manually.
- New ports are registered in the matching layer's `DependencyInjection.cs`. If a feature grows enough registrations to clutter it, extract a private `Add<Feature>` method inside that same file (as `AddIntelligence` does) rather than a new composition root.

## Organize by feature

Application is grouped by feature, then by command/query, then by use case:

```
Application/
  Orders/
    Commands/<UseCase>/        CreateOrder, CancelOrder
    Queries/<UseCase>/         GetOrders, GetOrderById, SearchOrdersNl
    Dtos/                      shared Order DTOs
    Mapping/                   OrderMappings (domain → DTO extensions)
  Reference/                   Products (same shape)
  Intelligence/                AI port + request/result types
  Common/                      Behaviors, Exceptions, Interfaces (ports), Models, CacheKeys
```

Infrastructure is grouped by technical concern (`Persistence`, `Caching`, `Intelligence`) because each
folder implements one port family.

## Write vs read paths (CQRS)

- **Writes** load the tracked aggregate through `IOrderRepository` / `IProductRepository`, call aggregate methods, then `SaveChangesAsync`.
- **Reads** go through `IOrderReadService`: `AsNoTracking` projections straight to DTOs, no aggregate materialization.
- Never route a query through a write repository.

## Business rules

All order rules live inside the `Order` aggregate (`Domain/Entities/Order.cs`): totals, status
transitions (`Confirm`, `Cancel`), line additions, AI result application (`ApplyIntelligence`,
`MarkIntelligenceFailed`). Handlers orchestrate; they never set state directly.

## Cross-cutting concerns

Provided once and consumed: logging (Serilog + `LoggingBehavior`), validation (`ValidationBehavior`),
error → HTTP mapping (`GlobalExceptionHandler`), caching (`ICacheService`), HTTP resilience (Polly in
`AddIntelligence`). Don't re-implement any of these per feature.

## Keep it honest

When something can't be determined from the code, say so rather than guessing. Update this file and
`ARCHITECTURE.md` when the structure changes.
