# Thiết lập môi trường phát triển Windows x64

SDK local đã cài là .NET 10.0.404 x64, bản ổn định của kênh LTS. Ngày 21/11/2026, [metadata Microsoft](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) ghi nhận bản này và ZIP `win-x64` với SHA-512:

`330ddd2665b43cd2e428216a0d8ea1ac7bb19b5434fdc530e62010dc3b52823a620bf95489d628e1ed6f5122401793ee4887cca4a019122361a1231497f632ee`

Git 2.47.1 và VS Code 1.146.0 đã có trên máy khảo sát. Không cần cài lại chúng.

## Cài SDK

Chạy tại root repository trong PowerShell. Tải ZIP chính thức vào thư mục tạm; nếu đã có ZIP từ nguồn này tại `$zip`, có thể bỏ qua bước tải. Bước kiểm hash và giải nén đã được kiểm với ZIP có sẵn; bước tải cần kết nối mạng.

```powershell
$zip = Join-Path $env:TEMP 'taxxManager-sdk-10.0.404-win-x64.zip'
$url = 'https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-win-x64.zip'
curl.exe --fail --location --output $zip $url
if ($LASTEXITCODE -ne 0) { throw 'Tải SDK thất bại.' }
```

```powershell
$zip = Join-Path $env:TEMP 'taxxManager-sdk-10.0.404-win-x64.zip'
$sdkDir = Join-Path (Get-Location).Path '.dotnet'
$expected = '330ddd2665b43cd2e428216a0d8ea1ac7bb19b5434fdc530e62010dc3b52823a620bf95489d628e1ed6f5122401793ee4887cca4a019122361a1231497f632ee'
if ((Get-FileHash -LiteralPath $zip -Algorithm SHA512).Hash -ne $expected) { throw 'SHA-512 không khớp; không giải nén.' }
if (Test-Path $sdkDir) { throw "Đã tồn tại: $sdkDir; kiểm tra trước khi tiếp tục." }
Expand-Archive -LiteralPath $zip -DestinationPath $sdkDir
& (Join-Path $sdkDir 'dotnet.exe') --info
& (Join-Path $sdkDir 'dotnet.exe') --list-sdks
```

ZIP đã dùng có SHA-512 khớp metadata. SDK nằm trong `.dotnet`, được Git bỏ qua. Không cài đè SDK hệ thống hoặc sửa PATH lâu dài.

## Kích hoạt theo phiên

Trong terminal PowerShell của VS Code hoặc Codex, từ root clone:

```powershell
. ./scripts/dev/activate-dotnet.ps1
dotnet --version
. ./scripts/dev/activate-dotnet.ps1 -Deactivate
```

Helper xác định repository theo vị trí script, nên cũng có thể dot-source bằng đường dẫn đầy đủ từ thư mục khác. Lệnh kích hoạt chỉ thay đổi PATH và DOTNET_ROOT của terminal hiện tại; `-Deactivate` khôi phục giá trị trước lần kích hoạt đầu, hoặc đóng terminal để bỏ thay đổi. Từ root, `dotnet --version` trả `10.0.404` với bản cài hiện tại. `global.json` pin phiên bản đó, tắt prerelease và cho phép `latestPatch` trong feature band 10.0.4xx; nếu cài thêm patch cao hơn trong band, chính sách có thể chọn patch đó.

## Quyết định solution

Bước 1.1 sau này sẽ dùng `TaxManager.sln`. Bước 0 chưa tạo solution hay project.
