# Đăng nhập bổ sung và khôi phục tài khoản local

29/11/2026. Người dùng chọn Google trước; yêu cầu cả email khôi phục và một cách không phụ thuộc email. Hướng tiếp theo là passkey cùng mã dự phòng; chưa triển khai TOTP hoặc SMS. Các đợt độc lập, giữ giới hạn local synthetic, không mở quyền phục vụ qua mạng.

## Đợt hiện tại: liên kết và đăng nhập Google

- Google là cách đăng nhập độc lập. Google đã biết mở lại đúng tài khoản; Google mới được tạo tài khoản không mật khẩu sau khi chủ động xác nhận phạm vi dữ liệu thử. Username là alias server sinh, không lấy email/tên từ Google; chủ có kho synthetic mới riêng. Tạo user, liên kết và ghi nhận đồng ý trong cùng transaction registry; không tự nhận kho của tài khoản khác.
- Tài khoản local đang có vẫn sở hữu kho cũ. Liên kết Google từ phiên đang đăng nhập: chọn Google, trở về và lưu liên kết, không nhập lại mật khẩu local. Không ghép theo tên/email. Intent ghim ID tài khoản đang mở, callback của A không được dùng cho B. CSRF và gate tài khoản vẫn bắt buộc.
- Dùng ASP.NET Core Google handler và Identity external cookie/UserLogins, không tự xử lý OAuth token hay xác thực trình duyệt. Chỉ xin scope `openid` lấy định danh ổn định `sub`, không thu email/profile ở đợt này; không lưu access/refresh token. Consent dùng dữ liệu thử không bị coi là đồng ý gửi email khôi phục.
- Khởi tạo Google bằng POST+CSRF; callback do framework kiểm state/correlation/PKCE. Provider không lấy từ form. Callback thất bại có thông báo chung, không echo query/token/lỗi upstream.
- Cookie ứng dụng vẫn Secure/HttpOnly/Strict. Khi liên kết/xác nhận hành động trở về từ Google, trang kết quả có bước tiếp tục cùng origin; form đích lấy CSRF theo phiên hiện tại, không phụ thuộc cookie Strict có được gửi trong chuỗi chuyển hướng ngoài hay không. Liên kết/gỡ đều POST; hủy không ghi liên kết. Gỡ Google của tài khoản có mật khẩu vẫn kiểm mật khẩu và lockout; tài khoản chỉ có Google không được gỡ credential duy nhất.
- External cookie ngắn hạn, Secure/HttpOnly/Lax để hoạt động qua chuyển hướng Google; không sliding. Việc hoàn tất đọc chứng cứ từ external cookie của framework, ghim user ID cho liên kết, không lấy subject từ hidden input. Không cho liên kết Google đã thuộc tài khoản khác; một Google trên mỗi tài khoản trong UI này.
- Đăng nhập lấy đúng kho cũ; kiểm user/lockout/pending deletion khi đã lấy gate tài khoản. Tài khoản đang xóa chỉ có thể tiếp tục xóa/đăng xuất theo guard hiện có. Tài khoản có mật khẩu vẫn xác nhận xuất/xóa bằng mật khẩu. Tài khoản chỉ có Google xác nhận lại bằng Google, proof ghim đúng user/provider/sub và mục đích export hoặc delete, hạn 5 phút; tiêu thụ trước thao tác, không tái sử dụng cho lần khác hoặc mục đích khác. Xóa vẫn cần checkbox tường minh sau khi Google trả về; challenge không tự xóa hoặc tải tệp. Google đang tắt thì không có nút giả; phải bật lại để xác nhận tài khoản Google-only.
- Google chỉ bật khi `Authentication:Google:Enabled=true` và có ClientId/ClientSecret. Thiếu cấu hình thì không dựng nút giả; bật nhưng thiếu khóa thì không khởi động. Không ghi secret vào repo/log. Callback `/signin-google` phải đăng ký đúng origin/port ở Google Cloud.

Bố cục: login giữ nút Đăng nhập chính, một nút Google phụ ở vùng phương thức khác, không divider. Tài khoản có hàng Google trong nhóm đăng nhập. Màn liên kết không có ô mật khẩu; màn gỡ của tài khoản local dùng trường mật khẩu hiện có. Google mới chỉ xác nhận thông báo dữ liệu thử; không thêm form đăng ký mật khẩu. Xuất/xóa chỉ có một primary ở mỗi trạng thái: xác nhận Google, rồi hành động thực sự. Giữ xanh/Inter/icon mắt và focus, không panel lồng.

Thông báo dữ liệu thử chuyển v4, giữ claim cũ và yêu cầu xác nhận mới trước route dữ liệu. Export thêm `account.linkedLogins` với provider/subject, không token. Thao tác xóa tài khoản dùng cascade Identity sẵn có để gỡ UserLogins; không sửa schema thuế hoặc registry.

### Cấu hình chạy thử Google thực tế

1. Tạo OAuth client loại Web application trong Google Cloud của bạn. Đăng ký redirect URI chính xác, ví dụ `https://localhost:5088/signin-google` nếu mở app qua localhost. Nếu mở bằng `127.0.0.1`, đăng ký thêm đúng URI với host đó; không đổi host giữa chừng. Dùng trình duyệt ngoài được Google hỗ trợ và chứng chỉ development hợp lệ; không dùng webview nhúng để đăng nhập Google.
2. Giữ ClientId/ClientSecret ngoài repository. Có thể chạy `dotnet user-secrets init --project src/TaxManager.Web` rồi dùng Secret Manager local để đặt ba khóa `Authentication:Google:Enabled`, `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret`. Lệnh init tạo UserSecretsId trong project; đó là mã kho local, không phải secret. Cấu hình môi trường tương ứng dùng dấu `__` giữa các cấp. Không gửi secret qua hội thoại, không thêm vào appsettings hoặc log.
3. Khởi động bản HTTPS local với `SyntheticDemo=true`, `SyntheticAccounts=true` và root synthetic muốn giữ. Khi tắt Google, không có nút Google; bật mà thiếu cấu hình thì startup từ chối. Google thật chưa được nghiệm thu trong đợt này vì không có OAuth client của chủ dự án.
4. Chọn Google ngay tại trang đăng nhập. Lần đầu chỉ xác nhận phạm vi dùng dữ liệu thử, không tạo mật khẩu riêng; lần sau đăng nhập thẳng vào kho cũ. Muốn gắn Google vào tài khoản local sẵn có: đăng nhập local, vào Tài khoản → Quản lý Google → chọn Google → Tiếp tục xác nhận → Lưu liên kết, không hỏi lại mật khẩu. Gỡ Google khỏi tài khoản có mật khẩu cần xác nhận mật khẩu và thu hồi phiên cũ. Tài khoản Google-only xác nhận xuất/xóa bằng chính Google, không bị buộc nhập mật khẩu chưa từng tạo.

Redirect URI phải khớp scheme/host/port/path theo [quy tắc Google](https://developers.google.com/identity/protocols/oauth2/web-server#uri-validation); cấu hình local không thay điều kiện vận hành qua mạng.

## Đ6h: email đã xác minh và đặt lại mật khẩu

Đã triển khai ngày 29/11/2026 trong phạm vi local synthetic. Có bốn màn: quản lý email, xác minh email, quên mật khẩu và đặt mật khẩu mới. Giữ xanh/Inter, panel trắng, một primary mỗi trạng thái, hướng dẫn ngắn cạnh ô nhập và icon mắt. Không dùng email Google làm email khôi phục tự động.

- Thêm/đổi/gỡ email cần mật khẩu hiện tại; tài khoản không mật khẩu xác nhận bằng đúng Google đã liên kết, proof `email` có hạn 5 phút và dùng một lần. Proof xuất/xóa không thay cho proof email. Hủy email đang chờ là POST+CSRF.
- Thêm/đổi chỉ lưu email đang chờ. Email cũ vẫn dùng được cho tới khi xác minh mới thành công. GET từ thư chỉ hiển thị bước xác nhận; POST+CSRF mới thay đổi. Không cho hai tài khoản xác minh cùng một địa chỉ chuẩn hóa, kể cả yêu cầu đồng thời. Registry legacy có nhiều chủ đã xác minh cùng email thì từ chối chọn một chủ tùy ý.
- Identity tạo và kiểm token theo user/mục đích/security stamp, hạn 30 phút. Registry chỉ lưu metadata email/nonce/thời gian, không lưu token thư. Mỗi yêu cầu mới thay nonce, tiêu thụ nonce khi hoàn tất; gate tài khoản và transaction bảo đảm một yêu cầu chỉ thành công một lần. Không cho khôi phục tài khoản đang xóa.
- Yêu cầu reset cho email lạ/chưa xác minh/trùng hoặc gửi thư lỗi có cùng phản hồi chung. Có rate limit hiện có và cooldown một phút mỗi tài khoản. Không hiển thị liên kết khôi phục trong UI/log hay dùng Host header để dựng URL.
- Mật khẩu mới 8–128 ký tự, không chỉ khoảng trắng, có nhập lại/live validation. Reset thành công thu hồi phiên cũ, gỡ lockout và yêu cầu đăng nhập lại; giữ nguyên kho và liên kết Google. Với Google-only, email đã xác minh có thể tạo mật khẩu riêng đầu tiên. Xác minh/gỡ email cũng đổi security stamp và thu hồi phiên cũ.
- Không thêm schema hoặc migration. Export chỉ thêm email, trạng thái xác minh và email đang chờ; không xuất token/nonce/thời gian yêu cầu. Thông báo dùng dữ liệu thử v5 nêu email tùy chọn và chuyển địa chỉ tới dịch vụ gửi thư, giữ các claim đồng ý cũ.

### Bật gửi email thật cho bản local

Email mặc định tắt. Bật cần đủ bốn khóa ngoài repository:

```text
EmailRecovery:Enabled=true
EmailRecovery:PublicOrigin=https://localhost:5088
EmailRecovery:Resend:ApiKey=<đặt bằng Secret Manager hoặc biến môi trường>
EmailRecovery:Resend:From=<địa chỉ người gửi đã xác minh với nhà cung cấp>
```

Không đặt khóa thật vào ví dụ, appsettings, câu lệnh ghi log hoặc hội thoại. Biến môi trường dùng `__` thay `:`. PublicOrigin phải đúng HTTPS loopback/port đang mở, chỉ origin, không path/query/userinfo. Links luôn dùng origin cố định này. Bộ gửi `IAccountEmailSender` hiện có adapter Resend HTTP tới endpoint cố định, không redirect, hạn 15 giây; gửi text, không đọc/echo lỗi upstream. Có thể thay adapter mà không đổi flow Identity. Cần tự chuẩn bị sender/domain theo điều kiện nhà cung cấp; không mặc định có quyền gửi miễn phí vô hạn.

Vào Tài khoản → Quản lý email → xác nhận chủ tài khoản → gửi thư → mở thư và xác minh. Khi quên mật khẩu, chọn Quên mật khẩu tại màn đăng nhập và nhập email đã xác minh. Google vẫn đăng nhập độc lập khi đã liên kết, không cần reset mật khẩu local trước.

Giới hạn bản development: Data Protection của host hiện vẫn ephemeral. Liên kết có hạn tối đa 30 phút và mất hiệu lực ngay khi app khởi động lại; chờ cooldown một phút nếu cần rồi yêu cầu thư mới. Email/metadata/tài khoản/kho vẫn được giữ. Persist key ring cần thiết kế vận hành riêng trước khi triển khai qua mạng; đợt này không đổi ranh giới đó. Kiểm chứng dùng sender giả, không gửi thư hay Google thật; xem `docs/reviews/2026-11-29-email-recovery.md`.

## Các đợt tiếp theo

1. Passkey: đăng ký khi đang đăng nhập và xác nhận lại chủ tài khoản; dùng API Identity/WebAuthn .NET 10. Chốt origin/RP ID ổn định, kế hoạch registry schema/copy-upgrade trước khi thêm bảng passkey vào DB cũ; không dùng EnsureCreated để giả nâng schema. Passkey là credential đã đăng ký, không tự lấy lại quyền nếu mất mọi credential.
2. Mã dự phòng: cấp trước, chỉ hiện một lần, dùng một lần/luân chuyển, không lẫn export dữ liệu thuế. Mất email, thiết bị và mã dự phòng không có đường khôi phục giả. Không tự thêm SMS hoặc dịch vụ tính phí.

TOTP sinh mã trên thiết bị có thể vận hành không phí gửi tin, nhưng cần đăng ký trước và cơ chế mất thiết bị; chưa chọn làm cách reset độc lập. Email có gói miễn phí có hạn mức; tên miền/nhà cung cấp có thể phát sinh phí riêng. Passkey dùng API framework/trình duyệt, không cần nhà cung cấp tính phí theo lượt; chi phí hosting/thiết bị vẫn độc lập.

Nguồn kỹ thuật: [Google + Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-10.0), [passkey .NET 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys?view=aspnetcore-10.0), [email reset](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-10.0).
