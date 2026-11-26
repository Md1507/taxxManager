# Đặc tả dự báo thuế năm và nhập liệu theo giai đoạn

Ngày 25/11/2026. Bản bàn giao từ grill-with-docs sau khi người dùng đồng ý các lựa chọn sản phẩm; chưa triển khai. Theo [ADR 0018](../decisions/0018-personal-annual-tax-forecast.md) và [GLOSSARY](../../GLOSSARY.md). Không đặt thời hạn hai tuần; chưa có người thử không chặn công việc kỹ thuật và không được mô tả là đã xác nhận nhu cầu thị trường.

Phạm vi thực hiện 3.1b được chốt sau đó: chỉ nhập giai đoạn, UI và trạng thái dữ liệu. Đặc tả dưới đây giữ định hướng đầy đủ; dự báo năm, kịch bản, lưu/mở lại, migration và audit forecast thuộc bước sau, chưa được nghiệm thu ở 3.1b.

## Kết quả mong muốn

Người dùng nhập mức lương theo giai đoạn và tháng ngoại lệ, ghi nhận bảo hiểm nhập tay và người phụ thuộc, rồi thấy ước tính thuế năm 2026 theo kịch bản cơ sở và thêm thưởng tiền chịu thuế. Hồ sơ phân biệt số thực tế, dự kiến và giả định; khi cập nhật thực tế, kết quả mới được giải thích mà không sửa lịch sử cũ. Các số từ công ty/eTax có thể được ghi nguồn từ đầu; tự động đối chiếu hai nguồn là giai đoạn sau.

Đây vẫn là bản thử local Development với dữ liệu synthetic trên nền Web 3.1, không tự mở truy cập công khai, bỏ các guard demo hoặc chuyển sang dữ liệu cá nhân thật.

## Phạm vi chức năng

- Cá nhân cư trú, một nơi trả lương, tiền lương thông thường bằng VND và kỳ 2026; bảo hiểm nhập tay. Thưởng chỉ gồm nhánh tiền chịu thuế từ đơn vị trả lương hiện tại mà Core đã hỗ trợ; không suy tính chịu thuế từ nhãn Tết/KPI/tháng 13.
- Bước đầu nhận gross thường kỳ, bảo hiểm và số người phụ thuộc. Mức áp dụng cả năm/giai đoạn và người phụ thuộc theo tháng phải được thể hiện, không ngầm coi số hiện tại là thực tế của cả năm.
- Dùng đầu vào theo khoảng tháng, xem trước 12 tháng và sửa tháng ngoại lệ. Áp dụng lại giai đoạn phải hiển thị tác động và không tự ghi đè tháng đã sửa; bảo hiểm và khấu trừ có luồng nhập riêng.
- Từng giá trị miền có kỳ, loại khoản/chỉ tiêu, trạng thái, nguồn, ghi chú và thời điểm nhập; số người phụ thuộc cũng phải có trạng thái dù không phải khoản tiền. Trạng thái nguồn dữ liệu không thay thế VerificationStatus hoặc Official/References của căn cứ luật.
- Trạng thái thực tế là ghi nhận của người dùng từ nguồn về khoản đã phát sinh, không phải kết luận được xác minh pháp lý. Giá trị app tự ngoại suy từ số hiện tại sang tháng khác mà người dùng chưa chủ động khai kế hoạch, hoặc tự lấy mặc định, phải có trạng thái giả định; lịch người dùng chủ động nhập cho tương lai là dự kiến, kể cả khi ứng dụng khai triển lịch đó thành nhiều tháng. Không dùng một nhãn chung cho toàn năm để che trạng thái từng giá trị.
- Thiếu đầu vào và giá trị 0 là hai trường hợp khác nhau. b-req phải liệt kê mọi mặc định, cách trình bày và điều kiện được tính; không được tự nhận dữ liệu giả định là đã thực nhận. Không tự tạo giả định khấu trừ bằng 0 khi chưa biết.
- Hồ sơ ban đầu chỉ lưu số liệu và metadata; không upload/giữ file phiếu lương, giấy tờ hoặc mật khẩu/tài khoản eTax. Nguồn không tự chứng minh nội dung đúng; ghi chú cũng là dữ liệu cá nhân cần bảo vệ khi lưu.

## Kết quả và kịch bản

1. Có đủ đầu vào xác định thuế cho 12 tháng thì trả nghĩa vụ thuế năm ước tính bằng engine năm; không lấy tổng thuế tháng để thay thế.
2. Khi thiếu khấu trừ, vẫn có thể trả nghĩa vụ năm nếu đủ dữ liệu cho phép tính đó, nhưng ẩn số nộp thêm/nộp thừa và ghi rõ thiếu phần khấu trừ. Chỉ trả chênh lệch khi khấu trừ có dữ liệu hoặc giả định riêng đủ cho toàn kịch bản; phân biệt phần thực tế với phần dự báo, không hứa được hoàn tiền.
3. Kịch bản cơ sở giữ toàn bộ khoản đã ghi nhận và dự kiến dùng làm nền, không giả định thêm thưởng chưa khai; kịch bản thứ hai thêm một khoản thưởng giả định có số tiền và tháng nhận dự kiến riêng. Không tự xóa thưởng thực tế, sửa hồ sơ gốc hay cộng trùng khoản đã nằm trong gross. Có tối đa khoảng 2–3 điều khiển kịch bản; mặc định không có số thưởng do app tự bịa.
4. Khi chưa nhập khoản thưởng bổ sung, không tạo khoảng giả: có thể thể hiện hai kịch bản trùng nhau hoặc yêu cầu bổ sung để tạo kịch bản thứ hai. Mỗi kịch bản có kết quả riêng; min/max chỉ là khoảng giữa các kịch bản đang xét, không là khoảng bảo đảm, xác suất hoặc khoảng tin cậy.
5. Hiển thị X/12 tháng có đầy đủ dữ liệu thực tế để tính nghĩa vụ thuế, các nhóm còn dự kiến/giả định và phạm vi loại trừ. Không tăng bộ đếm chỉ vì đã có gross thực tế trong khi dữ liệu còn lại của tháng vẫn giả định; khấu trừ có độ phủ riêng. Xác nhận không có khoản phát sinh phải được phân biệt với chưa nhập.
6. Luôn ghi phạm vi hỗ trợ và cảnh báo tham khảo, trạng thái/căn cứ bộ quy tắc thực dùng; không nâng Unverified thành Verified hoặc bỏ gate chỉ để có kết quả. Kỳ/rule không hỗ trợ phải báo rõ, không fallback.

## Tiêu chí nghiệm thu

| Mã | Hành vi quan sát được |
|---|---|
| AC1 | Một mức gross áp dụng cả năm chỉ cần nhập một lần; thêm mốc thay đổi cho ra đúng các tháng, tháng ngoại lệ không bị ghi đè âm thầm. |
| AC2 | Core có mô hình biểu diễn trạng thái và nguồn đầu vào dự báo độc lập Web/DB; đưa cùng dữ liệu qua lớp UI khác không làm mất sự phân biệt thực tế/dự kiến/giả định. |
| AC3 | Thiếu bảo hiểm, người phụ thuộc hoặc khấu trừ không tự thành số 0; mọi giá trị được điền thay đều hiện là giả định cùng lý do. |
| AC4 | Với đầu vào đầy đủ cho nghĩa vụ thuế năm và chưa có khấu trừ, trang hiển thị nghĩa vụ năm và thông báo thiếu khấu trừ, không hiển thị số tiền hoàn/nộp thêm sai nghĩa. |
| AC5 | Hai kịch bản dùng đúng engine năm, giữ thưởng đã phát sinh, chỉ thêm khoản thưởng riêng và không ghi ngược giả định vào hồ sơ gốc; các kết quả trùng nhau không tạo khoảng giả. |
| AC6 | Kết quả có nhãn từng trạng thái, X/12 tháng thực tế với định nghĩa được hiển thị và độ phủ khấu trừ riêng; min/max được ghi rõ chỉ giữa các kịch bản. |
| AC7 | Khi đủ dữ liệu/giả định khấu trừ, kết quả tách phần thực tế với dự báo và diễn giải chênh lệch, không gọi nộp thừa ước tính là số tiền chắc chắn được hoàn. |
| AC8 | Lưu và đọc lại hồ sơ dự báo qua context mới giữ chính xác số decimal, kỳ, loại chỉ tiêu, trạng thái, nguồn, ghi chú và ngày nhập; cập nhật thực tế rồi tính mới không sửa snapshot lịch sử cũ. |
| AC9 | Dự báo được lưu bằng contract riêng, không lẫn vào MonthlyIncome hoặc lịch sử hiện tại dưới tên thực tế; dữ liệu cá nhân/ghi chú mới được bảo vệ và audit phù hợp nền 2.3–2.4, migration additive giữ dữ liệu synthetic cũ. |
| AC10 | Form và kết quả hoạt động trên mobile và bàn phím, nêu phạm vi một nơi trả/kỳ 2026, không tự mở thêm nhánh thu nhập, nâng trạng thái pháp lý hoặc bỏ guard demo local. |
| AC11 | Các hồi quy Core/Data/Web hiện có tiếp tục đạt; PLAN và tài liệu vận hành phản ánh phạm vi thực sự đã làm, không đánh dấu hoàn tất chỉ vì có đặc tả. |

## Seam kiểm thử

Seam chính cao nhất: HTTP form tới kết quả render của ứng dụng Web thật, kế thừa cách dựng DemoHost/FormClient và kiểm thử hành vi ở [CalculationBehaviorTests](../../tests/TaxManager.Web.Tests/MonthlySalary/CalculationBehaviorTests.cs). Kiểm được nhập giai đoạn, tháng ngoại lệ, dữ liệu thiếu, hai kịch bản, nhãn/độ phủ và kết quả năm. Thêm ca cùng dữ liệu nhưng tổng thuế tháng khác thuế năm để bắt dùng nhầm đường tính; không chỉ kiểm sự tồn tại của nút hoặc gọi lại đúng hàm implementation làm đáp án.

HTTP không thay thế được kiểm bảo vệ DB hoặc nâng cấp: thêm kiểm thử SQLite synthetic đọc lại ở context mới, key sai, rollback, giữ snapshot cũ và không lộ metadata nhạy cảm/ghi chú. Dùng test Core cho những bất biến miền không quan sát được đầy đủ qua HTTP. QA browser ở bước triển khai cho mobile/bàn phím; không coi kiểm HTTP là bằng chứng bố cục đã đạt. Test không chứng minh tính đúng pháp lý của seed hoặc chất lượng nhu cầu thị trường.

## Thứ tự triển khai và phạm vi sửa đề xuất

Trong cùng phiếu, b-req chia các phần kiểm chứng được theo phụ thuộc: (1) Core về trạng thái/đầu vào và nhập giai đoạn 3.1b; (2) contract lưu dự báo cùng bảo vệ/audit và migration synthetic; (3) tích hợp Web, hai kịch bản và luồng cập nhật hồ sơ. Không refactor mọi calculator chỉ để thêm metadata; tận dụng annual/bonus engine hiện có, giữ nghĩa actual withholding và history hiện tại.

Phạm vi dự kiến: `src/TaxManager.Core/**`, `src/TaxManager.Data/**`, `src/TaxManager.Web/**`, `tests/TaxManager.Core.Tests/**`, `tests/TaxManager.Data.Tests/**`, `tests/TaxManager.Web.Tests/**`, `docs/**`, `GLOSSARY.md`, `PLAN.md`, `scripts/dev/**`. Giữ migration/designer cũ bất biến; thiết kế migration mới và kiểm DB synthetic, không đụng DB/keys vận hành. Không sửa skill/config Bint hoặc Git index/HEAD.

Ngoài phạm vi: nhiều nơi trả lương, freelance/kinh doanh, thu nhập nước ngoài, các loại thu nhập khác, OCR/upload/import CSV, kho tài liệu, đăng nhập công khai, triển khai cloud, local-first, cập nhật luật AI và UI đối chiếu nguồn/so sánh hai bản tính. Nhãn nguồn cho hồ sơ vẫn được ghi ngay để hỗ trợ bước sau.

Lệnh kiểm chứng đề xuất, chỉ chạy ở tác vụ triển khai sau cổng xác nhận của Bint:

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/dev/verify-web-mvp.ps1
```

Script hiện có build toàn solution, chạy Core/Data/Web tests và kiểm EF pending model changes. b-req bổ sung các ca mục tiêu và kế hoạch QA browser; không sửa script để bỏ kiểm thử hoặc che thất bại.
