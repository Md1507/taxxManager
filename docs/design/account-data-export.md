# Đ6c — xuất dữ liệu tài khoản thử

28/11/2026, tiếp Đ6b. Phạm vi là xuất dữ liệu đọc được của tài khoản đang đăng nhập trong mode local synthetic, không phải backup hệ thống Identity hoặc quyền dùng dữ liệu thật. Xóa tài khoản là bước riêng tiếp theo.

## Màn hình trước code

Route `/tai-khoan/xuat-du-lieu`, dùng layout tài khoản hiện có, H1 “Xuất dữ liệu tài khoản”, panel trắng trên nền xanh xám. Nêu rõ bao gồm tên đăng nhập, xác nhận dùng dữ liệu thử, hồ sơ/phiên bản/kết quả/nhật ký trong kho gốc. Ô mật khẩu hiện tại có icon mắt, hướng dẫn ngay dưới. Một primary “Tải dữ liệu tài khoản”, link phụ trở lại tài khoản. Không dropdown, divider hoặc card lồng nhau.

Tệp JSON đọc được, **không mã hóa**, người có tệp có thể đọc nội dung; người dùng tự giữ hoặc xóa tệp tải về. Đây là lựa chọn để kiểm tra và mang dữ liệu đi, khác với bản sao có mật khẩu đã có. Không cần thêm mật khẩu tệp mới. Tệp không phục hồi tài khoản hoặc nhập vào trang khôi phục.

Đ6h bổ sung `account.email`, `emailConfirmed`, `pendingEmail`; không xuất token hoặc nonce/thời gian yêu cầu khôi phục/xác minh. Email đang chờ không được hiểu là email đã xác minh.

## Ranh giới

- Chỉ POST có CSRF, cookie hợp lệ, kiểm lại mật khẩu bằng SignInManager với lockout. GET chỉ hiện form. Dùng policy rate limit account-entry hiện có; không tự xây hash/xác thực khác. Thiếu/sai mật khẩu không trả tệp, không echo mật khẩu hoặc chi tiết đường dẫn/khóa trong lỗi.
- Đ6g: tài khoản chỉ có Google xác nhận bằng Google, không yêu cầu mật khẩu chưa từng tạo. Proof ghim user/provider/subject/mục đích export, hạn 5 phút từ lúc khởi tạo, được tiêu thụ ở server trước khi xuất; cookie đã lưu không dùng lại được. Bước OAuth không tự tải tệp. Sai chủ/mục đích, thiếu/hết hạn hoặc đã dùng thì phải xác nhận lại. Lỗi kho không trả tệp, nhắm đúng nút xác nhận Google; tài khoản local vẫn kiểm mật khẩu như trước.
- Chủ và kho lấy từ subject/registry. Không nhận userId, root, profileId hoặc store selector từ browser; không dùng kho restored theo browser cookie. Chỉ xuất **kho gốc tài khoản**, kể cả khi đang mở kho khôi phục. Trang nêu kho khôi phục tải riêng ở Sao lưu; không gọi đây là mọi bản sao trong TEMP hoặc ngoài ứng dụng.
- Trang xuất là quyền tài khoản, không bắt xác nhận lại phạm vi dữ liệu thử. Tài khoản cũ có thể lấy dữ liệu của mình mà không đồng ý mở hồ sơ mới.
- DTO có allowlist: ID/tên tài khoản và giá trị claim xác nhận đã lưu; hồ sơ cũ, thu nhập tháng, phụ thuộc/kỳ; forecast hiện tại và mọi phiên bản còn giữ; snapshot tính tháng/năm và audit được giải mã qua context. Kể cả hồ sơ đã ordinary-delete còn snapshot/audit/history, không lọc chỉ theo hồ sơ đang sống.
- Đ6g bổ sung `account.linkedLogins`: tên provider và mã định danh liên kết của chính chủ; mảng rỗng nếu chưa liên kết. Đây là dữ liệu cá nhân dùng để nhận diện, không phải OAuth token; không xuất token/cookie hoặc credential. Thay đổi thêm trường giữ schemaVersion 1, không đổi ý nghĩa trường cũ.
- Không xuất password hash, security/concurrency stamp, token/cookie, vai trò, khóa mã hóa, ciphertext, sentinel, StoreRoot hoặc đường dẫn nội bộ. Không serialize thẳng IdentityUser hay EF entity. Không xuất Identity registry hoặc catalogue luật chưa dùng; snapshot đã tính giữ căn cứ tại thời điểm tính.
- Đọc kho thuế trong cùng transaction chỉ đọc. Tái dùng ForecastStore.ListAsync để xác thực DB đã khởi tạo và xử lý kho hoàn toàn mới không sentinel đúng contract; không tạo sentinel khi xuất. Không Save, tính lại hoặc tạo audit. Thiếu/sai khóa, payload hỏng hoặc quá giới hạn phải lỗi toàn bộ, không trả tệp một phần/giả rỗng.
- Schema JSON có version, giờ tạo UTC, scope `original-account-store`, arrays rỗng cho phần chưa có; decimal/null và chuỗi payload phiên bản giữ nguyên. Giới hạn 10.000 bản ghi mỗi nhóm và 32 MiB JSON; vượt giới hạn báo lỗi, không cắt thầm. Filename server tạo, `application/json`, attachment, no-store, nosniff.

## Bằng chứng

Hai tài khoản A/B: xuất chỉ có dữ liệu của chủ, bỏ qua userId/root giả; số tiền/null/metadata/phiên bản/kết quả cũ/audit đúng. Export không làm đổi payload, revision, lịch sử, audit hoặc sentinel. Hồ sơ đã xóa vẫn có lịch sử lưu giữ. Kho restored không tráo nguồn xuất. Kho trống xuất được, khóa hỏng từ chối cả khi đọc metadata. Kiểm CSRF/GET/anonymous/mật khẩu sai/lockout, không echo bí mật, mở lại cùng host root mới; kiểm form, icon, lỗi focus và responsive.

Tham chiếu kỹ thuật: [Microsoft: download personal data với Identity .NET 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/add-user-data?view=aspnetcore-10.0). Prototype chọn DTO tường minh để không đưa trường nội bộ StoreRoot vào tệp; không coi việc có nút tải là chứng nhận tuân thủ pháp luật.
