# Kế hoạch

## Hướng triển khai và quyết định đã chốt
Nối tiếp khảo sát S13 đã có, không lập lại roadmap/S12. Q-1/Q-2 đã trả lời: NLĐ Việt Nam lương doanh nghiệp, HĐLĐ thông thường đủ tháng đóng cả ba BH, chỉ2026, vùng I–IV tường minh; một lương đóng trước cap riêng với gross; NoRounding. Không còn gate câu hỏi mở. Các đoạn kế hoạch cũ chờ người dùng tự tra số liệu, nhận ba base độc lập, tự chọn nhiều mode hoặc bắt người dùng tính expected được thay bằng nội dung này. Vẫn chỉ sửa ba tài liệu ở b-req, không chạy b-feat.

Giữ API tiền BH nhập tay và provider S12 nguyên trạng; thêm API tự tính với kết quả riêng để lưu provenance đầy đủ. Gắn insurance/policy bất biến vào snapshot TaxRuleSet thay vì hardcode luật trong calculator. Dữ liệu có nguồn và Unverified không đồng nghĩa thiếu số liệu cản triển khai. MCP không có tri thức để cite; baseline39 xanh là bằng chứng S12/root ghi nhận, chưa phải nghiệm thu S13.

## API và khu vực mã
- TaxRuleSet.cs: thêm InsuranceRules/SalaryRoundingPolicy bằng tham số constructor tùy chọn cuối, null nghĩa chưa cấu hình. Giữ seed Vietnam2025/Vietnam2026 và lời gọi cũ nguyên trạng; thêm factory CreateVietnam2026SalaryProvider(region) tạo hai snapshot tổng hợp với cùng bậc/giảm trừ/căn cứ thuế hiện hữu, đăng ký rõ 01/01–30/06 và 01/07–31/12/2026. Mọi số luật, bảng vùng, rate và nhân tử20 nằm ở đây. Một provider cho một vùng/phạm vi; factory không lấy ngày hiện tại hoặc chọn vùng ngầm định.
- Thêm InsuranceRules.cs: InsuranceRuleSet/ba rule thành phần và SalaryRoundingPolicy bất biến, ID/version/phạm vi/khoảng tháng/căn cứ riêng. Phân biệt BhxhLegalFloor theo mức tham chiếu, cap BHXH/BHYT, cap BHTN và ScopeMinimumSalary theo vùng. Guard đủ-tháng là quyết định phạm vi liên kết nguồn minimum wage, không gán thành luật sàn chung; không tạo thêm sàn BHTN chưa đối chiếu. BHYT tham chiếu base BHXH đã giới hạn. RuleSet sao chép mọi collection đầu vào và chỉ đọc. Policy chỉ NoRounding, có ID/version và LegalBasis Unverified ghi quyết định người dùng; không thêm precision/midpoint mode/chế độ không dùng.
- Thêm InsuranceCalculator.cs: InsuranceInput(subjectId, region, salaryBase); subjectId chọn rõ nhóm VN doanh nghiệp đủ tháng, region I–IV phải có giá trị hợp lệ, salaryBase decimal trước cap. Không có ba base tùy ý hoặc cờ miễn/không tham gia; phạm vi khác báo chưa hỗ trợ. InsuranceCalculator.Calculate(input, ruleSet, year, month) kiểm snapshot BH/policy, phạm vi/vùng/tháng, salaryBase >= ScopeMinimumSalary rồi tính: baseBHXH=min(salaryBase,capBHXH); baseBHYT=baseBHXH; baseBHTN=min(salaryBase,capBHTN); từng raw=base áp dụng × rate. Không clamp lên floor; giữ floor/range trong model để giải thích và validate rule, không áp rộng ngoài phạm vi.
- InsuranceResult giữ base trước cap, ScopeMinimumSalary tách riêng, từng base áp dụng/rate/floor/cap, raw/applied và căn cứ tương ứng; totalRaw=totalApplied=DeductibleTotal là tổng ba số thô. Mỗi khoản giữ snapshot TaxRuleSet và InsuranceRuleSet/policy ID/version/kỳ. Cap suy tính giữ cả căn cứ nhân tử và nguồn mức tham chiếu/minimum vùng; tổng liên kết mọi khoản và policy. Input tự khai Missing; rate/cap/floor có nguồn Unverified, không tự Verified.
- Thêm InsuredMonthlySalaryCalculator.cs: Calculate(decimal grossIncome, InsuranceInput input, int dependents, int year, int month, TaxRuleProvider provider). Resolve đúng một snapshot GetForMonth; dùng InsuranceCalculator rồi MonthlySalaryCalculator cũ với DeductibleTotal để tính TNTT và PitResult thô. Trả InsuredMonthlySalaryResult riêng chứa các bước gross/giảm trừ/TNTT, InsuranceResult, PitResult và TaxAfterPolicy(raw,applied,policy,căn cứ); raw=applied. Không xuất Insurance.Basis.Missing của đường nhập tay như provenance BH tự tính. PitCalculator/Calculators.cs không sửa.
- TaxRuleProvider.cs giữ nguyên. Kỳ thuế 2026 và kỳ BH đăng ký tường minh, không lọc theo ngày hiệu lực chung Luật109; snapshot BH tự lưu phạm vi01/01–30/06 hoặc01/07–31/12. Nguồn/giá trị theo requirement; không dùng NĐ399 cho năm2026. Collection snapshot không đổi sau tính. GetForYear của provider có hai snapshot vẫn báo nhiều phiên bản theo semantics S12; API mới chỉ theo tháng.

## Validation
ArgumentNullException cho input/provider/rule bắt buộc null; ArgumentOutOfRangeException cho gross/base/phụ thuộc âm, rate ngoài[0,1], enum vùng không hợp lệ hoặc tháng/năm ngoài miền DateOnly. Subject không hỗ trợ hoặc năm2025/2027/kỳ gap báo NotSupportedException; base không âm nhưng dưới ScopeMinimumSalary cũng NotSupportedException có minimum/vùng/tháng rõ, không nâng lên. Vùng/phạm vi/khoảng snapshot không khớp dùng ArgumentException; null policy/BH trong snapshot dùng InvalidOperationException. Rule có sàn/trần âm/đảo, capBHXH nhỏ hơn floor/guard khiến không thể tính hoặc range không trọn tháng phải bị từ chối; provider kiểm overlap/duplicate. Không suy khớp kỳ từ Period string. Giữ OverflowException cho decimal tràn, không trả số0 hoặc bão hòa; không thêm ràng buộc insurance<=gross vào API cũ.

## Kiểm thử có expected độc lập
Thêm InsuranceRuleTests.cs, InsuranceCalculatorTests.cs và InsuredMonthlySalaryTests.cs trong tests/TaxManager.Core.Tests; fixture nhỏ cùng test. Giữ nguyên CalculatorTests.cs, RuleTests.cs, TaxRuleProviderTests.cs, Vietnam2025Tests.cs và39case. Không thêm package/csproj.

1. Bốn vùng: minimum−1/minimum/minimum+1 và phần lẻ; dưới guard từ chối, không nâng. Phân biệt guard với floor BHXH theo tham chiếu trong metadata; không kể case từ chối dưới guard là chứng minh luật sàn mọi nhóm. Mỗi cap BHXH/BHYT/BHTN kiểm cap−1/cap/cap+1 và phần lẻ, BHYT luôn theo baseBHXH; BHTN vẫn tăng khi BHXH đã cap, sau cap BHTN thì dừng. Seed caps tính độc lập từ20×mức nguồn, fixture rates/caps khác để bắt hardcode.
2. NoRounding: số lẻ và phần bằng0,5 được giữ nguyên ở từng khoản/tổng/thuế; raw=applied và DeductibleTotal đúng tổng thô. Không dựng nhiều rounding mode chỉ để test. Dùng ca dưới có tổng/thuế lẻ để bắt việc round từng khoản/tổng hoặc thuế; snapshot thiếu policy báo lỗi, input API nhập tay không đổi. Expected bằng decimal literal, không gọi implementation để tạo expected.
3. Snapshot:30/06–01/07/2026 đổi cap/căn cứ BH nhưng giữ bậc thuế;01/01 và31/12 chọn đúng bản,31/12/2025 và01/01/2027 không hỗ trợ tự tính. Kiểm gap/duplicate/overlap qua provider, vùng/phạm vi/kỳ không khớp, null/âm/overflow và cấu hình sai. Clear/sửa collection đầu vào sau tạo/sau tính không đổi kết quả; assert căn cứ riêng floor/rate/cap/guard/policy/tổng, ID/version/kỳ đúng, Unverified không thành Verified, raw input Missing đúng nghĩa.

Các ca đối chứng dưới tính độc lập theo thuế2026 hiện có, không phải xác minh pháp lý; tiền đơn vị đồng, không phụ thuộc engine để sinh expected:

| Tháng, vùng; gross / base / NPT | BHXH / BHYT / BHTN | TổngBH; TNTT; thuế raw=applied |
|---|---|---|
| 01, I; 30.000.000 / 30.000.000 / 0 | 2.400.000 / 450.000 / 300.000 | 3.150.000; 11.350.000; 635.000 |
| 07, II; 40.000.000 / 20.000.000 / 1 | 1.600.000 / 300.000 / 200.000 | 2.100.000; 16.200.000; 1.120.000 |
| 06, I; 100.000.000 / 80.000.000 / 0 | 3.744.000 / 702.000 / 800.000 | 5.246.000; 79.254.000; 14.276.200 |
| 07, I; 100.000.000 / 80.000.000 / 0 | 4.048.000 / 759.000 / 800.000 | 5.607.000; 78.893.000; 14.167.900 |
| 01, IV; 150.000.000 / 120.000.000 / 0 | 3.744.000 / 702.000 / 740.000 | 5.186.000; 129.314.000; 30.759.900 |
| 01, IV; 30.000.000 / 10.000.000,5 / 0 | 800.000,04 / 150.000,0075 / 100.000,005 | 1.050.000,0525; 13.449.999,9475; 844.999,99475 |

Ca đầu PIT=500.000+1.350.000×10%; ca đổi base=500.000+6.200.000×10%; hai ca biên cap=PIT8.500.000+(TNTT−60.000.000)×30%; ca cap vùngIV=PIT20.500.000+(TNTT−100.000.000)×35%; ca lẻ=500.000+3.449.999,9475×10%. Phép tính ca lẻ đã kiểm riêng bằng PowerShell decimal ở b-req, không phải chạy test ứng dụng. Không bắt người dùng tính expected lại.

Ở b-feat mới chạy lần lượt hai lệnh, kiểm exit code/passed/failed/skipped và bảo toàn39case:

```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location).Path '.dotnet/nuget-packages'
& ./.dotnet/dotnet.exe test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore --disable-build-servers -m:1
& ./.dotnet/dotnet.exe test TaxManager.sln --no-restore --disable-build-servers -m:1
```

SDK10.0.404/cache có sẵn; nếu cần DOTNET_CLI_HOME/APPDATA dùng thư mục tạm được phép, không cài/restore/package/quét toàn.dotnet. verify.command giữ trống. Lượt này chỉ lint tài liệu, không lấy lint/baseline39 làm bằng chứng test S13.

## Rủi ro và bàn giao
Rủi ro: nhầm guard với sàn luật, dùng baseBHXH đã cap cho BHTN, lấy luật mới2027 cho2026, mất provenance tổng hoặc tự round. Các ca boundary/base khácgross/sốlẻ/metadata phát hiện; Unverified và giới hạn rõ được giữ trong docs. Rollback chỉ hoàn tác phần S13 theo diff, giữ đường nhập tay và snapshot S11/S12; không reset diện rộng.

Chỉ ở b-feat tạo docs/decisions/0004-insurance-rounding.md: phạm vi, API/lỗi, bảng nguồn/kỳ/status và guard, quyết định NoRounding của người dùng, raw/applied, ví dụ độc lập, giới hạn rà soát pháp lý và lệnh/kết quả test thật. Tất cả tệp dự kiến trong allow đã có; không sửa PLAN/roadmap/setup/config/model/docs cũ hoặc39case. Reviewer kiểm diff và lỗi nghiêm trọng; root quyết định commit riêng S13 sau review sạch. Không đánh dấu AC/S13 hoàn tất chỉ vì đã có câu trả lời/lint. Bint Core quản lý trạng thái; lượt này dừng sau cập nhật tài liệu và lint, không gọi approve/status/verify hoặc tự chạy b-feat.
