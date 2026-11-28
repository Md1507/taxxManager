<#
.SYNOPSIS
Chạy bản thử TaxManager trên máy với một lệnh.
.EXAMPLE
.\run.ps1
.EXAMPLE
.\run.ps1 -TrustCertificate
.EXAMPLE
.\run.ps1 -NoAccounts
.EXAMPLE
.\run.ps1 -Store 'C:\Users\you\AppData\Local\Temp\taxmanager-web-demo-...'
#>
[CmdletBinding()]
param(
    [switch]$NoAccounts,
    [switch]$TrustCertificate,
    [string]$Store
)

$ErrorActionPreference = 'Stop'
$savedEnvironment = @{}
foreach ($name in @('PATH', 'DOTNET_ROOT', 'NUGET_PACKAGES', 'DOTNET_CLI_HOME', 'DOTNET_ENVIRONMENT', 'ASPNETCORE_ENVIRONMENT')) {
    $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

Push-Location $PSScriptRoot
try {
    if ($NoAccounts -and $TrustCertificate) {
        throw 'Chế độ không tài khoản dùng HTTP và không cần thiết lập chứng chỉ.'
    }
    $localDotnet = Join-Path $PSScriptRoot '.dotnet/dotnet.exe'
    if (Test-Path -LiteralPath $localDotnet) {
        $dotnet = $localDotnet
        $env:DOTNET_ROOT = Join-Path $PSScriptRoot '.dotnet'
        $env:PATH = "$env:DOTNET_ROOT$([IO.Path]::PathSeparator)$env:PATH"
    } else {
        $systemDotnet = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $systemDotnet) {
            throw 'Chưa có .NET SDK. Cài SDK theo global.json hoặc xem docs/decisions/0001-dev-setup.md rồi chạy lại.'
        }
        $dotnet = $systemDotnet.Source
    }

    $env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.dotnet/nuget-packages'
    $env:DOTNET_CLI_HOME = Join-Path $env:TEMP 'taxmanager-cli'
    $env:DOTNET_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $sdkVersion = & $dotnet --version
    if ($LASTEXITCODE -ne 0) {
        throw 'Không tìm thấy SDK phù hợp với global.json. Xem docs/decisions/0001-dev-setup.md để thiết lập.'
    }
    Write-Host "Dùng .NET SDK $sdkVersion. Chỉ nhập dữ liệu giả trong bản thử này."

    $applicationArguments = @('--SyntheticDemo=true')
    if ($NoAccounts) {
        $applicationArguments += '--SyntheticAccounts=false'
        $address = 'http://127.0.0.1:5088/'
    } else {
        if ($TrustCertificate) {
            & $dotnet dev-certs https --trust
            if ($LASTEXITCODE -ne 0) { throw 'Chưa thiết lập được chứng chỉ HTTPS development.' }
        }
        & $dotnet dev-certs https --check --trust *> $null
        if ($LASTEXITCODE -ne 0) {
            throw 'Chưa có chứng chỉ HTTPS development được tin cậy. Chạy .\run.ps1 -TrustCertificate và xác nhận khi hệ điều hành hỏi.'
        }
        $applicationArguments += '--SyntheticAccounts=true'
        $address = 'https://localhost:5088/'
    }
    if ($Store) { $applicationArguments += "--SyntheticStore=$Store" }

    Write-Host "Sau khi ứng dụng khởi động, mở $address"
    Write-Host 'Nhấn Ctrl+C để dừng. Các gói phụ thuộc được tự khôi phục nếu cần.'
    & $dotnet run --project src/TaxManager.Web --no-launch-profile --disable-build-servers -- @applicationArguments
    if ($LASTEXITCODE -ne 0) { throw "Ứng dụng dừng với mã lỗi $LASTEXITCODE." }
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    foreach ($name in $savedEnvironment.Keys) {
        if ($null -eq $savedEnvironment[$name]) {
            Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
        } else {
            [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
        }
    }
    Pop-Location
}
