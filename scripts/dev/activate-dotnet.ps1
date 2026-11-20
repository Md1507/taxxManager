param([switch]$Deactivate)

if ($Deactivate) {
    if ($null -ne $global:TaxManagerDotnetEnvironment) {
        $env:PATH = $global:TaxManagerDotnetEnvironment.Path
        $env:DOTNET_ROOT = $global:TaxManagerDotnetEnvironment.DotnetRoot
        Remove-Variable TaxManagerDotnetEnvironment -Scope Global
        Write-Host 'Đã khôi phục môi trường của phiên.'
    }
    return
}

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$dotnetDir = Join-Path $root '.dotnet'
if (-not (Test-Path (Join-Path $dotnetDir 'dotnet.exe')) -or
    -not (Test-Path (Join-Path $dotnetDir 'sdk/10.0.404'))) {
    Write-Error "Không tìm thấy SDK 10.0.404 tại $dotnetDir. Xem docs/decisions/0001-dev-setup.md để cài."
    return
}

if ($null -eq $global:TaxManagerDotnetEnvironment) {
    $global:TaxManagerDotnetEnvironment = @{
        Path = $env:PATH
        DotnetRoot = $env:DOTNET_ROOT
    }
}
$env:PATH = "$dotnetDir$([IO.Path]::PathSeparator)$($global:TaxManagerDotnetEnvironment.Path)"
$env:DOTNET_ROOT = $dotnetDir
Write-Host "Đã kích hoạt SDK tại $dotnetDir cho phiên này."
