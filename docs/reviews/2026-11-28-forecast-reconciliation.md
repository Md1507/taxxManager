# Chặng 6 — đối chiếu nguồn nhập tay

28/11/2026, local synthetic. [Contract và bố cục trước code](../design/forecast-reconciliation.md). Tiếp nối chặng 5, giữ mọi thay đổi đang có; không sửa Core tính thuế, migration, catalog, guard lịch sử hoặc các blackbox test cũ.

## Hành vi

- Mở từ Tổng quan hoặc sidebar của hồ sơ đã lưu. GET không có khoản nguồn hoặc trạng thái mặc định; năm theo hồ sơ, người dùng nhập kỳ/tháng và chọn chỉ tiêu. POST so đúng revision, kỳ và nghĩa của gross/bảo hiểm/khấu trừ; không dùng nghĩa vụ thuế để thay khấu trừ.
- Đọc document/snapshot nhất quán và xác thực protection cả lookup rỗng [k:01M85QGMTJVNX37D5XYH5161KM]. Không cần catalog hoặc luật mới vì chỉ đối chiếu các đầu vào đã nhập. Không tạo audit/revision/lịch sử.
- Nguồn/tài liệu/trạng thái/ghi chú và thời điểm server hiện riêng với provenance từng tháng hồ sơ. Nguồn không quyết định trạng thái; giả định cần giải thích. Tiền vi-VN dùng binder/JS hiện có [k:01M8F49W9Q99372V01ZZBZV6NK]. Trống khác 0, sai giữ input/lỗi.
- Nếu thiếu tháng của chỉ tiêu đang so, không hiện tổng một phần như tổng kỳ và không có delta. Khác năm cũng chưa so được. Delta có dấu, hướng và công thức nguồn trừ hồ sơ; không kết luận nguồn sai hoặc tiền hoàn/nộp thêm.
- Một primary, thanh hành động sticky cuối form, focus kết quả sau POST; sửa input/select hiện cảnh báo kết quả cũ. Sidebar thêm route riêng sau báo cáo, giữ section link và default submit editor [k:01M8G447XSG9YDAZ5Y8QZDZJ97] [k:01M8G33DP7ZSB0MXMQ9PV1A68A].

## Kiểm chứng

- 8/8 HTTP test mới qua. Tổng khấu trừ năm 18 triệu so nguồn 18,5 triệu → +500.000; tháng 6 1,5 triệu so nguồn 1 triệu → −500.000; bảo hiểm tháng 2–3 bằng 6 triệu → 0. Gross năm 360 triệu giữ nguyên dù có thưởng đã gồm 5 triệu và ngoài gross 10 triệu. Có ca tháng thiếu, zero, thiếu trường khác không chặn chỉ tiêu đang so, khác năm, metadata/XSS/clock, validation, CSRF/revision/xóa, sentinel/cipher hỏng và bảo toàn payload/snapshot/audit/lịch sử.
- Lượt đầu helper test lỗi trước POST do nhiều hidden `__Invariant` bị đưa vào Dictionary. Chỉ sửa helper test mới để bỏ marker culture trong probe chỉ gửi tháng/năm số nguyên; tiền giữ marker vi-VN. Không đổi markup sản phẩm hoặc assertion tiền. Đã ghi warning candidate `01M8GMBSMX6WPM31BTNS3EHCCN`; xác nhận origin agent, không có người duyệt. CLI ngoài Bint mặc định human đã được hiệu chỉnh ngay trên mục vừa tạo thành candidate trước khi tiếp tục.
- `bint test affected --run` không thực thi do widening full, thiếu `eval.test_command`; selection JSON xác nhận hai metadata Hallmark `unknown_file`. Giữ cấu hình, chạy script full trực tiếp theo [k:01M8F671GP7FD4E6G4CVEW6E7R]. Log `%TEMP%/taxmanager-reconciliation-affected.log`, selection `%TEMP%/taxmanager-reconciliation-selection.json`; affected không được coi là qua.
- Cổng cuối sau cả sửa căn lề 320 px và bố cục kết quả mobile: `powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/dev/verify-web-mvp.ps1`, **exit 0**, Core 209 + Data 328 + Web 151 = **688/688**, không bỏ qua; EF không có pending model changes. Build không lỗi, còn hai cảnh báo xUnit2031 cũ tại ForecastPersistenceBlackBoxTests dòng 241/243. Log `%TEMP%/taxmanager-reconciliation-full-gate-final.log`. `node --check workspace.js` và `git diff --check` qua. Các lượt full trước đã qua nhưng vẫn chạy lại vì có sửa bố cục sản phẩm sau đó; không dùng kết quả cũ thay kiểm mã cuối.

## QA trình duyệt và Hallmark finish

Orca dùng kho synthetic hiện có, không sửa số của các hồ sơ QA cũ. Route thử `/ho-so/cc95ac70-98ee-4923-8604-82267d230438/doi-chieu`, revision 1: khấu trừ hồ sơ bằng 0 đã nhập đủ 12 tháng, số nguồn 1.500.000 đ cho chênh lệch +1.500.000 đ và hướng nguồn cao hơn. Không coi 0 hồ sơ là thiếu.

- Đã thấy gõ `1500000` thành `1.500.000` và Enter gửi form/focus vào `reconciliation-heading`. POST rỗng focus `error-summary`; các lỗi có link và aria-invalid. Đổi select chỉ tiêu hiện cảnh báo kết quả cũ, so lại ẩn cảnh báo. Ở một lượt lặp cuối Orca type báo thành công nhưng không thay input; đã resnapshot và dùng fill rồi click, không tính lượt đó là bằng chứng gõ thành công. Nghiệm thu focus cửa sổ thực tế vẫn mở.
- Đo 320/375/768/1024/1280/1440/1920 px: không tràn body, đúng một primary, sidebar top 0 khi cuộn. Đã sửa lệch ô do nhãn wrap ở 320; xác nhận lại ba ô năm/tháng cùng y ở 320 và 1440. Bản cuối kiểm lại 320/375/1440: main result hiển thị đủ ba số bằng definition list ở mobile, bảng ngang ở desktop. Chỉ bảng provenance chi tiết cần cuộn riêng.
- Axe 4.11.0, bộ WCAG 2 A/AA + 2.1 AA trên trạng thái lỗi và kết quả desktop/mobile: **0 violations, 1 incomplete color-contrast tại #Note**. Tool báo bị che một phần do sticky action khi đo; không tuyên bố 0 incomplete. Đối chiếu computed styles: chữ ghi chú #1A1F1B trên trắng **16,72:1**; chữ phụ #526057 đạt **6,09:1** trên nền và **6,63:1** trên panel. Focus ô Note ở 320 px cho rect bottom 688,5 < actionbar top 727, đã xem ảnh xác nhận không bị che khi focus. Đây là kiểm thủ công bổ sung, không thay screenreader/native zoom.
- Ảnh đã xem: form mobile `ea5ad735-5809-4168-bbf9-4bfb2293f2ea`, focus Note `9329ab8f-e638-453e-b438-cef87a8a3799`; kết quả cuối 375 px `8793fcd4-da01-4784-8931-b09b07f3a022`, 320 px `059fff41-93c1-4722-83c4-3fcbd148fa6c`, desktop `4258c27c-0ebd-46cf-b32c-c0f4956125de`. PNG trong `%TEMP%/orca-browser-screenshots/` với hậu tố `-screenshot.png`, là ảnh tạm có thể hết hạn. Ảnh bảng mobile trước khi đổi bố cục không dùng làm bằng chứng bản cuối.

Finish review trong phiên Codex theo nhánh harness Hallmark, không có reviewer độc lập. Sau ảnh mobile đầu, bỏ nhu cầu cuộn ngang để tìm chênh lệch chính; giữ main result trong cùng một panel, không tạo card cho từng số. Trạng thái và hướng chênh lệch dùng chữ, không chỉ màu; không ảnh/biểu đồ trang trí hoặc dữ liệu chứng thực bịa.

Sloplint chạy `--scope app --genre modern-minimal --render --url` trên route này; native render không chạy vì thiếu puppeteer-core, dùng ảnh Orca ở trên. Log `%TEMP%/taxmanager-reconciliation-hallmark-final.json`. Static tool còn 1 FAIL / 1 REFLEX / 1 WARN đã đối chiếu, không báo tool trả sạch:

- Gate 56: sidebar và thead top 0 thuộc cột/scrollport riêng; không có hai banner chồng cùng vùng, không thêm offset sai cho bảng.
- Gate 37: regex đếm font-size token như font-family. Thực tế Inter/system fallback, không phải 10 họ chữ.
- F7: recipe chung input/select/button/textarea giữ hệ thống điều khiển; primary có nền đặc, input viền mảnh, liên kết trở lại là chữ. Vai trò khác nhau, không thêm biến thể viền/radius trang trí để làm im tool.

## Thiết kế và giới hạn

Áp dụng Hallmark app screen, frontend-design, ui-ux-pro-max trong hệ thống Inter/xanh đã khóa. Giữ bảng so trực tiếp trên desktop và ba hàng trên mobile, provenance qua một disclosure, không tạo card từng chỉ tiêu hoặc biểu đồ trang trí. Một native select cho chỉ tiêu; nguồn/trạng thái là radio. Đã sửa cụm năm/tháng cùng chiều rộng và bỏ padding fieldset thừa sau ảnh đầu.

Chỉ một nguồn nhập mỗi phép so, không lưu số đối chiếu để mở lại và chưa đối chiếu báo cáo nguồn nhiều nơi trả. Không thu file gốc, thông tin đăng nhập hoặc gọi eTax. Nghiệm thu UX bằng người dùng, screenreader và zoom native còn mở; không đóng toàn bộ UI Đ5 chỉ bằng test/axe.

## Bổ sung luồng sửa số và bàn phím

- `ForecastReconciliation.cshtml:19`: summary lỗi có tên truy cập qua heading, sắp lỗi theo thứ tự nguồn → tài liệu → năm/tháng → chỉ tiêu → tiền → trạng thái → ghi chú. POST rỗng đã kiểm thứ tự link; năm hợp lệ không bị liệt kê.
- `ForecastReconciliation.cshtml:26` và `:63`: thêm đích focus cho đầu form và liên kết “Sửa số nguồn” ngay ở kết quả. Fragment trong cùng trang giữ số vừa nhập; CSS có scroll-margin cho hai heading form/kết quả. Không thêm nút đặc hoặc JavaScript điều hướng.
- QA riêng trên trang Orca `029c90cc-292d-4d45-a070-8a2f8c2db411`: POST hợp lệ với khấu trừ hồ sơ 0 và nguồn 1.500.000 → +1.500.000. Ở 375 px, focus liên kết rồi Enter đổi fragment thành `#reconciliation-input`, activeElement đúng heading; Tab tới radio `Source`, `document.hasFocus()` và `:focus-visible` đều true. Giữ Company, tên tài liệu, năm 2026, tháng 1–12, Withheld, 1.500.000 và Actual. Đây là bằng chứng cho đoạn thao tác cụ thể, không thay nghiệm thu toàn bộ form bằng bàn phím.
- Kiểm lại 320/375 px: đích heading ở y=108 dưới thanh đầu trang, một primary, không tràn body. Đã xem ảnh 375 px có viền focus radio `efe7a6c2-8c3d-46fa-b79c-aff584a29c01`, ảnh 320 px `b0505913-7b13-4b4c-b478-a27c0d7220d8` trong `%TEMP%/orca-browser-screenshots/`.
- Áp dụng thêm web-design-guidelines cho luồng focus/lỗi/anchor; giữ giọng văn tiếng Việt và parser tiền tường minh của dự án khi khác khuyến nghị chung về Title Case hoặc Intl.
- 8/8 test HTTP đối chiếu qua, log `%TEMP%/taxmanager-reconciliation-keyboard-focused.log`. `bint test affected --run` vẫn không thực thi vì metadata Hallmark chưa được ánh xạ và thiếu `eval.test_command`; log `taxmanager-reconciliation-keyboard-affected.log`, selection `taxmanager-reconciliation-keyboard-selection.json` dưới TEMP. Không coi affected là qua.
- Cổng đầy đủ sau sửa: `scripts/dev/verify-web-mvp.ps1` **exit 0**, **688/688** (Core 209, Data 328, Web 151), EF không có pending model changes. Log `%TEMP%/taxmanager-reconciliation-keyboard-full.log`. `git diff --check` qua.
- Hallmark chạy lại với `--render`: native render vẫn thiếu puppeteer-core; static còn đúng 56/37/F7 đã giải thích phía trên, không tuyên bố lint sạch. Log `%TEMP%/taxmanager-reconciliation-keyboard-hallmark.json`. Finish review trong phiên Codex qua ảnh thực tế; không có reviewer độc lập.
