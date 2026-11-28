# Đ6b — ghi nhận đồng ý dùng dữ liệu thử

28/11/2026. Tiếp Đ6a trong UI Đ6, phạm vi local synthetic. Bước này hoàn thiện thông báo và ghi nhận xác nhận; xuất/xóa toàn bộ tài khoản là bước riêng tiếp theo. Không xác nhận tuân thủ pháp luật hoặc cho phép dữ liệu thật/triển khai qua mạng.

## Bố cục và nội dung

Giữ hệ xanh/Inter/panel trắng. Đăng ký có phần giải thích ngắn trong form trước nút chính, checkbox không chọn sẵn. Tài khoản cũ thiếu xác nhận thấy màn “Dữ liệu thử của bạn”: nội dung dễ đọc, checkbox, một nút chính “Đồng ý và tiếp tục”, thao tác phụ đăng xuất. Không thêm divider/dropdown hoặc icon thay câu đồng ý.

Nội dung phiên bản `local-demo-2026-11-28`: chỉ nhập dữ liệu giả; tên đăng nhập và mật khẩu được dùng để xác thực, mật khẩu lưu dạng hash; số lương/nguồn/ghi chú/lịch sử được lưu local để mở lại/tính thuế; đăng xuất không xóa dữ liệu; tệp đã tải và bản sao bên ngoài ứng dụng không tự biến mất. Nêu xuất/xóa toàn bộ tài khoản chưa có, không dựng quyền hoặc thao tác giả. Không gọi đây là chính sách pháp lý đã thẩm định.

## Contract

- Ghi phiên bản và thời gian UTC từ server trong claim riêng của Identity (`taxmanager.local-data-use`); dùng bảng claims hiện có, không sửa schema thuế/registry hoặc migration cũ. Chỉ dùng UserManager cho thao tác ghi Identity.
- Đăng ký yêu cầu checkbox ở server. Tạo tài khoản và claim trong cùng transaction registry; thất bại không để tài khoản được tạo thiếu xác nhận. Kho được provision theo cơ chế Đ6a; commit không rõ kết quả thì giữ kho, không xóa mạo hiểm.
- Tài khoản cũ phải xác nhận trước khi truy cập route dữ liệu. Mỗi request kiểm claim trong registry, không tin riêng cookie hoặc trường phiên bản/thời gian/userId do browser gửi. Claim lỗi/phiên bản cũ không mở quyền; không tự sửa hoặc suy đồng ý.
- Chưa xác nhận: GET/HEAD dữ liệu chuyển màn xác nhận; POST dữ liệu trả 403 và không chạy handler. Static assets và các trang tài khoản vẫn dùng được để đọc nội dung/đăng xuất. Demo không tài khoản giữ nguyên.
- Xác nhận có CSRF, chỉ cho subject đang đăng nhập; returnUrl chỉ local GET, bỏ URL ngoài hoặc endpoint tài khoản để tránh vòng lặp. Gửi lại không sửa thời điểm đã ghi; không xóa bằng đăng xuất.

## Bằng chứng cần có

Checkbox thiếu/false không tạo user hoặc claim; claim có phiên bản/giờ server và tồn tại qua host mới; tài khoản cũ bị chặn ở mọi đường dữ liệu và POST không đổi hồ sơ; chỉ A xác nhận thì B vẫn chặn; CSRF/anonymous/returnUrl ngoài không vượt gate; claim bị gỡ hoặc lỗi chặn ngay cả cookie đã có. Kiểm lỗi giữ input tên, không echo mật khẩu, checkbox/link/focus/Enter ở mobile/desktop. Chạy affected và cổng đầy đủ theo quy ước repo.

Tham chiếu: [UserManager.AddClaimAsync .NET 10](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.addclaimasync?view=aspnetcore-10.0). Việc lưu claim là lựa chọn triển khai prototype; claim này không phải bằng chứng thẩm định pháp lý.
