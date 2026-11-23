# Schema SQLite bước 2.3

Schema gồm ba migrations; hai migration cũ giữ nguyên. Tên và ba khoản tiền cá nhân
chỉ có cột BLOB mã hóa, không còn cột plaintext legacy. Chỉ dùng dữ liệu giả cho tới
khi review độc lập và người dùng review bảo mật hoàn tất.

```mermaid
erDiagram
    UserProfiles ||--o{ MonthlyIncomes : "ProfileId, cascade"
    UserProfiles ||--o{ Dependents : "ProfileId, cascade"
    Dependents ||--o{ DependentEligibilityPeriods : "ProfileId + DependentId, cascade"
    UserProfiles o|..o{ AuditEvents : "logical ProfileId; NO FK"
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
lộ version/key ID/kích thước. Không có FK AuditEvents → UserProfiles: đường chấm
chỉ liên kết logic, audit có thể tồn tại sau khi hồ sơ bị xóa. Metadata không chứa khóa;
khóa durable phải nằm ngoài source/repo và thư mục DB.

Hai trigger Period_NoOverlap_Insert/Update chặn khoảng chồng nhau cho cùng người
phụ thuộc; hai trigger TaxRulePeriod_NoOverlap_Insert/Update chặn kỳ luật chồng nhau
trên timeline chung. CHECK giữ giới hạn năm/tháng và khoảng tháng hợp lệ. Supported
writes/purge kiểm tra foreign keys đang bật, không chỉ dựa vào connection string.

Xóa thông thường liệt kê cả children chưa load, ghi delete events rồi xóa graph trong
cùng transaction; audit mã hóa được giữ. Purge riêng xóa graph và mọi audit của target,
không lưu receipt liên kết target. Không cam kết xóa free pages/WAL/backups vật lý.

Migrate DB legacy populated trực tiếp bị từ chối trước thay đổi schema. Upgrader
synthetic copy-forward tạo DB mới, đối chiếu toàn bộ dữ liệu/khóa/quan hệ/luật trước
publish. Down của migration 2.3 bị từ chối. Xem [ADR0015](decisions/0015-data-protection-audit.md)
và [hướng dẫn phát triển](development.md). Chưa có bảng lịch sử tính toán 2.4.
