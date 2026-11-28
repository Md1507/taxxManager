# Chặng 4 — sao lưu có bảo vệ và chỉnh bảng tháng

Phạm vi local synthetic, ngày 28/11/2026. [Contract](../design/protected-backup.md). Không sửa Core, Data, schema/migration, pin SDK/package hoặc đóng phiếu Bint cũ.

## Kết quả

- `/sao-luu`: tải toàn kho trong tệp `.taxbackup`, AES-256-GCM + PBKDF2-SHA256, mật khẩu 12–128 ký tự, không xuất key sidecar. Ảnh DB dùng SQLite online backup, không copy riêng tệp DB đang chạy.
- `/khoi-phuc`: chọn tệp và mật khẩu, kiểm giới hạn/phiên bản, schema/migration, SQLite integrity/FK, sentinel, mọi trường được bảo vệ, forecast payload/snapshot, lịch sử tháng/năm, audit và catalog.
- `/khoi-phuc/xem-truoc`: ngày sao lưu, số hồ sơ/phiên bản/kết quả/audit, ID trùng, xác nhận mở kho riêng hoặc hủy. Giữ payload, metadata, luật và lịch sử, không merge hay ghi đè kho gốc. SHA256 DB/key kiểm staging chưa đổi trước xác nhận.
- Kho chọn theo trình duyệt. Antiforgery gắn owner/thế hệ kho, request cùng trình duyệt tuần tự hóa: form cũ sau chuyển kho không thể ghi vào kho mới dù ID/revision trùng. Có nút quay về kho ban đầu; bản đã xác nhận được giữ trên disk và có thể mở lại bằng SyntheticStore hoặc xuất/nhập lại backup. Chọn kho không tồn tại qua restart.
- Staging hết hạn 10 phút, handle một lần, hủy/hết hạn dọn root; giới hạn upload 17 MiB và DB 16 MiB. Source không đổi khi export/preview/failure. Chỉ bản đã xác nhận được dùng để sửa hồ sơ.
- Danh sách xác định đủ/thiếu qua ForecastDraftReader/AnnualForecastCalculator.Assess để đọc cả contract normalized của Data. Editor kiểm draft không hỗ trợ trước dùng trường riêng Web, trả lỗi có hướng dẫn thay vì NullReference. Không đổi payload để làm hợp editor.

## Bảng 12 tháng

Theo ảnh người dùng: bỏ viền bao và thanh cuộn dọc riêng, giữ đường ngang mảnh; bỏ legend lặp và khung nhãn trong bảng. Trạng thái vẫn có chữ, màu và hình phân biệt; số tiền tabular/căn phải, `Chưa nhập` không wrap. Nút lớn và dòng link dài đổi thành `Sửa` / `Chi tiết` cùng hàng; accessible name vẫn nêu tháng và bản lưu. Nguồn/ghi chú giữ nguyên. Mỗi hàng khoảng 69,5px tại lần đo desktop, không ép toàn trang tràn ngang trên mobile.

Không sửa test cũ để đáp ứng markup mới. Lần kiểm đầu có một assertion phụ thuộc thứ tự attribute (`data-label="Lương">Chưa nhập`); đã giữ attribute đó ở cuối thẻ, cùng hành vi cũ.

## Kiểm chứng

- Build Web và host preview: 0 warning/error.
- `ProtectedBackupTests`: 13/13 qua. Logical dump mọi hàng của mọi bảng bằng nhau trước/sau restore; đọc lại bằng host mới cùng restored root. Có dữ liệu tháng, forecast revision, annual snapshots, hồ sơ đã ordinary-delete, audit, catalog; đọc nguồn/ghi chú/ngày/trạng thái giữ nguyên qua payload.
- Từ chối password sai, salt/nonce/cipher/tag bị sửa, truncate/unknown envelope/size; bản đã mã hóa hợp lệ nhưng sai schema/migration/catalog/key/sentinel/payload/audit cũng không được nhận. Có ca kho trống, kho đã xóa hồ sơ, source thiếu sentinel; không reset/seed/fallback bản nhập.
- HTTP kiểm export no-store, preview không ghi nguồn, ID trùng vẫn tách kho, owner khác với token riêng không dùng được handle, thiếu CSRF/form cũ/replay bị chặn, quay về kho gốc; kiểm root thực sự được dọn khi hủy/hết hạn và khi staging bị sửa trước confirm.
- `bint test affected --run`: lượt đầu sau hoàn thiện mã 134/134 Web qua, exit 0. Sau bổ sung test staging, lượt cuối 135/135 Web qua, exit 0, `%TEMP%/taxmanager-backup-final.log`. Hai warning xUnit2031 cũ ở ForecastPersistenceBlackBoxTests:241/243, không sửa ngoài scope.
- Sau thêm `.hallmark/log.json` và `preflight.json`, `bint test affected --json` mở rộng `scope=full`, `widening.reason=unknown_file`, command rỗng. Vì vậy chạy thêm toàn bộ `powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/dev/verify-web-mvp.ps1`, log `%TEMP%/taxmanager-backup-full-gate.log`: exit 0, build 0 warning/error, Core 209/209, Data 328/328, Web 135/135 (tổng 672/672); EF xác nhận model không thay đổi so với migration cuối. Không lấy lượt affected Web thay full gate và không sửa mapping để thu hẹp nó.
- Orca host `http://127.0.0.1:5089`: đã tải tệp bằng nút thật; upload tệp đó, xem trước và mở kho qua phím Enter, quay về kho gốc. Orca pointer auto-scroll ở mobile có một lần chỉ cuộn mà chưa kích hoạt; đã kiểm focus/Enter riêng, không ghi đó là pointer pass.
- Ảnh desktop/mobile đã xem lại: bảng (`ae095834-6dd0-4774-8c65-4fe0056e01ce`, `4b0feb04-f27f-4489-a813-92e4f078f952`), backup (`1df8a8b1-9fd0-4772-aca0-b3fd20c0ea40`), preview (`b78b39c6-4c38-4fed-be58-6b0a02b45b39`, mobile full `bcbbcaf9-c2a2-4f66-a809-ed96655eb2f7`) trong `%TEMP%/orca-browser-screenshots/`. Không lưu mật khẩu hay khóa vào ảnh/tài liệu.
- axe: preview khôi phục và riêng section bảng tháng 0 violations, 0 incomplete. Lượt toàn editor 0 violations, còn color-contrast incomplete ở các textarea bị phần sticky che trong viewport; không gọi là full accessibility pass. Chữ phụ #526057 trên trắng 6,63:1; trạng thái thực tế/dự kiến/giả định lần lượt 6,85/6,94/6,19:1. Giữ giới hạn nghiệm thu keyboard đầy đủ, screenreader, zoom native và thiết bị thật từ UI plan.

## Hallmark

Đã cài [Hallmark v2](https://github.com/nutlope/hallmark/tree/04977e15f3dfd6ee4b74c887dac3eb52665d6c92/skills/hallmark), dùng nhánh app screen/polish cùng frontend-design và ui-ux-pro-max, giữ brand/tokens đã chốt. Cache/log trong `.hallmark/`; không chạy scaffold/theme đổi hệ thống của dự án.

Đã chạy `sloplint.mjs ... --scope app --genre modern-minimal --render --url ...`. Render tier không chạy do thiếu puppeteer-core; kiểm render/axe qua Orca, không tuyên bố Hallmark render pass. Static tool báo:

- Gate56: sidebar và thead đều top0. Đã đối chiếu: sidebar là cột riêng; thead trong vùng cuộn bảng riêng, không xếp chồng cùng scrollport. Không phải hai banner cùng chỗ.
- Gate37: đếm `--font-xs`, `--font-sm`, … như font family. Thực tế chỉ Inter và fallback hệ thống; size/weight token không phải family.
- Finish F7: controls chia sẻ border/radius. Đây là nhóm control đồng nhất; các vai trò vẫn khác nhau bằng primary fill, text action và outline. Không thêm radius ngẫu nhiên để làm checker im lặng.

Fresh-context subagent xem ảnh và source kết luận `ship`, 0 phát hiện trong phạm vi đã xem. Đây là review UI, không phải audit cryptography/security độc lập.

## Giới hạn

Chỉ synthetic/loopback. Không auth đa người, không phục hồi mật khẩu, không tự di chuyển phiên bản DB khác. TEMP chứa DB protected và key ring; cleanup không bảo đảm forensic erasure. Không có security approval cho dữ liệu thật. Chặng 5 chưa triển khai trong tác vụ này.

Tri thức phát hiện: loader catalog tự bắt transaction, candidate `01M8G7PX3H903296WFCTCMTXBT` (origin agent), chờ người duyệt; không publish/approve.

## Sửa lệch form công cụ tính tháng theo ảnh phản hồi

Route `/`, `Pages/Index.cshtml`: lỗi thuộc nhóm nhập của công cụ tháng, khác bảng xem trước forecast đã sửa ở trên. Tái hiện trên Orca ở 1440px: legend là grid item đầu trong `fieldset.month`; hai ô lương/bảo hiểm ở y665 nhưng thuế khấu trừ ở y773, cả nhóm cao 248px.

Đặt ba label trong `div.form-grid`, giữ fieldset/legend làm nhóm ngữ nghĩa và bỏ `.month` khỏi selector grid. Tiêu đề đứng trên; ba ô cùng hàng desktop, xếp dọc ở <=768px. Không đổi tên/thứ tự 36 input, handler, required, giá trị lỗi, parser hay JavaScript tiền. Giữ palette/Inter, không thêm viền hoặc trang trí.

- Probe DOM cả 12 nhóm: cùng đường chuẩn và cùng chiều rộng ở 1024/1280/1440/1920px; xếp dọc cùng mép ở 320/375/768px; không tràn ngang ở các kích thước đó. Nhóm desktop còn 160px.
- Đã xem ảnh desktop `a1a22dcf-868c-492e-80e9-0e1f07f123f8` và mobile `45769c7d-b05f-4f04-a920-804ad17e6cb4` trong `%TEMP%/orca-browser-screenshots/`. Review Hallmark trong cùng phiên theo hướng dẫn Codex: bố cục nhóm rõ, không còn ô lệch; không có nội dung trước đây bị khó tìm hơn. Không gọi đây là review độc lập.
- Gõ thật `123456789` cho `123.456.789`; ô kế bên vẫn trống. Tab đi từ lương sang bảo hiểm rồi thuế. Orca chuyển focus đúng nhưng `:focus-visible` không được engine bật trong phép thử này; không tính là nghiệm thu focus ring bằng bàn phím thật.
- Hallmark static cho cùng ba cảnh báo đã giải thích ở mục Hallmark; render tier vẫn thiếu puppeteer-core, dùng ảnh Orca để đối chiếu.
- `bint test affected --run` yêu cầu full nhưng thiếu `eval.test_command` do phạm vi chưa commit hiện tại; chạy trực tiếp full gate thay thế, log `%TEMP%/taxmanager-month-layout-full-gate.log`: exit 0, build 0 warning/error; Core 209/209, Data 328/328, Web 135/135 (672/672), EF xác nhận model khớp migration cuối.
- Candidate chống tái diễn: `01M8G8KGHF2YMZ0JMY6RZ1ZMV7`, giữ legend ngoài lưới các ô tháng; không tự duyệt/publish.
