# Dotnet Agents

A configuration-driven, JSON-backed agent runtime in .NET 10.

## Requirements

- .NET 10 SDK
- Node 24 (for the web UI)

## Build & test

```bash
dotnet build
dotnet test
```

## Run the API

```bash
dotnet run --project src/DotnetAgents.Api -- --config /path/to/config.json
```

API listens on `http://0.0.0.0:5000`.

Config path resolution: `--config <path>` CLI > `DOTNET_AGENTS_CONFIG` env > `./config.json`.

## Run the web UI

> Coming soon.

```bash
cd web
npm install
npm run dev
```

Web UI listens on `http://0.0.0.0:5173`.

## What's available

- Configuration-driven agent + model + tool definitions (JSON)
- Multi-provider model support: OpenAI, OpenAI-compatible, Ollama
- Agentic execution via Microsoft.Agents.AI (`ChatClientAgent`, tool calling loop, session history)
- File-backed session persistence per user
- Built-in tools: echo, get_current_time, read_file, http_get
- REST API: agents, users endpoints
- SSE message streaming endpoint (in progress)
- Web UI (in progress)
