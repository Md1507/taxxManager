# Đề xuất lưu dự báo và migration

Thiết kế ban đầu; chặng 2 đang triển khai và chưa nghiệm thu. Đã bổ sung migration additive cho hồ sơ và snapshot dự báo trong worktree, chỉ dùng synthetic TEMP.

- Thêm bảng hồ sơ dự báo riêng với khóa hồ sơ và payload mã hóa, chứa năm, các giá trị theo kỳ, chỉ tiêu, trạng thái, nguồn, ghi chú và ngày nhập; không dùng `MonthlyIncome` cho giá trị dự kiến hoặc giả định.
- Thêm bảng snapshot dự báo append-only, có định danh lần tính và payload mã hóa chứa đầu vào, kết quả, provenance/rule và thời điểm tính; tạo snapshot mới khi tính lại và giữ nguyên snapshot trước.
- Nối pipeline protection và audit hiện có: xác thực khóa khi đọc lại kể cả kết quả trống, mã hóa metadata nhạy cảm, ghi dữ liệu và audit cùng transaction, xử lý rollback; giữ nghĩa ordinary-delete/purge hiện có.
- Migration additive thêm bảng/chỉ mục cần thiết, cập nhật EF model snapshot; không sửa migration hoặc designer cũ, không đổi bảng/snapshot lịch sử. Giữ guard cho DB legacy và chỉ thử trên SQLite/keys synthetic trong TEMP.
- Mở rộng graph annual/bonus ở Data bằng contract có kiểm thử, giữ registration theo kỳ và trạng thái căn cứ, giữ cổng References/Official. Không tạo luật từ trí nhớ hoặc fallback khi kỳ/rule không hỗ trợ.
- Kiểm chứng bằng SQLite synthetic cho upgrade, context mới, decimal và metadata, khóa sai/thiếu, rollback, audit, confidentiality và bất biến lịch sử; giữ kiểm tra EF pending model changes.

Snapshot hiện lưu đầu vào dự báo theo revision; chưa tạo kết quả năm giả. Bằng chứng kiểm thử và QA cần được kiểm chứng đầy đủ trước khi nghiệm thu.

## Contract riêng cho kết quả năm ở chặng 3

Snapshot đầu vào của chặng 2 không phải kết quả năm. Khi chặng 3 triển khai, mỗi lần tính năm cần một bản ghi kết quả riêng, append-only, có định danh lần tính, năm, thời điểm tính, tham chiếu chính xác revision snapshot đầu vào và phiên bản lược đồ payload. Payload kết quả được mã hóa phải giữ kết quả, trạng thái/độ đầy đủ và bản chụp căn cứ, phiên bản luật áp dụng cho từng kỳ; không chỉ trỏ tới luật hoặc đầu vào có thể bị thay đổi. Tính lại tạo bản ghi mới trong cùng transaction với audit, không ghi đè bản cũ; xóa hồ sơ thông thường vẫn giữ lịch sử theo semantics hiện tại. Chặng 3 sẽ xác định schema chi tiết và kiểm chứng migration, bảo vệ, tính nguyên tử và đọc lại các phiên bản; chặng 2 chưa có bảng hay API lưu kết quả năm và không tạo bản ghi giả.

## Đối chiếu byte migration cũ trong worktree

Lỗi `HistoryMigrationTests.AllSixPreviousMigrationFilesRemainByteIdentical` ở verify-3 do checkout đổi xuống dòng, không phải do thay đổi schema. Hai tệp `20261122161649_AddMonthlyTaxRules.Designer.cs` và `20261123034403_AddDataProtectionAndAudit.cs` đã được khôi phục byte từ cây gốc sau khi xác nhận nội dung chuẩn hóa LF giống Git blob; SHA-256 lần lượt là `307898C6F2F913E8A49868C7FCBD5E69DC3A233F1B889F3EA379C12414F6937D` và `E47461031B210F9F2906717ADEBCE15D4F271B29067335C743728123843EEE99`, đúng kỳ vọng test. Test lọc theo tên này chạy trực tiếp PowerShell với SDK 10.0.404 đạt 1/1. Git coi thay đổi xuống dòng là tương đương, nên một checkout khác vẫn cần giữ đúng byte trước khi chạy kiểm tra hash; kết quả này không thay thế kiểm chứng Bint hoặc kiểm thử migration dự báo và kiểm chứng đầy đủ.
