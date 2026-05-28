#!/usr/bin/env bash
set -euo pipefail

required_sdk=$(jq -r '.sdk.version' global.json)
if ! command -v dotnet >/dev/null || [ "$(dotnet --version)" != "$required_sdk" ]; then
  echo "Installing .NET SDK $required_sdk via dotnet-install..."
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version "$required_sdk"
  export PATH="$HOME/.dotnet:$PATH"
fi

if [ ! -f .config/dotnet-tools.json ]; then
  dotnet new tool-manifest --force
fi

dotnet tool install --local dotnet-quality --add-source ./artifacts 2>/dev/null || true
dotnet tool restore
dotnet quality install --into "$(pwd)"
