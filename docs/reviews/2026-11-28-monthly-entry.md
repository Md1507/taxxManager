# Nhập nhanh công cụ tính tháng

28/11/2026. Phạm vi `/`, theo ảnh phản hồi nhiều form lặp. [Bố cục và contract](../design/monthly-entry-redesign.md). Áp dụng frontend-design, ui-ux-pro-max và Hallmark trong hệ xanh/Inter hiện có; không chọn theme mới.

## Thay đổi

Nhập gross, bảo hiểm và khấu trừ chung cho khoảng tháng; xem số ô sắp đổi và số ô có dữ liệu trước khi áp dụng. Ô chung trống giữ nguyên từng tháng, 0 là số đã nhập. Có hoàn tác lần áp dụng gần nhất, giữ ô đã sửa riêng. Mỗi lần chỉ mở một tháng; lưới tháng thể hiện số ô đã nhập, không xác nhận số liệu đúng. Một primary tính cả 12 tháng ở thanh hành động dính cuối form. Chú thích gross/net vẫn còn dưới form; năm gọn, hướng dẫn định dạng ngay dưới nhóm ô tiền.

Giữ cả 36 input enabled, đúng thứ tự POST gross/insurance/withheld. Không thêm trường có name từ tiện ích chung vào contract server. Định dạng xử lý cả tháng ẩn, giữ `AmountFormat=vi-VN`, không đưa tiền qua JavaScript Number [k:01M8F49W9Q99372V01ZZBZV6NK]. Giữ legend ngoài div.form-grid [k:01M8G8KGHF2YMZ0JMY6RZ1ZMV7]. Tắt JavaScript vẫn có 12 nhóm nhập nền.

## Kiểm tra trình duyệt

Orca, host synthetic riêng `http://localhost:5093/`; không dừng app đang chạy từ workspace chính.

| Thao tác | Quan sát |
|---|---|
| Gõ `30000000` bằng phím thật | Thành `30.000.000` ngay khi gõ. |
| Enter trong ô chung | Chỉ xem thay đổi, chưa đổi ô từng tháng, không POST. |
| Áp dụng 3 khoản cho 1–12 | 36 ô có số; mỗi array FormData có đúng 12 giá trị, không input disabled. |
| Sửa gross tháng 6 rồi hoàn tác | Giữ số sửa riêng, các ô còn bằng giá trị vừa áp dụng được phục hồi. |
| Chỉ gross/bảo hiểm cho 3–6 | Không chạm tháng 2; bảo hiểm 0 có giá trị, khấu trừ chung trống giữ ô riêng trống. |
| Xem thay đổi | Chưa sửa dữ liệu; thông báo số ô có sẵn sẽ bị thay. |
| Đổi thông số sau preview | Bỏ preview cũ, phải xem lại. Khoảng 0–12 bị từ chối. |
| Gửi khi thiếu bảo hiểm tháng 9 đang ẩn | Tự mở tháng 9 và focus đúng ô; console không lỗi. |
| Enter trong ô tháng với đủ số | POST thật trả đủ 12 kết quả; tháng ẩn vẫn có đúng số vi-VN. |
| Tính xong → áp dụng chung/hoàn tác | Hiện thông báo kết quả cũ cần tính lại. |
| Tính xong → nhập riêng/đổi năm | Code gắn cùng trạng thái cũ; không tự tính hay ghi snapshot mới. |
| Focus sau Áp dụng/Hoàn tác/Hủy | Về tháng đang chọn hoặc Xem thay đổi; không kẹt ở nút vừa ẩn. |

Ảnh đã mở và kiểm: [desktop 1440](images/2026-11-28-monthly-entry/desktop-1440.png), [chỉnh tháng 375](images/2026-11-28-monthly-entry/month-375.png), [nhập 320](images/2026-11-28-monthly-entry/entry-320.png). Body scrollWidth bằng viewport tại 320/375; lưới 3 cột mobile, 6 cột desktop, một tháng hiện. Full-screenshot của Orca ghép sai sticky trong lượt thử, nên bằng chứng dùng screenshot viewport. Không che lỗi bằng overflow:hidden.

axe tại màn trống 375: 0 violations, 0 incomplete cho WCAG 2 A/AA, 2.1 A/AA và 2.2 AA được engine hỗ trợ. Đây không thay kiểm screenreader hoặc zoom native 400%; hai mục đó chưa kiểm.

Hallmark finish review trong context riêng tìm một lỗi P2: kết quả trước chưa được đánh dấu cũ sau Apply. Đã sửa và xác nhận Apply/Undo hiện thông báo; kết quả POST mới ban đầu không có cảnh báo. Linter còn static FAIL 56 (sidebar và thead trong vùng cuộn bảng riêng) và WARN F7 (native controls dùng chung recipe). Đối chiếu scope, không có hai sticky top:0 trong cùng context ở màn này. Native render của sloplint không chạy vì thiếu puppeteer-core; dùng Orca và ảnh thật, không ghi lint sạch.

## Cổng kỹ thuật

Chạy `bint test affected --run`: lượt đầu Bint in selection graph và command, không có log thực thi; không tính là test qua. Sau khi thêm ảnh, selection nới tới full, `ran:false` vì thiếu `eval.test_command` [k:01M8F671GP7FD4E6G4CVEW6E7R]. Chạy script đầy đủ trên bản nguồn đối chiếu ở TEMP vì app chính khóa DLL: **721 test và EF qua, mã 0**. Kết quả cuối cùng và giới hạn môi trường ghi tại [báo cáo xuất tài khoản](2026-11-28-account-export.md).

Lượt đầu phát hiện kiểm tra cũ yêu cầu chữ “Dữ liệu giả”; giữ ngữ nghĩa này bằng link mẫu “Dữ liệu giả mẫu”, không sửa test. Chưa nghiệm thu bằng người dùng; luồng tính 12 tháng và căn cứ pháp lý giữ nguyên.
