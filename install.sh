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

# Pack the local CLI so `dotnet tool restore` can resolve dotnet-quality via the
# local-artifacts feed configured in NuGet.config.
dotnet pack tools/Quality.Cli/Quality.Cli.csproj -o ./artifacts -c Release

dotnet tool install --local dotnet-quality --add-source ./artifacts 2>/dev/null || true
dotnet tool restore
dotnet quality install --into "$(pwd)"
