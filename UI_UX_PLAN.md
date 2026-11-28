# Kế hoạch UI/UX TaxManager

Ngày lập: 27/11/2026; đồng bộ ngày 28/11/2026. Phạm vi: kế hoạch thiết kế và tiến độ triển khai UI trên các luồng hiện có.
Nguồn trạng thái: [roadmap](docs/product-roadmap.md); kế hoạch kỹ thuật: [PLAN.md](PLAN.md); vận hành: [Web MVP](docs/web-mvp.md).
Khi tài liệu thiết kế cũ khác trạng thái hiện tại, dùng roadmap và mã hiện tại để đối chiếu; không đánh dấu tính năng đã có chỉ vì có wireframe.
Nền 3.1/3.1b, chặng 2 và chặng 3 đã nghiệm thu kỹ thuật; phiếu chặng 3 đã done. UI Đ1–Đ2 trên các route hiện có đã triển khai, gồm định dạng tiền khi gõ và chú thích gross/net theo phản hồi ngày 28/11. Xem [kiểm chứng UI](docs/reviews/2026-11-28-ui-phases-one-two.md) và [kiểm chứng chặng 3](docs/reviews/2026-11-27-annual-stage-three.md).
Màn chi tiết tháng đã nối với bản forecast lưu theo ID/tháng/revision, có bước tính, bậc thuế và căn cứ; xem [kiểm chứng màn tháng](docs/reviews/2026-11-28-forecast-month.md). Đã bổ sung menu thu gọn và liên kết lỗi với ô nhập; thao tác phím, lưu tháng và reflow đã kiểm từng phần tại [báo cáo khả năng truy cập](docs/reviews/2026-11-28-ui-accessibility.md). Còn nghiệm thu UX thủ công đầy đủ; dark mode hiện là hướng tham khảo từ ảnh người dùng, chưa triển khai.
Quy tắc phối hợp: roadmap quyết định phạm vi/thứ tự/tiến độ; PLAN quyết định điều kiện kỹ thuật; file này quyết định bố cục, token và hành vi UI. Khi triển khai, cập nhật cả ba liên kết/trạng thái trong cùng tác vụ, không lấy checkbox UI thay nghiệm thu nghiệp vụ.
Bổ sung nghiên cứu anti AI slop ngày 28/11/2026 tại [mục 15](#15-nghiên-cứu-anti-ai-slop-và-tiêu-chí-áp-dụng): nguồn web/GitHub, quyết định tiếp thu và cổng review; chưa phải nghiệm thu UI.

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
| Hai kịch bản | `/ho-so/{id}/kich-ban` | Thử thêm thưởng giả định | Cơ sở, khoản bổ sung, hai kết quả | so sánh kịch bản | Đ4; đã triển khai local synthetic |
| Đối chiếu nguồn | `/ho-so/{id}/doi-chieu` | So số cùng nghĩa và kỳ | Hồ sơ, công ty/eTax, chênh lệch | đối chiếu số liệu | Đ5; đã có nhập thủ công, không lưu phép đối chiếu |
| Đăng ký | `/tai-khoan/dang-ky` | Tạo tài khoản | Trường tài khoản, điều kiện dữ liệu thử | tạo tài khoản | Đ6a/b local synthetic đã có |
| Đăng nhập | `/tai-khoan/dang-nhap` | Mở dữ liệu của mình | Trường xác thực, lỗi có hướng sửa | đăng nhập | Đ6a local synthetic đã có |
| Quyền dữ liệu | `/tai-khoan/du-lieu` | Kiểm soát dữ liệu cá nhân | Lựa chọn đồng ý, phạm vi xuất/xóa | lưu lựa chọn dữ liệu | Đ6; chưa có |
| Xuất dữ liệu cá nhân | `/tai-khoan/xuat-du-lieu` | Lấy dữ liệu kho gốc | Phạm vi, JSON không mã hóa, mật khẩu hiện tại | tải dữ liệu tài khoản | Đ6c local synthetic đã có; chưa nghiệm thu dữ liệu thật |
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
Bố cục: sidebar 224px có logo nhỏ và icon nét đồng bộ; thanh trên chỉ chứa huy hiệu Demo local. Danh sách và editor là hai màn riêng, mỗi màn một nút chính; editor đặt thanh thao tác ở cuối form theo luồng trang, không dùng sticky footer che nội dung. Nội dung sáng một cột, tối đa 1100px; văn bản 72ch, lề 16/24/32px theo thiết bị. Chỉ chế độ sáng, CSS theo vai trò để thêm dark mode sau.
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
- Mỗi trạng thái màn hình có một hành động chính và một thông tin ưu tiên; các hành động phụ vẫn hiện tại nơi cần dùng. Lưu là hành động chính của editor, Preview là submit mặc định an toàn khi nhấn Enter; thứ tự DOM và độ nổi thị giác phục vụ hai yêu cầu khác nhau. Không thêm thành phần ngoài danh sách ở dưới.
- Các lựa chọn màu, cỡ, khoảng cách, bo góc chỉ xuất hiện tại token; CSS thành phần chỉ tham chiếu var(), không chép hex/px ra từng màn hình.
- Chỉ chế độ sáng; dark mode để sau và chỉ thay token theo vai trò. Sidebar chọn dùng brand-tint, chữ đậm 600, viền trái 3px brand, aria-current; giữ sẵn chỗ viền ở các mục khác để không nhảy bố cục.
- Thanh trên chứa huy hiệu Demo local; hành động chính theo từng màn hình. Sidebar 6 đến 8 mục khi đủ chức năng, dính theo viewport và cuộn riêng nếu cửa sổ thấp. Mỗi mục chuyển tới đúng phần, mục hiện tại tự cập nhật theo nội dung khi cuộn tay (aria-current=location). Liên kết trong editor chỉ đổi fragment, giữ form chưa lưu và query POST. Mobile giữ thanh Menu dính; menu mở phủ lên nội dung, đóng trước khi cuộn tới phần, không che tiêu đề đích. Chỉ khung thuế năm ước tính dùng nền brand; không xen kẽ khung đậm/nhạt để trang trí.

#### 2. Danh sách cấm

- Hạn chế vạch phân chia, đặc biệt đường ngang kéo dài hết vùng nội dung. Không thêm `hr` hoặc `border-top`/`border-bottom` chỉ để tách các phần, disclosure, thanh trên hay footer; ưu tiên khoảng trắng và tiêu đề. Chỉ giữ viền có chức năng rõ ở ô nhập, bảng dữ liệu, vùng xác nhận/lỗi và focus. Đây là điều chỉnh theo phản hồi trực tiếp ngày 28/11/2026.
- Không dùng accordion/dropdown làm bố cục mặc định. Hiện trực tiếp các phần nhập chính, khoản thưởng, giải thích gross/net và bảng bậc thuế; chỉ thu gọn nguồn, ghi chú hoặc nội dung tra cứu dài theo nhóm. Không lặp mũi tên tam giác/chevron trước từng tiêu đề, từng dòng hoặc từng căn cứ; trigger còn lại dùng chữ gạch chân, giữ ngữ nghĩa native và bàn phím. Không thay mũi tên bằng chuỗi dấu cộng hay icon trang trí khác. Đây là điều chỉnh visual theo phản hồi người dùng ngày 28/11/2026.
- Không gradient, kính mờ, blob, minh họa có sẵn, emoji làm icon; không thẻ giống nhau phủ kín trang hoặc thẻ lồng thẻ.
- Không khối liên kết nhanh lấp chỗ trống, nhãn IN HOA nhóm menu, nền hình tròn hoặc avatar minh họa; không dùng danh sách thẻ giống nhau thay bảng.
- Không dùng hero số lớn/nhãn nhỏ để đẩy công việc chính xuống dưới. Tổng thuế năm vẫn được nhấn theo mục 4.5, kèm kỳ, trạng thái và căn cứ. Không tự đổi hệ xanh đã chốt sang nền kem với serif/đất nung, nền gần đen với neon hoặc bố cục báo giấy chỉ để tạo vẻ khác lạ.
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
- Sau khi code, rà từng chi tiết trang trí theo tác dụng với thao tác/đọc số; bỏ chi tiết không có tác dụng và ghi lý do. Nếu không có gì thừa, ghi rõ kết luận; không bắt buộc xóa một chi tiết hoặc làm mất trợ giúp/trạng thái để đủ thủ tục.
- Áp dụng ma trận trạng thái và điều kiện chặn tại mục 15.4; ảnh đẹp ở trạng thái đủ dữ liệu chưa đủ để kết thúc review.
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
| Đ4 | Chặng 5 | Đã triển khai màn hai kịch bản sau kiểm chứng chặng 4; [contract](docs/design/forecast-scenarios.md), [kiểm chứng](docs/reviews/2026-11-28-forecast-scenarios.md). Nghiệm thu UX tổng thể còn mở. |
| Đ5 | Chặng 6 | Đã triển khai đối chiếu nhập tay chỉ đọc; [contract](docs/design/forecast-reconciliation.md), [kiểm chứng](docs/reviews/2026-11-28-forecast-reconciliation.md). Nghiệm thu UX tổng thể còn mở. |
| Đ6 | Bước 3.2 và quyền dữ liệu 4.1 | Đ6a đã có Identity opt-in, HTTPS và kho synthetic riêng mỗi tài khoản; [contract](docs/design/local-accounts.md). Đồng ý/xuất/xóa tài khoản và review bảo mật còn mở; chưa cho phục vụ qua mạng, không phải chặng sản phẩm 6. |

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

- [x] Một màn hình cơ sở và thêm thưởng giả định có tiền/tháng; không thêm nhiều thanh trượt. Route `/ho-so/{id}/kich-ban`, mở từ Tổng quan năm.
- [x] Dùng chung dòng kết quả, độ phủ và cách tính; trạng thái khoản giả định không ghi ngược vào hồ sơ thực tế. Giữ thưởng đã lưu, phần đã gồm gross; không tạo bản lịch sử từ phép thử.
- [ ] Xong khi: hai kết quả đọc riêng được, không cộng trùng gross, kết quả bằng nhau không tạo khoảng giả, không gọi khoảng là bảo đảm; QA qua.

### Đợt 5. Đối chiếu nguồn

- [x] Form nguồn/kỳ/chỉ tiêu/metadata và bảng đối chiếu; không tích hợp tài khoản eTax. Bản đầu chỉ đọc hồ sơ, không lưu số đối chiếu.
- [x] Summary lỗi theo thứ tự form; từ kết quả có “Sửa số nguồn” giữ dữ liệu, focus đầu form rồi Tab tới nguồn. Đã kiểm đoạn thao tác này ở mobile; [bằng chứng và giới hạn](docs/reviews/2026-11-28-forecast-reconciliation.md#bổ-sung-luồng-sửa-số-và-bàn-phím).
- [ ] Xong khi: so cùng nghĩa/kỳ, thiếu không thành 0, có dấu và hướng chênh lệch, người thử không hiểu là kết luận công ty sai; QA qua.

### Đợt 6. Tài khoản và quyền dữ liệu trước phục vụ qua mạng

- [x] Đ6a local synthetic: đăng ký/đăng nhập bằng Identity, cookie Secure/HttpOnly, CSRF, lockout, kho/khóa riêng theo tài khoản; kiểm URL/POST/backup/restore và host mới bằng hai tài khoản. Xem [contract](docs/design/local-accounts.md) và [kiểm chứng, giới hạn](docs/reviews/2026-11-28-local-accounts.md).
- [x] Đ6b local synthetic: thông báo phạm vi và checkbox không chọn sẵn khi đăng ký; ghi phiên bản/thời gian ở server, tài khoản cũ xác nhận trước route dữ liệu. Có màn đọc lại nội dung và trạng thái ở tài khoản. [Contract](docs/design/local-data-use.md), [711 test và visual QA](docs/reviews/2026-11-28-local-data-use.md); chưa thay nghiệm thu pháp lý/quyền dữ liệu toàn bộ ở dưới.
- [x] Đ6c local synthetic: xuất JSON kho gốc có kiểm lại mật khẩu, không lẫn kho khôi phục; gồm lịch sử/phiên bản/audit còn giữ, không có khóa hoặc trường bảo mật Identity. [Contract](docs/design/account-data-export.md), [kiểm chứng](docs/reviews/2026-11-28-account-export.md). Thông báo dữ liệu thử v2 yêu cầu xác nhận lại, xóa tài khoản còn mở.
- [ ] Đăng ký/đăng nhập dùng Identity, kiểm hai tài khoản không truy cập chéo hồ sơ hoặc lịch sử bằng URL/POST.
- [ ] Cho paste và password manager; không tự xây captcha hoặc cơ chế xác thực mới trong thiết kế.
- [ ] Đồng ý xử lý dữ liệu, xuất dữ liệu cá nhân, xác nhận xóa tài khoản/dữ liệu và mô tả giới hạn bản sao.
- [ ] Xong khi: kiểm cách ly quyền ở server, các luồng xuất/xóa đúng phạm vi, cookie/HTTPS/CSRF và review bảo mật cần thiết đã hoàn thành; QA qua.
- [ ] Số đợt không phải quyền public deploy: nếu cần nhiều người qua mạng sớm, đưa điều kiện Đ6 lên trước; demo local vẫn dùng synthetic cho tới khi đủ điều kiện dữ liệu thật.

Phản hồi visual 28/11: bộ chọn dữ liệu tháng trong editor dùng lưới 12 nút ngắn (6 cột desktop, 3 cột mobile), trạng thái đang sửa/đã nhập/chưa nhập bằng chữ, không lặp dropdown và 12 nút dài. Dropdown là fallback khi không có JavaScript; giữ đường submit Preview mặc định. Chênh lệch đối chiếu dùng xanh cho dương, đỏ trầm cho âm, trung tính cho 0; luôn giữ dấu và hướng bằng chữ, màu không kết luận nguồn đúng/sai.

Phản hồi công cụ tháng `/`: không bày 12 form lặp khi JavaScript hoạt động. Nhập chung theo khoảng tháng, xem thay đổi trước ghi đè, hoàn tác giữ sửa riêng; chọn một tháng để chỉnh. Không suy bảo hiểm/khấu trừ từ gross và không tự lấp 0. Kết quả trước phải được đánh dấu cũ sau sửa; giữ 36 input enabled/marker vi-VN khi POST. [Thiết kế](docs/design/monthly-entry-redesign.md), [bằng chứng](docs/reviews/2026-11-28-monthly-entry.md).

Summary lỗi không trình bày thành menu hyperlink: không khung đỏ đậm, không bullet hoặc gạch chân thường trực; heading gọn dùng màu lỗi, câu lỗi màu chữ thường. Giữ link đến trường để sửa nhanh bằng chuột/bàn phím; hover/focus mới gạch chân, có focus ring. Lỗi inline và aria-invalid vẫn chỉ đúng trường cần sửa.

Form tài khoản gọn đặt summary ngay trong panel nhập: icon cảnh báo, tiêu đề ngắn và câu lỗi thuần chữ theo thứ tự trường. Chỉ một hành động “Sửa thông tin” đưa focus về ô lỗi đầu; không biến từng câu lỗi thành hyperlink. Giữ focus summary sau POST lỗi và thông báo ngay dưới trường.

Thao tác quen thuộc có vị trí rõ như hiện/ẩn mật khẩu dùng icon mắt trong từng ô, không thêm hàng nút chữ. Icon-only phải có accessible name, phản ánh trạng thái, focus rõ và vùng bấm ít nhất 44 × 44 px. Giữ chữ cho hành động nghiệp vụ, điều hướng và sửa lỗi khi icon đơn lẻ dễ gây nhầm; không đổi toàn bộ nút thành icon.

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
- Trước code mỗi màn hình, ghi tác vụ, dữ liệu, palette/font/layout, ASCII và căn lề; dùng nguồn và quy trình mục 15. Chỉ so phương án bố cục khi còn quyết định chưa rõ; không tạo lại theme hoặc nhiều biến thể cho thay đổi nhỏ. Chỉ code sau khi bước đối chiếu đã xong.
- Giữ nguồn trạng thái theo mục 6 ở tất cả số; không tự mặc định thực tế, 0, khấu trừ, bảo hiểm hoặc số người phụ thuộc.
- Không bịa số liệu luật, căn cứ, kỳ áp dụng hoặc trạng thái Verified; nhận từ rule/snapshot của service, không hardcode phép tính trong Razor/JS.
- Giữ hợp đồng ForecastAmountBinder, ModelState và metadata; format hiển thị không được đổi nghĩa chuỗi hoặc reset ngày nhập.
- Chỉ hỗ trợ nhánh nghiệp vụ backend đã có contract/kiểm chứng; optional graph/catalog cần mở rộng riêng, không giả lập dữ liệu để UI trông hoạt động.
- POST sai giữ input và mở phần có lỗi; Enter không xóa; lưu/tính lại đúng transaction/service và snapshot bất biến hiện có.
- Không dùng ID route làm phân quyền, không bỏ guard synthetic để chạy demo; không đụng migration cũ, DB thật hay khóa thật trong tác vụ UI.
- Sau code, tự nhìn ảnh ba kích thước theo mục 10.9, rà chi tiết thừa, kiểm token và các trạng thái theo mục 15.4, ghi bằng chứng; nếu chưa chạy được browser thì báo chưa kiểm. Không lấy điểm tự chấm hoặc số lỗi scanner làm bằng chứng UI đạt.
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

## 15. Nghiên cứu anti AI slop và tiêu chí áp dụng

Ngày tra cứu: 28/11/2026. “AI slop” ở đây là giao diện lặp khuôn, trang trí thiếu mục đích hoặc trông hoàn chỉnh nhưng không giúp hoàn thành tác vụ. Đây không phải tiêu chuẩn có thể xác định chỉ bằng font, màu hay một điểm số. Các quy tắc dưới là tổng hợp và quyết định áp dụng riêng cho TaxManager.

### 15.1. Nguồn đã đọc, gồm repo GitHub gốc

| Mã / nguồn | Tiếp thu | Giới hạn khi áp dụng |
|---|---|---|
| S1. [Anthropic frontend-design](https://github.com/anthropics/skills/blob/main/skills/frontend-design/SKILL.md) | Xuất phát từ đối tượng, công việc và nội dung; cấu trúc phải mang nghĩa; thiết kế rồi tự phê bình. | Tránh mặc định thẩm mỹ không đồng nghĩa luôn cần font lạ hoặc bố cục phá cách. |
| S2. [pbakaus/impeccable](https://github.com/pbakaus/impeccable) | Rà card lồng nhau, icon tile lặp, thứ bậc yếu; tách critique, audit, harden và polish. | Detector chỉ gợi ý lỗi; cần kiểm màn hình và thao tác thực. |
| S3. [Studio-Groei/anti-slop](https://github.com/Studio-Groei/anti-slop) | Ghi thẩm mỹ, tham chiếu, ý định, ràng buộc; so phương án có khác biệt cấu trúc; review cả câu chữ. | Không bê quy trình nhiều agent/5 biến thể hay công cụ tuning vào mọi tác vụ. |
| S4. [wwewtech/anti-slop-design](https://github.com/wwewtech/anti-slop-design) | Một trọng tâm, tránh lặp KPI/bộ lọc, bỏ số liệu khoe thành tích không có nguồn; xét đủ trạng thái tương tác. | Các ngưỡng “85%”, điểm 4/5 và hiệu ứng shake/pulse/scale là quy ước của tác giả, không phải bằng chứng nghiên cứu cho TaxManager. |
| S5. [Leonxlnx/taste-skill](https://github.com/Leonxlnx/taste-skill) | Câu chữ trực tiếp, hệ thiết kế nhất quán, dùng nội dung sản phẩm và kiểm browser thật. | Repo thiên website công ty; ảnh lớn, hero, shader và scroll scene không phục vụ màn nhập thuế. |
| S6. [Dekuran/anti-slop-design](https://github.com/Dekuran/anti-slop-design/blob/main/README.md) | Lưu ý một danh sách “phong cách khác mặc định” vẫn có thể tạo ra mẫu rập khuôn mới. | Thử nghiệm do tác giả tự báo cáo; không sinh màu/font bằng hash tên dự án, ép texture hoặc cấm dải màu cho ứng dụng đang có hệ thiết kế. |
| S7. [Vercel Web Interface Guidelines](https://vercel.com/design/guidelines) | Focus rõ, paste được, liên kết đúng nghĩa, giữ ngữ cảnh điều hướng, phản hồi khi xử lý, chuyển động có lý do. | Không áp dụng máy móc ví dụ React/Next.js, optimistic success hay gợi ý cấu hình chặn zoom vào Razor Pages. |
| S8. [GOV.UK: validation](https://design-system.service.gov.uk/patterns/validation/) | Giữ input khi lỗi, summary đưa tới ô, thông báo chỉ cách sửa; tránh báo lỗi khi người dùng chưa nhập xong. | Không tự nới grammar tiền vi-VN hoặc bỏ kiểm tra server. |
| S9. W3C: [target size](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html), [reflow](https://www.w3.org/WAI/WCAG22/Understanding/reflow.html), [focus not obscured](https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum.html) | Đối chiếu tiêu chí truy cập, thay cho chấm điểm cảm tính về vẻ ngoài. | AA 2.5.8 là 24 CSS px với ngoại lệ; 44px là mục tiêu nội bộ cho điều khiển mobile thường dùng. Bảng dữ liệu có ngoại lệ reflow; trang chứa bảng vẫn phải reflow. |
| S10. [heydamir/unslop](https://github.com/heydamir/unslop) | Audit rồi sửa theo lớp: spacing, color, icons, copy, type, layout, details, visuals, motion; màn đã tốt chỉ sửa phần cần thiết. | Giữ contract và brand; không ép thêm hình/điểm nhấn hoặc làm video trước/sau. |
| S11. [ibelick/ui-skills](https://github.com/ibelick/ui-skills) | Bộ hướng dẫn design engineering và playbook có ví dụ tương tác cho từng lỗi nhỏ. | Lấy quy tắc phù hợp; scale nút, shadow, skeleton và truncate không tự trở thành yêu cầu của sản phẩm. |
| S12. [emilkowalski/skills](https://github.com/emilkowalski/skills) | Quyết định motion có lý do; thử phá UI bằng dữ liệu khó; ghi lại lý do của chỉnh sửa thiết kế để tránh lặp lỗi. | Không thêm animation/library vì repo có skill tương ứng. |
| S13. [yetone/kill-ai-slop](https://github.com/yetone/kill-ai-slop) | Catalogue trước/sau cho cả hình thức và câu chữ; lưu ý kiểu terminal hoặc editorial cũng có thể thành khuôn mới. | Nguồn chuyên biệt nhỏ hơn nhóm repo đầu bảng 15.5; không lấy phong cách paper/ink của tác giả làm theme ứng dụng. |
| S14. [Nutlope/hallmark](https://github.com/nutlope/hallmark) | Floor/Reflex/Finish, app-screen scope và giữ nội dung/wiring khi redesign. Phân tích riêng tại mục 15.8. | Không áp giới hạn chữ/hero của marketing lên bảng/form; không dùng clip để giấu overflow. |
| S15. [nextlevelbuilder/ui-ux-pro-max-skill](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill) | Tra cứu theo vấn đề, ưu tiên truy cập/tương tác; đối chiếu các sửa sai hướng dẫn mới. | Kết quả tìm kiếm không thay brief hoặc đo thực tế; không tái sinh design system đang dùng. |
| S16. [vercel-labs/agent-skills](https://github.com/vercel-labs/agent-skills) | Nguồn gốc workflow web-design-guidelines; dùng tiêu chí web ở S7 để review có vị trí và bằng chứng. | Sao của toàn bộ collection; chỉ lấy phần framework-agnostic cho Razor Pages. |

Các link GitHub trỏ nhánh đang phát triển, nội dung có thể đổi; ngày trên ghi thời điểm tra cứu, không khẳng định phiên bản phát hành. Dùng như tài liệu tham khảo, không tự cài skill, dependency, extension hay thực thi script từ repo. Khi đưa một công cụ thành cổng tự động, chốt phiên bản và kiểm false positive trước.

### 15.2. Quy tắc thiết kế có thể kiểm tra

Bảng này bổ sung mục 4–10; mã nguồn là căn cứ tham khảo, cột áp dụng là quyết định của dự án.

| Quy tắc | Áp dụng và cách kiểm tại TaxManager | Căn cứ |
|---|---|---|
| AS01. Công việc quyết định bố cục | Mở editor phải tìm được kỳ, trạng thái lưu, nơi nhập và hành động tiếp theo; không cần đi qua hero hoặc lời giới thiệu. | S1, S5 |
| AS02. Nhóm theo nghĩa | Dùng tiêu đề/khoảng cách cho nhóm nhập; bảng cho đối chiếu số. Khung chỉ khi phân biệt một vùng chức năng; không bọc từng nhãn, căn cứ hoặc hàng bằng card. | S1, S2 |
| AS03. Một trọng tâm, đủ thao tác | Không lặp cùng KPI hay bộ chọn kỳ ở nhiều widget cùng phạm vi. Bộ chọn tháng của editor và bộ chọn kỳ đối chiếu khác việc vẫn được giữ, phải ghi rõ phạm vi. | S4 |
| AS04. Mọi số có ngữ cảnh | Tiền nổi bật phải có đơn vị, kỳ, trạng thái và đường xem cách tính. Không thêm tỷ lệ tăng trưởng, “tiết kiệm thuế”, số người dùng hay badge tin cậy nếu thiếu dữ liệu chứng minh. | S4; mục 6–7 |
| AS05. Nhịp đọc thay cho trang trí | Duy trì căn trái nhãn, căn phải tiền, tabular-nums, khoảng cách theo nhóm. Dùng độ đậm/cỡ chữ có vai trò; không tạo hierarchy bằng nhãn IN HOA, icon tile hoặc một từ tiêu đề đổi màu. | S1, S2 |
| AS06. Câu chữ chỉ việc xảy ra | Dùng động từ thống nhất ở mục 8. Xóa lời quảng cáo như “tối ưu tài chính thông minh”; thay “có lỗi xảy ra” bằng chỉ tiêu lỗi và cách xử lý. | S3, S5, S8 |
| AS07. Bấm được phải có kết quả | Link đi tới tài nguyên thật; nút thực hiện hành động đúng tên. Chưa có tính năng thì ghi chưa hỗ trợ trong phạm vi liên quan, không dựng nút giả, bộ lọc vô tác dụng hoặc link `#` lấp chỗ. | S7 |
| AS08. Phản hồi trung thực | Xét default, hover, focus, active, đang xử lý, thành công, lỗi, disabled khi có nghĩa. Hiện “đã lưu” sau server xác nhận; không trì hoãn kết quả hoặc thêm skeleton chỉ để có animation. | S4, S7; contract lưu |
| AS09. Form giữ công sức đã nhập | Paste, di chuyển caret, sửa giữa chuỗi và POST lỗi phải giữ giá trị. Không tự bật lỗi đỏ mỗi phím; format tiền không tự coi chuỗi sai là hợp lệ. | S8; mục 5 |
| AS10. Điều hướng giữ đúng ngữ cảnh | Kiểm Back/Forward, refresh và link tháng/lần tính: giữ hồ sơ, kỳ, revision phù hợp. Đổi fragment trong editor không gửi form hoặc làm mất thay đổi chưa lưu. | S7; mục 3, 12 |
| AS11. Motion giải thích thay đổi | Không count-up tiền, stagger hàng, shimmer, shake lỗi hoặc pulse thành công. Mở/đóng có thể phản hồi ngắn theo token 150ms; 0ms hợp lệ, reduced-motion không mất thông tin. | S1, S7; mục 10 |
| AS12. Cùng hệ thống ở mọi trạng thái | Số dài, tên dài, lỗi nhiều dòng và bảng trên mobile phải giữ hierarchy; không giảm font, cắt tiền hoặc giấu trạng thái để làm ảnh gọn. | S2, S9; mục 7, 9 |

### 15.3. Điều chỉnh các rào chắn cũ

- Giữ Inter tự host vì có tiếng Việt và phục vụ đọc số. Không thay font chỉ vì một repo coi Inter là dấu hiệu slop; kiểm file tải thật, dấu và tabular-nums theo mục 10.
- Giữ palette xanh, trạng thái bằng chữ và viền chọn có nghĩa. Không dùng quy tắc “luôn tint màu đen”, sinh palette ngẫu nhiên hoặc thay toàn bộ theme để vượt scanner.
- Card, viền, disclosure và số tổng không tự là lỗi. Kiểm vai trò của từng thành phần; tiếp tục giữ phản hồi trực tiếp về giảm đường phân chia và không thu gọn phần nhập chính tại mục 10.2.
- Thay yêu cầu xóa ít nhất một chi tiết bằng rà soát có lý do ở mục 10.9. Không xóa hướng dẫn, nhãn nguồn, đơn vị, focus hoặc cảnh báo để trông tối giản.
- Một hành động chính là ưu tiên thị giác theo trạng thái, không phải giấu mọi thao tác khác. Preview tiếp tục đứng trước nút xóa trong thứ tự submit dù Lưu nổi hơn; thử Enter như quy tắc repo.
- Nét riêng đến từ bảng 12 tháng, giai đoạn/ngoại lệ, nguồn số và cách tính kiểm tra được; không bắt buộc thêm một “signature effect”.

### 15.4. Cổng review trước khi kết thúc thay đổi UI

Quy trình dùng cho từng màn hình, nối với mục 10.9 và thử người dùng ở mục 13:

1. Ghi tác vụ, dữ liệu thật từ contract và ràng buộc. Nếu cần tham chiếu, ghi URL, phần học được và phần không phù hợp; screenshot đẹp không chứng minh UX tốt.
2. Khi bố cục chưa rõ, so hai wireframe khác cấu trúc trên cùng dữ liệu/token; chọn theo khả năng tìm thông tin và hoàn thành việc. Sửa nhỏ dùng hệ hiện tại, không bắt buộc sinh biến thể.
3. Kiểm ma trận dưới, sửa lỗi chức năng/truy cập trước rồi mới polish. Xem ảnh render thật ở 375/768/1280px theo mục 10.9; thử riêng 320 CSS px hoặc zoom native 400% từ viewport 1280px cho reflow.
4. Lưu kết quả theo từng tiêu chí: đạt/chưa đạt/chưa kiểm, route, trạng thái dữ liệu, viewport, thao tác, ảnh hoặc kết quả test. Ngoại lệ theo bảng mục 10; không đổi checkbox thành đạt bằng nhận xét “đẹp hơn”.

| Trạng thái/bài thử | Kết quả cần quan sát |
|---|---|
| Trống, thiếu, 0, dự kiến, giả định | Không tự thêm 0, số demo hoặc KPI để lấp màn hình; chỉ dẫn bước tiếp theo và nhãn đúng từng giá trị. |
| Đủ dữ liệu; số/tên dài | Số và đơn vị đầy đủ, nguồn/cách tính tìm được; không tràn toàn trang hoặc ép cỡ chữ nhỏ. |
| POST lỗi và nhiều cảnh báo | Giữ chuỗi nhập; summary, focus và link mở đúng ô bị lỗi; thông tin quan trọng không chỉ nằm trong tooltip. |
| Chưa lưu, đang gửi, đã lưu, xung đột | Người dùng phân biệt được bản đang sửa và bản đã lưu; lỗi không báo thành công, không reset form. Kiểm phản hồi khi request thực sự còn chờ. |
| Bàn phím, Enter, menu, sticky | Đi được cả luồng; Enter xem trước an toàn; focus không bị che. W3C AA chỉ yêu cầu không che toàn bộ, dự án giữ mục tiêu thấy trọn điều khiển. |
| Không JS, Back/Forward, mở lại link | Luồng nền vẫn dùng được; định dạng có marker đúng contract, tháng/revision không tự đổi, không mất form vì đổi section. |
| Mobile và reflow | Nội dung thường đọc bằng cuộn dọc; bảng được cuộn riêng có tên và thao tác bàn phím. Không dùng `overflow: hidden` để che lỗi. |

Điều kiện chặn hoàn tất: đổi nghĩa tiền, mất input, ghi đè ngoài ý định, nhầm dự báo/thực tế, báo lưu giả, thao tác giả hoặc luồng chính không dùng được bằng phím. Bằng chứng accessibility và cổng kỹ thuật vẫn bắt buộc theo repo; điểm thẩm mỹ không bù được lỗi này. Trường hợp không áp dụng phải nêu lý do; chưa kiểm thì giữ mở, không tính là đạt.

Lượt nghiên cứu này chỉ cập nhật kế hoạch và tiêu chí review. Chưa chạy lại QA giao diện, chưa xác nhận các màn hiện có đạt AS01–AS12 và chưa đánh dấu hoàn tất thêm đợt UI nào.

### 15.5. Độ phổ biến và tính cập nhật đã xác minh

Snapshot kiểm tra ngày **28/11/2026**. Số sao đọc từ dữ liệu trang GitHub; SHA là HEAD nhánh mặc định lúc đọc; ngày commit dưới đây quy về UTC. GitHub API công khai bị rate limit trong lượt này, nên dùng trang repo và trang commit gốc để đối chiếu, không chép số từ bài tổng hợp. Không dùng ngày crawler để làm ngày tác giả cập nhật.

| Repo | Sao tại lúc kiểm tra | Commit gần nhất (UTC) / bằng chứng | Đọc mức cập nhật đúng phạm vi |
|---|---:|---|---|
| [anthropics/skills](https://github.com/anthropics/skills) | 201,665 | 2026-11-16 · [e4bb5d9](https://github.com/anthropics/skills/commit/e4bb5d960d87126ff3b3a8af1a104b7db67560ba) | Collection chung; HEAD sửa docx/pptx/xlsx, không chứng minh frontend-design vừa đổi. |
| [nextlevelbuilder/ui-ux-pro-max-skill](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill) | 155,328 | 2026-11-27 · [11ca2b3](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill/commit/11ca2b3bdd555649f278ee348c32a9302b24dcf1) | HEAD sửa nhận định WCAG large-text/contrast trong brand skills; có liên quan trực tiếp chất lượng guide. |
| [Leonxlnx/taste-skill](https://github.com/Leonxlnx/taste-skill) | 122,444 | 2026-11-26 · [952435f](https://github.com/Leonxlnx/taste-skill/commit/952435f63acece7d2fcb4b4d3c1786c1c43603e0) | HEAD về sponsor README; hoạt động repo không đồng nghĩa luật thiết kế mới. |
| [pbakaus/impeccable](https://github.com/pbakaus/impeccable) | 118,220 | 2026-11-26 · [2fc5e14](https://github.com/pbakaus/impeccable/commit/2fc5e1459efbbf06d92c7306517c286bf12490cf) | HEAD là hotfix CLI 4.11.1; không suy ngày cập nhật của từng quy tắc. |
| [nutlope/hallmark](https://github.com/nutlope/hallmark) | 62,230 | 2026-11-11 · [04977e1](https://github.com/nutlope/hallmark/commit/04977e15f3dfd6ee4b74c887dac3eb52665d6c92) | HEAD sửa CI; đọc app-screen scope tại SHA này, không suy có bộ quy tắc mới ngày 11/11. |
| [emilkowalski/skills](https://github.com/emilkowalski/skills) | 60,692 | 2026-11-15 · [64eb3de](https://github.com/emilkowalski/skills/commit/64eb3deb9a5fbfa041faf783e0bc3fd635872e9c) | HEAD thêm ask-emil để chọn skill; dùng riêng design-eng, break-ui và remove-animations khi cần. |
| [vercel-labs/agent-skills](https://github.com/vercel-labs/agent-skills) | 35,058 | 2026-11-20 · [5a37a63](https://github.com/vercel-labs/agent-skills/commit/5a37a6329fbad3666c3207392a6a7c0110e92efb) | HEAD merge sửa web-design-guidelines; xem nội dung guide liên quan trước khi áp dụng. |
| [heydamir/unslop](https://github.com/heydamir/unslop) | 22,778 | 2026-11-15 · [620163e](https://github.com/heydamir/unslop/commit/620163eedb3529d9cd23a3994ed8d287d06f760d) | HEAD sửa test trên macOS; workflow chín pass vẫn phải đọc từ skill đã chốt. |
| [ibelick/ui-skills](https://github.com/ibelick/ui-skills) | 11,481 | 2026-11-27 · [49289ef](https://github.com/ibelick/ui-skills/commit/49289efa4ce27c3c174f9c90731c6ce4013830eb) | HEAD merge filming-software; không suy tất cả UI skills đều cập nhật ngày đó. |
| [yetone/kill-ai-slop](https://github.com/yetone/kill-ai-slop) | 1,484 | 2026-09-15 · [f6e2ae3](https://github.com/yetone/kill-ai-slop/commit/f6e2ae32b30443ec7bd0da4da971ee18d8f8ffcb) | HEAD sửa ví dụ catalogue; chủ yếu làm nguồn so sánh trước/sau. |

Sao của `anthropics/skills` và `vercel-labs/agent-skills` là toàn bộ collection, không phải riêng skill thiết kế. Không thấy banner archived ở các trang trên lúc kiểm tra; đây không phải bảo đảm bảo trì tương lai. Nguồn S3/S4/S6 là góc nhìn bổ sung, không dùng chúng đại diện nhóm repo phổ biến. Ngày HEAD mới chỉ chứng minh hoạt động repo; muốn nói một hướng dẫn vừa đổi phải mở diff đúng tệp.

### 15.6. Guide và tips từ tác giả gốc

| Nguồn đọc thêm | Nội dung đáng dùng | Tính cập nhật / cách dùng |
|---|---|---|
| [UI Skills Playbook](https://www.ui-skills.com/playbook) | Ví dụ tương tác về khoảng cách nhóm, căn số, focus, chữ dài và trạng thái. | Kiểm lại bản đang phục vụ ngày 28/11; trang không nêu ngày xuất bản, không gọi đây là bài mới. |
| [Emil: Agents with Taste](https://emilkowal.ski/ui/agents-with-taste) | Viết lựa chọn thiết kế thành quy tắc kèm lý do, để agent sửa đúng triệu chứng. | Bài nền tảng được đọc lại ngày 28/11; không suy ngày xuất bản từ ngày truy cập. |
| [Emil: 7 Practical Animation Tips](https://emilkowal.ski/ui/7-practical-animation-tips) | Phản hồi thao tác, easing, tâm mở popover, tránh trì hoãn tooltip kế tiếp. | Chỉ dùng phần có ích cho tác vụ; giảm chuyển động vẫn là lựa chọn hợp lệ. |
| [Kill AI Slop catalogue](https://killaislop.com/) | So trực tiếp bản lỗi và bản sửa; kiểm cả các phong cách “anti-slop” đã thành công thức. | README và catalogue có thể lệch số mục; đọc ví dụ thực, không lấy con số marketing làm cổng QA. |
| [Anthropic: Harness design for long-running application development](https://www.anthropic.com/research/harness-design-long-running-apps) | Tách đánh giá thiết kế, tính riêng, độ hoàn thiện và chức năng; dùng thao tác browser làm bằng chứng. | Xuất bản 24/03/2026; vẫn có ích về quy trình nhưng không gọi là bản hướng dẫn mới tháng 11. Không sao chép trọng số ưu tiên thẩm mỹ hơn chức năng vào công cụ thuế. |

### 15.7. Mẹo bổ sung đưa vào review

Đây là lựa chọn áp dụng riêng, cần kiểm trên UI; không cộng thêm hiệu ứng chỉ để làm đủ checklist.

| Mẹo | Thực hiện / giới hạn | Nguồn |
|---|---|---|
| Sửa từng lớp trên cùng một màn | Với lượt polish lớn, rà spacing → color → icons → copy → type → layout → details → visuals → motion; bỏ qua lớp không có lỗi. Lỗi chức năng/truy cập vẫn sửa trước theo 15.4. | S10 |
| Khoảng cách thể hiện quan hệ | Tiêu đề gần nhóm của nó hơn nhóm trước; khoảng giữa hai nhóm lớn hơn giữa hai trường cùng nhóm. Chọn trong thang token 4/8/16/24/32, không sinh giá trị rời. | S10, UI Skills Playbook |
| Kiểm wrap bằng tiếng Việt thật | Thử tiêu đề, nút và nhãn trạng thái dài; dùng wrap thích hợp thay chèn `<br>` để cứu một viewport. `text-wrap: balance/pretty` chỉ tăng cường, fallback phải đọc được; không truncate tiền hoặc căn cứ. | UI Skills Playbook; mục 7, 9 |
| Căn icon bằng mắt sau khi căn hình học | So icon với baseline/x-height và nét chữ tại cỡ thực; nếu cần chỉnh quang học, dùng cùng quy tắc trong partial/token, không vá từng nút. | UI Skills Playbook |
| Giữ bố cục khi đổi trạng thái | Thử nhãn đang lưu/lỗi dài hơn nhãn thường; vùng phản hồi không đẩy nút hoặc ô đang thao tác sang chỗ khác. Chỉ thiết kế loading cho request đang chờ thực sự. | S11, S12; AS08 |
| Phản hồi hover đúng thiết bị | Chỉ dùng hover tinh chỉnh với pointer phù hợp; trên touch phải thấy trạng thái chọn qua chữ/aria-current, không mắc kẹt ở hover. Focus bàn phím luôn rõ, không chạy animation. | S12; mục 9 |
| Thử phá màn hình có chủ đích | Dùng tên một ký tự và rất dài, khoản tiền lớn, nhiều cảnh báo, danh sách trống, phím Enter lặp và cửa sổ thấp; ghi lỗi cụ thể thay vì chỉ chụp màn đủ dữ liệu. | S12; ma trận 15.4 |
| Lưu lý do của chỉnh sửa lặp lại | Khi người dùng sửa cùng một kiểu trình bày, cập nhật quy tắc tương ứng trong plan kèm lý do/bằng chứng; không để agent sau quay về kiểu đã bỏ. Tri thức Bint vẫn theo quy trình chờ duyệt của repo. | S12, Emil: Agents with Taste |

### 15.8. Hallmark: áp dụng phần nào cho TaxManager

Đã đối chiếu skill cục bộ `C:/Users/LENOVO/.agents/skills/hallmark/SKILL.md` (version 2.0.0), [App screens](https://github.com/nutlope/hallmark/blob/04977e15f3dfd6ee4b74c887dac3eb52665d6c92/skills/hallmark/references/flow.md#app-screens) và [gates](https://github.com/nutlope/hallmark/blob/04977e15f3dfd6ee4b74c887dac3eb52665d6c92/skills/hallmark/references/gates.md), cùng [bản nguồn tại SHA 04977e1](https://github.com/nutlope/hallmark/tree/04977e15f3dfd6ee4b74c887dac3eb52665d6c92/skills/hallmark). README hiện mô tả 50 theme; không dùng số theme/gate từ các bài tổng hợp cũ để nhận diện phiên bản.

- **Phân tầng kết quả:** học cách tách Floor (phải sửa), Reflex (mặc định cần xem lý do) và Finish (hoàn thiện). Lỗi đúng/sai, truy cập và trung thực dữ liệu ở 15.4 là điều kiện chặn của sản phẩm; ngoại lệ thẩm mỹ không miễn các điều kiện này.
- **Chọn đúng scope:** nhánh App screens bỏ hero, ngân sách chữ marketing, hình thay thế và reveal khi tải; cho phép một font sans quen thuộc. Vì vậy bảng/form thuế không bị ép xuống khoảng 80 từ hoặc phải đổi Inter.
- **Giữ wiring:** đổi trình bày phải giữ `name`, `id`, route, handler, form action và hook kiểm thử; trạng thái chưa có logic phải ghi là thiếu, không vẽ như đã hoạt động.
- **Giữ hệ đã chốt:** đọc token và brand của dự án trước; không bốc theme cho mỗi màn. Ví dụ/cảnh báo của Hallmark là dữ liệu đầu vào để review, không thay phản hồi trực tiếp của người dùng.
- **Kiểm overflow thật:** yêu cầu clip ở tool không phải lý do giấu tiền, focus hoặc lỗi responsive. Bảng cuộn riêng và reflow của dự án vẫn là chuẩn đối chiếu.
- **Tách nghiên cứu và chạy audit:** lượt này đọc hướng dẫn, chưa chạy `sloplint`, render hay review Hallmark trên app; không ghi số gate đạt. Một lượt audit sau phải chỉ rõ route, scope và bằng chứng trước/sau.

### 15.9. Cách giữ tài liệu nghiên cứu còn cập nhật

- Trước khi dùng một nguồn để sửa UI, kiểm lại tệp/commit liên quan nếu snapshot quá 30 ngày hoặc repo đổi major version; 30 ngày là nhịp kiểm nội bộ, không phải chuẩn của ngành.
- Khi refresh, ghi ngày kiểm, SHA hoặc version, thay đổi có tác động và quyết định giữ/đổi. Không thay quy tắc chỉ vì số sao tăng, tên skill đổi hoặc HEAD vừa thêm sponsor.
- Ưu tiên diff sửa tính đúng, truy cập và hành vi. Ví dụ mới: UI UX Pro Max ngày 27/11 đã sửa nhận định WCAG large-text/contrast trong brand skills; tiếp tục đo theo W3C và giữ ngưỡng chữ 4,5:1 hiện có của dự án.
- Hướng dẫn nền tảng cũ vẫn dùng được nếu điều kiện còn đúng; ghi rõ cũ/mới/chưa rõ ngày xuất bản. Không gọi “mới nhất” khi chỉ thấy ngày search index.
- Bảng sao/commit là snapshot của lượt này, không tự cập nhật. Khi chưa đọc được nguồn hoặc diff, ghi chưa xác minh; không điền ngày/số từ suy đoán.

### 15.10. Màn xác nhận xóa hồ sơ

- Một panel có chiều rộng giới hạn: năm và phiên bản thực đã lưu, thông tin nhận diện hữu ích, hậu quả rồi hành động. UUID chỉ dùng trong giao thức, không trình bày cho người dùng.
- Typography trong panel chỉ có hai cấp: tiêu đề 20 px/600, toàn bộ nội dung 16 px/400 cùng màu chữ chính, gồm phiên bản, nhãn, số liệu và giải thích. Không xen nhãn 14 px mờ, số liệu đậm hoặc một cụm bôi đậm giữa đoạn; thứ bậc dùng vị trí và khoảng cách. Nút giữ weight 500 theo hệ điều khiển chung. Quy tắc này theo phản hồi trực tiếp ngày 28/11 về chữ lúc to lúc nhỏ, lúc mờ lúc rõ.
- “Giữ hồ sơ” là hành động phụ; nút xác nhận xóa là primary đỏ duy nhất, có chữ và icon. Mobile xếp dọc, vùng chạm tối thiểu 44 px.
- Nói rõ dữ liệu nào còn giữ bằng từ ngữ thông thường; không đưa tên bảng, audit, WAL hoặc giải thích lưu trữ kỹ thuật lên UI. Không mô tả ordinary delete như xóa sạch thiết bị.
- GET thiếu phiên bản có thể đọc phiên bản hiện tại; POST phiên bản cũ phải dừng và mời mở lại. Không tự cập nhật phiên bản POST để xóa dữ liệu vừa thay đổi.
- Kiểm chữ Việt thật ở 320/375 px, tránh một từ rơi riêng cuối đoạn khi trình duyệt hỗ trợ `text-wrap: pretty`. Bằng chứng: [review màn xóa](docs/reviews/2026-11-28-forecast-delete.md).

### 15.11. Trình bày căn cứ tính tháng

- Danh sách nhóm trong một panel, mỗi nhóm: tên ngắn, trạng thái chữ Việt kèm icon/màu, rồi văn bản–điều–khoản–điểm. Không đổi trạng thái xác minh thực tế; không chỉ dùng màu.
- Không nối các bậc thuế vào tiêu đề; dùng bảng bậc đã có và nhãn căn cứ “Biểu thuế tháng”. Gom theo toàn bộ căn cứ, không gom chỉ theo URL làm mất khoản khác nhau.
- Mô tả gốc, mã và phiên bản/kỳ đặt trong một mục tra cứu chung cho mỗi tháng, không thêm dropdown ở từng nhóm. Liên kết nguồn an toàn xuất hiện một lần mỗi URL trong cùng tháng.
- Không lặp câu “chưa có” cho từng trường thiếu. Giữ thông báo thiếu ngắn, metadata có thật vẫn truy cập được; mọi nội dung nguồn phải được encode.
- Theo phản hồi “wall of text” ngày 28/11: dùng cỡ chữ nhất quán và khoảng cách, hạn chế viền và đoạn đậm dài. Bằng chứng: [review căn cứ tháng](docs/reviews/2026-11-28-monthly-bases.md).

### 15.12. Mật khẩu trực tiếp và lớp menu mobile

- Đăng ký: mật khẩu 8–128 ký tự, không chỉ gồm khoảng trắng, không thêm điều kiện chữ hoa/số/ký tự đặc biệt. Điều kiện và khớp nhập lại cập nhật ngay bằng chữ + icon + màu, aria-live chỉ đổi khi thông điệp đổi, không đọc hoặc lưu giá trị mật khẩu. Đổi mật khẩu chính phải tính lại khớp; cho paste, autofill và password manager. Server vẫn kiểm độc lập; không vô hiệu nút gửi để che nguyên nhân lỗi.
- Không áp điều kiện mật khẩu mới vào đăng nhập/xác nhận mật khẩu của tài khoản cũ, hoặc đổi chính sách mật khẩu tệp sao lưu theo điều kiện tài khoản.
- Menu phải cao hơn mọi thanh hành động hoặc header bảng trong nội dung. `.workspace` tạo stacking context ở layer base; sidebar ở layer intersection. Không chỉ so hai con số z-index khi chúng nằm trong những stacking context khác nhau. Kiểm hit-test thật tại vị trí nút tính bị che bởi menu, cả lúc đã cuộn.
- Nút menu dùng icon mở/đóng, tên truy cập rõ, vùng chạm 44 px; không để “Đóng menu” xuống dòng ép logo trên mobile. Bằng chứng: [review mật khẩu/menu](docs/reviews/2026-11-28-password-menu.md).

### Tài khoản: nhóm thao tác và giới hạn chiều rộng

- Trang tài khoản/xuất dữ liệu tối đa 46rem, form đăng nhập/đăng ký giữ khổ hẹp. Header và nội dung cùng trục; không kéo rộng toàn màn hình.
- Phân nhóm thông tin đăng nhập, dữ liệu và phiên đăng nhập bằng khoảng cách. Dùng hàng icon + tiêu đề/mô tả + nút phụ, không thẻ lồng thẻ, không thêm đường phân chia. Mỗi màn có đúng một nút đặc.
- Trạng thái xác nhận có chữ, icon và ngày thật; không biến thông tin trạng thái thành nút. Không sửa lời xác nhận có phiên bản trong lượt thiết kế.
- Xuất JSON và sao lưu để khôi phục phải phân biệt rõ công dụng và phạm vi. Nút tải là primary; quay lại/sao lưu là phụ. Trên mobile giữ thứ tự DOM và thứ tự nhìn thấy thống nhất.
- Bằng chứng và ảnh: [review tài khoản](docs/reviews/2026-11-28-account-ui.md).
- Lối vào tài khoản trên topbar là nút phụ có icon, nhãn và tên đang đăng nhập; mở thẳng trang tài khoản, không dropdown. Giữ một primary cho nhiệm vụ của màn chính. Tên dài được ellipsis với tên đầy đủ vẫn truy cập được; Demo local là huy hiệu riêng.
