# Đ6h: xác minh email và khôi phục mật khẩu

29/11/2026. Đã triển khai và kiểm chứng kỹ thuật trong phạm vi Development + SyntheticDemo + loopback/HTTPS. Không gửi email thật, không có API key/sender thật và không mở app qua mạng. Review trong phiên triển khai, chưa phải review bảo mật độc lập.

## Kết quả

- Bốn màn: `/tai-khoan/email`, `/tai-khoan/xac-minh-email`, `/tai-khoan/quen-mat-khau`, `/tai-khoan/dat-lai-mat-khau`. Nút quên mật khẩu và quản lý email chỉ xuất hiện khi tính năng được cấu hình/bật đầy đủ; mặc định tắt.
- Thêm/đổi/gỡ email cần mật khẩu hiện tại hoặc Google đúng chủ, đúng mục đích `email`, hạn 5 phút và dùng một lần. Không lấy email Google làm email khôi phục tự động. Email cũ còn hoạt động cho tới khi xác minh mới thành công.
- GET từ thư không ghi dữ liệu; POST có CSRF mới xác minh/reset. Identity token ghim user/mục đích/security stamp, tối đa 30 phút; request nonce bền trong registry chặn thư cũ/replay. Gate tài khoản và transaction giữ đúng một lần thành công khi gửi đồng thời, không có hai chủ cùng xác minh một email.
- Reset thu hồi phiên cũ, gỡ lockout, giữ kho/liên kết Google. Google-only có thể tạo mật khẩu riêng đầu tiên qua email đã xác minh. Tài khoản đang xóa không reset được. Gỡ email yêu cầu xác nhận và vô hiệu hóa liên kết reset cũ.
- Bộ gửi Resend dùng endpoint HTTPS cố định, không redirect, timeout 15 giây, chỉ text. Lỗi upstream không mang body/key/link vào exception. Phản hồi yêu cầu reset có cùng nội dung chung khi email không tồn tại/chưa xác minh/trùng hoặc gửi thư lỗi; không hiển thị mã/link trong UI. Chưa nghiệm thu khả năng chống suy đoán tài khoản qua thời gian phản hồi cho môi trường công khai.
- Không thêm schema/migration. Dùng Identity Email/UserTokens hiện có, metadata không chứa token thư. Export cho phép email/trạng thái xác minh/email đang chờ; không nonce/token. Thông báo dùng dữ liệu thử v5 giữ claim cũ và mô tả email tùy chọn, không suy đồng ý gửi thư từ Google.

Cấu hình và cách bật gửi thật ở `docs/design/account-sign-in-options.md`. Các khóa dùng Secret Manager hoặc biến môi trường, không nằm trong repository.

## Kiểm chứng

Lượt hẹp: 50 test email/sender/Google qua. Sau đó thêm ca hai tài khoản đồng thời xác minh cùng địa chỉ, được kiểm trong lượt đầy đủ:

```text
powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/dev/verify-web-mvp.ps1
Core: 209 passed
Data: 332 passed
Web: 260 passed
Total: 801 passed, 0 failed, 0 skipped
EF: No changes have been made to the model since the last migration.
```

Build không lỗi, còn hai cảnh báo xUnit2031 có sẵn trong ForecastPersistenceBlackBoxTests dòng 241/243. Log local: `%TEMP%/taxmanager-email-test.log`, `%TEMP%/taxmanager-email-full.log`.

`EmailRecoveryHttpTests` kiểm CSRF/credential, pending không reset được, GET không ghi, đổi stamp/thu hồi cookie, lockout, thiếu/sai/hết hạn/thay thế/dùng lại token, password ngắn/khoảng trắng/không khớp, email trùng legacy, đổi/hủy/gỡ, lỗi gửi thư, cooldown, export allowlist và host restart. `GoogleSignInHttpTests` bổ sung proof email đúng mục đích/tiêu thụ và Google-only tạo mật khẩu riêng; OAuth handler thật, backchannel giả. `AccountEmailSenderTests` kiểm request endpoint/header/payload và lỗi/redirect/transport, không có yêu cầu mạng thật tới Resend.

`bint test affected --run` không chạy test: bgraph mở rộng tới full do ảnh/audit/JSON ngoài project chưa có ánh xạ, `eval.test_command` chưa được cấu hình. Đã đọc `selection.widening` từ `--json`, giữ nguyên cấu hình và dùng cổng đầy đủ ở trên theo [k:01M8F671GP7FD4E6G4CVEW6E7R]. Không báo affected đã qua.

## Giao diện

Giữ palette xanh, Inter, panel trắng, không divider/dropdown/panel lồng. Mỗi trạng thái một primary; hướng dẫn cạnh ô, icon mắt và trạng thái trực tiếp tái dùng từ đăng ký. Email có icon phong bì trong trang tài khoản. Không thay bản thiết kế tổng thể đang chờ duyệt ở phiên khác.

Orca kiểm markup trả về từ host HTTPS thực tế, dùng HTTP mirror để render vì webview không tin chứng chỉ test; form mirror vô hiệu hóa submit. Luồng HTTP/CSRF/cookie do integration test HTTPS kiểm, không lấy mirror làm bằng chứng end-to-end HTTPS trong trình duyệt.

14 trạng thái × 320/1280 px = 28 lượt: axe WCAG 2 A/AA, 2.1 AA, 2.2 AA có 0 violations/0 incomplete; không tràn ngang, một primary và không trùng tham chiếu mô tả lỗi. Kết quả ở `2026-11-29-email-ux-checks.json`. Ảnh trong `images/2026-11-29-email-recovery/`, đã xem lại màn quản lý desktop, đã xác minh mobile, reset desktop và lỗi mobile. Trạng thái Google email được kiểm HTTP; chưa nằm trong ma trận 28 ảnh này.

Nhập trực tiếp bằng Orca trên màn reset: hai ô hợp lệ báo “Đạt điều kiện mật khẩu” và “Hai mật khẩu đã khớp”; icon mắt đổi type sang text và aria-pressed=true. Tab tiếp tới ConfirmPassword với outline solid 2px. Không ghi lại giá trị mật khẩu vào báo cáo/ảnh. Chưa kiểm screenreader, password manager thực, native zoom hoặc thiết bị vật lý.

Đã chạy Hallmark sloplint với `--render`. Native render không chạy vì thiếu puppeteer-core; dùng Orca/axe ở trên. Lượt CSS ghi gate 56: `.sidebar` và `thead th` có top:0 ở hai vùng cuộn độc lập, không có các phần tử đó trong account shell. Lượt HTML + CSS ghi thêm 26a/34/26b vì đọc HTML không resolve CSS ngoài; stylesheet có focus-visible, overflow-x:clip cho html/body, active/disabled và đã đo focus thực tế. Gate 19 bắt khoảng số “8–128”, đây là điều kiện mật khẩu, không phải dấu chia trang. F23 thiếu meta description trên trang khôi phục riêng tư: không thêm nội dung nhạy cảm vào preview. Đây là đối chiếu thủ công từng finding; không gọi lint tự động đã sạch.

## Giới hạn còn lại

Host development đang dùng Data Protection ephemeral: restart làm liên kết cũ vô hiệu ngay, tối đa 30 phút chỉ áp dụng trong cùng vòng đời host. Tài khoản/email/metadata/kho tồn tại qua host mới cùng root, đã kiểm theo [k:01M8CBFTQHZAEP0A2M9H7GWSHN]. Yêu cầu thư mới sau cooldown; không tạo lại kho hoặc key để vượt lỗi. Đã ghi bài học chờ duyệt `01M8HRM0K9NPGMBQDE1GB4443K`, không tự publish.

Cần sender/domain/API key và trình duyệt HTTPS hợp lệ để nghiệm thu email thật; Google thật vẫn cần OAuth client riêng. Persist key ring, review bảo mật độc lập, điều kiện dữ liệu thật/qua mạng, passkey và mã dự phòng vẫn mở. Chặng này không nhận là hoàn tất toàn bộ Đ6.
