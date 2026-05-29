$ErrorActionPreference = 'Stop'

$required = (Get-Content global.json | ConvertFrom-Json).sdk.version
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -or (dotnet --version) -ne $required) {
    Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
    .\dotnet-install.ps1 -Version $required
    $env:PATH = "$HOME\.dotnet;$env:PATH"
}

# Pack the local CLI so `dotnet tool restore` can resolve dotnet-quality via the
# local-artifacts feed configured in NuGet.config.
dotnet pack tools/Quality.Cli/Quality.Cli.csproj -o .\artifacts -c Release

dotnet tool restore
dotnet quality install --into (Get-Location)
