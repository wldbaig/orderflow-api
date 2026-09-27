# Performance

This is a proof-of-concept, but the performance work is measured against a realistic volume,
not a toy dataset. The seeder loads **50,000 orders** (with ~125k order lines) so the numbers
below reflect real query behaviour.

## What makes the read path fast

`GET /api/orders` (and the AI natural-language search, which reuses the exact same code path)
is built to scale:

1. **Projection straight to a DTO.** `OrderReadService` selects only the columns the list
   needs (`SELECT OrderNumber, CustomerName, Status, ...`) directly into `OrderListItemDto`.
   Full `Order` aggregates and their line collections are **never materialised** for a list —
   no over-fetching, no N+1.
2. **`AsNoTracking`.** Read queries don't pay for change-tracking snapshots.
3. **Server-side filtering, sorting and paging.** Every filter is a `WHERE`, sorting is a
   whitelisted column, and paging is `OFFSET/FETCH`. The database returns one page, not the
   whole table.
4. **Indexes on the real filter/sort columns** (added via migration in
   `OrderConfiguration`):
   - `IX_Orders_CreatedAtUtc` — the default sort
   - `IX_Orders_Status_CreatedAtUtc` — filter by status + sort by date
   - `IX_Orders_Priority_CreatedAtUtc` — filter by priority + sort by date
   - `IX_Orders_TotalAmount`, `IX_Orders_TotalUnits` — range filters and value sorts
   - `IX_Orders_OrderNumber` (unique) — lookup + stable paging tiebreaker

   `TotalAmount` and `TotalUnits` are **persisted columns maintained by the domain**, not
   correlated subqueries over the lines table — so they can be filtered and sorted against an
   index instead of recomputed per row.

## Benchmark: naive vs optimized

`tools/OrderFlow.Benchmark` runs both approaches against the seeded database and reports the
median of 5 runs (after a warm-up):

- **Naive** — the common mistake: `context.Orders.Include(o => o.Lines).ToList()` then sort
  and page **in memory**. Loads every row and every line, tracked.
- **Optimized** — the real `OrderReadService`: `AsNoTracking` + projection + server-side
  filter/sort/page against indexes.

| Scenario | Naive | Optimized | Speed-up |
|---|--:|--:|--:|
| Sort by `TotalAmount` desc, first page (25) | 1499.7 ms | 57.5 ms | **26×** |
| Filter `Status=Confirmed`, sort `CreatedAtUtc` desc, first page (25) | 1560.3 ms | 25.3 ms | **62×** |

*Environment: .NET 8, SQL Server LocalDB, 50,000 orders, single machine. Absolute numbers
vary by hardware; the ratio and its cause do not.*

The gap widens as the table grows: the naive query is **O(rows)** — it always reads the whole
table and all lines — while the optimized query touches only one indexed page and its cost is
effectively flat regardless of table size.

### Reproduce it

```bash
dotnet run --project tools/OrderFlow.Benchmark -c Release
# optional: pass a different order count, e.g. 100000
dotnet run --project tools/OrderFlow.Benchmark -c Release 100000
```

It migrates and seeds automatically (skipping seed if the target volume already exists), then
prints the table above.

## Caching a read-heavy reference endpoint

`GET /api/products` returns reference data that is read constantly and changes rarely — the
classic case for caching. It is served through `ICacheService` (an `IMemoryCache` adapter):

- The catalog is cached for 10 minutes, with a per-key lock to prevent a cache stampede
  (many concurrent misses all hitting the DB at once).
- **Invalidation is explicit and correct:** `CreateProductCommandHandler` calls
  `cache.Remove(CacheKeys.ProductCatalog)` after a successful write, so the next read
  rebuilds from the database. No stale reads, no time-based-only guessing.

## Other performance-minded choices

- **Order numbers from a SQL sequence** (`NEXT VALUE FOR`) instead of `COUNT(*) + 1` — no
  table scan and no race under concurrency.
- **Batched seeding** with `AutoDetectChangesEnabled = false` and `ChangeTracker.Clear()`
  between batches, so seeding 50k rows stays memory-flat.
- **`EnableRetryOnFailure`** on the SQL Server connection for transient-fault resilience.
- **`CancellationToken` threaded through** every handler, repository and query, so abandoned
  requests stop doing work.
