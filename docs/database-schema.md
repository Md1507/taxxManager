# Schema SQLite bước 2.4

Schema gồm bốn migrations; sáu tệp migration/designer của ba bước cũ giữ nguyên byte. Tên và ba khoản tiền cá nhân
chỉ có cột BLOB mã hóa, không còn cột plaintext legacy. Chỉ dùng dữ liệu giả cho tới
khi review độc lập và người dùng review bảo mật hoàn tất.

```mermaid
erDiagram
    UserProfiles ||--o{ MonthlyIncomes : "ProfileId, cascade"
    UserProfiles ||--o{ Dependents : "ProfileId, cascade"
    Dependents ||--o{ DependentEligibilityPeriods : "ProfileId + DependentId, cascade"
    UserProfiles o|..o{ AuditEvents : "logical ProfileId; NO FK"
    UserProfiles o|..o{ CalculationHistory : "retained logical ProfileId; NO FK"
    TaxRuleSnapshot ||--o{ TaxRuleBracket : "RuleId + Version, cascade"
    TaxRuleSnapshot ||--o{ TaxRulePeriod : "RuleId + Version, cascade"
    TaxRuleLegalBasis ||--o{ TaxRuleSnapshot : "four basis keys, restrict"
    TaxRuleLegalBasis ||--o{ TaxRuleBracket : "BasisKey, restrict"
    UserProfiles {
        TEXT Id PK
        BLOB DisplayNameEncrypted "encrypted JSON null or string"
    }
    MonthlyIncomes {
        TEXT ProfileId PK,FK
        INTEGER Year PK
        INTEGER Month PK
        BLOB GrossIncomeEncrypted "encrypted exact decimal"
        BLOB InsuranceAmountEncrypted "encrypted exact decimal"
        BLOB ActualWithheldTaxEncrypted "encrypted exact decimal"
    }
    Dependents {
        TEXT ProfileId PK,FK
        TEXT Id PK
        BLOB DisplayNameEncrypted "encrypted JSON null or string"
    }
    DependentEligibilityPeriods {
        TEXT ProfileId PK,FK
        TEXT DependentId PK,FK
        TEXT EligibleFromMonth PK
        TEXT EligibleToMonth
    }
    AuditEvents {
        TEXT Id PK
        TEXT OperationId "unique together with Ordinal"
        INTEGER Ordinal
        TEXT ProfileId "logical association; may outlive profile"
        TEXT Entity
        TEXT EntityKey
        TEXT Action
        TEXT TimestampUtc
        BLOB Payload "encrypted actor context correlation before-after"
    }
    DataProtectionMetadata {
        INTEGER Id PK "singleton 1"
        TEXT DatabaseId "AAD instance identifier"
        BLOB KeyCheck "authenticated sentinel; no key bytes"
    }
    CalculationHistory {
        TEXT Id PK
        TEXT ProfileId "logical association; retained after ordinary delete"
        INTEGER Year
        INTEGER Month
        INTEGER SchemaVersion "v1 document, separate from encryption envelope"
        TEXT CreatedAtUtc
        BLOB PayloadEncrypted "exact input result rules provenance engine"
    }
    TaxRuleSnapshot {
        TEXT RuleId PK
        TEXT Version PK
        TEXT Period
        INTEGER FromYear
        TEXT PersonalDeduction "public decimal"
        TEXT DependentDeduction "public decimal"
        TEXT PersonalBasisKey FK
        TEXT DependentBasisKey FK
        TEXT IncomeBasisKey FK
        TEXT TaxBasisKey FK
    }
    TaxRuleBracket {
        TEXT RuleId PK,FK
        TEXT Version PK,FK
        INTEGER Ordinal PK
        TEXT UpperBound "public nullable decimal"
        TEXT Rate "public decimal"
        TEXT BasisKey FK
    }
    TaxRulePeriod {
        TEXT Key PK
        TEXT RuleId FK
        TEXT Version FK
        TEXT PeriodFrom
        TEXT PeriodTo
    }
    TaxRuleLegalBasis {
        TEXT Key PK
        TEXT Id
        TEXT Version
        TEXT SourceUrl
        INTEGER Status
        TEXT VerifiedOn
    }
```

Mọi trường không ghi encrypted là metadata hoặc luật công khai plaintext. BLOB vẫn
lộ version/key ID/kích thước. Không có FK AuditEvents hoặc CalculationHistory → UserProfiles:
đường chấm chỉ liên kết logic, audit/history có thể tồn tại sau khi hồ sơ bị xóa. Metadata không chứa khóa;
khóa durable phải nằm ngoài source/repo và thư mục DB.

Hai trigger Period_NoOverlap_Insert/Update chặn khoảng chồng nhau cho cùng người
phụ thuộc; hai trigger TaxRulePeriod_NoOverlap_Insert/Update chặn kỳ luật chồng nhau
trên timeline chung. CHECK giữ giới hạn năm/tháng và khoảng tháng hợp lệ. Supported
writes/purge kiểm tra foreign keys đang bật, không chỉ dựa vào connection string.

Attach một principal Unchanged để tham chiếu hoặc thêm child không cập nhật parent
hay tạo audit cho parent. Setter sau Attach tạo write intent; chỉ các trường được
sửa lấy giá trị mới, các trường còn lại giữ giá trị DB. Đây là contract ứng dụng,
không thay đổi schema hoặc FK. Xem ADR0015 về detached Update và stale guards.

Xóa thông thường liệt kê cả children chưa load, ghi delete events rồi xóa graph trong
cùng transaction; audit và history mã hóa được giữ. Purge riêng xóa graph và mọi history/audit của target,
không lưu receipt liên kết target. Không cam kết xóa free pages/WAL/backups vật lý.

History có index (ProfileId, Year, Month, CreatedAtUtc), cho phép nhiều lần tính một kỳ;
CHECK year/month/schema. Không có FK tới nguồn income/dependent/rule có thể đổi.
Payload lưu input thực dùng, actual withheld riêng để đối soát, kết quả Core từng bước,
RuleId+Version/registration và căn cứ nguyên trạng, engine/build identity và UTC.
AAD xác thực mọi header; chỉ service tính từ dữ liệu đã lưu được append, supported
SaveChanges từ chối sửa/xóa trực tiếp. Đọc service luôn cần ProfileId+HistoryId;
đây không phải auth. Xem [ADR0016](decisions/0016-calculation-history.md).

Migrate DB legacy populated trực tiếp bị từ chối trước thay đổi schema. Upgrader
synthetic copy-forward tạo DB mới, đối chiếu toàn bộ dữ liệu/khóa/quan hệ/luật trước
publish. Sentinel được khởi tạo trong transaction import kể cả nguồn trống/chỉ có luật;
target công bố luôn kiểm tra khóa được khi reopen, không cần tạo hồ sơ giả.
Down của migrations 2.3 và 2.4 bị từ chối; rollback bằng matching backup/app.
Migration 2.3→2.4 chỉ thêm history/index, không rewrite dữ liệu/audit/luật/keys cũ.
Xem [ADR0015](decisions/0015-data-protection-audit.md) và [hướng dẫn phát triển](development.md).
