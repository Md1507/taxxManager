# Đ6a — tài khoản và kho synthetic riêng

28/11/2026. Bước kiểm chứng đầu tiên của PLAN 3.2 và UI Đ6, không phải chuyển demo thành hệ thống vận hành. Giữ đường demo cũ; chỉ bật tài khoản bằng `SyntheticAccounts=true` cùng `SyntheticDemo=true`, Development và loopback. Chế độ tài khoản chỉ nhận HTTPS.

## Ranh giới và dữ liệu

- ASP.NET Core Identity/EF lưu tên đăng nhập và password hash trong SQLite riêng dưới kho synthetic gốc; không thu email, CCCD hoặc MST. Không tự viết hash hay xác thực cookie. Mật khẩu 8–128 ký tự (đổi min từ 12 xuống 8 theo yêu cầu ngày 28/11/2026), không chỉ gồm khoảng trắng; cho paste/password manager, không phục hồi mật khẩu trong bản thử này. Form đăng ký kiểm điều kiện và khớp mật khẩu ngay khi nhập; máy chủ vẫn kiểm độc lập. Không áp dụng độ dài mới cho bước đăng nhập tài khoản đã có hoặc mật khẩu bảo vệ tệp sao lưu.
- Mỗi Identity user giữ đường dẫn kho synthetic riêng do server tạo. Tài khoản mới không nhận hồ sơ trong kho demo cũ. Các màn hồ sơ, tháng, lịch sử, báo cáo/CSV, kịch bản, đối chiếu và backup đều resolve kho từ subject đã xác thực; ID trong URL/form không chọn chủ sở hữu hoặc root.
- Không thêm owner giả vào mỗi query rồi bỏ sót route: dùng kho và khóa riêng cho tài khoản trong bước thử local. Không đổi migration thuế cũ. Không có admin, chia sẻ hoặc chuyển chủ sở hữu. Đây là quyết định cho prototype; triển khai qua mạng cần thiết kế vận hành và review riêng.
- Selection/preview khôi phục được định danh bằng subject + cookie trình duyệt. Token form ghim subject và generation; đổi tài khoản hoặc đổi kho không được gửi form cũ. Backup chỉ gồm kho thuế của tài khoản hiện tại, không gồm registry Identity, hash mật khẩu hoặc dữ liệu người khác. Nhập backup là người dùng chủ động đưa dữ liệu vào kho riêng; không merge vào kho khác.
- Cookie Identity Secure/HttpOnly/SameSite, hết hạn phiên ngắn; logout chỉ POST có antiforgery. Login lỗi chung, có lockout; returnUrl chỉ local. Identity security stamp được kiểm ở request. Key cookie vẫn ephemeral của demo, restart buộc đăng nhập lại; tài khoản và kho giữ bền để mở lại tường minh.

## Bố cục trước code

Giữ Inter, brand xanh và token hiện có. Màn xác thực không có sidebar hồ sơ khi chưa đăng nhập; logo nhỏ phía trên, nền xám pha xanh, panel trắng có chiều rộng đọc khoảng 28rem, không khung đậm/divider hoặc hình trang trí.

```text
TaxManager                                      Demo local

Đăng nhập
Mở kho dữ liệu thử của bạn.
Tên đăng nhập          [                           ]
Mật khẩu              [                           ]
                      [ Đăng nhập ]
Chưa có tài khoản? Tạo tài khoản thử
```

Đăng ký cùng cấu trúc với mật khẩu nhập lại và hướng dẫn ngay dưới trường. Một nút đặc “Tạo tài khoản thử”; “Đăng nhập” là link. Lỗi tập trung có focus và lỗi cạnh trường; không đưa lại mật khẩu vào HTML. Sau tạo tài khoản quay về đăng nhập, không tự đăng nhập để tránh giữ token anonymous cũ. Trang tài khoản sau đăng nhập có tên và nút phụ đăng xuất; các chức năng đồng ý/xuất/xóa tài khoản chưa có không dựng nút giả.

## Kiểm chứng tối thiểu

1. Anonymous bị chặn ở mọi trang dữ liệu; chỉ login/register và static assets công khai. HTTP bị từ chối trong mode tài khoản; cookie an toàn, POST thiếu/sai CSRF bị từ chối, redirect ngoài bị bỏ.
2. Hai tài khoản A/B: list chỉ thấy của mình; B GET các route tháng/năm/history/report/CSV/scenario/reconciliation theo ID A không có dữ liệu; POST sửa/xóa/tính năm/so sánh/đối chiếu ID A không đổi dữ liệu A. Sao lưu của B không chứa hồ sơ A; preview/selection không theo cookie cũ sang user khác.
3. Form cũ của A không submit được sau logout/login B kể cả cùng browser cookie. Logout GET không có tác dụng, cookie bị sửa/security stamp cũ bị từ chối. Login sai đủ lần bị lockout.
4. Host mới, cùng root: login lại và chỉ mở đúng kho cũ; không thay khóa/kho lỗi bằng kho trắng. Demo không tài khoản giữ nguyên hành vi và test.
5. Kiểm ảnh desktop/mobile, lỗi và Tab/Enter/paste; chạy affected rồi cổng đầy đủ nếu widening không chạy được. Review bảo mật độc lập, dữ liệu thật, email/reset/MFA, đồng ý/xuất/xóa tài khoản, triển khai HTTPS vận hành còn ngoài Đ6a.

Tham chiếu kỹ thuật: [Identity configuration .NET 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0). Contract được ghi trước triển khai, không tự đóng toàn bộ Đ6.
