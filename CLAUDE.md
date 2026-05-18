# dotnet-agents — Claude Code Guidelines

## Commit Message Guidelines

Lowercase, imperative, ≤ 72 chars, no trailing period. No conventional prefixes (`feat:`, `fix:`, `refactor:`, etc.). No milestone numbers, emoji, or `Co-authored-by` trailers. Short and direct.

## Workflow Rules

- Build and tests must pass on every commit: `dotnet build` + `dotnet test` (and `npm test` for web changes).
- Each commit is one logical change; refactors are separate from behaviour changes.
- New behaviour ships with a test in the same commit (or the immediately preceding commit for pure refactors).
- Update `README.md` in the same commit when public behaviour or quickstart changes.
- Never bypass hooks (`--no-verify`) or skip signing. Use `git add <files>` — never `git add .`.
- **Push commits** to remote after every logical set of commits (completed feature, passing tests).
- **Run the server** when changes require runtime verification: `dotnet run --project src/DotnetAgents.Api` (kill any prior instance first). Run the web UI with `npm run dev` in `web/`. This lets the user test on LAN.
- Add XML doc comments on public types and methods only when the name alone is insufficient; keep them to one line.

## Tool & Stack Notes

- .NET 10 / C# 14, NUnit 4.x, NSubstitute for mocks.
- Microsoft.Agents.AI 1.5.0 over `Microsoft.Extensions.AI` IChatClient abstraction.
- API binds to `0.0.0.0:5000`; web dev server binds to `0.0.0.0:5173`. Do not change without reason — LAN access is a requirement.
- Config path resolution: `--config <path>` CLI > `DOTNET_AGENTS_CONFIG` env > `./config.json`.
- Use `superpowers:subagent-driven-development` for multi-step implementations with 3+ independent tasks.
- Subagent responses must be concise: state findings directly, skip preamble. Instruct any dispatched subagent to do the same.
- Use `haiku` model for mechanical/spec-review subagents; `sonnet` for code-quality/integration tasks.
- Use the `frontend-design` skill when building or modifying any UI component.
- Responses to the user must be direct and brief — state the result, not the thought process.
