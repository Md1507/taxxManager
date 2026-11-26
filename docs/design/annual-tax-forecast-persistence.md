# Đề xuất lưu dự báo và migration

Tài liệu đề xuất để duyệt trước khi tạo migration; chưa có schema hay migration được áp dụng.

- Thêm bảng hồ sơ dự báo riêng với khóa hồ sơ và payload mã hóa, chứa năm, các giá trị theo kỳ, chỉ tiêu, trạng thái, nguồn, ghi chú và ngày nhập; không dùng `MonthlyIncome` cho giá trị dự kiến hoặc giả định.
- Thêm bảng snapshot dự báo append-only, có định danh lần tính và payload mã hóa chứa đầu vào, kết quả, provenance/rule và thời điểm tính; tạo snapshot mới khi tính lại và giữ nguyên snapshot trước.
- Nối pipeline protection và audit hiện có: xác thực khóa khi đọc lại kể cả kết quả trống, mã hóa metadata nhạy cảm, ghi dữ liệu và audit cùng transaction, xử lý rollback; giữ nghĩa ordinary-delete/purge hiện có.
- Migration additive thêm bảng/chỉ mục cần thiết, cập nhật EF model snapshot; không sửa migration hoặc designer cũ, không đổi bảng/snapshot lịch sử. Giữ guard cho DB legacy và chỉ thử trên SQLite/keys synthetic trong TEMP.
- Mở rộng graph annual/bonus ở Data bằng contract có kiểm thử, giữ registration theo kỳ và trạng thái căn cứ, giữ cổng References/Official. Không tạo luật từ trí nhớ hoặc fallback khi kỳ/rule không hỗ trợ.
- Kiểm chứng bằng SQLite synthetic cho upgrade, context mới, decimal và metadata, khóa sai/thiếu, rollback, audit, confidentiality và bất biến lịch sử; giữ kiểm tra EF pending model changes.

Các entity, mapping, migration và kiểm thử tương ứng vẫn cần triển khai. Đề xuất này không phải bằng chứng migration an toàn hay đã được chạy.

## Đối chiếu byte migration cũ trong worktree

Lỗi `HistoryMigrationTests.AllSixPreviousMigrationFilesRemainByteIdentical` ở verify-3 do checkout đổi xuống dòng, không phải do thay đổi schema. Hai tệp `20261122161649_AddMonthlyTaxRules.Designer.cs` và `20261123034403_AddDataProtectionAndAudit.cs` đã được khôi phục byte từ cây gốc sau khi xác nhận nội dung chuẩn hóa LF giống Git blob; SHA-256 lần lượt là `307898C6F2F913E8A49868C7FCBD5E69DC3A233F1B889F3EA379C12414F6937D` và `E47461031B210F9F2906717ADEBCE15D4F271B29067335C743728123843EEE99`, đúng kỳ vọng test. Test lọc theo tên này chạy trực tiếp PowerShell với SDK 10.0.404 đạt 1/1. Git coi thay đổi xuống dòng là tương đương, nên một checkout khác vẫn cần giữ đúng byte trước khi chạy kiểm tra hash; kết quả này không thay thế kiểm chứng Bint hoặc kiểm thử migration dự báo chưa được tạo.
