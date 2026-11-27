# Chi tiết tháng từ hồ sơ forecast đã lưu

Ngày 28/11/2026, tiếp phần còn mở của UI Đ1. Thực hiện trực tiếp theo yêu cầu người dùng; áp dụng frontend-design, ui-ux-pro-max, ASP.NET Core và QA bằng Orca.

## Kết quả

- Bảng 12 tháng của hồ sơ đã lưu có liên kết tới `/ho-so/{id}/thang/{month}?revision={revision}`. Bản xem trước chưa lưu không cung cấp liên kết kết quả giả cho thay đổi đang nhập.
- Màn mới hiển thị thuế tháng trước, trạng thái nghĩa vụ, đầu vào từng chỉ tiêu, thưởng, nguồn/ghi chú/ngày nhập, bước tính, từng bậc và căn cứ. Có điều hướng 12 tháng và một nút chính sửa đúng tháng tại form hiện có; lưu hồ sơ giữ tháng đang sửa.
- `ForecastMonthService` chỉ đọc: xác thực protection kể cả ID không tồn tại; đọc hồ sơ, snapshot cùng revision và catalog trong một transaction. Không ghi history/audit hay tạo UserProfile/MonthlyIncome trung gian. Lỗi revision, thiếu catalog, khóa/sentinel lỗi không fallback.
- Tái dùng Core `BonusMonthlySalaryCalculator`: thưởng đã gồm gross được tách khỏi phần nền rồi đưa qua Core một lần cùng thưởng cộng thêm. Không viết phép tính thuế trong Razor/JS. Khấu trừ chưa nhập giữ null và không chặn nghĩa vụ; tháng khác thiếu dữ liệu vẫn xem được tháng đủ dữ liệu.
- Kết quả tính khi mở và được ghi rõ **chưa lưu vào lịch sử**. Không phải snapshot lịch sử bất biến; mở lại dùng catalog hiện lưu. Lịch sử năm append-only giữ nguyên.
- Giữ tokens sáng, Inter, trạng thái có chữ/hình/viền, tự format tiền trong editor và hạn chế vạch chia. Sau nhìn màn thật đã đưa số thuế lên đầu và thu giải thích gross/net vào disclosure trên màn chỉ đọc; phần nhập lương vẫn có giải thích trực tiếp. Hướng tham khảo ảnh dark mode và layout đã ghi ở UI plan mục 4.4; chưa bật dark mode.

## Kiểm chứng

- `bint test affected --run` đã được gọi nhưng không chạy test: bgraph nới tới `full` vì ảnh PNG báo cáo chưa được ánh xạ; `eval.test_command` hiện null. Giữ cấu hình Bint, không thu hẹp phạm vi hay bỏ ảnh để vượt cổng.
- Thay bằng cổng đầy đủ `powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/dev/verify-web-mvp.ps1`: **653/653**, Core 209, Data 328, Web 116; build sạch và EF `has-pending-model-changes` không có thay đổi. Log `%TEMP%/taxmanager-month-detail-verify.log`.
- Sau điều chỉnh bố cục và thêm ca host mới, chạy lại toàn bộ Web: **117/117**, mã thoát 0. Log `%TEMP%/taxmanager-month-detail-web-final.log`. Tổng ca hiện có là **654**; không gọi đây là một lượt cổng đầy đủ 654. Data/Core/schema không thay sau cổng đầy đủ.
- 9 ca Data mới kiểm: đúng kỳ tháng 6/7, thưởng không cộng trùng/khác tháng, thiếu khác 0, khấu trừ độc lập, trạng thái, ngày nhập, revision cũ/xóa, catalog thiếu/lệch, khóa/sentinel và pending writes. Kiểm cả payload/ciphertext/revision không bị ghi lại, không phát sinh history/audit hoặc hồ sơ demo.
- 8 ca HTTP mới kiểm: lưu → mở chi tiết → mở editor đúng tháng → sửa/lưu → kết quả mới; metadata của khoản không sửa giữ nguyên; revision cũ trả 409, không tồn tại/xóa trả 404, thiếu catalog trả 503, tham số sai trả 400. Dispose host cũ rồi mở host mới cùng kho/khóa: giai đoạn, ngoại lệ, thưởng và ghi chú vẫn cho đúng kết quả tháng.
- Oracle synthetic: gross 30 triệu đã gồm thưởng 2 triệu, cộng thêm thưởng 3 triệu, bảo hiểm 3 triệu và một người phụ thuộc: `(33 − 3 − 15,5 − 6,2) triệu × 5% = 415.000`. Sửa bảo hiểm thành 4 triệu: 365.000; gross ngoại lệ 31 triệu sau mở host mới: 465.000.
- Browser trên binary đã build, Development/SyntheticDemo loopback, kho TEMP giữ nguyên qua lần chạy lại. Đã mở xem ảnh ở **375/768/1280px**, không tràn ngang trang; bộ chọn tháng 44×44px tối thiểu, đủ 12 liên kết và tháng hiện tại có `aria-current`. Ở 375px, axe-core 4.11.0 WCAG2A/2AA/2.1AA với toàn bộ disclosure mở: **0 vi phạm tự động**, đúng màn tháng 6 và số thuế 415.000,00 đ.
- Không xem axe là bằng chứng screenreader/thiết bị vật lý/zoom native 200%. Luồng ghi và bảo toàn metadata đã kiểm qua HTTP; không tuyên bố đã hoàn tất toàn bộ thao tác lưu bằng bàn phím trong browser của lượt này. Hai cảnh báo xUnit2031 cũ không thuộc thay đổi.

Ảnh đã tự xem: [375px](ui-phase-one-two/month-375.png), [768px](ui-phase-one-two/month-768.png), [1280px](ui-phase-one-two/month-1280.png), [tháng còn thiếu](ui-phase-one-two/month-missing-375.png).

## Phần tiếp theo

Luồng tháng gắn hồ sơ đã có contract và mã kiểm chứng. Đ1 còn nghiệm thu UX thủ công theo UI plan; tên hồ sơ/ngày lưu và định danh/đăng ký người phụ thuộc vẫn là mở rộng riêng. Đ3/chặng 4 tiếp theo cần contract báo cáo/checklist và backup có bảo vệ; chưa triển khai trong màn này.

Đã ghi cảnh báo thao tác Bint affected thành tri thức candidate `01M8F671GP7FD4E6G4CVEW6E7R`, origin agent; chưa tự duyệt/publish.
