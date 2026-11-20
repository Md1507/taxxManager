# Lõi tính thuế lương tháng — S11

`TaxRuleSet.Vietnam2026` chỉ chứa dữ liệu tham khảo từ PLAN, **CHƯA XÁC MINH pháp lý**. `FromYear = 2026` là nhãn dữ liệu, không xác định ngày hiệu lực hay chuyển tiếp. Chưa hoàn tất đối chiếu văn bản gốc, sửa đổi hoặc xác nhận của người dùng. Không dùng kết quả như kết luận pháp lý. Không có Web, DB, chọn luật tự động, tính tỷ lệ bảo hiểm hay quyết toán năm.

Các nguồn điều phối cung cấp để tiếp tục đối chiếu, chưa được dùng để xác nhận seed: [Luật 109/2025/QH15](https://vanban.chinhphu.vn/?classid=1&docid=216495&orggroupid=1&pageid=27160), [hướng dẫn quyết toán](https://xaydungchinhsach.chinhphu.vn/huong-dan-quyet-toan-thue-thu-nhap-ca-nhan-doi-voi-thu-nhap-tu-tien-luong-tien-cong-119260306092819051.htm), [biểu thuế tham khảo](https://thuvienphapluat.vn/phap-luat-doanh-nghiep/bai-viet/bieu-thue-thu-nhap-ca-nhan-2026-va-cong-thuc-tinh-thue-theo-bieu-thue-5-bac-22757.html), [giảm trừ tham khảo](https://thuvienphapluat.vn/phap-luat-doanh-nghiep/bai-viet/chi-tiet-cac-khoan-giam-tru-khi-tinh-thue-tncn-2027-giam-tru-gia-canh-y-te-giao-duc-23549.html). Không đưa các khoản 2027 vào engine.

Người dùng đã cung cấp thêm ba PDF để đối chiếu; các nguồn này chưa hoàn tất bước xác minh seed:

- `398+2026+ND+CP.pdf`: Nghị định 398/2026/NĐ-CP ngày 29/10/2026 ([trang văn bản](https://chinhphu.vn/?docid=220138&pageid=27160), [toàn văn](https://xaydungchinhsach.chinhphu.vn/toan-van-nghi-dinh-398-2026-nd-cp-ve-giam-thue-thu-nhap-ca-nhan-thue-thu-nhap-doanh-nghiep-doi-voi-ca-nhan-va-doanh-nghiep-119261031044659307.htm)). Điều 2 giảm 30% PIT cho thu nhập kinh doanh thuộc phạm vi quy định; Điều 3 liên quan CIT doanh nghiệp. Không áp dụng mức giảm này cho tiền lương trong S11.
- `87_03072026_182404.pdf`: Thông tư 87/2026/TT-BTC ngày 30/06/2026 ([trang văn bản](https://chinhphu.vn/?classid=1&docid=218772&orggroupid=4&pageid=27160)). Theo phần PDF đã đọc, Điều 6 quy định hiệu lực từ 01/07/2026 và thay thế Thông tư 111/2013/TT-BTC; cần tiếp tục đối chiếu quy định tiền lương, người phụ thuộc, kỳ áp dụng và các sửa đổi trước khi đưa vào dữ liệu sản phẩm. Điều kiện người phụ thuộc không đồng nhất cho mọi nhóm; engine chưa tự đánh giá điều kiện.
- `141-ndcp.signed.pdf`: Nghị định 141/2026/NĐ-CP ngày 29/04/2026 ([trang văn bản](https://vanban.chinhphu.vn/?docid=217960&pageid=27160)), liên quan hộ/cá nhân kinh doanh và CIT; không mở rộng engine lương theo nguồn này.

Các nguồn đã được cung cấp; phần còn lại là đối chiếu văn bản gốc, sửa đổi và kỳ áp dụng, cùng việc người dùng tự kiểm số học. Không suy ra Verified từ các liên kết hoặc phần đọc sơ bộ.

## API và quyết định

Gọi `PitCalculator.Calculate(taxableIncome, ruleSet)` hoặc `MonthlySalaryCalculator.Calculate(grossIncome, insurance, dependents, ruleSet)`. Kết quả chứa từng khoản, TNTT trước/sau chặn về 0, các phần thuế và tổng; tiền, thuế suất là `decimal`, không làm tròn. Mỗi khoản giữ rule và căn cứ; gross và insurance là đầu vào với căn cứ Missing, không xác nhận tính hợp pháp. Rule sao chép danh sách bậc; bậc và metadata bất biến. Chọn rule tường minh; không suy luận từ ngày.

Thiếu căn cứ là Missing, seed là Unverified. Verified phải được bên cung cấp đánh dấu rõ, có ID, version, ngày xác minh và URL HTTPS vbpl.vn; kiểm cú pháp không chứng minh văn bản hay tính hợp pháp. Metadata cho phép lưu điểm, khoản, điều, số văn bản, tóm tắt, kỳ và ngày nếu thực sự được cung cấp. Không có truy cập mạng trong Core.

`ArgumentNullException` cho đối số bắt buộc null, `ArgumentOutOfRangeException` cho giá trị âm/rate sai, `ArgumentException` cho cấu hình biểu hoặc metadata không nhất quán. Phép toán decimal vượt giới hạn phát sinh `OverflowException`. Bảo hiểm lớn hơn gross được chấp nhận; không đặt trần phụ thuộc tùy ý.

## Tự đối chiếu số học

Các ví dụ sau theo dữ liệu PLAN, chưa được người dùng xác nhận và không phải đối chiếu pháp lý:

- TNTT 25.000.000: 500.000 + 15.000.000 × 10% = 2.000.000.
- TNTT 75.000.000: 8.500.000 + 15.000.000 × 30% = 13.000.000.
- Gross 40.000.000, bảo hiểm 4.000.000, một phụ thuộc: TNTT 14.300.000, thuế 500.000 + 4.300.000 × 10% = 930.000.

## Gói và chạy test

Các phiên bản cố định: [xunit 2.9.3](https://www.nuget.org/packages/xunit/2.9.3), [xunit.runner.visualstudio 3.1.4](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.4), [Microsoft.NET.Test.Sdk 17.14.1](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/17.14.1). Chạy từ root bằng PowerShell:

```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location).Path '.dotnet/nuget-packages'
& ./.dotnet/dotnet.exe restore TaxManager.sln --packages $env:NUGET_PACKAGES
& ./.dotnet/dotnet.exe test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore
& ./.dotnet/dotnet.exe test TaxManager.sln --no-restore
```

Chạy từng lệnh tiếp theo chỉ khi lệnh trước thành công. Restore ban đầu trong sandbox gặp NU1301; sau đó điều phối đã restore thành công vào cache bằng SDK cục bộ. Ngày 21/11/2026, chạy lại cả hai lệnh `test` trên với `--disable-build-servers -m:1`: mỗi lệnh exit 0, 27 passed, 0 failed, 0 skipped. Phiên sandbox dùng `DOTNET_CLI_HOME` và `APPDATA` tạm để tránh lỗi quyền hồ sơ người dùng; cờ bổ sung hạn chế tình trạng MSBuild treo đã gặp trong lượt review. Kết quả này kiểm số học và hành vi kỹ thuật, chưa xác minh pháp lý hay xác nhận người dùng.
