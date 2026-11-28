# Kiểm tra tương tác và khả năng truy cập Đ1

Ngày 28/11/2026. Tiếp sau [màn chi tiết tháng](2026-11-28-forecast-month.md), áp dụng ui-ux-pro-max và frontend-design. Phạm vi local Development/SyntheticDemo, dữ liệu giả trong TEMP. Phần đầu ghi lượt QA ban đầu; điều chỉnh theo phản hồi về dropdown nằm ở cuối và thay thế kiểu menu/nhóm thu gọn ban đầu.

## Thay đổi

- Dưới 64rem, điều hướng thu thành disclosure native “Mở điều hướng”, giữ thứ tự và tên mục. Enter/Space mở, Tab/Shift+Tab di chuyển, Escape đóng và trả focus về nút. Chọn liên kết cùng trang đóng menu và chuyển focus tới phần tương ứng. Desktop giữ sidebar mở; không có JavaScript thì menu mặc định mở và các liên kết vẫn dùng được.
- Các ô input/select/textarea có lỗi từ server nhận `aria-invalid` và `aria-describedby` tới nhóm thông báo tương ứng. Giữ mô tả có sẵn, bỏ qua hidden input; dùng cùng quy tắc tạo ID cho ô và liên kết lỗi. Một ô có nhiều lỗi vẫn chỉ có một ID, các thông báo tách dòng.
- Chú giải trạng thái có role group để tên truy cập của nhóm được hỗ trợ. Không thêm vạch chia trang trí hay đổi token màu.

## Bằng chứng

- Cổng đầy đủ `scripts/dev/verify-web-mvp.ps1` qua **654/654**: Core 209, Data 328, Web 117; EF không có thay đổi model. Log `%TEMP%/taxmanager-ux-verify.log`.
- `bint test affected --run` chọn graph Web/Web.Tests và qua **117/117**, mã thoát 0. Lần gọi đầu thiếu SDK trong shell; đã kích hoạt SDK local và chạy lại thành công. Không đổi cấu hình hoặc thu hẹp phạm vi. Sau hai chỉnh markup cuối (thông báo tách dòng và role chú giải), chạy lại qua **117/117**, log `%TEMP%/taxmanager-ux-affected-release.log`. Core/Data/schema không đổi sau cổng đầy đủ.
- Ba ca HTTP tiền sai định dạng kiểm thêm thuộc tính lỗi, giữ `salary-terms`, ID và liên kết lỗi; vẫn kiểm không có hồ sơ được lưu. Không nới test dữ liệu hoặc đổi parser tiền.
- Browser Orca, profile QA riêng: mở bản lưu → sửa tháng 6 → gõ `4000000` thành `4.000.000` khi ô còn focus → Enter trên nút lưu → mở chi tiết revision mới. Bảo hiểm từ 3 triệu lên 4 triệu đổi thuế mẫu từ **415.000,00 đ** thành **365.000,00 đ**.
- Nhập `abc`, Enter để xem trước: giữ nguyên chuỗi, focus tới bảng lỗi; Tab rồi Enter trên liên kết đưa activeElement tới `Exceptions_5__Insurance`, phần chứa ô mở và ô nằm trong viewport. Đây là thao tác phím sau khi công cụ đặt focus ban đầu, chưa phải bằng chứng toàn bộ luồng chỉ dùng Tab từ đầu trang.
- Menu mở bằng Enter và Space, Tab/Shift+Tab di chuyển, Escape đóng và activeElement trở lại summary. Liên kết “Dữ liệu 12 tháng” cùng trang đưa focus tới `du-lieu` và đóng menu. Sau resize sang desktop và nhận sự kiện media query, menu mở lại, summary ẩn.
- Form lỗi không tràn ngang trang ở **320/375/768/1280px**; bảng dữ liệu có vùng cuộn riêng. Tăng font gốc lên 200% ở 1280px cho body 32px, không tràn ngang trang. Màn chi tiết tháng cũng không tràn ở 320px. Đây là kiểm thay cỡ chữ/reflow, không phải zoom native.
- axe-core 4.11.0, WCAG 2A/2AA/2.1AA: ban đầu có một mục cần kiểm thêm do `aria-label` trên div chú giải; đã bổ sung role group. Build và mở lại binary mới, kiểm lại cả form đã lưu và màn tháng 6 ở 375px, menu mở: **0 vi phạm tự động, 0 mục incomplete** ở cả hai trang. Ảnh menu mở và chi tiết tháng khi menu đóng đã xem trực tiếp trong lượt QA.

## Điều kiện còn mở

Không đánh dấu hoàn tất nghiệm thu UX Đ1. Chưa kiểm được zoom native 200%/400% trong browser nhúng; thao tác zoom đã thử không đổi mức zoom. Chưa kiểm lời đọc và thứ tự đọc bằng NVDA/Narrator hoặc thiết bị thật. Khi browser không có focus cửa sổ, activeElement vẫn đổi nhưng `:focus-visible` không kích hoạt; không lấy các lượt đó làm bằng chứng viền focus. Cần hoàn tất kiểm focus nhìn thấy/không bị sticky che khi cửa sổ có focus và luồng bàn phím đầy đủ.

Đ2 giữ phạm vi đã hoàn tất. Đ3/chặng 4 vẫn cần contract báo cáo/checklist và sao lưu có bảo vệ riêng; chưa triển khai trong lượt này. Dark mode vẫn là hướng tham khảo.

## Điều chỉnh visual sau phản hồi về dropdown

Người dùng nhận xét việc lặp mũi tên và dropdown làm giao diện vụn, mang cảm giác khuôn mẫu. Giữ font, màu và khoảng cách hiện có; sửa cách tổ chức nội dung thay vì thêm kiểu icon khác.

- Mobile có nút chữ **Menu / Đóng menu** cùng hàng tên ứng dụng, thay disclosure có tam giác. Desktop giữ sidebar. Dùng aria-expanded/controls, Enter/Space và Escape; khi đổi breakpoint, focus chuyển tới phần tử còn hiển thị. HTML khi chưa chạy JS vẫn hiện nav.
- Form hiện trực tiếp thưởng, người phụ thuộc và chọn/sửa tháng; thông tin thiếu ở danh sách hồ sơ cũng hiện trực tiếp. Nguồn/ghi chú của bốn chỉ tiêu tháng gom thành một nhóm. Nguồn/ghi chú của bảng 12 tháng gom một khối bên dưới, có tiêu đề theo tháng, giữ đủ giá trị/ngày nhập và metadata thưởng/phụ thuộc.
- Chi tiết tháng hiện trực tiếp chú thích gross/net, các khoản thưởng và bảng bậc thuế. Tất cả căn cứ nằm chung một khối tra cứu, không có disclosure con cho từng nguồn. Summary còn lại dùng chữ gạch chân, bỏ marker tam giác, giữ tương tác native.
- Với cùng hồ sơ synthetic đang sửa tháng 6: form giảm **22 → 3** khối details trong main; màn chi tiết tháng giảm **10 → 2**. Đây là số đo của hồ sơ mẫu, không phải giới hạn cứng cho số giai đoạn người dùng có thể thêm.
- `bint test affected --run`: **117/117 Web**, mã thoát 0; log `%TEMP%/taxmanager-ui-disclosure-tests.log`. Không sửa hoặc nới test để đổi bố cục; dữ liệu/Core/schema không đổi. Hai cảnh báo xUnit2031 cũ vẫn giữ nguyên.
- Browser trên binary mới: Menu Enter → Tab tới liên kết → Escape trả focus về Menu và aria-expanded=false; sang 1280px nav hiện, nút ẩn và focus chuyển tới liên kết. Không tràn ngang form ở 320/375/768/1280px. Màn tháng vẫn hiển thị **365.000,00 đ**; ảnh form, chi tiết tháng và textarea đã mở xem.
- axe-core 4.11.0, WCAG 2A/2AA/2.1AA với các khối main mở: màn tháng **0 vi phạm / 0 incomplete**; form **0 vi phạm**, một nhóm incomplete `color-contrast` tại ba textarea ghi chú đang trống. Cuộn từng ô vào viewport: tâm ô đúng chính textarea, chữ `rgb(26,31,27)` trên trắng có tỷ lệ **16,72:1**; ảnh không có nội dung che ô. Giữ ghi nhận incomplete của công cụ, không gọi đây là kết quả tự động hoàn toàn sạch.

Đã thêm quy tắc hạn chế accordion và mũi tên lặp vào UI plan mục 10; kế hoạch và roadmap giữ các mục nghiệm thu thủ công còn mở ở trên.

## Tách danh sách và editor, bổ sung logo và icon

- Danh sách riêng `/ho-so`, tạo/sửa giữ `/Forecast` và URL ID hiện có. Mỗi màn một nút đặc: Tạo hồ sơ hoặc Lưu hồ sơ. Sau xóa quay về danh sách. Bảng ẩn UUID, hiển thị Thiếu/Đủ dữ liệu bằng chữ và màu, số phiên bản; hàng mẫu cao 56px, các cột cùng đường chuẩn, chỉ có đường ngang mảnh.
- Form dùng panel trắng không khung fieldset, ô tháng hẹp, tiền căn phải/tabular có đ trong ô, ví dụ và mô tả định dạng ngay dưới. Giữ nullable, metadata và lỗi server. Thanh hành động dính cuối form; Enter vẫn xem trước nhờ submit mặc định đứng trước các nút bỏ dòng, không lưu hoặc xóa ngầm.
- Sidebar có logo sổ thuế nhỏ và icon SVG nét đồng bộ; nút tạo/xem trước/lưu có icon cùng nhãn chữ. SVG trang trí aria-hidden, không thêm mũi tên. Tên TaxManager giữ một hàng; nội dung tối đa 1100px. Thanh trên chỉ có huy hiệu Demo local (dữ liệu mẫu).
- `bint test affected --run` chọn Web/Web.Tests, qua **118/118**, mã thoát 0; log `%TEMP%/taxmanager-profile-ui-release.log`. Thêm một test HTTP phân tách màn hình, trạng thái thiếu/đủ và số 0 đã nhập; cập nhật route trong test mở lại bằng host mới và kiểm mô tả tiền. Giữ nguyên các test blackbox đóng băng. Cổng đầy đủ 654/EF ở trên thuộc lượt trước; không gọi đây là lượt chạy đầy đủ 655 test.
- Browser trên binary mới: danh sách và editor không tràn ngang trang ở **320/375/768/1440px**; bảng danh sách có cuộn riêng ở màn hẹp. Mỗi main một nút chính; logo một dòng. Thanh thao tác nằm sát đáy viewport, các nút xếp lại trong 320px. Đã xem ảnh desktop và mobile. Gõ `123456789` thành `123.456.789`, Enter xem trước giữ tháng đang sửa; số 0 và ô trống vẫn khác nhau. Lưu/mở lại hồ sơ synthetic tạo phiên bản 4, tháng 6 vẫn giữ bảo hiểm 4 triệu và thuế mẫu 365.000 đ.
- Màu CSS thực tế: chữ phụ trên nền trang **6,09:1**, trắng **6,63:1**; viền input trên trắng **3,84:1**; brand/trắng **7,65:1**; nhãn thiếu/trắng **6,19:1**.
- axe-core 4.11.0, WCAG 2A/2AA/2.1AA: danh sách **0 vi phạm / 0 incomplete**; editor **0 vi phạm**, incomplete tương phản tại ba textarea ghi chú trống. Chữ các ô này là rgb(26,31,27) trên trắng (16,72:1); giữ ghi nhận incomplete. Các mục screenreader, zoom native và kiểm đầy đủ focus bàn phím ở phần điều kiện còn mở vẫn chưa đóng.
- Lượt affected sau chỉnh logo một hàng cũng qua **118/118**, log `%TEMP%/taxmanager-profile-icons-final.log`. QA ảnh cuối phát hiện trang danh sách ngắn khiến grid giãn vùng header mobile; đặt hàng grid `auto 1fr`, kiểm lại 375px: header gọn 76px, trang không tràn ngang và còn đúng một nút chính. Thay đổi CSS cuối này được kiểm trực tiếp bằng browser.

## Sidebar dính và tự định vị theo phần đang xem

- Sidebar desktop dính ở đầu viewport, cao bằng cửa sổ và cuộn riêng khi không đủ chỗ; mobile giữ thanh Menu dính, menu mở phủ lên nội dung để tránh dịch phần đang đọc. Đóng menu khi chuyển mục, Escape hoặc focus rời vùng điều hướng; giữ khoảng cuộn để tiêu đề không bị thanh Menu che.
- Các mục Dữ liệu 12 tháng, Người phụ thuộc và Tổng quan năm dùng fragment trong chính editor, kể cả tài liệu POST. Mục đang chọn dùng `aria-current=location`, cập nhật khi heading đi vào một phần ba trên của vùng đọc. Dữ liệu riêng theo tháng và bảng xem trước thuộc Dữ liệu 12 tháng. Cuộn tay không thêm lịch sử URL hoặc chuyển focus.
- Browser 1440×900: cuộn từ 0 → 2300 → 3300 → cuối trang → 2575 → 0 tự đổi Dữ liệu → Phụ thuộc → Dữ liệu → Tổng quan → Phụ thuộc → Dữ liệu, sidebar top=0 suốt lượt và fragment cũ giữ nguyên. Bấm link desktop tới đúng phần; dùng viewport 1440×300, focus mục cuối tự cuộn trong sidebar và mục nằm trong vùng nhìn.
- Browser mobile 375×812: thanh Menu cao 76px, top=0 khi cuộn; mở menu không đổi vị trí phần đang đọc trong probe DOM cùng lượt. Chuyển qua ba mục đóng menu, tiêu đề nằm dưới header (108px hoặc thấp hơn khi sát cuối trang). Nhập tạm 45.678.900 rồi chuyển mục giữ nguyên giá trị và cùng document; đã trả về 30.000.000, không lưu phiên bản mới. Probe chương trình cho menu/link bổ sung cho lượt click thật desktop, không thay thế nghiệm thu bàn phím đầy đủ.
- `bint test affected --run` qua **119/119 Web**, mã thoát 0, log `%TEMP%/taxmanager-sticky-navigation-tests.log`. Test HTTP mới kiểm các link từ tài liệu POST Preview vẫn cùng pathname/query và có đích tương ứng. Các sửa CSS/JS sau lượt này được kiểm trực tiếp trong browser; không thay Core/Data/schema.
- Chạy lại affected sau các chỉnh sửa cuối: **119/119**, mã thoát 0, log `%TEMP%/taxmanager-sticky-navigation-final.log`. Kiểm focus rời menu mobile: aria-expanded=false, nav hidden và focus ở Gross. axe trên editor mới: **0 vi phạm**, vẫn còn incomplete tương phản ở ba textarea ghi chú trống đã ghi ở trên. Không đóng các mục nghiệm thu thủ công còn mở.
