# Hai kịch bản từ hồ sơ đã lưu — chặng 5

Phạm vi local synthetic năm 2026. Tái dùng `AnnualForecastCalculator.CalculateScenarios`; không thêm luật, schema, lịch sử hoặc ghi ngược giả định vào hồ sơ.

## Contract

- Route `/ho-so/{id}/kich-ban`. GET chọn revision hiện tại; POST bắt buộc revision đã mở, phát hiện xung đột 409 nếu hồ sơ đổi. Chỉ nhận tháng 1–12 và tiền không âm; trống khác 0. Dùng ForecastAmountBinder/MoneyInput, giữ chuỗi và lỗi khi POST sai.
- Đọc document, snapshot cùng revision và catalog trong một transaction nhất quán; xác thực protection ngay cả khi ID không tồn tại. Không seed/fallback hoặc lấy luật mới nhất để tính kỳ khác. Không nhận input lương, trạng thái, luật hoặc kết quả từ browser.
- Cơ sở giữ toàn bộ gross và thưởng đã lưu, kể cả thưởng đã gồm gross. Kịch bản thêm đúng một khoản thưởng tiền chịu thuế từ nơi trả hiện tại, ngoài gross, trạng thái Giả định. Nguồn/lý do mô tả phép thử do người dùng nhập; không giả là dữ liệu thực tế.
- GET chỉ hiển thị cơ sở và mời nhập khoản thử, không bịa tháng/tiền. POST 0 hợp lệ, kết quả bằng nhau ghi rõ không đổi; không tạo min/max hoặc khoảng xác suất.
- Thiếu gross/bảo hiểm/người phụ thuộc thì liệt kê chỉ tiêu và tháng, dẫn về sửa hồ sơ. Thiếu khấu trừ vẫn tính nghĩa vụ khi đủ các đầu vào khác. So sánh chỉ nghĩa vụ thuế, không suy khấu trừ cho thưởng hoặc số hoàn/nộp thêm của kịch bản.
- Không lưu kết quả so sánh, không thêm revision/audit/lịch sử. Refresh/replay chỉ tính lại; form cũ sau đổi kho bị antiforgery hiện có chặn. no-store cả thành công và lỗi.
- Hiện revision, thời điểm tính, độ phủ thực tế riêng từng kịch bản, khấu trừ cơ sở riêng, nguồn/phiên bản luật và trạng thái Unverified. Metadata hồ sơ và lịch sử cũ giữ nguyên.

## Bố cục trước triển khai

Giữ Inter, xanh #395B40, nền #F3F6F3, panel #FFFFFF, chữ #1A1F1B, chữ phụ #526057 qua token có sẵn. Sidebar hiện có giữ nguyên số mục; truy cập từ Tổng quan năm để không thêm mục điều hướng lặp.

```text
Trở lại hồ sơ
Hai kịch bản năm 2026
Phiên bản n, chỉ dùng dữ liệu đã lưu

Thử thêm thưởng                         [panel trắng]
Tháng [  ]   Số tiền [                 đ]
Giả định, cộng ngoài gross; hướng dẫn định dạng
                                      [So sánh]

Nghĩa vụ năm                            [hai cột, mobile xếp dọc]
Cơ sở                   Thêm thưởng giả định
... đ                   ... đ
x/12 tháng thực tế      y/12 tháng thực tế
Thuế tăng thêm ... đ / Nghĩa vụ không đổi

Bước tính so sánh                       [bảng, cuộn ngang khi hẹp]
Căn cứ và phạm vi                       [một disclosure]
```

Căn trái nhãn, căn phải tiền tabular trong ô và bảng; số lớn trong kết quả được wrap khi cần. Một nút đặc So sánh; liên kết trở lại là chữ. Không thanh trượt, mũi tên trang trí, biểu đồ hoặc khung quanh từng chỉ số. So với mặc định hai card/dashboard: dùng chung một vùng kết quả và một bảng để các chỉ tiêu cùng đường chuẩn.

## Kiểm chứng dự kiến

HTTP thật với host synthetic: giá trị thuế độc lập đã biết, thưởng đã gồm gross không cộng hai lần, thưởng cơ sở giữ nguyên, không ghi thêm document/snapshot/audit/history; zero/empty, thiếu đầu vào/khấu trừ, sai tiền/tháng, CSRF, revision cũ, xóa, khóa/catalog hỏng. Browser: tiền khi gõ, lỗi giữ input, desktop/mobile, một primary, thứ tự focus và căn lề. Không dùng QA tự động để đóng nghiệm thu screenreader/zoom native còn mở.
