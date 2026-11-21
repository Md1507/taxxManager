# 0010 — Đóng góp từ thiện trong ước tính thuế lương năm 2026

## Phạm vi và API

Bước S15e hỗ trợ tiền VND trong nước do một cá nhân Việt Nam cư trú thực đóng trong năm dương lịch 2026. Không xử lý hiện vật, ngoại tệ, thu nhập kinh doanh, người không cư trú, gross-up, đóng trực tiếp cho cá nhân, hưu trí hay các khoản y tế/giáo dục. Đây là ước tính, không quyết định quyền hoàn hoặc thủ tục quyết toán.

Tạo `CharitableContributionYearInput` với scope `VietnameseResidentDomesticVndCalendarYear`, hai xác nhận tự chi/không được nguồn khác chi trả và chưa trừ trùng. Mỗi `CharitableContribution` có ID, ngày đóng, tiền, ID và nhóm tổ chức, ba xác nhận pháp lý/mục đích/phi lợi nhuận và danh sách `ContributionEvidence`. ID được trim và so sánh ordinal duy nhất trong năm. Danh sách được sao chép, chỉ đọc; ngày chứng từ không thay thế ngày đóng. Số không được nhận, số âm và ngày sai năm bị từ chối. Nhãn chỉ để hiển thị.

Ba nhóm gồm cơ sở chăm sóc trẻ em hoàn cảnh đặc biệt, người khuyết tật hoặc người già không nơi nương tựa; quỹ từ thiện, nhân đạo hoặc khuyến học; và tổ chức có chức năng huy động tài trợ thành lập, hoạt động theo pháp luật. Caller khai và chịu trách nhiệm về nhóm, điều kiện, tính đầy đủ, trùng ẩn khác ID và chứng từ; engine không tra cứu hay xác minh. Chứng từ chỉ là tham chiếu, không tải lên hay lưu PDF. Bản chụp chứng từ thu hợp pháp **hoặc** chứng từ thanh toán không tiền mặt qua tổ chức tín dụng là đủ; cờ của nhánh còn lại không bắt buộc. Thiếu xác nhận hoặc chứng từ cho dòng đủ điều kiện bằng 0 và mã lý do. Các enum không hỗ trợ và dữ liệu cấu trúc sai bị từ chối.

Dùng `TaxRuleSet.CreateVietnam2026CharitableContributionProvider` với overload `AnnualSalaryCalculator.Calculate(year, months, dependents, provider, contributions)`, hoặc `CreateVietnam2026MultipleSalaryCharitableContributionProvider` với `MultipleSalaryCalculator.Calculate(input, provider, contributions)`. Overload cũ và factory cũ giữ nguyên. Nhiều nguồn được tổng hợp một lần ở cấp cá nhân; không phân bổ khoản đóng góp cho payer hoặc tháng. `AnnualSalaryResult.ContributionDetails` chứa dòng khai, số đủ điều kiện, lý do, chứng từ được dùng, snapshot và rule; các basis đầu vào/xác nhận/chứng từ là `Missing`. `EligibleCharitableContribution` là tổng đủ điều kiện, `IncomeReduction` là ảnh hưởng thực tế lên thu nhập. Thuế tháng vẫn là số trước giảm trừ quyết toán năm, không phải khấu trừ thực tế được tính lại.

## Công thức

G là gross trừ thu nhập miễn, D0 là bảo hiểm và giảm trừ bản thân/người phụ thuộc; E là tổng đủ điều kiện. Tổng giảm trừ D = D0 + E; before = G - D; thu nhập tính thuế = max(0, before). PIT tính lại theo biểu thuế năm của rule; delta = PIT - khấu trừ thực tế; nộp thêm = max(0, delta), nộp thừa ước tính = max(0, -delta). Ảnh hưởng thu nhập = min(E, max(0, G - D0)); phần không dùng = E trừ ảnh hưởng. Phần này chỉ là chênh lệch số học, không phải trần pháp lý và không chuyển sang năm sau. Decimal giữ nguyên, không làm tròn; tràn số được báo lỗi.

Ví dụ độc lập: gross 360 triệu, bảo hiểm 12 triệu, bản thân 186 triệu, người phụ thuộc 37,2 triệu, còn 124,8 triệu trước đóng góp. Đóng góp đủ điều kiện 10 triệu cho thu nhập tính thuế 114,8 triệu, PIT 5,74 triệu; đã khấu trừ 10,8 triệu thì delta -5,06 triệu và nộp thừa ước tính 5,06 triệu. Nếu đủ điều kiện 130 triệu, before -5,2 triệu, thu nhập tính thuế và PIT bằng 0; ảnh hưởng 124,8 triệu, phần không dùng 5,2 triệu. Nếu thực khấu trừ bằng 0 thì không phát sinh nộp thừa từ riêng đóng góp.

## Nguồn và kỳ

Rule được chọn theo ngày đóng và kiểm tra độ bao phủ từng tháng; thiếu rule không fallback. Cả năm 2026 được hỗ trợ cho thu nhập lương theo Điều 69(1)(a), chuyển tiếp Điều 70(2); ngày hiệu lực 01/07 không tự giới hạn đóng góp ở nửa cuối năm. Các basis seed `Unverified`, `VerifiedOn` null; kiểm tra metadata không thay thế thẩm định pháp lý.

[Nghị định 253/2026/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf), Điều 49(1)(a,b,c) và đoạn điều kiện/chứng từ quy định các nhóm và hai nhánh chứng từ; Điều 49(5) quy định năm phát sinh và không chuyển năm; Điều 46(2)(c) về giảm trừ, Điều 66(1)(a) về kỳ năm, Điều 68(1),(2),(4) về bối cảnh nộp thừa. Không áp ngưỡng hay điều kiện y tế/giáo dục Điều 49(3),(4) cho khoản này. [112/VBHN-VPQH](https://xaydungchinhsach.chinhphu.vn/luat-thue-thu-nhap-ca-nhan-119260623093630882.htm) hợp nhất Luật 109/2025/QH15 và Luật 09/2026/QH16; Điều 8(2), 11 và 11(3) liên quan giảm trừ, chứng từ và nguồn chi trả. Nguồn đã xem không đồng nghĩa đã xác minh toàn bộ quy định hoặc hồ sơ cụ thể.

## Kiểm thử và bàn giao

SDK 10.0.404, cache sẵn, không restore: `dotnet test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore --disable-build-servers -m:1`, sau đó `dotnet test TaxManager.sln --no-restore --disable-build-servers -m:1`: mỗi lệnh 169 passed, 0 failed, 0 skipped. Người dùng tự stage và commit sau review.




Với số tiền không âm, tổng đủ điều kiện không vượt tổng khai nên không thể tràn riêng sau khi tổng khai đã cộng thành công. Kiểm thử dùng các dòng không đủ điều kiện để chứng minh tràn tổng khai vẫn được báo; biên tổng giảm trừ, before và delta cũng được kiểm tra bằng `decimal.MaxValue`/`decimal.MinValue`. Khi gross sau miễn và tổng giảm trừ đều không âm, before không thể nhỏ hơn `decimal.MinValue` nếu tổng giảm trừ đã cộng thành công; PIT và khấu trừ thực tế không âm nên delta cũng nằm trong miền decimal nếu hai giá trị đầu vào hợp lệ.
