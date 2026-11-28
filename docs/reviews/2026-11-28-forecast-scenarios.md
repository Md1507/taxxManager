# Chặng 5 — hai kịch bản thưởng

28/11/2026, local synthetic. [Contract và bố cục trước triển khai](../design/forecast-scenarios.md). Giữ Core, migration, key/catalog và lịch sử hiện có; thêm service đọc nhất quán cùng màn so sánh, không triển khai chặng 6.

## Hành vi

- Mở từ Tổng quan năm, nhập tiền và tháng; GET chỉ hiện cơ sở, POST thêm một khoản Giả định ngoài gross. Cả hai dùng `AnnualForecastCalculator.CalculateScenarios` với một revision và catalog đọc cùng transaction. Revision cũ trả 409, đầu vào/luật/khóa hỏng từ chối, không fallback.
- Giữ thưởng thực tế/dự kiến có sẵn và tránh cộng trùng phần đã gồm gross. Thử 0 cho kết quả không đổi; không tạo min/max, khoảng bảo đảm hoặc xác suất. Thiếu chỉ tiêu nghĩa vụ được chỉ rõ tháng; khấu trừ theo dõi riêng. Không suy số nộp thêm/nộp thừa cho thưởng khi chưa có khấu trừ tương ứng.
- Không tạo revision, audit hay lịch sử; dữ liệu nhập nguồn, ngày, trạng thái và snapshot cũ giữ nguyên. no-store trên các phản hồi của PageModel, antiforgery bảo vệ POST và đổi kho theo cơ chế hiện có.
- Một nút chính So sánh, hai ô tiền/tháng, không dropdown mới. Tiền tự định dạng, giữ chuỗi/lỗi khi POST sai; nhãn kết quả cũ hiện khi sửa khoản thử. Độ phủ từng kịch bản có Thực tế/Dự kiến/Giả định, nguồn luật giữ Unverified.

## Kiểm chứng

- 8/8 test HTTP mới qua: kết quả độc lập 12,4 triệu → 13,4 triệu với thưởng thử 10 triệu; gross 360 triệu + thưởng cơ sở 10 triệu, khoản 5 triệu đã gồm gross không cộng lại. Thu nhập tính thuế 184/194 triệu, thuế 120 triệu × 5% + 64/74 triệu × 10%; đây là số liệu synthetic của test theo catalog tham khảo hiện có, không hướng dẫn thuế thực tế.
- Kiểm payload giữ đúng byte, một snapshot đầu vào và một kết quả lịch sử cũ, audit không tăng khi so sánh; các ca thiếu/0, validation giữ chuỗi, CSRF, revision đổi/xóa và catalog/sentinel/cipher hỏng. Giữ test cũ; chưa sửa assertion đóng băng.
- Ban đầu test đỏ vì route chưa có; sau triển khai, sửa kỳ vọng hiển thị từ số nguyên sang `,00` theo FormatAmount đang dùng. Giá trị thuế kỳ vọng không đổi.
- `bint test affected --run` lượt đầu qua 328 Data + 143 Web, exit 0; log `%TEMP%/taxmanager-scenarios-affected.log`. Lượt cuối sau cập nhật metadata Hallmark yêu cầu chạy full nhưng không có `eval.test_command`, nên không thực thi được; log `%TEMP%/taxmanager-scenarios-affected-final.log`. Không coi lượt affected cuối là qua; giữ cấu hình và chạy script đầy đủ trực tiếp.
- Đã chạy thay bằng toàn bộ `powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/dev/verify-web-mvp.ps1` trên mã cuối: exit 0, Core 209 + Data 328 + Web 143 = **680/680 test**, không bỏ qua; EF xác nhận không có pending model changes. Build không lỗi, còn hai cảnh báo xUnit2031 đã có tại ForecastPersistenceBlackBoxTests dòng 241/243. Log `%TEMP%/taxmanager-scenarios-full-gate.log`. `node --check` cho workspace.js qua.

## QA trình duyệt và finish review

Orca, kho synthetic riêng đang có; tạo thêm một hồ sơ QA, giữ các hồ sơ cũ. Mở `/ho-so/cc95ac70-98ee-4923-8604-82267d230438/kich-ban`, nhập bằng phím `10000000` thành `10.000.000`, tháng 12, rồi bấm So sánh. Thuế hiển thị 12.400.000,00 đ và 13.400.000,00 đ, tăng 1.000.000,00 đ. Độ phủ cơ sở 11 Thực tế / 1 Dự kiến, kịch bản 10 Thực tế / 1 Dự kiến / 1 Giả định. Sửa ô sau khi tính hiện thông báo kết quả cũ; so sánh lại ẩn thông báo.

- Đo 320, 375, 768, 1024, 1280, 1440 và 1920 px: không overflow body, đúng một primary; desktop hai ô cùng đường chuẩn, mobile xếp dọc. Bảng bước tính cuộn trong vùng riêng, không kéo rộng trang.
- Axe 4.11.0 trên kết quả cuối ở 375 và 1440 px, bộ WCAG 2 A/AA + 2.1 AA: **0 violations, 0 incomplete**, gồm kiểm tương phản tự động. Không coi kết quả này thay thế nghiệm thu UX bằng người dùng.
- Đã xem ảnh desktop `9771face-7b9f-4567-8252-98272d050dcd`; ảnh viewport mobile phần form `49c030c4-e88c-4241-adfc-f7d0385df08c`, kết quả `f9323332-ded5-45ed-8b84-3d2e3b293b96`, bảng `8aeb92ab-cede-4ccb-a96a-7e7a5d41f2c2`. Tệp PNG nằm trong `%TEMP%/orca-browser-screenshots/`, hậu tố `-screenshot.png`; ảnh tạm có thể hết hạn. Không dùng ảnh full-screenshot ghép lỗi lặp header làm bằng chứng bố cục.

Finish review thực hiện trong phiên Codex theo nhánh harness của Hallmark, không có reviewer độc lập. Giữ một vùng so sánh chung, nhãn trạng thái và tiền tabular; không thêm card cho từng chỉ số, biểu đồ trang trí hoặc dropdown mới. Kết quả và thao tác đọc riêng được ở desktop/mobile đã xem.

Đã chạy sloplint với `--scope app --genre modern-minimal --render --url` trên route này; log `%TEMP%/taxmanager-scenarios-hallmark.json`. Native render không chạy vì thiếu puppeteer-core; ảnh và axe nêu trên dùng Orca. Ba phát hiện static được đối chiếu như sau, không tuyên bố tool trả 0 FAIL:

- Gate 56 báo sidebar và `thead th` cùng `top:0`: sidebar nằm cột riêng, header bảng thuộc vùng cuộn bảng; hai scrollport không chồng nhau. Không có banner thứ hai cần bù offset.
- Gate 37 đếm các token cỡ chữ `.8125rem`, `.875rem`… như font family; thực tế dùng Inter và system fallback, không phải 10 họ chữ.
- F7 báo recipe chung cho input/select/button/textarea: thống nhất chiều cao/radius là chủ ý; nút So sánh có nền đặc, liên kết trở lại là chữ, input viền mảnh nên vai trò vẫn phân biệt.

## Thiết kế và giới hạn

Dùng Hallmark app screen, frontend-design, ui-ux-pro-max với brand/token có sẵn. Truy vấn design-system hai lần trả đề xuất marketing/hero/palette khác brief nên không áp dụng; dùng guideline form label, lỗi tại ô và chống tràn ngang cùng hệ thống hiện có. Không dựng hero hoặc thay palette theo kết quả tìm không phù hợp.

Nghiệm thu UX tổng thể (bàn phím thật có focus cửa sổ, screenreader, native zoom và thiết bị thật) vẫn mở như các màn trước. Không triển khai lưu/chia sẻ/xuất kịch bản, tự giả định khấu trừ, nhiều nơi trả lương hoặc thay đổi dữ liệu thật.

Áp dụng quy tắc xác thực khóa cả ID không có kết quả [k:01M85QGMTJVNX37D5XYH5161KM]; CLI cite không khả dụng ngoài BINT_RUN_ID nên ghi ID tại đây. Lịch sử cũ không được tạo trực tiếp hoặc ghi đè.
