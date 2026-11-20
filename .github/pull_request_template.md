## Summary
<!-- What changed and why -->

## Type
- [ ] Feature
- [ ] Fix
- [ ] Docs
- [ ] Refactor / performance

## Checklist
- [ ] `dotnet build OrderFlow.sln -c Release` passes
- [ ] `dotnet test OrderFlow.sln -c Release` passes; tests added/updated for changed behavior
- [ ] Business rules stay inside the `Order` aggregate
- [ ] No secrets in `appsettings*.json`
- [ ] EF migration added (if schema changed)
- [ ] Docs updated (README / ARCHITECTURE / PERFORMANCE)

## How to test
<!-- Requests from OrderFlow.Api.http, benchmark run, etc. -->
