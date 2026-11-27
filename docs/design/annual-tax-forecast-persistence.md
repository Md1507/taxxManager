# Lưu dự báo, kết quả năm và migration

Chặng 2 đã nghiệm thu kỹ thuật theo bốn tiêu chí của phiếu `01M8BK6R1EC84DFJ05Z8FH31C2`, done và land ngày 26/11/2026, tích hợp tại `f4f3133`, chỉ dùng synthetic TEMP. Hai migration additive `AddForecastDocuments` và `AddForecastSnapshots` đã nằm trên cây chính. Graph annual/bonus và kết quả năm đã triển khai trong phiếu chặng 3, đã nghiệm thu kỹ thuật ngày 28/11/2026 (Bint done, 629 test và EF sạch, review vòng 4 không có phát hiện); xem [bằng chứng](../reviews/2026-11-27-annual-stage-three.md).

- Thêm bảng hồ sơ dự báo riêng với khóa hồ sơ và payload mã hóa, chứa năm, các giá trị theo kỳ, chỉ tiêu, trạng thái, nguồn, ghi chú và ngày nhập; không dùng `MonthlyIncome` cho giá trị dự kiến hoặc giả định.
- Bảng snapshot dự báo hiện giữ đầu vào mã hóa theo revision, tạo bản mới khi lưu/cập nhật và giữ nguyên bản trước; chưa chứa kết quả tính năm hay snapshot luật năm. Contract kết quả năm riêng được mô tả bên dưới.
- Nối pipeline protection và audit hiện có: xác thực khóa khi đọc lại kể cả kết quả trống, mã hóa metadata nhạy cảm, ghi dữ liệu và audit cùng transaction, xử lý rollback; giữ nghĩa ordinary-delete/purge hiện có.
- Migration additive thêm bảng/chỉ mục cần thiết, cập nhật EF model snapshot; không sửa migration hoặc designer cũ, không đổi bảng/snapshot lịch sử. Giữ guard cho DB legacy và chỉ thử trên SQLite/keys synthetic trong TEMP.
- Chặng 3 mở rộng graph annual/bonus ở Data bằng contract có kiểm thử, giữ registration theo kỳ và trạng thái căn cứ, giữ cổng References/Official. Không tạo luật từ trí nhớ hoặc fallback khi kỳ/rule không hỗ trợ.
- Kiểm chứng bằng SQLite synthetic cho upgrade, context mới, decimal và metadata, khóa sai/thiếu, rollback, audit, confidentiality và bất biến lịch sử; giữ kiểm tra EF pending model changes.

Ở mốc chặng 2, snapshot chỉ lưu đầu vào theo revision. Log `verify-final-full.log` của phiếu ghi Core 205/205, Data 305/305, Web 70/70 và EF không có model changes; review vòng 7 không có phát hiện mới. QA mobile/bàn phím/focus do phiên điều phối trước thực hiện và lưu trong constraints/logs của phiếu; không coi đây là QA mới của lần rà soát worktree.

## Contract riêng cho kết quả năm ở chặng 3

Snapshot đầu vào chặng 2 giữ nguyên payload và revision. `ForecastDraftReader` đọc draft Web cùng EntryStamp gốc; không rewrite JSON cũ. Contract `ForecastAnnualInput` có envelope `InputSchemaVersion=1` cho caller ngoài Web. Giá trị thiếu vẫn nullable; không lưu placeholder khấu trừ của adapter Core.

Migration `20261127043040_AddAnnualRuleGraph` thêm cột graph nullable, migration `20261127102957_AddAnnualForecastResults` thêm `AnnualForecastRules` và `AnnualCalculationHistory`. Không sửa byte migration/designer cũ. Khi đọc schema trước cột graph, adapter SELECT bổ sung NULL; khi insert graph null, EF bỏ cột đó để đường legacy/copy-forward vẫn chạy. Chỉ ghi graph khác null trên schema hỗ trợ; guard copy-forward legacy giữ nguyên.

`AnnualForecastRuleCatalog` seed rõ ràng bộ References 2026 H1/H2 từ snapshot Core hiện có, gồm annual/rounding/bonus và căn cứ đầy đủ, bỏ graph tự tính bảo hiểm vì chỉ nhận nhập tay. Catalog năm riêng giữ catalog tháng cũ nguyên trạng. Khi tính, kiểm catalog tháng, nguồn và độ phủ kỳ cùng catalog năm đã lưu; không seed/fallback khi graph/kỳ bị thiếu hoặc lệch. Không nâng trạng thái nguồn và không dùng References như Official.

`AnnualForecastService.CalculateAsync(documentId, revision)` yêu cầu context không có pending writes; khóa ghi SQLite, xác thực sentinel, đọc hồ sơ, snapshot revision và catalog trong cùng transaction, rồi tự gọi engine năm. Revision cũ/bị xóa bị từ chối. Thiếu đầu vào nghĩa vụ trả danh sách tháng/chỉ tiêu, không append. Tính được thì append kết quả mới cùng audit, hỗ trợ savepoint và rollback transaction của caller; không nhận kết quả do caller cung cấp.

`AnnualCalculationHistory` có ID lần tính, document ID, revision, năm, schema version và thời điểm UTC. Payload mã hóa chứa nguyên draft gốc, input đã chuẩn hóa cùng trạng thái/nguồn/ghi chú/ngày nhập, ID snapshot đầu vào, phiên bản engine, kết quả chi tiết và bản chụp graph/kỳ áp dụng luật. AAD xác thực metadata nhận diện. SaveChanges từ chối append trực tiếp và mọi update/delete kết quả. Lịch sử đọc snapshot, không phụ thuộc live catalog/profile, và xác thực khóa kể cả lookup trống. Ordinary delete giữ lịch sử/audit; không thêm cascade. Chưa có purge riêng cho annual history hay chức năng xóa vật lý WAL/backups.

Downgrade không được xóa bảng lịch sử năm đã có dữ liệu; khi bảng năm trống, mốc guard lịch sử tháng cũ vẫn giữ nguyên. Khởi động kho synthetic chặng 2 xác thực kho trước, rồi nâng schema và seed catalog năm; kho đã nâng không được tự sửa catalog hỏng. Không dùng đường demo này cho DB vận hành.

## Đối chiếu byte migration cũ trong worktree

Lỗi `HistoryMigrationTests.AllSixPreviousMigrationFilesRemainByteIdentical` ở verify-3 do checkout đổi xuống dòng, không phải do thay đổi schema. Hai tệp `20261122161649_AddMonthlyTaxRules.Designer.cs` và `20261123034403_AddDataProtectionAndAudit.cs` đã được khôi phục byte từ cây gốc sau khi xác nhận nội dung chuẩn hóa LF giống Git blob; SHA-256 lần lượt là `307898C6F2F913E8A49868C7FCBD5E69DC3A233F1B889F3EA379C12414F6937D` và `E47461031B210F9F2906717ADEBCE15D4F271B29067335C743728123843EEE99`, đúng kỳ vọng test. Test lọc theo tên này chạy trực tiếp PowerShell với SDK 10.0.404 đạt 1/1. Git coi thay đổi xuống dòng là tương đương, nên một checkout khác vẫn cần giữ đúng byte trước khi chạy kiểm tra hash; kết quả này không thay thế kiểm chứng Bint hoặc kiểm thử migration dự báo và kiểm chứng đầy đủ.
