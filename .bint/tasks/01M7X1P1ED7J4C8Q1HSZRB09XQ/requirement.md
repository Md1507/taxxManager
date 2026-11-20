# Yêu cầu

## Mục tiêu và phạm vi
Thực hiện riêng S0, Bước 0 BL-00/BL-01 theo roadmap 01M7WZSJJDNS1D97A7J19YN37T và PLAN.md. Đầu ra triển khai gồm SDK project-local, pin SDK, ignore và hướng dẫn setup tiếng Việt. Lượt b-req hiện tại chỉ ghi ba tài liệu tác vụ; không tải/cài SDK hay tạo helper.

Đã đọc requirement/plan/tasks của roadmap phụ thuộc. Phiếu roadmap có đủ bằng chứng đối chiếu tài liệu, nhưng CLI hiện còn báo in_progress; Bint Core/orchestrator quản lý điều kiện chuyển bước, planner không đổi trạng thái. Không có parent trên phiếu S0.

## Hiện trạng
Repository chưa có solution, project, src, tests, global.json hoặc .dotnet; .gitignore chỉ gồm khối Bint. Git đã khởi tạo, các đầu vào còn untracked nên cần so hash, không chỉ git diff. Theo kiểm tra orchestrator: Git 2.47.1 và VS Code đã có; .NET hệ thống chỉ có runtime 8.0.34 x64, thiếu SDK. .bint/config.yml xác nhận các role dùng Codex, giữ nguyên model. MCP không có tri thức liên quan để áp dụng.

## Yêu cầu chức năng
1. Kiểm lại metadata Microsoft và cài đúng SDK ổn định 10.0.404 win-x64 vào .dotnet bằng archive chính thức; so SHA512 trước giải nén và ghi nguồn/giá trị/kết quả. Không thay SDK đã chọn chỉ vì latest-sdk thay đổi; nếu bản chỉ định không còn xác minh được thì báo trở ngại.
2. Chỉ dùng host .dotnet/dotnet.exe hoặc PATH/DOTNET_ROOT của phiên. Giữ nguyên .NET hệ thống, PATH user/machine, registry, profile và cấu hình/model Codex; ghi nhận Git/editor sẵn có.
3. Sau cài thành công, tạo global.json với sdk.version khớp bản đã cài, allowPrerelease false, rollForward latestPatch. Kiểm từ root trả đúng bản pin; chính sách chỉ cho patch trong feature band 10.0.4xx.
4. Giữ nguyên từng dòng khối Bint; bổ sung ignore cho SDK, bin/obj mọi cấp, IDE state, kết quả test và secrets/.env. Giữ khả năng theo dõi src/tests/global.json, .env.example và appsettings mẫu an toàn; không ignore toàn bộ appsettings*.json hay thư mục mã.
5. Tạo helper nhẹ scripts/dev/activate-dotnet.ps1, dùng đường dẫn theo PSScriptRoot, kích hoạt bằng dot-source trong terminal PowerShell VS Code/Codex. Thiếu SDK phải hướng dẫn cài rõ và không đổi môi trường; chạy lặp không nhân PATH; có cách khôi phục đúng PATH/DOTNET_ROOT trước kích hoạt, kể cả biến ban đầu chưa tồn tại.
6. Ghi docs/decisions/0001-dev-setup.md tiếng Việt: phiên bản, nguồn/hash, các lệnh cài lại đã kiểm, kích hoạt/khôi phục theo phiên, kiểm dotnet từ root; quyết định dùng TaxManager.sln khi thực hiện Bước 1.1 sau này.
7. Implementer kiểm hẹp và ghi lệnh, kết quả, mã thoát vào bằng chứng tiêu chí tương ứng; reviewer đọc diff/tệp mới và bằng chứng. verify.command giữ trống, không tự approve; không coi kế hoạch hay lint xanh là bằng chứng đã cài.

## Tiêu chí chấp nhận
- [ ] AC-1: SDK .NET10 ổn định x64 được cài trong .dotnet từ nguồn Microsoft; --info và --list-sdks có phiên bản đã chọn, Git và editor đã có được ghi nhận.
- [ ] AC-2: global.json pin phiên bản SDK thực sự được cài, allowPrerelease false và rollForward latestPatch; dotnet --version từ root trong môi trường kích hoạt khớp pin.
- [ ] AC-3: .gitignore giữ nguyên khối Bint, loại .dotnet, bin/obj, IDE state, test results và secrets; git check-ignore chứng minh đường dẫn phát sinh bị ignore còn source/tests/global.json và mẫu cấu hình an toàn không bị ignore.
- [ ] AC-4: Có tài liệu setup tiếng Việt trong docs/decisions với phiên bản/nguồn, cách cài và kích hoạt lặp lại được, kiểm SDK, quyết định solution .sln cho bước sau; helper nếu có chỉ thay đổi môi trường phiên và xử lý rõ SDK thiếu.
- [ ] AC-5: Chỉ thay đổi trong phạm vi Bước0; không đổi PLAN.md/config Codex, không tạo solution/src/tests hay test giả; từng tiêu chí có bằng chứng kiểm chứng phù hợp và review không còn lỗi nghiêm trọng.

## Ngoài phạm vi
Bước 1.1 và BL-10 trở đi: tạo solution/project/src/tests, restore/build/test ứng dụng, nghiệp vụ thuế, Web/DB, migration/CI. Không cài lại Git/editor, git init, commit, sửa PLAN.md/.bint/config.yml hoặc cài phần mềm toàn máy. Quy tắc test/commit chung trong PLAN.md được yêu cầu hiện tại cụ thể hóa thành kiểm setup và bàn giao orchestrator.

## Câu hỏi mở
