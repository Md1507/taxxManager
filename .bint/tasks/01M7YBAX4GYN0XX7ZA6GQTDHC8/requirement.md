# Yêu cầu

## Mục tiêu và phạm vi
S15a / BL-23a: thưởng bằng tiền chịu thuế từ cùng đơn vị trả lương cho một NLĐ Việt Nam cư trú trong năm 2026. Caller xác nhận khoản thưởng thuộc diện chịu thuế, quan hệ lao động hiện tại và số tiền đồng Việt Nam. Kỳ tính theo ngày thực nhận/chi trả, không theo tên thưởng, năm thành tích hoặc ngày quyết định.

Đã đọc requirement/plan roadmap 01M7WZSJJDNS1D97A7J19YN37T và phụ thuộc S14 01M7Y9GFD47ESD7PZ8Z8RBCP92 đã done; khảo sát API, tests và docs0005 ở HEAD 363cdaf. 82 test là baseline được bàn giao, chưa chạy lại tại b-req. MCP context_for_task không trả tri thức liên quan để cite. Lượt này chỉ cập nhật phiếu và ba artifact qua Bint; mọi AC/TSK triển khai còn chưa hoàn thành.

## Yêu cầu chức năng
1. Dòng thưởng bất biến có ID ổn định (Trim, so StringComparer.Ordinal, phân biệt hoa/thường), Amount decimal >= 0, ReceivedOn DateOnly và tax treatment bắt buộc tường minh cho thưởng tiền chịu thuế từ đơn vị hiện tại. Không để giá trị mặc định enum vô tình đồng nghĩa taxable; enum không biết/loại unsupported bị reject. Nhãn tùy chọn chỉ để hiển thị, không có tác dụng phân loại hay chọn kỳ. Chấp nhận nhiều dòng và dòng zero, không gộp mất ID; reject ID trùng trong một tháng hoặc cả năm sau normalize.
2. GrossIncome khi nhập kèm danh sách thưởng là LƯƠNG GỐC CHƯA GỒM các dòng thưởng đó; caller phải tách khoản đã nằm trong gross trước khi lập input. Tổng chịu thuế trước BH/giảm trừ = base gross + tổng thưởng, chỉ cộng một lần. API/docs phải công bố rõ hợp đồng này; engine không thể phát hiện một khoản đã ẩn trong con số gross không có sổ chi tiết. Không dùng hai trường tổng và base làm hai nguồn sự thật.
3. Mở rộng SalaryMonthInput bằng tham số trailing tùy chọn hoặc API tương thích; thiếu danh sách/null mặc định được coi là không bổ sung thưởng, list có null item bị reject. Không thưởng/empty giữ output số học cũ và không đòi bonus rule mới. API tháng mới gọi MonthlySalaryCalculator, luồng năm tích hợp trực tiếp AnnualSalaryCalculator, không tạo bộ quyết toán song song.
4. Mỗi dòng chỉ hợp lệ trong tháng/năm input khớp ReceivedOn và kỳ rule. Thưởng ghi cho 2025 nhưng nhận năm 2026 thuộc 2026; nhận năm 2027 không được ép sang năm 2026. Thiếu/trùng tháng năm, gap provider, năm chưa hỗ trợ, thiếu/sai rule hoặc kỳ phải báo lỗi, không bỏ dòng hoặc fallback. Annual vẫn cần đúng 12 tháng, bao gồm tháng zero tường minh.
5. Manual insurance hoặc InsuranceInput S13 giữ nguyên, không suy base BH từ total mới. Lương và thưởng gộp rồi giảm BH/GTBT/NPT một lần và áp biểu tháng một lần; không tách rate thưởng. ActualWithheldTax là số thực chung tháng và không bị tự tăng. Annual cộng total tháng một lần, tính TNTT năm/clamp/biểu năm/delta bằng S14, giữ NoRounding decimal và overflow.
6. Kết quả giữ base gross, từng dòng thưởng/ngày nhận/phân loại, tổng thưởng và total theo tháng/năm; giữ RuleSet và căn cứ thực dùng theo từng dòng. Input amount/ID/date/xác nhận caller là tự khai Missing; quy tắc taxable/timing có căn cứ riêng Unverified, không nâng Verified hay dựng vbpl ID/URL. Collection input được copy, output read-only; thay đổi collection caller không đổi kết quả.
7. Bộ rule thưởng nhỏ gọn liên kết TaxRuleSet, có version, kỳ, treatment và timing tường minh cùng căn cứ; factory S15a kế thừa các snapshot thuế/BH/năm S14 mà không sửa metadata seed cũ. Không xây framework phân loại toàn bộ thu nhập bất thường.

## Căn cứ và giới hạn đối chiếu
Đã mở lại hai nguồn Chính phủ và xem ảnh scan có sẵn ngoài repo: [NĐ253/2026/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf) trang 4 Điều 8(1), trang 5 Điều 8(2)i, trang 7 Điều 8(4)a, trang 30 Điều 46(3), trang 35–36 Điều 50(1),(2), trang 57 phần tiếp Điều 69(1)a và Điều 70(2). Phân loại thưởng tiền phải loại ngoại lệ không tính vào thu nhập; không thể dùng tên Tết/KPI/tháng 13 làm bằng chứng eligibility. Quy định nhận/trả và chuyển tiếp là căn cứ chọn kỳ, không thay thế số thực đã tạm nộp.

[Chính phủ trích Điều 8–10 VBHN112](https://xaydungchinhsach.chinhphu.vn/cach-tinh-thue-thu-nhap-ca-nhan-tu-tien-luong-tien-cong-119260623094516526.htm) hỗ trợ tổng thu nhập trong kỳ, biểu lũy tiến và giảm trừ. [Trả lương sau nghỉ việc](https://baochinhphu.vn/tra-luong-sau-khi-nghi-viec-tru-thue-tncn-ra-sao-102260910093753041.htm) dùng để xác định ngoại lệ ngoài S15a. Không chép toàn văn/PDF vào repo, không tuyên bố hoàn tất thẩm định pháp lý; metadata mới vẫn Unverified.

## Tiêu chí chấp nhận
- [ ] AC-1: Khoản thưởng tiền chịu thuế được khai riêng bằng ID Trim/Ordinal, số tiền decimal không âm, ngày nhận và phân loại tường minh; nhiều dòng hợp lệ được giữ riêng, loại ngoài phạm vi hoặc enum không hỗ trợ bị từ chối và không suy phân loại từ tên.
- [ ] AC-2: Quy tắc thưởng và thời điểm nhận liên kết TaxRuleSet theo kỳ năm 2026, có ID/version/căn cứ nguồn chính thức Unverified; factory S15a riêng giữ nguyên seed S11/S13/S14 và báo lỗi khi thiếu hoặc sai rule/kỳ.
- [ ] AC-3: Thuế tháng cộng lương gốc chưa gồm thưởng với tổng thưởng đúng một lần rồi gọi bộ tính tháng hiện có, giảm trừ một lần; ca BH0/NPT0 lương 30 triệu và thưởng 20 triệu cho TNTT 34,5 triệu và thuế 3,4 triệu.
- [ ] AC-4: AnnualSalaryCalculator tích hợp thưởng đúng tháng nhận vào đủ 12 tháng, giữ phân rã lương gốc/thưởng/tổng năm và tính lại bằng biểu năm S14; lương 30 triệu mỗi tháng và thưởng 20 triệu tháng 12 cho gross 380 triệu, TNTT 194 triệu và thuế năm 13,4 triệu.
- [ ] AC-5: Bảo hiểm tay hoặc tự tính S13 chỉ trừ một lần, bonus không tự đổi base bảo hiểm; ActualWithheldTax và tổng tạm nộp giữ đúng input thực, không sinh thuế khấu trừ thưởng hay giảm trừ riêng lần hai.
- [ ] AC-6: Validation từ chối null item, ID rỗng/trùng cùng năm kể cả khác tháng, tiền âm, sai tháng/năm nhận, thiếu/trùng tháng, gap, unsupported year và cấu hình mismatch; giữ lỗi decimal overflow và quy tắc ID phân biệt hoa/thường.
- [ ] AC-7: Kết quả bất biến giữ từng dòng thưởng, ngày nhận, tháng/năm, lương gốc/tổng thưởng/total và snapshot thực dùng; số tự khai Missing, phân loại/timing Unverified đúng kỳ/version, copy/read-only collections chống mutation và không rounding.
- [ ] AC-8: Giữ constructor/caller và semantics của 82 ca cũ, không đổi kết quả số học khi không có thưởng hoặc danh sách rỗng; test mới có expected độc lập bao phủ số học, timing, bảo hiểm, lỗi, provenance, rule giả và bất biến; test Core rồi solution thực chạy xanh.
- [ ] AC-9: Docs0006 ghi API, gross chưa gồm thưởng, phạm vi và ngoại lệ, nguồn/điều khoản/chuyển tiếp 2026, ví dụ độc lập, giới hạn và kết quả test thật; diff chỉ Core/tests/docs0006/ledger phiếu này, không package/network Core hay sửa PLAN/config/setup/solution/csproj/ledger cũ và không git add/commit/push.
- [ ] AC-10: Review độc lập đối chiếu toàn bộ tiêu chí và diff S15a không còn lỗi nghiêm trọng chưa xử lý; root xác nhận bằng chứng và hoàn tất bàn giao sau khi mã, test và docs đã đủ.

## Ngoài phạm vi
S15b/c/d/e/f; thưởng miễn theo Điều 8(4)a, chứng khoán/ESOP/hiện vật, khoản sau nghỉ việc/khấu trừ 10%, nguồn khác, phụ cấp/trợ cấp/miễn thuế, đổi ngoại tệ, gross-up, tự xác định eligibility/cư trú hay chứng từ. Loại không được mô hình hóa bị loại trừ bằng hợp đồng API/docs; nếu được truyền qua trường phân loại thì reject, không tính thử theo taxable. Không UI/DB, package/network Core, sửa công thức S11–S14, PLAN/config/setup/model/solution/csproj/docs cũ/ledger cũ. User tự commit; không git add/commit/push. verify.command giữ trống, Bint Core quản lý trạng thái.

## Câu hỏi mở
Không có câu hỏi cản trở triển khai.
