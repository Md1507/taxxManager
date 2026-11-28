# Báo cáo/checklist bản đầu và thứ tự điều hướng

Ngày 28/11/2026. Tiếp tục Đ3/chặng 4 trong Development/SyntheticDemo, theo [contract báo cáo](../design/forecast-report.md). Không sửa Core/Data/schema, migration hoặc các test blackbox đóng băng; không triển khai backup/khôi phục.

## Kết quả

- Editor gom giai đoạn lương/thưởng, dữ liệu riêng theo tháng và bảng xem trước liền nhau; sau đó Người phụ thuộc → Tổng quan năm. Sidebar cùng thứ tự. Test HTTP kiểm thứ tự fragment và thứ tự bảng trước phụ thuộc, đồng thời giữ kiểm liên kết trong tài liệu POST.
- Một màn `/ho-so/{id}/bao-cao`: checklist dữ liệu nhóm theo chỉ tiêu còn thiếu, 12 tháng có nhãn/metadata, thưởng đã gồm gross hoặc cộng thêm, kết quả năm đã lưu nếu cùng revision/payload. Không tính lại khi GET, không dùng kết quả của phiên bản khác. Một nút chính Xuất CSV, giữ palette/font và sidebar đã có.
- Reader dùng transaction và các store/service hiện có để kiểm protection và đọc nhất quán. Link CSV ghim revision và record; Guid.Empty ghim trạng thái chưa có kết quả. Thay đổi hồ sơ hoặc tính lại sau khi mở trang không đổi nội dung của link xuất cũ.
- CSV UTF-8 BOM/CRLF, quote mọi ô; số decimal invariant, thiếu để trống khác 0. Giữ nguồn/ghi chú/ngày nhập/trạng thái, nhãn thưởng, số liệu năm, định danh bản tính/engine và căn cứ luật đúng bản lưu. Ô chữ có nguy cơ công thức được thêm nháy đơn; không biến kết quả số âm thành chuỗi công thức. HTML/CSV no-store, tải về dạng attachment, không chứa khóa.

## Kiểm chứng

- Kiểm hẹp ban đầu 2/2 ca phân tách màn hình/điều hướng qua. Sau thêm báo cáo, 5/5 ca mục tiêu qua: dữ liệu thiếu/0, nhãn giả định/dự kiến/thực tế, nguồn công thức, dấu quote/xuống dòng, BOM/quoting, đọc không ghi, ghim revision/kết quả hoặc trạng thái chưa tính, sai revision/record, hồ sơ xóa và dữ liệu hỏng. Lượt đầu phát hiện assertion test mới coi kết quả decimal 0.00 khác 0; đổi sang so decimal chính xác, không thay dữ liệu xuất.
- `bint test affected --run` qua **122/122 Web**, mã thoát 0; log `%TEMP%/taxmanager-report-affected.log`. Core/Data không đổi; đây không phải cổng đầy đủ Core/Data/EF. Hai cảnh báo xUnit2031 cũ không đổi.
- Browser Orca trên binary build mới, kho synthetic cũ trong TEMP: danh sách mục Dữ liệu → Người phụ thuộc → Tổng quan khớp thứ tự đích. Tại 1440×900 cuộn qua dữ liệu → phụ thuộc (y=3500) → tổng quan (cuối trang) rồi ngược lại, sidebar top=0; không quay lại Dữ liệu ở giữa Phụ thuộc/Tổng quan. Tại 375×812, cả ba đích có nhãn tương ứng, top=108/108/192px dưới header 76px, trang không tràn ngang.
- Màn báo cáo không tràn trang tại 320/375/768/1440px, bảng có vùng cuộn riêng, mỗi màn đúng một nút chính. Đã xem ảnh desktop và mobile, rút gọn các liên kết checklist bị lặp. axe-core 4.11.0 WCAG2A/2AA/2.1AA trên markup cuối: **0 vi phạm, 0 incomplete**.
- Fetch chính link xuất trong browser trả 200, `text/csv; charset=utf-8`, attachment `taxmanager-2026-v4.csv`, no-store và BOM `[239,187,191]`; dữ liệu revision 4 của hồ sơ mẫu. Chưa kiểm mở file trong Excel desktop thật.
- Bản xem thử cuối chạy cổng **5089** bằng host tạm ngoài repository dùng cùng Development/SyntheticDemo và kho synthetic hiện có; cổng 5088 là binary của lượt trước. Không tác động dữ liệu/khóa thật, không tạo kết quả tính mới trong browser QA.
- Sau các chỉnh cuối, chạy lại `bint test affected --run`: **122/122 Web**, mã thoát 0; log `%TEMP%/taxmanager-report-final-tests.log`. Ca lỗi kiểm thêm thiếu DataProtectionMetadata kể cả truy vấn ID không tồn tại: trả 503, không tải CSV. Build host xem thử cuối không có warning/error; `git diff --check` qua.

## Phần còn mở

Chặng 4 mới xong phần báo cáo/checklist dữ liệu. Checklist không xác nhận điều kiện quyết toán; sao lưu/khôi phục cần contract bảo vệ bản sao, kiểm nhập và xử lý trùng trước khi triển khai. Nghiệm thu screenreader, zoom native và toàn bộ luồng bàn phím ở [báo cáo UX](2026-11-28-ui-accessibility.md) vẫn mở. Không gọi CSV là backup hoặc tờ khai chính thức.
