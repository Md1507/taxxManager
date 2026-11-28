# Đ6e — rà phiên đăng nhập và quyền dữ liệu local

Ngày 29/11/2026. Rà mã và kiểm thử HTTP trong phiên triển khai này; không phải review bảo mật độc lập hoặc nghiệm thu dữ liệu thật. Giữ nguyên thiết kế UI, Identity và giới hạn local synthetic. Không triển khai bản redesign đang chờ duyệt.

## Phát hiện và sửa

`DemoApplication` xác thực cookie trước khi chờ gate tài khoản. Kiểm tra sau gate chỉ xét user còn tồn tại. Nếu security stamp bị thu hồi hoặc ticket hết hạn trong lúc chờ, request vẫn chạy. Bốn ca HTTP tái hiện: GET danh sách trả 200 và POST lưu hồ sơ trả 302, cho cả hai tình huống.

Sau gate, đọc user bằng `AsNoTracking`, kiểm stamp qua `SignInManager.ValidateSecurityStampAsync(user, stamp)` và so `ExpiresUtc` với `TimeProvider`. Không dùng user tracked từ bước xác thực. `AuthenticateAsync` trong cùng request có thể trả kết quả cache nên cần kiểm hạn ticket tường minh. Phiên không hợp lệ được đăng xuất, trả 401 và `Cache-Control: no-store` trước handler dữ liệu.

Phạm vi sửa xử lý thay đổi trước lúc request nhận gate. Request đã qua điểm kiểm tra và đang thực thi không bị hủy giữa chừng. Gate nằm trong một host; không đồng bộ nhiều tiến trình.

## Đối chiếu ranh giới

| Ranh giới | Bằng chứng trong phạm vi local |
|---|---|
| Cách ly | Fallback policy yêu cầu đăng nhập; kho lấy subject và đường dẫn từ registry. Test account kiểm A/B qua URL, POST, history, CSV, backup và restore. |
| CSRF | Razor Pages kiểm token; stamp bổ sung gắn subject/workspace. Test account, export và deletion kiểm form cũ sau đổi tài khoản bị từ chối. |
| Phiên và mật khẩu | Identity xử lý hash, cookie, lockout và stamp. Test mới kiểm thu hồi/hết hạn trong lúc chờ và hạn cố định 30 phút dù truy cập đều. |
| Xuất | Kiểm lại mật khẩu, lấy kho gốc đúng chủ; JSON không chứa hash/khóa/đường dẫn. Test export kiểm tải tệp, kho lỗi và thiếu khóa. |
| Xóa dở | Manifest ghi trước filesystem, chặn dữ liệu/exports khi pending. Test deletion kiểm lỗi intent/lỗi cuối/host mới, restored/preview, request chờ và POST đồng thời. |

Đây là đối chiếu mã và các ca HTTP, không khẳng định đã kiểm mọi kiểu tấn công. Review độc lập trước dữ liệu thật, vận hành qua mạng, khóa vận hành và nghiệm thu UX toàn bộ vẫn mở. Đối chiếu cơ chế với [Identity configuration của Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0). Không sửa luật thuế/model EF/migration.

## Kiểm chứng

- Trước sửa: 4 ca chờ gate thất bại đúng hành vi cần chặn; 1 ca hạn cố định qua. Callback xác thực báo request đã xác thực trước khi test thu hồi stamp/đẩy đồng hồ; không dùng sleep để đoán thứ tự.
- Sau sửa: cả 5 ca `AccountSessionHttpTests` qua. Các ca từ chối kiểm không tạo hồ sơ, tài khoản/kho còn nguyên và đăng nhập lại sử dụng được. Ca hạn cố định kiểm truy cập ở phút 10/20/29, chặn ở phút 31, đồng thời chặn xuất/xóa bằng phiên cũ.
- `bint test affected --run` không chạy test: bgraph nới full nhưng thiếu `eval.test_command`. Đã đọc JSON widening; giữ nguyên ảnh/audit ngoài mapping. Chạy Web MVP đầy đủ thay thế theo [k:01M8F671GP7FD4E6G4CVEW6E7R]: exit 0, Core 209 + Data 332 + Web 209 = 750 test, không skip; EF không có pending model changes. Build của lượt script không có warning; lượt build kiểm hẹp trước đó có hai warning xUnit2031 hiện có trong `ForecastPersistenceBlackBoxTests.cs`, không sửa tệp đó.
- Log `%TEMP%/taxmanager-account-session-{red,green,full}.log`.
- Tri thức `01M8HMBSDAQRSXFZAXZJGYS3ZM` ở trạng thái agent/candidate, chưa duyệt/publish.
