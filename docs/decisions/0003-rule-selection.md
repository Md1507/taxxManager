# Chọn bộ quy tắc theo kỳ thuế — S12

`TaxRuleProvider` nhận danh sách `TaxRuleRegistration(ruleSet, periodFrom, periodTo)` với hai đầu bao gồm, bắt đầu ngày đầu tháng và kết thúc ngày cuối tháng. Caller đăng ký tường minh, ví dụ:

```csharp
var provider = new TaxRuleProvider([
    new(TaxRuleSet.Vietnam2025, new(2025, 1, 1), new(2025, 12, 31)),
    new(TaxRuleSet.Vietnam2026, new(2026, 1, 1), new(2026, 12, 31))]);
var rule = provider.GetForDate(new DateOnly(2026, 6, 30));
var result = MonthlySalaryCalculator.Calculate(30_000_000m, 3_150_000m, 0, rule);
```

`GetForDate` coi ngày là ngày thuộc tháng tính thuế, không phải ngày trả lương hoặc ngày hiệu lực văn bản; `GetForMonth(year, month)` chọn tháng; `GetForYear(year)` yêu cầu đủ 12 tháng cùng một instance snapshot. Khoảng không được trùng hoặc chồng; khoảng rời được phép nhưng không tự lấp khoảng trống. Tháng thiếu báo `NotSupportedException`, năm có nhiều snapshot báo `InvalidOperationException` và hướng chọn tháng. Năm hoặc tháng ngoài phạm vi báo `ArgumentOutOfRangeException`; danh sách hoặc rule null báo `ArgumentNullException`, phần tử null, khoảng sai hoặc chồng báo `ArgumentException`. Danh sách được sao chép; rule và căn cứ là snapshot bất biến. Không suy kỳ từ `FromYear` hay `EffectiveFrom/To`, không fallback năm gần nhất. Calculator S11 và tính từng phần không đổi.

## Nguồn và giới hạn

Seed 2025 dùng bảng 7 bậc do người dùng cung cấp và dẫn TT111/2013/TT-BTC; chưa đối chiếu đầy đủ văn bản gốc nên không gắn URL hay điều khoản chưa kiểm chứng. Giảm trừ 11 triệu và 4,4 triệu đồng/tháng theo [bài Báo Chính phủ được người dùng cung cấp](https://baochinhphu.vn/cai-cach-thue-thu-nhap-ca-nhan-nguoi-lao-dong-co-the-co-them-hang-chuc-trieu-dong-moi-nam-102261113121825969.htm); bài này không phải nguồn đầy đủ cho bảng bậc. Căn cứ TNTT chưa được cung cấp riêng nên là `Missing`. Các căn cứ đã có đều `Unverified`, không có ngày xác minh. Seed 2026 giữ nguyên trạng thái tham khảo `Unverified` của S11. Những dữ liệu này chưa được xác minh pháp lý đầy đủ.

Đăng ký kỳ 2026 từ tháng 1 là quyết định theo kỳ thuế, không suy từ ngày hiệu lực chung 01/07. [Hướng dẫn quyết toán](https://xaydungchinhsach.chinhphu.vn/huong-dan-quyet-toan-thue-thu-nhap-ca-nhan-doi-voi-thu-nhap-tu-tien-luong-tien-cong-119260306092819051.htm) và các nguồn Luật 109, TT87 cùng NĐ398/NĐ141 được ghi tại [quyết định S11](0002-core-monthly-tax.md); các nghị định về kinh doanh/CIT không mở rộng vào engine lương. Không tự tính bảo hiểm, quyết toán năm hoặc áp dụng khoản ngoài phạm vi.

## Đối chứng và kiểm thử

Với gross/bảo hiểm/phụ thuộc lần lượt 30 triệu/3,15 triệu/0; 30 triệu/5 triệu/1; 45 triệu/5 triệu/2, TNTT và thuế 2025 lần lượt là 15,85 triệu/1.627.500; 9,6 triệu/710.000; 20,2 triệu/2.390.000. Tính thuế tương ứng: 250.000 + 500.000 + 5.850.000 × 15%; 250.000 + 4.600.000 × 10%; 250.000 + 500.000 + 1.200.000 + 2.200.000 × 20%. Cùng ba đầu vào, TNTT/thuế 2026 lần lượt 11,35 triệu/635.000; 3,3 triệu/165.000; 12,1 triệu/710.000. Thuế tương ứng: 500.000 + 1.350.000 × 10%; 3.300.000 × 5%; 500.000 + 2.100.000 × 10%. Không làm tròn. Các ví dụ kiểm số học, không xác minh pháp lý.

Ngày 21/11/2026, SDK cục bộ 10.0.404, cache `.dotnet/nuget-packages`, `DOTNET_CLI_HOME` và `APPDATA` trong thư mục tạm:

```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location).Path '.dotnet/nuget-packages'
& ./.dotnet/dotnet.exe test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore --disable-build-servers -m:1
& ./.dotnet/dotnet.exe test TaxManager.sln --no-restore --disable-build-servers -m:1
```

Cả hai lệnh exit 0: 39 passed, 0 failed, 0 skipped, gồm 27 case S11 không sửa. Test bao phủ trần ±1 đồng, biên năm và giữa năm, khoảng trống/chồng, ngày cực trị, snapshot và căn cứ. Cần review thay đổi và tiếp tục đối chiếu pháp lý trước khi coi dữ liệu là đã xác minh.
