# Thuế TNCN từ tiền lương, tiền công

Thuật ngữ của dự án trong phạm vi thuế TNCN từ tiền lương, tiền công và các khoản liên quan.

## Language

**Quy định áp dụng**:
Quy định thuế phù hợp với kỳ thuế và trường hợp đang tính, xét cả hiệu lực, sửa đổi và điều khoản chuyển tiếp.
_Avoid_: Quy định mới nhất (khi chỉ nói về ngày xuất bản)

**Bộ quy tắc thuế**:
Một phiên bản các quy tắc dùng để tính thuế trong phạm vi và kỳ áp dụng xác định, kèm căn cứ văn bản tương ứng.
_Avoid_: Luật hiện tại (khi không xác định phiên bản và kỳ áp dụng)

**Đề xuất cập nhật thuế**:
Thay đổi bộ quy tắc do AI tìm nguồn, phân tích và đề xuất, kèm bằng chứng về nội dung và kỳ áp dụng.
_Avoid_: Quy định đã được xác minh (khi mới có đề xuất)

**Kiểm tra tự động căn cứ thuế**:
Quá trình AI đối chiếu nội dung, đối tượng và kỳ áp dụng với văn bản nguồn, kết hợp kiểm tra chéo độc lập và kiểm thử phép tính. Kết quả thể hiện mức kiểm tra tự động đã hoàn tất, không phải xác nhận của người thẩm định pháp lý.
_Avoid_: Thẩm định pháp lý bởi con người

**Kích hoạt bộ quy tắc**:
Việc đưa một phiên bản bộ quy tắc đã vượt các điều kiện kiểm tra tự động vào sử dụng cho phạm vi và kỳ áp dụng xác định.
_Avoid_: Áp dụng ngay khi tìm thấy văn bản mới

**Tính lại**:
Một lần tính mới cho kỳ đã có kết quả, tạo bản ghi riêng để đối chiếu với kết quả và căn cứ của lần trước.
_Avoid_: Ghi đè kết quả cũ

**Đối chiếu số liệu thuế**:
So sánh số liệu từ các nguồn cho cùng cá nhân, kỳ, phạm vi thu nhập và chỉ tiêu, kèm thời điểm ghi nhận của từng nguồn. Đối chiếu phát hiện chênh lệch, không tự xác định nguồn nào đúng pháp luật.
_Avoid_: Xác nhận công ty hoặc cơ quan thuế tính sai

**Chênh lệch đối chiếu**:
Phần khác nhau giữa hai số liệu có cùng ý nghĩa và phạm vi so sánh. Chênh lệch chưa được giải thích không đồng nghĩa với sai sót, số thuế phải nộp thêm hay quyền được hoàn thuế.
_Avoid_: Tiền được hoàn (khi chỉ có chênh lệch giữa hai nguồn)

**Số liệu dự kiến**:
Số liệu người dùng chủ động nhập theo kế hoạch cho phần chưa phát sinh. Kết quả sử dụng số liệu này là dự báo, có thể thay đổi khi có dữ liệu thực tế.
_Avoid_: Số liệu thực tế (khi chỉ sao chép từ lương thường kỳ hoặc năm trước)

**Số liệu thực tế**:
Số liệu người dùng ghi nhận cho khoản đã phát sinh, kèm nguồn và kỳ tương ứng. Trạng thái này không xác nhận chứng từ, cách phân loại thu nhập hoặc việc áp dụng pháp luật đã được thẩm định.
_Avoid_: Số liệu đã xác minh pháp lý

**Số liệu giả định**:
Số liệu ứng dụng điền hoặc suy ra để hoàn thiện một kịch bản, kèm cách hình thành giả định. Số chưa biết chưa có giá trị thay thế không phải là số 0.
_Avoid_: Số liệu dự kiến do người dùng nhập

**Kịch bản dự báo thuế năm**:
Một tập dữ liệu thực tế, dự kiến và giả định dùng để ước tính thuế cho một năm trong phạm vi được hỗ trợ. Kết quả phụ thuộc các đầu vào của kịch bản, không tự bảo đảm nghĩa vụ cuối cùng hoặc quyền được hoàn thuế.
_Avoid_: Kết quả quyết toán chính thức

**Hồ sơ thuế cá nhân**:
Tập số liệu người dùng lưu cho các kỳ thuế, kèm nguồn, trạng thái, ghi chú và thời điểm ghi nhận. Việc người dùng tự giữ hồ sơ không tự làm các nguồn trở nên độc lập hoặc được xác minh.
_Avoid_: Hồ sơ đã được kiểm chứng độc lập
