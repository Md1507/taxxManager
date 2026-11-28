# Đ6a — Identity và cách ly tài khoản trong demo local

28/11/2026. [Contract trước code](../design/local-accounts.md), PLAN 3.2/UI Đ6. Giữ demo mặc định và mọi thay đổi chặng 5/6 đang có; không stage/commit. Không thay schema/migration thuế, luật hoặc test đóng băng.

## Đã triển khai

- `SyntheticAccounts=true` cùng `SyntheticDemo=true`, chỉ Development/loopback, listener HTTPS. Identity/EF 10.0.15 theo cùng pin EF hiện có; registry SQLite riêng dưới root synthetic gốc, password hash do Identity tạo. Registry đã có nhưng mất file bị từ chối, không khởi tạo lại trắng.
- Đăng ký tên 3–64 ký tự, mật khẩu 12–128 ký tự; login lỗi chung, lockout 5 lần/5 phút, giới hạn 20 lượt vào trang xác thực mỗi phút cho host local. Cookie `__Host-taxmanager.identity`, Secure/HttpOnly/SameSite Strict, session 30 phút, không sliding. Security stamp kiểm mỗi request; logout POST/CSRF, không logout qua GET; returnUrl chỉ local.
- Mỗi user có root/khóa thuế synthetic riêng do server provision. Boundary SelectedDatabase chọn từ subject đã xác thực và registry; không lấy owner/root từ URL/POST. Kho lỗi/thiếu khóa trả lỗi, không tạo kho thay thế. Các route sẵn có đều đi qua boundary này; fallback authorization chặn anonymous, chỉ login/register được AllowAnonymous.
- Selection và preview khôi phục tách theo subject + browser cookie; workspace antiforgery giữ generation và identity. Backup chỉ lấy kho thuế hiện tại, không chép registry/password hash. Dữ liệu trong kho demo cũ không tự đưa vào tài khoản mới.
- Form xác thực dùng panel trắng gọn, giữ Inter/xanh và logo hệ thống, một primary. `autocomplete=username/current-password/new-password`, cho paste; toggle Hiện/Ẩn có accessible name và pressed state. Lỗi inline + summary focus, sắp theo thứ tự ô. Không echo mật khẩu khi validation/login/duplicate thất bại.

## Kiểm chứng

11 ca HTTP mới trong `Security/LocalAccountsHttpTests.cs`: opt-in/anonymous/static; cookie/returnUrl/logout/CSRF; validation/duplicate/password echo/hash; lockout; hai tài khoản đọc và sửa chéo các route; token cũ sau đổi tài khoản; export chỉ kho hiện tại; preview/selection restore theo chủ; host mới và thiếu khóa; security stamp/cookie sửa; rate limit/HTTP; mất registry không tạo lại.

- Đọc chéo kiểm editor, tháng, lịch sử và record, báo cáo/CSV, kịch bản, đối chiếu. Gửi ID người khác qua Save/Calculate/Delete/Scenario/Reconciliation với token hợp lệ của bên gửi; payload, revision, snapshot và audit của chủ cũ không đổi. Export của B chỉ có ID B. Preview của A không được mở/accept bởi B dù cùng browser cookie; chuyển trở lại A mới accept được.
- HTTPS test dùng chứng chỉ riêng fixture và client chỉ tin đúng thumbprint. Lượt đầu mọi request EOF ở TLS vì CreateSelfSigned dùng key ephemeral trên Windows; sửa fixture export/import PFX cho Schannel, không hạ HTTPS. Đã ghi candidate agent `[k:01M8GPVC77K0TX3XN7WDF4SDJ6]`, chưa tự duyệt.
- Lượt sau 5 ca đã qua, 4 ca còn lỗi helper vì Save trả redirect hợp lệ; helper theo Location rồi đọc profile, không đổi assertion sản phẩm. Lượt hẹp tiếp theo **17/17** (9 account + 8 reconciliation) qua, log `%TEMP%/taxmanager-accounts-focused-3.log`.
- Hai ca tăng thêm kiểm registry và transport/rate limit: TLS listener trên Windows có thể đóng kết nối trước HTTP 400. Test chỉ chấp nhận 400 hoặc IOException transport, không nhận 2xx; registry thiếu phải lỗi và không tạo file mới. Toàn bộ **11 account test** sau đó đã qua trong cổng đầy đủ.
- `bint test affected --run` không thực thi do widening full + thiếu `eval.test_command`, selection ghi metadata Hallmark unknown_file. Giữ nguyên cấu hình và dùng script full theo `[k:01M8F671GP7FD4E6G4CVEW6E7R]`. Log `%TEMP%/taxmanager-accounts-month-final-affected.log`, selection `taxmanager-accounts-month-final-selection.json`; không báo affected đã qua.
- Cổng trước chỉnh nhỏ summary/cụm liên kết cuối: **699/699** (Core 209, Data 328, Web 162), EF không pending model changes, exit 0; `%TEMP%/taxmanager-accounts-month-final-full.log`. Cổng mã cuối được bổ sung sau QA bên dưới; không dùng lượt cũ thay mã mới.

## Visual QA và giới hạn

Áp dụng Hallmark, frontend-design, ui-ux-pro-max; đối chiếu Identity với [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0). Finish review trong phiên Codex, không có reviewer độc lập.

Orca từ chối chứng chỉ development tại HTTPS 5091 với `ERR_CERT_AUTHORITY_INVALID`. Không thay trust store máy. Đã tải HTML response thật qua HTTPS vào thư mục TEMP, sao chép đúng CSS/font/JS và phục vụ bản xem tại HTTP 5092, chặn submit của bản xem. Ảnh này dùng kiểm bố cục/focus/toggle, **không chứng minh đăng nhập end-to-end trong browser**; xác thực chỉ được kiểm bằng HTTP test dùng TLS có pin chứng chỉ. Không đưa bản xem tạm vào sản phẩm.

Ảnh đã xem: login desktop `6bfc18a0-eb4f-487c-bea3-ec4168e2fbbe`, đăng ký 375 px `8b818f9b-1cdc-4467-b21d-3fd40f176e0d`, lỗi đăng ký desktop `0cd96cfb-c6d8-41f4-86db-cee4c35c9637`; PNG trong `%TEMP%/orca-browser-screenshots/`, có thể hết hạn. Ảnh lỗi đầu phát hiện thứ tự Password trước Username; đã sửa partial summary và đổi chữ synthetic thành dữ liệu thử, cần ảnh xác nhận bản cuối.

### Phản hồi tiếp theo: thiết kế lại summary và nút mật khẩu

Summary chuyển vào đầu panel nhập, nền trong suốt, không khung trang trí/bullet; icon cảnh báo và heading nhỏ, câu lỗi thuần chữ theo thứ tự ô. Chỉ một link “Sửa thông tin” đến ô lỗi đầu (Username khi lỗi đăng nhập chung), vùng bấm cao 44 px. Giữ role alert, focus sau POST, lỗi inline và ring khi dùng bàn phím. Bản xem 320/375 px không tràn, có đúng một primary và một link trong summary. Tab từ summary tới “Sửa thông tin”, Enter đưa focus tới Username đã được thử.

Hiện/ẩn mật khẩu đổi sang icon mắt nằm trong từng ô. Hai ô đăng ký bật riêng, không còn hàng nút chữ bên dưới. Accessible name/title đổi theo thao tác Hiện/Ẩn, aria-controls trỏ đúng ô, aria-pressed cập nhật; icon SVG decorative. Vùng bấm 44 × 44 px, input chừa chỗ bên phải, hidden khi không có JS. Thử click rồi Enter: chỉ ô đích chuyển password/text, giá trị giữ nguyên; một primary, không tràn 375 px. Giữ chữ cho đăng nhập/tạo tài khoản/sửa lỗi vì cần rõ hành động.

Đã xem ảnh 375 px form với icon `36ef44f3-d3dc-49ee-ad92-f7c98ba67ffa`, 320 px lỗi với icon/focus `929d42ec-1e8e-4fbf-96c7-094d0dcbbbc4`; cùng thư mục TEMP, không chứa tài khoản thật. Màu chữ phụ computed `#526057` trên panel trắng được kiểm tương phản riêng ở kết quả cuối. `node --check workspace.js` và build preview không lỗi/cảnh báo. Hallmark sweep cuối ghi `%TEMP%/taxmanager-account-error-final-hallmark.json`: một FAIL 56 và WARN F7 như giới hạn trên; không có render native, không báo lint sạch.

Tương phản chữ phụ trên trắng **6,63:1**, brand **7,65:1**, chữ lỗi **6,50:1** (tính luminance sRGB), vượt mức 4,5:1. Sweep sau icon `%TEMP%/taxmanager-password-icon-hallmark.log` giữ đúng 56/F7 và giới hạn renderer. Focus bàn phím/nhãn icon được kiểm trong bản xem; chưa coi đây là QA screenreader hoặc đăng nhập thật trên browser.

Ảnh login desktop 1440 px `dbea3b00-6451-4c04-834e-b72851288712` đã xem: một icon mắt, một primary, không tràn, input chừa 52 px bên phải. Cổng mã cuối sau thay icon: **699/699** (Core 209, Data 328, Web 162), EF không pending model changes, exit 0; `%TEMP%/taxmanager-password-icon-full.log`. Hai cảnh báo xUnit2031 có từ trước ở ForecastPersistenceBlackBoxTests:241/243. `bint test affected --run` vẫn không chạy vì metadata Hallmark làm widening full và thiếu eval.test_command; log `taxmanager-password-icon-affected.log`, selection `taxmanager-password-icon-selection.json`. Dùng script full, không sửa cấu hình Bint hoặc báo affected đã qua.

Giữ mode local/synthetic. Còn đồng ý xử lý, xuất/xóa toàn bộ tài khoản, reset/email/MFA, registry backup vận hành và quản lý khóa/cookie bền, review bảo mật độc lập, screenreader/native zoom và QA login trong browser tin chứng chỉ. Không đánh dấu hoàn tất Đ6 hoặc quyền triển khai qua mạng/dữ liệu thật.
