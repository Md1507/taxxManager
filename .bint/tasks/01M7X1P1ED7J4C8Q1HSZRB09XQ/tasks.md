# Công việc

Các việc dưới đây dành cho lượt triển khai S0; chưa thực hiện ở b-req. Làm BL-00/BL-01 trong phạm vi phiếu, reviewer theo flow hiện hành, không mở Bước 1.1.

- [ ] TSK-1 [AC-3]: Chụp hiện trạng và bổ sung .gitignore trước tải SDK — xong khi: khối Bint giữ nguyên; ma trận git check-ignore trong plan có đầy đủ match cho SDK/bin/obj/IDE/test/secrets và không match cho source/tests/global.json/mẫu an toàn; không tạo file giả trong src/tests.
- [ ] TSK-2 [AC-1]: Kiểm lại metadata và cài SDK 10.0.404 win-x64 từ ZIP Microsoft vào .dotnet — xong khi: SHA512 ZIP khớp entry metadata đúng version/RID trước giải nén, host local --info xác nhận x64/base path và --list-sdks liệt kê 10.0.404; nguồn/hash/lệnh/kết quả và Git/editor sẵn có được ghi làm bằng chứng AC-1; .NET hệ thống giữ nguyên.
- [ ] TSK-3 [AC-2]: Pin global.json và kiểm SDK từ root — xong khi: sdk.version là 10.0.404 đã cài, allowPrerelease false, rollForward latestPatch; host tuyệt đối và dotnet sau kích hoạt đều trả 10.0.404 từ root, Get-Command trỏ host local; có bằng chứng lệnh/kết quả AC-2.
- [ ] TSK-4 [AC-4]: Tạo helper PowerShell theo phiên và tài liệu setup tiếng Việt — xong khi: scripts/dev/activate-dotnet.ps1 báo rõ SDK thiếu trước đổi môi trường, dùng PSScriptRoot, kích hoạt lặp không nhân PATH và -Deactivate khôi phục nguyên trạng; docs/decisions/0001-dev-setup.md có nguồn/hash, cách cài lại đã kiểm, hướng dẫn terminal VS Code/Codex, kiểm SDK và quyết định TaxManager.sln cho bước sau; có bằng chứng kiểm helper/tài liệu AC-4.
- [ ] TSK-5 [AC-5]: Đối chiếu phạm vi, ghi bằng chứng và hoàn tất review S0 — xong khi: từng AC-1 đến AC-5 có bằng chứng cụ thể trong bint task criteria; PLAN.md/config khớp hash trước sửa, không có solution/project/src/tests/test giả hay thay đổi môi trường lâu dài, SDK/secrets bị ignore; reviewer đọc cả tệp untracked và bằng chứng, không còn phát hiện nghiêm trọng chưa xử lý; verify.command vẫn trống và không agent nào commit/tự approve.

