# Thiết kế dự báo thuế năm 3.1b

Thiết kế cho demo local, dành cho người làm công ăn lương chưa rành thuế. Phạm vi 3.1b được chốt ngày 25/11/2026 là nhập theo giai đoạn, ngoại lệ và xem trước trạng thái dữ liệu; tính nghĩa vụ, kịch bản và lưu hồ sơ để bước sau.

## Lựa chọn

Tiếp tục Razor Pages và CSS hiện có. Giữ nền #F6F7F9, mặt #FFFFFF, chữ #17212B, chữ phụ #52606D, nhấn #1457A6 và lỗi #B42318; Segoe UI/Tahoma hỗ trợ tiếng Việt, 16px/1.5, số canh phải dạng tabular. Bố cục một cột, giới hạn 1120px, khoảng cách 8/16/24px, điều khiển tối thiểu 44px và focus hiện rõ. Ưu tiên các trường có nhãn, ví dụ số trên phiếu lương, giải thích ngắn và phần nâng cao mở dần.

Truy vấn ui-ux-pro-max `salary tax calculator forms --design-system` trả phong cách tối giản phù hợp với công cụ nhập số, nhưng hero/CTA, bảng màu tối/cam và font tải mạng hướng về trang quảng bá nên không phù hợp demo hiện có; giữ tokens sáng đã được chọn. Truy vấn `error summary validation` phù hợp: giữ dữ liệu khi lỗi, báo lỗi cạnh trường và tóm tắt có thể nhận focus. `keyboard focus forms` phù hợp: focus rõ và không bị vùng cố định che. Áp dụng nguyên tắc frontend-design theo công việc nhập lương cụ thể, hướng dẫn Razor Pages của aspnet-core và web-design-guidelines cho nhãn, trạng thái, bố cục và bàn phím.

## Màn hình và trạng thái

Bộ chọn tháng dùng select và nút mở, chỉ một editor hiện tại hiển thị; danh sách tháng đã nhập cho phép quay lại. Các trường còn lại được giữ trong form khi chuyển tháng, thêm giai đoạn hoặc xem trước. Nút xóa chỉ xóa dữ liệu tháng đang chọn, khác với nhập số 0; thao tác yêu cầu xác nhận. Form và nút gốc hỗ trợ bàn phím, không phụ thuộc JavaScript để chuyển tháng. Giữ palette hiện tại theo kết quả truy vấn ở trên, không dùng mẫu hero cho công cụ nhập liệu.

```text
Dự báo thuế 2026 — demo dữ liệu giả, một nơi trả lương
Giai đoạn lương [từ tháng] [đến tháng] [gross] [xem trước]
12 tháng: giá trị và trạng thái | sửa ngoại lệ
Bảo hiểm / người phụ thuộc / khấu trừ [chi tiết]
Chưa tính thuế năm hoặc lưu hồ sơ
```

Khi trống, phân biệt chưa nhập và xác nhận bằng 0. Preview nêu ngoại lệ được giữ và các tháng bị giai đoạn sau thay thế. Lỗi giữ dữ liệu đã nhập; tiền và trạng thái có chữ, không dựa riêng vào màu. Bảng preview có nhãn từng ô khi xếp theo cột trên mobile. Tính nghĩa vụ, hai kịch bản, lưu/mở lại và QA riêng của chúng vẫn là thiết kế cho bước sau, chưa được triển khai trong 3.1b.

## Việc còn lại

- QA browser 3.1b sau bộ chọn tháng tại desktop, 320/390px và bằng bàn phím chưa có bằng chứng: ngày 26/11, `orca snapshot --page 78d43a34-2419-42d8-968e-0c44a5df8ce6 --interactive --json` trả `runtime_access_denied` (EPERM). Cần kiểm tra ngoài sandbox trước nghiệm thu. Test HTTP không thay thế kiểm tra bố cục và bàn phím.
- Cải thiện thông báo lỗi: hiện có tóm tắt và giữ input, nhưng chưa chuyển focus tới tóm tắt hay liên kết lỗi tới từng trường; đưa vào backlog UX.

## Rà luồng và sửa sau review

Trước sửa: người dùng nhập lương nền, thêm giai đoạn nhưng không bỏ được giai đoạn thừa; lỗi tại tháng ẩn không chỉ tháng; metadata luôn hiện dài; tiền có dấu phẩy bị model binder coi là dấu phân nhóm. Sau sửa: có nút bỏ từng giai đoạn và giữ các giá trị còn lại; chọn tháng hoặc dùng danh sách tháng đã nhập để sửa; metadata được nhóm trong phần mở rộng; lỗi tiền hiển thị cạnh ô, tháng lỗi được mở và tóm tắt gọi tên tháng/trường. Tiền chấp nhận dấu phẩy hoặc chấm thập phân theo cùng contract trang tháng, không nhận dấu phân cách hàng nghìn; giá trị không hợp lệ được giữ để sửa. Không có nút tính năm hoặc lưu hồ sơ.

Bằng chứng: test HTTP `Forecasting` 20/20 sau sửa, gồm tiền phần lẻ ở mọi nhóm, tiền sai, bỏ giai đoạn và mở tháng lỗi. QA trước sửa đã đo 320/390px không tràn, control hiển thị tối thiểu 44px; Tab/Enter chọn tháng 11 giữ gross. Sau sửa, ngày 26/11 lệnh `orca goto` trả `runtime_access_denied` / EPERM, vì vậy chưa kiểm lại bố cục, focus-visible, Tab/Enter/Space/Escape hay tương tác các phần mở rộng mới trên browser. CSS có `:focus-visible` nhưng đó không phải bằng chứng thao tác bàn phím. Cần QA lại desktop/mobile và focus sau POST ngoài sandbox trước nghiệm thu. Danh sách lỗi chưa có liên kết trực tiếp tới từng ô; cần hoàn thiện nếu QA xác định gây cản trở. Không có người dùng thử nên không kết luận về thị trường.

Lượt verify-9 trước thay đổi đã qua 204 Core, 300 Data, 42 Web và EF; verify-10 sau đó build qua, Core 204/204, Data 300/300 nhưng Web 42/43 do test tiền phần lẻ. Test tiền đó nay qua riêng và trong Forecasting; Bint phải chạy lại verify sau thay đổi. Các phát hiện review annual/Data cũ nằm ngoài 3.1b theo quyết định người dùng và cần reviewer đánh giá lại; không đánh dấu fixed. Phát hiện verify-8 khóa executable có verify-9 qua sau khi dừng server, không sửa script né test.

Sau review vòng 3: đặt một nút “Xem trước 12 tháng” ở đầu form, trước mọi nút thêm/xóa/chọn. Enter trong ô sẽ chọn submit mặc định là xem trước theo thứ tự DOM; nút xem trước cuối form vẫn thuận tiện sau khi nhập. Các nút sửa/xóa chỉ chạy khi được kích hoạt tường minh. Cần kiểm tra HTML render và POST thật, sau đó xác nhận Enter trên browser nếu môi trường cho phép.

Kiểm chứng vòng 3: test HTTP `Default_submit_previews_without_removing_a_period` đỏ trước sửa vì nút đầu tiên là bỏ giai đoạn, xanh sau sửa; POST preview giữ giai đoạn 40.000.000, nguồn synthetic và ngoại lệ 0. Toàn bộ test hẹp Forecasting trực tiếp 21/21. `orca goto` ngày 26/11 vẫn trả `runtime_access_denied` / EPERM, chưa kiểm Enter thực hoặc focus sau thay đổi này. `bint task check` check-28 lỗi khởi động sh.exe CreateFileMapping Win32 5; bg-6 thiếu SDK trong checkout tạm. Verify-11 thực tế chạy các lệnh dotnet test do graph sinh mà không tìm thấy SDK, không phải log chạy script verify; phiên điều phối cần chạy lại cổng thật với môi trường SDK đúng.

Sau review vòng 4: với gross nền chưa biết, xem trước các tháng chưa nhập và những tháng đã có ngoại lệ; không diễn giải thiếu thành 0. Lỗi khoảng tháng và lý do giả định của giai đoạn cần nằm cạnh đúng ô, nêu giai đoạn, giữ input và mở phần metadata chứa lỗi; cần QA browser sau khi sửa.

Review vòng 4: lỗi .11 và .12 được sửa; test HTTP `Preview_allows_unknown_base_salary_and_shows_known_months` và `Invalid_period_metadata_and_range_identify_fields_and_open_details` đều đỏ trước sửa và xanh sau sửa; cả nhóm Forecasting trực tiếp 23/23. Tháng không có gross nền vẫn hiển thị chưa nhập, số 0 bảo hiểm vẫn là 0; lỗi kỳ/lý do giả định có nhãn giai đoạn cạnh trường và mở metadata lỗi. Chưa QA browser các thay đổi mới; các giới hạn EPERM và kiểm focus/bàn phím nêu trên vẫn còn. Review vòng 4 không có phát hiện nghiêm trọng; các phát hiện annual/Data cũ được hoãn theo scope, không coi đã fixed.

Verify-12 thực tế là lệnh graph dotnet test không tìm được SDK; `. ./scripts/dev/activate-dotnet.ps1; dotnet --version` trong worktree trả 10.0.404. Check-29 gặp sh.exe CreateFileMapping Win32 5 trước khi chạy test. Không sửa script, harness hay guard để né lỗi; Bint cần chạy lại script đã xác nhận với môi trường SDK đúng.

Lượt kiểm tra tiếp ngày 26/11: verify-13 cũng dừng ở các lệnh graph `dotnet test` vì tiến trình không tìm thấy SDK, trước khi có kết quả test; log không chứng minh script verify đã chạy. Kích hoạt SDK trong worktree cho `dotnet --version` 10.0.404; chạy trực tiếp test HTTP Forecasting đạt 23/23 và SalaryScheduleTests đạt 3/3. `bint task check` dùng lại check-30 cũ thiếu SDK, không phải kết quả xanh mới. Cần chạy lại cổng Bint với SDK và cache được cấp cho tiến trình điều phối trước khi nghiệm thu.

Lần thử browser cùng ngày: `orca snapshot` vẫn trả `runtime_access_denied` / EPERM. Do đó chưa có bằng chứng browser sau các sửa mới cho focus, bàn phím và bố cục; cần QA ngoài sandbox, không suy kết quả từ CSS hoặc test HTTP. Review vòng 5 ghi Core 204/204, Data 300/300, Web 53/53 và EF không đổi khi chạy trực tiếp; đó không thay thế cổng Bint cuối.

## Kiểm tra lại 26/11 — giới hạn môi trường

Verify-14 gọi graph qua sh.exe và dừng vì không tìm thấy SDK 10.0.404; log không cho thấy script verify-web-mvp.ps1 đã chạy. Trong PowerShell, kích hoạt scripts/dev/activate-dotnet.ps1 rồi dotnet --list-sdks có 10.0.404 tại .dotnet/sdk. Cần phiên điều phối chạy lại cổng đã xác nhận với môi trường SDK phù hợp; không xem kết quả kiểm trực tiếp là cổng Bint đạt.

Orca snapshot lại từ TEMP vẫn trả runtime_access_denied (EPERM). Chưa có bằng chứng browser sau sửa cho focus-visible, focus sau POST, desktop/mobile và thao tác bàn phím; cần kiểm ngoài sandbox trước khi xác nhận AC3. Truy vấn ui-ux-pro-max keyboard focus forms nhắc kiểm focus nhìn thấy và không bị che; CSS hoặc HTTP không chứng minh được hành vi đó.

Lượt tiếp theo ngày 26/11: verify-15 cũng là graph gọi `dotnet test` qua sh.exe và không tìm SDK, không phải kết quả chạy script verify. Kích hoạt `scripts/dev/activate-dotnet.ps1` trong PowerShell cho `dotnet --version` 10.0.404; test trực tiếp lọc `Forecasting` đạt 23/23 và `SalaryScheduleTests` đạt 3/3. `bint task check` vẫn dùng lại check-32 bị `CreateFileMapping` Win32 5, không phải cổng xanh. `orca snapshot` chạy từ TEMP vẫn trả `runtime_access_denied` / EPERM; QA browser sau sửa và cổng Bint còn cần môi trường có quyền và SDK cho tiến trình điều phối.

## Sửa gross giai đoạn thêm — 26/11

Trước đây gross trống trong giai đoạn thay đổi chỉ tạo thông báo chung. Khi xem trước, ô này nay có lỗi tiếng Việt nêu đúng số giai đoạn, yêu cầu nhập gross hoặc bỏ giai đoạn; dữ liệu còn lại được giữ. Gross nền vẫn có thể để trống khi chưa biết, còn số 0 là một giá trị. Lỗi được gắn vào ô và vùng tổng hợp lỗi hiện có được đặt autofocus; cần kiểm focus thực trên browser, không suy từ HTML. Đây áp dụng hướng dẫn Forms của ui-ux-pro-max: lỗi cạnh trường, giữ input và tập trung vùng lỗi sau submit.

Test HTTP `Empty_added_period_gross_has_specific_inline_error_and_preserves_inputs` đỏ trước sửa, xanh sau sửa; nhóm `ForecastHttpTests` chạy PowerShell trực tiếp 24/24. Check-34 không chạy được sh.exe (CreateFileMapping Win32 error 5), không phải Bint check xanh. `orca snapshot` từ TEMP tiếp tục trả `runtime_access_denied` EPERM: chưa QA desktop/mobile, Tab/Enter và focus sau sửa. Cần chạy lại browser và cổng Bint ngoài sandbox; verify-17 và lần chạy script trực tiếp trước đó không chứng minh thay đổi mới. Những phát hiện annual/Data cũ vẫn ngoài phạm vi theo quyết định người dùng, chờ reviewer đánh giá lại, không đánh dấu fixed.
