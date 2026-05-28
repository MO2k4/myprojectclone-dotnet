$ErrorActionPreference = 'Stop'

$required = (Get-Content global.json | ConvertFrom-Json).sdk.version
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -or (dotnet --version) -ne $required) {
    Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
    .\dotnet-install.ps1 -Version $required
    $env:PATH = "$HOME\.dotnet;$env:PATH"
}

if (-not (Test-Path .config/dotnet-tools.json)) {
    dotnet new tool-manifest --force
}

dotnet tool install --local dotnet-quality --add-source .\artifacts 2>$null
dotnet tool restore
dotnet quality install --into (Get-Location)
