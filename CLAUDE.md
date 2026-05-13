# dotnet-agents — Claude Code Guidelines

## Commit Message Guidelines

**Rules (strict):**
- Short, imperative, present tense (`add session store`, `wire sse streaming in api`, `fix sandbox path comparison`).
- All lowercase. No leading capitals, no trailing period.
- **No** milestone numbers, phase numbers, or ticket numbers in the message.
- **No** Claude/Anthropic attribution: never include `Co-authored-by: Claude`, `Generated with Claude Code`, or any similar trailer.
- **No** emoji.
- Subject line ≤ 72 chars. Body (if needed) wraps at 72 and explains *why*, not *what*.
- Optional conventional-style prefixes (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`) are allowed but not required — match what's already in `git log` if it has an established style.

**Good examples:**
- `scaffold solution with core runtime api tools and tests`
- `add config validator that collects all errors`
- `wire sse streaming in api`
- `fix path traversal check in read_file tool`

**Bad examples:**
- `Add Session Store.` (capitalised, trailing period)
- `[M1-Task-3] add config validator` (milestone/task reference)
- `add session store 🚀` (emoji)
- `add session store\n\nCo-authored-by: Claude <noreply@anthropic.com>` (forbidden trailer)

## Workflow Rules

- Build and tests must pass on every commit. Run `dotnet build` + `dotnet test` (and `npm test` for web changes) before committing.
- Each commit is one logical change. Refactors are separate commits from behaviour changes.
- New behaviour ships with a test in the same commit (or the immediately preceding commit if it's a pure refactor).
- Update `README.md` in the same commit when public behaviour or quickstart changes.
- Never bypass hooks (`--no-verify`) or skip signing.
- Use `git add <files>` (specific paths). Avoid `git add .` / `git add -A`.

## Tool & Stack Notes

- .NET 10 / C# 14, NUnit 4.x, NSubstitute for mocks.
- Microsoft.Agents.AI 1.5.0 over `Microsoft.Extensions.AI` IChatClient abstraction.
- API binds to `0.0.0.0:5000`; web dev server binds to `0.0.0.0:5173`. Do not change without reason — LAN access is a requirement.
- Config path resolution: `--config <path>` CLI > `DOTNET_AGENTS_CONFIG` env > `./config.json`.
