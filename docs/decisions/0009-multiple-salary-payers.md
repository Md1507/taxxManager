# S15d: Tổng hợp lương từ nhiều nơi trả

API Core áp dụng cho một cá nhân cư trú Việt Nam, năm dương lịch 2026, VND trong nước, và tiền lương gross thông thường hoàn toàn chịu thuế. Đây là ước tính thuế năm và đối chiếu số đã khấu trừ thực tế; không xác định nghĩa vụ khai, ủy quyền, nơi nộp hay quyền hoàn. Không có adapter cho thưởng, phụ cấp, khoản vãng lai hoặc các bước S15e/f. Không suy loại khoản từ nhãn.

## Hợp đồng

Tạo `SalaryReceipt(paymentId, receivedOn, grossPaid, actualWithheldTax, subject, label)` với subject `OrdinaryEmploymentFullyTaxableGross`. Nhóm các khoản của cùng payer trong một `SalaryPayerMonth(payerId, payments)`, rồi tạo `MultipleSalaryMonth(year, month, deductibleInsurance, payers)`. Payer rỗng xác nhận rõ tháng không có thu nhập và không khấu trừ; payer có mặt phải có ít nhất một khoản. `actualWithheldTax` có thể null khi thu thập nhưng `Calculate` từ chối null và chỉ rõ ID; số 0 là số đã biết. Số thực tế có thể lớn hơn thu nhập hoặc thuế ước tính.

`MultipleSalaryYearInput(year, scope, completeNonOverlappingIncomeConfirmed, deductibleInsuranceConfirmed, months, dependents)` cần đủ 12 tháng khác nhau và scope `VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary`. Cả hai xác nhận phải đúng khi tính. Ngày nhận phải thuộc tháng, payer không trùng trong tháng và payment ID không trùng trong năm sau Trim, so sánh ordinal phân biệt hoa thường. Danh sách được sao chép và chỉ đọc. `MultipleSalaryCalculator.Calculate(input, TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(region))` trả `MultipleSalaryResult`: input, kết quả annual, các tháng sắp thứ tự và chi tiết payer/khoản, snapshot và rules thực dùng. `Annual` cung cấp tổng, thuế, delta, additional payable và estimated overpaid. Không phân bổ thuế hay giảm trừ cho payer.

Người gọi chịu trách nhiệm xác nhận đúng người, phạm vi, toàn bộ thu nhập cần tổng hợp, các khoản không bị chia, gộp hoặc trùng ẩn, và tổng bảo hiểm thực được trừ theo chứng từ, không lặp giữa payer. ID không phát hiện cùng khoản mang mã khác; chương trình không xác minh cư trú, hồ sơ, chứng từ hoặc URL. Không đưa lại các khoản đã nằm trong gross qua hệ thống khác. Bảo hiểm chỉ là một số nhập tay mỗi tháng; engine không tự tính hoặc phân bổ bảo hiểm. Region là tham số bắt buộc của seed annual kế thừa; quy tắc bảo hiểm được mang theo snapshot nhưng không được engine này dùng để tính số nhập tay.

## Cách tính và ví dụ

Gross và actual được cộng theo khoản, payer, tháng rồi chuyển thành 12 `SalaryMonthInput` không bonus/extras; gọi `AnnualSalaryCalculator` một lần. Giảm trừ bản thân và lịch phụ thuộc chỉ tính một lần cho cá nhân. Số thập phân được giữ nguyên, không làm tròn; tràn decimal phát sinh `OverflowException`.

* A trả 20 triệu và B trả 10 triệu mỗi tháng, bảo hiểm 1 triệu/tháng, một người phụ thuộc tháng 7–12: gross 360 triệu, bảo hiểm 12 triệu, giảm trừ bản thân 186 triệu, phụ thuộc 37,2 triệu, thu nhập tính thuế 124,8 triệu, thuế 6,48 triệu. Actual tổng 10,8 triệu thì chênh lệch -4,32 triệu; nếu actual 6 triệu thì còn nộp thêm 480 nghìn.
* A trả 30 triệu/tháng trong sáu tháng đầu, B trả cùng mức sáu tháng cuối, không bảo hiểm hay phụ thuộc: gross 360 triệu, thu nhập tính thuế 174 triệu, thuế 11,4 triệu. Nếu actual bằng 0, chênh lệch phải nộp là 11,4 triệu.
* Sáu tháng đầu không có thu nhập, sáu tháng cuối 40 triệu/tháng, không bảo hiểm hay phụ thuộc: thuế năm 2,7 triệu. Actual 11,7 triệu tạo chênh lệch nộp thừa ước tính 9 triệu; số này không quyết định quyền hoàn.

## Căn cứ và provenance

Nguồn [Nghị định 253/2026/NĐ-CP bản ký](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf): Điều 46(1),(2) tổng hợp và khoản được trừ, 46(3) thời điểm nhận; 48(1)a,b giảm trừ bản thân, 48(2)c người phụ thuộc và nhiều nguồn; 66(1)a kỳ năm; 69(1)a áp dụng kỳ lương 2026; 70(2) chuyển tiếp; 68(1),(2),(4) bối cảnh quyết toán, nộp thừa và thủ tục. Coverage trong rule mô tả phạm vi sản phẩm, không phải khẳng định ngày hiệu lực. Nguồn seed vẫn `Unverified`, `VerifiedOn` null; dữ liệu và xác nhận caller mang `Missing`. Biểu thuế và số giảm trừ được tái sử dụng từ seed annual, không lặp trong engine mới.

## Kiểm thử và bàn giao

Đã chạy SDK 10.0.404 với `DOTNET_ROOT=.dotnet`, `NUGET_PACKAGES=.dotnet/nuget-packages`, `DOTNET_CLI_HOME` trong thư mục tạm: `dotnet test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore --disable-build-servers -m:1` rồi `dotnet test TaxManager.sln --no-restore --disable-build-servers -m:1`. Mỗi lệnh đạt 143, lỗi 0, bỏ qua 0; bao gồm 122 test cũ. Người dùng tự stage và commit.
