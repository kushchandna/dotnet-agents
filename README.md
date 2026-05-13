# dotnet-agents

A configuration-driven, JSON-backed agent runtime in .NET 10.

> Status: **M1 work-in-progress.** Public APIs, configuration shape, and CLI flags will change.

## Requirements

- .NET 10 SDK
- Node 24 (for the upcoming web UI)

## Build & test

```bash
dotnet build
dotnet test
```

## Layout

```
src/
  DotnetAgents.Core           # config models, abstractions, validation
  DotnetAgents.Tools.BuiltIn  # built-in tools (fs, http, shell, ...)
  DotnetAgents.Runtime        # agent + session host, chat-client wiring
  DotnetAgents.Api            # ASP.NET Core HTTP/SSE surface
samples/
  ConsoleSample               # minimal CLI driver for the runtime
tests/
  DotnetAgents.*.Tests        # NUnit test projects (one per src project)
```
