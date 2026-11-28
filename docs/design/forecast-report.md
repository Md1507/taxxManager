# Báo cáo hồ sơ và checklist dữ liệu, bản đầu

Ngày 28/11/2026. Phần đầu Đ3/chặng 4, sau chặng 3; chỉ Development/SyntheticDemo. Tái dùng dữ liệu và kết quả đã lưu, không mở thêm nhánh tính thuế.

## Hành vi

- Một màn `/ho-so/{id}/bao-cao`, một nút chính Xuất CSV. Bố cục: tên báo cáo và phiên bản; checklist dữ liệu; bảng 12 tháng; thưởng; nguồn/ghi chú; kết quả năm đã lưu nếu có. Giữ palette/token và sidebar hiện có, bảng có vùng cuộn riêng.
- Chỉ đọc hồ sơ còn tồn tại. Có thể đọc revision đã lưu cụ thể; không dùng dữ liệu form chưa lưu. ID rỗng hoặc query không hợp lệ trả 400; route không khớp, hồ sơ/revision/kết quả không tồn tại trả 404; dữ liệu/khóa lỗi trả 503; không tạo kho hay khóa thay thế.
- Đọc hồ sơ, snapshot revision và lịch sử năm trong một transaction nhất quán, qua service kiểm protection hiện có. Không ghi hồ sơ, audit hoặc lịch sử khi mở/xuất báo cáo.
- Kết quả năm chỉ lấy từ bản tính đã lưu có cùng revision và payload, không cộng thuế tháng hoặc tự tính khi GET. Không có bản tính phù hợp thì ghi chưa có kết quả và dẫn tới tính từ hồ sơ hiện tại.
- Link xuất ghim revision và ID bản tính; dùng ID rỗng để ghim trạng thái chưa có kết quả. Một bản tính mới hoặc cập nhật hồ sơ sau khi mở trang không làm CSV đổi sang dữ liệu/kết quả khác.
- Checklist chỉ kiểm dữ liệu: thiếu gross/bảo hiểm/phụ thuộc/khấu trừ, giá trị còn dự kiến/giả định, nguồn còn thiếu và việc kiểm thưởng đã gồm gross. Không khẳng định đủ điều kiện quyết toán hoặc thay checklist pháp lý chính thức.

## CSV

- GET cùng route với `handler=Csv`, attachment UTF-8 có BOM, phân tách bằng dấu phẩy, CRLF; quote mọi ô và escape dấu quote. Số decimal invariant, không phân nhóm; ô chưa nhập để trống và nhãn Chưa nhập, số 0 vẫn là 0.
- Cột năm/revision/kỳ/chỉ tiêu/giá trị/đơn vị/trạng thái/nguồn/ghi chú/ngày nhập UTC/đã gồm gross. Giữ metadata từng giá trị và thưởng. Bản tính năm có thì xuất kết quả, thời điểm tính, phiên bản luật/kỳ và căn cứ đã lưu, không ghép luật mới.
- Ô chữ có thể bị Excel hiểu như công thức (kể cả sau khoảng trắng hoặc ký tự điều khiển) thêm dấu nháy đơn bảo vệ; không áp dụng cho giá trị số do hệ thống tạo. Giữ dấu phẩy, quote và xuống dòng trong metadata bằng quoting CSV.
- Header `Cache-Control: no-store` cho HTML và CSV. Không bật CORS hoặc gửi dữ liệu ra ngoài. CSV là báo cáo tham khảo có dữ liệu đọc được, không chứa khóa và không phải bản sao lưu hay tờ khai chính thức.

## Kiểm chứng

- Thiếu khác 0, nhãn/trạng thái và metadata nhiều dòng/quote/chuỗi công thức giữ đúng nội dung an toàn.
- Cập nhật hồ sơ rồi xuất link cũ vẫn đúng revision cũ; ghim bản tính hoặc trạng thái chưa tính không bị đổi. Đọc/xuất không thêm lịch sử hay sửa hồ sơ.
- Bản tính năm và luật xuất đúng bản lưu; lỗi ID/revision/record, hồ sơ đã xóa và protection fail đóng an toàn.
- Browser desktop/mobile: một nút chính, không tràn ngang trang, link sidebar đúng màn; thứ tự dữ liệu → phụ thuộc → tổng quan trong editor liền mạch, không quay ngược mục.

Sao lưu/khôi phục chưa triển khai ở phần này. Cần contract bảo vệ bản sao, xem trước nhập và giải quyết trùng riêng trước khi bật.
