# PLAN.md – Web quản lý thuế thu nhập cá nhân (.NET)

> Dự án cá nhân, dùng toàn công cụ miễn phí, phát triển theo kiểu vibecode.
> Trọng tâm: **nghiệp vụ và logic tính thuế** trước, giao diện sau.
> Phát triển hoàn toàn bằng Codex; người dùng cung cấp và đối chiếu dữ liệu pháp lý, review các phần cần thiết theo quy tắc bên dưới.

---

## 0. Quy tắc làm việc (dành cho cả người và công cụ AI)

1. Mỗi lần chỉ triển khai **MỘT bước** theo phụ thuộc trong plan này. Có thể chuẩn bị yêu cầu, thiết kế, ví dụ và rà phụ thuộc cho bước sau song song; chưa triển khai trước khi điều kiện bắt đầu đạt.
2. Không tự thêm tính năng ngoài phạm vi của bước hiện tại.
3. Mọi bước kết thúc bằng: `dotnet test` xanh toàn bộ, rồi `git commit`.
4. **Mọi con số luật** (biểu thuế, mức giảm trừ, tỷ lệ/mức trần bảo hiểm, ngưỡng miễn thuế...) chỉ nằm trong bộ quy tắc `TaxRuleSet`. Tuyệt đối không hardcode trong logic.
5. AI **không được tự điền số liệu luật từ trí nhớ**. Số liệu nào chưa có trong file này thì phải được người dùng tra từ văn bản chính thức rồi cung cấp.
6. Mọi khoản tiền dùng `decimal`, không dùng `double`/`float`.
7. Code ngắn gọn, tên rõ ràng, comment tiếng Việt ở chỗ nghiệp vụ.
8. Phần bảo mật (mã hóa, Identity, phân quyền) phải được người dùng đọc lại hoặc nhờ công cụ khác review. AI hay viết code "chạy được nhưng chưa an toàn".
9. Ứng dụng chỉ mang tính **tham khảo**, không thay thế tư vấn thuế chính thức. Phải có dòng disclaimer trên giao diện.

---

## 1. Kiến thức nền (đã tra cứu, cần xác minh lại với văn bản gốc trước khi dùng)

### 1.1 Văn bản pháp lý
| Nội dung | Văn bản | Ghi chú |
|---|---|---|
| Luật Thuế TNCN mới | Luật 109/2025/QH15, thông qua 10/12/2025 | Hiệu lực 01/07/2026; thay Luật 04/2007/QH12 |
| Mức giảm trừ gia cảnh | Nghị quyết 110/2025/UBTVQH15 | Áp dụng từ 01/01/2026 |
| Quản lý thuế | Luật Quản lý thuế 108/2025/QH15 | Hiệu lực 01/07/2026 (riêng hóa đơn điện tử của hộ/cá nhân kinh doanh từ 01/01/2026) |
| Bảo vệ dữ liệu cá nhân | Luật 91/2025/QH15, Nghị định 356/2025/NĐ-CP | Hiệu lực 01/01/2026 |

Các mốc áp dụng khác nhau (01/01/2026 và 01/07/2026), nên hệ thống phải lưu **bộ quy tắc theo kỳ tính thuế và ngày hiệu lực**.

### 1.2 Giảm trừ gia cảnh (kỳ tính thuế 2026)
- Bản thân: **15.500.000 đồng/tháng**
- Mỗi người phụ thuộc: **6.200.000 đồng/tháng**
- Mức này có thể được điều chỉnh theo CPI trong tương lai, vì vậy phải để trong cấu hình.

### 1.3 Biểu thuế lũy tiến từng phần (lương, tiền công, kinh doanh của cá nhân cư trú, kỳ 2026)
| Bậc | TNTT/tháng | TNTT/năm | Thuế suất |
|---|---|---|---|
| 1 | đến 10 triệu | đến 120 triệu | 5% |
| 2 | trên 10 đến 30 triệu | trên 120 đến 360 triệu | 10% |
| 3 | trên 30 đến 60 triệu | trên 360 đến 720 triệu | 20% |
| 4 | trên 60 đến 100 triệu | trên 720 triệu đến 1,2 tỷ | 30% |
| 5 | trên 100 triệu | trên 1,2 tỷ | 35% |

Tính **từng phần**: chỉ phần vượt ngưỡng mới chịu thuế suất cao hơn.

Biểu cũ (đến hết kỳ 2025) có 7 bậc: 5% – 10% – 15% – 20% – 25% – 30% – 35%. **Ngưỡng từng bậc của biểu cũ do người dùng tra từ nguồn chính thức** rồi mới đưa vào bước 1.2.

### 1.4 Công thức thu nhập tính thuế (lương, tiền công)
```
Thu nhập tính thuế = Tổng thu nhập chịu thuế
                     − thu nhập miễn thuế
                     − bảo hiểm bắt buộc
                     − giảm trừ gia cảnh (bản thân + người phụ thuộc)
                     − các khoản giảm trừ khác (từ thiện, hưu trí tự nguyện... nếu có)
Nếu kết quả < 0 thì bằng 0.
```

### 1.5 Bảo hiểm bắt buộc (phần người lao động đóng)
Tỷ lệ tham khảo từ tài liệu đã tra: BHXH 8%, BHYT 1,5%, BHTN 1% (tổng 10,5%).
**Mức lương đóng tối đa/tối thiểu (mức trần) phải do người dùng tra và cung cấp**, không để AI tự điền.

### 1.6 Các khái niệm nghiệp vụ
Cá nhân cư trú/không cư trú; mã số thuế; người phụ thuộc (đăng ký theo tháng); khấu trừ tại nguồn; tạm nộp; quyết toán năm; thu nhập vãng lai (khấu trừ 10% với khoản chi trả từ ngưỡng nhất định, cần xác minh lại ngưỡng hiện hành).

### 1.7 Các điểm cần xác minh thêm trước khi code
- Quy tắc làm tròn của cơ quan thuế (làm tròn ở bước nào, lên hay xuống).
- Mẫu và cấu trúc tờ khai quyết toán hiện hành (02/QTT-TNCN hoặc mẫu thay thế theo thông tư mới, ví dụ Thông tư 87/2026/TT-BTC được nhắc trong các nguồn đã tra).
- Mức trần phụ cấp ăn uống, quy định miễn thuế làm thêm giờ/làm đêm, mức khấu trừ hưu trí tự nguyện (tài liệu đã tra nêu tối đa 1 triệu đồng/tháng).

### 1.8 Trích dẫn pháp lý theo ngữ cảnh (Contextual Legal Citation)
- Không xây kho luật toàn văn. Mỗi quy tắc/công thức có metadata căn cứ: ID ổn định; điểm, khoản, điều, số văn bản; giải thích ngắn 2–3 câu; URL văn bản gốc trên vbpl.vn; phiên bản, kỳ và ngày áp dụng; trạng thái và ngày xác minh. URL chi tiết phải được đối chiếu trước khi nạp dữ liệu thật, không tự bịa itemid.
- Mỗi thành phần kết quả giữ liên kết đến đúng căn cứ của rule và phiên bản/kỳ đã dùng. Metadata thuộc mô hình độc lập, không khiến Core phụ thuộc Web/DB; đường tính thuế không tự gọi mạng. Khi thêm DB, lưu metadata cùng phiên bản `TaxRuleSet`.
- Cạnh từng kết quả có nút `?` mở khung giải thích 2–3 câu và liên kết văn bản gốc; thao tác được bằng bàn phím và trên mobile. Thiếu căn cứ, URL hoặc chưa xác minh thì hiển thị rõ trạng thái tương ứng, không tạo nguồn giả hay trình bày nguồn chưa xác minh như đã xác minh.

**Ví dụ UX do người dùng cung cấp — chỉ minh họa, cần đối chiếu văn bản gốc và hiệu lực theo kỳ trước khi hiện thực, không xác nhận là luật hiện hành:** thu nhập vãng lai 2.500.000đ, khấu trừ minh họa 250.000đ (10%). Khung giải thích mô tả cá nhân cư trú không ký hợp đồng hoặc có hợp đồng dưới 03 tháng, với khoản chi từ 2.000.000đ/lần; người dùng dẫn điểm i khoản 1 Điều 25 Thông tư 111/2013/TT-BTC. Chưa gắn URL chi tiết khi chưa xác minh.

**Checklist nghiệm thu khi hiện thực**
- [ ] Mỗi thành phần kết quả liên kết đúng căn cứ theo rule và kỳ/phiên bản thực sự được dùng; đổi kỳ chọn đúng phiên bản.
- [ ] Nút `?` mở giải thích 2–3 câu và link văn bản gốc vbpl.vn đã xác minh; thiếu nguồn hoặc chưa xác minh được thể hiện rõ, không tạo link giả.
- [ ] Kiểm thử URL sai hoặc không phải nguồn gốc được phát hiện và không nạp như nguồn đã xác minh; kiểm thử thao tác bàn phím và mobile.

---

## 2. Stack công nghệ (miễn phí)

| Thành phần | Lựa chọn |
|---|---|
| Runtime / ngôn ngữ | .NET 10 (LTS), C# |
| Web | ASP.NET Core (Razor Pages hoặc MVC cho MVP) |
| ORM | EF Core |
| Database | SQLite khi phát triển; PostgreSQL nếu cần (cài trực tiếp hoặc Docker, tùy chọn) |
| Test | xUnit |
| Xác thực | ASP.NET Core Identity |
| Mã hóa | Data Protection API hoặc AES-GCM, khóa để ngoài mã nguồn (user-secrets / biến môi trường) |
| CI | GitHub Actions |
| Quản lý mã nguồn | Git |

---

## 3. Cấu trúc solution mục tiêu

Feature `TaxRules` trong Data chứa persistence lương tháng/manual; xem ADR 0014 về hai đường nguồn và phạm vi chưa hỗ trợ.

```
TaxManager.sln
├── src/
│   ├── TaxManager.Core/        # Engine tính thuế (thuần C#, không phụ thuộc web/DB)
│   ├── TaxManager.Data/        # EF Core, migration, seeder   (từ giai đoạn 2)
│   └── TaxManager.Web/         # ASP.NET Core Web             (từ giai đoạn 3)
└── tests/
    ├── TaxManager.Core.Tests/
    └── TaxManager.Data.Tests/  (từ giai đoạn 2)
```

Quy ước cho mã Data và Data.Tests mới:

- Nhóm theo trách nhiệm hoặc tính năng: `Profiles`, `Income`, `Dependents`; `Infrastructure` chứa context và factory. Mỗi kiểu chính có tệp riêng, cấu hình EF và mapper tách khỏi entity.
- Đặt migration và model snapshot do EF sinh trong `Migrations`; giữ nguyên cấu trúc tệp generated theo công cụ EF.
- Tests chia theo tính năng và hành vi, fixture dùng chung đặt trong `Fixtures`; không gộp các test và fixture không liên quan vào một tệp.
- Quy ước này áp dụng cho phần Data mới, không yêu cầu di chuyển hoặc refactor Core hiện có.

---

## 4. Lộ trình

### Bước 0 – Chuẩn bị
- [ ] Cài .NET 10 SDK, VS Code hoặc Visual Studio Community, Git.
- [ ] Tạo thư mục dự án, `git init`, đặt file `PLAN.md` này ở thư mục gốc.
- [ ] Tạo `.gitignore` cho .NET.

---

### GIAI ĐOẠN 1 – LÕI TÍNH THUẾ (chưa có web, chưa có DB)

#### Bước 1.1 – Tính thuế lương theo THÁNG, luật 2026
**Mục tiêu:** engine tính thuế cơ bản, có test.

**Việc cần làm**
- Tạo solution với `TaxManager.Core` và `TaxManager.Core.Tests`.
- Models: `Bracket`, `TaxRuleSet` (có `FromYear`, giảm trừ bản thân, giảm trừ người phụ thuộc, danh sách bậc thuế), `TaxRuleSet.Vietnam2026`.
- `PitCalculator`: tính thuế lũy tiến từng phần từ thu nhập tính thuế. Không làm tròn trong class này.
- `MonthlySalaryCalculator`: nhận `grossIncome`, `insurance` (số tiền nhập vào), `dependents`, `ruleSet`; trả về record kết quả có từng bước trung gian và thuế phải nộp.
- Thu nhập tính thuế âm thì coi là 0.
- Thiết kế metadata căn cứ độc lập Web/DB cho từng rule/công thức và liên kết từng thành phần kết quả với căn cứ đã dùng; không gọi mạng trong đường tính thuế. Test mapping kết quả và trạng thái thiếu/chưa xác minh.

**Test bắt buộc (xUnit Theory)**

Tính từ thu nhập tính thuế:
| Thu nhập tính thuế | Thuế |
|---|---|
| 0 | 0 |
| 10.000.000 | 500.000 |
| 30.000.000 | 2.500.000 |
| 60.000.000 | 8.500.000 |
| 100.000.000 | 20.500.000 |
| 120.000.000 | 27.500.000 |
| 10.000.001 | 500.000,1 (kiểm tra điểm biên, không làm tròn) |

Tính từ lương:
| Lương | Bảo hiểm | Phụ thuộc | Kết quả |
|---|---|---|---|
| 30.000.000 | 3.150.000 | 0 | Thuế 635.000 |
| 20.000.000 | 2.100.000 | 1 | Thu nhập tính thuế âm → thuế 0 |
| 50.000.000 | 5.000.000 | 2 | Thuế 1.210.000 |

**Prompt mẫu cho công cụ vibecode**
```
Đọc PLAN.md. Chỉ thực hiện Bước 1.1. Tạo solution .NET 10 gồm src/TaxManager.Core
(class library, không dùng thư viện ngoài) và tests/TaxManager.Core.Tests (xUnit).
Làm đúng theo mục "Việc cần làm" và "Test bắt buộc" của bước 1.1, tuân thủ mục 0
(Quy tắc làm việc). Chạy dotnet test cho đến khi xanh. Không thêm tính năng ngoài phạm vi.
```

**Xong khi**
- [ ] `dotnet test` xanh toàn bộ.
- [ ] Không còn con số luật nào nằm ngoài `TaxRuleSet`.
- [ ] Bạn tự bấm máy tính 2–3 ca ngoài danh sách và khớp với kết quả của code.
- [ ] Bạn đọc hiểu code của `PitCalculator`.
- [ ] `git commit`.

#### Bước 1.2 – Thêm bộ luật 2025 và cơ chế chọn luật theo kỳ
- Bạn tra và cung cấp **ngưỡng các bậc của biểu 7 bậc cũ** và mức giảm trừ cũ từ nguồn chính thức.
- Thêm `TaxRuleSet.Vietnam2025`; thêm `TaxRuleProvider` chọn bộ luật theo năm/kỳ tính thuế.
- Test: cùng một mức lương cho ra kết quả khác nhau đúng theo năm; năm không có bộ luật thì báo lỗi rõ ràng.
- Test chọn đúng phiên bản căn cứ theo kỳ/ngày áp dụng khi thay đổi bộ luật.
- **Xong khi:** test xanh, commit.

#### Bước 1.3 – Bảo hiểm bắt buộc và quy tắc làm tròn
- Bạn cung cấp mức trần/mức sàn lương đóng bảo hiểm hiện hành (tra từ nguồn chính thức).
- Tự tính BHXH/BHYT/BHTN từ lương đóng bảo hiểm; thêm các tỷ lệ vào bộ quy tắc.
- Chốt quy tắc làm tròn (viết thành test và ghi chú trong code).
- **Xong khi:** nhập lương gross ra thuế; test xanh; commit.

#### Bước 1.4 – Người phụ thuộc theo tháng và tổng hợp cả năm
- Người phụ thuộc có tháng bắt đầu/kết thúc hiệu lực giảm trừ.
- Tổng hợp 12 tháng; ước tính quyết toán: thuế thực tế cả năm so với thuế đã tạm nộp, ra số **hoàn** hoặc **nộp thêm**.
- **Xong khi:** có test cho người phụ thuộc đăng ký giữa năm và test quyết toán cả năm; commit.

#### Bước 1.5 – Các loại thu nhập khác
Làm **từng loại một**, và chỉ làm khi bạn đã có tài liệu chính thức để đối chiếu:
- Thưởng, thu nhập bất thường.
- Làm thêm giờ, làm đêm, phụ cấp miễn thuế (có mức trần).
- Thu nhập vãng lai (khấu trừ tại nguồn).
- Với thu nhập vãng lai, đối chiếu ví dụ mục 1.8 với văn bản gốc và hiệu lực theo kỳ trước khi hiện thực; test liên kết căn cứ tương ứng.
- Nhiều nơi trả lương trong cùng năm.
- Khoản giảm trừ khác (từ thiện, hưu trí tự nguyện).
- **Xong khi:** mỗi loại có test riêng; commit.

---

### GIAI ĐOẠN 2 – DỮ LIỆU VÀ BẢO MẬT DỮ LIỆU

#### Bước 2.1 – EF Core và lưu dữ liệu
- Tạo `TaxManager.Data`; entity: hồ sơ người dùng, khoản thu nhập theo tháng, người phụ thuộc.
- EF Core + SQLite; migration.
- Thiết kế bảng theo nhu cầu thực tế của giai đoạn 1 (không ép theo số bảng cố định).
- **Xong khi:** lưu và đọc lại đúng; test dữ liệu xanh.

#### Bước 2.2 – Bảng luật thuế có ngày hiệu lực
- Lưu snapshot lương tháng/bảo hiểm nhập tay, bậc thuế và metadata căn cứ theo phiên bản trong SQLite; các rule graph tùy chọn khác chưa được hỗ trợ và bị từ chối.
- Kỳ registration trọn tháng khác ngày hiệu lực văn bản; FromYear chỉ là nhãn. Trigger SQLite chặn kỳ trùng hoặc chồng trên timeline chung.
- Nạp chính thức chỉ nhận căn cứ Verified có ID, version, VerifiedOn và URL HTTPS trang chi tiết vbpl.vn với ItemID hợp lệ. Không coi kiểm tra URL là audit pháp lý.
- Seed và load tham khảo 2025/2026 phải gọi tường minh, giữ nguyên trạng thái và metadata Core chưa xác minh; không tự seed trong startup/migration và không dùng làm fallback chính thức.
- **Xong khi:** engine đọc luật từ DB; kiểm thử round-trip, migration và chống chồng kỳ xanh; review độc lập hoàn tất.

#### Bước 2.3 – Mã hóa và audit log
- Chỉ lưu dữ liệu cần thiết; chưa thu thêm MST hoặc CCCD.
- Mã hóa tên và các khoản thu nhập, bảo hiểm, thuế đã khấu trừ bằng AES-GCM; khóa bền ở ngoài mã nguồn và DB. Audit thay đổi thu nhập và khai báo phải lưu nguyên tử với thay đổi nghiệp vụ.
- **Trạng thái 23/11/2026:** đã triển khai toàn bộ tích hợp EF protected BLOB/hydration/dirty detection, key-check và external durable ring; audit sync/async cùng transaction/savepoint với dữ liệu nghiệp vụ; append-only guard, ordinary delete giữ audit và purge riêng.
- Migration thứ ba giữ nguyên bốn tệp migration/designer cũ; direct upgrade populated bị chặn, copy-forward synthetic TEMP tạo target mới và đối chiếu field/PK/quan hệ/rules bằng context mới. Chưa có upgrader vận hành cho dữ liệu thật; không chạy trên DB người dùng.
- **Kiểm chứng sau sửa review vòng 2:** build solution thành công (0 warning/error); Data 226/226 và Core 197/197 đạt, không skip; EF has-pending-model-changes báo không có thay đổi. Có kiểm thử crypto/context swaps, DB/WAL/log confidentiality, backup/key reopen/restore, SQL/commit/cancel faults, transaction/savepoint/Save(false), stale/busy/unknown-commit retries, cascade/purge và upgrade failure/source preservation. 25 ca hồi quy vòng 1 tiếp tục đạt: accept/clear rồi ghi ngay sau Save(false), guard chưa accept; detached period Update/Delete và stale thật; upgrade trống/rules-only, giữ toàn bộ luật, key validation khi reopen, lỗi import và mất metadata ở DB populated. Thêm 66 ca sync/async vòng 2: Attach Unchanged/no-op, principal thêm child, readonly concurrent edit kể cả mixed save, setter sau Attach (null/empty name và từng/nhiều khoản tiền), detached Update/Remove với actual before-state và stale write thật cho cả bốn loại entity. Probe gốc reviewer vòng 2 trả Save=0, giữ tên/audit và không stale-check readonly ở cả hai API.
- **Review:** vòng 3 độc lập đã kiểm chứng lại cả bốn lỗi của vòng 1–2, không có phát hiện mới. Data 226/226 và Core 197/197 qua, model khớp migration. Phần triển khai/kiểm thử/review kỹ thuật 2.3 hoàn tất; người dùng vẫn cần review bảo mật trước dữ liệu thật. Tiếp tục 2.4 bằng dữ liệu giả.
- Giới hạn vận hành, metadata plaintext, khóa/backup, ValidateProtection khi reopen (kể cả DB empty), retry/OperationId và xóa/purge được ghi tại ADR0015, development và sơ đồ database-schema. Không có auth, chống DBA hoặc cam kết forensic erasure/retention pháp lý.
- **Xong khi:** triển khai và kiểm thử đầy đủ, review độc lập hoàn tất; người dùng review bảo mật trước khi dùng dữ liệu thật. Metadata, bản sao và di tích dữ liệu cũ có thể vẫn lộ thông tin.

#### Bước 2.4 – Lịch sử tính toán
- **Trạng thái 23/11/2026:** đã triển khai service tính thật bằng MonthlySalaryCalculator từ persisted income/manual insurance và dependent intervals đúng hồ sơ, catalog cùng transaction/write lock. SourceMode Official/References bắt buộc, dùng chung strict source policy với 2.2; không seed/fallback hoặc nhận result caller. Chưa Web/Identity hay thêm nguồn thu/optional graph.
- Snapshot v1 tự chứa input thực dùng (actual withheld riêng để đối soát, exemptIncome=0), mọi kết quả trung gian/portion/basis Core, RuleId+Version/tham số/provenance/registration và trạng thái/ngày pháp lý nguyên trạng, schema/engine contract/Core assembly+MVID và UTC. Đọc không phụ thuộc nguồn hay calculator hiện tại. Decimal exact và collections bất biến; schema/graph/số không hỗ trợ bị reject.
- History/audit dùng protection/key-check/external ring 2.3, AAD xác thực mọi header; chỉ service được append, supported SaveChanges sync/async từ chối caller Add/Update/Delete. History và audit Add atomic với toàn operation, caller savepoint/rollback và unknown-commit reconciliation theo OperationId. Không ghi names/amounts/provenance cá nhân plaintext trong payload/SQL/logs.
- **Q-1 đã chốt:** ordinary-delete giữ encrypted history bằng logical ProfileId như audit, không FK cascade. Đọc luôn ProfileId+HistoryId kể cả sau delete, không phải auth. Purge xóa toàn bộ target history/audit kể cả hồ sơ đã xóa, không receipt liên kết; không tự đặt retention pháp lý, không hứa forensic erasure.
- Migration thứ tư additive `20261123045552_AddCalculationHistory`, sáu tệp ba migration/designer cũ giữ nguyên SHA256. Fresh và upgrade từ populated2.3 giữ/decrypt nguyên dữ liệu/audit/rules/sentinel và bốn overlap triggers. Down từ chối; rollback bằng matching backup/app/keys. Không migration/key/DB thật hoặc Git index/HEAD mutation.
- **Kiểm chứng:** solution build exit0 (0 error, 1 NU1900 do không truy cập endpoint vulnerability NuGet); history 74/74, full Data300/300 và Core197/197, 0 failed/skipped. EF has-pending-model-changes exit0, model khớp. Tests bao phủ Core/provenance/full snapshot roundtrip sau sửa/xóa nguồn, culture/decimal/clamp, source/ownership, guard/tamper/key/context, confidentiality payload/DB/WAL/logs/SQL parameters, concurrent writer lock, stale tracked sources, SQL/crypto/size/overflow/cancel/commit faults, savepoint/OperationId và delete/purge rollback. Tất cả fixtures DB/keys tại TEMP. Scoped diff whitespace check qua; global check còn blank line có sẵn trong tasks.md của2.3, giữ nguyên baseline.
- **Review:** vòng 1 độc lập (run 01M83D1FQC0K2XSGANCQQX6T69) đã kiểm tra đủ 44 tệp task-only, không có phát hiện mới; chạy lại Data300/300, Core197/197, model và sáu hash migration cũ đều đạt. Triển khai/kiểm thử/review kỹ thuật hoàn tất. Người dùng tự commit và vẫn cần review bảo mật trước dữ liệu thật. Contract/giới hạn tại ADR0016, development và database-schema.

---

### GIAI ĐOẠN 3 – WEB MVP

#### Lộ trình sản phẩm sau 3.1

[Roadmap sản phẩm](docs/product-roadmap.md) là nguồn đối chiếu đầy đủ 12 chức năng cốt lõi, 7 tiện ích, phạm vi và điều kiện hoàn thành. Sáu chặng dưới đây là thứ tự giao theo phụ thuộc; các bước nền và chứng cứ nghiệm thu 3.1 vẫn giữ nguyên. Mỗi chặng sau cần phiếu và kiểm chứng riêng:

1. **Nhập liệu 3.1b:** lương theo giai đoạn và tháng ngoại lệ, xem trước, nền nhập bảo hiểm, giảm trừ, khấu trừ và trạng thái dữ liệu theo phạm vi 3.1b đã hoàn tất.
2. **Lưu và mở hồ sơ năm:** chọn năm thuế, phạm vi và phiên bản luật (bản đầu chỉ 2026); lưu/mở/cập nhật hồ sơ, khấu trừ và metadata, bảo vệ/audit và xóa có chủ đích. Giữ bất biến đầu vào, snapshot và luật của lịch sử; lưu kết quả hiện có và chuẩn bị contract kết quả năm, không đòi dự báo năm đã có.
3. **Tổng hợp và dự báo năm:** dùng hồ sơ đã lưu; tích hợp thưởng thực tế/dự kiến được hỗ trợ, người phụ thuộc theo thời gian, bảo hiểm, giảm trừ và khấu trừ; giải thích phép tính, căn cứ đúng kỳ, dữ liệu thiếu, trạng thái và độ phủ riêng. Chỉ hiện chênh lệch khi đủ khấu trừ hoặc giả định rõ; nộp thừa ước tính không bảo đảm được hoàn.
4. **Xuất dữ liệu và checklist:** báo cáo tham khảo và checklist chuẩn bị quyết toán; sao lưu và khôi phục riêng, bảo toàn metadata, phiên bản luật và lịch sử, kiểm tra bản nhập trước khi ghi và không ghi đè âm thầm. Báo cáo không tự là bản sao lưu; PDF trình bày đẹp để sau.
5. **Hai kịch bản:** so sánh cơ sở với thêm thưởng giả định được hỗ trợ, tránh cộng trùng gross và không ghi giả định vào hồ sơ thực tế; khoảng giữa kết quả không phải bảo đảm hay xác suất.
6. **Đối chiếu:** so hồ sơ với số người dùng cung cấp từ công ty hoặc eTax theo nguồn, chỉ tiêu và kỳ; nêu chênh lệch và phần thiếu, không kết luận nguồn kia sai.

Quyền riêng tư xuyên suốt: thu tối thiểu, không giữ file gốc hay tài khoản eTax; giải thích phạm vi xóa hồ sơ, lịch sử và bản sao, bảo vệ bản sao. Đăng nhập và cách ly tài khoản là điều kiện trước khi phục vụ nhiều người qua mạng; hiện chỉ local với dữ liệu giả, không cho phép dùng dữ liệu thật hay deploy theo lộ trình này.

Các tiện ích dán Excel có xem trước, sao chép năm mới có xác nhận lại, so sánh hai lần dự báo đã lưu và Gross–Net nằm trong backlog, chưa cam kết. Cập nhật luật bằng AI, nhiều nơi trả lương, freelance/kinh doanh, thu nhập nước ngoài, OCR và XML cần quyết định và kế hoạch riêng. Chặng 2 đã nghiệm thu kỹ thuật theo bốn tiêu chí của phiếu `01M8BK6R1EC84DFJ05Z8FH31C2`, done và land ngày 26/11/2026, tích hợp tại commit `f4f3133`; chỉ local synthetic. Chặng 3–6 và backlog chưa được coi là hoàn thành; có thể chuẩn bị thiết kế song song nhưng chỉ triển khai khi chặng trước hoàn tất. Đối chiếu hai phiếu và bằng chứng tại [báo cáo rà soát worktree](docs/reviews/2026-11-27-bint-worktrees.md).

#### Bước 3.1 – Giao diện cơ bản (chưa cần đăng nhập)
- `TaxManager.Web` (Razor Pages hoặc MVC).
- Trang nhập lương 12 tháng, trang kết quả hiển thị **từng bước tính** (để người dùng hiểu số thuế từ đâu ra).
- Thêm nút `?` cạnh từng kết quả, mở giải thích 2–3 câu và link nguồn gốc đã xác minh; thể hiện nguồn thiếu/chưa xác minh. Kiểm thử mở/đóng bằng bàn phím và sử dụng trên mobile.
- Disclaimer "chỉ mang tính tham khảo".
- **Xong khi:** dùng được trên máy local.

#### Bước 3.1b – Nhập theo giai đoạn và trạng thái dữ liệu
- Demo local synthetic năm 2026, một nơi trả lương: nhập gross theo giai đoạn, tháng ngoại lệ và xem trước 12 tháng; bảo hiểm, người phụ thuộc và khấu trừ nhập theo tháng cùng trạng thái, nguồn và ghi chú. Chưa nhập khác với 0.
- Phạm vi chốt ngày 25/11/2026 chỉ gồm nhập liệu, preview và trạng thái dữ liệu. Chưa có tính dự báo năm, kịch bản thưởng, lưu/mở hồ sơ forecast, migration hay audit forecast; các phần này và QA tương ứng làm ở bước sau. Core hiện có là nền kỹ thuật, không phải tính năng Web đã hoàn thành.
- **Trạng thái:** phiếu 01M89PFQ3ETQDTCSGPMAN0DEPW đã hoàn tất và đưa về cây gốc; không thay đổi nghiệm thu 3.1.

#### Bước 3.2 – Đăng nhập (ASP.NET Core Identity)
- Đăng ký, đăng nhập, mỗi người chỉ thấy dữ liệu của mình.
- Bật HTTPS, cookie an toàn, chống CSRF (mặc định của framework, kiểm tra lại).
- **Xong khi:** thử bằng 2 tài khoản, không xem chéo được dữ liệu nhau.

#### Bước 3.3 – Màn hình quyết toán năm
- Theo thứ tự roadmap: lưu/mở hồ sơ và bảo vệ/audit ở chặng 2 trước tổng hợp, dự báo năm ở chặng 3; xuất/checklist ở chặng 4, hai kịch bản ở chặng 5 và đối chiếu ở chặng 6. Không coi preview 3.1b là quyết toán.
- Tổng hợp nghĩa vụ năm và bảng chi tiết từng tháng; chỉ hiển thị chênh lệch nộp thừa hoặc phải nộp thêm khi đủ dữ liệu khấu trừ hoặc có giả định rõ ràng, không hứa nộp thừa chắc chắn được hoàn.
- **Xong khi:** kết quả khớp với test của bước 1.4.

---

### GIAI ĐOẠN 4 – HOÀN THIỆN VÀ TRIỂN KHAI

#### Bước 4.1 – Quyền dữ liệu và xuất báo cáo
- Xuất Excel/PDF **tham khảo** (chưa phải tờ khai chính thức).
- Chức năng đồng ý xử lý dữ liệu, xuất dữ liệu cá nhân, xóa tài khoản và dữ liệu.

#### Bước 4.2 – CI, sao lưu, deploy
- GitHub Actions chạy `dotnet test` mỗi lần push.
- Cơ chế sao lưu dữ liệu.
- Deploy lên gói miễn phí (kiểm tra điều kiện và giới hạn RAM hiện hành của nhà cung cấp tại thời điểm deploy).
- **Không đưa dữ liệu thật lên cloud khi còn trong giai đoạn thử nghiệm.**

#### Bước 4.3 – (Tùy chọn) Xuất XML tờ khai quyết toán
- Chỉ làm khi đã có **mẫu và cấu trúc chính thức hiện hành** để đối chiếu, và thử import được vào phần mềm khai thuế của cơ quan thuế.
- Không để AI tự suy đoán cấu trúc XML.

---

## 5. Theo dõi tiến độ

| Bước | Trạng thái | Ghi chú |
|---|---|---|
| 0 | ☐ | |
| 1.1 | ☐ | |
| 1.2 | ☐ | |
| 1.3 | ☐ | |
| 1.4 | ☐ | |
| 1.5 | ☐ | |
| 2.1 | ☑ | SQLite persistence; Core 197/197 + Data 15/15; review vòng 2 đạt ngày 22/11/2026. Quy ước tổ chức file tại mục 3. |
| 2.2 | ☑ | SQLite lưu luật theo phiên bản/kỳ và căn cứ; Core 197/197 + Data 83/83. Review độc lập vòng 5 được người dùng cho phép: không còn lỗi chặn, đã sửa lỗi dữ liệu cũ khi cập nhật (23/11/2026). |
| 2.3 | ◐ | Triển khai, 423 test và review độc lập vòng 3 hoàn tất; bốn lỗi cũ đã được kiểm chứng sửa, không có phát hiện mới. Còn review bảo mật của người dùng trước dữ liệu thật; có thể tiếp tục 2.4 với dữ liệu giả. |
| 2.4 | ◐ | Triển khai, docs, 497 test và review độc lập vòng 1 hoàn tất, không có phát hiện mới; model khớp. Còn review bảo mật của người dùng trước dữ liệu thật. |
| 3.1 | ✅ | Web MVP local bằng dữ liệu giả đã hoàn tất nghiệm thu kỹ thuật ngày 24/11/2026: nhập 12 tháng, kết quả và căn cứ từ snapshot; build không warning, 527 test qua (Core 197, Data 300, Web 30), EF không lệch migration, Bint verify qua và review vòng 6 xác nhận lỗi cuối đã sửa. Đã kiểm tra desktop/mobile giả lập và bàn phím; hướng dẫn, phạm vi và giới hạn tại docs/web-mvp.md. |
| 3.1b | ☑ | Phiếu 01M89PFQ3ETQDTCSGPMAN0DEPW đã done và land; phạm vi nhập liệu, preview và trạng thái, các chặng tiếp theo chưa hoàn thành. |
| 3.2 | ☐ | |
| 3.3 | ☐ | |
| 4.1 | ☐ | |
| 4.2 | ☐ | |
| 4.3 | ☐ | |

## 6. Nguồn tham chiếu nên dùng để đối chiếu
- Cổng thông tin Tổng cục Thuế / Bộ Tài chính (nguồn chính thức nhất).
- Toàn văn các văn bản ở mục 1.1 (ví dụ trên thuvienphapluat.vn).
- Khi nguồn không chính thức mâu thuẫn nhau, luôn lấy văn bản gốc làm chuẩn.
