# Yêu cầu

## Mục tiêu và phạm vi
S15c/BL-23c thuộc Bước 1.5 PLAN.md, sau S15b đã done (01M7YD4AHRXZR4V4E4X9TNVPP5). API riêng tính và ghi nhận khấu trừ tại nguồn theo từng lần chi trả hoàn chỉnh, cho một cá nhân cư trú, tiền lương/công/thù lao không kinh doanh, bằng VND tại Việt Nam, hoàn toàn chịu thuế và được caller phân loại thuộc Điều 50(2). Phạm vi hỗ trợ tính là ngày thực nhận 01/07–31/12/2026; không phải quyết toán năm.

B-req deep chỉ ghi ba artifact qua Bint. Đã đọc requirement/plan roadmap 01M7WZSJJDNS1D97A7J19YN37T và phụ thuộc S15b; phiếu không có parent, không cần tách thêm giai đoạn. Không sửa mã/test/docs sản phẩm hoặc chạy kiểm thử .NET ở bước này.

## Yêu cầu chức năng
1. Input bất biến giữ PaymentId/PayerId Trim, so sánh Ordinal, ReceivedOn, Paid decimal không âm, nullable ActualWithheld, scope và subject explicit. Scope xác nhận đầy đủ cư trú/không kinh doanh/fully taxable/VND/tại Việt Nam. Subject chỉ NoContract, ContractUnderThreeMonths hoặc PostTerminationArticle50Paragraph2 do caller xác nhận; Unspecified, enum lạ và scope ngoài hỗ trợ bị từ chối, không suy từ nhãn.
2. Một payment là toàn bộ một lần chi; caller tự gộp các thành phần, không chia nhỏ để tránh ngưỡng, không trùng với gross nơi khác. API single và batch cùng một payer, một cá nhân, năm 2026; batch không rỗng, cấm mixed payer và trùng ID sau Trim kể cả khác ngày. A/a khác nhau. ID không phát hiện đổi mã hoặc khoản đã ẩn trong gross.
3. Rule gắn TaxRuleSet qua tham số trailing tùy chọn mới, factory H2/2026 riêng không cần InsuranceRegion. Rule có ID/version/year/period/coverage/timing/mode/supported subjects, threshold/rate và LegalBasis theo nhánh. Seed threshold 5.000.000, rate 0,10; calculator chỉ đọc rule, không dùng PIT lũy tiến, giảm trừ hay BH. Giữ nguyên seed/factory/constructor cũ.
4. Chế độ mutually exclusive, phải chọn tường minh: Normal dưới threshold trả expected 0, từ threshold trở lên trả Paid*Rate; VoluntaryBelowThreshold chỉ hợp lệ dưới threshold và trả Paid*Rate; CallerConfirmedCommitment trả expected 0 tạm thời khi có đủ xác nhận. Voluntary tại/trên threshold bị reject như giới hạn API; không đưa ra kết luận ưu tiên pháp luật.
5. Commitment phải xác nhận riêng cả ba điều: chỉ có thu nhập thuộc diện tỷ lệ này; ước tính tổng chịu thuế sau giảm trừ gia cảnh chưa phát sinh nghĩa vụ; cam kết đúng mẫu đã gửi và được payer tiếp nhận để áp dụng. Thiếu/sai xác nhận, commitment trong mode khác hay mode commitment thiếu đối tượng bị reject. EvidenceReference là tùy chọn tự khai; không tạo mẫu 08/CK, không thêm điều kiện MST hoặc xác minh hồ sơ/thu nhập khác.
6. ActualWithheld nullable do caller nhập, không âm, giữ nguyên dù khác expected; không áp trần bằng expected/Paid vì cần ghi nhận số thực khai cả khi cần đối chiếu. Difference = ExpectedWithheld - ActualWithheld chỉ có khi biết actual; có thể âm, không mang nghĩa hoàn/nộp thêm cuối năm. Batch TotalActual/TotalDifference null nếu bất kỳ dòng nào chưa biết actual. Không rounding hoặc nuốt decimal overflow; không cần thêm net/gross-up.
7. Result giữ input, paid/expected/actual/difference, decision reason, snapshot TaxRuleSet và casual rules thực dùng, căn cứ threshold/rate/request/commitment/timing/period/transition. Input amount/subject/actual/declaration có provenance Missing; derived decision dựa đúng rule, không biến chứng từ tự khai thành Verified. Collection đầu vào/rule/kết quả được copy/read-only.
8. Không fallback latest/bộ cũ khi thiếu rule, gap, ngày/kỳ/năm sai, mode/timing/subject không hỗ trợ hoặc metadata basis không hợp lệ. Guard kiểm cả coverage tháng được provider chọn và ngày thực nhận. Tất cả dòng, kể cả Paid=0, đều phải hợp lệ.
9. Giữ 103 test hiện có và semantics S11–S15b; API mới không được tự nối SalaryMonthInput hoặc AnnualSalaryCalculator. B-feat hoàn tất mã, test, docs0008 và bằng chứng AC1–9 trước review; AC10 do root chốt sau review độc lập.

## Giả định và căn cứ
Nguồn chính là [NĐ 253/2026/NĐ-CP bản ký](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf): scan trang 35–36 Điều 50(2), trang 30 Điều 46(3), trang 56–57 Điều 69(1), 69(1)a và 70(2). Đã xem các PNG có sẵn ngoài repo; không tải lại hoặc cài PDF dependencies. [Giải đáp chính thức của Thuế TP Hà Nội trên Báo Chính phủ](https://baochinhphu.vn/tra-luong-sau-khi-nghi-viec-tru-thue-tncn-ra-sao-102260910093753041.htm) đã mở lại để đối chiếu phạm vi sau nghỉ việc và nhánh theo lần chi. Không lấy câu hỏi MOF hoặc ví dụ PLAN làm kết luận luật.

Hiệu lực văn bản 01/07/2026, áp dụng lương cư trú kỳ 2026 và chuyển tiếp không khai lại tháng/quý trước hiệu lực là ba nội dung khác nhau. H2 là giới hạn hỗ trợ S15c, không phải kết luận luật chỉ áp H2; 31/12 là hết coverage dự án, không phải hết hiệu lực văn bản. Không suy ngược threshold mới cho khấu trừ lịch sử JanJun. Basis mới giữ Unverified/VerifiedOn null vì chưa đối chiếu đủ contract vbpl.vn; metadata có ID/version/điều-khoản/URL HTTPS/kỳ/summary Việt 2–3 câu. MCP không trả tri thức liên quan, không có mục [k:] được áp dụng.

## Tiêu chí chấp nhận
- [ ] AC-1: AC1: Input khoản chi fullytaxable nonbusiness của cá nhâncưtrú thuộc50(2) tườngminh, ID/payer TrimOrdinal/date/decimal và phânloại, không suy từlabel; reject unsupported/default/âm/null.
- [ ] AC-2: AC2: Rule khấutrừ gắnTaxRuleSet/factoryS15c riêng H2/2026 cóthreshold5m/rate10% chỉtrongseed, lookupđúngngày/snapshot, căn cứ50(2)/46(3)/69/70 Unverified đủmetadata, giữseed/constructorcũ; rule/căn cứ khônghợp lệ đượcchặn.
- [ ] AC-3: AC3: Normal dưới5m khôngkhấutrừ, từ5m khấutrừ10% trêntoànbộpaid; requestedbelow explicit áp10%, validcommitment chỉtạmchưakhấutrừ cóxácnhậnđủ, thiếu/conflict khôngngầmmiễn; decisionreasonvàbasisđúngnhánh.
- [ ] AC-4: AC4: Tính theo từnglầnchitrả hoànchỉnh, batchcùngpayer khôngcộngngưỡngtháng,3m+3mnormal0 khác6msingle600k; duplicateIDbịrejectvà giữchitiếttừngdòng bấtbiến.
- [ ] AC-5: AC5: Expectedwithheld tách nullableactualwithheld và chênhlệchkhicóactual; khônggánunknown0 hoặc épactual==expected, no familydeductions/BH/annualsettlement; giữdecimal khôngrounding và overflow.
- [ ] AC-6: AC6: DatecoverageH2,June30/Jan2027/outofperiod/gap/missingrule/year/version/HTTPS/basis/supportedsubject/ruleinvalid rejectrõ, input/result/rulescopy-readonly, derivedsourceđúngsnapshot và inputMissing; negativefixtures côlập từngđiềukiện cóvalidcontrol.
- [ ] AC-7: AC7: Giữ103test/API/seed/tháng/năm S11–S15bnguyênsemantics/metadata; khôngtựnối vàoAnnualSalaryCalculator, khôngS15d/nhiềunguồnhayUI/DB/config/package; factorymới khôngcầnsuyBHregion.
- [ ] AC-8: AC8: Testexpectedđộclậpđủ0/dưới-đúng-trên5m/fraction/normalvsvoluntary2.5m/commitmentđủ-thiếu/actualunknown/datebiên/batch/copy/invalid/customrule; Core rồi solution no-restorethựcchạyxanh và103regressiongiữ.
- [ ] AC-9: AC9: Docs0008 đủAPI/đầuvào/branch/căn cứ/H2scopevslegalannualapplicability/vídụPLANcũ/khôngquyếttoán/giớihạn/testcountthật/usertựcommit; diffchỉCore/testmới/docs0008/ledgerphiếuS15c, index/HEAD/config/PLAN/oldledgerkhôngđổi; AC1–9đủbằngchứngtrướcreview.
- [ ] AC-10: AC10: Reviewđộclập đủdiffS15c/AC sauhoànthiệnmã-test-docs khôngcònlỗinghiêmtrọngchưaxửlý; rootchốtbằngchứngvàbàngiaođúngtrạngthái nguồnUnverified/usercommit.

## Ngoài phạm vi
S15b company policy/payroll/chấm công, S15d nhiều nguồn/ủy quyền/ngưỡng 15 triệu/quyết toán, các bước roadmap khác; không cư trú, kinh doanh/freelance thuộc kinh doanh, HĐLĐ từ 3 tháng đang làm, mixed taxable+exempt, hiện vật/ESOP/ngoại tệ/net gross-up, năm hoặc ngày ngoài hỗ trợ, lịch sử TT111/threshold 2 triệu. Không UI/DB/network Core/packages/framework kho luật.
Chỉ Core, test mới, docs/decisions/0008-casual-withholding.md và ledger phiếu S15c khi triển khai. Không sửa PLAN/config/model/global/solution/csproj/docs0001–0007/ledger cũ. Không đổi Git index/HEAD, không stage/unstage/stash/reset/commit/push; user tự commit. Giữ verify.command rỗng, không task approve/status/verify dưới Bint Core.

## Câu hỏi mở
Không có câu hỏi cản trở triển khai; các lựa chọn kỹ thuật được chốt trong plan, không cần hỏi lại quyền tra cứu hoặc dữ liệu đã có.
