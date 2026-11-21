# Bảo hiểm và chính sách số thập phân — S13

Phạm vi tự tính là NLĐ Việt Nam, lương do doanh nghiệp quyết định, hợp đồng lao động thông thường, làm đủ tháng và tham gia cả ba bảo hiểm trong năm 2026. Caller xác nhận đối tượng và chọn vùng I–IV; không suy từ gross, tuổi, quốc tịch hay địa chỉ. Lương đóng là đầu vào riêng, không mặc định bằng gross. Các nhóm miễn/không tham gia, không đủ tháng, nhóm khác và năm khác chưa được hỗ trợ; API nhập bảo hiểm bằng tay S11 vẫn dùng được.

```csharp
var provider = TaxRuleSet.CreateVietnam2026SalaryProvider(InsuranceRegion.II);
var input = new InsuranceInput(InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.II, 20_000_000m);
var result = InsuredMonthlySalaryCalculator.Calculate(40_000_000m, input, 1, 2026, 7, provider);
```

Provider chọn tường minh theo tháng, với hai snapshot tháng 1–6 và 7–12; kỳ thuế và khoảng bảo hiểm được lưu riêng. Không có fallback. Input dưới mức tối thiểu vùng bị từ chối; đây là giới hạn phạm vi đủ tháng, không phải khẳng định sàn bảo hiểm chung cho mọi nhóm. BHYT dùng base BHXH sau cap, BHTN dùng base gốc với cap riêng. Kết quả giữ input, rule, policy, từng căn cứ rate/floor/cap, base áp dụng, số raw/applied và tổng; input tự khai có căn cứ Missing.

Rule có sàn BHTN riêng bị từ chối khi cấu hình: phạm vi này chưa hỗ trợ sàn đó và calculator không tự nâng base. Guard đủ tháng vẫn được kiểm riêng.

Theo lựa chọn người dùng, NoRounding giữ nguyên decimal cho từng khoản, tổng và thuế cuối; raw bằng applied, không làm tròn bậc thuế. Đây là quyết định tham khảo của dự án, chưa xác minh là quy tắc pháp lý. Không mặc định policy khi thiếu. Dữ liệu luật và policy đều Unverified; các nguồn sau chưa hoàn tất rà soát văn bản gốc, sửa đổi và phạm vi áp dụng, vì vậy kết quả không phải kết luận pháp lý.

| Dữ liệu tham khảo | Giá trị | Căn cứ |
| --- | --- | --- |
| BHXH NLĐ | 8%, floor tham chiếu, cap 20 lần tham chiếu | [Luật 41/2024, Điều 31, 33, 141](https://xaydungchinhsach.chinhphu.vn/toan-van-luat-so-41-2024-qh15-bao-hiem-xa-hoi-119240723163650489.htm) |
| BHYT NLĐ | 1,5%, base BHXH đã giới hạn | [NĐ 188/2025, Điều 6](https://datafiles.chinhphu.vn/cpp/files/vbpq/2025/7/188-ndcp.signed.pdf) |
| BHTN NLĐ | 1%, cap 20 lần tối thiểu vùng | Tỷ lệ: [NĐ 374/2025, Điều 4 khoản 1](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/01/374-1.signed.pdf); công thức cap: [Luật 74/2025, Điều 34 khoản 2](https://xaydungchinhsach.chinhphu.vn/toan-van-luat-viec-lam-119250711173403835.htm); mức vùng: NĐ 293/2025, Điều 3 khoản 1 bên dưới. |
| Tham chiếu tháng 1–6 | 2.340.000; cap BHXH/BHYT 46.800.000 | [NĐ 73/2024, Điều 3](https://xaydungchinhsach.chinhphu.vn/toan-van-nghi-dinh-73-2024-nd-cp-quy-dinh-muc-luong-co-so-va-che-do-tien-thuong-119240701121328994.htm) |
| Tham chiếu tháng 7–12 | 2.530.000; cap BHXH/BHYT 50.600.000 | [NĐ 161/2026, Điều 3](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/5/161-ndcp.signed.pdf) |
| Tối thiểu vùng I–IV | 5.310.000 / 4.730.000 / 4.140.000 / 3.700.000 | [NĐ 293/2025](https://xaydungchinhsach.chinhphu.vn/nghi-dinh-so-293-2025-nd-cp-quy-dinh-muc-luong-toi-thieu-doi-voi-nguoi-lao-dong-lam-viec-theo-hop-dong-lao-dong-119251110172808433.htm) |

Cap BHTN tính bằng 20 lần tối thiểu vùng: 106.200.000 / 94.600.000 / 82.800.000 / 74.000.000; đây là phép tính từ các nguồn, không phải bảng trích nguyên văn. NĐ 399/2026 áp dụng từ 2027 không được dùng cho kỳ 2026. Seed thuế 2026 vẫn là dữ liệu tham khảo S11, chưa được xác minh đầy đủ.

Ví dụ tính độc lập: gross 40.000.000, base 20.000.000, vùng II tháng 7, một người phụ thuộc: BHXH 1.600.000, BHYT 300.000, BHTN 200.000, tổng 2.100.000; TNTT 16.200.000; thuế 500.000 + 6.200.000 × 10% = 1.120.000. Base 10.000.000,5 cho ba khoản 800.000,04 / 150.000,0075 / 100.000,005; không làm tròn tổng hay từng khoản.

Đối số null, giá trị âm hoặc vùng/tháng sai bị từ chối; không đúng phạm vi, dưới guard hoặc tháng thiếu báo lỗi, snapshot sai vùng/kỳ báo lỗi, thiếu cấu hình báo lỗi. Decimal tràn phát sinh OverflowException. Provider vẫn kiểm kỳ chồng và giữ gap; không sửa PitCalculator hoặc API nhập tay.

Kiểm thử bằng SDK 10.0.404 và cache `.dotnet/nuget-packages`, `DOTNET_CLI_HOME`/`APPDATA` trong thư mục tạm:

```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location).Path '.dotnet/nuget-packages'
& ./.dotnet/dotnet.exe test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore --disable-build-servers -m:1
& ./.dotnet/dotnet.exe test TaxManager.sln --no-restore --disable-build-servers -m:1
```

Cả hai lệnh ngày 21/11/2026 exit 0: 55 passed, 0 failed, 0 skipped; gồm 39 ca cũ. Trước đó test hẹp hai kiểm thử sàn BHTN và metadata hai snapshot qua 3 ca. Đây là kiểm thử số học và hợp đồng kỹ thuật, không xác minh pháp lý. Review độc lập của bước tiếp theo vẫn cần thực hiện.
