# AI Rules

> What to do / avoid when generating or editing code here. Pair with [token-efficiency.md](token-efficiency.md),
> [conventions.md](conventions.md), and [coding-style.md](coding-style.md).

## Golden rules

1. **Reuse before you write.** Extend existing ports and helpers instead of adding parallel ones.
2. **Match the surroundings.** Mirror the nearest use case's structure, naming, and style.
3. **Keep the build green.** Release build passes with no new warnings; fix causes, don't suppress.
4. **Don't touch what you don't own.** Don't hand-edit generated migration snapshots (`OrderFlowDbContextModelSnapshot.cs`) — regenerate via `dotnet ef`.
5. **Stay in scope.** Make the requested change and what it strictly requires.
6. **Learn APIs from in-repo usage, not from binaries.** Copy existing MediatR / FluentValidation / EF / Polly usage; don't decompile packages.
7. **Every interface, DTO, or DI registration needs a real caller in the same change.** No scaffolding "for symmetry" or "in case we need it later".
8. **Business rules belong in the aggregate.** If a handler starts making decisions about order state, move that logic into `Order`.
9. **AI is best-effort.** Order capture must succeed even when the AI provider fails, times out, or returns garbage.

## DO

- Respect layer boundaries ([architecture.md](architecture.md)).
- Wire new capabilities end-to-end: domain → command/query + handler + validator → port/implementation → controller action → `.http` sample ([prompts/implement.md](prompts/implement.md)).
- Validate input with a FluentValidation validator next to the command/query; the pipeline runs it.
- Throw the existing exception types; let `GlobalExceptionHandler` map them.
- Call `SaveChangesAsync` after every write-repository `AddAsync` or aggregate mutation.
- Use `AsNoTracking` projections for every new read path.
- Invalidate `CacheKeys` entries when writing data that a cached query serves.
- Add an EF migration for every schema change and review the generated SQL.
- State assumptions in your response or PR description, never as code comments.

## AVOID

- Duplicated logic or copy-paste variants of existing handlers.
- New packages without justification ([dependencies.md](dependencies.md)).
- Public setters or mutable collections on domain entities.
- Business logic in controllers or Infrastructure.
- Calling EF / `HttpClient` from Application or Domain.
- Returning domain entities from the API — always DTOs.
- Logging or persisting raw API keys, prompts containing secrets, or full AI payloads at Information level.
- Suppressing warnings, deleting tests, or weakening nullability to force a green build.
- Broad refactors bundled into an unrelated change.

## Existing abstractions (use directly)

| Need | Use | Registered in |
|---|---|---|
| Load/save an order aggregate | `IOrderRepository` (`AddAsync`, `GetByIdAsync`, `SaveChangesAsync`) | Infrastructure DI |
| Load/save products | `IProductRepository` | Infrastructure DI |
| Order list / detail reads | `IOrderReadService` (`ListOrdersAsync`, `GetOrderDetailAsync`) | Infrastructure DI |
| Next order number | `IOrderNumberGenerator.NextAsync` | Infrastructure DI |
| Caching | `ICacheService.GetOrCreateAsync` / `Remove` + keys in `CacheKeys` | Infrastructure DI (singleton) |
| AI analysis / NL search | `IOrderIntelligenceService` | Infrastructure DI (by `Ai:Provider`) |
| Send a command/query from Api | `ISender` (MediatR) | MediatR |
| Paging | `PagedResult<T>` | — |
| HTTP envelope | `ApiResponse<T>.Ok(...)` / `ApiError` | — |

Don't wrap these in another adapter. Extend the interface (and its implementation) when a new operation is needed.

## When unsure

Ask or flag, don't guess. If requirements are ambiguous, pick the smallest reasonable interpretation,
state it, and proceed. Never invent config keys or framework behavior.

## Definition of done

- [ ] Requested behavior implemented and covered by unit tests.
- [ ] `dotnet build OrderFlow.sln -c Release` green, no new warnings.
- [ ] No duplication; existing patterns reused.
- [ ] No dead code, stray scope, or suppressed warnings.
- [ ] Every write path calls `SaveChangesAsync`.
- [ ] Migration added for schema changes; `.http` sample added for new endpoints.
- [ ] Docs updated if behavior/structure changed.
