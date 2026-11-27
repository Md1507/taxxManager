# UI Đ1–Đ2 trên các luồng hiện có

Ngày 28/11/2026. Thực hiện trực tiếp theo lựa chọn của người dùng, sau khi phiếu chặng 3 `01M8DJDSFX7JJBRP9YZPVSG8YG` đã done. Áp dụng frontend-design, ui-ux-pro-max và ASP.NET Core; QA bằng Orca. Không thay service tính/lưu, schema hay dữ liệu vận hành.

## Phần đã triển khai

- Layout/sidebar dùng chung, Inter tự host kèm OFL, tokens theo đúng palette của plan. Giữ `/`, `/Forecast`, `/ForecastDelete`, `/ForecastAnnualHistory`.
- Danh sách hồ sơ có bản lưu và chi tiết trường/tháng còn thiếu; không dựng ngày lưu hoặc tên hồ sơ mà backend chưa cung cấp. Xác nhận xóa nêu ID, revision và phạm vi giữ lịch sử.
- Form giai đoạn, trạng thái hiển thị cạnh phần nhập, metadata mở rộng, một nút lưu chính; editor tháng vẫn lưu chung hồ sơ. Giai đoạn chồng tháng mới bị chặn trước preview/save, giữ nguyên giá trị và chỉ ra khoảng cần sửa. Hồ sơ cũ có chồng khoảng vẫn mở và tính từ bản đã lưu được; muốn lưu sửa cần chỉnh khoảng.
- Bảng 12 tháng gồm gross/bảo hiểm/phụ thuộc/khấu trừ. Badge từng giá trị có chữ, SVG và kiểu viền; nguồn/ghi chú/ngày nhập mở tại tháng. Bảng cuộn riêng, cố định cột tháng và tiêu đề; thiếu vẫn khác 0.
- Kết quả năm dùng một khung brand, độ phủ nghĩa vụ/khấu trừ riêng, các bước/bậc/căn cứ và lịch sử snapshot. Thiếu khấu trừ vẫn có nghĩa vụ, không dựng chênh lệch. Đầu vào sửa sau khi tính được ghi rõ là chưa lưu/kết quả cũ.
- Chú thích gross/net ngay ở phần nhập lương. Chưa thêm tính ngược gross/net.
- Theo phản hồi trực tiếp: bỏ vạch ngang ở disclosure, thanh trên, footer và danh sách tổng hợp; bỏ luôn viền trang trí ở chú thích. Giữ viền điều khiển, bảng, lỗi và focus. Quy tắc được ghi tại UI plan mục 10.2.
- Tiền tự format **trong sự kiện input**, không chờ blur. `12344556` thành `12.344.556`, số lẻ dùng dấu phẩy. Không tính qua JS Number. Form định dạng gửi `AmountFormat=vi-VN`; server dùng grammar chấm nghìn/phẩy lẻ tường minh. Giao thức cũ không marker vẫn giữ chấm/phẩy thập phân. Xử lý cả 36 trường tiền ẩn của tháng chưa mở để không đổi nghĩa dữ liệu cũ.

## Kiểm chứng

`bint test affected --run` chọn phạm vi graph, chạy Web và bộ test Web: **109/109 đạt**, mã thoát 0. Log cuối: `%TEMP%/taxmanager-ui-affected-complete.log`. Không chạy lại cổng Core/Data/EF của chặng 3 vì không đổi mã các module này hoặc schema; kết quả 629 test/EF trước đó là bằng chứng baseline, không phải lượt test UI này.

Thêm 8 ca HTTP cho định dạng vi-VN, lưu decimal chính xác, phân biệt `1.234` theo giao thức tường minh, thiếu khác 0, nhóm sai không ghi dữ liệu và công cụ tháng dùng cùng grammar. Cập nhật các probe HTML theo nhãn mới, ký hiệu `đ`, giờ Việt Nam, cột/bố cục mới. Giữ kiểm tra payload, metadata/ngày nhập, audit/mã hóa, snapshot, thiếu/0 và các oracle tiền độc lập. Test giai đoạn chồng đổi sang yêu cầu sửa đúng theo plan, kiểm cả không lưu được dữ liệu lỗi.

QA app Development/SyntheticDemo tại loopback từ bản build sao chép dưới TEMP:

- Gõ thật bằng bàn phím `12344556`: ô đang focus hiển thị `12.344.556`; nhập tiếp `,50`, Enter preview, lưu/mở lại vẫn `12.344.556,50`.
- Hồ sơ mẫu có hai giai đoạn và ngoại lệ tháng 3, bảo hiểm legacy `1.234` theo giao thức cũ, tức **1,234 đồng**: trường ẩn hiển thị `1,234`, tính năm vẫn dùng đúng **1,234 đồng**, không biến thành 1.234 đồng. Số thuế giữ phần lẻ chính xác. Thiếu khấu trừ tháng 7–12 không hiện nộp thêm/nộp thừa.
- `1,234,567` sai được giữ nguyên, focus chuyển tới error summary có link đến ô lỗi. Enter preview, Space mở cách tính; focus thấy outline solid 2px. Khi rời form đang sửa, Orca ghi nhận native beforeunload dialog (tool tiếp tục điều hướng); không tuyên bố đã thử lựa chọn hủy dialog.
- Viewport 375/768/1280: không tràn ngang trang. Sau cuộn bảng 200px ngang và 100px dọc ở 375px, cột tháng cách mép vùng 1px, hàng tiêu đề cách mép trên 1px. Đã bắt và sửa lỗi min-content của sidebar làm viewport nhỏ bị giãn.
- Axe-core 4.11.0, tags WCAG2A/2AA/2.1AA: **0 vi phạm tự động** trên công cụ tháng, danh sách/form, xác nhận xóa, lịch sử; kết quả năm mở toàn bộ disclosure cũng 0. Đây không thay kiểm thủ công.
- Inter đã tải thật; kiểm cmap WOFF2 không thiếu ký tự của chuỗi tiếng Việt trong plan và `ĐẶNG THỊ THUỶ`. Hai chuỗi số `1111111111`/`8888888888` cùng rộng **103.671875px** ở Inter 16px với tabular-nums.
- Đo màu render: chữ trắng trên brand **7,65:1**, chữ chính trên nền trang **16,02:1**. Chưa thử điện thoại vật lý, screenreader hoặc zoom native 200%.

Ảnh đã mở xem lại: [gõ tiền](ui-phase-one-two/typing-format.png), [kết quả 375px](ui-phase-one-two/annual-375.png), [768px](ui-phase-one-two/annual-768.png), [1280px](ui-phase-one-two/annual-1280.png), [lỗi 375px](ui-phase-one-two/error-375.png).

## Phạm vi còn mở

Tại thời điểm chuẩn hóa các route hiện có, Đ1 còn thiếu luồng “mở hồ sơ → sửa tháng → đọc kết quả tháng của chính hồ sơ”. Phần nối này sau đó đã triển khai riêng qua service chỉ đọc, không ghép kết quả demo vào hồ sơ năm; xem [contract và kiểm chứng màn tháng](2026-11-28-forecast-month.md). Đ1 còn nghiệm thu UX thủ công đầy đủ. Tên hồ sơ/lần lưu có ngày cần hợp đồng riêng. Đ2 chỉ tái dùng số người phụ thuộc theo giai đoạn; thông tin từng người/đăng ký vẫn là mở rộng riêng như plan.

Đ3–Đ6 và chặng nghiệp vụ 4–6 chưa triển khai. Không xem QA synthetic này là chấp thuận dữ liệu thật hoặc triển khai qua mạng.

Đề xuất tri thức mới `01M8F49W9Q99372V01ZZBZV6NK` ở trạng thái candidate, origin agent; chưa tự duyệt/publish.
