# Nhập nhanh công cụ tính tháng

28/11/2026, phản hồi ảnh 12 panel nhập lặp lại. Chỉ đổi màn `/` (công cụ demo tính tháng), giữ nghiệp vụ 2025/2026, các snapshot/service, và ba arrays POST gross/insurance/withheld đúng 12 phần tử. Đây là tiện ích nhập liệu, không suy bảo hiểm hoặc thuế đã khấu trừ từ gross.

```text
Năm [2026]
Nhập chung cho nhiều tháng
Gross [       đ]  Bảo hiểm [       đ]  Khấu trừ [       đ]
Từ tháng [1] đến [12]   Xem thay đổi
Thông tin số ô/tháng sẽ đổi   Áp dụng / Hủy

Sửa từng tháng
Tháng 1 ... Tháng 12  (6 cột desktop, 3 mobile; chữ thiếu/đủ ô nhập)
Tháng đang chọn: Gross / Bảo hiểm / Khấu trừ

                          [Tính 12 tháng]
```

Giữ xanh/Inter/token hiện tại, nền xám và hai panel nhập trắng; không 12 card, viền chia hoặc dropdown tháng. Nhóm chung để trống ban đầu, trường chung bỏ trống nghĩa là giữ nguyên từng tháng, số 0 phải nhập rõ. Xem thay đổi trước khi áp dụng, thông báo số ô có sẵn sẽ thay thế; không áp dụng bằng Enter. Có hoàn tác lần áp dụng gần nhất, không ghi đè ô đã sửa tay sau đó. Thay thông số chung phải xem lại trước khi áp dụng.

Chọn tháng chỉ đổi phần hiện trong cùng form, không gửi hoặc mất dữ liệu. Lưới thể hiện số trường đã nhập (không tuyên bố dữ liệu đúng); tháng thiếu khác ô có 0. Giữ 36 inputs đúng thứ tự và enabled để POST toàn bộ, định dạng vi-VN xử lý cả các tháng đang ẩn [k:01M8F49W9Q99372V01ZZBZV6NK]. JavaScript tắt giữ 12 nhóm nhập nền hiện có. Trường có lỗi được mở trước focus, lỗi POST tháng N mở N. Không giấu input invalid làm submit không hoạt động.

Một primary “Tính 12 tháng” đứng đầu thứ tự submit; bulk/đổi tháng/hoàn tác đều type button. Không thay route vào forecast, sample hoặc các kết quả/căn cứ. Kiểm thao tác bàn phím, reflow 320/375/1440, input chính xác decimal/blank/0, apply/undo/confirm, thay đổi riêng không bị mất và POST vẫn đúng arrays.
