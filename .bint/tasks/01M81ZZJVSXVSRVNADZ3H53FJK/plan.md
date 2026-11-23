# Kế hoạch Bước 2.2 — Persist luật lương tháng

## Giả định và mâu thuẫn
Khảo sát HEAD c6418b2b2ddb86cac2e4b1c148b4ac8b2b77aca4, toàn bộ S21 vẫn chưa commit; phụ thuộc 01M81AG6WG0EKADKAV4HSQEV96 đã done, requirement/plan và ADR0013 đã đọc. 197 Core + 15 Data là baseline theo hồ sơ/người dùng, chưa chạy lại ở b-req. MCP context_for_task không có tri thức liên quan nên không có knowledge ID cần cite. Core TaxRuleSet thực tế chứa nhiều optional graph; SalaryInputMapper S21 chỉ manual insurance, không bonuses/extras. Kế hoạch không tuyên bố lưu đầy đủ những graph đó.

PLAN Bước 2.2 hiện gộp seeder 2025/2026 với yêu cầu chỉ nạp nguồn verified, nhưng docs/legal-source-status.md và factories xác nhận không có seed Verified. Giải quyết bằng hai API chính thức/tham khảo tách rõ theo yêu cầu hiện tại; không sửa dữ liệu Core hoặc bắt audit luật trước persistence. PeriodFrom/To khác LegalBasis.EffectiveFrom/To, FromYear không sinh ngày. PLAN mục 0 yêu cầu commit nhưng chỉ dẫn hiện tại để người dùng tự commit có ưu tiên. Không có parent; không lặp lại schema/input S21, không sửa ledger bước đã xong.

## Lựa chọn và lý do
Chọn bốn bảng chuẩn hóa, không JSON graph: constraints/ordered brackets và provenance theo role cần được truy xuất/kiểm riêng. Chọn decimal/TEXT native thay vì REAL hoặc integer áp scale vì phải round-trip decimal không mất chữ số. Chọn trigger SQLite thay precheck trong bộ nhớ: invariant overlap còn đúng qua raw SQL, connection khác và cạnh tranh writer.

Chỉ một timeline monthly/manual toàn DB, không chia theo Id/version/FromYear; rule khác vẫn có thể chồng kỳ nên không dùng các trường đó làm khóa timeline. Hai API khác policy, không tạo hai timeline ẩn để reference đè official. DB đã seed tham khảo sẽ khiến official load thất bại; caller dùng file giả khác hoặc cung cấp bộ dữ liệu chính thức hợp lệ tường minh, không tự chuyển chế độ. Không thêm schema multi-region/multi-graph hoặc workflow duyệt mới; scope này hoàn thành trong một phiếu, chưa cần phiếu con.

## Schema và hướng triển khai
| Bảng dự kiến | Khóa, quan hệ và dữ liệu |
| --- | --- |
| TaxRuleSnapshots | PK (RuleId, Version); Period, FromYear, PersonalDeduction, DependentDeduction; bốn FK bắt buộc đến LegalBases, tương ứng đúng bốn role Core |
| TaxRuleBrackets | PK (RuleId, Version, Ordinal), ordinal từ 0; FK snapshot; UpperBound nullable, Rate decimal/TEXT; BasisKey FK bắt buộc |
| TaxRuleLegalBases | BasisKey surrogate opaque không chứa nghĩa luật; metadata Core Id nullable riêng biệt cùng đủ 12 trường còn lại như requirement; không unique theo legal ID vì có thể thiếu hoặc dùng lại |
| TaxRuleRegistrations | RegistrationKey surrogate opaque; FK (RuleId, Version); PeriodFrom/To bắt buộc; unique cặp kỳ toàn timeline và triggers chống mọi overlap |

Các tên có thể theo convention EF nhưng contract không đổi. Bốn role dùng FK tường minh, không một Basis dùng chung mặc định làm mất khác biệt; có thể cùng trỏ một hàng nếu toàn bộ metadata thực sự giống nhau. Missing là hàng có Status=Missing và metadata đúng input, không phải FK null; thiếu hàng/role/bracket là corruption, không thay bằng LegalBasis.Missing khi load. UpperBound null chỉ biểu diễn bậc cuối vô hạn; các string/date nullable giữ nguyên null. Mapper round-trip so giá trị metadata, không yêu cầu reference equality giữa các basis khác role.

Configuration riêng mỗi entity: NOT NULL/không whitespace cho rule identity/version/Period; FromYear 1–9999; ordinal không âm, unique PK; enum Status hợp lệ và consistency VerifiedOn chỉ có khi Verified; FK bắt buộc và delete behavior rõ. Ngày TEXT canonical yyyy-MM-dd, kiểm đủ calendar Gregorian/leap year 1–9999, NULL không lọt CHECK; optional legal dates kiểm khi có và không đảo, không bắt nằm trong coverage thuế. Registration từ đầu tháng đến cuối tháng, không chỉ kiểm ngày cuối bằng 28–31; tránh date arithmetic tràn năm 9999. Không dùng so sánh lexical cho decimal hay CAST REAL; validation không âm, rate 0–1, bậc hữu hạn tăng nghiêm ngặt/bậc cuối null và ordinal liên tục tại validator/loader. SQL ghi trực tiếp có thể bỏ qua semantic decimal/graph; loader phải phát hiện, docs nói rõ giới hạn.

Trigger BEFORE INSERT/UPDATE của TaxRuleRegistrations dùng NEW.PeriodFrom <= existing.PeriodTo và NEW.PeriodTo >= existing.PeriodFrom, UPDATE loại hàng OLD theo RegistrationKey. Trigger xét mọi rule identity/version, bao gồm đổi FK, khóa hoặc ngày khi UPDATE; self-update không lỗi, ngày sát nhau ở hai tháng khác nhau hợp lệ. Trigger tạo/xóa trong migration, không chỉ SaveChanges. Foreign Keys=True cho mọi fixture/caller theo S21, test PRAGMA thực tế. Thêm validation Data cho cả SaveChanges và SaveChangesAsync, gọi helper TaxRules từ context, giữ validation S21 nguyên vẹn.

## Mapper, import, seeder và loader
TaxRuleMapper nhận snapshot/registration tường minh, reject trước ghi tất cả chín optional properties liệt kê trong requirement, kể cả RoundingPolicy đơn lẻ. Không flatten graph hoặc gộp luật phụ vào monthly. Mapper dùng constructor Core để validate, không thêm EF/network vào Core.

OfficialTaxRuleImporter và TaxRuleProviderLoader.LoadOfficialAsync là đường chính thức, kiểm mọi role/bracket bằng LegalBasisValidator: Verified, ID/version không whitespace, VerifiedOn, absolute HTTPS; host bằng vbpl.vn hoặc đuôi .vbpl.vn trên Uri.Host sau parse, không substring. Chỉ chấp nhận path chi tiết nhận diện rõ như /Pages/vbpq-toanvan.aspx hoặc /Pages/vbpq-chitiet.aspx (có thể có prefix cơ quan), ItemID đúng một giá trị số nguyên dương; reject root, search/list, path chỉ chứa từ khóa, missing/empty/duplicate ItemID, lookalike/userinfo hoặc URL không parse được. Không tự tạo ItemID, thay URL hay network fetch; gate kỹ thuật không chứng minh nguồn đã được thẩm định. Positive fixture Verified chỉ là dữ liệu synthetic để test contract, không một seed luật production mới.

ReferenceTaxRuleSeeder.SeedVietnam2025And2026Async và TaxRuleProviderLoader.LoadReference2025And2026Async là API opt-in riêng, không bool allowUnverified mặc định. Seeder lấy hai static Core snapshots nguyên trạng; registrations [2025-01-01,2025-12-31] và [2026-01-01,2026-12-31] là kỳ tham khảo do caller chọn, không ngày hiệu lực pháp lý. Reference loader đọc DB rồi so đầy đủ với đúng hai snapshots và coverage này; không return lại static snapshots. Reject thêm/thiếu/đổi dữ liệu tham khảo, không tạo cửa load tùy ý Unverified. Không gọi seed trong constructor/OnModelCreating/startup/migration/HasData/UseSeeding.

Seeder/import kiểm input và graph đã lưu trong transaction rồi mới ghi; commit cả batch hoặc rollback tất cả. Matching identity/version phải so đầy đủ amounts, bracket order, mọi metadata/status/null và registrations; giống nhau no-op, khác báo drift. Không catch unique/overlap/corruption rồi coi thành công, không upsert ghi đè hoặc partial seed nếu hàng thứ hai lỗi. Dùng transaction cùng DB constraint để chống race, lỗi lock/constraint được báo rõ, không biến retry thành overwrite. So sánh decimal theo giá trị CLR, không theo text format/culture.

Loader dùng read transaction nhất quán, AsNoTracking đọc đầy đủ snapshots/bases/brackets/registrations; kiểm unknown enums, gaps ordinal, missing rows, malformed decimal/date và overlap trước dựng provider. Empty DB hoặc graph không hoàn chỉnh fail rõ kèm identity/field/kỳ, không fallback. Dựng mỗi snapshot đúng một lần theo (RuleId,Version), tái sử dụng instance cho nhiều registration để giữ semantics GetForYear dùng ReferenceEquals. Provider xử lý thiếu kỳ/split-year bằng lỗi Core hiện có; không tự lấp gap hoặc chọn bằng FromYear/legal dates. Dùng provider đã load với MonthlySalaryCalculator; static Core snapshots chỉ dùng làm input/expected của đường reference, không tham gia đường load official/fallback.

## Tệp/ký hiệu sẽ chạm và vùng ảnh hưởng
- src/TaxManager.Data/TaxRules/: mỗi entity/configuration nêu trên một file; TaxRuleMapper.cs, TaxRuleValidator.cs, LegalBasisValidator.cs, TaxRuleGraphComparer.cs, OfficialTaxRuleImporter.cs, ReferenceTaxRuleSeeder.cs, TaxRuleProviderLoader.cs. Helper thực sự cần mới tách riêng cùng feature; không generic repository hoặc refactor Core.
- src/TaxManager.Data/Infrastructure/TaxManagerDbContext.cs: DbSets và hook validation TaxRules; vẫn ApplyConfigurationsFromAssembly, không tự migrate/seed.
- src/TaxManager.Data/Migrations/: migration kế tiếp AddMonthlyTaxRules, designer và snapshot cập nhật; giữ byte nguyên hai file 20261122100144_InitialPersistence. Không rebuild/drop/alter bốn bảng S21.
- tests/TaxManager.Data.Tests/TaxRules/: test classes theo round-trip/provider, official policy, reference seed/load, decimal, validation/corruption, relational constraints; Fixtures dùng lại/mở rộng SqliteFileDatabase và fixture graph synthetic. Migrations/MigrationTests.cs cập nhật kỳ vọng đủ initial+kế tiếp thay Assert.Single; thêm MigrationUpgradeTests.cs và MigrationRollbackTests.cs trong cùng thư mục, không đặt migration tests lẫn feature.
- docs/decisions/0014-tax-rule-persistence.md, docs/development.md, PLAN.md chỉ mục 3 và Bước 2.2; không sửa mục 5/ledger S21/roadmap.

Runtime hiện chỉ Data tests/design-time factory gọi DB, chưa có Web/startup tích hợp; caller mới chọn explicit loader và truyền Core provider. Dữ liệu mới chỉ bảng luật, không thay dữ liệu profile/income/dependent, dependencies, solution/tool config hay Core model. Đã bổ sung allow tests/TaxManager.Data.Tests/Migrations/** vì test hiện có cố định một migration; các đường còn lại nằm trong allow có sẵn. Protected root migrations/** và .github/workflows/** không thuộc kế hoạch; migration được giao nằm src/TaxManager.Data/Migrations/**. Không xin duyệt lại công việc đã được người dùng giao.

## Chiến lược kiểm thử
1. SQLite file riêng mỗi test, Migrate thật, writer dispose rồi fresh context/connection. Không EnsureCreated/EF InMemory. Round-trip mọi field/role/bracket, null/Missing, status/date, identity/version, coverage khác legal dates/FromYear; cùng snapshot nhiều registrations vẫn giữ GetForYear đúng. Provider chọn hai phía boundary, gap và split-year đúng lỗi Core; calculator giữ rule/provenance từ DB.
2. Synthetic official snapshot có giá trị khác seed để chứng minh kết quả tính phụ thuộc DB, không static fallback. Test positive verified host/subdomain + trang chi tiết; negative Unverified/Missing từng role và bracket, thiếu ID/version/VerifiedOn, HTTP, root/search/list, ItemID sai/thiếu, host lookalike. Core không đổi để làm yếu test. Reference seed cả hai năm, gọi lại không đổi counts/content, official loader từ chối reference DB, reference loader không được tự seed hoặc trả static khi DB trống/corrupt.
3. Drift gồm amount, bracket order/rate, metadata/null/status và coverage: cùng version phải reject, không thay dữ liệu cũ. Batch có phần hợp lệ và phần overlap/drift/corrupt phải rollback cả phần hợp lệ; thử hai connection cùng identity/kỳ để chứng minh không để duplicate/overlap sau cạnh tranh. Reject từng optional graph riêng, không chỉ snapshot chứa tất cả.
4. Raw SQL/FK thật: duplicate identity/version/ordinal/registration; orphan FK; year 0/10000, ngày sai format/không tồn tại/leap year, partial month/đảo khoảng; full-month qua năm và min/max year hợp lệ. INSERT và UPDATE overlap dạng trùng/lồng/chéo/chạm biên, khác identity/version vẫn bị chặn; UPDATE chính hàng hợp lệ, tháng liền kề được nhận. Corruption fixture cô lập kiểm missing basis/bracket, ordinal gap, bad enum/decimal và metadata/graph sai; loader lỗi rõ, không loại hàng hỏng.
5. Decimal cả hai deductions, finite bounds và rate: 0, 0.1, phần lẻ 28 chữ số, số lớn hợp lệ sát MaxValue cho field phù hợp, null cuối bậc; update/read bằng fresh context, typeof TEXT, vi-VN/en-US. Rate nằm 0–1 và bounds vẫn increasing; không đưa MaxValue vào phép tính gây overflow ngoài mục tiêu. Validation sync/async reject semantic invalid; không hạ precision để test qua.
6. Migration fresh: hai history entries đúng tên/order, no pending migrations/model changes, gọi Migrate hai lần không mất dữ liệu và không có seed tự sinh. Upgrade: IMigrator tới initial, tạo profile/monthly income/dependent/multiple periods giả, đóng context, migrate latest, so mọi dữ liệu và SQL/hoạt động hai trigger Period_NoOverlap_Insert/Update cũ. Down về initial trên file dùng riêng, chỉ bảng/trigger luật mất; dữ liệu/trigger S21 còn, up lại/model consistency đúng, không auto-seed.
7. Chạy development build, TaxRules tests, Migrations tests rồi full solution; giữ 197 Core và 15 test hành vi Data cũ đều xanh ngoài tests mới, 0 skipped/failed; migration count test cập nhật đúng assembly, không bỏ/disable test. Kiểm manual formatting và diff whitespace. Chưa kiểm luật đúng nội dung, graph ngoài scope, PostgreSQL, auth/encryption hoặc DB người dùng.

## Lệnh development cho b-feat
Các lệnh này chưa chạy ở b-req; không đặt vào verify.command, không cần thêm approval do người dùng đã giao chạy trực tiếp trong development. SDK local 10.0.404, EF/tool 10.0.15 giữ nguyên; chỉ restore cached nếu cần, lỗi mạng phải ghi cụ thể và tiếp tục phần việc độc lập. Mọi database update đều truyền file giả mới thuộc TEMP.

```powershell
$env:DOTNET_ROOT = Join-Path $PWD '.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:NUGET_PACKAGES = Join-Path $PWD '.dotnet/nuget-packages'
$env:DOTNET_CLI_HOME = Join-Path $env:TEMP 'taxmanager-cli'
& ./.dotnet/dotnet.exe --info
& ./.dotnet/dotnet.exe ef --version
& ./.dotnet/dotnet.exe build TaxManager.sln --no-restore --disable-build-servers -m:1
& ./.dotnet/dotnet.exe ef migrations add AddMonthlyTaxRules --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext --output-dir Migrations
& ./.dotnet/dotnet.exe test tests/TaxManager.Data.Tests/TaxManager.Data.Tests.csproj --no-restore --disable-build-servers -m:1 --filter FullyQualifiedName~TaxRules
& ./.dotnet/dotnet.exe test tests/TaxManager.Data.Tests/TaxManager.Data.Tests.csproj --no-restore --disable-build-servers -m:1 --filter FullyQualifiedName~Migrations
& ./.dotnet/dotnet.exe test TaxManager.sln --no-restore --disable-build-servers -m:1
& ./.dotnet/dotnet.exe ef migrations has-pending-model-changes --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext
$migrationTestDir = Join-Path $env:TEMP ("taxmanager-s22-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $migrationTestDir
$migrationTestDb = Join-Path $migrationTestDir 'migration-smoke.db'
& ./.dotnet/dotnet.exe ef database update --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext --connection "Data Source=$migrationTestDb;Foreign Keys=True"
& ./.dotnet/dotnet.exe ef database update 20261122100144_InitialPersistence --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext --connection "Data Source=$migrationTestDb;Foreign Keys=True"
& ./.dotnet/dotnet.exe ef database update --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext --connection "Data Source=$migrationTestDb;Foreign Keys=True"
```

Ghi exit code/counts/log bằng chứng sau từng nhóm; không coi command được liệt kê là đã chạy. Nếu assets cache thiếu thì restore solution/tool bằng tool hiện có, không đổi packages/config. has-pending-model-changes và integration test kiểm model, không tắt warning model mismatch. dotnet format named pipe lỗi thì format tay, không bỏ dở readability.

## Rủi ro và rollback
Rủi ro chính là làm nguồn chưa thẩm định thành official, mất metadata/decimal, bypass overlap và migration hỏng dữ liệu S21; các nhóm policy/round-trip/raw SQL/upgrade tests tương ứng phát hiện. Down xóa trigger mới trước bảng mới, chỉ dùng DB giả; dữ liệu luật sẽ mất khi down, dữ liệu S21 phải còn. Không chạy migration/rollback trên DB người dùng; không sửa lịch sử initial, không rollback Git hoặc fake commit. SQLite custom trigger cần được giữ khi migration tương lai rebuild bảng, kế hoạch này tránh rebuild S21.

Bint phải chụp baseline working tree trước b-feat, gồm thay đổi S21 chưa commit. Reviewer dùng bint task diff 01M81ZZJVSXVSRVNADZ3H53FJK --all để xem delta so snapshot và đối chiếu diff lọc theo task, không lấy git diff HEAD làm toàn bộ thay đổi của S22. Nếu snapshot chưa có thì ghi hạn chế, không nhập S21 vào finding S22. Trường hợp sandbox chặn object snapshot, có thể đặt GIT_OBJECT_DIRECTORY tại TEMP với GIT_ALTERNATE_OBJECT_DIRECTORIES trỏ repository objects chỉ đọc cho tiến trình Bint; không thay actual HEAD/index hoặc commit giả.

Giữ checksum initial để đối chiếu: main SHA256 5E5A3D4A0E2879787CEDDEAF714873B0BC1613B43AA3EEB93A4506B4984B6695; designer B984B241E0EEC2178265F7E8FF8543277D4D1AD364B21C0A395DA5911DF40B06. Reviewer độc lập do Bint roles hiện có điều phối sau triển khai; sửa actionable findings và cung cấp test/diff evidence để reviewer xác nhận vòng sau, không tự đánh dấu review hoàn tất hoặc đổi trạng thái/approve.

## Checklist nhất quán
Liên kết chính trong tasks.md: AC-1 được TSK-2/6/7 bao phủ; AC-2 bởi TSK-3/4/5; AC-3 bởi TSK-8/9/10; AC-4 bởi TSK-1/11/12. Nội dung TSK còn kiểm chéo policy/decimal/constraints theo plan. Mỗi TSK có điều kiện xong đo được; mọi file dự kiến thuộc allow đã có hoặc thư mục test Migrations vừa bổ sung. Requirement giữ nguyên bốn criteria theo thứ tự; verify.command trống. Chỉ ba tài liệu Bint ở bước này, không thêm trạng thái/file workflow. Chưa chạy tests/migration, chưa implement/review; Bint Core tiếp tục b-feat rồi b-review, không planner tự chuyển trạng thái.

## Nguồn kỹ thuật đã đối chiếu
- [Microsoft.Data.Sqlite types](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types): decimal lưu TEXT tránh mất chính xác, DateOnly có định dạng yyyy-MM-dd; precision/scale không do SQLite tự enforce.
- [EF SQLite limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations): migration rebuild có giới hạn với artifact ngoài model, SQLite không có idempotent migration script; trigger cần quản lý rõ trong Up/Down.
- [Managing migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing): sinh migration/designer/snapshot và has-pending-model-changes; giữ nguyên migration đã có. Không dùng tính năng --add của EF11 trong EF10.
Chỉ tra tài liệu kỹ thuật Microsoft; không audit hoặc thay nguồn luật. Các contract nghiệp vụ dựa trên mã Core/TaxRuleProvider/Calculators, Data S21, docs/legal-source-status.md và test migration đã đọc.

## Sửa finding .10 sau review4 (23/11/2026)
Review4 đã hoàn tất theo đúng một vòng bổ sung người dùng cho phép; không được chạy review5 hoặc sửa cấu hình. Chưa chốt AC-4/TSK-12/done. Tiếp tục b-feat để sửa cụ thể finding 01M81ZZJVSXVSRVNADZ3H53FJK.10 rồi chạy kiểm chứng. Root sẽ xin phép riêng nếu cần review tiếp.
Đọc nguyên finding .10 trong review.json và probe TEMP/taxmanager-review4-probe/Program.cs, evidence.txt. Effective graph phải bắt đầu bằng dữ liệu DB đọc trong transaction và áp chính xác các thuộc tính PropertyEntry.IsModified mà EF sẽ ghi cho entity Modified; không thay toàn bộ DB entity bằng tracked entity cũ. Rà đồng nhất snapshots, brackets, legal bases, periods và graph validation/lifecycle. Giữ write transaction và ownership/cleanup hiện tại, không làm yếu loader, không tự cleanup dữ liệu để che lỗi, không đổi schema hay luật.
Thêm regression hai context cùng load trước, save lần lượt và retry nếu liên quan; sync/async cho shared legal basis và bracket bounds như reviewer. Writer thứ hai phải bị từ chối trước ghi hoặc fresh official loader phải đọc graph hợp lệ. Chạy targeted + full solution và EF model consistency; kiểm HEAD/index/initial hashes; giữ S21 chưa commit. Cập nhật docs và dòng PLAN 2.2 đúng trạng thái sửa xong/chờ review cùng số test thật. Ghi rõ giới hạn review, không tự chạy reviewer hoặc chuyển done, không commit/stage/approve. Giữ code dễ đọc và file có trách nhiệm rõ.
