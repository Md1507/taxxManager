# Thiết kế tạm Web MVP 3.1

Ngày 24/11/2026 — bản bàn giao thiết kế trước mã Web, cho demo local bằng dữ liệu giả.
Người dùng chọn giao diện sáng, gọn, thiên về bảng số; sẽ gửi tham chiếu chi tiết sau.
Tài liệu mô tả thiết kế dự kiến, chưa phải bằng chứng UI/browser đã chạy.

## Hướng thiết kế và tokens

Trang làm việc nhập và đọc lương từng tháng. Bảng 12 tháng là trọng tâm; chữ tiếng Việt
dễ đọc, tiền thẳng cột, căn cứ ngay tại kết quả. Nội dung căn trái; số tiền căn phải.
Không cần ảnh, biểu đồ, hero hay hiệu ứng trang trí cho luồng này.

| Token | Giá trị | Vai trò |
|---|---|---|
| background | #F6F7F9 | Nền sáng trung tính |
| surface | #FFFFFF | Vùng biểu mẫu/kết quả |
| text | #17212B | Nội dung, label và số tiền |
| muted | #52606D | Chỉ dẫn, metadata; đường phân cách dùng sắc độ nhạt của màu này |
| accent | #1457A6 | Nút chính, link gạch chân và focus |
| error | #B42318 | Lỗi kèm chữ, không chỉ dùng màu |

Font `Segoe UI, Tahoma, sans-serif` có hỗ trợ tiếng Việt, không tải font từ mạng.
Body/input 16px, tiêu đề 24px/32px, line-height 1.5; tiền dùng `tabular-nums`.
Khoảng cách 8/16/24px; nội dung tối đa 1120px, gutter desktop 24px/mobile 16px;
controls và vùng bấm ? tối thiểu 44px, bo góc nhẹ 6px, không cần shadow.
Focus-visible viền accent 2px có khoảng cách, không bị vùng sticky che.
Đặt tokens CSS tập trung và tách partial nhập/tháng/kết quả/căn cứ để dễ đổi diện mạo.

## Wireframe desktop

```text
+---------------------------------------------------------------------------+
| Thuế thu nhập từ lương                         Nhập liệu | Kết quả          |
| Demo local — chỉ nhập dữ liệu giả. Kết quả chỉ mang tính tham khảo.         |
|                                                                           |
| Lương 12 tháng      Năm [2026 v]                     [Dùng dữ liệu mẫu]     |
| Tiền (đồng): nhập không phân nhóm; phần lẻ dùng dấu phẩy.                   |
| Tháng | Lương chịu thuế  | Bảo hiểm nhập tay | Thuế đã khấu trừ             |
| 01    | [            ]  | [              ] | [              ]             |
| 02 đến 12: cùng cấu trúc, label riêng cho từng tháng                        |
|                                                                           |
| Người phụ thuộc (tùy chọn): mã giả | Từ tháng | Đến tháng                   |
| [P1]                                [01 v]    [12 v]       [Bỏ dòng P1]    |
| [Thêm người phụ thuộc]                                  [Tính 12 tháng]    |
|                                                                           |
| Kết quả từng tháng — chỉ xuất hiện sau khi đủ 12 tháng thành công          |
| [Tháng 01: thuế từ snapshot] [Mở chi tiết]                                 |
|   Bước tính                                      Số tiền               ?  |
|   Lương / Bảo hiểm / Giảm trừ / Thu nhập tính thuế / Thuế                  |
|   Bậc | Khoảng | Thuế suất | Phần chịu thuế | Thuế từng bậc             ?  |
|   [Căn cứ nội tuyến: 2–3 câu, trạng thái, rule/version/kỳ, link nếu có]     |
| [Tháng 02 đến 12: mở chi tiết riêng]                                       |
+---------------------------------------------------------------------------+
```

Thứ tự đọc là nhập trước, kết quả sau. Link điều hướng là anchor trong trang, không
đưa tiền, dữ liệu form hoặc profile/history ID vào URL. Không thêm tổng quyết toán năm.
Thuế đã khấu trừ hiển thị riêng để đối chiếu, không tham gia công thức tính thuế tháng.

## Wireframe mobile

```text
+-------------------------------------+
| Thuế thu nhập từ lương               |
| Demo — chỉ dữ liệu giả / tham khảo   |
| [Tới nhập liệu] [Tới kết quả]         |
| Năm [2026 v]  [Dùng dữ liệu mẫu]      |
| Tháng 01                            |
| Lương chịu thuế (đồng) [           ] |
| Bảo hiểm nhập tay     [           ] |
| Thuế đã khấu trừ      [           ] |
| Tháng 02 đến 12: xếp dọc tương tự    |
| Phụ thuộc: P1 [Từ tháng] [Đến tháng] |
| [Thêm người phụ thuộc]              |
| [Tính 12 tháng]                     |
| Kết quả từng tháng                  |
| [Tháng 01 — Mở chi tiết]            |
| Bước tính             Số tiền   [?] |
| Căn cứ mở ngay dưới dòng đã chọn    |
| Bậc: nhãn/giá trị xếp dọc           |
| [Tháng 02 đến 12 — Mở chi tiết]     |
+-------------------------------------+
```

Ở khoảng dưới 768px, mỗi tháng nhập thành fieldset dọc, vẫn đủ label và cùng thứ tự
DOM. Chi tiết bậc đổi sang nhóm nhãn/giá trị, không ép cả trang cuộn ngang.
Kiểm ở viewport 390px và 320px, zoom 200%; không khóa zoom hoặc che field đang focus.

## Trạng thái và hành vi

| Trạng thái | Người dùng nhìn thấy và thao tác |
|---|---|
| Chưa tính | Đủ 12 tháng, chỉ dẫn nhập hoặc dùng mẫu; chưa có kết quả. Tháng không có lương vẫn nhập 0, không ngầm coi ô thiếu là 0. |
| Dữ liệu mẫu | Nút điền bộ synthetic rõ ràng, không ghi DB hoặc tính trước khi gửi; nếu thay dữ liệu đang sửa thì xác nhận ngay tại thao tác. |
| Nhập | Tiền là text/inputmode decimal, ví dụ `30000000,25`; không dấu phân nhóm trong ô. Parse exact phía server với quy ước công khai, báo lỗi dạng mơ hồ/không biểu diễn chính xác, không làm tròn. Hiển thị tiền từ decimal phía server theo vi-VN, giữ phần lẻ; không chuyển qua JS Number/float. |
| Phụ thuộc | Không thu tên/CCCD/MST; mã giả theo dòng, từ/đến tháng gồm cả hai đầu trong năm chọn. Chuyển sang dependent intervals của Data, từ chối khoảng sai; không nhận IDs hồ sơ tùy ý. |
| Đang gửi | Giữ input, báo “Đang tính…” khi có enhancement; chặn gửi lặp trong request. Không tự retry khi kết quả commit chưa rõ. |
| Sai dữ liệu | Tổng hợp lỗi tiếng Việt liên kết từng field, lỗi sát ô, aria-invalid/describedby. Giữ giá trị, focus lỗi đầu khi gửi sai; ví dụ “Tháng 03: nhập số tiền không âm.” Không đưa raw input vào log. |
| Kỳ/field không hỗ trợ | Báo rõ không tính được; không lấy kỳ gần nhất hoặc bỏ field. Năm chỉ hiển thị 2025/2026 khi catalog hiện hỗ trợ; POST giả mạo vẫn bị từ chối. |
| Đủ kết quả | Chỉ hiện thành công khi đủ 12 snapshot, chi tiết từng bước và từng bậc lấy nguyên từ snapshot đã dùng. Thuế đã khấu trừ là dữ liệu đối chiếu riêng. |
| Lỗi tính/lưu | Nêu chưa hoàn tất 12 tháng, giữ form để sửa; không trình bày kết quả một phần như thành công. Commit chưa rõ thì báo trạng thái chưa xác định, không hứa rollback hoặc hướng dẫn gửi lại mù. |
| Căn cứ đóng/mở | ? có tên “Căn cứ [tên kết quả], tháng [tháng]”, aria-expanded/controls. Enter/Space mở/đóng panel nội tuyến; Escape đóng và trả focus về nút. Panel có thể đọc và tới link bằng Tab, không focus trap. |
| Căn cứ thiếu/chưa xác minh | Giữ literal Missing/Unverified/Verified từ metadata, kèm nhãn Việt. Thiếu nguồn nói thiếu, không tạo link; link hợp lệ nhưng chưa đối chiếu vẫn ghi chưa xác minh. |

Panel căn cứ dùng 2–3 câu ngắn ghép từ LegalBasis thực của đúng amount/portion,
đúng rule/version/kỳ tính và registration trong snapshot; ngày hiệu lực văn bản trình bày
riêng khi có. Hiển thị trạng thái/ngày xác minh nguyên trạng, không suy Verified thành
thẩm định pháp lý hoặc References thành Official/kiểm tra tự động của ADR0017.
Chỉ link URL metadata hợp lệ qua bộ lọc nguồn/HTTPS an toàn; không ghép ItemID,
không bịa giải thích luật hay evidence. Razor escape cả nội dung và URL, không raw HTML.

Disclaimer hiển thị trước form và tại kết quả: “Kết quả chỉ mang tính tham khảo,
không thay thế tư vấn thuế chính thức.” Cảnh báo demo/dữ liệu giả luôn rõ ràng.
Không đưa thông tin triển khai khóa/DB/OperationId vào bước nhập thông thường.

## Đối chiếu trước triển khai và bàn giao kiểm chứng

Đã đối chiếu thiết kế với frontend-design: trọng tâm là bảng 12 tháng và căn cứ tại
điểm đọc số, palette sáng theo lựa chọn người dùng; bỏ thẻ thống kê/hero để dành chỗ
cho dữ liệu. Theo aspnet-core, dùng Razor Pages/partials và handler mỏng gọi service
đã có. Theo web-design-guidelines, ưu tiên HTML ngữ nghĩa, label riêng từng ô, skip
link tới main, heading có thứ bậc, table caption/header, button cho hành động, link
cho điều hướng; thông báo động có aria-live phù hợp, trạng thái có chữ và focus rõ.
Form server-rendered là nền; enhancement chỉ hỗ trợ tương tác, không tính thuế,
lưu nháp browser hay đồng bộ dữ liệu nhạy cảm vào URL. Nội dung căn cứ dài được wrap,
không cắt mất điều khoản hoặc link; motion mặc định không có, tôn trọng reduced-motion.

Sau triển khai phải kiểm app thật bằng Orca browser theo orca-cli: desktop 1280px,
mobile 390px/320px, zoom 200%, Tab/Shift+Tab qua form, lỗi và kết quả, Enter/Space/?
mở/đóng panel, Escape và quay focus; so label/thứ tự đọc và không cuộn ngang toàn trang.
Ghi lệnh, viewport, thao tác, kết quả quan sát và screenshot thật vào docs/web-mvp.md;
chỉ gọi là kiểm tra viewport mobile nếu chưa dùng thiết bị thật. Chưa chạy ở b-req.
