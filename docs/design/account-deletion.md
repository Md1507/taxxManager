# Đ6d — xóa tài khoản thử

29/11/2026. Tiếp xuất JSON Đ6c, chỉ trong local synthetic/HTTPS hiện có. Không mở quyền dùng dữ liệu thật hoặc phục vụ qua mạng.

## Màn hình và hành vi

`/tai-khoan/xoa`: layout tài khoản, Inter/xanh hiện có, panel trắng rộng vừa đọc. H1 “Xóa tài khoản thử”, tên đang đăng nhập, phạm vi xóa và giới hạn bản sao. Một ô mật khẩu hiện tại có icon hiện/ẩn và một checkbox xác nhận chưa chọn. Một nút đặc “Xóa tài khoản và dữ liệu”, nút phụ “Giữ tài khoản”. Cảnh báo bằng icon/chữ, không thêm divider, dropdown hoặc thẻ lồng nhau. Nút xóa trong màn tài khoản là phụ và mở màn xác nhận; GET tuyệt đối không xóa.

Phạm vi: tài khoản Identity, xác nhận dữ liệu thử, kho gốc (hồ sơ, mọi phiên bản, lịch sử và audit), preview và kho khôi phục mà phiên chạy hiện tại còn quản lý cho tài khoản đó. Không xóa tài khoản khác, registry chung, kho demo chung, tệp người dùng tải, sao lưu ngoài ứng dụng hoặc kho TEMP từ phiên chạy trước không còn liên kết chủ sở hữu. Không hứa xóa vật lý khỏi ổ đĩa/backups.

## Thực thi

- POST cần cookie hợp lệ, CSRF gắn subject, mật khẩu được SignInManager kiểm lại với lockout và checkbox xác nhận. Không nhận owner/root từ browser. Tài khoản chưa chấp nhận phiên bản dữ liệu thử mới vẫn có quyền xóa. Không echo mật khẩu hoặc đường dẫn khi lỗi.
- Đ6g: tài khoản chỉ có Google thay ô mật khẩu bằng xác nhận Google mới, ghim đúng user/provider/subject và mục đích delete, hạn 5 phút từ lúc khởi tạo. Proof được tiêu thụ ở server trước khi xóa, không dùng lại bằng cookie đã lưu, không nhận proof export. Sau Google vẫn cần checkbox và POST xóa riêng. Lỗi xóa cần xác nhận Google lại để tiếp tục; route xóa vẫn cho khởi tạo challenge khi đang pending. Tài khoản có mật khẩu giữ quy tắc cũ.
- Serialize request có xác thực theo subject trong host, gồm tài khoản và các kho/phiên browser, rồi kiểm lại Identity sau khi chờ. Vì xóa có thể chạy đồng thời đọc/ghi từ tab khác, gate ở từng browser riêng không đủ. Không thay contract fragment/sidebar hoặc phép tính.
- Trước lần xóa đầu, kiểm toàn bộ root server biết: đường dẫn tuyệt đối ngay dưới TEMP, tên/marker synthetic đúng, không liên kết ở root/cây con/ancestor; không trùng registry hoặc kho gốc của tài khoản khác. Không cần giải mã DB mới xóa được kho hỏng.
- Ghi manifest xóa vào claim Identity bền trước khi xóa tệp. Manifest gắn user ID và StoreRoot, chứa root đã kiểm; không có khóa/mật khẩu. Khi claim tồn tại, các route dữ liệu và xuất bị chặn, chỉ cho tiếp tục xóa hoặc đăng xuất. Không gọi thao tác đã xong khi tệp còn chưa xóa được.
- Gỡ các tham chiếu cache/khôi phục của đúng subject, dispose key, xóa các root đã ghi. Sau đó mới xóa user/claims bằng UserManager. Lỗi giữa chừng giữ claim để thử lại cùng thao tác, kể cả sau host mới; root đã mất được coi là hoàn tất. Không tạo lại kho/khóa hoặc bỏ qua liên kết mới xuất hiện. Đây là xóa tệp, không phải ordinary-delete từng hồ sơ.
- Thành công đăng xuất, cookie các browser khác không còn hợp lệ do user không còn; form cũ và POST lặp không có tác dụng. Tạo lại cùng tên đăng nhập tạo subject/kho mới.

## Kiểm chứng

GET/anonymous/CSRF/mật khẩu/checkbox/lockout, không echo bí mật; A/B không ảnh hưởng nhau, chủ lấy từ server kể cả khi giả userId/root; xóa khi đang mở restored store vẫn xóa kho gốc và các restored/preview còn quản lý của A; không cần đồng ý nội dung mới. Kiểm lỗi filesystem, pending chặn tab khác, thử lại với host mới, root thiếu/hỏng key, liên kết/ngoài TEMP/registry/kho B bị từ chối trước xóa. Giữ full Web MVP/EF, kiểm mobile và focus/lỗi.
