#!/bin/bash
# Entrypoint wrapper for the CS2 dedicated server container.
#
# Tees all server stdout+stderr to a shared Docker volume so the Fluent Bit
# sidecar can tail it and forward to the OTel Collector — without needing the
# Docker fluentd log driver or any host-side ports.
#
# Output is still written to the container's own stdout so `docker logs` works.
#
# Usage (set via ENTRYPOINT + CMD in Dockerfile):
#   /entrypoint-wrapper.sh <original-startup-command...>

set -euo pipefail

LOG_DIR="${LOG_DIR:-/logs/cs2}"
LOG_FILE="$LOG_DIR/server.log"

mkdir -p "$LOG_DIR"

# Truncate on each startup so the file doesn't grow across restarts.
: > "$LOG_FILE"

echo "[cs2-observability] stdout/stderr → $LOG_FILE"

# Run the original startup command.
# tee writes each line to both the shared log file and the container stdout.
# PIPESTATUS[0] propagates the server's own exit code.
"$@" 2>&1 | tee -a "$LOG_FILE"
exit "${PIPESTATUS[0]}"
