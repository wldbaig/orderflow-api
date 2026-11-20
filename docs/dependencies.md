# Dependencies

## Add deliberately

- Prefer the BCL and existing packages before adding anything new.
- A new package must earn its place (value vs. size, security surface, maintenance).
- Avoid single-function packages you can write in a few lines.
- Check the license is compatible before adding.

## Current packages (per layer)

| Layer | Packages |
|---|---|
| Domain | none |
| Application | MediatR 12.4.1, FluentValidation 11.11.0 (+ DI extensions), Microsoft.Extensions.*.Abstractions / Options |
| Infrastructure | EF Core 8.0.11 (+ SqlServer, Relational, Design), Microsoft.Extensions.Http.Polly 8.0.11 |
| Api | Serilog.AspNetCore 8.0.3, Swashbuckle.AspNetCore 6.9.0, EF Core Design |
| tools/OrderFlow.Benchmark | see its csproj |

Already-available capabilities — use these before adding a package:

- Validation → FluentValidation. Mediation/pipelines → MediatR behaviors.
- Caching → `ICacheService` (`IMemoryCache`). HTTP resilience → Polly via `AddHttpClient`.
- JSON → `System.Text.Json`. Logging → `ILogger<T>` + Serilog. Mapping → hand-written extensions.

## Pin & centralize

- Versions are pinned per `PackageReference` in each csproj (no Central Package Management yet).
- Keep the same package at the same version across projects (EF Core packages move together).
- Keep build-only packages (`Microsoft.EntityFrameworkCore.Design`) with `PrivateAssets=all`.
- Add with `dotnet add <project> package <Name> --version <x.y.z>`; always specify the version.

## Keep it healthy

- Update deliberately; read changelogs for breaking changes (MediatR and FluentValidation majors break APIs).
- Remove unused packages.
- Watch for security advisories (`dotnet list package --vulnerable`).

## Layer discipline

Domain stays package-free. Application may only take abstractions-level packages. EF Core, Polly,
HTTP clients and AI SDKs belong in Infrastructure; ASP.NET Core, Serilog sinks and Swagger in Api.
