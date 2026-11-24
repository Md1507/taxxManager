# Phát triển persistence

Chỉ dùng dữ liệu giả. SDK local net10, EF packages và dotnet-ef giữ versions hiện có.
Từ repository root:

```powershell
. ./scripts/dev/activate-dotnet.ps1
$env:NUGET_PACKAGES = Join-Path $PWD '.dotnet/nuget-packages'
$env:DOTNET_CLI_HOME = Join-Path $env:TEMP 'taxmanager-cli'
dotnet build TaxManager.sln --no-restore --disable-build-servers -m:1
dotnet test tests/TaxManager.Data.Tests --no-restore --disable-build-servers -m:1
dotnet test tests/TaxManager.Core.Tests --no-restore --disable-build-servers -m:1
dotnet ef migrations has-pending-model-changes --no-build --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext
```

Nếu thiếu cached assets thì restore với versions hiện tại; không nâng package để vượt
lỗi. Cảnh báo NU1900 do endpoint vulnerability NuGet không truy cập được không phải
kết quả kiểm tra bảo mật packages. Tests không bị skip vì cảnh báo đó.

## Database và khóa synthetic

Constructor không tự migrate/seed. Design-time factory chỉ dùng :memory: để tạo model,
không trỏ mặc định vào taxmanager.local.db và không cần khóa vận hành. Chỉ chạy lệnh
database với kết nối synthetic tường minh:

```powershell
$testDir = Join-Path $env:TEMP ("taxmanager-dev-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $testDir | Out-Null
$testDb = Join-Path $testDir 'synthetic.db'
dotnet ef database update --project src/TaxManager.Data --startup-project src/TaxManager.Data --context TaxManagerDbContext --connection "Data Source=$testDb;Foreign Keys=True;Pooling=False"
```

Lệnh trên chỉ tạo schema trống; không tạo key/sentinel/actor. Tests dùng
SqliteFileDatabase để tạo khóa random **synthetic** ở TEMP, ngoài thư mục DB và repo;
các file đó được fixture dọn sau test. Không sao chép key fixture sang dữ liệu thật.

Runtime cần FileKeyRing.Load với đường dẫn absolute tới key ring đã được operator cấp,
đường dẫn repository thực và thư mục DB thực. Key ring JSON và các ranh giới được ghi ở
[ADR0015](decisions/0015-data-protection-audit.md). Không tự provision khóa vận hành, không
đặt keys trong DB/repo, không bật EnableSensitiveDataLogging hoặc log entities/payload.
Khóa ở ngoài DB vẫn cần ACL và backup riêng; đây không phải auth hay chống SQLite admin.

Ví dụ tích hợp C# (biến đường dẫn do caller cung cấp; không có secret trong source):

```csharp
using var keys = FileKeyRing.Load(keyPath, repositoryDirectory, databaseDirectory);
var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
    .UseSqlite($"Data Source={syntheticDbPath};Foreign Keys=True;Pooling=False").Options;
await using var context = new TaxManagerDbContext(options, keys)
{
    AuditContext = new AuditWriteContext("synthetic-operator", "synthetic edit",
        Correlation: "synthetic-correlation", OperationId: Guid.NewGuid())
};
// On an initialized DB, call even if only reading metadata / an empty result:
context.ValidateProtection();
// Then use tracked entities and SaveChanges/SaveChangesAsync.
```

Một DB fresh chưa ghi protected data chưa có sentinel: lần protected Save đầu tiên
khởi tạo sentinel trong cùng transaction. Không gọi ValidateProtection trước lần đó.
Target do SyntheticDatabaseUpgrader công bố luôn có sentinel, kể cả nguồn trống hoặc
chỉ có luật công khai: mở lại target phải gọi ValidateProtection ngay.
Mở lại DB đã initialized thì gọi ValidateProtection, kể cả khi hồ sơ đã ordinary-delete.
Materialization của names/amounts và protected Save cũng xác thực key-check. Count/ID
projections là truy cập metadata và không tự xác thực khóa. Thiếu key/ciphertext lỗi
không được thay bằng 0, empty string hoặc plaintext. Luật công khai không cần khóa.

AuditWriteContext.Actor chỉ là metadata caller chưa xác thực. Cần actor/context trước
mọi thay đổi thật của profile/income/dependent/period; no-op và TaxRules-only không cần.
Giữ OperationId ổn định cho một logical operation nếu cần đối soát. Không dùng cùng ID
cho thay đổi mới: operation đã ghi bị reject khi batch còn thay đổi. Không có retry tự
động hoặc cam kết exactly-once. Với commit outcome unknown, mở context mới, đọc các
Set<AuditEvent>().AsNoTracking() theo OperationId, gọi ReadAudit và đối chiếu business
state trước khi quyết định retry. Không lặp mù Add/Delete.

Caller-owned transactions giữ quyền commit/rollback. Save dùng preparation savepoint,
nên lỗi không xóa công việc trước savepoint. Sau outer rollback phải clear/reopen/reload.
Sau SaveChanges(false), AcceptAllChanges hoặc clear/reopen trước lần save tiếp theo.
Có thể edit rồi Save ngay sau AcceptAllChanges, hoặc Add/Update rồi Save ngay sau
Clear; không cần chèn một lần Save no-op. Nếu entry của lần Save(false) chưa accept
hoặc detach/clear thì Save tiếp theo vẫn bị từ chối, không ghi audit trùng.
Period đã load có stale guard giống income; detached Update/Remove dùng before-state
thực tế trong DB, kể cả delete chỉ có khóa. Snapshot EF tự tạo lúc attach không phải
concurrency token: detached writes không phát hiện được stale-client intent.

Attach chỉ thiết lập baseline Unchanged; tên/số tiền mặc định hoặc được gán trước
Attach không phải lệnh ghi. Có thể Attach principal bằng khóa rồi Add child mà không
ghi đè parent hoặc tạo audit parent. Entity chỉ đọc không bị stale-check khi Save,
kể cả khi context khác vừa sửa nó hoặc Save đang thêm entity khác.
Setter public gọi sau Attach mới đánh dấu trường nhạy cảm cần ghi (kể cả tên null).
Ví dụ Attach income bằng ProfileId/Year/Month, rồi gán GrossIncome sẽ giữ nguyên hai
khoản còn lại từ DB trong transaction; nhiều setter sau Attach đều được ghi nhận.
Detached Update là thay thế toàn bộ: phải cung cấp đủ ba khoản tiền. Entry thực sự
đã load và được chỉnh sửa vẫn có stale guard. Giá trị không đổi không sinh audit.

Không dùng raw SQL/bulk writes, chỉnh OriginalValues hay interceptor mutate/suppress
writes để bỏ qua pipeline nghiệp vụ. SQL sort/filter/sum trên tên/tiền unmapped không
được hỗ trợ; lọc bằng metadata rồi đưa giá trị đã hydrate sang Core.

## Xóa, nâng cấp và restore

ProfileDeletionService hoặc Remove thông thường xóa graph, giữ audit mã hóa, kể cả
delete events của children chưa load. Đây không phải erasure. ProfilePurgeService chỉ
chạy với context fresh/no tracked entities và explicit target/actor/purpose; xóa graph
cùng toàn bộ audit target atomically, không giữ receipt liên kết target. Không có
retention mặc định, phân quyền hay quyết định pháp lý. Cần policy được người dùng duyệt.
Purge không xóa chắc bytes trong WAL/free pages/SSD snapshots/backups.

Migrations 2.1/2.2 không đổi. Migration 2.3 chỉ cho direct upgrade khi bốn bảng nhạy cảm
legacy trống, giữ constraints và cả bốn overlap triggers. DB populated sẽ báo
RequireCopyForwardForPopulatedDatabase và giữ nguyên schema/history/data. Down 2.3
bị từ chối; không downgrade tự giải mã ra plaintext.

SyntheticDatabaseUpgrader chỉ là API development cho file absolute, không links,
**trong TEMP**, syntheticDataOnly=true, đúng hai migrations cũ. Không chạy với
src/TaxManager.Data/taxmanager.local.db hoặc key thật. Không có auto-upgrade startup.

```csharp
var result = await new SyntheticDatabaseUpgrader().UpgradeAsync(
    syntheticLegacyPath, newSyntheticTargetPath, keys, syntheticDataOnly: true, cancellationToken);
```

Dừng writers trước khi gọi. Source được mở read-only với một snapshot; target phải
chưa tồn tại, kể cả WAL/SHM/journal cũ. Upgrader tạo pending file mới, mã hóa từng hàng,
khởi tạo sentinel trong cùng transaction với copy toàn bộ public rules/provenance/keys,
kiểm tra bằng context mới rồi mới rename
không overwrite. Audit import có actor synthetic-upgrader và thời điểm import, không
bịa lịch sử cũ. Nguồn trống/rules-only vẫn tạo target xác thực khóa được, giữ mọi luật
công khai và không tạo business audit giả. Lỗi SQL/crypto/readback/key/cancel/publish giữ nguyên source; chỉ pending
files của attempt được dọn. Không chuyển bytes plaintext của source sang file target.

Backup DB nhất quán bằng SQLite BackupDatabase hoặc đóng toàn bộ connections/writers
trước khi copy; không copy riêng live main file khi còn WAL. Backup key ring ở nơi khác,
giữ đúng DB instance metadata/key pair và các old keys. Restore thử sang TEMP, load key
file ở lifetime mới, ValidateProtection và đối chiếu sensitive fields/audit/rules. Sai/mất
key phải thất bại, không reset metadata hoặc tạo khóa thay thế để che lỗi. Tests có ca
BackupDatabase + reopen key và wrong-key restore.

Sau cutover đã review, source plaintext vẫn là rollback material nhạy cảm. Inventory
source/WAL/journal/temp files/backup/snapshot và xử lý theo policy; secure_delete/VACUUM
không chứng minh forensic erasure trên SSD hoặc media backup. Tác vụ này không thực hiện
cutover hay cleanup dữ liệu thật.

## Quy tắc thuế và trạng thái bàn giao

TaxRuleStore.ImportOfficialAsync/LoadOfficialAsync vẫn yêu cầu căn cứ Verified, ID,
version, VerifiedOn và HTTPS trang chi tiết vbpl.vn có ItemID. Không nâng trạng thái
Core để vượt source gate. SeedReferencesAsync/LoadReferencesAsync là lựa chọn tường
minh cho 2025/2026, không tự seed/fallback và không chứng minh hiệu lực pháp lý.

Phạm vi luật vẫn chỉ lương tháng/bảo hiểm nhập tay; optional graph bị từ chối. Kỳ
registration khác ngày hiệu lực văn bản. Decimal luật TEXT được validate exact và
graph/lifecycle/stale guards của 2.2 vẫn chạy trong transaction, kể cả mixed batch.

Xem [schema](database-schema.md), [ADR0014](decisions/0014-tax-rule-persistence.md),
[ADR0015](decisions/0015-data-protection-audit.md) và PLAN cho test/review status.
Code/tests phải qua review độc lập Bint, rồi người dùng review bảo mật trước dữ liệu
thật. Bước 2.4 đã có implementation và synthetic tests. Web MVP 3.1 chỉ chạy demo
synthetic local, chưa có Identity; xem [hướng dẫn Web](web-mvp.md). Contract và giới
hạn tại [ADR0016](decisions/0016-calculation-history.md).

## Lịch sử tính lương tháng 2.4

Sau khi DB synthetic đã migrate và có input/rules persisted tường minh:

```csharp
context.AuditContext = new AuditWriteContext("synthetic-caller", "monthly calculation",
    OperationId: Guid.NewGuid()); // metadata caller, chưa phải authenticated identity
var history = new CalculationHistoryService(context);
var saved = await history.CreateMonthlyAsync(profileId, 2026, 1,
    CalculationSourceMode.References); // Official dùng strict verified source gate
var read = await history.ReadAsync(profileId, saved.HistoryId);
```

Import namespace TaxManager.Data.CalculationHistory. Context không được có pending
writes. Service đọc gross/manual insurance/actual withheld và khoảng phụ thuộc đúng
hồ sơ trong cùng transaction với catalog; gọi Core, không nhận result caller. Không
seed/fallback, không tạo tháng thiếu. Actual withheld chỉ đối soát, exemptIncome=0;
optional graphs không hỗ trợ bị reject. References giữ nguyên trạng thái pháp lý,
Official không tự xác minh nội dung luật. Registration khác legal effective dates.

Snapshot bất biến tự chứa mọi input/result/portion/basis/rule/engine binary identity và
UTC. Đọc không join nguồn hiện tại hay chạy lại Core. Decimal exact; unknown schema,
malformed/tampered payload hoặc sai khóa báo lỗi, không default. DTO read/saved có thể
dùng sau khi đóng context; accessor entity CalculationRecord.Snapshot cần context sống.
Không log snapshot, amount, provenance hoặc actor/purpose; SQL lưu BLOB đã mã hóa.

Supported SaveChanges sync/async từ chối caller Add/Update/Delete history. Chỉ service
được append; chỉ profile purge được erase. Raw/bulk SQL, reflection và SQLite admin
ở ngoài contract này. Ordinary-delete giữ history/audit bằng logical ProfileId, không
FK cascade; service Read cần đúng cặp ProfileId+HistoryId kể cả sau delete, chưa phải
authorization. Purge xóa cả target history/audit dù profile đã xóa, không receipt;
WAL/free pages/backups vẫn có giới hạn như 2.3, không có retention pháp lý tự đặt.

Create giữ writer lock trước lần đọc đầu, rồi history/audit cùng atomic coordinator.
Caller transaction giữ quyền commit/rollback; savepoint lỗi không mất earlier SQL.
Validation/overflow/size/crypto/SQL/cancel lỗi không để partial history/audit. Không retry
tự động; commit unknown cần fresh context tra OperationId, decrypt audit và đối chiếu
history trước quyết định retry. Không reuse ID đã ghi. Sau outer rollback reload/reopen.

Migration thứ tư chỉ thêm CalculationHistory; không chạy migration trên user DB.
Downgrade 2.4 từ chối, dùng backup2.3 cùng app/key ring/metadata phù hợp để rollback.
Giữ external keys và backup theo ADR0015; không tạo hay reset keys để chữa lỗi đọc.
Chạy nhóm mới trước full Data/Core/model checks:

```powershell
dotnet test tests/TaxManager.Data.Tests --no-restore --disable-build-servers -m:1 --filter FullyQualifiedName~CalculationHistory
```

Review task này dùng `bint task diff 01M836AAX9R6EPSHC16WVB85R1`, sau khi Bint ghi
checkpoint b-feat; trong lượt b-feat đang mở, `--all` cho delta từ snapshot trước tác vụ
vì file ownership chỉ được ghi khi bước kết thúc. Không dùng diff HEAD để lẫn baseline2.3.
Không tự commit hoặc tự ghi đã review độc lập; Bint Core điều phối b-review tiếp theo.
