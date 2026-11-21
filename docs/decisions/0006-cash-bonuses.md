# 0006 — Thưởng tiền chịu thuế theo ngày nhận (S15a)

## Hợp đồng

`CashBonus(id, amount, receivedOn, treatment, label)` biểu diễn một khoản thưởng tiền VND của đơn vị trả lương hiện tại. Caller xác nhận khoản này chịu thuế và người nhận là cá nhân cư trú; chỉ `TaxableCashFromCurrentEmployer` được hỗ trợ. ID được Trim và so sánh Ordinal phân biệt hoa/thường; nhãn chỉ để hiển thị. Không dùng tên Tết, KPI hay tháng 13 để xác định diện thuế hoặc kỳ. Ngày thực nhận/chi trả quyết định kỳ, kể cả thưởng cho năm trước.

`SalaryMonthInput` nhận `bonuses` tùy chọn ở cuối constructor và sao chép danh sách. `GrossIncome` là lương gốc **chưa bao gồm** những dòng thưởng bổ sung; caller phải tách chúng khỏi gross. Engine không thể phát hiện một khoản đã nằm ẩn trong tổng gross. `BonusMonthlySalaryCalculator.Calculate(input, dependents, provider)` cộng một lần và gọi `MonthlySalaryCalculator`; `AnnualSalaryCalculator.Calculate` tiếp tục yêu cầu đủ 12 tháng, giữ từng dòng và cộng tổng tháng một lần trước khi quyết toán theo S14. ID trùng trong năm, sai tháng nhận, thiếu rule hoặc kỳ không khớp bị từ chối. Input không có thưởng vẫn dùng được provider cũ.

`TaxRuleSet.CreateVietnam2026CashBonusProvider(region)` kế thừa dữ liệu thuế, bảo hiểm và năm S14, bổ sung `CashBonusRuleSet` theo từng snapshot. Kết quả giữ base, tổng thưởng, tổng gross, từng dòng và rule thực dùng; số tự khai có `Missing`, căn cứ phân loại và thời điểm là `Unverified`. Không làm tròn decimal. Bảo hiểm lấy từ input riêng, không tự cộng thưởng vào cơ sở BH; số thuế thực tạm nộp là input chung tháng, không tự tạo khấu trừ thưởng.

## Phạm vi và căn cứ

Không xử lý thưởng được loại trừ theo Điều 8(4)a, chứng khoán/hiện vật, khoản trả sau nghỉ việc, nguồn khác, trợ cấp hoặc khoản miễn thuế, đổi ngoại tệ hay tự xác định eligibility. Caller không được đưa những khoản đó vào treatment được hỗ trợ. Không hỗ trợ năm ngoài 2026. Không có UI, lưu trữ hay kiểm tra chứng từ trong bước này.

[Nghị định 253/2026/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf) Điều 8(2)i đề cập thưởng; Điều 8(4)a liệt kê các khoản không tính; Điều 46(3) quy định thời điểm; Điều 50(1), (2) phân biệt cách tính và trường hợp sau hợp đồng. Điều 69(1)a và Điều 70(2) liên quan áp dụng kỳ 2026 và chuyển tiếp quyết toán. [Cổng Chính phủ trình bày Điều 8–10](https://xaydungchinhsach.chinhphu.vn/cach-tinh-thue-thu-nhap-ca-nhan-tu-tien-luong-tien-cong-119260623094516526.htm); [hướng dẫn trả sau nghỉ việc](https://baochinhphu.vn/tra-luong-sau-khi-nghi-viec-tru-thue-tncn-ra-sao-102260910093753041.htm) nằm ngoài phạm vi. Các nguồn được ghi Unverified, không thay thế thẩm định trường hợp cụ thể.

## Ví dụ và kiểm thử

BH và người phụ thuộc bằng 0: base 30 triệu, thưởng 20 triệu, gross 50 triệu, trừ 15,5 triệu còn 34,5 triệu; PIT tháng = 0,5 + 2 + 0,9 = 3,4 triệu. Cả năm 12 tháng base 30 triệu, thưởng 20 triệu tháng 12: gross 380 triệu, giảm trừ 186 triệu, thu nhập tính thuế 194 triệu, PIT năm = 6 + 7,4 = 13,4 triệu. Thuế năm tính lại theo biểu năm, không lấy tổng thuế tháng.

Ngày 2026-11-21, SDK sẵn có, `dotnet test` với `--no-restore --disable-build-servers -m:1`: project Core 89 passed, 0 failed, 0 skipped; solution 89 passed, 0 failed, 0 skipped. Người dùng tự commit; bước này không stage hoặc commit.
