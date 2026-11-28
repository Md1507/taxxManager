# Đ6c — xuất kho gốc tài khoản synthetic

28/11/2026. [Contract](../design/account-data-export.md), route `/tai-khoan/xuat-du-lieu`. JSON đọc được, không mã hóa; POST + CSRF + kiểm lại mật khẩu bằng Identity, lockout/rate limit hiện có. Chỉ lấy subject/registry, không nhận kho hoặc chủ từ browser. Xuất kho gốc ngay cả khi browser đang dùng kho khôi phục. Không nhập/khôi phục từ tệp này.

## Bằng chứng

4 Data tests kiểm plaintext/null/0/metadata, forecast versions, snapshot tháng/năm nguyên gốc, audit còn giữ sau ordinary delete; đọc không ghi, không tạo sentinel cho kho chưa dùng, thiếu khóa ở DB đã khởi tạo và dữ liệu mã hóa hỏng bị từ chối. Xóa catalog sau tính vẫn xuất nguyên snapshot năm, không tính lại. Áp dụng [k:01M85QGMTJVNX37D5XYH5161KM], [k:01M85R5FV73D0YXHBS25P7X4XA], [k:01M85QH0MMV8Y56ZK918ZB7QBV].

6 Web tests qua host HTTPS với chứng chỉ fixture pin theo thumbprint:

- Opt-in/anonymous/GET/POST/CSRF và mật khẩu hiện tại; response tệp có attachment, no-store, nosniff. Kho chưa dùng không phát sinh sentinel.
- Hai tài khoản; tham số chủ/kho giả không đổi nguồn. Tệp chỉ có dữ liệu chủ, gồm các phiên bản/hồ sơ đã xóa còn lưu; không chứa hash, stamps, mật khẩu, khóa, ciphertext hoặc đường dẫn. Số audit không đổi.
- Chấp nhận một bản khôi phục cũ rồi xuất: lấy phiên bản mới ở kho gốc.
- Payload mã hóa hỏng: 422, không attachment, không echo mật khẩu/nội dung/đường dẫn.
- Tài khoản chưa xác nhận thông báo vẫn xuất được; token cũ sau đổi tài khoản trả 400.
- Năm lần thử mật khẩu sai khóa thao tác, kể cả cookie vẫn còn; mật khẩu đúng trong lockout không trả tệp.

Giới hạn 10.000 dòng/nhóm và 32 MiB output được chặn trong code, chưa stress-test sát biên. Materialization có thể dùng bộ nhớ trước khi chạm giới hạn output; đây là giới hạn prototype local, chưa chứng minh vận hành dữ liệu lớn.

## Thông báo dữ liệu thử

Đổi `LocalDataUse.Version` sang `local-demo-2026-11-28-v2` vì đã có xuất dữ liệu kho gốc. Claim cũ được giữ; người dùng phải xác nhận nội dung mới trước route dữ liệu. Không tự nâng claim hoặc suy đồng ý; trang xuất tài khoản vẫn dùng được mà không cần xác nhận mới. Không coi thông báo prototype là đồng ý xử lý dữ liệu thật đã được thẩm định.

## Visual và truy cập

Giữ xanh/Inter, một primary, icon mắt có nhãn, thông báo rõ JSON không mã hóa và phạm vi kho gốc. [Desktop 1440](images/2026-11-28-account-export/desktop-1440.png), [320](images/2026-11-28-account-export/form-320.png), [lỗi 375](images/2026-11-28-account-export/error-375.png) đã mở và xem. Body không tràn tại 320/375. Link Sửa thông tin focus đúng Password, mật khẩu rỗng sau lỗi. axe trên màn lỗi 375: 0 violations/0 incomplete cho các tag A/AA 2.0/2.1/2.2 được engine hỗ trợ.

Orca chưa tin chứng chỉ development. Ảnh dùng HTML phản hồi thật của host HTTPS với tài khoản QA synthetic mới, phục vụ bản mirror inert tại 5092 cùng CSS/fonts/JS thật. Chỉ mirror visual/tương tác không gửi; các HTTP tests mới chứng minh server. Không hạ HTTPS hay sửa trust store. Chưa kiểm screenreader và zoom native.

## Kiểm chứng kỹ thuật cuối

`scripts/dev/verify-web-mvp.ps1` kết thúc mã 0: build 0 lỗi/0 cảnh báo; Core 209, Data 332, Web 180, tổng **721 test qua**; EF báo không có thay đổi model chờ migration. `node --check workspace.js` và `git diff --check` qua. SHA-256 đối chiếu 307 tệp nguồn/test/script/config với workspace chính: 0 khác biệt.

Chạy trên `C:/Users/LENOVO/AppData/Local/Temp/taxmanager-export-verify-47e2ed76865f45f99871fbcac807e449`, log cuối `%TEMP%/taxmanager-export-full-verify-complete.log`. App chính khóa DLL nên dùng bản nguồn riêng; không dừng app của người dùng. Lượt trước 721 test qua nhưng EF không tìm thấy SDK; đã khôi phục đủ tệp chuẩn bị SDK/global.json trong bản kiểm thử, giữ pin 10.0.404/EF 10.0.15 và chạy lại toàn bộ script đến mã 0. Không thay script sản phẩm hoặc migration.

`bint test affected --run` cuối bị chặn khi selection nới tới full do 6 ảnh ngoài ánh xạ bgraph và `eval.test_command` null; JSON có `ran:false`. Không bỏ ảnh, sửa cấu hình Bint hoặc tính selection là pass. Cổng script đầy đủ ở trên là bằng chứng kiểm chứng thay thế [k:01M8F671GP7FD4E6G4CVEW6E7R].

Điểm fixture mới được ghi thành candidate `01M8GTYSF3CF4BCBHPA7T286QA`: SyntheticDatabase.Open gán OperationId cố định mỗi context, các lần ghi độc lập phải dùng context fresh. Chưa publish hoặc tự duyệt tri thức này.

Chưa làm xóa toàn bộ tài khoản, tự xóa tệp tải về/bản sao bên ngoài, triển khai public hoặc nghiệm thu bảo mật độc lập. Đây là Đ6c local synthetic; Đ6 tổng thể vẫn mở.
