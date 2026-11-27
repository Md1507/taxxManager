# Lộ trình sản phẩm

Lộ trình này ghi thứ tự sản phẩm mới nhất đã chốt; các chặng tương lai dưới đây chưa được coi là hoàn thành. Nền 3.1 đã nghiệm thu kỹ thuật cho local với dữ liệu giả; không mở lại nghiệm thu đó. Phiếu 3.1b `01M89PFQ3ETQDTCSGPMAN0DEPW` và phiếu chặng 2 `01M8BK6R1EC84DFJ05Z8FH31C2` đều đã done, land và tích hợp vào `master`; chặng 3 đã nghiệm thu kỹ thuật và Bint done ngày 28/11/2026; chặng 4–6 chưa triển khai. Xem [bằng chứng chặng 3](reviews/2026-11-27-annual-stage-three.md).

Tham chiếu: [PLAN](../PLAN.md), [kế hoạch UI/UX](../UI_UX_PLAN.md), [ADR 0018](decisions/0018-personal-annual-tax-forecast.md) và [đặc tả dự báo năm](design/annual-tax-forecast.md). PLAN đã đồng bộ thứ tự và trạng thái thực tế; giữ nguyên bằng chứng nghiệm thu 3.1.

## Phối hợp với kế hoạch UI/UX

Đồng bộ ngày 28/11/2026: roadmap là nguồn tiến độ/phụ thuộc, PLAN giữ điều kiện kỹ thuật, UI_UX_PLAN giữ màn hình/token/hành vi. [Mục 11 UI plan](../UI_UX_PLAN.md#11-các-đợt-phát-hành-ui) ánh xạ Đ1 với nền chặng 1–2, Đ2 với chặng 3, Đ3–Đ5 với chặng 4–6; Đ6 UI dành cho tài khoản/quyền dữ liệu trước phục vụ qua mạng, không phải chặng sản phẩm 6.

Chặng 3 đã có mã, cổng đầy đủ đã xanh; freeze Bint/review lại còn cần hoàn tất theo [bằng chứng](reviews/2026-11-27-annual-stage-three.md). Hiện tiếp tục chuẩn bị tài liệu UI, chưa thay giao diện trong luồng đang nghiệm thu. Sau khi chặng 3 xong, chuẩn hóa UI Đ1 rồi Đ2 trên form thưởng/phụ thuộc, tính năm và lịch sử đã có; không viết lại service hoặc gỡ tính năng để chia đợt. Việc UI hoàn thành không tự đóng chặng nghiệp vụ, và kế hoạch thiết kế không mở quyền deploy/dữ liệu thật.

Hướng giao diện chốt tại mục 4 và 10 UI plan: sidebar dọc 6 đến 8 mục khi đủ chức năng, thanh trên tên hồ sơ/hành động chính, nền sáng, brand `#395B40`, bộ token theo vai trò. Chỉ khung tổng thuế năm dùng nền brand; tiền đầy đủ vi-VN với `đ`, không viết tắt K/M; biểu đồ chỉ khi hỗ trợ so sánh, không thay bảng. Token CSS chưa được triển khai ở lần đối chiếu này. Khi giao từng màn hình, cập nhật trạng thái/liên kết trong cả ba tài liệu nếu phạm vi hoặc tiến độ đổi.

## Phạm vi và nguyên tắc chung

- Bản đầu dành cho cá nhân cư trú, một nơi trả lương, kỳ 2026, bảo hiểm nhập tay; dự án miễn phí, .NET, một người làm. Không đặt hạn hai tuần; hiện chưa có người dùng thử và chưa có bằng chứng xác nhận nhu cầu thị trường.
- Mỗi giá trị phân biệt thực tế, dự kiến và giả định; trạng thái dữ liệu này độc lập với trạng thái xác minh căn cứ luật. Thiếu dữ liệu khác số 0. Khi đủ đầu vào có thể tính nghĩa vụ năm, nhưng thiếu khấu trừ thì không hiện số hoàn hoặc nộp thêm giả; không hứa khoản nộp thừa ước tính chắc chắn được hoàn.
- Lưu số liệu cùng kỳ, loại chỉ tiêu, trạng thái, nguồn, ghi chú và thời điểm nhập; chưa lưu file gốc. Kết quả chỉ tham khảo, nêu phạm vi, độ đầy đủ và căn cứ/trạng thái luật được dùng; không tự nâng trạng thái xác minh hoặc dùng luật sai kỳ.
- Thử local một người bằng dữ liệu synthetic chưa cần đăng nhập. Trước khi phục vụ nhiều người qua mạng phải có đăng nhập và cách ly tài khoản; lộ trình này không cấp quyền triển khai công khai hay dùng dữ liệu thật.

## Đối chiếu chức năng với lộ trình

Mười hai chức năng dưới đây là phạm vi đích; sáu chặng là thứ tự giao theo phụ thuộc. Gom chức năng vào cùng một chặng không loại bỏ hành vi nào. Trạng thái trong bảng là phạm vi và việc còn lại, không xác nhận các chặng tương lai đã hoàn thành.

| Chức năng cốt lõi | Hành vi tối thiểu | Chặng phụ trách | Trạng thái / phần còn hoãn |
|---|---|---|---|
| 1. Hồ sơ theo năm | Chọn năm thuế, phạm vi và phiên bản luật áp dụng; phân biệt năm thuế với ngày nhập. | 2, dùng tại 3 | Bản đầu chỉ bật 2026 cho phạm vi đã chốt; không ngầm hỗ trợ năm khác. |
| 2. Lương theo giai đoạn | Nhập mức nền một lần, thêm mốc thay đổi, chọn tháng ngoại lệ, xem trước và bỏ giai đoạn thêm nhầm; không âm thầm ghi đè ngoại lệ. | 1 | 3.1b đã done và land theo tiêu chí riêng. |
| 3. Thưởng và khoản bất thường | Ghi tháng, số tiền và khoản đã nằm trong gross hay chưa để tránh cộng trùng; chỉ xử lý loại có căn cứ và engine hỗ trợ. | 3: thực tế/dự kiến; 5: thưởng giả định kịch bản | Bản đầu chỉ gồm thưởng tiền chịu thuế từ nơi trả lương hiện tại theo nhánh engine hỗ trợ; không mở rộng 3.1b. |
| 4. Người phụ thuộc theo thời gian | Ghi tháng bắt đầu/kết thúc và nguồn; phân biệt tự khai với xác nhận đăng ký. | 3 | Đã có số lượng theo giai đoạn và ngoại lệ tháng, chờ nghiệm thu chặng 3; hồ sơ từng người/thông tin đăng ký chưa được coi là đã có. |
| 5. Bảo hiểm và giảm trừ | Nhập tay từ phiếu lương các khoản engine hỗ trợ; phân biệt thiếu với 0. | 1: nền nhập theo scope hiện tại; 3: tích hợp | Tự tính bảo hiểm để sau khi có đủ xác minh; không suy thêm khoản engine chưa hỗ trợ. |
| 6. Khấu trừ từng tháng | Ghi số công ty đã khấu trừ riêng với thuế ứng dụng tính; dùng dữ liệu đã lưu khi tổng hợp. | 2: lưu; 3: sử dụng | Không tự suy thuế đã khấu trừ từ kết quả tính của ứng dụng. |
| 7. Tính thuế và giải thích | Trình bày các bước tính, khoản đã đưa vào và chưa hỗ trợ cùng căn cứ đúng kỳ. | 3 | Đã có Web tính năm và giải thích/bậc thuế/căn cứ, chờ nghiệm thu chặng 3; giữ trạng thái xác minh luật. |
| 8. Tổng hợp và dự báo năm | Tính nghĩa vụ năm ước tính và trình bày chênh lệch chỉ khi đủ dữ liệu hoặc giả định khấu trừ rõ ràng cho cả kịch bản. | 3 | Đã tích hợp tính năm, chờ kiểm chứng cuối; nộp thừa ước tính không phải tiền chắc chắn được hoàn. |
| 9. Trạng thái và độ đầy đủ | Phân biệt thực tế, dự kiến, giả định; nêu tháng và chỉ tiêu còn thiếu, độ phủ khấu trừ riêng và trạng thái xác minh pháp lý riêng. | 1: nền; 2: lưu; 3: độ phủ năm | Nền theo scope 3.1b; không coi một giá trị thực tế là toàn tháng hay toàn năm đầy đủ. |
| 10. Lưu, mở và lịch sử | Mở lại hồ sơ, cập nhật từng tháng; giữ bất biến snapshot, đầu vào và phiên bản luật của mỗi lần tính. | 2–3 | Lưu/mở hồ sơ đã nghiệm thu ở chặng 2; service và lịch sử năm riêng đã có mã ở chặng 3, đang chờ nghiệm thu. |
| 11. Xuất và khôi phục | Xuất báo cáo dùng để tự giữ hoặc gửi HR; sao lưu và khôi phục giữ metadata, phiên bản và lịch sử. Kiểm tra bản nhập trước khi ghi, không âm thầm ghi đè. | 4 | Báo cáo xuất không tự là bản sao lưu có thể khôi phục; PDF trình bày đẹp để sau. |
| 12. Quyền riêng tư | Thu tối thiểu, không giữ file gốc hay tài khoản eTax; xóa có chủ đích khi lưu trữ, bảo vệ bản sao, có đăng nhập và cách ly trước khi phục vụ nhiều người qua mạng. | Xuyên suốt 1–6; xóa ở 2, bản sao ở 4 | Hiện chỉ local với dữ liệu giả; không tự cấp quyền dùng dữ liệu thật hay deploy. |

| Tiện ích | Hành vi tối thiểu | Chặng / trạng thái |
|---|---|---|
| Checklist | Chỉ rõ thông tin chuẩn bị quyết toán đã có và còn thiếu. | 4 |
| Hai kịch bản | So sánh cơ sở với thêm một khoản thưởng giả định trong phạm vi hỗ trợ. | 5 |
| Đối chiếu | Đối chiếu hồ sơ với số người dùng cung cấp theo nguồn, chỉ tiêu và kỳ. | 6 |
| Dán Excel | Cho xem trước dữ liệu được dán trước khi áp dụng. | Backlog |
| Sao chép năm mới | Yêu cầu xác nhận lại giá trị và luật áp dụng cho năm mới. | Backlog |
| So sánh hai lần dự báo | So sánh các lần dự báo đã lưu; khác với hai kịch bản trong cùng một lần dự báo. | Backlog |
| Gross–Net | Tiện ích tính riêng cho Gross–Net. | Backlog |

Backlog là các ý tưởng chưa cam kết triển khai; mỗi mục cần quyết định và phạm vi riêng.

Quyền riêng tư áp dụng xuyên suốt cả sáu chặng: chỉ thu dữ liệu cần thiết, không thu file gốc hay tài khoản eTax; khi có lưu trữ phải có cách xóa có chủ đích, và bản sao ở chặng 4 cũng cần được bảo vệ. Xóa hồ sơ, lịch sử và bản sao cần được diễn giải rõ phạm vi, không hứa xóa khỏi mọi bản sao một cách tự động. Đăng nhập và cách ly là điều kiện trước khi phục vụ nhiều người qua mạng, không phải cho phép dùng dữ liệu thật hay triển khai công khai lúc này.

## Sáu chặng theo thứ tự

### 1. Hoàn tất 3.1b: nhập liệu, UX và trạng thái

- **Bắt đầu:** phiếu `01M89PFQ3ETQDTCSGPMAN0DEPW` đã done và land trên nền 3.1.
- **Tối thiểu:** hoàn tất mô hình trạng thái đầu vào, nhập theo giai đoạn và tháng ngoại lệ, cùng UX thuộc phiếu; phân biệt thiếu và 0, không âm thầm ghi đè ngoại lệ hay coi giả định là thực tế.
- **Xong khi:** phiếu 3.1b hoàn tất theo tiêu chí và kiểm chứng riêng, được đưa về cây gốc; không xem các chặng sau là xong theo đó.

### 2. Lưu và mở lại hồ sơ theo năm

- **Trạng thái:** đã nghiệm thu kỹ thuật theo bốn tiêu chí của phiếu `01M8BK6R1EC84DFJ05Z8FH31C2`, done và land ngày 26/11/2026; mã tại commit `f4f3133`. Phạm vi local synthetic, năm 2026; danh sách hồ sơ nằm ngay tại `/Forecast`. Log đầy đủ cuối phiếu: Core 205, Data 305, Web 70 test qua, EF không lệch migration; QA browser do phiên điều phối trước thực hiện. Contract kết quả năm đã triển khai riêng trong phiếu chặng 3, đang chờ nghiệm thu.
- **Bắt đầu:** chặng 1 hoàn tất và contract trạng thái đầu vào đã ổn định.
- **Tối thiểu:** lưu, mở lại và cập nhật hồ sơ năm và khấu trừ từng tháng với metadata từng giá trị; bảo vệ dữ liệu, ghi audit và hỗ trợ xóa có chủ đích với phạm vi được giải thích. Lưu kết quả hiện có và chuẩn bị contract riêng cho kết quả năm khi chặng 3 có; không biến giả định thành dữ liệu thực tế.
- **Xong khi:** mở lại qua phiên lưu trữ mới giữ đúng số liệu và metadata; cập nhật tháng và tính lại không sửa snapshot, đầu vào hay phiên bản luật của lịch sử cũ; kiểm chứng bảo vệ, bảo toàn dữ liệu và hành vi xóa. Không đòi hỏi dự báo năm đã triển khai.

### 3. Tổng hợp và dự báo năm

- **Trạng thái:** đã triển khai trong phiếu `01M8DJDSFX7JJBRP9YZPVSG8YG`, cổng đầy đủ đã qua Core209/Data319/Web101 và EF; đã nghiệm thu kỹ thuật, Bint `done` ngày 28/11/2026 sau review vòng 4 không có phát hiện và cổng 629 test/EF sạch. Có service tính từ revision/catalog đã lưu, Web hiển thị bước tính/căn cứ/độ phủ và lịch sử năm mã hóa append-only; xem [bằng chứng](reviews/2026-11-27-annual-stage-three.md).

- **Bắt đầu:** chặng 2 hoàn tất để dự báo dùng được hồ sơ đã lưu và mở lại.
- **Tối thiểu:** tích hợp lương, thưởng thực tế/dự kiến được hỗ trợ, bảo hiểm nhập tay, người phụ thuộc theo thời gian và khấu trừ đã lưu theo tháng; dùng engine năm cho nghĩa vụ thuế năm ước tính. Hiển thị phần thực tế/dự kiến/giả định, số tháng đủ dữ liệu thực tế, độ phủ khấu trừ riêng và phạm vi chưa bao gồm. Không thay phép tính năm bằng tổng thuế tháng hay suy khấu trừ từ thuế tính được.
- **Xong khi:** kết quả năm giải thích được các bước, khoản đã tính/chưa hỗ trợ từ hồ sơ và căn cứ đúng kỳ; thiếu đầu vào được báo rõ, chỉ hiện chênh lệch khi đủ khấu trừ hoặc giả định rõ cho cả kịch bản; các trường hợp đủ và thiếu được kiểm chứng.

### 4. Xuất dữ liệu và checklist chuẩn bị quyết toán

- **Bắt đầu:** chặng 3 hoàn tất, có tổng hợp năm và trạng thái độ đầy đủ để xuất.
- **Tối thiểu:** xuất báo cáo số liệu, nguồn/kỳ và trạng thái cùng kết quả tham khảo để tự giữ hoặc gửi HR; cung cấp checklist chuẩn bị quyết toán. Có bản sao lưu riêng có thể khôi phục, bảo vệ bản sao và bảo toàn metadata, phiên bản luật, lịch sử; kiểm tra bản nhập trước khi ghi và không âm thầm ghi đè. Báo cáo không tự là backup; đây không phải tờ khai chính thức. PDF trình bày đẹp để sau.
- **Xong khi:** người dùng lấy được báo cáo và checklist nhất quán với hồ sơ, thấy phần thiếu và giới hạn tham khảo; sao lưu rồi khôi phục giữ metadata, phiên bản và lịch sử, bản nhập không hợp lệ bị từ chối trước khi ghi và không có ghi đè âm thầm.

### 5. Hai kịch bản đơn giản

- **Bắt đầu:** chặng 4 hoàn tất; dùng hồ sơ và phép tính năm của các chặng trước.
- **Tối thiểu:** kịch bản cơ sở và kịch bản thêm một khoản thưởng giả định chịu thuế từ nơi trả lương hiện tại, có số tiền và tháng riêng. Giữ khoản thực tế, tránh cộng trùng gross; không ghi giả định trở lại hồ sơ thực tế.
- **Xong khi:** mỗi kịch bản có kết quả riêng và có thể so sánh; hai kết quả bằng nhau không tạo khoảng giả, khoảng giữa chúng không được trình bày là bảo đảm hay xác suất.

### 6. Đối chiếu hồ sơ riêng với số người dùng cung cấp

- **Bắt đầu:** chặng 5 hoàn tất; hồ sơ, kết quả và metadata nguồn/kỳ đã sẵn sàng.
- **Tối thiểu:** cho người dùng nhập số từ công ty hoặc eTax kèm nguồn, kỳ và ghi chú, rồi đối chiếu với hồ sơ riêng theo chỉ tiêu và kỳ tương ứng; không thu mật khẩu hay lưu file gốc.
- **Xong khi:** người dùng thấy số của từng nguồn, chênh lệch và phần thiếu hoặc chưa tương ứng; kết quả không kết luận công ty hay eTax sai.

## Làm song song và việc để sau

Có thể chuẩn bị yêu cầu, thiết kế, ví dụ và rà phụ thuộc cho chặng sau trong khi chặng trước đang thực hiện. Chỉ bắt đầu **triển khai** từng chặng khi điều kiện bắt đầu ở trên đã đạt; phối hợp theo tiến độ không làm thay đổi scope của phiếu 3.1b. Mỗi chặng cần phiếu và kiểm chứng riêng theo phạm vi thực tế.

Các tiện ích backlog ở bảng trên không đưa vào 3.1b. Cập nhật luật bằng AI, nhiều nơi trả lương, freelance/kinh doanh, thu nhập nước ngoài, OCR và XML là các chặng riêng cần quyết định và lập kế hoạch sau; chưa được tính là năng lực của sáu chặng này.
