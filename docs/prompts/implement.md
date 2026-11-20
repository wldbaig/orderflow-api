# Prompt: Implement a Feature

> Steps to add a use case or endpoint. Follow [ai-rules.md](../ai-rules.md),
> [conventions.md](../conventions.md), [coding-style.md](../coding-style.md), and
> [token-efficiency.md](../token-efficiency.md).

## 1. Understand & reuse

- Restate the requirement in one line: inputs, outputs, success criteria, HTTP route.
- Find the nearest existing use case and copy its shape:
  - write that creates an aggregate → `Orders/Commands/CreateOrder`
  - write that changes state → `Orders/Commands/CancelOrder`
  - paged list → `Orders/Queries/GetOrders` + `OrderReadService.ListOrdersAsync`
  - single read → `Orders/Queries/GetOrderById`
  - cached reference read + invalidation → `Reference/Queries/GetProductCatalog` + `Reference/Commands/CreateProduct`
  - AI-assisted → `Orders/Queries/SearchOrdersNl`

## 2. Design the slice

- **Domain:** add behavior to the aggregate with invariants; throw `DomainException` on invalid transitions.
- **Application:** `<Feature>/Commands|Queries/<UseCase>/` with the request record, handler, and validator. Add/extend DTOs and mapping extensions.
- **Ports:** extend `IOrderRepository` / `IOrderReadService` (or add a port in `Common/Interfaces`) only if the handler calls it.

## 3. Implement end-to-end

- **Infrastructure:** implement the port; `AsNoTracking` + projection for reads; EF configuration + migration for schema changes; register in `DependencyInjection.cs` if it's a new type.
- **Api:** request contract in `Contracts/`, thin action that maps request → command, `ISender.Send`, wraps in `ApiResponse<T>.Ok`. Add `[ProducesResponseType]` for success and error codes.
- Add a sample call to `src/OrderFlow.Api/OrderFlow.Api.http`.

## 4. Test

- Add tests for the behavior once a test project exists; at minimum cover aggregate invariants and validator rules.
- Otherwise, exercise the endpoint via the `.http` file against a local run and say so in the PR.

## 5. Finish

- `dotnet build OrderFlow.sln -c Release` with no new warnings.
- Update `README.md` endpoint table / `ARCHITECTURE.md` / `PERFORMANCE.md` as relevant.
- Check the [Definition of Done](../ai-rules.md#definition-of-done).

## Guardrails

- Don't hand-edit migration snapshots.
- Don't add packages without justification ([dependencies.md](../dependencies.md)).
- Extend existing stubs rather than duplicating them.
