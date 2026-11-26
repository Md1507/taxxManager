# ADR 0018 — Dự báo thuế năm cho dự án cá nhân

Trạng thái: định hướng sản phẩm được người dùng chốt ngày 25/11/2026; ba lựa chọn về kết quả ban đầu, phạm vi và kịch bản đã được xác nhận trong phiên grill-with-docs; chưa triển khai.

## Mục tiêu và đánh đổi

Đây là dự án cá nhân, miễn phí, một người phát triển bằng .NET. Không yêu cầu chứng minh lợi thế thị trường hoặc có người dùng thử trước khi tiếp tục; hiện chưa có người thử và chưa có bằng chứng xác nhận nhu cầu thị trường. Không áp thời hạn hai tuần. Ưu tiên hoàn thiện tác vụ trong phạm vi rõ ràng thay vì cố có nhiều chức năng hoặc khác mọi ứng dụng hiện có.

Dự báo thuế cả năm là tác vụ chính; đối chiếu nguồn là bổ trợ. Tận dụng nghiệp vụ năm ở bước 1.4, nhưng không coi Core đã có phép tính là Web/Data đã tích hợp hoàn chỉnh. Luồng hướng tới: nhập hồ sơ lương thuận tiện, xem các kịch bản năm, cập nhật bằng số liệu thực tế và giải thích kết quả thay đổi. Minh bạch căn cứ hỗ trợ niềm tin; không phải lời hứa xác nhận công ty hoặc eTax đúng/sai.

## Dữ liệu và cách thể hiện

- Phân biệt từng đầu vào: thực tế từ nguồn về khoản đã phát sinh, dự kiến do người dùng nhập kế hoạch, và giả định do ứng dụng điền. Đây là trạng thái dữ liệu trong mô hình miền, không chỉ nhãn giao diện và không thay thế trạng thái xác minh căn cứ pháp lý. Không lưu giá trị giả định vào lịch sử như khoản thực tế.
- Kết quả phải thể hiện mức đầy đủ dữ liệu, bao gồm số tháng thực tế trong tổng 12 tháng, trạng thái từng nhóm đầu vào, các giả định và phạm vi chưa bao gồm. Không suy toàn bộ tháng là thực tế chỉ vì gross là thực tế; độ phủ khấu trừ phải tách khỏi độ phủ đầu vào xác định nghĩa vụ thuế năm.
- Kết quả đầu tiên với gross, bảo hiểm nhập tay và số người phụ thuộc là nghĩa vụ thuế năm ước tính. Chỉ hiển thị chênh lệch nộp thêm/nộp thừa khi có dữ liệu hoặc giả định khấu trừ riêng đủ cho kịch bản; không đổi thiếu khấu trừ thành 0, không gọi nộp thừa ước tính là tiền chắc chắn được hoàn.
- Hai kịch bản ban đầu là cơ sở và thêm thưởng tiền chịu thuế trong phạm vi engine hỗ trợ. Kịch bản cơ sở giữ các khoản đã phát sinh; lựa chọn không có thưởng thêm không xóa thưởng thực tế. Khoản thêm trong kịch bản không được cộng lần nữa nếu đã nằm trong gross.
- Hiển thị kết quả từng kịch bản và khoảng giữa chúng; khoảng này không phải khoảng tin cậy, biên bảo đảm hay xác suất. Các điều chỉnh giới hạn khoảng 2–3 điều khiển; khi hai kết quả bằng nhau vẫn hiển thị trung thực hai kịch bản có cùng kết quả.
- Ưu tiên giảm nhập lặp bằng giai đoạn lương và tháng ngoại lệ, có xem trước dữ liệu tháng. Bảo toàn phân biệt thực tế/dự kiến/giả định khi khai triển; không tự suy số khấu trừ thực tế từ thuế tính toán.
- Hồ sơ ban đầu lưu số liệu kèm kỳ, loại khoản/chỉ tiêu, số tiền, trạng thái, nguồn, ghi chú và ngày nhập; chưa lưu tài liệu gốc. Phân biệt kỳ được mô tả với ngày nhập và thời điểm nguồn được ghi nhận khi thiết kế đối chiếu.
- Không có dữ liệu không mặc nhiên bằng 0. Chỉ khi có một giả định thay thế được trình bày rõ mới dùng giá trị đó và gắn trạng thái giả định; nếu chưa có thì báo phần kết quả chưa đủ đầu vào. Các mặc định cụ thể phải được b-req liệt kê để người dùng duyệt cùng kế hoạch, không suy số liệu pháp lý từ trí nhớ.

## Phạm vi và quan hệ với nền hiện tại

Phạm vi bản đầu đã chốt là cá nhân cư trú làm công ăn lương, một nơi trả lương, kỳ 2026, bảo hiểm nhập tay và thưởng tiền chịu thuế từ nơi trả lương hiện tại theo nhánh engine đã hỗ trợ. Không gồm hai nơi trả, freelance/kinh doanh, thu nhập nước ngoài hoặc loại thu nhập khác chưa hỗ trợ. Không suy các nhánh Core tự được hỗ trợ bởi Web/Data. Giao diện phải nêu giới hạn, các phần chưa bao gồm và kết quả chỉ tham khảo, không thay thế tư vấn thuế.

Giữ nền Core/Data đã có và lịch sử bất biến; vị trí các kiểu miền dự báo, adapter sang phép tính năm, persistence mới và migration cần được thiết kế riêng. Dữ liệu theo tháng vẫn có thể là đầu vào phép tính dù giao diện nhập theo giai đoạn; quyết định này không bắt buộc thay DB hiện tại bằng lưu sự kiện. Không ghi nhận thay đổi của dự báo bằng cách sửa snapshot thực tế cũ.

Cơ chế cập nhật pháp luật bằng AI và UI so sánh các lần tính vẫn là giai đoạn riêng theo [ADR 0017](0017-ai-tax-rule-updates.md). Chưa có kiến trúc local-first, quyền triển khai công khai hoặc phê duyệt dữ liệu thật từ quyết định sản phẩm này.

## Bàn giao cho lập kế hoạch

Yêu cầu và tiêu chí nghiệm thu tại [đặc tả dự báo thuế năm](../design/annual-tax-forecast.md). b-req chọn cấu trúc kiểu miền dự báo trong Core, adapter phép tính năm, persistence/migration, route Web và cách kiểm thử cụ thể; không mở rộng phạm vi sản phẩm hoặc tự thay đổi nghĩa các trạng thái đã chốt. Giai đoạn 3.1b bắt đầu bằng mô hình trạng thái và nhập liệu, sau đó tích hợp dữ liệu và dự báo năm; mỗi phần phải có kiểm chứng trước khi đi tiếp. PLAN.md hiện chưa có bước 3.1b: tác vụ triển khai phải cập nhật lộ trình với các phụ thuộc trên, giữ nguyên bằng chứng hoàn tất 3.1 cũ và tránh mô tả 3.3 như đã được hoàn tất chỉ bởi tài liệu này.
