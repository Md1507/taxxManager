# Công việc và backlog

## Checklist lập kế hoạch của phiếu hiện tại
Các TSK dưới đây chỉ mô tả đầu ra tài liệu để review; không ủy quyền viết ứng dụng. Checklist không thay trạng thái do Bint Core quản lý.
- [ ] TSK-1 [AC-1]: Ghi bằng chứng hiện trạng và khoảng cách với PLAN.md — xong khi: requirement.md nêu kết quả kiểm Git, SDK, solution, cấu hình và phiếu cập nhật PLAN đã done; không nhận tính năng chưa có là đã làm.
- [ ] TSK-2 [AC-2]: Lập roadmap và đề xuất phiếu theo chặng — xong khi: plan.md ánh xạ Bước 0 đến 4.3, phân biệt MVP/mở rộng và có thứ tự, scope, depends_on, ví dụ lệnh chỉ để tham khảo.
- [ ] TSK-3 [AC-2]: Viết backlog, chi tiết Bước 0 và 1.1 — xong khi: mỗi BL có ưu tiên, loại, phụ thuộc, phạm vi, điều kiện bắt đầu và xong khi; chặng đầu nêu tệp/ký hiệu, API dự kiến và test bắt buộc.
- [ ] TSK-4 [AC-3]: Lập bảng dữ liệu pháp lý và quyết định theo chặng — xong khi: plan.md có L0-L9 gồm nguồn chính thức, phần người dùng cung cấp, thời điểm cần và xử lý khi thiếu; không thêm số luật từ trí nhớ.
- [ ] TSK-5 [AC-5]: Gắn trích dẫn theo ngữ cảnh vào toàn backlog liên quan — xong khi: metadata/kỳ nằm ở BL-11/13/20, mapping/test ở BL-15/20, nguồn và DB ở BL-31, UI ở BL-40, vãng lai ở BL-23c; ví dụ 10% ghi rõ minh họa chờ xác minh.
- [ ] TSK-6 [AC-4]: Lưu và kiểm hợp đồng ba tài liệu qua Bint — xong khi: requirement/plan/tasks tồn tại trong phiếu, bint task lint --strict thoát 0 và so hash ngoài ledger không phát hiện đổi PLAN.md, cấu hình hay mã; không cài công cụ/chạy ứng dụng.

## Backlog thực thi tương lai — tất cả chưa thực hiện
BL không phải checklist triển khai của phiếu này. Không BL nào đã hoàn thành; chưa lập các phiếu S. P0/P1/P2 và MVP-L/MVP-D/mở rộng được định nghĩa trong plan.md. Phạm vi là --allow dự kiến của phiếu tương lai; mọi phiếu thêm .bint/tasks/**, giữ deny PLAN.md và .bint/config.yml. Không dùng bảng để ghi ngoài scope hiện tại.

Các đường dẫn ngắn: Core = src/TaxManager.Core/**; TC = tests/TaxManager.Core.Tests/**; Data = src/TaxManager.Data/**; TD = tests/TaxManager.Data.Tests/**; Web = src/TaxManager.Web/**; TW = tests/TaxManager.Web.Tests/**; DQ = docs/decisions/**. Các ký hiệu này chỉ rút gọn đường dẫn, không phải module có sẵn. L là điều kiện dữ liệu/quyết định trong plan.md; cùng lúc chỉ thực hiện một bước PLAN.

### Chặng đầu — kế hoạch chi tiết
| Mã / ưu tiên / loại | Việc; phạm vi dự kiến | Phụ thuộc | Bắt đầu khi | Xong khi đo được |
|---|---|---|---|---|
| BL-00 / P0 / MVP-L / S0 | Chuẩn bị SDK mục tiêu, kiểm Git/editor, ghi quyết định; DQ | Phiếu lập kế hoạch được xem và kết thúc theo Bint | Có quyền thực thi chặng S0; L0 về công cụ | dotnet --list-sdks liệt kê SDK đã chọn, Git hoạt động, ghi phiên bản/cách kiểm; không đổi model hay git init lại |
| BL-01 / P0 / MVP-L / S0 | Ignore .NET và pin SDK; .gitignore, global.json, DQ | BL-00 | SDK cụ thể đã chọn | bin/obj/IDE/test-output/secrets bị ignore, khối Bint giữ nguyên; mã/test/global.json không bị ignore; pin khớp SDK |
| BL-10 / P0 / MVP-L / S11 | Tạo TaxManager.sln, Core class library và Core.Tests xUnit; TaxManager.sln, Core, TC | BL-01 | S0 hoàn tất; chọn phiên bản package tương thích trong lượt thực thi | Solution có đúng 2 project, test tham chiếu Core, restore/build được; Core không có dependency EF/Web/thư viện ngoài; không tạo test giả để báo hoàn tất nghiệp vụ |
| BL-11 / P0 / MVP-L / S11 | Xác minh L1; Bracket, TaxRuleSet, dữ liệu Vietnam2026 và metadata phiên bản/kỳ; Core, TC, DQ | BL-10 | Có nguồn gốc L1 và L0 về đối tượng/đầu vào; nếu thiếu chỉ làm schema/fixture giả | Mỗi tham số luật có nguồn/kỳ/trạng thái; tiền decimal; fixture hợp lệ/không hợp lệ được phân biệt; chỉ nạp Vietnam2026 thật khi nguồn/hiệu lực đã đối chiếu |
| BL-12 / P0 / MVP-L / S11 | PitCalculator và MonthlySalaryCalculator, record kết quả; Core, TC | BL-11 | Contract dữ liệu và lỗi đầu vào L0 đã chốt | Tính từng phần, không làm tròn trong PitCalculator; insurance nhận số tiền; thu nhập tính thuế âm thành 0; trả từng bước, không hardcode số luật trong thuật toán |
| BL-13 / P0 / MVP-L / S11 | Metadata căn cứ và mapping vào result; Core, TC | BL-11; ghép kết quả BL-12 | Có schema rule/version/kỳ và danh sách thành phần kết quả | Mỗi thành phần liên kết ID/version căn cứ thực dùng; có điểm/khoản/điều/số văn bản, 2-3 câu, URL vbpl.vn, kỳ/ngày áp dụng, trạng thái/ngày xác minh; thiếu/chưa xác minh biểu diễn được; không HTTP/Web/DB trong Core |
| BL-14 / P0 / MVP-L / S11 | Kiểm số học và đầu vào; TC, DQ | BL-12 | L1 được đối chiếu cho các ca luật thật; expected do người dùng đối chiếu | 7 ca Pit và 3 ca lương của PLAN Bước 1.1 được đối chiếu và xanh bằng Theory; test quanh các ngưỡng/âm sau giảm trừ/không làm tròn/lỗi đầu vào theo L0 xanh; thêm 2-3 ca độc lập có expected được tính riêng |
| BL-15 / P0 / MVP-L / S11 | Test căn cứ và bàn giao 1.1; TC, DQ | BL-13, BL-14 | Fixture căn cứ/version và trạng thái sẵn có | Test mapping từng thành phần, thay version/kỳ, thiếu/chưa xác minh/URL sai đều xanh; Core không có I/O mạng; suite Core và solution xanh, người dùng đối chiếu PitCalculator; không báo hoàn tất luật thật nếu L1 còn thiếu |

BL-00/01 chỉ hoàn thành Bước 0; BL-10 đến 15 chỉ hoàn thành Bước 1.1. Các ca số học PLAN.md là dữ liệu nguồn cần đối chiếu, không bằng chứng pháp lý do planner xác nhận. Không yêu cầu cài SDK hay đối chiếu luật để hoàn thành phiếu lập roadmap này.

### Các chặng sau — backlog đề xuất, chưa thiết kế/triển khai
| Mã / ưu tiên / loại | Việc; phạm vi dự kiến | Phụ thuộc | Bắt đầu khi | Xong khi đo được |
|---|---|---|---|---|
| BL-20 / P0 / MVP-L / S12 | Bộ quy tắc 2025 và TaxRuleProvider theo kỳ/ngày; Core, TC, DQ | BL-15 | Có L2 và nguồn căn cứ tương ứng | Test cùng đầu vào khác kỳ ra expected đã đối chiếu; biên ngày chọn đúng rule/căn cứ; kỳ thiếu hoặc khoảng mơ hồ báo lỗi rõ, không dùng bản mới nhất tùy tiện |
| BL-21 / P0 / MVP-L / S13 | BH bắt buộc và làm tròn; Core, TC, DQ | BL-20 | L3 đủ đối tượng/tỷ lệ/trần/sàn/cách làm tròn | Test sát trần/sàn và các điểm làm tròn đã chốt xanh; số luật trong rule, căn cứ đúng kỳ; không làm đổi nguyên tắc không làm tròn của PitCalculator |
| BL-22 / P1 / MVP-L / S14 | Phụ thuộc theo tháng và tổng hợp năm; Core, TC, DQ | BL-21 | Có L4 và expected năm độc lập | Test đăng ký giữa năm, kết thúc giảm trừ, đủ 12 tháng và hoàn/nộp thêm khớp expected; mỗi phần kết quả giữ căn cứ/kỳ thực dùng |
| BL-23a / P2 / Mở rộng / S15a | Thưởng/thu nhập bất thường; Core, TC, DQ | BL-22 | Chọn làm loại này, có L5 tương ứng | Có test riêng cho phân loại/kỳ/căn cứ và số kỳ vọng đã đối chiếu; không thay kết quả lương cơ bản |
| BL-23b / P2 / Mở rộng / S15b | Làm thêm/đêm/phụ cấp; Core, TC, DQ | BL-23a | Có quy tắc miễn/trần L5 theo kỳ | Test dưới/đúng/trên ngưỡng đã xác minh, mapping căn cứ đúng; số luật chỉ trong rule |
| BL-23c / P2 / Mở rộng / S15c | Thu nhập vãng lai và căn cứ; Core, TC, DQ | BL-23b | Có L5-VL: người dùng đối chiếu văn bản gốc, hiệu lực và URL | Test theo điều kiện/ngưỡng/kỳ thật được đối chiếu; mapping đúng căn cứ; ví dụ 2.500.000 đồng → 250.000 đồng (10%) vẫn ghi là minh họa chờ xác minh cho tới khi có bằng chứng, không mặc nhiên biến thành quy tắc |
| BL-23d / P2 / Mở rộng / S15d | Nhiều nơi trả lương; Core, TC, DQ | BL-23c | Có L5 cho tổng hợp/khấu trừ nhiều nguồn | Test tổng hợp không đếm trùng, tạm nộp và quyết toán khớp bộ ca độc lập, nguồn gắn đúng loại/kỳ |
| BL-23e / P2 / Mở rộng / S15e | Giảm trừ từ thiện; Core, TC, DQ | BL-23d | Có L5 cho điều kiện/chứng từ/phạm vi giảm trừ | Test đủ/thiếu điều kiện và giới hạn nếu có theo nguồn đã xác minh; kết quả/căn cứ đúng kỳ |
| BL-23f / P2 / Mở rộng / S15f | Hưu trí tự nguyện; Core, TC, DQ | BL-23e | Có L5 cho điều kiện/mức giới hạn | Test biên giới hạn, kỳ áp dụng, trạng thái thiếu căn cứ; không tự lấy số tham khảo trong PLAN.md làm dữ liệu thật |
| BL-30 / P0 / MVP-L / S21 | EF Core + SQLite; hồ sơ, thu nhập, phụ thuộc; TaxManager.sln, Data, TD, migrations/**, DQ | BL-22 | Mô hình lõi ổn định; L6 chốt dữ liệu tối thiểu; không phụ thuộc nhóm BL-23 | Migration trên DB giả sạch chạy được, test lưu/đọc đúng dữ liệu và quan hệ xanh; scope migration được xem trước thực thi |
| BL-31 / P0 / MVP-L / S22 | Luật và căn cứ cùng phiên bản trong DB; Data, TD, migrations/**, DQ | BL-30 | Nguồn/URL/hiệu lực L1-L4 đã đối chiếu cho bộ sẽ seed | Roundtrip đúng rule/version/căn cứ; phát hiện chồng/trùng kỳ, URL sai/ngoài nguồn gốc/thiếu hoặc chưa xác minh; không seed như verified; engine dùng snapshot từ DB mà Core không biết ORM |
| BL-32 / P0 / MVP-L / S23 | Mã hóa, quản lý khóa, audit; Data, TD, DQ | BL-31 | L6 chốt trường nhạy cảm/khóa/audit; kế hoạch review bảo mật | DB không đọc được plaintext trường đã chọn; test đọc lại/khóa sai/sửa ciphertext; audit đủ thay đổi và không lộ dữ liệu/khóa; người dùng hoặc reviewer Codex đọc lại |
| BL-40 / P1 / MVP-L / S31 | Form 12 tháng, chi tiết và nút ?; TaxManager.sln, Web, TW, DQ | BL-32 | Có L7; chỉ local với dữ liệu giả, chưa công bố nhiều người | Kết quả khớp Core; mỗi thành phần có ? mở 2-3 câu + điểm/khoản/điều/số văn bản + link vbpl.vn đã đối chiếu; thiếu/chưa xác minh hiện rõ; test keyboard mở/đóng/focus và mobile đạt; có disclaimer tham khảo |
| BL-41 / P0 / MVP-L; bắt buộc trước đa người dùng / S32 | Identity, cô lập người dùng, HTTPS/cookie/CSRF; Web, Data, TW, TD, migrations/**, DQ | BL-40 | Chốt L6/L7 và có ca truy cập trực tiếp bằng ID | Hai tài khoản không đọc/sửa chéo kể cả request trực tiếp; request CSRF sai bị từ chối; cấu hình cookie/HTTPS kiểm được; bảo mật được đọc lại |
| BL-42 / P1 / MVP-L / S33 | Màn hình năm/hoàn/nộp thêm; Web, TW, DQ | BL-41, BL-22 | Có dữ liệu giả đủ 12 tháng và expected năm | UI khớp ca Core 1.4, chi tiết từng tháng và căn cứ/kỳ đúng; không đổi công thức ở Web |
| BL-50 / P0 / MVP-D / S41 | Đồng ý xử lý, xuất dữ liệu cá nhân, xóa tài khoản/dữ liệu; Web, Data, TW, TD, DQ | BL-42 | Có L8 về lưu/xóa và phạm vi xuất, review bảo mật | Test luồng đồng ý, xuất đúng dữ liệu của chủ tài khoản, xóa theo chính sách đã chốt, không để bản ghi mồ côi; không coi đồng ý UI tự nó là đủ tuân thủ pháp luật |
| BL-51 / P0 / MVP-D / S42 | CI, backup/restore và deploy; .github/workflows/**, deploy/**, docs/operations/** | BL-50 | L8 có nơi deploy, điều kiện miễn phí hiện tại, khóa, mục tiêu khôi phục; phạm vi bảo vệ được duyệt | CI chạy suite thật xanh; thử restore backup trên môi trường giả đối chiếu được dữ liệu và khóa cần thiết; smoke test deploy; không cloud dữ liệu thật khi thử nghiệm |
| BL-60 / P2 / Mở rộng / S41E | Excel/PDF tham khảo; Web, TW, DQ | BL-42 | Có L9 về cột/mẫu và chọn thư viện phù hợp điều kiện miễn phí | File mở được, số liệu khớp kết quả/chi tiết; ghi tham khảo, không gọi là tờ khai chính thức; không lộ dữ liệu người khác |
| BL-61 / P2 / Mở rộng / S43E | XML quyết toán; Web, TW, DQ | BL-60 | Có mẫu/schema hiện hành và công cụ import chính thức từ L9 | XML hợp lệ theo schema cung cấp và import thử thành công; không tự đoán cấu trúc |

Nếu không chọn một loại thu nhập mở rộng, bỏ riêng nhánh tương ứng và cập nhật phụ thuộc khi lập phiếu; không dùng sự thiếu dữ liệu của nhánh đó để chặn BL-30/MVP lương. PostgreSQL là lựa chọn mở rộng sau khi đánh giá nhu cầu tại L8, chưa có phiếu di chuyển DB trong MVP; cần kế hoạch/test/rollback riêng nếu chọn.

## Trình tự đề nghị và kiểm chứng
Bắt đầu bằng BL-00 → BL-01 sau khi người dùng xem roadmap; chuẩn bị L1 cho BL-11 song song về tài liệu, chưa chạy bước ứng dụng trước S0. Không cần trả lời đầu vào luật của năm/BH/vãng lai/XML để hoàn tất roadmap hoặc Bước 0.

Lượt hiện tại chỉ kiểm hợp đồng bằng bint task lint 01M7WZSJJDNS1D97A7J19YN37T --strict và so hash phạm vi. Các lệnh dotnet test cho Core/solution là đề xuất trong plan.md cho lượt thực thi khi SDK/solution có thật; chưa chạy, không được báo là đã xanh.

