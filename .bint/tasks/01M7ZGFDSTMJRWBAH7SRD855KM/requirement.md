# Yêu cầu

## Mục tiêu và phạm vi
Chỉ S15d/BL-23d của Bước 1.5 PLAN.md, sau S15c done 01M7YTT7JTYQ91DH59N7E3YXVY; tham chiếu roadmap 01M7WZSJJDNS1D97A7J19YN37T. Tổng hợp tiền lương gross thông thường hoàn toàn chịu thuế của đúng một cá nhân Việt Nam cư trú, năm dương lịch 2026, VND trong nước, nhiều nơi đồng thời hoặc đổi nơi giữa năm. Kết quả là ước tính thuế năm so với khấu trừ thực tế, có chi tiết từng khoản/nơi/tháng và căn cứ. Không xác định nghĩa vụ khai hoặc quyền được hoàn.

B-req deep chỉ ghi ba tài liệu của phiếu qua stdin; chưa sửa Core/test/docs0009, chưa chạy .NET hoặc hoàn thành bất kỳ AC triển khai nào.

## Yêu cầu chức năng
1. Hợp đồng explicit gồm sổ năm, đúng 12 tháng kể cả tháng không thu nhập, nhóm payer duy nhất mỗi tháng và từng khoản có PaymentId/ReceivedOn/GrossPaid/nullable ActualWithheldTax/subject. Scope phải được caller xác nhận; không suy từ label. ID trim, so Ordinal; chặn null/âm/default/enum lạ, sai năm/ngày/tháng, trùng payer-tháng và PaymentId toàn năm. Copy và read-only mọi collection; không cần MST/CCCD.
2. Caller xác nhận một cá nhân, sổ đủ mọi thu nhập thuộc phạm vi tổng hợp trong năm, các khoản không trùng/chia/gộp sai và BH là tổng thực hợp lệ được trừ. Tháng không thu nhập được khai tường minh bằng danh sách payer rỗng: xác nhận 0 thu nhập và 0 actual trong tháng, không phải dữ liệu bị thiếu. Nhóm payer có mặt phải có khoản; khoản số 0 hợp lệ nhưng vẫn cần actual đã biết. Thiếu actual ở bất kỳ khoản nào thì không xuất kết quả quyết toán; không thay null bằng 0 hoặc số dự kiến.
3. Cộng gross và actual theo payer/tháng rồi toàn tháng, chuyển thành đúng 12 SalaryMonthInput không bonus/extras và gọi AnnualSalaryCalculator một lần. Không tính lũy tiến hay quyết toán từng payer rồi cộng. Giữ tất cả input và tổng theo nguồn-tháng để kiểm tra được.
4. Mỗi tháng chỉ nhận một DeductibleInsurance decimal nhập tay không âm, không InsuranceInput/auto BH hay phân bổ về payer. Caller chịu trách nhiệm xác định khoản đóng hợp lệ và chứng từ, loại phần employer/không được trừ, tránh cộng trùng; API không áp trần BH theo gross hoặc số nơi làm. AnnualSalaryCalculator dùng giảm trừ bản thân năm và lịch DependentPeriod một lần; chặn chồng khoảng cùng dependent ID theo engine cũ.
5. Rule nhiều nguồn gắn TaxRuleSet bằng tham số trailing optional; factory bọc snapshot annual hiện có, giữ seed số thuế/giảm trừ và policy không làm tròn. Rule/basis/snapshot đúng năm/kỳ/coverage, đủ metadata; không fallback. Các snapshot thực dùng xuất theo tháng; annual result giữ annual tax rule thực dùng. Basis mới Unverified/VerifiedOn null; input/cam kết/actual/BH Missing. Overflow phải báo lỗi.
6. Phạm vi nhỏ chỉ lương thông thường hoàn toàn chịu thuế từ quan hệ lao động thông thường do caller xác nhận. Không có CashBonus/SalaryExtra/CasualPayment adapter trong S15d: treatment thưởng hiện có nói current employer, extras có quy tắc miễn/trần riêng, casual có subject/cam kết/actual khác. Sổ đã trộn khoản ngoài phạm vi không được xác nhận là hợp lệ; caller không được thêm lại khoản đã chứa trong tổng gross. Không thay engine S11–S15c để nhận nhiều nguồn.
7. Bộ test mới có expected literal tính độc lập, fixture tham số khác seed và valid control từng guard; giữ nguyên semantics và 122 test cũ. Chạy Core rồi solution bằng SDK/cache sẵn, không restore/cài package. Hoàn tất mã/test/docs0009 và bằng chứng AC1–7 trước review độc lập; AC8 do reviewer/root chốt sau.

## Tiêu chí chấp nhận
- [ ] AC-1: AC1: Input nhiều payer/tháng rõ phạm vi một cá nhân cư trú 2026, đủ coverage năm và zero explicit, ID/duplicate/null/âm/year/subject validation, collection bất biến.
- [ ] AC-2: AC2: Tổng hợp đồng thời và đổi việc giữa năm, thu nhập và actual được giữ từng nguồn-tháng, không đếm trùng hoặc tính thuế riêng từng payer rồi cộng; actual unknown không thành0.
- [ ] AC-3: AC3: BH hợp lệ nhập tay một lần cấp cá nhân/tháng, giảm trừ bản thân và lịch phụ thuộc một lần cấp cá nhân; tái sử dụng quy tắc năm, kết quả thuế và delta/nộp thêm/nộp thừa đúng.
- [ ] AC-4: AC4: Căn cứ mới cho nhiều nguồn thuộc TaxRuleSet, không hardcode luật; rule/snapshot/kỳ/coverage hợp lệ, result đúng nguồn thực dùng, input Missing/nguồn Unverified, giữdecimal và overflow.
- [ ] AC-5: AC5: Giữ API/semantics và122 test S11–S15c, chỉ many-salary2026, khôngautoBH/autoquyếttoáncasual/miễn phụ cấp theo payer/S15e/f/Web/DB/config/package; trách nhiệm caller rõ.
- [ ] AC-6: AC6: Bộ test có expected độc lập đủ simultaneous/switch/onepayer/zero/dependents/insurance/refund/additional/actual/duplicate/invalid/customrule/provenance/copy/overflow; Core và solution thực chạy xanh no-restore.
- [ ] AC-7: AC7: Docs0009 đủAPI/căn cứ/phạmvi/ví dụ/testcountthật/giới hạn và usertựcommit; AC1–7 hoàn tất trước review; scope/index/HEAD nguyên theo baseline.
- [ ] AC-8: AC8: Review độc lập đủdiff/AC không còn lỗi nghiêm trọng chưa xử lý, root chốt bằng chứng/trạng thái nguồn đúng thực tế và bàn giao chưastage/commit.

## Ngoài phạm vi
Bonus nhập dòng hoặc bonus lẫn trong gross, phụ cấp miễn/mixed gross, overtime/night/meal S15b, casual/khấu trừ 10%/cam kết S15c, adapter giữa các engine; S15e/f từ thiện/hưu trí; không cư trú/người nước ngoài/kỳ 12 tháng khác năm dương lịch/ngoại tệ/net gross-up/kinh doanh. Không quyết định phải khai, miễn quyết toán, ủy quyền, nơi nộp hoặc hoàn tiền; không kiểm chứng hồ sơ hay dữ liệu thật.

Chỉ triển khai Core, test mới, docs/decisions/0009-multiple-salary-payers.md và ledger mới của phiếu này. Không sửa PLAN/config/global/solution/csproj/packages/Web/Data/docs0001–0008/ledger cũ; test cũ chỉ được điều chỉnh tương thích nếu có lý do và giữ assertions. Không đổi index/HEAD, stage/commit/push/reset/stash/unstage; người dùng tự thực hiện Git. verify.command giữ rỗng, không status/approve/verify dưới Bint Core.

## Câu hỏi mở
Không có câu hỏi cản trở triển khai. Các xác nhận scope/coverage/không trùng/BH là dữ liệu bắt buộc của API, không phải engine tự xác minh hoặc một bước duyệt mới.

