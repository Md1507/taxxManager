# ADR 0014: Lưu quy tắc thuế lương tháng trong SQLite

## Phạm vi

Lưu snapshot lương tháng với bảo hiểm nhập tay, các bậc có thứ tự và metadata LegalBasis đầy đủ theo từng vai trò và bậc. Các đồ thị bảo hiểm, làm tròn, quyết toán năm, thưởng, phụ cấp, vãng lai, nhiều nguồn lương, từ thiện và hưu trí chưa được lưu; adapter từ chối snapshot có các đồ thị này.

Snapshot được định danh bằng ID và phiên bản; registration định nghĩa khoảng kỳ tính thuế gồm cả hai đầu, bắt đầu và kết thúc trọn tháng. Khoảng này không phải hiệu lực văn bản LegalBasis; FromYear chỉ là nhãn. SQLite lưu decimal dưới dạng TEXT, dùng khóa ngoại, khóa duy nhất và trigger chống khoảng kỳ chồng nhau trong một timeline. Migration kế tiếp không sửa migration S21 và không tự seed.

## Hai đường dữ liệu

`ImportOfficialAsync` và `LoadOfficialAsync` yêu cầu mọi căn cứ Verified, có ID, phiên bản, ngày xác minh và HTTPS tới trang chi tiết vbpl.vn hoặc subdomain với ItemID hợp lệ. Đây là kiểm tra kỹ thuật, không thay thế thẩm định nội dung pháp lý.

`SeedReferencesAsync` và `LoadReferencesAsync` là lựa chọn tường minh cho riêng dữ liệu Core 2025/2026. Chúng giữ nguyên metadata và trạng thái Unverified/Missing; kỳ tham khảo là hai năm lịch, không khẳng định ngày hiệu lực văn bản. Loader kiểm tra dữ liệu đọc lại, không tự seed hoặc fallback. Hai chế độ dùng cùng timeline; dữ liệu tham khảo không vượt cổng official. Import và seed dùng transaction, từ chối thay đổi cùng phiên bản và kỳ chồng nhau.

Các thay đổi TaxRules lấy quyền ghi SQLite trước khi đọc graph để kiểm tra và giữ quyền đó đến khi ghi xong. Với transaction do caller quản lý, SaveChanges sử dụng transaction đó và không tự commit hay dispose; lỗi hoặc xung đột khóa cần được caller xử lý. Với transaction do context tạo, thay đổi tracked chỉ được chấp nhận sau khi commit thành công.

Trước khi EF chuyển kiểu, loader và importer kiểm tra giá trị decimal gốc có thể biểu diễn chính xác bằng decimal; các cách viết khác nhau của cùng giá trị được chấp nhận. Year, status và ordinal phải có kiểu lưu trữ SQLite integer và nằm trong miền tương ứng. SaveChanges kiểm tra toàn bộ graph bậc sau khi gộp dữ liệu đã lưu với các thay đổi đang theo dõi. Import từ chối context có thay đổi chưa lưu, tránh ghi nhầm công việc của caller.

Khi sửa hoặc xóa graph qua context, caller phải xử lý các LegalBasis không còn được tham chiếu trong cùng lần lưu. Validation gộp quan hệ đã lưu với thay đổi đang theo dõi, kể cả cascade xóa snapshot, và từ chối trước khi ghi nếu có basis thiếu hoặc mồ côi. Mỗi snapshot còn lại phải có ít nhất một registration và mỗi registration phải trỏ tới snapshot còn lại; có thể xóa kỳ khi snapshot vẫn còn kỳ khác, hoặc xóa toàn bộ graph trong cùng lần lưu. Catalog hoàn toàn trống được phép lưu nhưng loader báo lỗi khi đọc. Basis dùng chung vẫn được giữ khi còn tham chiếu; xóa đầy đủ phải đánh dấu các dependent trước basis theo quan hệ EF. Loader vẫn từ chối dữ liệu mồ côi do ghi ngoài context.

## Vận hành

Chỉ thử nâng cấp và rollback trên database giả tách biệt. Down migration xóa dữ liệu các bảng luật mới; sao lưu và quy trình vận hành thực tế cần được xác định trước khi sử dụng dữ liệu thật. Chưa có audit pháp lý, mã hóa hoặc lưu toàn bộ rule graph.
