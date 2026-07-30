#!/usr/bin/env bash
set -uo pipefail

API_PORT="${API_PORT:-5168}"
WEB_PORT="${WEB_PORT:-5000}"
PWA_PORT="${PWA_PORT:-5050}"
BUILD_ONLY=false
RUN_PWA=false

usage() {
    cat <<EOF
Usage: $(basename "$0") [OPTIONS]

Starts all StockScanTool applications.

Options:
  --build-only      Build the solution without running any apps
  --with-pwa        Also start the ScannerPwa app (default: off)
  --api-port PORT   API port (default: 5168)
  --web-port PORT   Web admin port (default: 5000)
  --pwa-port PORT   ScannerPwa port (default: 5050)
  -h, --help        Show this help message
EOF
}

while [[ $# -gt 0 ]]; do
    case $1 in
        --build-only) BUILD_ONLY=true; shift ;;
        --with-pwa)   RUN_PWA=true; shift ;;
        --api-port)   API_PORT="$2"; shift 2 ;;
        --web-port)   WEB_PORT="$2"; shift 2 ;;
        --pwa-port)   PWA_PORT="$2"; shift 2 ;;
        -h|--help)    usage; exit 0 ;;
        *)            echo "Unknown option: $1"; usage; exit 1 ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PIDS=()

cleanup() {
    echo ""
    echo "Shutting down..."
    for pid in "${PIDS[@]}"; do
        kill "$pid" 2>/dev/null || true
    done
    for pid in "${PIDS[@]}"; do
        wait "$pid" 2>/dev/null || true
    done
    echo "All apps stopped."
}

trap cleanup EXIT INT TERM

log() {
    local color="$1"; shift
    echo -e "\033[1;${color}m[$(date +%H:%M:%S)] $*\033[0m"
}

kill_port() {
    local port="$1"
    local pids
    pids=$(lsof -ti :"$port" 2>/dev/null || true)
    if [[ -n "$pids" ]]; then
        log 6 "Killing stale process(es) on port ${port}..."
        echo "$pids" | xargs kill -9 2>/dev/null || true
        sleep 1
    fi
}

# ── Kill stale ports ───────────────────────────────────
kill_port "$API_PORT"
kill_port "$WEB_PORT"
if [[ "$RUN_PWA" == true ]]; then
    kill_port "$PWA_PORT"
fi

# ── Build ──────────────────────────────────────────────
log 6 "Building solution..."
dotnet build "$SCRIPT_DIR/StockScanTool.sln" -c Debug --verbosity quiet 2>&1
if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    log 1 "Build FAILED"
    exit 1
fi
log 2 "Build succeeded."

# ── Validate build output ────────────────────────────
log 6 "Validating build output..."
WEB_FRAMEWORK="$SCRIPT_DIR/src/StockScanTool.Web/bin/Debug/net10.0/wwwroot/_framework"
if [ ! -f "$WEB_FRAMEWORK/dotnet.js" ] || [ ! -s "$WEB_FRAMEWORK/dotnet.js" ]; then
    log 1 "Web Admin build output is missing or empty (dotnet.js)"
    exit 1
fi
if ! ls "$WEB_FRAMEWORK"/StockScanTool.Web.*.wasm 2>/dev/null | grep -q .; then
    log 1 "Web Admin build output is missing (StockScanTool.Web.*.wasm)"
    exit 1
fi
API_OUTPUT="$SCRIPT_DIR/src/StockScanTool.Api/bin/Debug/net10.0/StockScanTool.Api.dll"
if [ ! -f "$API_OUTPUT" ]; then
    log 1 "API build output is missing ($API_OUTPUT)"
    exit 1
fi
log 2 "Build output validated."

if [[ "$BUILD_ONLY" == true ]]; then
    log 6 "Build-only mode. Exiting."
    exit 0
fi

# ── API ────────────────────────────────────────────────
log 5 "Starting API on http://0.0.0.0:${API_PORT} ..."
ASPNETCORE_ENVIRONMENT=Development dotnet run \
    --project "$SCRIPT_DIR/src/StockScanTool.Api" \
    -c Debug \
    --no-build \
    --urls "http://0.0.0.0:${API_PORT}" \
    > /tmp/stockscan-api.log 2>&1 &
PIDS+=($!)
disown
log 2 "API started (PID ${PIDS[-1]})"

# Wait for API to be ready
log 6 "Waiting for API to be ready..."
for i in $(seq 1 30); do
    if curl -sf "http://localhost:${API_PORT}/openapi/v1.json" > /dev/null 2>&1; then
        log 2 "API is ready."
        break
    fi
    if ! kill -0 "${PIDS[-1]}" 2>/dev/null; then
        log 1 "API process exited unexpectedly. Check /tmp/stockscan-api.log"
        tail -20 /tmp/stockscan-api.log
        exit 1
    fi
    sleep 1
done

# ── Web Admin ──────────────────────────────────────────
log 3 "Starting Web Admin on http://localhost:${WEB_PORT} ..."
ASPNETCORE_URLS="http://localhost:${WEB_PORT}" \
ASPNETCORE_ENVIRONMENT=Development \
dotnet run \
    --project "$SCRIPT_DIR/src/StockScanTool.Web" \
    -c Debug \
    --no-build \
    > /tmp/stockscan-web.log 2>&1 &
PIDS+=($!)
disown
log 2 "Web Admin started (PID ${PIDS[-1]})"

# Wait for Web to be ready
log 6 "Waiting for Web Admin to be ready..."
for i in $(seq 1 20); do
    if curl -sf "http://localhost:${WEB_PORT}/" > /dev/null 2>&1; then
        log 2 "Web Admin is ready."
        break
    fi
    if ! kill -0 "${PIDS[-1]}" 2>/dev/null; then
        log 1 "Web Admin process exited unexpectedly. Check /tmp/stockscan-web.log"
        tail -20 /tmp/stockscan-web.log
        exit 1
    fi
    sleep 1
done

# ── Scanner PWA (optional) ─────────────────────────────
if [[ "$RUN_PWA" == true ]]; then
    log 4 "Starting Scanner PWA on http://localhost:${PWA_PORT} ..."
    ASPNETCORE_URLS="http://localhost:${PWA_PORT}" \
    ASPNETCORE_ENVIRONMENT=Development \
    dotnet run \
        --project "$SCRIPT_DIR/src/StockScanTool.ScannerPwa" \
        -c Debug \
        --no-build \
        > /tmp/stockscan-pwa.log 2>&1 &
    PIDS+=($!)
    disown
    log 2 "Scanner PWA started (PID ${PIDS[-1]})"

    log 6 "Waiting for Scanner PWA to be ready..."
    for i in $(seq 1 20); do
        if curl -sf "http://localhost:${PWA_PORT}/" > /dev/null 2>&1; then
            log 2 "Scanner PWA is ready."
            break
        fi
        if ! kill -0 "${PIDS[-1]}" 2>/dev/null; then
            log 1 "Scanner PWA process exited unexpectedly. Check /tmp/stockscan-pwa.log"
            tail -20 /tmp/stockscan-pwa.log
            exit 1
        fi
        sleep 1
    done
fi

# ── Summary ────────────────────────────────────────────
echo ""
log 2 "════════════════════════════════════════════════"
log 2 "  StockScanTool is running!"
log 2 "════════════════════════════════════════════════"
echo ""
log 5 "  API:        http://localhost:${API_PORT}"
log 5 "  Swagger:    http://localhost:${API_PORT}/swagger"
log 3 "  Web Admin:  http://localhost:${WEB_PORT}"
if [[ "$RUN_PWA" == true ]]; then
    log 4 "  ScannerPWA: http://localhost:${PWA_PORT}"
fi
echo ""
log 6 "  Logs: /tmp/stockscan-*.log"
log 6 "  Press Ctrl+C to stop all apps."
echo ""

# ── Keep alive ─────────────────────────────────────────
while true; do
    for pid in "${PIDS[@]}"; do
        if ! kill -0 "$pid" 2>/dev/null; then
            log 1 "Process $pid exited unexpectedly."
            exit 1
        fi
    done
    sleep 5
done
