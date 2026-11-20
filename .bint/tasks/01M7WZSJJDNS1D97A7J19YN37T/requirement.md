# Yêu cầu

## Mục tiêu và phạm vi
Phiếu 01M7WZSJJDNS1D97A7J19YN37T chỉ tạo roadmap và backlog tiếng Việt từ PLAN.md hiện tại cho web quản lý thuế cá nhân .NET/C#. Chỉ ghi requirement.md, plan.md, tasks.md qua Bint trong phạm vi .bint/tasks/**; Bint lưu thêm bản runtime ngoài repository. Không có phần triển khai ứng dụng trong phiếu này.

Đầu ra phân biệt rõ: kết quả lập kế hoạch hiện tại; backlog thực thi tương lai; bản MVP dùng local và điều kiện trước khi triển khai có người dùng/dữ liệu thật. Phát triển bằng Codex theo cấu hình sẵn có: planner/reviewer gpt-6.1-sol, implementer gpt-6.1-terra; người dùng cung cấp/đối chiếu nguồn pháp lý và đọc lại nghiệp vụ, bảo mật.

## Hiện trạng và khoảng cách
Đối chiếu ngày 21/11/2026, giờ Việt Nam:
- Danh sách tệp qua rg --files --hidden và thư mục gốc không có src/, tests/, .sln, .slnx, .csproj hay global.json; mới có PLAN.md và cấu hình/skills/ledger Bint. Cấu trúc solution trong PLAN.md là mục tiêu, chưa được tạo.
- git rev-parse --is-inside-work-tree trả true; git log -1 báo nhánh master chưa có commit. Git đã khởi tạo, không cần làm lại git init.
- dotnet --list-sdks thoát 0, không in SDK nào trong môi trường hiện tại; chưa thể build/test ứng dụng. .gitignore chỉ có khối Bint, chưa có quy tắc .NET.
- .bint/config.yml có roles dùng Codex và model như trên; eval.test_command chưa đặt. Giữ nguyên cấu hình.
- bint task list --json hiện có một phiếu chưa kết thúc là phiếu này. Ledger còn phiếu 01M7X0ADTRPA8G6VN3H7J474JY đã done, review không có phát hiện: chỉ bổ sung PLAN.md, không phải đã xây tính năng.
- PLAN.md có mục 1.8 và các nối việc tại Bước 1.1, 1.2, 1.5, 2.2, 3.1. Trên Windows, tham chiếu plan.md ở gốc trỏ cùng PLAN.md, không có một kế hoạch thứ hai độc lập.
- MCP context_for_task không trả tri thức liên quan; không áp dụng mục [k:] nào. Mọi dữ liệu luật, hiệu lực, bảng ví dụ trong PLAN.md vẫn là đầu vào cần xác minh trước khi dùng thật.

Khoảng cách cần lấp: môi trường và solution; engine và bộ quy tắc theo kỳ; dữ liệu và bảo mật; web MVP; quyền dữ liệu, sao lưu và triển khai. Chưa có bằng chứng chạy được cho bất kỳ phần nào.

## Yêu cầu chức năng
1. Đánh giá hiện trạng bằng bằng chứng đọc được, rồi ánh xạ roadmap vào từng bước của PLAN.md; không suy ra chức năng đã tồn tại từ checklist.
2. Chi tiết Bước 0 và 1.1: môi trường, khung Core/tests, TaxRuleSet, PitCalculator, MonthlySalaryCalculator, metadata pháp lý, test và điều kiện dữ liệu được phép dùng. Các bước sau chỉ lập backlog và đề xuất phiếu riêng, không thiết kế chi tiết trước.
3. Mỗi việc tương lai có mã BL, ưu tiên, MVP/mở rộng, phụ thuộc, phạm vi dự kiến, điều kiện bắt đầu và xong khi đo được. Không đánh dấu backlog tương lai hoàn thành.
4. Chia MVP: lõi lương/tháng/năm và luật theo kỳ; lưu dữ liệu, bảo mật, web local và trích dẫn theo ngữ cảnh. Đăng nhập/phân quyền là điều kiện trước khi mở cho nhiều người; quyền dữ liệu và sao lưu là điều kiện trước dữ liệu thật/deploy. Loại thu nhập khác, PostgreSQL, báo cáo Excel/PDF và XML là phần mở rộng; đây là đề xuất phân kỳ để người dùng xem.
5. Mỗi quy tắc có metadata căn cứ độc lập Web/DB: ID, điểm/khoản/điều/số văn bản, giải thích 2-3 câu, URL văn bản gốc vbpl.vn, rule/version, kỳ/ngày áp dụng, trạng thái/ngày xác minh. Kết quả liên kết đúng căn cứ thực sự dùng; không tải nguồn từ mạng lúc tính.
6. Web tương lai có nút ? cạnh từng thành phần kết quả, popover giải thích và liên kết gốc; mở/đóng được bằng bàn phím và dùng trên mobile. Thiếu nguồn/URL hoặc chưa xác minh phải hiện rõ, không tạo căn cứ giả. Có test mapping theo kỳ, URL sai và trạng thái dữ liệu.
7. Ví dụ người dùng: 2.500.000 đồng vãng lai, khấu trừ 250.000 đồng (10%), dẫn điểm i khoản 1 Điều 25 Thông tư 111/2013/TT-BTC chỉ là minh họa UX chờ xác minh. Chưa triển khai vãng lai ở Bước 1.1; không suy ra hiệu lực hay URL chi tiết từ ví dụ.
8. Liệt kê nguồn chính thức và quyết định người dùng cần cung cấp trước từng chặng; thiếu dữ liệu giai đoạn sau không chặn việc lập roadmap hiện tại. Xác định việc bắt đầu ngay và cách kiểm chứng.

## Tiêu chí chấp nhận
- [ ] AC-1: Có đánh giá hiện trạng repository và roadmap theo giai đoạn bám PLAN.md, phân biệt MVP với phần mở rộng.
- [ ] AC-2: Có backlog với ưu tiên, phụ thuộc, phạm vi, điều kiện bắt đầu và tiêu chí hoàn thành kiểm chứng được; chi tiết chặng Bước 0 và Bước 1.1.
- [ ] AC-3: Có danh sách dữ liệu pháp lý và quyết định cần bổ sung theo từng chặng, không tự điền luật chưa được xác minh.
- [ ] AC-4: Ba tài liệu requirement.md, plan.md, tasks.md bằng tiếng Việt được lưu gắn với phiếu và vượt qua bint task lint --strict; không sửa PLAN.md hoặc mã ứng dụng.
- [ ] AC-5: Roadmap và backlog bao phủ Trích dẫn pháp lý theo ngữ cảnh: metadata theo quy tắc và kỳ áp dụng, giải thích 2-3 câu cạnh kết quả qua nút ?, liên kết văn bản gốc vbpl.vn, test mapping và dữ liệu chưa xác minh; ví dụ 10% được ghi rõ là minh họa chờ xác minh.

## Ngoài phạm vi
Sửa PLAN.md, cấu hình model, mã nguồn hay test ứng dụng; cài SDK/công cụ; tạo phiếu triển khai; chạy b-feat, dotnet test, commit hoặc deploy. Không xác nhận nội dung/hiệu lực luật, không tư vấn thuế, không xây kho luật toàn văn. Các lệnh triển khai trong tài liệu chỉ là đề xuất tương lai.

## Câu hỏi mở
Không có câu hỏi cản trở hoàn thành việc lập roadmap. Các đầu vào chưa có được đặt tại bảng điều kiện theo chặng trong plan.md, không coi là câu hỏi chặn phiếu lập kế hoạch.

