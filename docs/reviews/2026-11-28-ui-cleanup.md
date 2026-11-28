# Dọn chi tiết UI sau khi duyệt báo cáo

Phạm vi: toàn bộ màn hiện có. Áp dụng 11 mục đã duyệt; giữ chức năng, nội dung cần quyết định, palette và giọng văn. Các thay đổi tài khoản, xóa hồ sơ và nhập tháng đang có trong workspace được giữ nguyên.

## Đối chiếu trước/sau

Đếm ở cùng hồ sơ synthetic, desktop 1440×1000, trừ dòng ghi rõ mobile. Hồ sơ mẫu có hai khoản thưởng; trạng thái sửa tháng là tháng 6. “Lớp nền” chỉ tính nền nhóm nhìn thấy; “cấp HTML” tính cả fieldset trong suốt.

| Chi tiết | Trước | Sau | Thiết bị giữ hoặc thay thế |
|---|---:|---:|---|
| Đoạn giải thích gross/net | 3 | 1 | Một đoạn định nghĩa và chỉ dẫn tìm gross; không thêm panel |
| Nhắc định dạng tiền, hồ sơ mới | 2 | 1 | Hướng dẫn chung trong form; đơn vị và ví dụ giữ tại từng ô |
| Nhắc định dạng tiền, hồ sơ mẫu đang sửa tháng 6 | 6 | 1 | Các ô cùng tham chiếu `money-format-help`; mô tả lỗi riêng vẫn giữ |
| Lời nhắc trống/0 cạnh gross | 2 | 1 | Quy ước chung phân biệt thiếu và giá trị 0 |
| Dòng metadata trước ô nhập | 2 | 1 | Phạm vi và trạng thái lưu cùng đoạn dưới h1; span live status vẫn hoạt động |
| Legend giai đoạn đầu hiển thị | 1 | 0 | h2 xác định phần nhập; legend còn cho công nghệ hỗ trợ |
| Vị trí ô nhập đầu, 390×844 | y=497px | y=418px | Giảm lặp thứ bậc, dùng khoảng cách 8/24px |
| Nhóm có nền, hồ sơ mới | 7 | 5 | Bỏ nền fieldset trong panel thưởng/người phụ thuộc |
| Nhóm có nền, hồ sơ mẫu | 9 | 6 | Giữ nền panel ngoài; khoảng cách 16px chia nhóm bên trong |
| Cấp nền lồng tối đa trong form hồ sơ | 2 | 1 | Nền trong suốt và padding 0 cho fieldset con trong panel |
| Cấp container HTML tối đa trong form hồ sơ | 3 | 3 | Giữ fieldset ngữ nghĩa; không báo giảm DOM khi chỉ đổi nền |
| Viền ngoài bảng chi tiết tháng | 2 | 0 | Mặt bảng và đường phân hàng; giữ cuộn/focus/sticky |
| Viền ngoài bảng báo cáo | 1 | 0 | Như trên; margin 16px, radius 0 |
| Tín hiệu mục menu đang chọn | 4 | 2 | Giữ nền tint và weight 600; bỏ viền trái có màu và chữ xanh |
| Vai trò màu brand trên toàn UI | 9 | 6 | Link, nút, focus, nhận diện, lựa chọn, tóm tắt kết quả năm |
| Màu xanh/đỏ gán cho chênh lệch nguồn | 2 | 0 | Dùng màu chữ, giữ dấu cộng/trừ và lời giải thích |
| Đoạn ghi chú liên tiếp ở kết quả kịch bản | 4 | 2 nhóm, vẫn 4 đoạn | Nhóm dữ liệu/khấu trừ và phạm vi/thời điểm, cách 24px; trong nhóm 8px |
| Lời nhắc lại nhãn mật khẩu ở màn xuất | 1 | 0 | Nhãn “Mật khẩu hiện tại” và nút tải đã đủ chỉ hành động |
| Cảnh báo JSON không được bảo vệ | 14px, muted | 16px, text | Nâng mức đọc của nội dung quyết định việc lưu giữ tệp |
| Disclosure `?` cho căn cứ trong kết quả tháng | 168 | 0 | 12 khối căn cứ có tên, mỗi tháng một khối |
| Disclosure mở kết quả tháng | 12 | 12 | Giữ khả năng chọn tháng cần đọc |
| Disclosure metadata nguồn theo tháng | 0 | 12 | Chỉ gom mô tả gốc/phiên bản; trạng thái và điều khoản giữ bên ngoài |
| Nhóm căn cứ nguồn trong mẫu 2025 | 168 lần trình bày riêng | 48 nhóm | Gom căn cứ bằng nhau; giữ danh sách chỉ tiêu/bậc áp dụng và metadata |

Số vai trò màu được đếm lại trên toàn UI: còn **6**, vì phần tóm tắt kết quả năm vẫn dùng nền brand. Không tính việc đổi riêng số tiền kịch bản thành màu chữ là đã xóa vai trò nhấn kết quả ở mọi màn.

## Kiểm kê sau sửa

- **Phân tách:** không thêm bóng, fieldset viền hay viền nhấn một bên. Panel ngoài giữ nền trắng, khoảng cách và tiêu đề chia nội dung; bảng giữ đường phân hàng. Các cảnh báo/lỗi hiện có giữ nguyên thiết bị riêng. Đối chiếu số nhóm và viền ở bảng trên.
- **Tương tác:** nút/menu/thẻ giữ điểm bấm và không có gạch chân mặc định. Link văn bản để điều hướng hoặc mở văn bản nguồn vẫn giữ. Không thêm mũi tên cho nút. Mỗi màn/trạng thái đã kiểm có một nút chính; nhóm form hồ sơ có 1 nút chính, 4 nút viền, 12 nút chữ chọn tháng và 1 disclosure ở trạng thái mới.
- **Màu:** giữ **12 mã màu**: `#395B40` brand; `#E7FFEC` tint; `#F3F6F3` nền; `#FFFFFF` mặt nhóm; `#1A1F1B` chữ; `#526057` chữ phụ; `#76867B` viền; `#246647` hoàn tất/thực tế; `#895600` cảnh báo/giả định; `#B02A37` lỗi; `#365C85` dự kiến; `#DDE5DE` phân hàng. Số 13 trong kiểm kê cũ là đếm nhầm; alias focus/on-brand không phải mã mới. Trạng thái tiếp tục có chữ và hình thức riêng ngoài màu.
- **Chữ:** giữ Inter, 6 cỡ 13/14/16/20/24/32px; các độ đậm 400/500/600/700 hiện có. Ghi chú dùng text-muted trên nền surface/bg, không giảm tương phản. Không thêm mono, nhãn in hoa giãn chữ hoặc số thứ tự trang trí.
- **Container:** độ sâu HTML tối đa của hồ sơ vẫn 3, độ sâu nền nhóm giảm còn 1. Các nhóm trong suốt giữ legend và controls.
- **Spacing:** giữ bộ 0/4/8/12/16/24/32/48px. Đã dùng 8px cho thông tin cùng nhóm và 24px giữa hai nhóm ghi chú kịch bản; không đặt một khoảng cách cho mọi quan hệ.
- **Trang trí/chuyển động:** không thêm gradient, glow, animation, zoom hover hoặc hiệu ứng nảy. Khối căn cứ cuối cùng có 48 icon trạng thái cho mẫu 2025 (mỗi nhóm nguồn một icon), kèm nhãn trạng thái bằng chữ; không dùng icon làm khung trang trí. Các icon chức năng và focus có sẵn được giữ.

## Mã đã đổi

`MoneyFieldTagHelper` trỏ hướng dẫn chung, không sinh đoạn ghi chú sau mỗi ô. Partial `_MoneyFormatHelp` được đặt một lần trong từng form forecast, kịch bản và đối chiếu. `_SalaryTerms` dùng một đoạn chung cho cả forecast và công cụ tháng.

`Forecast.cshtml` gộp metadata và giữ status cập nhật qua JavaScript. `ForecastScenarios.cshtml` nhóm ghi chú theo vai trò. `Accounts/Export.cshtml` bỏ lời nhắc lặp, bỏ ID tham chiếu tương ứng và nâng cảnh báo. `Index.cshtml` gom căn cứ theo `LegalBasis`, `_Basis` trình bày chỉ tiêu, trạng thái và điều khoản trực tiếp; `_BasisProvenance` giữ mô tả/phiên bản nguồn theo tháng. Khi các bậc dùng cùng căn cứ, nhãn chung là “Biểu thuế tháng”; nếu khác căn cứ, giữ khoảng tiền và thuế suất tương ứng. `_LegalSources` tiếp tục chỉ hiện mỗi URL một lần trong tháng. CSS dùng các token có sẵn.

AGENTS.md được bổ sung quy tắc tránh ghi chú lặp/lồng bên ngoài khối Bint quản lý. Quy tắc hyperlink có chủ đích trước đó được giữ.

## Kiểm chứng

- `bint test affected --run` **không chạy được**: bgraph nới full vì các ảnh review và `.hallmark` chưa có ánh xạ; `eval.test_command` chưa cấu hình. Không sửa cấu hình hoặc bỏ ảnh để thu hẹp scope.
- Đã chạy `scripts/dev/verify-web-mvp.ps1` và kiểm lại cùng chuỗi build/Core/Data/Web/EF ở cấu hình Release sau thay đổi cuối. **209 Core + 332 Data + 185 Web = 726 test qua**; EF báo không có thay đổi model chưa migration. Cấu hình Release tránh `testhost` của phiên khác đang khóa DLL Debug, không giảm phạm vi test. Có hai cảnh báo xUnit2031 sẵn ở `ForecastPersistenceBlackBoxTests`.
- Test accessibility giữ mô tả gross, lỗi server và hướng dẫn tiền chung có đúng một ID. Test căn cứ giữ trạng thái, mã nguồn, mã hóa HTML, loại URL không an toàn, liên kết duy nhất và ánh xạ chỉ tiêu với căn cứ.
- Sau lượt full, nhãn tiêu đề căn cứ được tách khỏi nhãn ánh xạ chi tiết trong workspace; đã build lại và chạy riêng **2 test LegalCitations, đều qua**, rồi cập nhật bản publish xem thử. Khoảng tiền/thuế suất của các bậc dùng nguồn khác nhau vẫn giữ trong metadata tương ứng.
- Orca kiểm inventory 13 route ở 1440×1000. Kiểm 320px ở hồ sơ mới, hồ sơ sửa tháng 6, chi tiết tháng, báo cáo, kịch bản, đối chiếu, sao lưu, khôi phục và danh sách hồ sơ: không tràn ngang, không thiếu đích `aria-describedby`.
- Xem trực tiếp ảnh hồ sơ ở 390/320px và khối căn cứ ở desktop; focus bàn phím có outline solid 2px. Enter trong ô gross gọi `handler=Preview`, giữ giai đoạn thay đổi. Không ghi hồ sơ mẫu trong phép kiểm Enter.
- Màn xuất tài khoản được render mới qua host tài khoản có xác thực bằng fixture synthetic, rồi xem bản HTML ở 320px: cảnh báo 16px/text, không còn nhắc mật khẩu lặp, không còn tham chiếu đã xóa. Đây là kiểm render; hành vi xác thực/xuất tệp được kiểm bằng test HTTP.

Giữ hợp đồng nhập tiền nullable/định dạng tường minh [k:01M8BMXR829S6QRCBBJWYSB8SF], định dạng cả trường tháng ẩn [k:01M8F49W9Q99372V01ZZBZV6NK], submit Preview đầu tiên [k:01M8G33DP7ZSB0MXMQ9PV1A68A], thứ tự section theo sidebar [k:01M8G5SHPRAGW03VQ3252DERKG] và lưới nằm bên trong fieldset tháng [k:01M8G8KGHF2YMZ0JMY6RZ1ZMV7].
