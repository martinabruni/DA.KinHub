#!/usr/bin/env bash
#
# Fails if any productive source references the Model Context Protocol (MCP).
# The Kin List / auth migration removed MCP entirely (endpoints, transport,
# handlers, tools, packages, configuration and tests); this gate keeps it out.
#
# Documentation and historical planning notes (docs/, plans/) are allowed to
# mention MCP, so they are excluded. This script excludes itself as well.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

SCAN_DIRS=(src ops scripts .github)
PATTERN='\bmcp\b|modelcontextprotocol|model-context-protocol'

matches="$(
  grep -rInE "$PATTERN" "${SCAN_DIRS[@]}" \
    --binary-files=without-match \
    --exclude-dir=bin \
    --exclude-dir=obj \
    --exclude-dir=dist \
    --exclude-dir=node_modules \
    --exclude='verify-no-mcp.sh' \
    -i 2>/dev/null \
    | grep -v 'verify-no-mcp' || true
)"

if [ -n "$matches" ]; then
  echo "ERROR: Found productive references to MCP. They must be removed:" >&2
  echo "$matches" >&2
  exit 1
fi

echo "No productive MCP references found."
