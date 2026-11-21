# S14: Ước tính quyết toán lương năm

`TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(region)` cung cấp snapshot tháng và quy tắc năm riêng. `AnnualSalaryCalculator.Calculate(year, months, dependents, provider)` nhận đủ 12 `SalaryMonthInput`; mỗi tháng phải cung cấp gross, thuế thực tạm nộp và đúng một trong bảo hiểm nhập tay hoặc `InsuranceInput` S13. Tháng không thu nhập vẫn là bản ghi tường minh, có thể nhập bảo hiểm 0. Kết quả sắp theo tháng, giữ snapshot, kết quả tính tháng, lịch người phụ thuộc, chi tiết bảo hiểm tự tính và thuế thực tạm nộp. Thuế tháng là ước tính tính lại theo lịch quyết toán, không phải số đã khấu trừ.

Caller tự tạo `AnnualSalaryRuleSet` phải khai báo kỳ chính xác: rule và căn cứ thuế năm dùng `"2026 annual"`, căn cứ giảm trừ người phụ thuộc dùng `"2026 dependent-month"`, căn cứ thu nhập và các căn cứ quyết toán dùng `"2026"`; rule và căn cứ thuế tháng dùng `"2026 monthly"`. Căn cứ năm phải có ID, version, nguồn, điều khoản và kỳ; nếu khai ngày hiệu lực thì phải bao phủ cả năm. Căn cứ giảm trừ và thu nhập giữa rule tháng và năm phải giống nhau; mọi snapshot phải cùng hợp đồng năm. Đây là kiểm tra tính nhất quán metadata do caller cung cấp, không kiểm tra URL trực tuyến hoặc xác nhận tính pháp lý của nguồn.

`DependentPeriod` nhận ID ổn định và tháng đầu/cuối được giảm trừ, bao gồm cả hai đầu. Caller xác định điều kiện và các khoảng; tháng đăng ký không đồng nhất với tháng bắt đầu nghĩa vụ nuôi dưỡng. Các khoảng cùng ID không được chồng lấn, kể cả ngoài năm; khoảng qua năm được cắt trong kết quả. Hệ thống không đánh giá hồ sơ, cư trú, điều kiện phụ thuộc hay một người phụ thuộc được khai bởi người khác.

Quy tắc năm dùng đơn vị đồng: giảm trừ bản thân 186.000.000/năm, người phụ thuộc 6.200.000/người-tháng; ngưỡng năm 120/360/720/1.200 triệu, thuế suất 5/10/20/30/35%. Thu nhập tính thuế năm bằng tổng gross trừ bảo hiểm và giảm trừ cả năm, clamp về 0 một lần; thuế năm không phải tổng thuế tháng. Chênh lệch bằng thuế năm trừ thực tạm nộp; hai số nộp thêm và nộp thừa dự kiến là hai phía không âm của chênh lệch. Nộp thừa là ước tính số học, không phải quyết định hoàn tiền hay kết quả xét thủ tục.

| Nguồn | Nội dung và kỳ áp dụng |
| --- | --- |
| [VBHN 112/VBHN-VPQH](https://xaydungchinhsach.chinhphu.vn/luat-thue-thu-nhap-ca-nhan-119260623093630882.htm), hợp nhất Luật 109/2025 và Luật 09/2026 | Điều 8, 9, 10: thu nhập tính thuế, biểu lũy tiến và giảm trừ; kỳ lương cư trú 2026. |
| [Nghị định 253/2026/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf) | Điều 46, 48, 51, 66: thời điểm thu nhập, bảo hiểm, giảm trừ và quyết toán năm; Điều 68: nộp thừa và thủ tục; Điều 69, 70: áp dụng và chuyển tiếp. |
| [Bảng biểu thuế Chính phủ](https://xaydungchinhsach.chinhphu.vn/chi-tiet-bieu-thue-thu-nhap-ca-nhan-luy-tien-tung-phan-119260327091407544.htm) | Biểu năm và tháng 2026. |

Nguồn là `Unverified`, không có ngày xác minh; số tự khai có căn cứ `Missing`. Kết quả không xác minh pháp lý. Chỉ hỗ trợ lương thông thường của một cá nhân cư trú, một nguồn lương; không xét thu nhập miễn hay các giảm trừ khác. Bảo hiểm giữ căn cứ và snapshot tháng thực dùng. Decimal giữ nguyên theo lựa chọn không làm tròn của người dùng; không tự làm tròn, overflow được báo lỗi.

Ví dụ kiểm tra số học độc lập: sáu tháng đầu gross 0, sáu tháng sau gross 40 triệu, không bảo hiểm và người phụ thuộc: gross 240 triệu, giảm trừ 186 triệu, thu nhập tính thuế năm 54 triệu, thuế năm 2,7 triệu. Nếu thực tạm nộp 11,7 triệu thì dự kiến nộp thừa 9 triệu. Gross 30 triệu mỗi tháng với một người phụ thuộc tháng 7–12 cho thu nhập tính thuế 136,8 triệu và thuế 7,68 triệu. Các ví dụ không chứng minh tính pháp lý của cấu hình.

Người dùng tự commit; không add, commit hoặc push trong tác vụ này.

Kiểm thử thực chạy với SDK 10.0.404, `NUGET_PACKAGES=.dotnet/nuget-packages` và thư mục cấu hình .NET tạm: `dotnet test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore --disable-build-servers -m:1` và `dotnet test TaxManager.sln --no-restore --disable-build-servers -m:1` đều exit 0, 82 passed, 0 failed, 0 skipped; gồm 55 ca cũ. Review độc lập và kiểm tra pháp lý chưa hoàn thành.
