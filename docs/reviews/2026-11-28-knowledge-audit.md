# Rà soát 11 mục tri thức — 28/11/2026

> Đã triển khai sửa theo yêu cầu tiếp theo của người dùng; xem mục **Kết quả sau khi sửa** ở cuối. Các phân loại và verdict trong phần rà soát ban đầu dưới đây là trạng thái trước khi sửa.

Đối chiếu worktree tại HEAD `95368325a0100a0ee40f3632a26fad736b3dc0e5`: trước khi viết báo cáo, `git status --short` và `git diff HEAD --stat` đều rỗng. Các dòng dưới chỉ worktree này, trừ nơi ghi rõ cây gốc. `bint knowledge show` lấy metadata từ cây gốc `D:/Project/taxxManager`; so sánh trực tiếp cho thấy `workspace.js` và `Forecast.cshtml` có thay đổi nội dung ở cây gốc, còn một số tệp khác chỉ khác xuống dòng. Không coi kết quả fingerprint của CLI trên cây gốc là bằng chứng về worktree. Báo cáo lịch sử `docs/reviews/2026-11-27-bint-worktrees.md` không có trong worktree hoặc HEAD; không suy diễn rằng sự kiện chưa xảy ra.

Đã lưu 11 verdict bằng `bint knowledge verdict`, model `gpt-6.1-terra`. `unsure` ở ba mục có nội dung sai nghĩa là **cần người sửa nội dung**, không phải chưa xác định được phần sai: CLI chỉ có `valid`, `outdated`, `fix-evidence`, `unsure`, không có `edit-content`. Không dùng `valid` hay `fix-evidence` để làm mới nội dung sai, và không dùng `outdated` khi vẫn nên giữ phần hướng dẫn còn giá trị. Người duyệt cần sửa nội dung qua quy trình quản lý knowledge, đối chiếu lại evidence rồi mới quyết định trạng thái; không áp hàng loạt trước khi xem từng đề xuất. Các verdict `fix-evidence` nêu một vị trí thay thế: người duyệt cần giữ/kiểm lại các evidence còn liên quan, không giả định một vị trí đại diện đủ cho toàn bộ nội dung.

## Các mục cần sửa nội dung

### 01M85R5BA80S0DDJVNMBYQ00BS — unsure; sửa nội dung

- **Lý do:** không còn đúng rằng mọi optional graph bị từ chối. `src/TaxManager.Data/TaxRules/TaxRuleValidator.cs:14-22` từ chối các graph bảo hiểm, lương bổ sung, vãng lai, nhiều nguồn lương, từ thiện và hưu trí, nhưng chuyển graph lương năm, làm tròn và thưởng tiền mặt sang codec. `tests/TaxManager.Data.Tests/TaxRules/AnnualRuleGraphCodecTests.cs:54` kiểm round-trip; `src/TaxManager.Data/TaxRules/TaxRuleProviderLoader.cs:57-67` vẫn kiểm bộ tham khảo chính xác.
- **Thay thế đề xuất:** “Adapter hỗ trợ graph lương năm, chính sách làm tròn và thưởng tiền mặt trong phạm vi codec; các graph bảo hiểm, lương bổ sung, vãng lai, nhiều nguồn lương, từ thiện và hưu trí vẫn bị từ chối. Seed/LoadReferences chỉ nhận bộ tham khảo đã định; nguồn official phải qua ValidateOfficial. Không nâng trạng thái nguồn hoặc thêm fallback để vượt cổng.” Giữ giới hạn cần mở rộng có kiểm thử, không coi đây là giới hạn sản phẩm vĩnh viễn.
- **Enforcement:** metadata chưa khai báo `enforced_by`; `tests/TaxManager.Data.Tests/TaxRules/TaxRuleValidatorTests.cs:10` và `tests/TaxManager.Data.Tests/TaxRules/AnnualRuleGraphCodecTests.cs:13` kiểm các nhánh tương ứng. Đề xuất người duyệt gắn test phù hợp sau khi sửa.

### 01M8BMXR829S6QRCBBJWYSB8SF — unsure; sửa nội dung

- **Lý do:** `src/TaxManager.Web/Pages/Forecast.cshtml.cs:17-34` chọn parser theo marker; `src/TaxManager.Web/MoneyInput.cs:13-20` nhận dấu chấm phân nhóm và dấu phẩy thập phân khi `AmountFormat=vi-VN`. Không marker, một chấm hoặc phẩy là thập phân. `tests/TaxManager.Web.Tests/Forecasting/GroupedMoneyHttpTests.cs:14` đối chiếu `1.234` theo cả hai chế độ; `tests/TaxManager.Web.Tests/Forecasting/ForecastHttpTests.cs:296` kiểm thiếu khác 0.
- **Thay thế đề xuất:** “Giữ tiền nullable: trống là thiếu, 0 là đã nhập. ForecastAmountBinder dùng MoneyInput: với `AmountFormat=vi-VN`, chấm phân nhóm hợp lệ và phẩy là thập phân; không marker, một chấm hoặc phẩy là thập phân và không nhận cú pháp phân nhóm. Không dùng parser culture với AllowThousands; POST sai giữ chuỗi và ModelState.”
- **Enforcement:** metadata trỏ `tests/TaxManager.Web.Tests/Forecasting/ForecastHttpTests.cs`; test bổ sung `GroupedMoneyHttpTests.cs:35` kiểm nhóm sai và giữ input. Đề xuất người duyệt bổ sung tham chiếu test grouped.

### 01M8CBFWCGN1RHN8ZHFV2P3M9Z — unsure; sửa nội dung

- **Lý do:** `tests/TaxManager.Data.Tests/CalculationHistory/HistoryMigrationTests.cs:48-54` hiện nâng 3 lên 8 migration, rồi downgrade bị từ chối và còn 4; câu “lên 6 rồi gỡ hai” đã cũ. `tests/TaxManager.Data.Tests/Migrations/MigrationTests.cs:15-21` kiểm 8 và idempotency; `tests/TaxManager.Data.Tests/Profiles/ProfilePersistenceTests.cs:54-58` kiểm danh sách migration.
- **Thay thế đề xuất:** “HistoryMigrationTests hiện nâng từ 3 lên 8 migration; khi downgrade về trước AddCalculationHistory bị từ chối, còn 4. Đối chiếu các mốc test theo danh sách migration thực tế, không thay đồng loạt mọi số 4.” Giữ hai đoạn hướng dẫn còn lại; kiểm lại số khi có migration mới.
- **Enforcement:** metadata đã khai báo ba test MigrationTests, ProfilePersistenceTests và HistoryMigrationTests; test cuối còn kiểm dữ liệu, sentinel, audit và trigger tại `HistoryMigrationTests.cs:58-72`.

## Các mục còn đúng, cần sửa evidence

### 01M8CBFS1DPP02R61NX2E264B6 — fix-evidence

- **Lý do/evidence:** `src/TaxManager.Web/Pages/Forecast.cshtml.cs:299-329` dịch Periods, ModelState và stamp; `Forecast.cshtml.cs:455-461` giữ EnteredAt theo fingerprint. `tests/TaxManager.Web.Tests/Forecasting/ForecastHttpTests.cs:45-66` tăng đồng hồ một ngày và kiểm kết quả; fingerprint test thay đổi không làm sai hợp đồng. Đề xuất evidence test tại dòng 45–66, giữ evidence mã liên quan.
- **Enforcement:** metadata khai báo test `Removing_earlier_period_preserves_remaining_entry_time` tại `ForecastHttpTests.cs:45`.

### 01M8CBFTQHZAEP0A2M9H7GWSHN — fix-evidence

- **Lý do/evidence:** `src/TaxManager.Web/Demo/SyntheticDatabase.cs:19-52` kiểm TEMP, marker, links và khóa; dòng 60–69 xác thực kho trước migration bổ sung. `tests/TaxManager.Web.Tests/DemoHost.cs:18-24` truyền root; `tests/TaxManager.Web.Tests/Forecasting/ForecastPersistenceBlackBoxTests.cs:128-168` kiểm khóa sai/thiếu và host mới. Nội dung vẫn phù hợp dù constructor đã mở rộng; đề xuất evidence constructor dòng 19–69, giữ evidence host/test.
- **Enforcement:** metadata khai báo hai test mở lại/cập nhật và từ chối khóa sai hoặc thiếu trong `ForecastPersistenceBlackBoxTests.cs:128` và `:168`.

### 01M8CBFY0W4W2J2AWVTG0TFGMK — fix-evidence

- **Lý do/evidence:** `tests/TaxManager.Web.Tests/Forecasting/ForecastPersistenceBlackBoxTests.cs:639-662` gửi lại `Fields`, còn `:672-684` đọc HTML thô và HtmlDecode textarea. `src/TaxManager.Web/Pages/Forecast.cshtml:38` giữ `AttemptedValue` và Razor mã hóa; test ghi chú nhiều dòng tại `ForecastPersistenceBlackBoxTests.cs:105`. Đề xuất evidence markup dòng 38, giữ evidence probe.
- **Enforcement:** metadata chưa khai báo `enforced_by`; test persistence ở `ForecastPersistenceBlackBoxTests.cs:95` kiểm ghi chú thưởng nhiều dòng qua lưu/mở lại. Đề xuất người duyệt gắn test này.

### 01M8F49W9Q99372V01ZZBZV6NK — fix-evidence

- **Lý do/evidence:** nội dung còn phù hợp. Trong worktree, `src/TaxManager.Web/MoneyInput.cs:8-20` phân biệt hai grammar; `src/TaxManager.Web/wwwroot/workspace.js:52-86` đặt marker, chọn cả input ẩn và chuyển chuỗi không dùng Number. Trong cây gốc tại thời điểm đối chiếu, HEAD là `3870d45ead77229bd6bd923516bc63ce80ad0b3c`, `workspace.js` và `Forecast.cshtml` có thay đổi chưa commit: `D:/Project/taxxManager/src/TaxManager.Web/wwwroot/workspace.js:103-136` vẫn thực hiện các bước đó, `D:/Project/taxxManager/src/TaxManager.Web/Pages/Forecast.cshtml:101-105` đánh dấu input tiền ẩn bằng data-money, và `D:/Project/taxxManager/src/TaxManager.Web/MoneyInput.cs:14-20` giữ hai grammar. `bint knowledge show` tại thời điểm kiểm báo evidence cũ `workspace.js:53-99` dịch nguyên nội dung tới `104-150` ở cây gốc; đề xuất sửa evidence tới vị trí đó, giữ evidence MoneyInput. Vị trí cần kiểm lại khi cây gốc thay đổi.
- **Enforcement:** metadata chưa khai báo `enforced_by`; `tests/TaxManager.Web.Tests/Forecasting/GroupedMoneyHttpTests.cs:14-43` kiểm phân biệt hai định dạng và lỗi grouping ở server. Chưa xác minh có test tự động cho formatting JS và input ẩn; đề xuất người duyệt bổ sung enforcement phù hợp. Đây là candidate: verdict chỉ đề xuất sửa evidence, việc duyệt thuộc người duyệt.

## Các mục còn đúng

### 01M85QGPZKCDQB7YBCVGCJ8JNG — valid

- **Lý do/evidence:** `tests/TaxManager.Data.Tests/Audit/TrackingWriteIntentTests.cs:151-175` Attach trước setter và xác nhận dữ liệu/audit; `src/TaxManager.Data/Audit/AuditSaveCoordinator.cs:221-252` đọc bản lưu và chỉ stale-check original đáng tin. Các evidence hiện lưu vẫn trỏ vùng liên quan; không có căn cứ đổi nội dung hoặc vị trí chỉ vì nằm trong danh sách cần cập nhật.
- **Enforcement:** metadata chưa khai báo; test `TrackingWriteIntentTests.cs:151` và `:182` kiểm Attach và detached Update. Đề xuất người duyệt khai báo test.

### 01M85QGSCXRZR59Q7GF09F63X0 — valid

- **Lý do/evidence:** `src/TaxManager.Data/Audit/AuditSaveCoordinator.cs:66-70` từ chối save khi còn pending acceptance; `tests/TaxManager.Data.Tests/Audit/PendingAcceptanceTests.cs:14-54` kiểm Accept/Clear và `:91-104` kiểm từ chối lưu lặp. Evidence vẫn đúng vị trí; không cần sửa nội dung chỉ vì yêu cầu ban đầu xếp mục vào nhóm evidence.
- **Enforcement:** metadata chưa khai báo; các test PendingAcceptanceTests vừa nêu kiểm hai nhánh và audit. Đề xuất người duyệt khai báo test.

### 01M85R5FV73D0YXHBS25P7X4XA — valid

- **Lý do/evidence:** `src/TaxManager.Data/CalculationHistory/CalculationHistoryService.cs:23-32` từ chối pending writes và mở transaction; `:49-77` đọc nguồn, tính và tạo snapshot mới; `src/TaxManager.Data/Audit/AuditSaveCoordinator.cs:62-65` chặn thao tác trực tiếp. `tests/TaxManager.Data.Tests/CalculationHistory/ImmutabilityTests.cs:27` và `:54` kiểm bất biến và chặn tạo giả. Evidence hiện hữu vẫn liên quan.
- **Enforcement:** metadata chưa khai báo; các test ImmutabilityTests và `tests/TaxManager.Data.Tests/CalculationHistory/ConsistencyTests.cs:12` kiểm các phần chính. Đề xuất người duyệt gắn test, lưu ý rule cần người duyệt.

## Chưa đủ căn cứ để làm mới hoặc loại

### 01M8DHBHKD7AWVN92JBTKRJ706 — unsure

- **Lý do/evidence:** mục candidate dẫn `docs/reviews/2026-11-27-bint-worktrees.md:38`, nhưng `git ls-files` và `git log --all --` không tìm thấy tệp đó trong worktree/lịch sử đang có. Chưa có nguồn độc lập chứng minh việc dọn hai thư mục, snapshot đã land hay tình trạng junction cụ thể; không đề xuất xóa chỉ vì thiếu tệp.
- **Enforcement:** metadata chưa khai báo và chưa tìm thấy test/script trong `scripts/dev` hay `docs/reviews` xác nhận sự kiện. Người duyệt cần tìm lại báo cáo/snapshot gốc và kiểm hiện trạng trước khi quyết định giữ, sửa hay loại; chưa đề xuất nội dung thay thế khi chưa có chứng cứ.


## Kết quả sau khi sửa

Theo yêu cầu “sửa đi” của người dùng, đã cập nhật trực tiếp 7 mục nguồn, tăng version và updated_at; giữ nguyên ID, nguồn gốc, trạng thái và thông tin lần duyệt trước:

- Sửa nội dung và evidence của `01M85R5BA80S0DDJVNMBYQ00BS`: hỗ trợ graph lương năm/làm tròn/thưởng trong phạm vi codec, giữ giới hạn bảo hiểm nhập tay và cổng nguồn official/reference.
- Sửa nội dung và evidence của `01M8BMXR829S6QRCBBJWYSB8SF`: phân biệt AmountFormat=vi-VN với đầu vào không marker, giữ nullable và chuỗi nhập lỗi.
- Sửa nội dung và evidence của `01M8CBFWCGN1RHN8ZHFV2P3M9Z`: giữ mốc downgrade còn bốn migration đầu; xác định mốc mới nhất theo danh sách thực tế thay vì đóng cứng số 6 hoặc 8.
- Cập nhật evidence/fingerprint đã đối chiếu của `01M8CBFS1DPP02R61NX2E264B6`, `01M8CBFTQHZAEP0A2M9H7GWSHN`, `01M8CBFY0W4W2J2AWVTG0TFGMK`, `01M8F49W9Q99372V01ZZBZV6NK`.
- Bổ sung enforced_by trỏ test hiện có cho adapter, nhập tiền, probe textarea và giao thức vi-VN; test HTTP không được xem là kiểm chứng tự động formatting JS.
- Ba mục `01M85QGPZKCDQB7YBCVGCJ8JNG`, `01M85QGSCXRZR59Q7GF09F63X0`, `01M85R5FV73D0YXHBS25P7X4XA` giữ nguyên vì evidence đã đúng.
- `01M8DHBHKD7AWVN92JBTKRJ706` giữ nguyên candidate/unsure; chưa khôi phục được nguồn lịch sử, không dựng lại sự kiện thiếu chứng cứ.

Kiểm chứng: `bint knowledge verify` cho 10 mục có đủ căn cứ trả về 0 lỗi, 0 cảnh báo, không có evidence bị dời và không có chuyển trạng thái. Chỉ thay đổi tài liệu tri thức, không sửa mã sản phẩm/test. Đã chạy `bint bootstrap publish --yes` để dựng lại khối quản lý trong AGENTS.md. Bint giữ giới hạn 15 mục nên một số mục chỉ được truy xuất từ kho knowledge theo phạm vi, không nằm trong khối luôn nạp.

## Bổ sung: đã xử lý 01M8DHBHKD7AWVN92JBTKRJ706

Sau khi người dùng yêu cầu xử lý nốt mục còn lại, đã mở rộng kiểm tra sang archive và runtime Bint ngoài repository. Hai biên bản review gốc cùng hai thư mục archive vẫn tồn tại. Đối chiếu được 45/45 tệp archive khớp snapshot landed; 44/45 khớp commit tích hợp, khác biệt duy nhất là PLAN.md cũ đã được roadmap mới thay thế. Metadata của cả hai junction .dotnet xác nhận chúng trỏ về SDK cây chính.

Đã cập nhật nội dung mục này, chuyển evidence sang [báo cáo đối chiếu mới](2026-11-28-bint-worktrees.md), tăng version lên 2 và lưu verdict valid thay unsure. Giữ trạng thái candidate; sửa nội dung/bằng chứng không đồng nghĩa với người dùng đã duyệt trạng thái. Không khôi phục giả tài liệu cũ, không di chuyển/xóa archive hay thay đổi SDK. Bint knowledge verify cho mục này trả về 0 lỗi, 0 cảnh báo. Kết luận thiếu bằng chứng trong phần rà soát ban đầu đã được thay thế bởi kết quả đối chiếu này.
