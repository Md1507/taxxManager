# Yêu cầu

## Mục tiêu và phạm vi
Chỉ thực hiện Bước 2.1 (S21/BL-30): thêm Data/Data.Tests net10.0 vào TaxManager.sln, lưu dữ liệu giả bằng EF Core/SQLite và migration thật; bổ sung quy ước tổ chức file ở mục 3 PLAN.md cùng tài liệu phát triển/quyết định schema. Giữ Core độc lập EF và nguyên bộ 197 tests hiện có. Đây là kế hoạch deep của tác vụ 01M81AG6WG0EKADKAV4HSQEV96, không phải bằng chứng đã triển khai.

## Yêu cầu chức năng
1. Hồ sơ dùng Guid opaque do ứng dụng sinh, không Guid.Empty, tên hiển thị nullable; không có CCCD, MST, tài khoản hay định danh đời thực. Tên không làm khóa và không bắt buộc duy nhất.
2. Bốn bảng: UserProfiles; MonthlyIncomes gắn hồ sơ; Dependents có ID cục bộ trong hồ sơ; DependentEligibilityPeriods gắn đúng cặp hồ sơ/người phụ thuộc. Một MonthlyIncome cho mỗi ProfileId/Year/Month, chỉ biểu diễn lương gross thông thường một nguồn, hoàn toàn chịu thuế, VND; GrossIncome, InsuranceAmount nhập tay và ActualWithheldTax đều decimal không âm, bắt buộc cung cấp, chấp nhận 0. Không tự giới hạn withheld hoặc insurance nhỏ hơn gross vì Core không có điều kiện đó.
3. Lịch phụ thuộc theo DateOnly ngày đầu tháng, From/To bắt buộc, bao gồm cả hai đầu; cho phép qua năm và nhiều khoảng rời nhau của cùng người. Cấm đảo khoảng, ngày giữa tháng và chồng khoảng cùng cặp ProfileId/DependentId, kể cả chạm cùng tháng ở hai đầu. Không cấm các người khác nhau hoặc các hồ sơ khác nhau cùng thời gian. FK, khóa duy nhất và CHECK thời gian được thực thi tại SQLite; chống chồng khoảng bằng trigger INSERT/UPDATE trong migration.
4. Mapping tường minh trong Data: MonthlyIncome sang SalaryMonthInput với insuranceAmount, không InsuranceInput, Bonuses/Extras rỗng; period sang DependentPeriod giữ ID và ngày. Nếu cung cấp chiều nhập SalaryMonthInput, phải từ chối InsuranceInput, Bonuses/Extras có dữ liệu trước khi lưu, không âm thầm bỏ trường. Lưu được năm chưa đủ 12 tháng; không tự sinh tháng thiếu hay coi dữ liệu thiếu là 0. Caller chỉ gọi AnnualSalaryCalculator khi có đủ 12 tháng và tự cung cấp TaxRuleProvider; không lưu kết quả/rule snapshot.
5. Giữ tiền decimal ở CLR, mapping cột TEXT với provider SQLite; không dùng REAL/double/float, scaled integer hay làm tròn. Chứng minh qua fresh context với số phân số, 28 chữ số phần lẻ, số lớn sát decimal.MaxValue, 0 và culture khác nhau. Đảm bảo validation số âm trước SaveChanges và SaveChangesAsync; tài liệu phân biệt validation ứng dụng với constraint DB, không dùng CAST sang REAL để kiểm tiền.
6. Integration tests dùng file SQLite mới riêng mỗi test, Database.Migrate/MigrateAsync, đóng writer rồi đọc bằng context/connection mới. Kiểm migration history, schema/trigger, FK bật, CRUD/relations, cascade, owner-scoped queries, unique, thời gian, chống chồng khoảng khi insert/update và decimal chính xác. Không dùng EnsureCreated, EF InMemory hoặc mock DbSet thay các bằng chứng này.
7. Data/Data.Tests tách thư mục theo trách nhiệm/feature, mỗi type chính một file; mapping/configuration, migrations và test fixtures tách rõ. Chỉ áp dụng quy ước cho mã mới; không di chuyển hay refactor Core. Pin EF Sqlite/Design và dotnet-ef cùng 10.0.15, Design PrivateAssets=all, không nâng SDK/net11. Restore/build/tool smoke ở bước thực thi phải chứng minh tương thích thật.
8. Docs giải thích schema, mapping và dữ liệu chưa hỗ trợ, lệnh kích hoạt SDK/restore/test/migrate tái lập, giới hạn SQLite, dữ liệu giả và logical profile isolation không phải phân quyền. Ignore DB local cùng journal/WAL/SHM. Sau triển khai, reviewer độc lập do Bint roles hiện có chạy, đối chiếu cả bốn AC; không đánh dấu review hoàn tất ở bước lập kế hoạch.

## Tiêu chí chấp nhận
- [ ] AC-1: Data/Data.Tests projects organized clearly; PLAN structure conventions documented.
- [ ] AC-2: SQLite migration from empty synthetic file DB and fresh-context save/load of profiles/monthly income/dependent relations pass.
- [ ] AC-3: Exact decimal persistence, relevant validation/constraints/profile isolation verified on real SQLite; existing 197 Core tests remain green.
- [ ] AC-4: Developer docs explain supported persistence scope, limitations and reproducible commands; independent review complete.

## Ngoài phạm vi
Bước 2.2: rule DB, seeder và lưu metadata pháp lý/rule snapshot; Bước 2.3: encryption/audit; Web/Identity, phân quyền, dữ liệu thật, cloud, PostgreSQL và refactor Core. Chưa lưu InsuranceInput tự tính; CashBonus; SalaryExtra (overtime/night/meal); CasualPayment/commitment; MultipleSalaryYearInput, payer/payment; charitable/retirement và chứng từ/xác nhận của chúng. Không gộp những loại này vào gross rồi tuyên bố đã round-trip đầy đủ. Không sửa nghiệp vụ, số luật hoặc legal metadata. Không stage/commit/unstage/reset/stash/push, không đổi Git index/HEAD; không approve, --force, raw-edit state Bint hoặc sửa cấu hình model/workflow.

## Câu hỏi mở
Không có câu hỏi cản trở triển khai. Người dùng đã giao planner chọn schema tối thiểu và giới hạn mapping; các lựa chọn cụ thể được ghi trong plan.
