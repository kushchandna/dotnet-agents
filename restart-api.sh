#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG="${1:-$SCRIPT_DIR/samples/config.json}"

echo "Stopping existing API..."
pkill -f "DotnetAgents.Api" 2>/dev/null || true
sleep 1

echo "Starting API with config: $CONFIG"
nohup dotnet run --project "$SCRIPT_DIR/src/DotnetAgents.Api" --no-launch-profile -- --config "$CONFIG" \
  > /tmp/dotnet-agents-api.log 2>&1 &

echo "Waiting for API to be ready..."
for i in $(seq 1 20); do
  if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
    echo "API ready at http://localhost:5000"
    exit 0
  fi
  sleep 1
done

echo "ERROR: API did not start in 20s. Check /tmp/dotnet-agents-api.log" >&2
exit 1
