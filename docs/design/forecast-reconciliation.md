# Đối chiếu nguồn — chặng 6

Phạm vi bản đầu: local synthetic, một nơi trả lương, hồ sơ 2026 đã lưu. Nhập thủ công một số nguồn theo kỳ; phép đối chiếu không lưu, không cập nhật hồ sơ, lịch sử, audit hoặc catalog. Không nhận file hay tài khoản eTax.

## Contract

- `/ho-so/{id}/doi-chieu`, mở từ Tổng quan năm hoặc sidebar. GET đọc revision hiện tại; POST bắt buộc revision đã mở. Đổi revision trả 409, xóa trả 404, khóa/sentinel/payload lỗi trả 503. Đọc document/snapshot trong một transaction, ValidateProtection trước cả lookup rỗng.
- Nguồn Công ty/eTax/Khác kèm tên tài liệu hoặc mô tả nguồn, trạng thái Thực tế/Dự kiến/Giả định, ghi chú, thời điểm nhập do server ghi nhận. Giả định cần ghi chú. Nguồn không tự quyết định trạng thái hay mức xác minh.
- Chỉ tiêu tường minh: gross đã nhập (gồm thưởng đã nằm trong gross, không cộng thưởng ngoài gross), bảo hiểm nhập tay, thuế đã khấu trừ. Không so thuế ứng dụng tính với khấu trừ, gross với thu nhập tính thuế; chưa hỗ trợ chỉ tiêu khác hoặc nhập số thu nhập tổng không xác định nghĩa.
- Năm nguồn và tháng đầu/cuối bắt buộc; một tháng khi đầu = cuối, cả năm khi 1–12. Hồ sơ chỉ có 2026; chọn năm khác hiện chưa tương ứng và không tính chênh lệch. Mỗi phép so dùng đúng chỉ tiêu và đúng khoảng tháng của cả hai số.
- Số tiền không âm nullable, ô trống báo thiếu nguồn, 0 là đã nhập. ForecastAmountBinder và marker vi-VN hiện có; giữ nguyên input/lỗi khi POST sai. Không JS Number hoặc parser culture phân nhóm.
- Nếu thiếu tháng thuộc chỉ tiêu đã chọn, không dùng tổng một phần làm tổng kỳ; hiện các tháng thiếu, link sửa và không có delta. Thiếu chỉ tiêu khác không chặn phép so này. Tổng đủ kỳ giữ cơ cấu trạng thái và provenance từng tháng.
- Chênh lệch = số nguồn − số hồ sơ. Dương ghi + và cao hơn, âm ghi − và thấp hơn, 0 ghi bằng nhau. Không quy kết nguồn sai hoặc coi chênh lệch là tiền hoàn/nộp thêm. Tính decimal checked; overflow từ chối, không hiện số cắt cụt.
- Không persist số đối chiếu; metadata nguồn/ghi chú/thời điểm hiện trong kết quả POST và giữ trong form. Đây là phép thử theo yêu cầu chặng 6, chưa là kho nhiều nguồn hay báo cáo đối chiếu đã lưu. POST no-store, antiforgery hiện có, không query string chứa tiền/ghi chú. Kết quả cũ được đánh dấu khi sửa form.

## Thiết kế trước code

Hallmark app screen + frontend-design + ui-ux-pro-max. Giữ brand #395B40, nền #F3F6F3, panel #FFFFFF, chữ #1A1F1B, chữ phụ #526057 từ tokens.css, Inter tự host. Không đổi palette; hướng bảng đối chiếu dữ liệu cụ thể thay dashboard số lớn/card từng chỉ tiêu.

```text
Trở lại hồ sơ
Đối chiếu nguồn
Phiên bản n, phép đối chiếu không lưu vào hồ sơ

Số từ nguồn ngoài                        [panel trắng]
Nguồn       ( ) Công ty ( ) eTax ( ) Khác
Tài liệu/nguồn [                         ]
Năm [2026]  Từ tháng [ ] Đến tháng [ ]
Chỉ tiêu    [native select: 3 nghĩa cụ thể]
Số tiền [                             đ]
Trạng thái  ( ) Thực tế ( ) Dự kiến ( ) Giả định
Ghi chú [                               ]
                              [Đối chiếu số liệu]

Kết quả đối chiếu
Chỉ tiêu và kỳ; trạng thái từng nguồn
                   Hồ sơ | Nguồn nhập | Nguồn − hồ sơ
Số tiền                … |         … | +… cao hơn
Thiếu hoặc khác năm: chưa so được, không dùng 0
Chi tiết nguồn hồ sơ theo tháng          [một disclosure]
```

Một nút đặc, ô tiền căn phải tabular có đ và hướng dẫn sát ô; năm/tháng hẹp, giữ cùng đường chuẩn khi nhãn xuống dòng. Nhãn trái, số phải; desktop đặt ba số trong bảng ngang, mobile xếp Hồ sơ/Nguồn nhập/Chênh lệch thành ba hàng để không phải cuộn tìm kết quả chính. Bảng provenance chi tiết cuộn riêng khi hẹp. Chỉ một native select cho chỉ tiêu để không kéo dài ba mô tả radio; nguồn/trạng thái dùng radio, không thêm chevron trang trí. Sidebar giữ thứ tự cũ, thêm Đối chiếu sau Báo cáo khi có hồ sơ; editor không thêm section giả cho route riêng.

Tìm design-system lần đầu trả marketing Hero + Features + CTA, palette navy và Outfit/Work Sans không phù hợp hệ thống đang khóa; không áp dụng/persist. Truy vấn UX riêng `error summary validation` phù hợp lỗi gần ô, giữ input, focus summary; `data table comparison` phù hợp vùng cuộn bảng (bỏ gợi ý bulk actions không thuộc màn này).

## Kiểm chứng

HTTP với DB synthetic: tổng theo kỳ/chỉ tiêu, dấu +/-/0, thiếu khác 0, metadata/status độc lập nguồn, khác năm, validation, payload/snapshot/audit/history bất biến, revision/CSRF/xóa, protection/payload lỗi. Browser: gõ tiền, đổi selection báo kết quả cũ, lỗi/focus, desktop/mobile, căn hàng và một primary. Không đóng nghiệm thu screenreader, native zoom hoặc UX thực tế chỉ từ axe.
