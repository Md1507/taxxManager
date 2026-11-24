$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repoRoot
try {
    . (Join-Path $PSScriptRoot 'activate-dotnet.ps1')
    $env:NUGET_PACKAGES = Join-Path $repoRoot '.dotnet/nuget-packages'
    $env:DOTNET_CLI_HOME = Join-Path $env:TEMP 'taxmanager-cli'
    # Bint gives each verification a fresh TEMP/CLI home, including a fresh local-tool resolver cache.
    dotnet tool restore --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet build TaxManager.sln --no-restore --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    foreach ($project in @('TaxManager.Core.Tests', 'TaxManager.Data.Tests', 'TaxManager.Web.Tests')) {
        dotnet test "tests/$project/$project.csproj" --no-build --no-restore --disable-build-servers -m:1
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    dotnet ef migrations has-pending-model-changes --no-build --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}
