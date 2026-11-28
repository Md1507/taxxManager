# Kế hoạch UI/UX TaxManager

Ngày lập: 27/11/2026; đồng bộ ngày 28/11/2026. Phạm vi: kế hoạch thiết kế và tiến độ triển khai UI trên các luồng hiện có.
Nguồn trạng thái: [roadmap](docs/product-roadmap.md); kế hoạch kỹ thuật: [PLAN.md](PLAN.md); vận hành: [Web MVP](docs/web-mvp.md).
Khi tài liệu thiết kế cũ khác trạng thái hiện tại, dùng roadmap và mã hiện tại để đối chiếu; không đánh dấu tính năng đã có chỉ vì có wireframe.
Nền 3.1/3.1b, chặng 2 và chặng 3 đã nghiệm thu kỹ thuật; phiếu chặng 3 đã done. UI Đ1–Đ2 trên các route hiện có đã triển khai, gồm định dạng tiền khi gõ và chú thích gross/net theo phản hồi ngày 28/11. Xem [kiểm chứng UI](docs/reviews/2026-11-28-ui-phases-one-two.md) và [kiểm chứng chặng 3](docs/reviews/2026-11-27-annual-stage-three.md).
Màn chi tiết tháng đã nối với bản forecast lưu theo ID/tháng/revision, có bước tính, bậc thuế và căn cứ; xem [kiểm chứng màn tháng](docs/reviews/2026-11-28-forecast-month.md). Đã bổ sung menu thu gọn và liên kết lỗi với ô nhập; thao tác phím, lưu tháng và reflow đã kiểm từng phần tại [báo cáo khả năng truy cập](docs/reviews/2026-11-28-ui-accessibility.md). Còn nghiệm thu UX thủ công đầy đủ; dark mode hiện là hướng tham khảo từ ảnh người dùng, chưa triển khai.
Quy tắc phối hợp: roadmap quyết định phạm vi/thứ tự/tiến độ; PLAN quyết định điều kiện kỹ thuật; file này quyết định bố cục, token và hành vi UI. Khi triển khai, cập nhật cả ba liên kết/trạng thái trong cùng tác vụ, không lấy checkbox UI thay nghiệm thu nghiệp vụ.

## 1. Mục tiêu và giả định về người dùng

- Giúp cá nhân tự giữ số liệu lương, kiểm tra phép tính và chuẩn bị thông tin quyết toán.
- Người dùng đầu tiên là người làm công ăn lương, chưa cần hiểu thuật ngữ thuế chuyên sâu.
- Phạm vi hồ sơ đầu: cá nhân cư trú, một nơi trả lương, năm 2026, bảo hiểm nhập tay.
- Công cụ tính tháng hiện có vẫn giữ lựa chọn 2025/2026; không suy hồ sơ dự báo hỗ trợ cả hai năm.
- Ưu tiên nhập và rà soát trên máy tính; điện thoại phải mở, sửa tháng và đọc kết quả được.
- Hình ảnh bình tĩnh, rõ ràng, đáng tin; giống tài liệu có cấu trúc, ưu tiên đọc số và tính nhất quán.
- Hiện chỉ thử local bằng dữ liệu giả; tài khoản, dữ liệu thật và phục vụ qua mạng cần các điều kiện riêng.

## 2. Tình huống sử dụng chính

| Tình huống | Kết quả người dùng cần |
|---|---|
| Bắt đầu năm, lương chưa thay đổi | Nhập lương cho một giai đoạn, xem 12 tháng, lưu hồ sơ mà không gõ lại 12 lần. |
| Tăng lương hoặc có tháng khác thường | Thêm giai đoạn, sửa ngoại lệ, biết tháng nào bị tác động trước khi áp dụng. |
| Nhận phiếu lương tháng mới | Cập nhật số thực tế, bảo hiểm, khấu trừ và nguồn; giữ nguyên dữ liệu các tháng khác. |
| Muốn biết nghĩa vụ thuế năm | Xem ước tính, mức đầy đủ, các giả định và phép tính; biết vì sao chưa hiện chênh lệch. |
| Chuẩn bị làm việc với HR hoặc quyết toán | Đối chiếu số cùng kỳ, xem chênh lệch, lấy báo cáo và checklist còn thiếu. |

## 3. Sơ đồ điều hướng và danh sách màn hình

Đ1 đến Đ6 là đợt UI ở mục 11, không phải đánh số lại chặng nghiệp vụ. `/ho-so` là danh sách riêng; `/Forecast` tạo hồ sơ và `/Forecast?id={id}` sửa hồ sơ. `/`, `/ForecastDelete`, `/ForecastAnnualHistory`, `/ho-so/{id}/thang/{month}?revision={revision}` và `/ho-so/{id}/bao-cao` đang có; các route `/ho-so/...` khác vẫn là đề xuất.
Giữ route hiện có trong Đ1 để giảm chi phí; đường dẫn tiếng Việt không dấu cho màn hình mới dễ nhận biết hơn tên nội bộ.

```text
hồ sơ (/ho-so)
  +-- tạo hoặc mở hồ sơ
      +-- nhập theo giai đoạn
      +-- bảng 12 tháng
      |   +-- chi tiết tháng và căn cứ
      +-- người phụ thuộc
      +-- tổng quan năm
      |   +-- lịch sử và chi tiết lần tính
      |   +-- hai kịch bản
      +-- báo cáo và checklist
      +-- đối chiếu nguồn
công cụ tính tháng (/)                 dữ liệu và tài khoản (khi có)
```

| Màn hình | Route đề xuất | Mục đích | Dữ liệu hiển thị | Hành động chính duy nhất | Đợt / hiện trạng |
|---|---|---|---|---|---|
| Danh sách hồ sơ | `/ho-so` | Tìm và tiếp tục hồ sơ | Năm, trạng thái Thiếu/Đủ, số phiên bản; không hiện UUID | tạo hồ sơ | Đ1; tách khỏi editor |
| Nhập theo giai đoạn | `/Forecast?id={id}` | Tạo/sửa lịch lương | Giai đoạn, ngoại lệ, metadata | lưu hồ sơ | Đ1; đã có nền |
| Bảng 12 tháng | `/Forecast?id={id}#bang-thang` | Rà dữ liệu cả năm | Gross, bảo hiểm, phụ thuộc, khấu trừ, trạng thái | sửa tháng đã chọn | Đ1; mở rộng preview |
| Công cụ tính tháng | `/` | Giữ công cụ 2025/2026 | Đầu vào 12 tháng, kết quả riêng từng tháng | tính 12 tháng | Đ1; đã có |
| Chi tiết tháng | `/ho-so/{id}/thang/{month}?revision={revision}` | Đọc một tháng của bản lưu, mở editor hiện có | Đầu vào, bước tính, bậc thuế, căn cứ | sửa tháng | Đ1; đã nối forecast qua service chỉ đọc |
| Xác nhận xóa hồ sơ | `/ForecastDelete?id={id}` | Xóa có chủ đích | Hồ sơ, phạm vi xóa, phần còn giữ | xóa hồ sơ | Đ1; đã có |
| Tổng quan năm | `/ho-so/{id}/tong-quan` | Hiểu ước tính và phần thiếu | Nghĩa vụ năm, độ phủ, chênh lệch có điều kiện | tính thuế năm | Đ2; đã chuẩn hóa tại `/Forecast` |
| Chi tiết thuế năm | `/ho-so/{id}/tong-quan#cach-tinh` | Kiểm tra phép tính | Các bước, bậc năm, căn cứ đúng phiên bản | xem đầu vào | Đ2; partial kết quả đã chuẩn hóa |
| Thưởng và khoản bất thường | `/ho-so/{id}/khoan-thu` | Tránh bỏ sót/cộng trùng | Khoản, tháng nhận, đã gồm gross, nguồn | lưu khoản thu | Đ2; form thưởng trong `/Forecast` đã chuẩn hóa |
| Người phụ thuộc | `/ho-so/{id}/nguoi-phu-thuoc` | Quản lý theo thời gian | Khoảng tháng, tự khai/đăng ký, nguồn | lưu người phụ thuộc | Đ2; số theo giai đoạn đã có, hồ sơ đăng ký còn mở rộng |
| Lịch sử tính | `/ho-so/{id}/lich-su` | Đọc các lần đã tính | Thời điểm, phạm vi, độ phủ, phiên bản luật | tính lại | Đ2; `/ForecastAnnualHistory?id={id}` đã có và được chuẩn hóa |
| Chi tiết lần tính | `/ho-so/{id}/lich-su/{recordId}` | Đọc bản đã lưu | Snapshot đầu vào, kết quả, căn cứ cũ | tính lại | Đ2; `record={recordId}` tại route lịch sử đã có |
| Báo cáo và checklist | `/ho-so/{id}/bao-cao` | Chuẩn bị trao đổi/quyết toán | Báo cáo bản lưu, nguồn, phần thiếu | xuất CSV | Đ3; đã có bản đầu, backup chưa có |
| Sao lưu và khôi phục | `/sao-luu`, `/khoi-phuc`, `/khoi-phuc/xem-truoc` | Tự giữ bản có thể khôi phục | Ngày, số hồ sơ/phiên bản/lịch sử, ID trùng, mở kho riêng | Tải bản sao / Kiểm tra / Mở kho khôi phục theo màn | Đ3; đã có, local synthetic |
| Xem trước khôi phục | `/du-lieu/khoi-phuc` | Kiểm tra trước khi ghi | Hồ sơ sẽ thêm, trùng, lỗi, phiên bản | khôi phục bản đã kiểm tra | Đ3; chưa có |
| Hai kịch bản | `/ho-so/{id}/kich-ban` | Thử thêm thưởng giả định | Cơ sở, khoản bổ sung, hai kết quả | so sánh kịch bản | Đ4; chưa có |
| Đối chiếu nguồn | `/ho-so/{id}/doi-chieu` | So số cùng nghĩa và kỳ | Hồ sơ, công ty/eTax, chênh lệch | đối chiếu số liệu | Đ5; nhập thủ công |
| Đăng ký | `/tai-khoan/dang-ky` | Tạo tài khoản | Trường tài khoản, điều kiện xử lý | tạo tài khoản | Đ6; chưa có |
| Đăng nhập | `/tai-khoan/dang-nhap` | Mở dữ liệu của mình | Trường xác thực, lỗi có hướng sửa | đăng nhập | Đ6; chưa có |
| Quyền dữ liệu | `/tai-khoan/du-lieu` | Kiểm soát dữ liệu cá nhân | Lựa chọn đồng ý, phạm vi xuất/xóa | lưu lựa chọn dữ liệu | Đ6; chưa có |
| Xuất dữ liệu cá nhân | `/tai-khoan/du-lieu/xuat` | Lấy bản dữ liệu cá nhân | Phạm vi, loại dữ liệu | xuất dữ liệu cá nhân | Đ6; chưa có |
| Xóa tài khoản và dữ liệu | `/tai-khoan/xoa` | Xác nhận hành động không thể hoàn tác | Phạm vi xóa, lịch sử, giới hạn bản sao | xóa tài khoản và dữ liệu | Đ6; chưa có |

Lựa chọn điều hướng: thanh ngang hoặc sidebar. Chọn thanh bên dọc theo brief mới; thứ tự hiện có là hồ sơ, dữ liệu 12 tháng, người phụ thuộc, tổng quan năm, lịch sử, báo cáo, công cụ tháng. Các phần của editor cùng thứ tự sidebar; dữ liệu riêng tháng và bảng xem trước nằm trọn trước người phụ thuộc, không xen kẽ làm mục chọn quay ngược. Đối chiếu, dữ liệu và tài khoản chỉ thêm khi có chức năng.
Chỉ hiện mục đã có chức năng trong đợt đang làm, không tạo mục giả để đủ số; thưởng ở nhập liệu, kịch bản ở tổng quan, tính tháng là công cụ trong hồ sơ. Mobile thu sidebar thành nút `mở điều hướng`, giữ thứ tự và tên; chưa có tài khoản thì không hiện lựa chọn tài khoản.
Mỗi màn hình có một thông tin ưu tiên và một nút chính; các liên kết điều hướng, hủy, thêm dòng dùng kiểu phụ.
Trang xem trước khôi phục/xóa là ngữ cảnh riêng, không tranh hành động chính với trang dữ liệu.

## 4. Thiết kế chi tiết các màn hình quan trọng

### Design plan ngắn trước triển khai

Palette nền/chữ: nền trang `#F3F6F3`, bề mặt `#FFFFFF`, chữ chính `#1A1F1B`, chữ phụ `#526057`, viền mạnh `#76867B`.
Màu thương hiệu `#395B40`, nền chọn `#E7FFEC`; giữ bốn màu ngữ nghĩa đã định nghĩa ở mục 10. Đây là bộ chốt thay palette cũ, không tạo màu mới.
Font: Inter tự host, một họ cho tiêu đề, nội dung, bảng và trường nhập; độ đậm 400/500/600, số dùng tabular-nums.
Bố cục: sidebar 224px có logo nhỏ và icon nét đồng bộ; thanh trên chỉ chứa huy hiệu Demo local. Danh sách và editor là hai màn riêng, mỗi màn một nút chính; editor đặt thanh thao tác dính cuối form. Nội dung sáng một cột, tối đa 1100px; văn bản 72ch, lề 16/24/32px theo thiết bị. Chỉ chế độ sáng, CSS theo vai trò để thêm dark mode sau.
Chữ, nhãn và tiêu đề căn trái; tiền căn phải; vùng nội dung nằm giữa viewport nhưng không căn giữa đoạn chữ.

```text
+-------------------+----------------------------------------------+
| TaxManager        |                     [Demo local]             |
| hồ sơ             +----------------------------------------------+
| dữ liệu 12 tháng  | kỳ, phạm vi, tình trạng lưu                   |
| tổng quan năm     | bảng hoặc form của công việc                  |
| người phụ thuộc   | chú giải trạng thái, lỗi tại chỗ              |
| lịch sử           | cách tính, bậc thuế, căn cứ                   |
| báo cáo           |                                              |
| đối chiếu         | chỉ tổng thuế năm được dùng khung nền brand   |
| dữ liệu/tài khoản | kết quả chỉ tham khảo                         |
+-------------------+----------------------------------------------+
```

### Đối chiếu với phương án mặc định, đã chỉnh trước khi code

| Phương án dễ lặp ở brief tương tự | Quyết định sửa | Lý do |
|---|---|---|
| Dashboard mở đầu bằng nhiều thẻ tổng tiền | Trang đầu là danh sách; chỉ tổng thuế năm dùng một khung nền brand khi có kết quả | Không biến số chưa đủ dữ liệu thành điểm nhấn, không cạnh tranh với bảng. |
| Sidebar rộng và biểu đồ chiếm đầu trang | Sidebar gọn 6 đến 8 mục, nội dung bảng trước; biểu đồ có điều kiện | Theo brief mới, không dùng liên kết nhanh để lấp chỗ trống. |
| Màu xanh framework và bóng cho mọi khung | Brand xanh trầm theo token chốt, viền, không bóng | Không tự chọn palette khác, không xen kẽ vùng đậm/nhạt trang trí. |
| Một badge thực tế cho toàn hồ sơ | Nhãn tại từng giá trị và bộ đếm độ phủ | Một tháng có gross thực tế vẫn có thể thiếu bảo hiểm hoặc khấu trừ. |
| Nút xem trước và lưu cùng độ nổi | Chỉ lưu là nút chính trong form; xem trước là phụ | Làm rõ dữ liệu đã lưu hay mới xem, tránh hiểu nhầm sau POST. |
| Nguồn tra cứu gợi ý hero, kính mờ, hai font trang trí | Loại toàn bộ; dùng hướng bảng và font đơn của brief | Gợi ý tìm kiếm không phù hợp công cụ thuế; không dùng nó như bằng chứng thiết kế đúng. |

Phương án chốt là tài liệu làm việc có tương tác, không cần tạo nét lạ. Đối chiếu này đã xong cho kế hoạch; từng màn hình vẫn phải nhìn và sửa thủ công khi triển khai.
Trong các wireframe dưới, chữ trong ngoặc vuông là nút hoặc trường, không phải kiểu thẻ bắt buộc. Số có dấu `*` là dữ liệu mẫu và phải có nhãn trạng thái khi render.

### 4.1. Danh sách hồ sơ

Ưu tiên: biết hồ sơ nào cần tiếp tục. Hành động chính: `tạo hồ sơ`.

```text
+------------------------------------------------------------------+
| hồ sơ thuế                                         [tạo hồ sơ]    |
| dữ liệu mẫu / local                                               |
| [năm: 2026]                                                       |
| tên hồ sơ          năm     dữ liệu còn thiếu       lưu gần nhất    |
| lương chính        2026    bảo hiểm tháng 8        27/11/2026       |
| [mở hồ sơ]                                  [xem thao tác xóa]    |
+------------------------------------------------------------------+
```

- Thành phần: tiêu đề, bộ lọc năm, bảng danh sách, liên kết mở, xác nhận xóa; chưa cần tìm kiếm/phân trang khi ít hồ sơ.
- Tên hồ sơ dài xuống dòng, không chỉ hiện tooltip; khả năng đặt tên là bổ sung UI, không giả định hiện đã có field lưu tên.
- Trống: `tạo hồ sơ năm 2026 để nhập mức lương đầu tiên`; vẫn chỉ một nút tạo.
- Lỗi mở kho: `không mở được kho dữ liệu. kiểm tra đường dẫn kho và tệp khóa rồi mở lại`; không tạo kho thay thế âm thầm.
- Thiếu: chỉ rõ chỉ tiêu và tháng từ dữ liệu lưu; chưa có engine độ phủ thì liệt kê trường trống, không tự tạo tỷ lệ hoàn thành.
- Xóa mở trang xác nhận hiện có; nêu riêng hồ sơ, lịch sử và audit theo đúng service, không hứa xóa mọi bản sao.

### 4.2. Nhập liệu theo giai đoạn

Ưu tiên: biết khoảng tháng sắp áp dụng. Hành động chính: `lưu hồ sơ`.

```text
+------------------------------------------------------------------+
| nhập lương năm 2026                              [lưu hồ sơ]      |
| chưa lưu thay đổi                                                 |
| giai đoạn nền: [tháng 1] đến [tháng 12]                            |
| lương gross mỗi tháng [             ] đ                            |
| trạng thái [dự kiến]   [nguồn và ghi chú]                          |
| giai đoạn thay đổi: [tháng 7] đến [tháng 12] [số tiền] đ           |
| [bỏ giai đoạn]                         [thêm giai đoạn]           |
| cảnh báo tại giai đoạn: trùng tháng 7, 8 [sửa khoảng tháng]         |
| [xem trước 12 tháng]                                              |
| tác động: các tháng sẽ đổi / ngoại lệ được giữ                     |
+------------------------------------------------------------------+
```

- Thành phần: panel trắng có tiêu đề, fieldset/legend ngữ nghĩa không viền; ô tháng hẹp, tiền căn phải có đơn vị đ trong ô, ví dụ và hướng dẫn ngay dưới; trạng thái, metadata mở rộng, bản xem trước tác động.
- Có hai hướng xử lý chồng giai đoạn: cho ghi đè sau xác nhận hoặc yêu cầu sửa khoảng. Chọn yêu cầu sửa trước khi lưu ở UI đích vì dễ hiểu và ít nhánh.
- Đây là thay đổi so với preview hiện tại cho giai đoạn sau thay giai đoạn trước; khi triển khai phải cập nhật hành vi có kiểm thử, không đổi âm thầm.
- Tháng ngoại lệ luôn được giữ; muốn bỏ ngoại lệ phải chọn tại tháng đó, không gộp vào sửa khoảng.
- Trống: giai đoạn nền mở sẵn nhưng tiền trống; nhắc `nhập lương gross trên phiếu lương, hoặc để trống khi chưa biết`.
- Lỗi: `tháng kết thúc phải từ tháng bắt đầu đến tháng 12`; gắn đúng ô, giữ nguyên toàn bộ form.
- Thiếu: vẫn xem trước/lưu nháp thiếu dữ liệu; chặn tính kết quả phụ thuộc phần thiếu, không ép gõ 0.
- Enter xem trước, không thêm/bỏ giai đoạn; chỉ nút lưu được kích hoạt tường minh mới lưu. Không nhân đôi nút chính ở hai đầu form.

### 4.3. Bảng 12 tháng

Ưu tiên: tìm tháng hoặc chỉ tiêu cần sửa. Hành động chính: `sửa tháng đã chọn`.

```text
+------------------------------------------------------------------+
| dữ liệu 12 tháng                                                  |
| tháng đang chọn [tháng 8]                  [sửa tháng đã chọn]     |
| chú giải: hình vuông thực tế / tròn dự kiến / tam giác giả định     |
| tháng | gross + nhãn | bảo hiểm + nhãn | phụ thuộc | khấu trừ       |
|     1 | 30.000.000*  | chưa nhập      | 0*        | chưa nhập       |
|   ... |              |               |           |                 |
|     8 | chưa nhập    | chưa nhập     | chưa nhập | chưa nhập       |
| tổng | chỉ cộng các giá trị đã có, không đổi ô trống thành 0        |
| thiếu bảo hiểm tháng 1 [sửa tháng 1]                               |
+------------------------------------------------------------------+
```

- Thành phần: bảng semantic, chú giải ngay phía trên, bộ chọn tháng native, link sửa trong hàng, tổng có điều kiện.
- Mỗi ô có giá trị phải có badge; nguồn/ghi chú của bản xem trước gom vào một khối dưới bảng, chia bằng tiêu đề tháng. Các dòng bảng giữ thao tác sửa và mở chi tiết; tránh thêm một khối thu gọn ở từng dòng.
- Có hai hướng mobile: đổi thành thẻ hoặc giữ bảng cuộn. Chọn giữ bảng, cố định cột tháng và hàng tiêu đề để so sánh ngang không mất ngữ cảnh.
- Trống: 12 hàng vẫn có kỳ rõ, tiền ghi `chưa nhập`; mời `nhập lương theo giai đoạn` bằng liên kết phụ.
- Lỗi: liên kết lỗi đưa đến đúng tháng/ô; cảnh báo nhiều tháng gom theo chỉ tiêu và cho mở danh sách đầy đủ.
- Thiếu: không hiện tổng cả năm nếu thiếu kỳ; có thể hiện `tạm cộng các tháng đã nhập`, kèm danh sách tháng và cơ cấu trạng thái.
- Nguồn trạng thái trong từng cột độc lập; không dùng màu toàn hàng để ngầm nhận cả tháng là thực tế.

### 4.4. Chi tiết một tháng

Ưu tiên: biết thuế tháng được hình thành như thế nào. Hành động chính ở chế độ sửa: `lưu tháng`.
Ở chế độ chỉ đọc kết quả, hành động chính là `sửa tháng`; không hiển thị đồng thời hai nút chính.

```text
+------------------------------------------------------------------+
| tháng 8/2026                                       [lưu tháng]    |
| gross [số tiền] đ    [trạng thái] [nguồn và ghi chú]                |
| bảo hiểm [số tiền] đ    [trạng thái]                               |
| người phụ thuộc [số lượng] [trạng thái]                            |
| thuế đã khấu trừ [số tiền] đ    [trạng thái]                        |
| kết quả tháng: chưa tính / số tiền, trạng thái, độ đầy đủ           |
| các bước tính                      giá trị                 [?]    |
| gross / bảo hiểm / giảm trừ / thu nhập tính thuế / thuế             |
| từng bậc: khoảng thu nhập | thuế suất | phần chịu thuế | tiền thuế  |
| căn cứ: phiên bản, kỳ áp dụng, trạng thái, giải thích, liên kết     |
+------------------------------------------------------------------+
```

- Thành phần: cùng ô tiền/metadata, bảng bước tính, bảng bậc thuế, disclosure căn cứ; ghi riêng thuế đã khấu trừ và thuế ứng dụng tính.
- Gom căn cứ của màn chi tiết tháng vào một khối “Văn bản và trạng thái xác minh”, hiện các nguồn bên trong bằng văn bản và liên kết. Công cụ demo giữ nút `?` có tên truy cập rõ; tránh disclosure lồng nhau hoặc lặp cho từng căn cứ trong cùng phần.
- Khi dữ liệu thay đổi, kết quả cũ ghi `đầu vào đã thay đổi, tính lại để xem kết quả mới`; không để số cũ trông như vừa tính.
- Trống: `nhập số từ phiếu lương tháng 8 để xem cách tính`; không tự điền tiền.
- Lỗi: `bảo hiểm phải là số không âm. nhập theo dạng 1.234.567,50`; giữ chuỗi người dùng nhập.
- Thiếu: liệt kê chỉ tiêu chặn tính; thiếu khấu trừ không chặn phép tính nghĩa vụ tháng nếu engine không cần nó.
- Chỉ nối tính vào dữ liệu đã lưu qua service phù hợp; Đ1 không mở thêm nhánh engine hoặc ghi kết quả do trình duyệt tính.
- Công cụ `/` giữ chức năng hiện có; metadata chưa được luồng cũ hỗ trợ thì ghi rõ dữ liệu demo, không tự gắn nhãn thực tế.

#### Triển khai màn tháng từ bản lưu, 28/11/2026

Giữ palette đã chốt: brand `#395B40`, nền `#F3F6F3`, bề mặt `#FFFFFF`, chữ `#1A1F1B`, chữ phụ `#526057`, viền chức năng `#76867B`; Inter 400/500/600 và số tabular. Nội dung căn trái, tiền căn phải. Tái dùng sidebar và header; chỉ thuế năm có khung brand.

```text
bảng 12 tháng / bản đã lưu             [sửa tháng]
tháng 6/2026
[1] [2] [3] [4] [5] [6 chọn] ... [12]
thuế tháng tạm tính / hoặc chỉ tiêu còn thiếu
dữ liệu đã lưu và trạng thái từng chỉ tiêu
nguồn, ghi chú, ngày nhập (mở rộng)
bước tính / từng bậc / căn cứ (mở rộng)
```

So với dashboard mặc định, không thêm thẻ KPI trùng số hoặc biểu đồ trang trí. Dùng khoảng trắng gom dữ liệu và kết quả, không có vạch chia dài. Thành phần bổ sung duy nhất là điều hướng 12 tháng bằng liên kết native, có tháng chọn bằng nền tint và `aria-current`, đích URL giữ cả ID/tháng/revision. Nút chính duy nhất là sửa tháng, mở đúng editor hiện có; lưu hồ sơ giữ tháng đang sửa.

Contract `ForecastMonthService.ReadAsync(id, revision, month)` đọc bản hồ sơ, snapshot đầu vào cùng revision và catalog trong một transaction, xác thực khóa kể cả lookup rỗng. Kết quả tính khi đọc, không ghi lịch sử/audit hay tạo hồ sơ demo; UI ghi rõ giới hạn này. Thưởng tháng được tính đúng một lần qua Core, khấu trừ nullable độc lập, không cần các tháng khác đủ đầu vào. Link cũ gặp revision mới bị từ chối và có đường mở lại. Không sửa engine, schema hoặc snapshot đã lưu.

Hai ảnh tham khảo của người dùng ngày 28/11: dùng ảnh sáng để giữ hierarchy/sidebar; ảnh tối để tham khảo nhóm số liệu, khoảng cách và bộ chọn kỳ. Hướng dark mode tương lai: nền than có sắc xanh, surface tối nâng nhẹ, chữ sáng, xanh lá cho trạng thái chọn; cần đo contrast và chốt token riêng trước triển khai. Ảnh không thay palette sáng hiện tại, không yêu cầu thêm donut, avatar, nguồn thu hoặc thông báo chưa có dữ liệu.

### 4.5. Tổng quan và dự báo năm

Ưu tiên: nghĩa vụ thuế năm ước tính có đủ căn cứ đầu vào hay chưa. Hành động chính: `tính thuế năm`, thống nhất nút hiện có.

```text
+------------------------------------------------------------------+
| tổng quan năm 2026                             [tính thuế năm]    |
| nghĩa vụ thuế năm ước tính: chưa tính / giá trị và trạng thái       |
| X/12 tháng là thực tế; Y dự kiến; Z giả định; M thiếu               |
| khấu trừ: đã có .../12 tháng; thực tế ...; dự kiến ...              |
| chênh lệch: chưa hiển thị vì thiếu khấu trừ tháng ...                |
| thông tin cần bổ sung [đến tháng cần sửa]                           |
| [cách tính năm] [từng bậc thuế] [căn cứ pháp lý]                    |
| phiên bản luật, kỳ, nguồn và trạng thái xác minh                    |
| phạm vi đã tính / chưa hỗ trợ / kết quả chỉ tham khảo               |
+------------------------------------------------------------------+
```

- Thành phần: một khung nền brand cho tổng thuế năm, chữ surface, nhãn trạng thái/độ phủ đọc được; các bảng và phần thiếu nền sáng, không dùng hero marketing hoặc nền đậm xen kẽ.
- Dùng engine năm; không cộng 12 số thuế tháng để thay kết quả năm. Chọn tính theo yêu cầu thay vì tự tính mỗi lần gõ để giữ luồng lưu và lịch sử rõ ràng.
- Đủ dữ liệu nghĩa vụ nhưng thiếu khấu trừ: vẫn hiện nghĩa vụ, không hiện số nộp thêm/nộp thừa; có liên kết đến tháng thiếu.
- Có đủ khấu trừ hoặc giả định minh bạch cho cả kỳ mới hiện chênh lệch; số có dấu và chữ chỉ hướng theo mục 7.
- Trống: `nhập lương và các khoản cần thiết để tính thuế năm`; mã tính năm hiện đã có và được chuẩn hóa chặng 3 trước khi đổi UI, không ẩn/bỏ chức năng này chỉ để chia đợt.
- Lỗi không có luật đúng kỳ: `chưa có bộ quy tắc phù hợp cho kỳ này. chưa thể tính dự báo`; không lấy luật kỳ gần nhất.
- Thiếu: vẫn đọc/sửa hồ sơ; mỗi phần bị chặn nói rõ chỉ tiêu, kỳ và cách bổ sung.
- Dữ liệu thay đổi sau lần tính: ghi thời điểm/snapshot của kết quả đang đọc và trạng thái cần tính lại.

### 4.6. Người phụ thuộc theo tháng

Ưu tiên: biết người phụ thuộc được tính vào tháng nào. Hành động chính: `lưu người phụ thuộc`.

```text
+------------------------------------------------------------------+
| người phụ thuộc                         [lưu người phụ thuộc]     |
| tên gợi nhớ [                 ]                                   |
| từ [tháng] đến [tháng]   trạng thái [thực tế / dự kiến / giả định]  |
| đăng ký [tự khai / có xác nhận / chưa có thông tin]                 |
| [nguồn và ghi chú]                                                |
| tên gợi nhớ | tháng áp dụng | thông tin đăng ký | thao tác sửa      |
| xem theo tháng: 1 | 2 | 3 | ... | 12                               |
| số người mỗi tháng và nhãn trạng thái                              |
+------------------------------------------------------------------+
```

- Thành phần: tái dùng form số người theo giai đoạn và ngoại lệ đã có; tên gợi nhớ/thông tin đăng ký là mở rộng cần contract riêng, không tự coi backend đã hỗ trợ. Không thu CCCD/MST hay file chứng từ.
- Chọn tên gợi nhớ thay cho hồ sơ định danh đầy đủ để thu ít dữ liệu; không dùng tự khai làm xác nhận đủ điều kiện giảm trừ.
- Không tự cộng danh sách mới vào số lượng tháng cũ. Khi chuyển đổi, cho xem trước tháng sẽ thay thế và yêu cầu xác nhận.
- Tháng ngoài các khoảng đã nhập vẫn chưa biết cho tới khi người dùng xác nhận số lượng; không ngầm điền 0.
- Trống: `thêm người phụ thuộc, hoặc xác nhận không có người phụ thuộc cho khoảng tháng đã chọn`.
- Lỗi: `khoảng tháng của người này bị trùng. sửa tháng bắt đầu hoặc kết thúc`; không chặn hai người khác nhau cùng một tháng.
- Thiếu thông tin đăng ký: hiển thị riêng, nêu giới hạn kết quả; điều kiện tính theo hợp đồng nghiệp vụ, không tự suy từ màu/badge.

### 4.7. Đối chiếu nguồn

Ưu tiên: biết số nào lệch trong cùng phạm vi. Hành động chính: `đối chiếu số liệu`.

```text
+------------------------------------------------------------------+
| đối chiếu nguồn                             [đối chiếu số liệu]   |
| nguồn [công ty / eTax / khác]   kỳ [tháng / năm]                    |
| chỉ tiêu [thuế đã khấu trừ]   số tiền [       ] đ    [trạng thái]   |
| ghi chú [                         ]   thời điểm nhập              |
| chỉ tiêu | kỳ | hồ sơ và nhãn | nguồn nhập và nhãn | chênh lệch    |
| ...      | ...| ...           | ...                | chưa so được  |
| cách tính chênh lệch / phạm vi chưa tương ứng / thông tin còn thiếu |
+------------------------------------------------------------------+
```

- Thành phần: form số từ nguồn ngoài, bảng hai nguồn và cột chênh lệch; chỉ so cùng loại chỉ tiêu và kỳ, không so gross với thu nhập tính thuế.
- Chọn nhập thủ công thay vì kết nối eTax vì ít phụ thuộc, không thu mật khẩu hoặc lưu file gốc.
- Giá trị từ công ty có thể thực tế hoặc dự kiến; nhãn nguồn không tự xác định trạng thái, và không phải xác minh pháp lý.
- Trống: `nhập một số từ công ty hoặc eTax cùng kỳ để bắt đầu đối chiếu`.
- Lỗi: `hai số đang khác kỳ. chọn cùng tháng hoặc cùng năm trước khi đối chiếu`.
- Thiếu: `chưa có số khấu trừ tháng 8 trong hồ sơ`; liên kết sửa, không lấy 0 để tính chênh lệch.
- Chênh lệch không kết luận nguồn nào sai, không tự thành tiền hoàn hoặc phải nộp thêm.

### 4.8. Lịch sử và chi tiết lần tính

Ưu tiên: biết đang đọc kết quả của lần nào, với dữ liệu và luật nào. Hành động chính: `tính lại`.

```text
+------------------------------------------------------------------+
| lịch sử tính                                           [tính lại] |
| thời điểm | kỳ | trạng thái đầu vào | phiên bản luật | xem chi tiết |
| ...       | ...| X/12 thực tế ...  | ...            | [mở lần tính] |
| lần đang mở: ngày giờ, kỳ, phạm vi, dữ liệu đã chốt                 |
| đầu vào / bước tính / từng bậc / căn cứ của bản này                 |
| kết quả cũ được giữ khi tạo lần tính mới                           |
+------------------------------------------------------------------+
```

- Thành phần: bảng lịch sử và cùng bố cục kết quả đọc; liên kết sang trang snapshot, không tạo trình xem riêng nhiều kiểu.
- Tính lại tạo bản mới bằng service từ dữ liệu đã lưu; liên kết giữa các lần chỉ hiện khi backend có hỗ trợ, không suy từ cùng tháng.
- Trống: `tính thuế năm từ hồ sơ đã lưu để tạo lần tính đầu tiên`; hành động chính tại đây đổi thành `tính thuế năm`.
- Lỗi tính lại do thiếu dữ liệu/luật: giữ bản cũ, chỉ rõ phần thiếu và liên kết sửa; không tạo bản thành công giả.
- Thiếu nguồn của snapshot cũ: giữ đúng dữ liệu/căn cứ đã lưu và trạng thái thiếu, không ghép nguồn luật mới vào bản cũ.
- So sánh hai lần dự báo thuộc backlog; Đ2 chỉ mở từng bản và đọc, không dựng so sánh ngầm.

## 5. Thiết kế nhập liệu

| Quyết định | Hành vi chốt và lý do |
|---|---|
| Nhập giai đoạn thay vì 12 form mở sẵn | Một gross, khoảng tháng và trạng thái; xem trước các tháng nhận giá trị để giảm gõ lặp. |
| Khoản ngoài gross | Ở Đ2 ghi loại được hỗ trợ, tháng chi trả, số tiền, trạng thái, đã gồm gross hay chưa; không biết thì chưa cộng vào kết quả. |
| Bảo hiểm và khấu trừ nhập riêng | Không suy bảo hiểm bằng tỷ lệ cố định, không lấy thuế ứng dụng tính làm số công ty khấu trừ. |
| Áp dụng nhiều tháng | Cho chọn khoảng, nhập chung metadata, xem trước và xác nhận; không mặc định các tháng tương lai là thực tế. |
| Trạng thái ban đầu | Bắt đầu ở dự kiến với nhãn hiện rõ; chọn thực tế là thao tác chủ động, nguồn/ghi chú có thể bổ sung sau. |
| App tự điền/ngoại suy | Chỉ khi người dùng yêu cầu, ghi giả định và lý do; không dùng mặc định để lấp khoản chưa biết. |
| Ngoại lệ | Hiện nguồn giá trị là giai đoạn hay nhập riêng; sửa riêng ưu tiên hơn giai đoạn; bỏ ngoại lệ có xem trước giá trị sẽ quay về. |
| Chồng giai đoạn | Cảnh báo tại cả hai khoảng, liệt kê tháng trùng và link sửa; UI đích chặn lưu khoảng xung đột, không âm thầm ghi đè. |
| Dữ liệu thiếu | Lưu nháp được; chỉ chặn hành động cần phần thiếu, dẫn đến trường tương ứng. |
| Metadata | Disclosure nguồn, ghi chú, thời điểm ngay từng trường/nhóm áp dụng; trạng thái luôn lộ ra ngoài, không giấu cùng metadata. |
| Thời điểm nhập | Tự ghi khi giá trị hoặc metadata đổi; xem trước/lưu không đổi nội dung không được cập nhật ngày nhập. |
| Mất thay đổi | Hiện chưa lưu; khi rời form có thay đổi thì xác nhận lưu hoặc bỏ; không hứa autosave ở đợt đầu. |
| Hai cửa sổ cùng sửa | Dùng revision hiện có; báo bản lưu đã đổi và mời mở bản mới, không âm thầm ghi đè. |

### Ô tiền và hợp đồng nhập

- Dùng `type="text"`, `inputmode="decimal"`, label luôn hiện và đơn vị đồng rõ ràng; không dùng input number có nút tăng giảm cho tiền.
- Cập nhật theo yêu cầu người dùng ngày 28/11/2026: tự tách hàng nghìn khi nhập `123456789` thành `123.456.789`; nhận số dán có phân nhóm đúng. Phần lẻ dùng dấu phẩy, ví dụ `1.234.567,50`. Focus lại vẫn giữ định dạng này.
- Lớp tiền dùng chung chỉ xử lý chuỗi, không qua JS Number/float và không làm tròn. Bao gồm trường tiền ẩn của tháng chưa mở để tránh đổi nghĩa khi gửi form.
- Khi JavaScript định dạng form, gửi dấu định dạng `AmountFormat=vi-VN`; server xác thực nhóm chấm đúng ba chữ số và dấu phẩy thập phân bằng grammar tường minh, không dùng AllowThousands/NumberStyles.Number theo culture.
- Form cũ hoặc không JavaScript giữ hợp đồng chuỗi không phân nhóm: một dấu phẩy/chấm là thập phân. Chẳng hạn `1.234` trong giao thức cũ vẫn là 1,234 đồng; trong form vi-VN mới là 1.234 đồng. Dữ liệu cũ mở lại được chuyển định dạng từ chuỗi canonical, không đoán nghĩa từ số dấu chấm.
- POST lỗi ưu tiên ModelState.AttemptedValue và giữ nguyên chuỗi sai. Không định dạng lại bản POST lỗi để che lỗi. Không tự điền 0 cho thiếu dữ liệu.
- Giải thích gross/net ngay ở phần nhập lương; gross là trước khấu trừ, net là thực nhận. Chỉ biết net thì hướng dẫn kiểm phiếu lương, chưa thêm tính ngược gross/net.
- Kiểm HTTP và trình duyệt: gõ/dán, phần lẻ, số 10 chữ số, 0, thiếu, nhóm sai, sửa giữa chuỗi, mở lại và gửi trường tháng ẩn. Bàn phím điện thoại thật còn cần thử riêng.

## 6. Hệ thống trạng thái dữ liệu

**Tuân theo mục Quy tắc giao diện số 5.** Tên trạng thái dùng chữ thường trên UI; không dùng ALL CAPS và letter-spacing để gây chú ý.

| Trạng thái | Nhãn | Hình dạng SVG | Viền hoặc nền | Màu token | Ý nghĩa |
|---|---|---|---|---|---|
| THỰC TẾ | thực tế | hình vuông có dấu kiểm | viền liền | success | Người dùng ghi nhận khoản đã phát sinh; chưa xác nhận pháp lý. |
| DỰ KIẾN | dự kiến | hình tròn rỗng | viền nét đứt | info | Người dùng chủ động nhập kế hoạch cho phần chưa phát sinh. |
| GIẢ ĐỊNH | giả định | tam giác rỗng | viền chấm | warning | Giá trị thay thế/ngoại suy có lý do để dựng kịch bản. |
| Thiếu, không phải trạng thái thứ tư của một giá trị | chưa nhập | hình thoi rỗng | viền trung tính, không có số | text-muted | Chưa có giá trị; không thay bằng 0. |

- Mỗi trạng thái có ít nhất ba lớp phân biệt: nhãn chữ, hình dạng, kiểu viền; không dùng emoji, không cần nền gạch chéo/gradient.
- SVG đi cùng nhãn đặt aria-hidden; screenreader đọc chữ một lần. Badge không được chỉ hiện icon khi thu nhỏ màn hình.
- Giá trị tiền, số người phụ thuộc, khoản thưởng, số nguồn đối chiếu và kết quả đều phải có trạng thái hoặc cơ cấu trạng thái ngay cạnh.
- Trạng thái kết quả mô tả nguồn đầu vào: toàn thực tế thì ghi `tính từ dữ liệu thực tế`; pha trộn thì ghi `ước tính` kèm số tháng từng nhóm, không gắn bừa một trạng thái.
- Kết quả tính từ thực tế vẫn là tham khảo. Mức giảm trừ/thuế suất là tham số luật, hiện nguồn và trạng thái xác minh riêng; không gắn nhãn dự kiến như số cá nhân.
- Tháng, năm, số thứ tự bậc là định danh kỳ/cấu trúc, không phải giá trị đầu vào cần badge trạng thái.
- Một ô thực tế không làm cả tháng thành thực tế. Tháng thực tế phải đủ mọi đầu vào cần cho nghĩa vụ thuế và tất cả đều thực tế.
- Bộ đếm không trùng: thiếu nếu thiếu đầu vào bắt buộc; còn lại giả định nếu có giả định; còn lại dự kiến nếu có dự kiến; còn lại thực tế. X + Y + Z + M = 12.
- Trên mọi kết quả dự báo luôn có `X/12 tháng là thực tế`, số tháng dự kiến, giả định và thiếu; giải thích quy tắc phân nhóm ngay cạnh.
- Khấu trừ có độ phủ riêng theo giá trị và trạng thái; không lấy X tháng thực tế để khẳng định khấu trừ đã đầy đủ.
- Chú giải cố định về vị trí, luôn nhìn thấy gần bảng, không chỉ trong tooltip; xuất/in giữ nhãn chữ và cơ cấu trạng thái.
- Missing/Unverified/Verified của luật là trục độc lập; nguồn dữ liệu công ty/eTax cũng không tự thành Verified.

## 7. Quy ước hiển thị

| Loại | Quy ước | Lý do / giới hạn |
|---|---|---|
| Tiền đọc | Culture vi-VN, dấu chấm nghìn, dấu phẩy lẻ, đơn vị hiển thị `đ`; ví dụ `12.345.678 đ`, có lẻ `1.234,50 đ` | Hiển thị đầy đủ, không K/M; tên truy cập đọc là đồng. |
| Cột tiền | Cùng số chữ số lẻ trong một cột; mặc định 0 nếu mọi giá trị nguyên, nếu có lẻ thì đủ biểu diễn chính xác toàn cột | Không làm mất phần lẻ mà engine vẫn giữ. |
| Số quá nhiều chữ số lẻ | Mở rộng/có vùng cuộn và giá trị đầy đủ; không cắt bằng ellipsis hoặc chỉ tooltip | Ưu tiên kiểm số; xem lại quy tắc hiển thị sau thử người dùng. |
| Làm tròn | Đợt đầu không làm tròn làm mất giá trị; không gọi format là quy tắc làm tròn thuế | Chưa được tự thêm chính sách pháp lý; số lưu luôn giữ decimal gốc. |
| Số 0 | Hiện `0 đ` cùng trạng thái | Đã biết bằng 0 khác chưa nhập. |
| Thiếu | `chưa nhập` hoặc `chưa đủ dữ liệu`, không dùng dấu gạch thay tiền | Không tạo ấn tượng số 0 hoặc kết quả đã tính. |
| Chênh lệch thuế năm | Nghĩa vụ trừ khấu trừ: `+... đ, ước tính cần nộp thêm`; `-... đ, ước tính nộp thừa` | Có dấu và chữ, không phụ thuộc màu; không mũi tên tăng giảm kèm phần trăm, nộp thừa không bảo đảm được hoàn. |
| Chênh lệch đối chiếu | Ghi công thức `số nguồn nhập trừ số hồ sơ`; hiện `+`/`-` và `cao hơn`/`thấp hơn` | Không lẫn chênh lệch nguồn với nghĩa vụ thuế. |
| Ngày và giờ | `dd/MM/yyyy HH:mm`, ghi `giờ Việt Nam` trong tiêu đề cột; lưu UTC và chuyển Asia/Ho_Chi_Minh để đọc | Tránh nhầm ngày nhập với kỳ thuế; ngày cũ không tự đổi khi xem lại. |
| Kỳ tính thuế | `tháng 8/2026`, `năm thuế 2026`; đầu và cuối khoảng ghi rõ | Không dùng ngày nhập để chọn luật. |
| Phần trăm | Dấu phẩy thập phân, cùng độ chính xác trong cột; luôn có `%` | Không tự bịa mức thuế hoặc ngưỡng minh họa pháp lý. |
| Lần tính | Hiện ngày giờ, kỳ và phiên bản luật, ID đầy đủ có thể mở xem | Không chỉ dựa vào giờ nếu có hai lần tính gần nhau. |

Ví dụ tiền trong tài liệu chỉ minh họa format; khi đưa lên giao diện thử phải có nhãn `dữ liệu mẫu` và trạng thái từng giá trị.

## 8. Microcopy tiếng Việt

Chữ thường đầu câu theo brief, động từ chủ động; tên riêng giữ cách viết đúng. Không có dấu gạch ngang dài, dấu chấm giữa, mũi tên gắn sau mọi nút.

| Ngữ cảnh | Nội dung thống nhất |
|---|---|
| Tạo / lưu | `tạo hồ sơ`; `lưu hồ sơ`; `đã lưu hồ sơ`; `chưa lưu thay đổi` |
| Sửa tháng | `sửa tháng đã chọn`; `lưu tháng`; `đã lưu tháng 8/2026` |
| Giai đoạn | `thêm giai đoạn`; `bỏ giai đoạn`; `xem trước 12 tháng` |
| Tính | `tính 12 tháng`; `tính thuế năm`; `tính lại`; thông báo nêu `đã tính ...` và lần mới |
| Xuất / backup | `xuất báo cáo`; `đã xuất báo cáo`; `tạo bản sao lưu`; `đã tạo bản sao lưu` |
| Khôi phục | `kiểm tra bản sao lưu`; `khôi phục bản đã kiểm tra`; `đã khôi phục bản đã kiểm tra` |
| Gợi ý gross | `tìm tổng lương trước khi trừ bảo hiểm và thuế trên phiếu lương` |
| Gợi ý khấu trừ | `tìm số thuế TNCN công ty đã khấu trừ trên phiếu lương; không dùng số ứng dụng tính` |
| Tiền sai cú pháp | `số tiền chưa đúng định dạng. dùng dấu chấm cho hàng nghìn và dấu phẩy cho phần lẻ, ví dụ 1.234.567,50` |
| Tiền âm không hợp lệ | `số tiền phải từ 0 trở lên. sửa số hoặc để trống nếu chưa biết` |
| Giả định thiếu lý do | `thêm lý do cho giá trị giả định để biết số này được hình thành thế nào` |
| Chồng giai đoạn | `hai giai đoạn cùng áp dụng cho tháng 7 và 8. sửa khoảng tháng trước khi lưu` |
| Giữ ngoại lệ | `tháng 8 đã được nhập riêng và sẽ được giữ. mở tháng 8 nếu muốn thay đổi` |
| Thiếu khấu trừ | `chưa có số khấu trừ tháng 8. bổ sung để xem chênh lệch ước tính của cả năm` |
| Xung đột lưu | `hồ sơ đã được cập nhật ở nơi khác. mở bản mới để kiểm tra trước khi lưu tiếp` |
| Xác nhận xóa tháng | `xóa dữ liệu đã nhập riêng của tháng 8? xem trước sẽ cho biết giá trị từ giai đoạn còn áp dụng` |
| Không có hồ sơ | `tạo hồ sơ năm 2026 để nhập mức lương đầu tiên` |
| Chưa có nguồn đối chiếu | `nhập số từ công ty hoặc eTax cùng kỳ để bắt đầu đối chiếu` |
| Dữ liệu mẫu | `dữ liệu mẫu, không phải số liệu của bạn`; không điền đè hồ sơ đang mở nếu chưa xác nhận |
| Mô tả trạng thái | `thực tế: bạn ghi nhận đã phát sinh`; `dự kiến: kế hoạch bạn nhập`; `giả định: số thay thế có lý do` |
| Disclaimer kết quả | `kết quả chỉ mang tính tham khảo, không thay thế tư vấn thuế chính thức` |
| Nộp thừa | `số nộp thừa là ước tính, không xác nhận bạn chắc chắn được hoàn thuế` |
| Luật chưa xác minh | `căn cứ chưa hoàn tất xác minh. xem nguồn và phạm vi áp dụng trước khi sử dụng kết quả` |

Lỗi có biểu tượng, mô tả cạnh ô và tóm tắt liên kết ở đầu form. Thông báo thành công ở vùng status, không dùng popup tự biến mất làm bằng chứng lưu duy nhất.

## 9. Responsive và khả năng truy cập

- Desktop là nơi nhập dài; điện thoại ưu tiên mở hồ sơ, sửa một tháng, xem phần thiếu và đọc kết quả. Tablet dùng cùng thành phần.
- Hai hướng mobile cho bảng: thẻ hoặc cuộn riêng. Chọn bảng cuộn riêng, giữ cột tháng và hàng tiêu đề; không làm toàn trang tràn ngang.
- Khung bảng có tên, hướng dẫn `cuộn ngang để xem các cột`, focus được bằng bàn phím, có chiều cao giới hạn để sticky header hoạt động; kiểm sticky ở cả hai trục.
- Dưới breakpoint tablet, form một cột; nút chính xuống dòng, vẫn chỉ một nút. Không làm sticky footer ở đợt đầu để tránh che ô đang sửa.
- Desktop sidebar dọc; dưới 64rem dùng nút chữ `Menu` cùng hàng với tên ứng dụng, mở ra đổi thành `Đóng menu`. Không dùng mũi tên dropdown cho điều hướng. Nút có aria-expanded/controls, không khóa focus; đóng khi chọn trang hoặc Escape, trả focus về nút khi đóng bằng phím. Không có JavaScript thì hiện toàn bộ liên kết.
- Chữ thân bài và ô nhập tối thiểu 16px, line-height từ 1,5; không giảm font để nhét tiền dài. Đoạn giải thích tối đa 72ch.
- Mọi chữ đạt 4,5:1; viền input, icon mang nghĩa và focus đạt 3:1 so với nền sát nó. Viền chia vùng trang trí không thay viền điều khiển.
- Mục tiêu bấm tối thiểu 24x24px; nút chính/điều khiển thường dùng trên điện thoại 44px, khoảng cách giữa nút 8px.
- Có skip link, một h1, heading theo cấp, label/for thật, fieldset/legend; bảng có caption, th scope row/col, tiền dùng td căn phải.
- Lỗi dùng aria-invalid và aria-describedby; focus tóm tắt lỗi sau POST sai, liên kết tới ô; tự mở disclosure chứa lỗi trước khi focus.
- Khi mở/đóng dialog: focus vào nội dung phù hợp, Escape hủy, trả focus về nút mở; không dùng dialog để hiện mọi lời giải thích.
- Thử Tab/Shift+Tab/Enter/Space/Escape; chọn tháng không đòi thao tác chuột. Nút bỏ giai đoạn không được là submit mặc định.
- Focus có viền nhìn rõ và không bị thanh/cột sticky che; điều chỉnh scroll-margin bằng token, thử zoom 200% và reflow 400%.
- Mở căn cứ qua disclosure hoạt động không cần JS; không phụ thuộc hover, không bỏ outline hoặc chặn zoom trình duyệt.
- Bản mẫu đọc với NVDA hoặc screenreader sẵn có; Lighthouse/axe không thay được kiểm tra bàn phím, thứ tự đọc và phân biệt trạng thái.

## 10. Hệ token và danh sách thành phần theo mục Quy tắc giao diện số 1

### Quy tắc giao diện

#### 1. Token trước, giao diện sau

- Định nghĩa token trong `src/TaxManager.Web/wwwroot/css/tokens.css` khi code; file CSS đã được tạo trong đợt triển khai ngày 28/11/2026.
- Chỉ dùng brand, brand-tint, bg, surface, text, text-muted, border-strong và warning/error/info/success của bộ chốt dưới đây. Token theo vai trò `--surface`, `--text`, không lấy xanh mặc định framework hoặc thêm màu ngoài bộ này.
- Một họ font Inter hỗ trợ tiếng Việt, thang 13/14/16/20/24/32; 400/500/600, không thêm font mono/serif cho nhãn.
- Khoảng cách theo bội số 4px; tối đa hai bo góc 4px và 8px; không bóng. Tách vùng bằng nền và khoảng cách; viền input mảnh đủ tương phản, bảng danh sách chỉ có đường ngang mảnh.
- Mỗi màn hình đúng một hành động chính, một thông tin quan trọng nhất; không thêm thành phần ngoài danh sách ở dưới.
- Các lựa chọn màu, cỡ, khoảng cách, bo góc chỉ xuất hiện tại token; CSS thành phần chỉ tham chiếu var(), không chép hex/px ra từng màn hình.
- Chỉ chế độ sáng; dark mode để sau và chỉ thay token theo vai trò. Sidebar chọn dùng brand-tint, chữ đậm 600, viền trái 3px brand, aria-current; giữ sẵn chỗ viền ở các mục khác để không nhảy bố cục.
- Thanh trên chứa huy hiệu Demo local; hành động chính theo từng màn hình. Sidebar 6 đến 8 mục khi đủ chức năng, dính theo viewport và cuộn riêng nếu cửa sổ thấp. Mỗi mục chuyển tới đúng phần, mục hiện tại tự cập nhật theo nội dung khi cuộn tay (aria-current=location). Liên kết trong editor chỉ đổi fragment, giữ form chưa lưu và query POST. Mobile giữ thanh Menu dính; menu mở phủ lên nội dung, đóng trước khi cuộn tới phần, không che tiêu đề đích. Chỉ khung thuế năm ước tính dùng nền brand; không xen kẽ khung đậm/nhạt để trang trí.

#### 2. Danh sách cấm

- Hạn chế vạch phân chia, đặc biệt đường ngang kéo dài hết vùng nội dung. Không thêm `hr` hoặc `border-top`/`border-bottom` chỉ để tách các phần, disclosure, thanh trên hay footer; ưu tiên khoảng trắng và tiêu đề. Chỉ giữ viền có chức năng rõ ở ô nhập, bảng dữ liệu, vùng xác nhận/lỗi và focus. Đây là điều chỉnh theo phản hồi trực tiếp ngày 28/11/2026.
- Không dùng accordion/dropdown làm bố cục mặc định. Hiện trực tiếp các phần nhập chính, khoản thưởng, giải thích gross/net và bảng bậc thuế; chỉ thu gọn nguồn, ghi chú hoặc nội dung tra cứu dài theo nhóm. Không lặp mũi tên tam giác/chevron trước từng tiêu đề, từng dòng hoặc từng căn cứ; trigger còn lại dùng chữ gạch chân, giữ ngữ nghĩa native và bàn phím. Không thay mũi tên bằng chuỗi dấu cộng hay icon trang trí khác. Đây là điều chỉnh visual theo phản hồi người dùng ngày 28/11/2026.
- Không gradient, kính mờ, blob, minh họa có sẵn, emoji làm icon; không thẻ giống nhau phủ kín trang hoặc thẻ lồng thẻ.
- Không khối liên kết nhanh lấp chỗ trống, nhãn IN HOA nhóm menu, nền hình tròn hoặc avatar minh họa; không dùng danh sách thẻ giống nhau thay bảng.
- Không hero số lớn/nhãn nhỏ; trang chính là dữ liệu. Không nền kem với serif/đất nung, nền gần đen với neon, bố cục báo giấy hairline/cột dày/bo góc 0.
- Không nhãn IN HOA giãn chữ, đánh số 01/02/03 cho nội dung không phải trình tự; không đổi màu/nghiêng riêng một từ tiêu đề.
- Không trượt khi cuộn, nảy hover, thẻ nhấc lên; chỉ chuyển động phục vụ mở/đóng/xác nhận, tối đa 150ms, tôn trọng prefers-reduced-motion.
- Không gạch ngang dài trong nội dung UI, chuỗi nối dấu chấm giữa, mũi tên gắn vào mọi nút, font mono cho nhãn hoặc đen tinted #0B0B0B/#111.

#### 3. Chữ và số

- Inter tự host có bộ ký tự tiếng Việt, font-display swap; không gọi dịch vụ font bắt buộc và không thêm gói trả phí.
- Chỉ chốt file font sau thử câu: `tiếng Việt đủ dấu: ă â ê ô ơ ư đ; ắ ằ ẳ ẵ ặ, ấ ầ ẩ ẫ ậ, ế ề ể ễ ệ, ố ồ ổ ỗ ộ, ớ ờ ở ỡ ợ, ứ ừ ử ữ ự, ý ỳ ỷ ỹ ỵ`.
- Thử thêm chữ hoa `ĐẶNG THỊ THUỶ`, nhiều dòng và số; chữ thân bài line-height từ 1,5, chiều dài dòng dưới 80 ký tự.
- Mọi số tiền dùng font-variant-numeric: tabular-nums. Đo chuỗi 1111111111 và 8888888888 trên font đã tải để xác nhận bề rộng; fallback hệ thống chưa phải bằng chứng.
- Tiền vi-VN theo mục 7; số âm, nộp thừa và nộp thêm có dấu và chữ, không chỉ màu.

#### 4. Bảng

- Cột chữ căn trái, cột tiền/số căn phải; header căn như dữ liệu, không căn giữa nhãn. Cùng số chữ số thập phân trong cột.
- Dòng tổng dùng đậm 600 và viền, không màu rực. Tổng thiếu kỳ phải ghi phạm vi đã cộng và trạng thái pha trộn.
- Bảng 12 tháng mobile cuộn ngang trong khung riêng, cố định cột tháng/hàng tiêu đề, nền sticky đặc để không đè chữ xuyên nhau.
- Biểu đồ là phần phụ, luôn có bảng số tương ứng: ưu tiên cột 12 tháng hoặc thanh ngang so sánh. Vành khuyên chỉ khi tối đa 3 phần có nhãn trực tiếp; không dùng biểu đồ ở Đ1 hoặc chỉ để trang trí.

#### 5. Trạng thái dữ liệu

- Thực tế, dự kiến, giả định khác nhau bằng nhãn, hình dạng SVG và viền liền/nét đứt/chấm theo mục 6; không chỉ dựa vào màu.
- Chú giải luôn có gần bảng; không giấu trạng thái khi thiếu chỗ. Mọi kết quả dự báo có `X/12 tháng là thực tế` và các nhóm còn lại.
- Số liệu pha trộn nêu cơ cấu, dữ liệu thiếu không thành 0; độ phủ khấu trừ và xác minh luật là hai thông tin riêng.

#### 6. Form

- Nhãn luôn hiện, placeholder không thay label; có gợi ý tìm chỉ tiêu ở đâu trên phiếu lương.
- Ô tiền dùng inputmode decimal, tự tách hàng nghìn ngay khi gõ theo mục 5; không làm đổi nghĩa decimal hoặc trường tiền ẩn của hồ sơ cũ.
- Lỗi có icon, cạnh ô, mô tả cách sửa; giữ nguyên input. Cảnh báo chồng giai đoạn và thiếu dữ liệu nằm ngay nơi liên quan, có link tới ô sửa.

#### 7. Khả năng truy cập tối thiểu để phát hành

- Chữ từ 4,5:1; viền input, biểu tượng mang nghĩa, vòng focus từ 3:1. Bàn phím dùng được, focus không bị che.
- Vùng bấm tối thiểu 24x24px, khuyến nghị 44px cho nút chính điện thoại; dùng table, th scope, label, button đúng nghĩa.
- Không phát hành màn hình khi chỉ có kiểm tự động mà chưa thử bàn phím, cuộn bảng và trạng thái không dựa vào màu.

#### 8. Nội dung

- Chữ thường đầu câu, động từ chủ động, tên nút chỉ đúng việc xảy ra; một hành động giữ một tên và thông báo hoàn tất tương ứng.
- Trống mời làm bước tiếp theo; lỗi nói chuyện sai và cách sửa, không xin lỗi chung chung. Dữ liệu thử hợp lý, luôn ghi `dữ liệu mẫu`.

#### 9. Quy trình rà soát cho mỗi màn hình

- Chụp và tự xem ảnh ở 375px, 768px, 1280px; dữ liệu gồm số 10 chữ số, 0, tháng thiếu, tên hồ sơ dài và nhiều cảnh báo.
- Chạy Lighthouse hoặc axe bản miễn phí, đo tương phản, thử cả luồng chỉ bằng bàn phím; ghi lỗi và ảnh trước/sau sửa.
- Kiểm trực tiếp: có bỏ được chi tiết trang trí nào không, có giá trị ngoài token không, có chỉ một hành động chính không?
- Sau khi code, bỏ ít nhất một chi tiết trang trí không giúp thao tác/đọc số và ghi thứ đã bỏ cùng lý do; không xóa trợ giúp hoặc tín hiệu trạng thái để đủ thủ tục.
- Ghi mọi ngoại lệ token vào `Ngoại lệ có chủ ý` bên dưới; chưa chụp/chưa thử thì ghi chưa kiểm, không suy từ CSS là đã đạt.

### Token CSS mẫu

Các giá trị cứng chỉ được khai báo ở file token. Đây là mẫu kế hoạch, chưa phải CSS đang chạy.
Bộ dưới đây đã có tại `src/TaxManager.Web/wwwroot/css/tokens.css`; không tự suy có một palette khác. Bốn màu ngữ nghĩa giữ giá trị từ bản plan trước; chỉ các vai trò brand/nền/chữ/viền được thay theo brief mới.

```css
:root {
  --brand: #395B40;
  --brand-tint: #E7FFEC;
  --bg: #F3F6F3;
  --surface: #FFFFFF;
  --text: #1A1F1B;
  --text-muted: #526057;
  --border-strong: #76867B;
  --success: #246647;
  --warning: #895600;
  --error: #B02A37;
  --info: #365C85;
  --on-brand: var(--surface);
  --focus: var(--brand);
  --divider: #DDE5DE;
  --backdrop: var(--text);
  --backdrop-opacity: .24;
  --font-family: "Inter", system-ui, sans-serif;
  --font-xs: .8125rem; /* 13px khi root 16px */
  --font-sm: .875rem;
  --font-base: 1rem;
  --font-lg: 1.25rem;
  --font-xl: 1.5rem;
  --font-title: 2rem;
  --font-regular: 400;
  --font-medium: 500;
  --font-semibold: 600;
  --line-body: 1.5;
  --line-heading: 1.25;
  --space-0: 0;
  --space-1: .25rem;
  --space-2: .5rem;
  --space-3: .75rem;
  --space-4: 1rem;
  --space-6: 1.5rem;
  --space-8: 2rem;
  --space-12: 3rem;
  --radius-control: .25rem;
  --radius-region: .5rem;
  --border-width: 1px;
  --nav-selected-border-width: 3px;
  --focus-width: 2px;
  --focus-offset: var(--space-1);
  --shadow: none;
  --content-max: 68.75rem;
  --month-input-width: 6rem;
  --month-field-width: 7rem;
  --profiles-table-min: 32rem;
  --actionbar-clearance: 10rem;
  --sidebar-width: 14rem;
  --prose-max: 72ch;
  --breakpoint-tablet: 48rem;
  --breakpoint-desktop: 64rem;
  --gutter-mobile: var(--space-4);
  --gutter-tablet: var(--space-6);
  --gutter-desktop: var(--space-8);
  --target-min: 1.5rem;
  --target-touch: 2.75rem;
  --icon-size: 1rem;
  --brand-mark-size: 1.75rem;
  --row-min: 3rem;
  --table-max-height: 28rem;
  --scroll-offset: var(--space-12);
  --motion-duration: 150ms;
  --motion-none: 0ms;
  --motion-ease: ease-out;
  --layer-base: 0;
  --layer-sticky: 10;
  --layer-intersection: 20;
  --layer-dialog: 30;
}
```

- Vai trò chữ: h1 24px, tiêu đề màn hình rộng tối đa 32px khi cần; h2 20px; thân bài/input/bảng 16px; ghi chú 14px; 13px chỉ metadata phụ, không dùng cho số quan trọng.
- Dùng 400 cho nội dung, 500 cho label, 600 cho heading/tổng; không dùng letter-spacing tăng cho nhãn.
- Khoảng nhỏ 4/8px, giữa trường 16px, giữa nhóm 24px, giữa vùng 32px; 48px chỉ tách phần lớn. Mọi giá trị qua token tương ứng.
- Input dùng border-strong, đường ngang bảng dùng divider; panel không có khung đậm. Nút chính surface trên brand. Khung thuế năm dùng on-brand trên brand; badge trong khung dùng chữ/viền on-brand không tô success/warning lên nền brand.
- Đo màu CSS thực tế: chữ phụ trên nền trang 6,09:1 và trắng 6,63:1; viền input trên trắng 3,84:1; on-brand trên brand 7,65:1. Nhãn thiếu dữ liệu trên trắng 6,19:1. Vẫn kiểm render và trạng thái tương tác riêng.
- Badge dùng success/info/warning trên surface hoặc bg; khung brand dùng on-brand cùng hình/viền trạng thái. Không tạo màu mới hoặc làm nhạt cả điều khiển gây mất tương phản.
- Dùng border-style phân biệt trạng thái, không gradient tạo vân. Nút phụ nền surface, chữ text hoặc brand có viền border-strong; disabled giữ độ đọc và có chữ giải thích.

### Danh sách thành phần cho phép

| Thành phần | Biến thể và hành vi | Dùng ở đâu |
|---|---|---|
| App layout | Sidebar dọc, thanh trên tên hồ sơ/nút chính, main sáng, skip link, footer | Mọi màn hình |
| Điều hướng | 6 đến 8 mục ở bản đầy đủ, mục chọn viền trái/đậm/nền, nút chữ Menu ở mobile, aria-current | Trong hồ sơ và tài khoản |
| Tiêu đề và ngữ cảnh | h1, năm, phạm vi, trạng thái lưu, một nút chính | Mọi màn hình |
| Button/link | Chính, phụ, nguy hiểm; nguy hiểm chỉ làm chính ở màn xác nhận | Lưu, tính, thêm/bỏ, xóa |
| Field | Label, hint, error, đơn vị; text/select/checkbox/radio native, textarea | Form |
| Ô tiền | Field text decimal, phân nhóm ngay khi gõ, đơn vị, phần lẻ dấu phẩy | Mọi số tiền nhập |
| Nhóm giai đoạn | Fieldset khoảng tháng + tiền + trạng thái; thêm/bỏ bằng button | Lịch lương/phụ thuộc |
| Metadata disclosure | Gom nguồn, ghi chú, thời điểm theo nhóm; trigger chữ, không lặp mũi tên; không giấu nhãn trạng thái | Giai đoạn lương, tháng đang sửa hoặc toàn bộ bản xem trước |
| Bảng dữ liệu | Header/row scope, số phải, tổng, khung cuộn/sticky | 12 tháng, bậc thuế, lịch sử |
| Badge trạng thái | SVG hình dạng + chữ + kiểu viền | Giá trị và kết quả |
| Độ phủ và chú giải | Văn bản số tháng + danh sách thiếu, không cần biểu đồ/donut | Bảng và dự báo |
| Khung thuế năm | Một khung brand duy nhất, tiền đầy đủ, trạng thái và độ phủ on-brand | Tổng quan năm |
| Biểu đồ tùy chọn | Cột tháng/thanh ngang, bảng số đi kèm; donut tối đa 3 phần và nhãn trực tiếp | Sau Đ1 khi có tác vụ so sánh rõ |
| Banner/status | Thông tin, thành công, cảnh báo, lỗi; có heading khi dài | Lưu, phạm vi, lỗi chung |
| Tóm tắt lỗi | Link đúng ô, focus sau POST; inline error cùng nội dung | Form |
| Disclosure căn cứ/cách tính | Details/summary, link nguồn, metadata và trạng thái luật | Kết quả |
| Trạng thái trống | Một câu mời và hành động đúng luồng | Danh sách, lịch sử, đối chiếu |
| Xác nhận | Trang xác nhận ưu tiên, dialog chỉ cho tác vụ nhỏ; mô tả phạm vi, hủy | Xóa, bỏ thay đổi, khôi phục |
| Bộ lọc nhỏ | Select năm/kỳ/nguồn native; không custom combobox | Danh sách và đối chiếu |

Không thêm toast, wizard, skeleton động hoặc icon library. Biểu đồ chỉ dùng các biến thể cho phép ở trên, chưa bắt buộc phải triển khai; SVG đơn giản cùng nét/cỡ token, không thêm màu chuỗi dữ liệu.

### Ngoại lệ có chủ ý

| Ngoại lệ | Phạm vi, lý do, cách kiểm |
|---|---|
| Breakpoint trong media query | CSS var không dùng trực tiếp trong điều kiện @media thông thường. Cho phép literal 48rem/64rem đúng token, chỉ trong site.css; comment dẫn tên token, kiểm khi sửa token. Không thêm preprocessor chỉ vì việc này. |
| Giá trị cấu trúc CSS | 0, auto, none, 100%, 1fr là cấu trúc, không phải cỡ thiết kế mới; cỡ nhìn thấy, màu, radius vẫn lấy token. |
| inputmode decimal | Ô tiền dùng decimal để hỗ trợ phần lẻ dấu phẩy theo yêu cầu định dạng mới; đã thử trình duyệt, chưa khẳng định bàn phím thiết bị thật. |
| System fallback khi font chưa tải | Tạm dùng system-ui qua token font-family để đọc được; không chốt typography/tabular bằng ảnh fallback. |

Chưa có ngoại lệ màu hoặc typography khác. Mỗi đề xuất phải ghi màn hình, token bị vượt, lý do, ảnh thử và điều kiện gỡ; không dùng bảng này để hợp thức hóa giá trị tùy ý.
Rà soát tự phê bình của kế hoạch: đã bỏ ý tưởng dải 12 chấm tiến độ trang trí, giữ văn bản độ phủ và bảng để không lặp thông tin. Đã rà màn hình thật trong đợt UI: bỏ vạch ngang của disclosure, thanh trên, footer và dòng tổng hợp theo phản hồi người dùng; dùng khoảng trắng thay thế. Chứng cứ trong báo cáo UI.

## 11. Các đợt phát hành UI

Các checklist theo dõi phần chuẩn hóa UI; không yêu cầu viết lại chức năng hiện có. Mỗi lần chỉ làm một màn hình, tái dùng thành phần đã xong.
Chặng 3 đã done; phần chuẩn hóa Đ1–Đ2 và màn tháng gắn kết quả tính với hồ sơ forecast đã triển khai. Xem [kiểm chứng màn tháng](docs/reviews/2026-11-28-forecast-month.md). Thông tin định danh/đăng ký người phụ thuộc vẫn cần contract riêng; công cụ demo tháng giữ độc lập.

| Đợt UI | Liên hệ roadmap / PLAN | Trạng thái nền ngày 28/11 và việc UI còn làm |
|---|---|---|
| Đ1 | Chặng 1–2, bước 3.1/3.1b | Đã chuẩn hóa token/sidebar/form/bảng và nối chi tiết tháng từ forecast đã lưu; HTTP, host mới, menu/lỗi bằng phím và reflow đã kiểm. Còn luồng bàn phím đầy đủ, focus khi cửa sổ hoạt động, screenreader và zoom native. |
| Đ2 | Chặng 3, bước 3.3 | Nền chặng 3 đã done. Đã tái bố trí kết quả năm, thưởng, phụ thuộc theo số lượng và lịch sử; giữ service hiện có. |
| Đ3 | Chặng 4, bước 4.1/4.2 | Đã có báo cáo/checklist dữ liệu, CSV và backup/khôi phục có mật khẩu vào kho riêng theo [contract](docs/design/protected-backup.md), xem [kiểm chứng](docs/reviews/2026-11-28-protected-backup.md). Nghiệm thu UX tổng thể vẫn còn mở. |
| Đ4 | Chặng 5 | Chưa triển khai; chỉ bắt đầu sau chặng 4. |
| Đ5 | Chặng 6 | Chưa triển khai; chỉ bắt đầu sau chặng 5. |
| Đ6 | Bước 3.2 và quyền dữ liệu 4.1 | Chưa triển khai; điều kiện trước phục vụ nhiều người qua mạng, không phải chặng sản phẩm 6. |

### Đợt 1. Bản nhỏ nhất dùng được: nhập, lưu, mở và kiểm tháng

- [x] Tạo tokens.css đúng bộ màu chốt, sidebar/thanh trên/light mode, field tiền, badge, bảng, disclosure, error summary; thử font tiếng Việt và tabular-nums.
- [x] Tách danh sách `/ho-so` khỏi form `/Forecast`; bảng không hiện UUID, trạng thái Thiếu/Đủ dữ liệu và số phiên bản; mỗi màn có một nút chính. Sau xóa trở về danh sách; giữ các URL mở/sửa hồ sơ cũ.
- [x] Form giai đoạn, nguồn/ghi chú, ngoại lệ và bảng 12 tháng; chỉ một nút lưu chính.
- [x] Chi tiết tháng gắn hồ sơ forecast qua service chỉ đọc, bước tính/bậc/căn cứ đúng kỳ, xử lý thiếu/0/thưởng, mở editor đúng tháng và lưu chung hồ sơ. Kết quả tính khi mở, chưa lưu lịch sử tháng.
- [x] Công cụ `/` vẫn tính tháng 2025/2026; dữ liệu mẫu tách khỏi hồ sơ đang dùng.
- [x] Thử mất thay đổi, xung đột revision, lỗi tiền, chồng giai đoạn, thiếu khác 0 và giữ ngày nhập.
- [x] Kiểm từng tương tác bằng phím: menu Enter/Space/Tab/Shift+Tab/Escape, liên kết lỗi tới đúng ô, gõ tiền và lưu/mở kết quả tháng; reflow 320–1280px và cỡ chữ 200%. Xem [bằng chứng và giới hạn](docs/reviews/2026-11-28-ui-accessibility.md).
- [ ] Nghiệm thu UX đầy đủ: luồng tạo/lưu, giai đoạn/ngoại lệ, mở qua host mới, sửa và đọc kết quả đã qua HTTP; ảnh và axe đã qua. Còn luồng chỉ bàn phím từ đầu đến cuối, viền focus không bị che khi cửa sổ có focus, screenreader và zoom native 200%/400%; không thay bằng activeElement hoặc kết quả tự động.

### Đợt 2. Tổng hợp năm, đầu vào bổ sung và lịch sử

- [x] Chỉ chuẩn hóa UI sau khi mã contract/engine/lưu snapshot năm hiện có được nghiệm thu chặng 3; đọc lại bằng chứng ở đầu file để cập nhật trạng thái.
- [x] Tái dùng form thưởng có tháng nhận/đã gồm gross và số phụ thuộc theo giai đoạn; thông tin từng người/đăng ký là mở rộng cần phiếu và contract riêng, không lén thêm vào việc đổi bố cục.
- [x] Tổng quan năm và phần cách tính/bậc năm/căn cứ cùng trang; độ phủ nghĩa vụ và khấu trừ riêng.
- [x] Lịch sử/chi tiết snapshot dùng chung kết quả đọc; tính lại tạo bản mới, giữ bản cũ.
- [x] Xong khi: đủ dữ liệu ra nghĩa vụ năm đúng engine; thiếu khấu trừ không hiện chênh lệch; số tháng trạng thái đúng; đọc bản cũ không đổi sau cập nhật; QA qua.

### Đợt 3. Báo cáo, checklist, sao lưu và khôi phục

- [x] Một trang báo cáo/checklist dữ liệu, xuất bảng tham khảo đọc được bằng Excel; giữ revision, metadata, trạng thái và kết quả năm đã lưu. Xem [contract](docs/design/forecast-report.md). Checklist không thay điều kiện quyết toán pháp lý.
- [x] Chọn CSV UTF-8 BOM, quoting/CRLF, số decimal invariant và bảo vệ ô chữ khỏi công thức; không gọi CSV là bản sao lưu.
- [x] Trang sao lưu và xem trước khôi phục; kiểm loại/phiên bản, schema/catalog/sentinel, trùng hồ sơ, metadata và lịch sử trước khi mở kho riêng; không merge/ghi đè kho gốc.
- [x] Bản sao có mật khẩu, AES-GCM và PBKDF2 theo contract; UI nêu không phục hồi mật khẩu và chọn kho chỉ tồn tại trong phiên chạy. Khóa đọc kho nằm trong gói mã hóa, không có key sidecar.
- [ ] Xong khi: báo cáo khớp hồ sơ, giữ nhãn/phần thiếu; backup khôi phục được metadata/lịch sử/phiên bản; dữ liệu hỏng bị từ chối, không ghi đè âm thầm; QA qua.

### Đợt 4. Hai kịch bản

- [ ] Một màn hình cơ sở và thêm thưởng giả định có tiền/tháng; không thêm nhiều thanh trượt.
- [ ] Dùng chung dòng kết quả, độ phủ và cách tính; trạng thái khoản giả định không ghi ngược vào hồ sơ thực tế.
- [ ] Xong khi: hai kết quả đọc riêng được, không cộng trùng gross, kết quả bằng nhau không tạo khoảng giả, không gọi khoảng là bảo đảm; QA qua.

### Đợt 5. Đối chiếu nguồn

- [ ] Form nguồn/kỳ/chỉ tiêu/metadata và bảng đối chiếu; không tích hợp tài khoản eTax.
- [ ] Xong khi: so cùng nghĩa/kỳ, thiếu không thành 0, có dấu và hướng chênh lệch, người thử không hiểu là kết luận công ty sai; QA qua.

### Đợt 6. Tài khoản và quyền dữ liệu trước phục vụ qua mạng

- [ ] Đăng ký/đăng nhập dùng Identity, kiểm hai tài khoản không truy cập chéo hồ sơ hoặc lịch sử bằng URL/POST.
- [ ] Cho paste và password manager; không tự xây captcha hoặc cơ chế xác thực mới trong thiết kế.
- [ ] Đồng ý xử lý dữ liệu, xuất dữ liệu cá nhân, xác nhận xóa tài khoản/dữ liệu và mô tả giới hạn bản sao.
- [ ] Xong khi: kiểm cách ly quyền ở server, các luồng xuất/xóa đúng phạm vi, cookie/HTTPS/CSRF và review bảo mật cần thiết đã hoàn thành; QA qua.
- [ ] Số đợt không phải quyền public deploy: nếu cần nhiều người qua mạng sớm, đưa điều kiện Đ6 lên trước; demo local vẫn dùng synthetic cho tới khi đủ điều kiện dữ liệu thật.

### Gộp hoặc cắt để một người làm được

| Hạng mục | Quyết định |
|---|---|
| Nhập giai đoạn và bảng 12 tháng | Gộp cùng hồ sơ, có anchor; giảm chuyển trang và lặp form. |
| Chi tiết năm, căn cứ, từng bậc | Gộp vào tổng quan bằng disclosure; không thêm ba màn hình độc lập. |
| Checklist và báo cáo | Gộp một trang, tái dùng dữ liệu độ phủ. |
| Lịch sử danh sách/chi tiết | Chung kết quả đọc; không có so sánh hai lần ở Đ2. |
| PDF đẹp, dark mode, tìm kiếm nâng cao | Cắt khỏi các đợt đầu; biểu đồ chỉ thêm sau Đ1 nếu có tác vụ so sánh cụ thể, theo mục 10.4. |
| Dán Excel, sao chép năm mới, so sánh hai lần, Gross-Net | Backlog; chừa điểm mở rộng ở nhập/hồ sơ/lịch sử/công cụ, chưa thêm route hay nút. |
| AI cập nhật luật | Backlog riêng; chừa vị trí metadata nguồn/phiên bản và thông tin ảnh hưởng, chưa có luồng quản trị AI. |
| Nhiều nơi trả lương, freelance/kinh doanh, nước ngoài | Chừa trường phạm vi ở header; không thêm payer/tab thu nhập khi chưa có nghiệp vụ được duyệt. |
| OCR, XML | Chừa ranh giới nhập/xuất, chưa upload file hay tạo tờ khai; XML cần cấu trúc chính thức và thử import riêng. |

## 12. Quy tắc cho công cụ vibecode khi code giao diện

Ngày 28/11/2026: dùng thêm [Hallmark v2](https://github.com/nutlope/hallmark/tree/04977e15f3dfd6ee4b74c887dac3eb52665d6c92/skills/hallmark) theo yêu cầu người dùng, cùng frontend-design và ui-ux-pro-max. Áp dụng nhánh app screen/polish trên hệ thống xanh đã chốt; không bốc theme/scaffold mới cho mỗi màn. Bảng ưu tiên nhịp hàng, chữ và số; nhãn trạng thái không cần khung riêng, thao tác ngắn nhưng accessible name đủ ngữ cảnh. Lượt kiểm tĩnh, render Orca và review độc lập ghi tại [báo cáo](docs/reviews/2026-11-28-protected-backup.md); cache/log trong `.hallmark/`. Không lấy số gate hoặc báo cáo tự động thay việc nhìn màn hình thật.

- Mỗi lần chỉ làm một màn hình và các phần dùng chung tối thiểu của màn hình đó; không triển khai cả roadmap trong một lượt.
- **Chỉ dùng token, không viết giá trị rời.** Ngoại lệ phải ghi ở mục 10 trước khi áp dụng.
- Đọc roadmap và trạng thái đầu file trước mỗi lượt; không khôi phục nhận định cũ chưa có tính năm/lịch sử. Sidebar, brand xanh, tiền đầy đủ `đ` theo bản 28/11; không quay về palette tím/thanh ngang cũ.
- Dùng chung Razor layout, partial field/error/badge/bảng/disclosure; không copy CSS khác nhau cho cùng hành vi.
- Chỉ dùng thành phần trong mục 10; muốn thêm thì sửa kế hoạch và nêu lý do trước, không tự thêm tính năng ngoài file này.
- Trước code mỗi màn hình, ghi palette/font/layout, ASCII và căn lề; đối chiếu mặc định rồi chỉnh. Chỉ code sau khi bước đối chiếu đã xong.
- Giữ nguồn trạng thái theo mục 6 ở tất cả số; không tự mặc định thực tế, 0, khấu trừ, bảo hiểm hoặc số người phụ thuộc.
- Không bịa số liệu luật, căn cứ, kỳ áp dụng hoặc trạng thái Verified; nhận từ rule/snapshot của service, không hardcode phép tính trong Razor/JS.
- Giữ hợp đồng ForecastAmountBinder, ModelState và metadata; format hiển thị không được đổi nghĩa chuỗi hoặc reset ngày nhập.
- Chỉ hỗ trợ nhánh nghiệp vụ backend đã có contract/kiểm chứng; optional graph/catalog cần mở rộng riêng, không giả lập dữ liệu để UI trông hoạt động.
- POST sai giữ input và mở phần có lỗi; Enter không xóa; lưu/tính lại đúng transaction/service và snapshot bất biến hiện có.
- Không dùng ID route làm phân quyền, không bỏ guard synthetic để chạy demo; không đụng migration cũ, DB thật hay khóa thật trong tác vụ UI.
- Sau code, tự nhìn ảnh ba kích thước theo mục 10.9, bỏ một chi tiết trang trí thừa, kiểm token và ghi bằng chứng; nếu chưa chạy được browser thì báo chưa kiểm.
- Chạy kiểm tra phù hợp và `bint test affected --run` sau sửa mã ngoài tác vụ Bint; nếu chạm script/csproj/global.json thì chạy đầy đủ cổng Web MVP theo quy tắc repo.
- Với tài liệu kế hoạch hiện tại chỉ kiểm cấu trúc/nội dung, không đánh dấu checkbox triển khai hoặc báo UI đã qua QA.

## 13. Rủi ro UX và cách thử nhanh với 3 đến 5 người

Chọn 3 đến 5 người có nhận lương, mức hiểu thuế khác nhau; mỗi phiên 30 đến 40 phút, chỉ dùng dữ liệu mẫu, không xin phiếu lương thật.
Thử prototype/HTML local của từng đợt, một người điều phối, ghi lỗi theo tác vụ; chưa có người thử thì ghi chưa có bằng chứng.

| Rủi ro | Bài thử | Dấu hiệu cần sửa |
|---|---|---|
| Hiểu dự kiến là đã nhận | Cho hồ sơ có tháng thực tế, dự kiến, giả định; hỏi số nào đã phát sinh và vì sao | Người thử gọi số dự báo là số đã nhận hoặc chỉ nhìn màu để phân biệt. |
| Hiểu gross thực tế là cả tháng đủ | Cho gross thực tế nhưng thiếu bảo hiểm/phụ thuộc; hỏi X/12 có tăng không | Người thử tin tháng đã đầy đủ; cần làm rõ định nghĩa bộ đếm và phần thiếu. |
| Nhầm chưa nhập với 0 | Yêu cầu ghi tháng không phát sinh khấu trừ và tháng chưa có phiếu lương | Cả hai được điền 0 hoặc người thử không biết để trống được. |
| Nhầm nộp thừa với được hoàn | Cho kết quả có nộp thừa và trường hợp thiếu khấu trừ | Người thử khẳng định chắc chắn nhận lại tiền, hoặc muốn lấy 0 cho tháng thiếu. |
| Ghi đè ngoại lệ | Tăng lương từ tháng 7, giữ một tháng đã nhập riêng | Người thử không dự đoán được tháng nào sẽ đổi, bỏ sót cảnh báo. |
| Format tiền đổi nghĩa | Nhập 10 chữ số, 0, `1234,50`, chuỗi sai, blur/focus rồi gửi | Số sau lưu khác dự định, phần lẻ mất hoặc bàn phím không nhập được. |
| Metadata khó tìm | Yêu cầu chỉ ra nguồn, ghi chú, ngày của một ô và nguồn luật kết quả | Không tìm được hoặc nhầm nguồn phiếu lương với căn cứ pháp lý. |
| Đối chiếu bị hiểu là kết luận sai luật | Cho hai số cùng kỳ lệch nhau, yêu cầu diễn giải | Kết luận công ty/eTax sai chỉ từ chênh lệch. |
| Bảng/keyboard khó dùng | Sửa tháng 8 trên điện thoại, mở căn cứ và đi qua form bằng phím | Mất cột tháng, focus bị che, không quay lại được ô lỗi. |

Tiêu chí nội bộ: ít nhất 3 người làm được luồng chính không được chỉ vị trí nút; không chấp nhận lỗi đổi giá trị, ghi đè, nhầm dự báo là thực tế hoặc nộp thừa là chắc được hoàn.
Nếu một người mắc lỗi hiểu sai nghiêm trọng, sửa nhãn/bố cục rồi thử lại tác vụ đó; không dùng điểm trung bình để che lỗi.
Ghi thời gian hoàn thành, chỗ cần trợ giúp, câu diễn giải của người thử và ảnh trước/sau; không coi 3 đến 5 người là xác nhận thị trường.

## 14. Giả định và câu hỏi còn mở

| Câu hỏi | Hai hướng | Mặc định / lý do |
|---|---|---|
| Ưu tiên thiết bị nào? | Điện thoại trước hoặc desktop nhập dài | Desktop sidebar, mobile thu điều hướng và sửa tháng; thử 375/768/1280px. |
| Có tài khoản ngay? | Local một người hoặc nhiều tài khoản | Local synthetic ở đầu; tài khoản/cách ly bắt buộc trước dùng nhiều người qua mạng. |
| Hồ sơ tên gì? | Chỉ năm hoặc tên gợi nhớ | Tên gợi nhớ không bắt buộc; cần contract lưu trước khi thêm field, không dựng tên lưu giả. |
| Bảo hiểm có tự tính? | Tự tính hoặc nhập phiếu lương | Nhập tay; không mở rộng luật/đối tượng trong tác vụ UI. |
| Chồng giai đoạn? | Ghi đè có xác nhận hoặc bắt sửa khoảng | UI đích bắt sửa; khác preview hiện tại nên triển khai như thay đổi có kiểm chứng. |
| Độ chính xác hiển thị? | Làm tròn đồng hoặc giữ số lẻ | Giữ chính xác, đồng nhất toàn cột; chưa tự thêm rounding policy. |
| Bàn phím có nhập số lẻ được? | Decimal theo hợp đồng định dạng mới | Đã ghi ngoại lệ inputmode decimal; vẫn cần thử điện thoại vật lý. |
| Báo cáo đầu tiên? | CSV tương thích Excel hoặc XLSX/PDF | CSV tham khảo trước; PDF đẹp và XLSX chỉ làm khi có nhu cầu, backup tách biệt. |
| Nguồn có bắt buộc khi chọn thực tế? | Bắt buộc hoặc khuyến khích | Khuyến khích, có thể lưu chưa ghi nguồn; không gọi là đã xác minh. |
| Xóa hồ sơ giữ gì? | Xóa thường hoặc purge có mục đích | Dùng đúng service hiện có và nói rõ phạm vi; không hứa xóa WAL/backups hay tự cung cấp purge vận hành. |
| Khôi phục khi trùng? | Ghi đè hoặc tạo bản riêng | Đã chốt: mở toàn kho riêng sau xem trước, giữ ID/lịch sử, không merge hay ghi đè. Có thao tác quay về kho ban đầu. |
| Bảo vệ bản sao bằng gì? | Bí mật do người dùng giữ hoặc cơ chế quản lý khóa phù hợp môi trường | Đã chốt [contract v1](docs/design/protected-backup.md): AES-256-GCM, PBKDF2-SHA256 600.000 vòng, mật khẩu do người dùng giữ, không key sidecar; chỉ synthetic. |
| Khi nào dữ liệu thật/public? | Sau UI hoặc sau điều kiện vận hành | Chỉ sau review bảo mật, quyền dữ liệu, auth/cách ly cần thiết; kế hoạch này không cấp quyền triển khai. |

### Lưu ý trung thực

- Đây là bộ rào chắn, không bảo đảm hết “mùi AI”. Bước quyết định vẫn là nhìn từng màn hình thật và sửa tay ở lượt cuối.
- Chưa khảo sát giao diện các công cụ thuế Việt Nam; không kết luận chúng có hay không có các lỗi bị cấm trong brief.
- Khuyến nghị font và line-height tiếng Việt là thực hành cần thử; chưa tải/đo file font hoặc chụp UI thì chưa coi là đã kiểm chứng.
- Token màu là lựa chọn thiết kế; phải đo từng cặp thực dùng, trạng thái focus/lỗi và ảnh render, không suy chất lượng từ bảng màu.
- UI các luồng hiện có đã triển khai và chạy axe/QA như báo cáo ngày 28/11/2026; chưa thử với nhóm người dùng độc lập hoặc điện thoại vật lý. Các checkbox còn mở vẫn là điều kiện chưa đạt, không phải kết quả nghiệm thu.
