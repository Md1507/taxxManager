# Lộ trình sản phẩm

Lộ trình này ghi thứ tự sản phẩm mới nhất đã chốt; các chặng tương lai dưới đây chưa được coi là hoàn thành. Nền 3.1 đã nghiệm thu kỹ thuật cho local với dữ liệu giả; không mở lại nghiệm thu đó. Phiếu 3.1b `01M89PFQ3ETQDTCSGPMAN0DEPW` đang thực hiện nhập liệu, UX và trạng thái theo phạm vi đã chốt của chính phiếu; lộ trình này không mở rộng phiếu đó.

Tham chiếu: [PLAN](../PLAN.md), [ADR 0018](decisions/0018-personal-annual-tax-forecast.md) và [đặc tả dự báo năm](design/annual-tax-forecast.md). Khi 3.1b được đưa về cây gốc, liên kết và đồng bộ PLAN với thứ tự và trạng thái thực tế của lộ trình này, giữ nguyên bằng chứng nghiệm thu 3.1. PLAN đang được phiếu khác cập nhật.

## Phạm vi và nguyên tắc chung

- Bản đầu dành cho cá nhân cư trú, một nơi trả lương, kỳ 2026, bảo hiểm nhập tay; dự án miễn phí, .NET, một người làm. Không đặt hạn hai tuần; hiện chưa có người dùng thử và chưa có bằng chứng xác nhận nhu cầu thị trường.
- Mỗi giá trị phân biệt thực tế, dự kiến và giả định; trạng thái dữ liệu này độc lập với trạng thái xác minh căn cứ luật. Thiếu dữ liệu khác số 0. Khi đủ đầu vào có thể tính nghĩa vụ năm, nhưng thiếu khấu trừ thì không hiện số hoàn hoặc nộp thêm giả; không hứa khoản nộp thừa ước tính chắc chắn được hoàn.
- Lưu số liệu cùng kỳ, loại chỉ tiêu, trạng thái, nguồn, ghi chú và thời điểm nhập; chưa lưu file gốc. Kết quả chỉ tham khảo, nêu phạm vi, độ đầy đủ và căn cứ/trạng thái luật được dùng; không tự nâng trạng thái xác minh hoặc dùng luật sai kỳ.
- Thử local một người bằng dữ liệu synthetic chưa cần đăng nhập. Trước khi phục vụ nhiều người qua mạng phải có đăng nhập và cách ly tài khoản; lộ trình này không cấp quyền triển khai công khai hay dùng dữ liệu thật.

## Sáu chặng theo thứ tự

### 1. Hoàn tất 3.1b: nhập liệu, UX và trạng thái

- **Bắt đầu:** phiếu `01M89PFQ3ETQDTCSGPMAN0DEPW` đang chạy trên nền 3.1; giữ nguyên phạm vi và tiêu chí của phiếu đó.
- **Tối thiểu:** hoàn tất mô hình trạng thái đầu vào, nhập theo giai đoạn và tháng ngoại lệ, cùng UX thuộc phiếu; phân biệt thiếu và 0, không âm thầm ghi đè ngoại lệ hay coi giả định là thực tế.
- **Xong khi:** phiếu 3.1b hoàn tất theo tiêu chí và kiểm chứng riêng, được đưa về cây gốc; không xem các chặng sau là xong theo đó.

### 2. Lưu và mở lại hồ sơ theo năm

- **Bắt đầu:** chặng 1 hoàn tất và contract trạng thái đầu vào đã ổn định.
- **Tối thiểu:** lưu, mở lại và cập nhật hồ sơ năm với metadata từng giá trị; bảo vệ dữ liệu và ghi audit phù hợp nền hiện có. Lưu dự báo theo contract riêng, không biến giả định thành dữ liệu thực tế; giữ snapshot lịch sử cũ khi tính lại.
- **Xong khi:** mở lại qua phiên lưu trữ mới giữ đúng số liệu và metadata; cập nhật thực tế và tính lại không sửa lịch sử cũ, có kiểm chứng bảo vệ và bảo toàn dữ liệu.

### 3. Tổng hợp và dự báo năm

- **Bắt đầu:** chặng 2 hoàn tất để dự báo dùng được hồ sơ đã lưu và mở lại.
- **Tối thiểu:** tổng hợp đầu vào theo tháng, dùng engine năm cho nghĩa vụ thuế năm ước tính; hiển thị phần thực tế/dự kiến/giả định, số tháng đủ dữ liệu thực tế, độ phủ khấu trừ riêng và phạm vi chưa bao gồm. Không thay phép tính năm bằng tổng thuế tháng.
- **Xong khi:** kết quả năm có thể giải thích từ hồ sơ và căn cứ đúng kỳ; thiếu đầu vào được báo rõ, thiếu khấu trừ không tạo chênh lệch giả; các trường hợp đủ và thiếu được kiểm chứng.

### 4. Xuất dữ liệu và checklist chuẩn bị quyết toán

- **Bắt đầu:** chặng 3 hoàn tất, có tổng hợp năm và trạng thái độ đầy đủ để xuất.
- **Tối thiểu:** xuất số liệu, nguồn/kỳ và trạng thái cần thiết cùng kết quả tham khảo; cung cấp checklist chuẩn bị quyết toán chỉ rõ phần đã có và còn thiếu. Đây không phải tờ khai chính thức.
- **Xong khi:** người dùng có thể lấy bản xuất và checklist nhất quán với hồ sơ, thấy rõ phần thiếu và giới hạn tham khảo.

### 5. Hai kịch bản đơn giản

- **Bắt đầu:** chặng 4 hoàn tất; dùng hồ sơ và phép tính năm của các chặng trước.
- **Tối thiểu:** kịch bản cơ sở và kịch bản thêm một khoản thưởng giả định chịu thuế từ nơi trả lương hiện tại, có số tiền và tháng riêng. Giữ khoản thực tế, tránh cộng trùng gross; không ghi giả định trở lại hồ sơ thực tế.
- **Xong khi:** mỗi kịch bản có kết quả riêng và có thể so sánh; hai kết quả bằng nhau không tạo khoảng giả, khoảng giữa chúng không được trình bày là bảo đảm hay xác suất.

### 6. Đối chiếu hồ sơ riêng với số người dùng cung cấp

- **Bắt đầu:** chặng 5 hoàn tất; hồ sơ, kết quả và metadata nguồn/kỳ đã sẵn sàng.
- **Tối thiểu:** cho người dùng nhập số từ công ty hoặc eTax kèm nguồn, kỳ và ghi chú, rồi đối chiếu với hồ sơ riêng theo chỉ tiêu và kỳ tương ứng; không thu mật khẩu hay lưu file gốc.
- **Xong khi:** người dùng thấy số của từng nguồn, chênh lệch và phần thiếu hoặc chưa tương ứng; kết quả không kết luận công ty hay eTax sai.

## Làm song song và việc để sau

Có thể chuẩn bị yêu cầu, thiết kế, ví dụ và rà phụ thuộc cho chặng sau trong khi chặng trước đang thực hiện. Chỉ bắt đầu **triển khai** từng chặng khi điều kiện bắt đầu ở trên đã đạt; phối hợp theo tiến độ không làm thay đổi scope của phiếu 3.1b. Mỗi chặng cần phiếu và kiểm chứng riêng theo phạm vi thực tế.

Backlog tiện ích: dán Excel, sao chép năm, Gross–Net và so sánh các lần tính; không đưa vào 3.1b. Cập nhật luật bằng AI, nhiều nơi trả lương, freelance/kinh doanh, thu nhập nước ngoài, OCR và XML là các chặng riêng cần quyết định và lập kế hoạch sau; chưa được tính là năng lực của sáu chặng này.
