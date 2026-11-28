# Đ6b — thông báo và xác nhận dữ liệu thử

28/11/2026, theo contract `docs/design/local-data-use.md`. Tiếp Đ6a; chỉ local synthetic, không dùng dữ liệu thật/qua mạng. Giữ toàn bộ thay đổi chặng 5/6/Đ6a chưa commit và không sửa AGENTS.md do phiên khác cập nhật.

## Kết quả

Đăng ký có nội dung và checkbox không chọn sẵn, được kiểm ở server. Tạo user và claim xác nhận trong cùng transaction registry; không đổi schema/migration thuế hoặc registry. Bản ghi lưu phiên bản `local-demo-2026-11-28` và thời gian UTC server qua UserManager. Tài khoản cũ thiếu xác nhận phải đi qua `/tai-khoan/du-lieu-thu` trước khi mở dữ liệu; GET/HEAD chuyển trang, POST dữ liệu trả 403. Gate đọc claims trong registry mỗi request, không tin bản trong cookie hoặc userId/version/time POST. URL quay về chỉ local và loại trang tài khoản để tránh vòng lặp. Đăng xuất vẫn dùng được khi chưa xác nhận.

Tài khoản đã xác nhận có thể đọc lại nội dung và thời điểm từ trang tài khoản. Gửi xác nhận lặp không đổi thời điểm. Chưa có thao tác rút đồng ý, xuất hoặc xóa toàn bộ tài khoản; không dựng nút giả hoặc coi đây là nghiệm thu pháp lý. Bước tiếp theo là phạm vi xuất dữ liệu tài khoản, rồi xóa với vòng đời kho/bản sao rõ ràng.

## Kiểm thử

Lượt tập trung đầu **22/22** qua (`%TEMP%/taxmanager-data-use-focused.log`): 11 account cũ + 11 ca xác nhận. Sau đó bổ sung một ca ép lỗi ghi claim bằng trigger chỉ trong fixture để kiểm rollback tài khoản/claim và retry. Không sửa test đóng băng, không giảm assertion bảo mật cũ; helper đăng ký gửi thêm checkbox, test duplicate vẫn thực sự đi qua nhánh trùng tài khoản.

12 ca mới bao phủ: checkbox thiếu/false, không tạo user/claim; phiên bản/giờ server; duplicate không thêm claim; lỗi ghi hoàn tác; tài khoản cũ bị chặn GET/HEAD/POST các route dữ liệu; A xác nhận không mở quyền cho B; CSRF; đọc lại qua host mới cùng root; gửi lặp; gỡ claim chặn cookie cũ; claim lỗi/cũ/tương lai; URL ngoài/trang tài khoản/URL tilde/encoded không tạo redirect loop; đường quay về local giữ query/fragment.

`bint test affected --run` không thực thi do widening full và thiếu `eval.test_command`; giữ cấu hình, dùng script full theo [k:01M8F671GP7FD4E6G4CVEW6E7R]. Log `%TEMP%/taxmanager-data-use-affected.log`, selection `taxmanager-data-use-selection.json`. Cổng cuối `scripts/dev/verify-web-mvp.ps1` ghi `taxmanager-data-use-full.log`; kết quả hoàn tất cập nhật ở cuối báo cáo.

## Visual và giới hạn

Áp dụng frontend-design, ui-ux-pro-max và Hallmark trong hệ đã chốt: Inter, nền xám pha xanh, panel trắng, không divider/disclosure. Một primary trên mỗi màn; checkbox bằng chữ, lỗi inline + một link sửa, icon mật khẩu giữ nguyên. Nội dung chính 16 px, metadata 14 px. Review finish trong phiên Codex, không có reviewer độc lập.

Orca vẫn từ chối chứng chỉ development HTTPS. Tải HTML response thật sau login qua HTTPS từ tài khoản QA synthetic mới, phục vụ bản xem inert cùng CSS/font/JS ở 5092; không hạ HTTPS/trust store. Để tái hiện tài khoản cũ, chỉ gỡ claim của đúng tài khoản QA vừa tạo trong registry synthetic, rồi capture GET/POST lỗi/xác nhận/đọc lại. Không sửa dữ liệu tài khoản khác. HTTP flow thật xác nhận xong quay về `/ho-so`; browser mirror chỉ chứng minh bố cục và tương tác không gửi server.

Đã xem ảnh 375 px `5f17810f-fba8-48ed-9c1e-63e67b9d7e64` và lỗi 320 px `8b623cfb-b240-4f72-8791-c3367fed625b`, trong `%TEMP%/orca-browser-screenshots/` có thể hết hạn. Không tràn body, một primary, checkbox chưa chọn; Space chọn checkbox, Tab tới primary; lỗi focus summary, Tab/Enter đưa tới AcceptDataUse. Sau ảnh đầu tăng nội dung chính từ 14 lên 16 px theo scale hệ thống; ảnh xác nhận ghi ở cuối.

Hallmark `%TEMP%/taxmanager-data-use-hallmark.log`: static FAIL 56 (sidebar và thead ở scroll context riêng) cùng WARN F7 (recipe native control dùng chung), giữ lý do đã đối chiếu ở báo cáo Đ6a. Native render không chạy vì thiếu puppeteer-core; không tuyên bố lint sạch. Chưa kiểm screenreader, zoom native hoặc login browser tin chứng chỉ. `node --check workspace.js` và diff whitespace qua, hai cảnh báo xUnit2031 cũ vẫn ở ForecastPersistenceBlackBoxTests:241/243.

Tham chiếu kỹ thuật đã kiểm: [UserManager.AddClaimAsync .NET 10](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.addclaimasync?view=aspnetcore-10.0). Đây là API ghi claim của Identity; lựa chọn dùng nó để ghi nhận là quyết định của prototype.

## Kết quả cuối

`scripts/dev/verify-web-mvp.ps1` **exit 0**, **711/711** (Core 209, Data 328, Web 174), EF không pending model changes, log `%TEMP%/taxmanager-data-use-full.log`. Ca lỗi ghi claim/rollback mới cũng qua. Giữ hai cảnh báo xUnit2031 cũ. Không có cổng affected thành công vì thiếu cấu hình full, không tự thay cấu hình.

Ảnh xác nhận sau tăng chữ chính lên 16 px: mobile 375 `ad230cdd-e821-4081-9a34-db49c344fda7`, desktop đọc lại 1440 `7358a20c-e5bb-4010-a48c-4c2471a24a21` đã xem. Không overflow; desktop main rộng 512 px, một primary, trạng thái đã xác nhận không hiện checkbox để không tạo cảm giác có thể bỏ chọn và rút đồng ý. CSS cuối chỉ đổi kích thước nội dung notice; không đổi hành vi đã kiểm. Trình duyệt vẫn dùng bản xem inert vì chứng chỉ; HTTP fixture và capture flow thật kiểm phần server riêng.
