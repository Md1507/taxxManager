# Kế hoạch

## Hướng triển khai
Mở rộng SalaryMonthInput và luồng hiện tại bằng helper chuẩn bị thu nhập dùng chung cho BonusMonthlySalaryCalculator và AnnualSalaryCalculator. MonthlySalaryCalculator vẫn trừ BH/giảm trừ/clamp và gọi PIT; annual tính lại raw năm theo S14. S15b là giai đoạn giao được riêng của roadmap, không cần tách thêm phiếu hoặc làm S15c–f. B-req chỉ viết artifact, không thực thi mã/test.

## Giả định và mâu thuẫn
Đã đọc Core, các caller/tests, requirement/plan roadmap và phụ thuộc S15a đã done. 89 test là baseline docs0006, chưa chạy lại trong lượt này. MCP không có tri thức liên quan. S15a đang staged trên HEAD363cdaf; SHA256 index khi khảo sát: 635E04807B85092D382DE12CF35B3558545A044F0360F4E506C145F4CC257B10; git diff --name-only ban đầu rỗng. Giữ toàn bộ nền S15a và index, không stage/unstage/reset/stash/commit/push.

Gross hiện tại cần tiếp tục biểu diễn ALL paid, thêm exemption/taxablegross thay vì đổi GrossIncome thành số sau miễn. SameTax trong annual chỉ so PIT/giảm trừ; không thêm extras vào phép so vì JanJun/JulDec hợp lệ khác nhau. Giữ guard PIT/BH/policy S14, kiểm extras tại từng tháng có dòng. GetForYear từ chối nhiều snapshot nên dùng GetForMonth.

Caller xác nhận đối tượng và qualified dựa trên tài liệu lao động; engine không chứng minh chứng từ. Không suy mọi OT/night được miễn hoặc chỉ miễn premium. Rule coverage là kỳ tính thuế, không lấy ngày hiệu lực văn bản để loại OTJan. Không có giả định đổi cách làm cần Q; yêu cầu hiện tại user tự commit ưu tiên hơn roadmap cũ.

## Lựa chọn và lý do
(A) Calculator extras độc lập rồi adapter trừ gross trước API cũ; (B) helper chung và optional exemption trong MonthlySalaryCalculator. Chọn B để giữ nghĩa gross, không tạo luồng năm thứ hai hay nhân bản PIT, giữ per-line provenance. BonusMonthlySalaryCalculator xử lý extras bằng cùng helper, không cần entrypoint mới bắt buộc.

Rule có thể hardcode if kind/cap trong calculator hoặc dùng entries kind→mode/cap gắn TaxRuleSet. Chọn entries bất biến; lookup kind chọn entry, dispatch theo mode, cap đọc từ entry. Seed OT/night dùng CallerConfirmedQualifiedAmount, meal dùng MonthlyAggregateCap. Chỉ hỗ trợ hai mode, reject unknown. Meal phân bổ theo ReceivedOn tăng dần rồi ID Trim/Ordinal nên đảo list không đổi kết quả per-ID.

## API, tệp và trình tự triển khai
Tên mới là đề xuất, có thể đổi nhất quán khi triển khai nhưng giữ hợp đồng.
1. Thêm src/TaxManager.Core/SalaryExtras.cs: enum SalaryExtraKind gồm Unspecified=0, Overtime, NightWork, CashMeal; SalaryExtra bất biến có Id, Kind, PaidAmount, ReceivedOn, nullable LaborLawConditionsTimeAndLimitQualifiedAmount, enum xác nhận CallerConfirmedConditionsTimeLimitsAndSupportingRecords; Label/EvidenceReference tùy chọn chỉ hiển thị/tự khai. Validate ID/decimal/enum/qualified trong [0,paid]. Prepare theo qualified mode bắt buộc amount và confirmation kể cả qualified=0; thiếu không mặc định miễn. Meal cap không nhận qualified thay trần; reject trường qualification không áp dụng.
2. Thêm SalaryExtraRuleSet cùng tệp hoặc src/TaxManager.Core/SalaryExtraRules.cs: ID/version/year/PeriodFrom/To/timing, entries copy/read-only theo kind; entry gồm mode, MonthlyCap nullable và ExemptionBasis. Qualified mode không có cap; cap mode có cap không âm. Reject null/trùng kind/invalid enum; timing chỉ ReceivedOrPaidDate. Giữ TimingBasis/ApplicabilityBasis/TransitionBasis và meal-specific applicability. Basis không Missing, đủ ID/version/HTTPS/article/period, version nội bộ nhất quán; coverage tường minh phải phủ kỳ. Không ép version extras bằng version PIT/BH kế thừa, không I/O mạng.
3. Sửa src/TaxManager.Core/TaxRuleSet.cs: property SalaryExtraRules và optional trailing constructor arg. Factory CreateVietnam2026SalaryExtrasProvider(region) lấy snapshot tháng1/7 từ CreateVietnam2026CashBonusProvider, copy nguyên tax/BH/rounding/annual/bonus references rồi thêm extras. Hai registration JanJun/JulDec đều có OT/night; chỉJulDec có CashMeal cap1_200_000m. Cap số chỉ ở seed. Không sửa dữ liệu/metadata seed/factory S11/S13/S14/S15a.
4. Sửa SalaryMonthInput trong src/TaxManager.Core/AnnualSalaryCalculator.cs: extras optional trailing sau bonuses, copy read-only, null collection thành empty; null item reject khi prepare. GrossIncome là base chưa có bonus/extras. Giữ positional constructor/deconstruction result cũ; thêm properties breakdown vào SalaryMonthResult/AnnualSalaryResult/BonusMonthlySalaryResult.
5. Thêm internal SalaryIncomePreparation trong SalaryExtras.cs hoặc src/TaxManager.Core/SalaryIncomePreparation.cs: IDs chung bonus+extras dùng Trim/Ordinal; gọi CashBonusCalculation.Prepare giữ S15a, chuẩn bị extras theo snapshot thực dùng. Kiểm year2026/monthly period/coverage cả tháng/ReceivedOn/timing/kind entry/mode; zero line vẫn phải hợp lệ. Không dòng không đòi rule extras. Trả base, bonus total/lines, extras paid/exempt/taxable/lines, ALLgross=base+bonus+paid, taxablegross=ALLgross-exempt; không catch decimal overflow.
6. Qualified mode miễn đúng amount xác nhận, taxable=paid-exempt. Cap mode cộng paid cùng kind/tháng, remaining=min(sum,cap); duyệt ReceivedOn/ID, exempt=min(paid,remaining) rồi trừ remaining. Không cap mỗi dòng. Output theo thứ tự phân bổ, mỗi line giữ input/year/month/paid/exempt/taxable/cap/mode và rule entry/snapshot/basis thực dùng. Cap lặp là metadata, không sum thành tổng trần. InputBasis/QualifiedInputBasis=Missing; derived exemption/timing/applicability liên kết rule, không giả chứng từ.
7. Sửa src/TaxManager.Core/Calculators.cs: thêm optional trailing decimal exemptIncome=0m vào Calculate, validate 0<=exempt<=gross. Before=gross-exempt-insurance-personal-dependent; clamp một lần rồi PitCalculator cũ. MonthlySalaryResult thêm ExemptIncome/TaxableGrossIncome bằng properties tương thích; GrossIncome vẫn ALL paid, không extras giữ số học/basis cũ. Không sửa công thức PIT.
8. Sửa src/TaxManager.Core/CashBonuses.cs: BonusMonthlySalaryCalculator dùng helper chung, BH từ input tính một lần; gọi MonthlySalaryCalculator với ALLgross và exemption. Bổ sung extras/exempt/taxablegross vào result, giữ base/bonus và ActualWithheldTax. Không silently ignore list extras.
9. Sửa AnnualSalaryCalculator: preflight IDs bonus+extras toàn12tháng, chặn cross-kind/cross-month kể cả zero. Vòng tháng dùng helper/BH cũ/calculator tháng mở rộng; giữ immutable details. Cộng raw gross/exempt/insurance/withheld/dependentMonths. Taxablegross=gross-exempt; before=taxablegross-insurance-annualPersonal-dependentDeduction; clamp một lần rồi PIT năm; delta=PIT-withheld. Annual.GrossIncome vẫn ALL paid, thêm ExtrasPaid/Exempt/Taxable/TaxableGrossIncome; không sum PIT/TNTT đã clamp tháng.
10. Thêm tests/TaxManager.Core.Tests/SalaryExtrasTests.cs, SalaryExtraRulesTests.cs, SalaryExtrasMonthlyTests.cs, SalaryExtrasAnnualTests.cs (có thể gộp); thêm docs/decisions/0007-overtime-night-meal.md. Không cần sửa test cũ, TaxRuleProvider, BH/AnnualSalaryRules hoặc ledger cũ.

## Căn cứ, coverage và provenance
Nguồn chuẩn [scan NĐ253/2026/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf), đã mở PDF official61trang và xem PNG trang5/18/30/56/57 có sẵn ngoài repo. Điều8(2)g cho meal; Điều26(1),(3) cho qualified/over-limit; Điều46(3) cho ngày nhận; Điều69(1)a,b cho kỳ2026/mealJulOnly; Điều70(2) cho chuyển tiếp. Không dựa vào trang MOF164026 chưa lấy được toàn văn hoặc typo d/g của MOF164679; không draft/blog/TT111 cũ.

Statutory effective01/07/2026 khác applicability lương cả kỳ2026; meal chỉJulDec. Kết quả tháng là estimate, không thay ActualWithheldTax hoặc yêu cầu khai lại tháng/quý trước hiệu lực. LegalBasis.EffectiveFrom có thể null khi RuleSet đã biểu diễn coverage áp dụng; không bịa01/01 là ngày hiệu lực để qua guard. Basis.Period thống nhất "2026", version nội bộ nhất quán; coverage kiểm từng snapshot, không ép cùng rule cả năm. SourceUrl official trên, DocumentNumber/Article/Clause/Point/kỳ/version rõ, Summary Việt2–3câu. Giữ Unverified/VerifiedOn null vì chưa có vbpl URL đã đối chiếu theo hợp đồng Verified. Qualified/evidence reference vẫn tự khai; không chép PDF vào repo hoặc đổi metadata S15a.

## Vùng ảnh hưởng
Callers SalaryMonthInput/AnnualSalaryCalculator và BonusMonthlySalaryCalculator có thêm extras. Callers decimal MonthlySalaryCalculator/InsuredMonthlySalaryCalculator cũ giữ exemption=0; API decimal không nhận list. Kết quả mới giữ breakdown và snapshot theo tháng cho consumer hiển thị. Không UI/DB/persisted data/dependency mới; Core không truy cập mạng.

## Rủi ro và rollback
Double count gross, trừ miễn/BH hai lần, sum TNTT tháng hay cap mỗi dòng được phát hiện bằng expected tay và invariants base+bonus+paid=ALLgross, paid=exempt+taxable, taxablegross=ALLgross-exempt. Invalid/fixture/mutation tests bắt sai eligibility/date/coverage/provenance. Regression89 và omitted/null/empty bảo vệ API cũ. MealJune30/July1 và OTJan bắt nhầm hiệu lực/kỳ.

Docs buộc caller tách base/bonus/extras và OT/night không overlap, đặc biệt night pay đã nằm trong lương; different ID không chứng minh khác khoản. Review dùng bint task diff so snapshot trước b-feat, không git diff HEAD lẫn stagedS15a. Rollback chỉ hoàn tác từng thay đổi S15b theo snapshot đó, giữ S15a/index/ledger cũ; không reset/checkout/stash/clean diện rộng. Ngừng dùng factory mới vẫn giữ API/seed cũ; không có schema cần rollback.

## Chiến lược kiểm thử
Expected literal/phép tính tay độc lập, không gọi production sinh oracle; thêm test theo nhóm:
- OT/night paid3m qualified3m/2m/0 cho miễn3m/2m/0, taxable0/1m/3m. Thiếu amount/confirmation, unspecified/false confirmation, qualified âm/>paid reject.
- Meal800k/1.2m/1.5m miễn800k/1.2m/1.2m; 800k+700k cùng tháng miễn1.2m, taxable300k; đảo list vẫn phân bổ đúng ReceivedOn/Ordinal. Tháng khác reset. Meal1_200_000.5 taxable0.5 không rounding.
- Tháng BH0/NPT0: base30m+bonus20m+OT3m đủ3m+night1.3m đủ1.3m+meal1.5m => paidextras5.8m, gross55.8m/exempt5.5m/taxablegross50.3m/TNTT34.8m/PIT500k+2m+960k=3.46m, withheld950k nguyên vẹn. Khôngextras PIT950k.
- Năm12base30m, bonus/extras chỉtháng12 => gross385.8m/exempt5.5m/taxablegross380.3m/GTBT186m/TNTT194.3m/PIT6m+7.43m=13.43m; withheld11.4m/additional2.03m/overpaid0, khác tổng PIT tháng13.91m.
- Annual clamp riêng: chỉtháng12 base30m+OT3m đủ2m, 11tháng0 => gross33m/exempt2m/taxablegross31m/before=-155m/TNTT0/PIT0; không dùng tổng TNTT tháng15.5m hoặc thuế tháng1.05m. Đối chiếu raw before để bắt trừ miễn hai lần.
- Timing OTJan1/Dec31 hợp lệ; mealJune30 reject kể cảzero/July1accept; sai tháng/năm, Dec31/Jan1 năm ngoài2026 reject. Label không routing; annual không dòngJanJun vẫn chạy đủ12tháng. Thiếu/trùng tháng hoặc gap provider reject.
- IDs Trim/Ordinal trùng trong extras, bonus/extras cùng và khác tháng/kind/zero; A/a khác hợp lệ. Null item/input, blankID, âm, kind/mode/timing/confirmation invalid, thiếu rule/entry, coverage bắt đầu giữa tháng, year/period/basis/version mismatch reject.
- Custom cap900k cho meal1.5m => miễn900k/taxable600k; Jan/Jul ruleID/version/basis khác hợp lệ phải giữ đúng từngtháng. Thêm/tháo entry bắt lookup kind hardcode; mode lạ hoặc cap không tương thích reject, không fallback1.2m. Fixture giả không gắn là luật thật.
- BHauto trước/sau extras/bonus giữ contributionbase/DeductibleTotal, tháng6/7 đúng snapshot; manual trừ một lần, NPT không lặp. Withheld0/950k giữ input. Overflow tổng dòng/base/năm và khoản trừ lớn phải nổi lên.
- Copy input rồi Clear/replace không đổi snapshot; rules/results/lines read-only. Kiểm input/qualified Missing, rule basis Unverified đúng version/period/url/article/summary, VerifiedOn null. Omitted/null/empty với provider S14/S15a giữ API/số học/basis và bonus vẫn tính đúng.

Implementer chạy baseline89 trước sửa, rồi Core và solution sau sửa tuần tự bằng SDK/cache đã có; không restore/install. Đây là test development user giao b-feat, không đặt verify.command hoặc chạy task verify/approve. PowerShell:
    $env:DOTNET_ROOT = Join-Path (Get-Location).Path '.dotnet'
    $env:NUGET_PACKAGES = Join-Path (Get-Location).Path '.dotnet/nuget-packages'
    $env:DOTNET_CLI_HOME = Join-Path $env:TEMP 'taxxmanager-dotnet-cli'
    & ./.dotnet/dotnet.exe test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore --disable-build-servers -m:1
    & ./.dotnet/dotnet.exe test TaxManager.sln --no-restore --disable-build-servers -m:1
Kiểm exit code từng lệnh, không che lỗi; ghi passed/failed/skipped và SDK10.0.404 thực vào ledger/docs0007. Chưa kiểm ở b-req: build/test .NET, tính đúng triển khai, hồ sơ lao động/chứng từ, thẩm định pháp luật đầy đủ hoặc UI/DB. Lint chỉ kiểm hợp đồng tài liệu.

## Checklist nhất quán và bàn giao
AC-1→TSK-1; AC-2→TSK-2; AC-3→TSK-3; AC-4→TSK-4; AC-5→TSK-5; AC-6→TSK-6; AC-7→TSK-7; AC-8→TSK-8; AC-9→TSK-9; AC-10→TSK-10. Mỗi TSK đúng một [AC-N], có xong khi đo được. Model/rule→miễn→tháng/năm→validation/compatibility→test→docs hoàn chỉnh→review.

Allow đủ mọi tệp Core/tests/docs0007 và .bint/tasks/01M7YD4AHRXZR4V4E4X9TNVPP5/**; glob ledger rộng không cho phép sửa ledger cũ. Không protected_paths/PLAN/config/global/solution/csproj/Web/Data/docs0001–0006/packages. Planner chỉ ghi ba artifact qua stdin/lint --strict, giữ verify.command rỗng, không status/approve/verify dưới Bint Core.

B-feat xong mã/test/docs và đối chiếu AC1–AC9 trước review. Reviewer độc lập dùng bint task diff từ snapshot trước b-feat, kể cả tệp mới; root xử lý findings/chốt AC10 sau mã/test/docs/review đủ bằng chứng, không force cổng. Bint Core/root điều phối bint run b-feat --task 01M7YD4AHRXZR4V4E4X9TNVPP5; giữ config/models/index hiện tại, user tự commit.
