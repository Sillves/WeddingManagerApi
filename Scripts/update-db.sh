#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$script_dir/.." && pwd)"

connection_string="${1:-${DatabaseSettings__ConnectionString:-}}"

if [[ -z "$connection_string" ]]; then
  appsettings="$root/WeddingManager.Web/appsettings.Development.json"
  if [[ ! -f "$appsettings" ]]; then
    echo "Could not find appsettings.Development.json at $appsettings" >&2
    exit 1
  fi

  connection_string="$(python3 - <<'PY' "$appsettings"
import json
import sys

with open(sys.argv[1], "r", encoding="utf-8") as f:
    data = json.load(f)

print(data.get("DatabaseSettings", {}).get("ConnectionString", ""))
PY
)"
fi

if [[ -z "$connection_string" ]]; then
  echo "No connection string found. Pass one as the first argument or set DatabaseSettings__ConnectionString." >&2
  exit 1
fi

export DatabaseSettings__ConnectionString="$connection_string"

cd "$root"
dotnet ef database update --project WeddingManager.Infrastructure --startup-project WeddingManager.Web
