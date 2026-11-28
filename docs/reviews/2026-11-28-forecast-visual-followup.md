# Chỉnh visual theo phản hồi 28/11

Giữ palette, giọng văn và nghiệp vụ. Tiếp nối đối chiếu nguồn và Đ6a; không đổi parser tiền, dữ liệu, snapshot, guard lịch sử hoặc submit mặc định của forecast.

## Chênh lệch nguồn

Dương dùng token success `#246647`, âm dùng error `#B02A37`, 0 giữ text `#1A1F1B`; chỉ tô số ở cả desktop/mobile. Giữ dấu và câu cao hơn/thấp hơn/bằng nhau, không dùng màu để kết luận hoàn/nộp thêm hoặc nguồn sai. Trình duyệt kiểm +1.500.000 ở hai bản render đều computed rgb(36,102,71), không tràn 375 px. Tương phản với trắng của các token được đối chiếu ở lượt xác nhận cuối.

## Bộ chọn tháng

Ảnh người dùng có dropdown toàn chiều rộng, nút chọn và 12 nút dài dính sát nhau. Thay bằng lưới 12 tháng, 6 cột desktop/3 cột mobile, nhãn ngắn và khoảng cách đều. Có chữ Đã nhập/Chưa nhập/Đang sửa và aria-current cho tháng hiện tại. Không biến status này thành đã đủ dữ liệu hoặc đã lưu. Dropdown/nút cũ giữ trong fallback chỉ hiện khi JavaScript tắt; select ẩn vẫn gửi SelectedMonth để giữ hợp đồng POST. Preview đứng trước mọi submit khác `[k:01M8G33DP7ZSB0MXMQ9PV1A68A]`.

QA actual app: 1440 px có 12 nút/6 cột, fallback ẩn; chọn tháng 3 POST về đúng handler/fragment, SelectedMonth và EditingMonth bằng 3, gross 30.000.000 giữ nguyên. 320/375 px có 3 cột, không tràn body, một primary; đã xem ảnh `8e663569-e0c9-487b-8c91-fc10c2417b5b` desktop và `b339a1e9-da2f-43c5-892c-bb9b85071598` 320 px. Không lưu thay đổi profile chỉ để kiểm visual.

## Các liên kết cuối hồ sơ

Theo ảnh tiếp theo: gom hai tiện ích vào một cụm nền trắng, có icon và mô tả ngắn; không gạch chân thường trực, không thêm mũi tên. Lịch sử thành hàng tiêu đề/action riêng, có mô tả rỗng hoặc số kết quả và danh sách có cấu trúc. Xóa hồ sơ đặt cuối, chữ phụ và nhấn đỏ khi tương tác; vẫn mở màn xác nhận, không xóa ngay. Giữ route/revision/record, một primary Lưu và thanh hành động hiện có.

Hallmark sweep `--scope app --render`: static 56/37/F7 giữ giải thích tại báo cáo reconciliation; native render không chạy do thiếu puppeteer-core. Log `%TEMP%/taxmanager-accounts-month-hallmark.json`. Không tuyên bố lint 0 FAIL hoặc kiểm screenreader đã xong. Ảnh/kiểm chứng mã cuối bổ sung sau khi xác nhận.

## Xác nhận sau chỉnh

Đã xem cụm tiện ích cuối hồ sơ ở 1440 px (`ccb60dea-daad-4329-9315-c007248ec879`) và 320 px (`5ec9bc05-efe5-4c09-8fac-3a37742371cc`): hai route có đúng revision, một primary Lưu, không tràn body. Ảnh trong `%TEMP%/orca-browser-screenshots/`, có thể hết hạn. Bộ chọn tháng giữ các kết quả QA ở trên.

Actual app đối chiếu: nguồn gross 0 so với hồ sơ 360.000.000 cho `−360.000.000 đ`, computed `rgb(176,42,55)`; nguồn khấu trừ 0 so với hồ sơ 0 cho `0 đ`, không class dương/âm, computed `rgb(26,31,27)`. Ca dương +1.500.000 đã kiểm trước đó. Tương phản trên trắng: success 6,85:1; error 6,50:1; chữ phụ `#526057` 6,63:1; brand 7,65:1. Đều vượt 4,5:1; không dùng riêng màu làm thông tin.

Form tài khoản và ô lỗi được chỉnh theo các phản hồi tiếp theo; xem báo cáo `2026-11-28-local-accounts.md`. Hallmark sweep sau icon: `%TEMP%/taxmanager-password-icon-hallmark.log`, còn static 56/F7 đã đối chiếu, native renderer vẫn thiếu dependency. `git diff --check` không có lỗi whitespace; các cảnh báo LF/CRLF không phải thay đổi migration.

Kiểm chứng mã cuối: `scripts/dev/verify-web-mvp.ps1` exit 0, **699/699** (209 Core + 328 Data + 162 Web), EF không pending model changes. Log `%TEMP%/taxmanager-password-icon-full.log`. Affected không chạy do widening full/thiếu eval.test_command, đã dùng full gate theo [k:01M8F671GP7FD4E6G4CVEW6E7R], giữ nguyên cấu hình.
