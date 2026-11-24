# ADR 0017 — Cập nhật quy tắc thuế bằng AI theo kỳ áp dụng

Trạng thái: định hướng sản phẩm đã được người dùng chốt ngày 24/11/2026; cơ chế cập nhật bên dưới chưa được triển khai. Đây là quyết định về sản phẩm, không phải kết luận văn bản hay dữ liệu thuế hiện tại đã được xác minh.

## Phạm vi và lựa chọn

Trước mắt tập trung thuế TNCN từ tiền lương, tiền công và các khoản liên quan. Chọn quy định phù hợp với kỳ thuế và trường hợp đang tính, xét cả hiệu lực, sửa đổi và chuyển tiếp; ngày xuất bản mới nhất không tự quyết định quy tắc áp dụng.

Dự án không có người phụ trách xác minh pháp lý. Chọn quy trình AI tìm nguồn, đề xuất, kiểm tra chéo và kích hoạt có điều kiện thay cho quy trình chờ người xác minh từng cập nhật. Đánh đổi: có thể tự vận hành nhưng phải chấp nhận một số cập nhật còn chờ và một số phép tính chưa thực hiện được khi bằng chứng chưa đủ.

## Điều kiện cập nhật

1. Dùng toàn văn từ CSDL quốc gia về pháp luật, Công báo và cổng chính thức của cơ quan ban hành làm căn cứ; bài báo và trang tổng hợp hỗ trợ phát hiện thay đổi. Mỗi quy tắc phải truy được văn bản và điều khoản nguồn.
2. AI đề xuất nội dung; một lượt AI độc lập kiểm tra lại nội dung, đối tượng, kỳ áp dụng, sửa đổi và chuyển tiếp từ bằng chứng nguồn, thay vì chỉ đồng ý với kết luận lượt trước.
3. Chỉ kích hoạt khi bằng chứng truy được, các vấn đề áp dụng đã được giải quyết và các test tính thuế liên quan đạt. Hai AI đồng ý hoặc test đạt riêng lẻ không chứng minh diễn giải pháp lý đúng; kết quả được ghi rõ là kiểm tra tự động.
4. Khi thiếu nguồn hoặc có mâu thuẫn chưa giải quyết, giữ đề xuất ở trạng thái chờ, tiếp tục tìm bằng chứng và thử lại; không tạo phụ thuộc vào người xác minh.
5. Chỉ dùng phiên bản trước khi nó vẫn phù hợp với kỳ đang tính. Nếu chưa xác định được quy tắc áp dụng, báo phần bị ảnh hưởng chưa tính được; không âm thầm lấy luật của kỳ gần nhất.

## Lịch sử và so sánh

Giữ phiên bản bộ quy tắc và các kết quả cũ để đối chiếu. Khi phát hiện thay đổi có thể ảnh hưởng kỳ đã tính, đánh dấu kết quả liên quan; trước mắt chỉ tính lại khi người dùng yêu cầu, mỗi lần tạo bản ghi mới.

UI so sánh thuộc giai đoạn sau: cho chọn hai lần tính để xem chênh lệch kết quả và căn cứ áp dụng của từng lần. Lượt hiện tại chỉ ghi định hướng và điều chỉnh tri thức; chưa xây UI so sánh hay cơ chế theo dõi, kiểm tra và kích hoạt tự động.

## Quan hệ với mã hiện tại

- Các giới hạn catalog lương tháng/bảo hiểm nhập tay trong ADR 0014 và ADR 0016 mô tả adapter hiện tại; chúng có thể được mở rộng bằng tác vụ riêng, với kiểm thử và migration phù hợp.
- References hiện chỉ chấp nhận bộ tham khảo Core 2025–2026. Đây không phải đường nhập tùy ý quy định mới.
- Cổng Verified/Official hiện ràng buộc nguồn vbpl.vn và metadata kỹ thuật. Cần thiết kế riêng cách biểu diễn kiểm tra tự động và nguồn chính thức được hỗ trợ; quyết định này không tự đổi nghĩa trạng thái hay bỏ cổng hiện tại.
- Cadence tìm nguồn, lưu trữ bằng chứng, cơ chế kiểm tra độc lập, sửa bộ quy tắc cho cùng kỳ và cách xác định kết quả bị ảnh hưởng cần được lập kế hoạch triển khai riêng. Giữ UI so sánh thành giai đoạn riêng sau phần dữ liệu.
