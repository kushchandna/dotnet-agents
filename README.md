# Dotnet Agents

A configuration-driven, JSON-backed agent runtime in .NET 10 with a React web UI.

A single `config.json` declares agents, models, users, and tool settings. The same agent runs from a C# façade (in-process), a REST API with SSE streaming, and a React web UI.

## Requirements

- .NET 10 SDK
- Node 24 (for the web UI)

## Quickstart

```bash
# 1. Configure
cp samples/config.example.json config.json
# Edit config.json: set apiKeyEnvVar values to match your env, or adjust models
export OPENAI_API_KEY=sk-...   # if using the openai provider

# 2. Build and test
dotnet build
dotnet test

# 3. Run the API
dotnet run --project src/DotnetAgents.Api -- --config config.json

# 4. In another shell, run the web UI
cd web
npm install
npm run generate:api   # generates typed API client from running API
npm run dev
```

Open `http://localhost:5173` (or `http://<host-ip>:5173` from another machine on your LAN).

## Restarting the API

After editing `config.json`, restart the API to pick up the changes:

```bash
./restart-api.sh                      # uses config.json by default
./restart-api.sh /path/to/config.json # optional custom config path
```

The script kills any running API process, starts a new one in the background, and waits until `/health` responds. Logs go to `/tmp/dotnet-agents-api.log`.

## Configuration

The API resolves the config path in this order:

1. `--config <path>` CLI argument
2. `DOTNET_AGENTS_CONFIG` environment variable
3. `./config.json` in the working directory

See `samples/config.example.json` for a full example and `samples/config.schema.json` for the JSON Schema.

### Supported providers

| Provider            | Required fields                       |
|---------------------|---------------------------------------|
| `openai`            | `modelName`, `apiKeyEnvVar`           |
| `openai-compatible` | `modelName`, `endpoint`, `apiKeyEnvVar` (optional) |
| `ollama`            | `modelName`, `endpoint`               |

### Built-in tools

`get_current_time`, `echo`, `read_file` (sandboxed via `tools.readFile.sandboxRoot`), `http_get` (allowlisted via `tools.httpGet.allowlist`).

## Endpoints

| Method | Route                                                  |
|--------|--------------------------------------------------------|
| GET    | `/health`                                              |
| GET    | `/api/users`                                           |
| GET    | `/api/agents`                                          |
| GET    | `/api/agents/{agentId}`                                |
| GET    | `/api/users/{userId}/sessions`                         |
| POST   | `/api/users/{userId}/sessions`                         |
| GET    | `/api/users/{userId}/sessions/{sessionId}`             |
| DELETE | `/api/users/{userId}/sessions/{sessionId}`             |
| GET    | `/api/users/{userId}/sessions/{sessionId}/messages`    |
| POST   | `/api/users/{userId}/sessions/{sessionId}/messages`    |

The POST `/messages` endpoint returns `text/event-stream` with `data: {...}\n\n` events of type `delta`, `tool_call`, `tool_result`, `done`, or `error`. OpenAPI spec is served at `/openapi/v1.json`.

## Ports

- API: `http://0.0.0.0:5000`
- Web dev server: `http://0.0.0.0:5173` (proxies `/api`, `/health`, `/openapi` to the API)

Both bind to `0.0.0.0` so other machines on your LAN can connect.

## Console sample

The same `IAgentRuntime` façade can be used directly in-process:

```bash
dotnet run --project samples/ConsoleSample -- --config config.json
```

## Testing

```bash
dotnet test                           # all .NET tests
cd web && npm test                    # Vitest component tests
cd web && npm run test:e2e            # Playwright E2E (requires browsers)
```

To run Playwright tests, first install the browser:

```bash
cd web && npx playwright install --with-deps chromium
```

The Playwright config boots the API and Vite dev server automatically.

## Project layout

```
src/
├── DotnetAgents.Core/        Domain models, config, session store, runtime interfaces
├── DotnetAgents.Tools.BuiltIn/   Built-in tools and registry
├── DotnetAgents.Runtime/     Provider factory, agent executor (Microsoft.Agents.AI)
└── DotnetAgents.Api/         Minimal API, SSE streaming, OpenAPI
web/                          React 18 + Vite 6 + TypeScript UI
samples/
├── ConsoleSample/            In-process façade demo
├── config.example.json       Example configuration
└── config.schema.json        JSON Schema for IDE validation
tests/                        NUnit fixtures for each project
```
