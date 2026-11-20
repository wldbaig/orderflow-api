# CLAUDE.md

Claude follows [AGENTS.md](AGENTS.md) — it's the single source of truth for commands, rules, and the
doc map (`docs/`). This file only adds Claude-specific notes.

## Claude notes

- Read `AGENTS.md` first, then only the `docs/` files relevant to the task.
- Use sub-agents for broad multi-file searches; keep conclusions, not file dumps ([token efficiency](docs/token-efficiency.md)).
- Model routing: planning/design on Opus (high effort); mechanical implementation on Sonnet (medium effort).
- Before opening a PR: `dotnet build OrderFlow.sln -c Release` green and `.github/pull_request_template.md` filled in.
