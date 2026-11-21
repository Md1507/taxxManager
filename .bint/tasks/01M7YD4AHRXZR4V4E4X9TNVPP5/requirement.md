# Yêu cầu

## Mục tiêu và phạm vi
S15b/BL-23b sau S15a đã done (01M7YBAX4GYN0XX7ZA6GQTDHC8), thuộc Bước 1.5 của PLAN.md và roadmap 01M7WZSJJDNS1D97A7J19YN37T. Tích hợp tiền làm thêm giờ, làm đêm và tiền ăn giữa ca/ăn trưa bằng tiền vào cùng luồng tháng và quyết toán đủ 12 tháng. Chỉ một cá nhân Việt Nam cư trú, HĐLĐ thông thường tại đơn vị hiện tại, VND, năm 2026.

B-req chỉ ghi ba artifact phiếu này qua Bint; không sửa application code hay chạy test .NET. Độ sâu deep, không tách thêm giai đoạn: S15b đã là phần giao được riêng của roadmap. Phiếu không có parent; đã đọc requirement/plan roadmap và phụ thuộc S15a đã done. MCP context_for_task không có tri thức liên quan, không áp dụng mục [k:] nào.

## Yêu cầu chức năng
1. Dòng bất biến có ID Trim/Ordinal phân biệt hoa/thường, enum loại tường minh, PaidAmount decimal không âm, ReceivedOn, nhãn tùy chọn chỉ hiển thị. OT/night phải khai riêng số tiền đáp ứng đầy đủ điều kiện, thời gian và mức luật định dựa trên tài liệu lao động, kèm xác nhận tường minh; không đồng nhất với phần trả cao hơn lương thường. Thiếu số tiền hoặc xác nhận không được miễn mặc định; cho phép qualified bằng 0, một phần hoặc toàn bộ nhưng không vượt paid.
2. Thêm rule/factory S15b riêng nối từ S15a, không sửa dữ liệu/metadata seed cũ. Rule theo snapshot có ID/version/year/khoảng áp dụng, entries theo kind, mode, cap khi cần, timing và LegalBasis. OT/night có rule cả năm; meal chỉ tháng 7–12. Mọi dòng meal tháng 1–6, kể cả paid=0, bị reject. Tháng không có dòng không cần rule extras.
3. Mode CallerConfirmedQualifiedAmount miễn đúng qualified caller khai, phần còn lại chịu thuế. Mode MonthlyAggregateCap cộng meal cùng tháng, miễn tối đa cap một lần; phân bổ theo ReceivedOn tăng dần rồi ID đã Trim dùng Ordinal. Giữ kết quả theo từng dòng và thứ tự phân bổ được tài liệu hóa; không hardcode cap trong logic.
4. GrossIncome của SalaryMonthInput là base chưa gồm bonus/extras. Giữ riêng base, bonus, extras paid/exempt/taxable; total gross gồm toàn bộ tiền đã nhận, taxable gross = total gross - exemption trước BH/giảm trừ. Mở rộng MonthlySalaryCalculator tương thích để trừ miễn trước BH/giảm trừ và gọi PitCalculator hiện có; BonusMonthlySalaryCalculator phải xử lý extras bằng cùng helper, không bỏ list.
5. AnnualSalaryCalculator giữ 12 tháng/snapshot thực dùng, cộng raw gross và exemption rồi trừ một lần, trừ BH/GTBT/NPT và clamp một lần trước PIT năm. Không cộng PIT hoặc TNTT đã clamp của tháng thành năm. BH manual/auto lấy input S13 riêng, không cộng bonus/extras vào base BH; ActualWithheldTax là số thực caller khai.
6. Reject null item, ID trắng/trùng sau Trim trong và khác tháng, kể cả bonus với extras; A/a khác ID. Reject âm, enum lạ, qualified sai, thiếu confirmation, sai ngày/tháng/năm, thiếu/sai rule/year/coverage/timing/mode/basis, gap provider, kỳ ngoài hỗ trợ. Decimal overflow phải nổi lên; không rounding. Copy/read-only mọi collection input/result; giữ từng dòng và snapshot/basis thực dùng; tiền tự khai và qualified có Missing.
7. Caller chịu trách nhiệm split khoản không overlap khỏi base/bonus và giữa OT/night; ID không phát hiện khoản ẩn trong gross hoặc đổi ID. Engine không xét chứng từ, tính lương giờ, giới hạn giờ hay eligibility pháp luật lao động.
8. Constructor/caller cũ và 89 test hiện có giữ semantics. Extras omitted/null/empty không đòi rule S15b, provider S15a vẫn chạy bonus. Kết quả bổ sung bằng property/tham số trailing tùy chọn, không đổi positional constructor/deconstruction cũ.
9. Expected độc lập: BH0/NPT0, tháng 7+ base30m, bonus20m, OT3m đủ3m, night1.3m đủ1.3m, meal1.5m cho gross55.8m, exempt5.5m, taxablegross50.3m, TNTT34.8m, PIT3.46m; withheld0.95m giữ nguyên. Năm 12 base30m, bonus/extras chỉ tháng12 cho gross385.8m, exempt5.5m, taxablegross380.3m, GTBT186m, TNTT194.3m, PIT13.43m; withheld11.4m và additional2.03m.
10. B-feat phải hoàn thiện mã/test/docs0007 và bằng chứng AC1–AC9 trước review. Reviewer độc lập chỉ xem diff S15b từ snapshot Bint trước b-feat; root chốt AC10 sau review. Không thay index S15a, không stage/unstage/reset/stash/commit/push; người dùng tự commit.

## Căn cứ và chuyển tiếp
Nguồn chuẩn là [scan Nghị định 253/2026/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf): trang 5 Điều 8(2)g về meal; trang 18 Điều 26(1),(3) về OT/night; trang 30 Điều 46(3) về ngày nhận; trang 56–57 Điều 69(1)a,b và Điều 70(2) về hiệu lực, kỳ áp dụng và chuyển tiếp. Đã mở nguồn chính thức và xem PNG các trang trên có sẵn ngoài repo, không tải/cài hay chép toàn văn vào repository.

Phân biệt hiệu lực văn bản 01/07/2026, áp dụng lương cư trú kỳ 2026 và ngoại lệ meal từ tháng 7. Kết quả tháng là ước tính theo bộ luật kỳ 2026; không tự thay số thực đã tạm nộp hay quyết định kê khai tháng/quý trước hiệu lực. Không áp công thức miễn phần premium TT111 cũ cho nhánh này. Mọi basis mới giữ Unverified, VerifiedOn null, actual official HTTPS URL, metadata điều/khoản/điểm/kỳ/version và Summary Việt 2–3 câu; không giả vbpl ID hay ngày đối chiếu.

## Tiêu chí chấp nhận
- [ ] AC-1: AC1: Dòng OT/night/meal explicit, ID Trim/Ordinal, ngày nhận, decimal, khai phần đủ điều kiện và xác nhận rõ; validation âm/null/enum/qualified vượt paid, không suy theo label.
- [ ] AC-2: AC2: Rule S15b/factory riêng liên kết TaxRuleSet theo kỳ; OT/night năm2026 và meal cap1.2m chỉJulDec, thiếu/sai coverage/rule bị reject; seed cũ không đổi; căn cứ điều8/26/46/69/70 Unverified đúng version/kỳ.
- [ ] AC-3: AC3: Tính meal gộp cùng tháng áp trần một lần, phân bổ per-line ổn định; OT/night miễn phần đủ điều kiện caller xác nhận và phần vượt chịu thuế; không dùng công thức premium cũ hay hardcode cap trong logic.
- [ ] AC-4: AC4: Luồng tháng chung với bonus tích hợp đúng một lần, gross gồm cả miễn, exempt và taxablegross tách rõ; ví dụ độc lập gross55.8m/exempt5.5m/TNTT34.8m/PIT3.46m, không bỏ qua extras trong API cũ.
- [ ] AC-5: AC5: AnnualSalaryCalculator đủ12tháng tính lại raw annual, clamp một lần, tổnggross385.8m/exempt5.5m/TNTT194.3m/PIT13.43m; actualwithheld11.4m/additional2.03m; BH riêng không tự đổi base/trừ2lần, không tự sinh withheld.
- [ ] AC-6: AC6: Chặn trùngID cảbonus/extras trong và khác tháng, sai ngày/kỳ/năm/rule/coverage/gap, giữ overflow; collections bất biến và từng dòng giữ input Missing/rule basis Unverified đúng snapshot; decimal không rounding.
- [ ] AC-7: AC7: Giữ constructor/caller/89test cũ và output không có extras/null-default/empty; provider S15a cũ vẫn dùng được khi không extras, chỉ S15b cần rule mới; không thay semantics regression.
- [ ] AC-8: AC8: Test mới expected độc lập bao phủ miễn toàn bộ/từng phần/0, meal dưới-đúng-trên cap/gộp/reset/tháng6-7, OTJan, fractions, BH/withheld, annual clamp, customcap/basis và invalid/mutation; Core rồi solution thực chạy xanh no-restore.
- [ ] AC-9: AC9: Docs0007 đầy đủ API/phạmvi/overlap/nguồn/điềukhoản/chuyểntiếp/mealJulOnly/vídụ/testthật/giớihạn; diff S15b chỉ phạmvi cho phép, không sửa ledger/docs cũ/PLAN/config/package, giữ nguyên index S15a; user tựcommit.
- [ ] AC-10: AC10: Review độc lập toàn bộ diff riêng S15b và AC không còn phát hiện nghiêm trọng chưa xử lý; root chốt bằng chứng và bàn giao đầy đủ sau mã/test/docs.

## Ngoài phạm vi
S15a đã hoàn tất và S15c/d/e/f; phụ cấp khác, suất/phiếu ăn/hiện vật, trả ngày chưa nghỉ phép, nguồn khác/sau nghỉ việc, năm ngoài 2026, tiền ăn tháng 1–6. Loại chưa được mô hình hóa bị loại bằng hợp đồng API/docs; nếu biểu diễn qua enum unsupported thì reject. Không UI/DB/đọc file/chấm công/gross-up/tự kiểm hồ sơ. Không package/network Core, sửa PLAN/config/global/solution/csproj/Web/Data/docs0001–0006/ledger cũ. Chỉ cho phép Core/tests/docs0007/ledger phiếu hiện tại; giữ verify.command trống. Bint Core quản lý trạng thái/verify, agent không approve/status/verify.

## Câu hỏi mở
Không có câu hỏi cản trở triển khai.
