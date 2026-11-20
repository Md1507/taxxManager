# Yêu cầu

## Mục tiêu và phạm vi
S11, chỉ Bước 1.1 và BL-10 đến BL-15 theo PLAN.md cùng roadmap 01M7WZSJJDNS1D97A7J19YN37T. S0 01M7X1P1ED7J4C8Q1HSZRB09XQ đã done; đã đọc requirement/plan của S0 và plan/tasks roadmap. Chưa có solution, project hay mã ứng dụng; SDK local/pin/helper của S0 đã tồn tại. Phân tích standard; lượt b-req chỉ ghi ba artifact, không viết code, restore hay chạy test. MCP không có tri thức liên quan.

## Yêu cầu chức năng
1. Tạo TaxManager.sln đúng hai project net10.0: src/TaxManager.Core (thuần C#, không package ngoài) và tests/TaxManager.Core.Tests (xUnit, tham chiếu Core). Chỉ package test/runner cần thiết, pin phiên bản sau khi đối chiếu nguồn chính thức trong lượt triển khai.
2. Bracket và TaxRuleSet chứa FromYear, giảm trừ, biểu thuế, ID/version/kỳ, snapshot căn cứ. Tiền và thuế suất dùng decimal. Vietnam2026 chỉ là snapshot THAM KHẢO CHƯA XÁC MINH: bản thân 15.500.000, mỗi phụ thuộc 6.200.000; trần bậc tháng 10.000.000/30.000.000/60.000.000/100.000.000/không giới hạn, suất 0,05/0,10/0,20/0,30/0,35. FromYear=2026 là nhãn PLAN, không suy ra hiệu lực.
3. PitCalculator nhận TNTT không âm và ruleSet, tính từng phần; MonthlySalaryCalculator nhận grossIncome, insurance là tiền do người dùng nhập, dependents và ruleSet. Trả record giữ đầu vào, giảm trừ từng loại, TNTT trước/sau chặn 0, chi tiết từng bậc và tổng thuế. Không làm tròn; tham số luật chỉ nằm ở ruleSet, expected trong test là đối chứng độc lập.
4. Dùng exception .NET thông thường: null, số âm, số phụ thuộc âm, rule/biểu sai đều có lỗi rõ. Dependents là int không âm; không đặt thêm trần nghiệp vụ chưa có nguồn. Snapshot defensive cả collection và metadata để sửa dữ liệu đầu vào sau tạo rule hoặc sau tính không đổi rule/kết quả cũ.
5. Metadata độc lập UI/DB gồm ID/version, điểm/khoản/điều/số văn bản, summary được cung cấp, URL gốc, kỳ/ngày áp dụng, trạng thái/ngày xác minh. Cho phép thiếu dữ liệu với trạng thái Missing/Unverified rõ; không chép các trích dẫn tham khảo trong PLAN thành căn cứ Verified. URL sai/ngoài nguồn vbpl.vn không được dùng làm nguồn Verified; URL đúng cú pháp cũng không tự xác minh. Từng thành phần kết quả và từng bậc giữ đúng rule/basis snapshot thực dùng; insurance là đầu vào chưa xác minh luật. Không HTTP trong engine.
6. xUnit Theory với MemberData/TheoryData decimal bao phủ đủ 7 ca PIT và 3 ca lương Bước 1.1, thêm biên, số lẻ, validation, snapshot, version/kỳ/mapping và thiếu/chưa xác minh/URL sai. Expected phải tính độc lập. Chạy kiểm hẹp rồi toàn solution sau restore trong lượt b-feat; ghi trung thực nếu dependency chưa có.
7. docs/decisions/0002-core-monthly-tax.md ghi API, exception/validation, cách chạy test, ca số học để người dùng tự bấm, nguồn package đã chọn và phần chưa kiểm. Bàn giao kỹ thuật tách rõ phần còn chờ nguồn pháp lý và người dùng tự đối chiếu; không tuyên bố người dùng đã xác nhận.

## Tiêu chí chấp nhận
- [ ] AC-1: Có TaxManager.sln gồm đúng Core net10.0 và Core.Tests xUnit; tests tham chiếu Core, Core không có package ngoài hay phụ thuộc Web/DB.
- [ ] AC-2: Bracket/TaxRuleSet và snapshot Vietnam2026 dùng đúng dữ liệu PLAN bằng decimal, có validation nhất quán và trạng thái chưa xác minh; thuật toán không hardcode số luật.
- [ ] AC-3: PitCalculator và MonthlySalaryCalculator tính đúng từng phần, không làm tròn, xử lý thu nhập tính thuế âm về 0 và trả chi tiết; đầu vào sai có lỗi rõ.
- [ ] AC-4: Metadata và kết quả liên kết đúng căn cứ/version/kỳ thực dùng, độc lập Web/DB/mạng; thiếu hoặc URL sai không được coi là nguồn đã xác minh, snapshot không bị đổi theo đầu vào mutable.
- [ ] AC-5: Toàn bộ 10 ca số học Bước1.1 cùng kiểm biên/không làm tròn/validation/mapping/missing/unverified có test có ý nghĩa; dotnet test Core.Tests và toàn solution thực sự đạt sau restore.
- [ ] AC-6: Có hướng dẫn API/chạy test và ca số học cho người dùng tự đối chiếu; ghi rõ còn chờ nguồn pháp lý/người dùng kiểm, không tuyên bố đã xác minh luật hoặc đã có Web/DB.
- [ ] AC-7: Thay đổi đúng phạm vi, giữ PLAN.md/config/setup S0; đủ bằng chứng tiêu chí và review không còn lỗi nghiêm trọng chưa xử lý.

## Ngoài phạm vi
Bước 1.2 trở đi: provider tự chọn luật/kỳ, bộ 2025, tự tính BH, làm tròn nghiệp vụ, quyết toán năm, vãng lai; Web/Data/DB, UI/HTML, HTTP, migration và CI. Không sửa PLAN.md, .bint/config.yml, global.json, .gitignore, helper/setup S0 hoặc model/harness; không cài lại SDK/Git/editor, quét toàn .dotnet, commit, tự approve, verify hay đổi trạng thái.

Nguồn chính thức/hiệu lực biểu và giảm trừ 2026 cùng việc người dùng đọc/bấm kiểm còn chờ phản hồi, nhưng không chặn engine và test số học trên dữ liệu tham khảo theo yêu cầu hiện tại. Yêu cầu hiện tại cho phép bàn giao kỹ thuật có nhãn chưa xác minh, cụ thể hóa điều kiện L1 nghiêm hơn trong roadmap; không sửa roadmap cũ. verify.command giữ trống.

## Câu hỏi mở

