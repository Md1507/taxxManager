# TaxManager — rà soát và kế hoạch redesign UI/UX

Ngày: 29/11/2026. Trạng thái: **đề xuất để duyệt, chưa triển khai UI**.

Đề xuất chọn hướng **không gian làm việc với bảng thuế 12 tháng**: bảng tháng để rà soát, vùng sửa tháng ở gần bảng, kết quả năm gắn với độ đầy đủ của dữ liệu. Theme được tinh chỉnh theo hướng **nền trung tính pha xanh, xanh rêu làm điểm nhấn**. Giữ palette xanh, Inter và cách trình bày số; thay đổi cách dùng màu, mật độ, thứ bậc chữ và phản hồi tương tác cùng với bố cục. Phương án theme bổ sung ở mục 6.1–6.6 cũng đang chờ duyệt.

## 1. Phạm vi và quyết định thiết kế

**Hallmark · v2.0.0**

| Mục | Quyết định đề xuất |
|---|---|
| Redesign | Full trong app shell hiện có; không tạo ứng dụng hoặc hệ điều hướng bằng route mới. |
| Before | Linter ghi 9 FAIL và 4 REFLEX; có cảnh báo trùng và nhận diện sai, được phân tích ở mục 9. Đây không phải 13 lỗi UX độc lập. |
| Keeps | Chức năng, dữ liệu, route, handler, field name/ID, script hooks, palette xanh, Inter, tiếng Việt và nội dung cảnh báo/đồng ý. |
| Files | Sửa theo từng đợt ở mục 7; không xóa tệp hoặc route. Lượt lập kế hoạch này chỉ ghi tài liệu và bằng chứng kiểm tra. |
| Theme | Project green tinh chỉnh: nền trung tính pha xanh, xanh rêu làm điểm nhấn; genre modern-minimal, scope app. Chi tiết token và component ở mục 6. |
| Screen | Hồ sơ và bảng 12 tháng là trung tâm; nhập liệu, đọc kết quả, so sánh và quản lý dữ liệu dùng cùng quy tắc. |
| Chrome | Giữ sidebar desktop và menu mobile; phân nhóm điều hướng toàn ứng dụng với ngữ cảnh hồ sơ. |
| Signature | Bảng dữ liệu 12 tháng với trạng thái từng chỉ tiêu và vùng sửa tháng rõ ngữ cảnh. |
| Material | Dùng màn hình thật và kho synthetic. Không thêm số liệu, biểu đồ, ảnh hoặc nhận xét người dùng không có trong sản phẩm. |
| Review | Rà trong phiên này theo hướng dẫn harness Codex của Hallmark; chưa có review nghiệm thu thiết kế mới. |

Người dùng cần nhập lương theo giai đoạn, sửa ngoại lệ tháng, phân biệt chưa nhập với 0, kiểm tra nguồn và hiểu mức tin cậy của kết quả. Desktop là nơi nhập/rà nhiều số; mobile phải sửa được một tháng và đọc đủ các giới hạn kết quả.

### So sánh hướng đi

| Hướng | Phù hợp đến đâu | Quyết định |
|---|---|---|
| Chỉ đổi màu, bo góc, khoảng cách | Không giải quyết được việc bảng tháng nằm sau hàng nghìn pixel form. | Dùng cho sửa nhỏ, không đủ cho lượt này. |
| Dashboard với các thẻ số và biểu đồ | Chưa có nhu cầu hoặc dữ liệu bổ sung để biện minh; phân tán việc nhập và kiểm tra số. | Không chọn. |
| Bố cục theo hồ sơ, bảng tháng và kết quả có độ phủ | Phù hợp dữ liệu hiện có, giữ nghiệp vụ và giúp quay lại cập nhật hằng tháng. | **Chọn.** |

## 2. Cách rà và giới hạn bằng chứng

- Baseline ứng dụng được publish riêng từ HEAD `4dd95fe`, chạy bằng kho synthetic; ảnh chính ở 1440 × 900 và 390 × 844. Baseline Hallmark còn có 375 px và các khung desktop 1280/1440/1920.
- Đã tạo 15 ảnh baseline cho `/`, `/Forecast`, `/ho-so`; 18 ảnh bổ sung cho 9 route/trạng thái; 10 ảnh tài khoản từ HTML do host kiểm thử render. Không phải mọi ảnh được tạo đều đã được duyệt thị giác riêng.
- Đã trực tiếp xem các màn đại diện: hồ sơ, editor desktop/mobile, công cụ tháng, chi tiết tháng mobile, báo cáo, kịch bản, đối chiếu, lịch sử rỗng, xóa hồ sơ, sao lưu/khôi phục mobile và 5 màn tài khoản. Các nhánh điều kiện còn lại được đọc code; chưa coi là nghiệm thu bằng trình duyệt.
- Ảnh tài khoản là bản render HTML với tài nguyên CSS/JS, không chứng minh luồng đăng nhập hoặc submit hoạt động trên trình duyệt. Trong lúc rà có thay đổi xóa tài khoản từ công việc khác; không sửa hoặc nghiệm thu các thay đổi đó. Nội dung quyền dữ liệu trong ảnh có thể chậm hơn mã đang sửa.
- Có 17 Razor page trong bản baseline. Workspace bổ sung `/tai-khoan/xoa` đang phát triển thành page thứ 18; chỉ đưa vào phạm vi tương thích sau khi thay đổi đó ổn định.
- `/khoi-phuc/xem-truoc` không có handle hợp lệ trả HTTP 410; công cụ bỏ qua ảnh. Đã đọc template cho cả trạng thái hợp lệ và hết hạn, chưa kiểm thị giác nhánh khôi phục hợp lệ.

Bằng chứng máy nằm trong `.hallmark/audit-2026-11-29.json`, `.hallmark/audit-2026-11-29/inventory.json`, ảnh trong `.hallmark/before/` và `.hallmark/audit-2026-11-29/screens/`. Đây là bản ghi cục bộ của lượt rà, không phải ảnh thiết kế mới.

## 3. Kiểm kê trước đánh giá

### Hệ thống dùng chung

| Nhóm | Kiểm kê có căn cứ | Nơi dùng |
|---|---|---|
| Màu | 12 giá trị màu; `--on-brand`, `--focus`, `--backdrop` là alias, không đếm thêm. | `wwwroot/css/tokens.css:2` |
| Font | 1 họ chủ động: Inter, có fallback system; 6 cỡ khai báo: 13/14/16/20/24/32 px. Ba token đậm 400/500/600; `strong` còn có thể dùng đậm mặc định của trình duyệt. | `tokens.css:18`; `site.css:4` |
| Chữ phụ | `--text-muted: #526057`, thường cỡ 14 px trên trắng hoặc nền `#F3F6F3`; trạng thái trong bảng preview hiện cỡ 13 px. | `site.css:18`, `.monthly-preview .badge` |
| Spacing | 8 token: 0/4/8/12/16/24/32/48 px; fieldset/panel thường padding 24 px, mobile nhiều vùng 16 px. Không phải chỉ có một spacing lặp khắp nơi. | `tokens.css:30`; `site.css:64` |
| Phân tách | Có khoảng cách, nền trắng, đường dưới hàng bảng, viền control, viền lỗi và badge. `fieldset` có nền nhưng border 0; viền trái nav mặc định trong suốt. | `site.css:28`, `:45`, `:64`, `:93`, `:102`, `:115` |
| Bóng và hiệu ứng | `--shadow: none`; có inset 1 px phản hồi hover nút chính, không phải bóng container. Có backdrop menu mobile. Không có gradient/glow hoặc animation trang trí trong CSS đã rà. | `tokens.css:44`; `site.css` |
| Container | App shell, sidebar, vùng main; các panel nghiệp vụ ở trong main. Trong hồ sơ mẫu đo được 4 vùng nền và tối đa 1 cấp nền bao nhau. Fieldset lồng về ngữ nghĩa không tự đồng nghĩa với panel lồng về thị giác. | `inventory.json`; `Forecast.cshtml:20` |
| Trang trí | Logo SVG, icon nét ở nav/hành động, icon trạng thái, dấu mở/đóng disclosure. Không thấy nhãn in hoa giãn chữ, gradient tiêu đề hoặc dãy 01/02/03 trang trí. | `_Layout.cshtml:23`; `_ForecastStatus.cshtml`; `site.css:82` |

### Palette và vai trò

| Token / màu | Vai trò hiện tại | Hướng xử lý |
|---|---|---|
| `--brand` `#395B40` | Nhận diện; link chữ; nút; lựa chọn tháng; focus qua alias; nền tổng nghĩa vụ năm — 6 nhóm vai trò theo cách đếm này (`site.css`, gồm `.month-option[aria-current]`). | Giữ 5 nhóm đầu; đề xuất thay nền toàn khối kết quả bằng thứ bậc chữ ở mục 6. |
| `--brand-tint` `#E7FFEC` | Nền active/hover. | Giữ; active còn có độ đậm/`aria-current`. |
| `--bg` `#F3F6F3`, `--surface` `#FFFFFF` | Nền ứng dụng và bề mặt làm việc. | Giữ. |
| `--text` `#1A1F1B`, `--text-muted` `#526057` | Chữ chính, phụ; `--text` còn làm backdrop qua alias. | Giữ, đo tương phản sau khi đổi bố cục. |
| `--border-strong` `#76867B`, `--divider` `#DDE5DE` | Biên control và phân hàng. | Giữ, không dùng divider nhạt làm biên input. |
| `--success` `#246647` | Thực tế/đủ dữ liệu/đã xác nhận. | Giữ chữ mô tả; không gộp các nghĩa này chỉ bằng một chấm xanh. |
| `--warning` `#895600` | Giả định, thiếu, cảnh báo. | Giữ nhãn cụ thể theo ngữ cảnh. |
| `--error` `#B02A37` | Lỗi, thao tác xóa. | Giữ thông báo và bước xác nhận. |
| `--info` `#365C85` | Dự kiến. | Giữ nhãn Dự kiến. |

### Số đếm của màn mẫu

Đo sau khi JS khởi tạo, tại đầu trang; đếm phần tử có hiển thị dù nằm dưới vùng nhìn. Link gạch chân tính trong `main`; nút gồm `button` và `a.button`; vùng nền chỉ gồm fieldset/panel/kết quả được nhận diện, không gồm từng ô bảng. Badge là số phần tử, không phải số trạng thái khác nhau. Đây không phải tổng cố định cho mọi hồ sơ.

| Màn / dữ liệu | Vùng nền / cấp lồng | Nút | Link gạch chân | Badge | Thanh thao tác desktop / phone |
|---|---:|---:|---:|---:|---:|
| Hồ sơ, 2 dòng dữ liệu | 0 / 0 | 1 | 2 | 0 | Không có |
| Forecast bản 1, chọn tháng 6 | 4 / 1 | 31 | 13 | 51 | 76 / 124 px |
| Công cụ tháng, đầu vào ban đầu | 1 / 1 | 14 | 2 | 0 | 76 / 68 px |

Trong preview Forecast đầy đủ: 12 hàng × 4 chỉ tiêu = 48 trạng thái dữ liệu; 12 nút Sửa và 12 link Chi tiết khi bản lưu được xác nhận (`_ForecastMonths.cshtml:10`). Hai thao tác này có công việc khác nhau, không xóa một thao tác chỉ vì xuất hiện cạnh nhau. Các nguồn đang đóng trong disclosure không tính là link đang hiển thị.

## 4. Báo cáo theo vai trò

“Cản trở” chỉ ảnh hưởng tìm, nhập, đọc hoặc hiểu dữ liệu. “Lựa chọn” là quyết định bố cục/thẩm mỹ cần thử; không gọi là lỗi chức năng.

| Vị trí | Quan sát, số đếm và bằng chứng | Vì sao cần sửa hoặc giữ | Sửa cụ thể / thiết bị thay thế | Mức độ |
|---|---|---|---|---|
| Điều hướng chung | 3 mục fragment của Forecast vẫn xuất hiện khi chưa mở hồ sơ; `_Layout.cshtml:28`. Hai nhãn dài xuống dòng ở 1280 px. | Cản trở nhận biết phạm vi: phần của hồ sơ đứng ngang hàng với công cụ toàn ứng dụng. Xuống dòng riêng nó không phải lỗi. | Giữ điểm đến và thứ tự tương đối; chia nhóm bằng khoảng cách 24 px và nhãn thường 14 px; nhóm hồ sơ chỉ hiện khi có ngữ cảnh editor/hồ sơ. Khi đang POST preview, fragment phải ở nguyên document. | Cao |
| Thứ tự editor | Ở 390 px: sửa tháng bắt đầu y=2.560, preview y=4.078, tổng quan y=6.098. Main dài 6.743 px, trên cùng hồ sơ mẫu; `inventory.json`. | Cản trở cập nhật tháng và rà cả năm; người dùng phải đi qua form thưởng trước khi thấy bảng. | Chuyển preview lên ngay sau cụm nhập lương gọn; đặt vùng sửa tháng sau bảng trong cùng nhóm `du-lieu`. Dùng grid field có liên quan, không giấu form bằng tab JS. | Cao |
| Thanh hành động | `site.css:69` dùng sticky bottom; ảnh editor phone cho thấy thanh cao 124 px, sát đáy viewport 844 px. | Cản trở đọc vùng nhập hiện tại, nhất là khi bàn phím mở. | Mặc định `position:static`; đặt sau nhóm nhập và người phụ thuộc, trước phần đọc kết quả. Không tạo bản sao nút nổi; giữ Save chính, Preview phụ và submit Preview đầu tiên. | Cao |
| Bảng tháng trên phone | 6 cột, `min-width:48rem`; Sửa/Chi tiết nằm ở cột cuối (`_ForecastMonths.cshtml:10`, `site.css:119`). | Cản trở tìm thao tác khi vùng nhìn chỉ 390 px. Cuộn ngang hợp lý cho số liệu, nhưng không nên là điều kiện để sửa tháng. | Giữ một bảng semantic; đưa ô thao tác cạnh nhãn tháng và ghim cụm nhận diện/thao tác trong vùng cuộn. Không che số dưới cột ghim; kiểm 320 px. Nếu không đủ, đưa bộ chọn tháng hiện có lên ngay trên bảng. | Cao |
| Cảnh báo nguồn của kết quả năm | Thông báo nguồn Unverified và giới hạn hỗ trợ nằm trong một `details` đóng (`_AnnualResult.cshtml:73`, `:82`). | Cản trở đánh giá kết quả; đây là thông tin quyết định, không phải metadata tùy chọn. | Đưa nguyên văn hai thông tin ra vùng kết quả luôn thấy. Giữ disclosure cho danh mục căn cứ, phiên bản và provenance. | Cao |
| Giai đoạn lương, thưởng, phụ thuộc | Trường lương chính đã có grid; các khoản thưởng và phụ thuộc vẫn xếp trường dọc (`Forecast.cshtml:41`, `:107`). Trong mẫu, vùng thưởng cao 1.437 px trên phone. | Cản trở rà khoảng tháng, số tiền, trạng thái như một đơn vị. Không kết luận mọi trường nguồn đều thừa. | Desktop dùng grid cột tháng/số tiền/trạng thái, nhãn phía trên; mobile 1 cột, cặp từ/đến chung hàng. Metadata vẫn chỉnh được; lỗi mở vùng liên quan và dẫn focus đúng. | Trung |
| Ghi chú nhập liệu | Một hướng dẫn tiền chung và một giải thích gross/net đã tồn tại. “Xem trước không lưu” còn có ở heading (`Forecast.cshtml:11`) và action bar (`:131`); gross trống dùng giai đoạn có ở `:52`, `:72`. | Chỉ các câu nhắc trùng làm tăng tải đọc. Quy ước tiền, trống khác 0, gross/net và ưu tiên ngoại lệ đều có ý nghĩa. | Giữ một vị trí cho mỗi quy tắc; nối input bằng `aria-describedby`; bỏ bản nhắc lặp, dùng nhãn trạng thái lưu sát tiêu đề thay panel ghi chú mới. | Trung |
| Danh sách hồ sơ | 2 dòng mẫu cùng tên “Hồ sơ năm 2026”, cột năm lặp 2026 (`ForecastProfiles.cshtml:19`). | Khó phân biệt hai hồ sơ; chỉ đổi spacing không giải quyết được hoàn toàn. | Tăng thứ bậc cho revision và tình trạng có sẵn, giữ link tên hồ sơ là điểm mở chính. Tên riêng hoặc ngày cập nhật cần tác vụ dữ liệu riêng nếu model chưa có; không tự tạo. | Trung |
| Trạng thái trong bảng | 48 nhãn riêng ở preview đầy đủ; CSS preview đã bỏ khung badge, dùng chữ 13 px và icon. | Giữ: từng chỉ tiêu có thể khác trạng thái. Đếm nhiều không chứng minh là dư thừa. | Dùng khoảng cách 4 px giữa số và trạng thái; đề xuất chữ 14 px nếu còn đủ rộng. Không đổi thành một trạng thái chung cho cả hàng. | Ổn, giữ |
| Link và nút | Nav, `.button`, `.text-action`, disclosure đã có kiểu riêng; còn link Chi tiết trong 12 hàng preview. | Cần thống nhất affordance; Sửa là submit, Chi tiết là điều hướng khác nên giữ cả hai. | Bỏ gạch chân ở link thuộc ô thao tác có bố cục rõ; thay bằng hit area 44 px, hover nền tint và focus 2 px. Link trong đoạn giải thích, nguồn và lỗi vẫn có gạch chân. | Trung |
| Tổng quan năm | 1 khối nền xanh đậm cho nghĩa vụ; 10 dòng tổng và các đoạn độ phủ phía sau (`_AnnualResult.cshtml:18`, `:34`). | Lựa chọn: khối màu lớn làm số nghĩa vụ tách khỏi điều kiện đọc số. Không phải lỗi tính thuế. | Đề xuất nền trắng, số 32 px/600, độ phủ và revision ngay bên dưới; thay nhấn bằng màu bằng cỡ chữ và khoảng cách 24/32 px. | Thấp |
| Kịch bản và đối chiếu | Có form, kết quả và thông báo không lưu/stale; ảnh kịch bản desktop chỉ thấy bắt đầu kết quả sau form. | Cần đọc số cơ sở, số thử và chênh lệch theo cùng một trục. Không thêm biểu đồ nếu bảng đã đủ. | Kết quả dùng hàng chỉ tiêu đối chiếu; giữ cơ sở/giả định/khác kỳ và trạng thái ngay cạnh số. Tối ưu khoảng cách form, không giấu các giới hạn sau disclosure. | Trung |
| Lịch sử và căn cứ | Template hiển thị GUID ở mọi dòng và ghép dài metadata; ảnh lịch sử mới kiểm nhánh rỗng (`ForecastAnnualHistory.cshtml:13`; `_ForecastBasis.cshtml:3`). | Ưu tiên thông tin máy hơn thời điểm/revision. Nhận xét có căn cứ code, chưa khẳng định lỗi overflow nhánh có dữ liệu. | Thời điểm, revision, độ phủ dẫn đầu; GUID/graph version ở metadata phụ. Giữ đầy đủ giá trị; căn cứ dùng nhãn–giá trị, trạng thái kiểm chứng luôn thấy. | Trung |
| Tài khoản, sao lưu, xóa hồ sơ | Form một cột; một hành động chính; xóa hồ sơ nói rõ lịch sử còn giữ; JSON cảnh báo không mật khẩu, backup yêu cầu mật khẩu. | Ổn: các ghi chú này giúp quyết định, không phải slop để xóa. | Giữ khung hiện tại. Chỉ đồng bộ spacing/control và kiểm lỗi/mobile sau thay đổi chung. Nội dung đồng ý, giới hạn và phạm vi xóa giữ nguyên. | Ổn, giữ |
| Palette, font, container | 12 màu, Inter; độ lồng nền mẫu tối đa 1; không có nền gradient hoặc bóng panel trang trí. | Không có bằng chứng cần thay nền tảng hoặc tiếp tục “làm phẳng” bằng mọi giá. | Giữ palette, font và độ lồng. Không đặt chỉ tiêu giảm màu/container để tạo cảm giác đã redesign. | Ổn, giữ |

Ảnh đại diện để đối chiếu: [editor trên điện thoại](../../.hallmark/audit-2026-11-29/screens/current-Forecast-id-cc95ac70-98ee-4923-8604-82267d230438-month-6-phone.png), [báo cáo hiện tại](../../.hallmark/audit-2026-11-29/screens/current-ho-so-cc95ac70-98ee-4923-8604-82267d230438-bao-cao-desktop.png). Đường dẫn bằng chứng khác nằm trong mục 2, không gắn link lặp cho mọi tệp.

## 5. Cấu trúc màn đề xuất

### Hồ sơ và editor

Desktop giữ main tối đa 1100 px, sidebar 224 px. Không đặt bảng 6 cột và toàn bộ editor cạnh nhau khi không đủ chỗ; bảng cần chiều rộng để đọc tiền. Vùng nhập tháng dùng grid bên dưới bảng, không mở modal để sửa các trường hiện có.

```text
Sidebar hiện có              Hồ sơ năm 2026: bản đã lưu / chưa lưu
  Hồ sơ thuế                 Quy ước nhập liệu chung

  [Ngữ cảnh hồ sơ]            Dữ liệu 12 tháng (#du-lieu)
  Dữ liệu 12 tháng              Giai đoạn lương: từ / đến / gross / trạng thái
  Người phụ thuộc               Bảng 12 tháng (#bang-thang)
  Tổng quan năm                 Sửa tháng được chọn (#sua-thang)
  Lịch sử tính                  Thưởng: các trường theo nhóm, không panel lồng
  Báo cáo và checklist
  Đối chiếu nguồn             Người phụ thuộc (#nguoi-phu-thuoc)

  Công cụ tính tháng          [Xem trước 12 tháng] [Lưu hồ sơ]
  Sao lưu và khôi phục
                             Tổng quan năm (#tong-quan)
                               Tính thuế năm từ dữ liệu đã lưu
                               Nghĩa vụ, độ phủ, trạng thái nguồn, giới hạn
                               Phép tính / đầu vào / căn cứ chi tiết
                               Lịch sử và các điểm đến phân tích hiện có
```

Đây là thứ tự công việc đề xuất, chưa phải DOM đã sửa. Tất cả phần thuộc `du-lieu` phải nằm trước `nguoi-phu-thuoc`, rồi `tong-quan`; giữ mapping scrollspy và thứ tự phím. Không dùng CSS `order` tạo thứ tự nhìn khác thứ tự đọc.

Hồ sơ mới chưa có preview vẫn thấy nhập lương và thao tác hiện có để tạo preview; không dựng bảng số giả. Hồ sơ đã lưu có preview thì bảng xuất hiện trước các form dài. Khi mở sửa tháng hoặc có validation error, dẫn tới `#sua-thang`/ô lỗi; người dùng không phải cuộn lại từ đầu. Khi chưa đủ nghĩa vụ, thông tin tháng thiếu giữ dễ thấy và dẫn tới thao tác sửa.

Mobile giữ cùng thứ tự; nhãn tháng và thao tác phải dùng được mà không cuộn ngang tới tận cột cuối. Thanh hành động theo luồng trang, không đè nội dung. Menu hiện có tiếp tục hỗ trợ mở/đóng, Escape, focus và không JS.

### Các nhóm màn còn lại

| Nhóm màn | Bố cục đề xuất | Giữ nguyên |
|---|---|---|
| Danh sách hồ sơ | Bảng gọn; tên, tình trạng và revision có thứ bậc rõ. Không thêm thẻ để lấp chỗ trống. | Tạo/mở hồ sơ và trạng thái đủ/thiếu. |
| Công cụ tháng | Quy ước nhập một lần; nhập chung theo khoảng tháng gọn; bộ chọn và vùng sửa tháng gần nhau; kết quả bảng. | Năm 2025/2026, preview áp dụng chung, xác nhận ghi đè, undo, đầu vào tháng khác. |
| Chi tiết tháng, kết quả năm | Kỳ + revision, số chính, độ phủ/giới hạn, đầu vào và cách tính; metadata nguồn thành nhãn–giá trị. | Số tiền, snapshot, nguồn chưa kiểm chứng và phân biệt tháng/năm. |
| Báo cáo | Header + xuất CSV; checklist để biết cần bổ sung gì; bảng số và nguồn phía sau. | Checklist không xác nhận điều kiện quyết toán; CSV không thay backup. |
| Kịch bản, đối chiếu | Form gọn; bảng so số cùng trục; giới hạn ngay cạnh kết quả. | Không lưu, không sửa hồ sơ, stale, khác kỳ, thiếu dữ liệu. |
| Lịch sử | Danh sách theo thời điểm/revision/độ phủ; chi tiết bản tính đọc riêng. | Bản cũ và căn cứ cũ, kể cả khi hồ sơ đã xóa. |
| Tài khoản, sao lưu/khôi phục, xóa | Giữ shell gọn hiện có; đồng bộ thành phần chung và responsive. | Nội dung xác nhận, mật khẩu, cảnh báo và phạm vi dữ liệu. |

## 6. Quy tắc component và token khi triển khai

- **Màu:** giữ 12 mã màu; phân lại cách dùng theo mục 6.2. Hành động chính dùng `--brand`; nút phụ dùng chữ `--text`; menu active dùng `--brand-tint`, hover menu dùng `--bg`; trạng thái dùng token ngữ nghĩa kèm nhãn. Đề xuất kết quả năm chuyển sang `--surface`/`--text`, số `--font-title`; không thay nền xanh bằng bóng hoặc viền nhấn bên trái.
- **Chữ:** giữ 13/14/16/20/24/32 px hiện có. Nội dung/nhãn chính 16, metadata 14, h2 20, h1 24, số nghĩa vụ 32. Không dùng 13 px cho thông tin bắt buộc phải đọc để quyết định. Giữ tabular-nums và căn phải số tiền.
- **Spacing:** khoảng số–trạng thái 4 px; nhãn–input 8 px; trường cùng nhóm 12–16 px; nhóm trường 24 px; section khác công việc 32 px. Đây là vai trò của spacing, không thay mọi gap thành một giá trị.
- **Bề mặt:** mỗi nhóm một thiết bị phân tách chính. Bảng dùng hàng và khoảng cách; form dùng nền hoặc khoảng cách. `fieldset` vẫn giữ ngữ nghĩa nhưng phần lồng có thể `padding:0; background:transparent; border:0`. Không thêm panel chỉ để chứa ghi chú.
- **Control:** giữ hit target tối thiểu 44 px cho nút/input/menu; radius 4 px, vùng 8 px. Chưa cần rút control xuống 32 px chỉ để đạt mật độ desktop.
- **Hành động:** một primary nổi bật trong vùng công việc; Forecast Save chính, Preview phụ, Calculate là hành động riêng tại tổng quan dùng kiểu phụ. Điều hướng vẫn là `<a>`, submit/thay đổi trạng thái vẫn là `<button>`. Nút điều hướng có thể là `<a class="button">` mà không gạch chân; không đổi hết anchor thành button.
- **Link:** chỉ bỏ gạch chân khi menu/nút/ô thao tác đã chỉ rõ khả năng bấm. Giữ hover nền tint và `:focus-visible` 2 px, offset 4 px. Không làm cả hàng có hai hành động thành một điểm bấm mơ hồ; không thêm mũi tên hàng loạt.
- **Ghi chú:** một quy ước tiền chung mỗi form; một giải thích gross/net ở nhóm lương. Giữ hướng dẫn ngoại lệ ở ô cần nó. Cảnh báo bảo mật, pháp lý, dữ liệu thiếu và kết quả cũ không bị gộp thành chú thích khó thấy.
- **Disclosure:** giữ cho lịch sử chi tiết nguồn và bước tính dài; thông tin cần quyết định luôn mở. Các ô lỗi nằm trong disclosure phải được mở tự động; lý do giả định cần được thấy khi trạng thái yêu cầu.
- **Nguồn thuế:** trạng thái pháp lý/kiểm tra nguồn độc lập với Thực tế/Dự kiến/Giả định của dữ liệu nhập. Nếu luồng cập nhật tự động theo ADR 0017 được triển khai sau này, UI phải phân biệt hệ thống kiểm tra với người dùng kiểm tra, không yêu cầu duyệt từng văn bản làm điều kiện mặc định để tính; snapshot/căn cứ/lịch sử vẫn phải đọc lại được. Đây là yêu cầu tương thích tương lai, không thêm tính năng cập nhật luật vào lượt redesign này. [k:01M85R5PT58FXP9PJ83VTH7BVZ]
- **Chuyển động:** chỉ phản hồi hành động, không thêm fade-in section, hover zoom hoặc easing nảy. Giữ reduced motion và nội dung khi không JS.

Các giá trị trên là đề xuất sử dụng token hiện có. Không chạy scaffold đè `tokens.css` của dự án. Sau khi duyệt, ghi hệ thống đã chốt vào một tài liệu thiết kế chung; không tạo thêm nhiều nguồn token cạnh tranh.

### 6.1. Hướng theme và cảm giác sử dụng

Theme đề xuất cần tạo cảm giác giống một hồ sơ tài chính rõ ràng: bình tĩnh, dễ quét số, nhận ra việc cần làm và mức chắc chắn của kết quả. Tinh chỉnh có thể nhìn thấy qua phân cấp màu, nút và mật độ; không chỉ ghi lại các token hiện tại.

| Thành phần | Hiện trạng có bằng chứng trong `site.css` | Theme đề xuất | Lý do theo công việc |
|---|---|---|---|
| Nền và bề mặt | Nền trang `--bg`, sidebar/panel trắng; mỗi fieldset thường padding 24 px. | Giữ hai nền. Bảng là một bề mặt trắng; nhóm nhập liên quan dùng khoảng cách, fieldset trong nhóm nền trong suốt. Giữ panel trắng cho form tài khoản và bước xác nhận độc lập. | Người dùng nhận ra vùng công việc mà không phải đọc qua nhiều khung. |
| Nút chính | Nền brand, chữ trắng; weight kế thừa 500; hover có inset shadow. | Nền brand, chữ trắng 600, cao tối thiểu 44 px; hover dùng `--text` làm nền xanh đen, bỏ inset shadow. | Hành động cam kết như Lưu hoặc Xác nhận được nhận ra, phản hồi hover rõ. |
| Nút phụ | Chữ brand như phần lớn link, cùng viền control. | Nền trắng, chữ `--text`, viền `--border-strong`; hover nền `--bg`. | Preview, Hủy và thêm dòng vẫn rõ là nút nhưng không tranh ưu tiên với Lưu. |
| Menu | Hover và active cùng nền `--brand-tint`. | Hover nền `--bg`; active nền `--brand-tint`, weight 600 và `aria-current`; mục thường chữ `--text`. | Nhận ra vị trí hiện tại khi đang rê chuột qua mục khác. |
| Chọn tháng | Hover và current cùng nền tint/chữ brand; tháng có dữ liệu thêm nền `--bg`. | Hover nền `--bg`; current nền tint và chữ brand 600. Giữ nhãn Đang sửa/Đã nhập/Chưa nhập; không thêm chấm trạng thái. | Phân biệt tháng đang sửa với tháng chỉ có dữ liệu. |
| Kết quả năm | Nền xanh đậm bao nghĩa vụ; độ phủ và giới hạn ở ngoài hoặc disclosure. | Nền trắng, số chữ `--text` 32 px/600; độ phủ và giới hạn sát dưới số. Chênh lệch có dấu và nhãn, không chỉ đổi xanh/đỏ. | Đọc số cùng điều kiện sử dụng số; không hiểu số lớn là kết quả đã xác nhận. |
| Bảng số | Preview dùng chữ 14 px, trạng thái 13 px; bảng khác thường 16 px; padding hàng chưa đồng nhất. | Giá trị 16 px, trạng thái 14 px; căn phải số, khoảng cách số–trạng thái 4 px; padding dọc 8 px cho hàng hai dòng. | Tăng khả năng đọc số và trạng thái, bù chiều cao bằng padding thay vì thu nhỏ chữ. |
| Thông tin phụ | `.hint`/`.muted` thường 14 px nhưng vị trí/độ lặp khác nhau. | Giữ 14 px `--text-muted` cho metadata; lỗi/cảnh báo cần quyết định dùng chữ 16 px và nhãn rõ; ghi chú trùng được gộp. | Người dùng phân biệt thông tin bổ sung với điều kiện bắt buộc. |
| Viền và radius | Control 4 px, vùng 8 px; badge mặc định có viền, preview đã bỏ viền. | Giữ radius 4/8 px. Status trong bảng dùng nhãn/icon không khung; lỗi và input giữ viền có nghĩa. | Giảm cạnh tranh thị giác giữa trạng thái và phần tử bấm được. |

Các điều chỉnh nền kết quả, nút phụ và hover là lựa chọn theme cần xem bằng mẫu; không phải kết luận màu cũ gây lỗi chức năng. Phân biệt current/hover, độ đọc trạng thái và việc thông tin quyết định luôn thấy là tiêu chí sử dụng cần kiểm.

### 6.2. Hợp đồng màu và token đề xuất

Giữ nguyên `--brand #395B40`, `--brand-tint #E7FFEC`, `--bg #F3F6F3`, `--surface #FFFFFF` và các màu chữ/viền/trạng thái trong mục 3. Nền chọn sáng chỉ dùng trên diện tích control/menu nhỏ. Màu trắng thuộc bề mặt, không dùng cho một khối trang trí riêng.

Alias theo vai trò dự kiến bổ sung trong `wwwroot/css/tokens.css` khi triển khai:

| Token mới | Giá trị lấy từ palette hiện có | Phạm vi dùng |
|---|---|---|
| `--action-primary-bg` | `var(--brand)` | Nút chính. |
| `--action-primary-hover-bg` | `var(--text)` | Hover/pressed nút chính với chữ trắng. |
| `--action-secondary-text` | `var(--text)` | Nút phụ; không đổi link trong văn bản. |
| `--action-secondary-hover-bg` | `var(--bg)` | Hover/pressed nút phụ. |
| `--navigation-hover-bg` | `var(--bg)` | Menu/bộ chọn tháng chưa active khi hover. |
| `--navigation-current-bg` | `var(--brand-tint)` | Mục/tháng hiện tại. |
| `--result-surface` | `var(--surface)` | Bề mặt kết quả và độ phủ. |

Nút nguy hiểm tiếp tục dùng `--error`; hover có thể giữ màu và tăng độ dày viền bên trong kích thước đã dành sẵn, tránh xê dịch chữ. Focus giữ `--focus`, outline 2 px/offset 4 px và xuất hiện tức thì. Không dùng opacity để làm nhạt toàn bộ nội dung ở trạng thái disabled hoặc pending.

Alias chỉ tạo một nơi quản lý vai trò. Số màu thực vẫn là 12; việc giảm nhiễu phải được kiểm bằng cách các component hiển thị, không lấy số alias làm bằng chứng thành công. Nếu hover nút chính chuyển sang xanh đen không hợp khi xem mẫu, giữ brand và dùng thay đổi viền; chốt một phương án chung trước khi nhân rộng.

### 6.3. Hợp đồng chữ, số và mật độ

| Vai trò | Cỡ / weight / line-height đề xuất | Khoảng cách và quy tắc |
|---|---|---|
| Tiêu đề màn | 24 px / 600 / 1,25 | Cách metadata 8 px; cách nhóm công việc đầu 24 px. |
| Tiêu đề nhóm | 20 px / 600 / 1,25 | Cách nhóm trước 32 px; cách nội dung 16 px. |
| Nhãn input, nội dung và số trong bảng | 16 px / 500 cho nhãn, 400 cho dữ liệu / 1,5 | Nhãn–input 8 px. Input vẫn 16 px trên mobile. |
| Metadata, trạng thái bảng | 14 px / 400–500 / 1,5 | Số–trạng thái 4 px; tránh nhãn 13 px cho thông tin quyết định. |
| Số kết quả chính | 32 px / 600 / 1,25 | Đơn vị đi cùng số; mobile có thể xuống 24 px theo rule hiện có khi cần fit. |
| Hàng hồ sơ một dòng | Nội dung 16 px, metadata 14 px | Padding dọc 12 px, ngang 16 px; chiều cao tự tăng khi nhãn wrap. |
| Hàng tháng hai dòng | Giá trị 16 px + trạng thái 14 px | Padding dọc 8 px, ngang 12 px; không khóa height. Control bên trong vẫn có vùng bấm 44 px. |

Giữ một họ Inter và 6 cỡ token hiện có; không thêm font tiêu đề hoặc mono cho số/mã luật. Cỡ 13 px chỉ còn dành cho metadata phụ ít ưu tiên nếu thực sự cần. Số tiền tiếp tục `font-variant-numeric:tabular-nums`, căn phải; chuỗi số và đơn vị không tách thành hai dòng nếu còn đủ không gian. Không đổi định dạng tiền để ép vừa cột.

Grid nhập: desktop dùng cột cho các trường cùng quyết định, chẳng hạn từ tháng/đến tháng/gross/trạng thái. Mobile giữ cặp tháng trên một hàng, số tiền và trạng thái có hàng riêng. Gap trong nhóm 12–16 px; giữa các khoản 24 px. Các trường nguồn/ghi chú vẫn giữ khả năng truy cập và hiển thị lỗi.

### 6.4. Bộ trạng thái cần thiết kế cùng theme

| Trạng thái | Quy tắc hiển thị |
|---|---|
| Default | Control có biên hoặc nền đủ nhận ra; trạng thái thông tin không có hover/cursor bấm. |
| Hover | Dùng nền theo vai trò ở mục 6.2; active vẫn khác hover. Không zoom, glow hoặc mũi tên xuất hiện. |
| Focus bàn phím | Outline 2 px rõ trên nền thực; không bị clipping, sticky hoặc container che. |
| Pressed/current | Pressed phản hồi nền/viền ngay; current còn có nhãn/độ đậm và thuộc tính aria phù hợp. |
| Disabled | Giữ chữ đọc được và lý do khi không thể suy ra; không dùng opacity toàn vùng. Không vô hiệu hóa Lưu chỉ vì hồ sơ chưa đủ dữ liệu nếu nghiệp vụ vẫn cho phép. |
| Invalid | Viền error + thông báo chữ tại trường; summary dẫn tới đúng ô. Không xóa dữ liệu đã nhập. |
| Pending | Chỉ thiết kế nếu luồng đã có trạng thái chờ: giữ nhãn có nghĩa và vị trí control ổn định; không dựng spinner/skeleton cho một GET SSR thông thường. |
| Empty/stale | Rỗng có lời giải thích và một đường tiếp tục; kết quả cũ có thông báo sát kết quả, không chỉ đổi màu hoặc làm mờ. |

Các trạng thái này là yêu cầu cho component khi triển khai, chưa phải logic mới đã được thêm vào sản phẩm. Điều hướng vẫn là anchor và hành động vẫn là button; affordance đến từ đúng component, không thêm hyperlink nhắc lại.

### 6.5. Mẫu cần xem trước khi áp dụng toàn bộ

Đầu ra đầu tiên sau khi duyệt plan là một bộ mẫu theme dùng cùng component/token trên ba màn thật với dữ liệu synthetic:

1. **Danh sách hồ sơ:** kiểm nền, sidebar, hover/current, bảng gọn, primary tạo hồ sơ và empty state.
2. **Forecast với tháng đang sửa:** kiểm nút chính/phụ, input tiền, ghi chú, error, trạng thái bảng và độ dày nội dung trên phone.
3. **Chi tiết tháng/kết quả đọc:** kiểm số chính, nguồn Unverified, giới hạn kết quả, metadata và disclosure. Kết quả năm dùng cùng recipe; phải kiểm thêm bản có dữ liệu trước khi nhân rộng.

Mỗi mẫu có desktop 1440 px và phone 390 px; bổ sung 320 px cho nhãn dài và 200% zoom. Mẫu theme phải thể hiện cả normal/focus/error và thông tin chưa đủ, không chỉ một ảnh trạng thái đầy đủ. Không thêm route mẫu vào sản phẩm hoặc thay handler để thuận tiện chụp ảnh.

Tiêu chí duyệt: nhận ra primary và vị trí hiện tại ngay; đọc được số/trạng thái; metadata không lấn át; không nhầm badge thành nút; ghi chú không lồng; nguồn/giới hạn không bị giấu. So ảnh cùng dữ liệu và cùng viewport với baseline mục 2. Lượt bổ sung plan này chưa tạo mẫu giao diện mới.

### 6.6. Triển khai theme và đo lại

- Chốt mapping token và recipe trong tài liệu này trước. Khi đã duyệt, thêm alias vào `tokens.css`; sửa các rule thuộc recipe trong `site.css`, gom rule trùng của component bị sửa thay vì nối thêm nhiều lớp override. Giữ các rule không thuộc phạm vi và mọi hook JS.
- Đồng bộ nút chính/phụ, menu, input, bảng và trạng thái trên ba mẫu trước; áp dụng cùng recipe sang tài khoản, báo cáo, kịch bản, đối chiếu và backup/restore sau khi mẫu được xem.
- Kiểm tương phản trên từng cặp chữ/nền thực, gồm primary hover, warning, muted, placeholder, disabled và focus; alias không thay cho kiểm tra này. Không giảm tương phản để đạt cảm giác nhẹ.
- Báo trước/sau về số primary cùng xuất hiện, số vùng dùng nền nhấn, số badge có khung, cỡ chữ trạng thái, số ghi chú trùng và cấp panel. Không tự đặt mục tiêu “giảm còn X” khi chưa đếm cùng trạng thái; 48 thông tin trạng thái tháng vẫn phải còn đủ.
- Tệp dự kiến sửa cho theme: `wwwroot/css/tokens.css`, `wwwroot/site.css` và markup component tương ứng trong kế hoạch từng đợt. Không thay tính toán, route, lưu trữ hoặc nội dung cảnh báo. Chưa chạy scaffold hoặc sửa token sản phẩm trong lượt lên plan.

## 7. Plan triển khai theo đợt

Chỉ bắt đầu sau khi phương án này được duyệt. Thứ tự ưu tiên dựa trên mức cản trở ở mục 4; thay đổi dùng chung phải kiểm lại các màn ít sửa.

| Đợt | Kết quả cụ thể | Tệp dự kiến sửa | Điều kiện xong |
|---|---|---|---|
| 0 — Chốt theme bằng mẫu | Mapping màu theo vai trò; recipe chữ/control/bảng/trạng thái; ba màn đại diện theo mục 6.5 trước khi triển khai rộng. | `wwwroot/css/tokens.css`, `wwwroot/site.css`; `_Layout.cshtml`, `ForecastProfiles.cshtml`, `Forecast.cshtml`, `ForecastMonth.cshtml` và partial tương ứng khi triển khai mẫu. | Xem được desktop/phone, normal/focus/error; primary–secondary và hover–current phân biệt rõ; palette vẫn 12 màu; handler và dữ liệu giữ nguyên. |
| 1 — Điều hướng và che nội dung | Nhóm nav theo ngữ cảnh; action bar theo luồng; trạng thái kiểm chứng/giới hạn kết quả luôn thấy. | `Pages/Shared/_Layout.cshtml`, `Pages/Forecast.cshtml`, `Pages/Shared/_AnnualResult.cshtml`, `wwwroot/site.css`; `workspace.js` chỉ khi mapping nav cần điều chỉnh. | Fragment trong POST preview không mất draft; focus/menu hoạt động; phone không có thanh che input; Enter vẫn Preview. |
| 2 — Editor và bảng tháng | Đưa bảng lên; vùng sửa tháng sát bảng; grid thưởng/phụ thuộc; thao tác tháng dùng được trên phone; bỏ lời nhắc trùng có danh sách trước/sau. | `Pages/Forecast.cshtml`, `Pages/Shared/_ForecastMonths.cshtml`, `Pages/Index.cshtml`, `wwwroot/site.css`; giữ JS nhập tiền và bulk apply/undo. | Nhập và lưu cùng payload; preview giữ ngoại lệ; tháng ẩn không sai tiền; lỗi không bị giấu; sửa một tháng không thay tháng khác. |
| 3 — Đọc và đối chiếu kết quả | Dùng chung thứ bậc kỳ/revision/số/độ phủ/căn cứ; kết quả năm nền trung tính; nguồn theo nhãn–giá trị; metadata lịch sử bớt tranh ưu tiên. | `ForecastMonth.cshtml`, `ForecastAnnualHistory.cshtml`, `ForecastReport.cshtml`, `ForecastScenarios.cshtml`, `ForecastReconciliation.cshtml`, `_AnnualResult.cshtml`, `_ForecastBasis.cshtml`, các partial căn cứ hiện có, `site.css`. | Kết quả/snapshot/giá trị xuất không đổi; nguồn chưa xác minh luôn thấy; số khác kỳ/thiếu và stale vẫn đúng. |
| 4 — Đồng bộ và nghiệm thu toàn UI | Áp dụng theme đã chốt, rà danh sách/tài khoản/backup/restore/xóa sau thay đổi chung; ảnh trước/sau và số đếm lại. | `ForecastProfiles.cshtml`, account/backup/restore/delete templates khi có lỗi cụ thể; `site.css` theo recipe đã chốt, `tokens.css` dùng chung. | Toàn bộ trạng thái trong mục 8 được kiểm hoặc ghi rõ còn thiếu; không thay logic dữ liệu/tài khoản trong tác vụ UI. |

Không xóa route, không đổi PageModel/service/schema, không thêm tính năng tên hồ sơ, biểu đồ hoặc xác thực. Nếu thiết kế phát hiện cần khả năng mới, tách thành yêu cầu chức năng riêng. Các tên tệp template ở bảng trên đều nằm trong `src/TaxManager.Web/Pages/` trừ đường dẫn `wwwroot`.

Không sửa test chỉ để hợp với markup mới nếu hành vi vẫn phải giữ. Nếu test khóa cấu trúc cũ trong khi cấu trúc mới đã được duyệt, nêu rõ mâu thuẫn và giữ kiểm tra hành vi tương đương; không xóa kiểm tra Enter, draft, số tiền hoặc nguồn.

## 8. Danh mục màn và tiêu chí nghiệm thu

| Page / route đang có | Trạng thái cần kiểm sau sửa | Mức bằng chứng lượt rà này |
|---|---|---|
| `ForecastProfiles` — `/ho-so` | Rỗng, nhiều hồ sơ, đủ/thiếu, mở đúng ID. | Ảnh có 2 dòng + code rỗng. |
| `Forecast` — `/Forecast[?id=…]` | Mới, lưu, preview, invalid, sửa tháng, thiếu, annual result, stale. | Ảnh mới/lưu; đọc code các nhánh còn lại. |
| `Index` — `/` | 2025/2026, nhập chung theo khoảng, ghi đè, undo, sửa tháng, kết quả, lỗi. | Ảnh đầu vào + code/JS. |
| `ForecastMonth` — `/ho-so/{id}/thang/{month}?revision=…` | Đúng bản lưu/tháng, đủ/thiếu, tháng lỗi, nguồn Unverified. | Ảnh tháng 6 + code. |
| `ForecastAnnualHistory` — `/ForecastAnnualHistory?id=…[&record=…]` | Rỗng, nhiều lần tính, bản cũ, hồ sơ đã xóa, chi tiết. | Ảnh rỗng; code các nhánh có dữ liệu. |
| `ForecastReport` — `/ho-so/{id}/bao-cao` | Checklist thiếu/đủ, CSV, nguồn, có/chưa có annual cùng revision. | Ảnh một bản lưu; code các nhánh. |
| `ForecastScenarios` — `/ho-so/{id}/kich-ban` | Cơ sở, có thưởng, lỗi, thiếu, stale. | Ảnh cơ sở; code/JS các nhánh. |
| `ForecastReconciliation` — `/ho-so/{id}/doi-chieu` | Form, kết quả, khác kỳ, thiếu, số 0, stale. | Ảnh form; code/JS các nhánh. |
| `ForecastDelete` — `/ForecastDelete?id=…` | Revision hiện tại/cũ, xác nhận, hủy, lịch sử được giữ. | Ảnh xác nhận + code. |
| `Backup` — `/sao-luu` | Kho gốc/khôi phục, lỗi mật khẩu, tải tệp. | Ảnh kho gốc + code. |
| `Restore` — `/khoi-phuc` | Chọn tệp, lỗi, kho đang khôi phục. | Ảnh form + code. |
| `RestorePreview` — `/khoi-phuc/xem-truoc` | Handle hợp lệ, hết hạn/đã dùng, hủy, mở kho. | Code; request không handle nhận 410. |
| `Accounts/Login` — `/tai-khoan/dang-nhap` | Rỗng, sai mật khẩu, khóa, hiện/ẩn mật khẩu. | Render snapshot + code; không kiểm auth trong browser. |
| `Accounts/Register` — `/tai-khoan/dang-ky` | Rỗng, policy mật khẩu, xác nhận, lỗi, đọc phạm vi. | Render snapshot + code. |
| `Accounts/DataUse` — `/tai-khoan/du-lieu-thu` | Chưa/đã xác nhận, version, lỗi. | Render snapshot đã xác nhận + code; nội dung đang được cập nhật ngoài lượt này. |
| `Accounts/Index` — `/tai-khoan` | Đăng nhập, dữ liệu, phiên, điều hướng. | Render snapshot + code. |
| `Accounts/Export` — `/tai-khoan/xuat-du-lieu` | Phạm vi JSON, mật khẩu sai/đúng, kho gốc/khôi phục. | Render snapshot + code. |
| `Accounts/Delete` — `/tai-khoan/xoa` | Theo hợp đồng của chức năng đang phát triển; kiểm lại sau khi ổn định. | Chỉ đọc mã workspace; ngoài baseline đã publish. |

### Cổng nghiệm thu khi triển khai

1. Render các nhóm màn ở 320/375/390/768/1280/1440/1920 px; kiểm 200% zoom, nhãn tiếng Việt dài và bàn phím mobile. Cuộn ngang chỉ ở bảng chủ đích; không cắt control bằng `overflow-x:clip` để che lỗi bố cục.
2. Tab/Shift+Tab, Enter, Escape, menu resize, skip link và error links phải tới vùng thấy được. Kiểm Enter trong ô Forecast: Preview, không bỏ giai đoạn hoặc xóa tháng. Không chỉ dựa vào test HTTP.
3. Tắt JS: form, dữ liệu, cảnh báo và điều hướng vẫn dùng được theo fallback hiện có. Không thêm skeleton/loading khi luồng SSR không có logic tải bất đồng bộ.
4. Giữ tiền vi-VN và marker `AmountFormat=vi-VN`; kiểm cả input tháng ẩn. Giữ null khác 0, ngoại lệ, nguồn/ngày nhập, actual/planned/assumed và stale result.
5. Kiểm quy trình thực: tạo → preview → sửa tháng → lưu → mở lại → tính năm → đọc bản cũ → CSV; công cụ tháng bulk apply/undo; so sánh/đối chiếu không ghi dữ liệu. Các luồng xóa/restore dùng dữ liệu synthetic và xác nhận hiện có.
6. Kiểm tương phản các cặp chữ/nền thực sau sửa, focus và status không chỉ dựa màu. Không báo đạt accessibility chỉ từ ảnh hoặc linter Hallmark.
7. Chạy test liên quan hiện có trong `ForecastHttpTests`, `ForecastProfilesHttpTests`, `GroupedMoneyHttpTests`, `ForecastPersistenceBlackBoxTests`, `AnnualSavedProfileBlackBoxTests`, các test Month/Report/Scenario/Reconciliation/Delete, Backup và Security theo phần bị ảnh hưởng. Sau sửa mã chạy `bint test affected --run` theo AGENTS; đọc widening nếu công cụ không chọn được test. Nếu phải dùng cổng Web MVP thay thế, chuẩn bị SDK/assets và chạy hết script theo quy ước dự án.
8. Chạy Hallmark `--render --kept` trên cùng mẫu/route đã có baseline, đối chiếu wiring Razor và JS thủ công bổ sung. Baseline linter chỉ có 3 route và không trích được đầy đủ handler Razor; không dùng số “0 handlers” để kết luận không có wiring cần giữ.

### Số đếm sẽ báo lại sau triển khai

| Chỉ số cùng mẫu | Trước | Mục tiêu / cách quyết định | Hiện tại sau lượt lập plan |
|---|---:|---|---|
| Vị trí đầu preview, phone 390 px | y=4.078 px | Trước vùng sửa tháng và form thưởng dài; đo lại cùng dữ liệu, không đặt số giảm giả định. | Chưa sửa. |
| Chiều cao action bar phủ viewport Forecast | 124 px | 0 px phủ viewport khi dùng luồng trang. | Chưa sửa. |
| Vùng nền / cấp lồng, editor mẫu | 4 / 1 | Tối đa 1 cấp; số vùng theo công việc. Không bắt buộc giảm 4 nếu cả 4 còn cần. | Chưa sửa. |
| Vai trò màu brand theo phân loại mục 3 | 6 | 5 nếu chấp thuận thay nền summary bằng thứ bậc chữ. | Chưa sửa. |
| Trạng thái từng chỉ tiêu trong preview đầy đủ | 48 | Giữ 48 thông tin trạng thái; không gộp mất nghĩa. | Chưa sửa. |
| Câu nhắc “Preview không lưu” trong heading/action bar | 2 vị trí | 1 vị trí; vẫn phân biệt rõ preview và save. | Chưa sửa. |
| Font / màu gốc | 1 họ / 12 giá trị | Giữ. | Không đổi. |
| Viền, đường kẻ, nền, bóng của từng nhóm đã sửa | Chưa có tổng toàn ứng dụng theo mọi state | Đếm lại từng viewport/state với quy tắc đếm như mục 3, ghi cả mục giữ nguyên. | Không tuyên bố đã giảm. |

## 9. Đọc kết quả Hallmark đúng ngữ cảnh

Lệnh baseline: `sloplint.mjs src/TaxManager.Web/wwwroot --genre modern-minimal --scope app --render --before`, với URL `/Forecast`, `/`, `/ho-so`.

| Kết quả máy | Đánh giá khi đối chiếu |
|---|---|
| 7 FAIL nhãn xuống dòng | Hai nhãn sidebar lặp trên 3 route, cộng 1 nút phụ thuộc ở 320 px. Chưa có bằng chứng mất chữ. Đưa vào việc điều chỉnh nav/nhãn và kiểm chiều cao; không ép `nowrap` gây overflow. |
| 2 FAIL “hai sticky top:0” | Một phát hiện render, một phát hiện CSS; sidebar và header bảng thuộc các vùng khác nhau. Chưa chứng minh chúng đè nhau; không thêm offset mù theo gợi ý máy. |
| 1 REFLEX “10 font families” | Nhận diện sai các token cỡ chữ thành font family; source cho thấy một họ Inter. Không đổi font để làm linter im. |
| 2 REFLEX nút tháng 71 px | Lặp trên 2 route; mỗi nút có tháng và trạng thái hai dòng. Xem xét mật độ trong thiết kế, không coi chiều cao này là lỗi accessibility. |
| 1 REFLEX fieldset chật | Cần kiểm lại sau khi bỏ sticky action bar; ảnh và số đo đã xác nhận thanh thao tác che nội dung. Chưa quy mọi trường hợp âm 82 px thành padding sai. |

Lượt này **không có kết quả After, không có tuyên bố Floor pass và không chạy test nghiệm thu thay đổi UI**. Chỉ thực hiện rà hiện trạng và lập kế hoạch.

## 10. Quan hệ với tài liệu cũ

`UI_UX_PLAN.md` là kế hoạch nền. Các đoạn cũ nói route kịch bản/đối chiếu/sao lưu chưa có, hoặc đề xuất thêm route tổng quan/quyền dữ liệu, phải đọc cùng danh mục mã hiện tại ở mục 8; lượt redesign này không thực hiện các route đề xuất cũ. Roadmap và PLAN tiếp tục quyết định tiến độ nghiệp vụ.

Sau khi duyệt, chốt theme bằng đợt 0, triển khai đợt 1 rồi đợt 2 để xem bố cục editor trên desktop/mobile trước khi lan sang các màn đọc kết quả. Các lỗi che nội dung hoặc giấu thông tin quyết định ở mục 4 phải được xử lý trước khi nghiệm thu mẫu. Mỗi đợt giao ảnh cùng trạng thái trước/sau, mục đã giữ/bỏ và kết quả kiểm chứng; phản hồi dựa trên màn chạy thật.
