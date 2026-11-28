# Sao lưu và khôi phục local synthetic — v1

Ngày 28/11/2026. Phạm vi chặng 4; chỉ demo Development trên loopback với dữ liệu giả.

## Hành vi

Sao lưu toàn bộ kho đang chọn, gồm hồ sơ hiện hành, mọi revision, lịch sử tháng/năm, audit, catalog và khóa đọc tương ứng. CSV báo cáo không thay thế bản sao này. SQLite online backup tạo ảnh nhất quán, không copy tệp DB đang chạy hoặc riêng WAL. Khôi phục giữ nguyên ID, số revision, payload và trạng thái nguồn; không tính lại hoặc nâng cấp luật.

Tệp `.taxbackup` được bảo vệ bằng mật khẩu do người dùng giữ. Mất mật khẩu thì không mở được. Không xuất khóa rời. Chọn tệp và nhập mật khẩu → kiểm tra → xem ngày sao lưu, số hồ sơ/phiên bản/kết quả/audit và số ID trùng → xác nhận mở kho riêng. Không merge hoặc ghi đè kho hiện hành. Có nút quay lại kho ban đầu. Kho khôi phục có thể sửa và sao lưu tiếp; bản sao gốc không thay đổi.

Mỗi trình duyệt có cookie HttpOnly, SameSite Strict được Data Protection ký/mã hóa. Kho chọn nằm phía server. Form antiforgery gắn với trình duyệt và thế hệ kho; sau chuyển kho, POST từ form cũ bị từ chối. Yêu cầu của cùng trình duyệt được tuần tự hóa suốt request để không đổi kho giữa đọc và ghi. Không phải cơ chế phân quyền đa tài khoản.

Xem trước chỉ tạo kho staging riêng dưới TEMP, không ghi vào kho đang dùng. Handle ngẫu nhiên chỉ dùng một lần, gắn trình duyệt, hết hạn sau 10 phút. Hủy/hết hạn dọn staging; kho đã xác nhận được giữ để không mất chỉnh sửa. Sau restart cần mở lại bằng SyntheticStore trỏ chính kho đó, hoặc nhập lại bản sao; lựa chọn trình duyệt/handle không tồn tại qua host restart. Không hứa xóa vật lý khỏi WAL, vùng trống, swap, crash dump hoặc bản sao người dùng giữ.

## Định dạng v1

Không ZIP, không tên đường dẫn từ tệp. Header cố định 8 byte `TAXBK001`, salt 16 byte, nonce 12 byte; ciphertext và tag GCM 16 byte. Toàn bộ header/salt/nonce là AAD. Plaintext có UTC ticks (Int64 little endian), độ dài khóa và DB (Int32 little endian), bytes key JSON và SQLite. Không nén. Giới hạn DB 16 MiB, key JSON 16 KiB, upload 17 MiB, mật khẩu 12–128 ký tự; phiên bản không biết bị từ chối trước KDF. Mật khẩu không trim/normalize.

PBKDF2-HMAC-SHA256 cố định 600.000 vòng, key 32 byte; AES-256-GCM với nonce/salt mới mỗi export. Không nhận số vòng từ tệp. Dùng BCL, zero các buffer key/plaintext trong finally; string mật khẩu và bộ nhớ framework không bảo đảm zero. Không đưa mật khẩu vào HTML/ModelState/URL/log; luôn no-store.

Kiểm trước khi hoạt hóa: định dạng, độ dài, GCM/password, SQLite integrity/FK, danh sách migration và schema đúng bản app, sentinel/khóa, giải mã toàn bộ trường được bảo vệ, snapshot lịch sử, audit, payload forecast và catalog tham khảo đúng contract hiện tại. Schema cũ/mới khác bị từ chối, không gọi upgrader và không tạo fallback/seed để sửa bản nhập.

Giới hạn tài nguyên demo: tối đa 128 phiên trình duyệt và 8 kho đã khôi phục mỗi host; một preview mỗi trình duyệt. Preview được dọn định kỳ. Không cho nhập tiếp khi đang dùng kho khôi phục: quay lại kho ban đầu trước.

## Bố cục

Giữ palette/font/token xanh hiện có. Trang Sao lưu có một panel trắng căn trái, ô mật khẩu/xác nhận và đúng một nút đặc “Tải bản sao”; liên kết phụ mở màn Khôi phục. Màn Khôi phục có ô tệp/mật khẩu và nút đặc “Kiểm tra bản sao”. Màn xem trước thay form nhập bằng số liệu và cảnh báo ID trùng; nút đặc “Mở kho khôi phục”, nút Hủy phụ. Không thêm dropdown, bảng viền đậm hoặc breadcrumb lặp H1.

```text
Sao lưu                         Khôi phục
[Mật khẩu]                      [Tệp bản sao]
[Nhập lại mật khẩu]             [Mật khẩu]
[Tải bản sao]  Khôi phục         [Kiểm tra bản sao]

Xem trước bản sao
Ngày / hồ sơ / phiên bản / lịch sử / audit
ID trùng: n. Mở kho riêng, giữ nguyên kho ban đầu.
[Mở kho khôi phục]  Hủy
```

## Nguồn kỹ thuật

- [Microsoft SQLite online backup](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup).
- [Microsoft PBKDF2](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2?view=net-10.0) và [AES-GCM](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.encrypt?view=net-10.0).
- [OWASP PBKDF2 guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html): 600.000 vòng SHA256 là baseline lưu mật khẩu được tham khảo cho lựa chọn KDF này; không phải chứng nhận định dạng backup.

Chưa cho phép dữ liệu thật/public. Review bảo mật và nghiệm thu người dùng vẫn theo PLAN.
