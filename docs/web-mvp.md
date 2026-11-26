# Web MVP 3.1 — demo local

## Increment 3.1b — xem trước đầu vào

Tại `/Forecast`, nhập gross theo một hoặc nhiều giai đoạn; tháng ngoại lệ được giữ,
tháng chồng giai đoạn được thông báo. Mở phần chi tiết để nhập bảo hiểm, người phụ
thuộc và khấu trừ từng tháng cùng trạng thái, nguồn và ghi chú. Preview phân biệt
chưa nhập với 0; thời điểm nhập UTC chỉ đổi khi giá trị hoặc metadata thay đổi.
Chặng 2 đang triển khai lưu, mở, sửa và xóa hồ sơ dự báo synthetic; chưa nghiệm thu.
Chưa tính kết quả năm hoặc kịch bản thưởng. Chỉ dùng dữ liệu giả.

## Bước 3.1 đã nghiệm thu

Chỉ dùng dữ liệu giả. Giao diện tiếng Việt nhận một nguồn lương thông thường cho đủ
12 tháng, bảo hiểm nhập tay và thuế đã khấu trừ. Nút dữ liệu mẫu điền các số giả;
chọn năm 2025 hoặc 2026 rồi bấm **Tính 12 tháng**. Mở từng tháng và nút `?` để xem
các bước, bậc và căn cứ của snapshot. Kết quả chỉ mang tính tham khảo.

Từ thư mục repository trên PowerShell, sau khi chuẩn bị SDK và NuGet cache theo
[development.md](development.md):

```powershell
. ./scripts/dev/activate-dotnet.ps1
$env:NUGET_PACKAGES = Join-Path $PWD '.dotnet/nuget-packages'
$env:DOTNET_CLI_HOME = Join-Path $env:TEMP 'taxmanager-cli'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/TaxManager.Web -- --SyntheticDemo=true
```

Mở `http://127.0.0.1:5088/`; dừng server bằng Ctrl+C trong terminal đã chạy. Cổng
5088 phải còn trống. Không đưa thông tin cá nhân hay dữ liệu thật vào demo.

Mặc định mỗi lần khởi tạo database demo tạo thư mục ngẫu nhiên dưới TEMP, một SQLite mới
trong thư mục `data` và khóa giả tách khỏi thư mục database. Database và khóa này
không tự được xóa khi app dừng; chúng chỉ dùng cho thử nghiệm synthetic. Sau khi mở
trang `/Forecast`, terminal in đường dẫn kho. Để mở lại kho đó trong phiên mới, thêm
`--SyntheticStore="<đường dẫn kho synthetic dưới TEMP>"` sau `--SyntheticDemo=true`;
giữ nguyên file khóa và marker, không tự tạo lại khóa bị mất. App không
mở `taxmanager.local.db`, không tự provision khóa vận hành hoặc nâng cấp DB thật.
Không có đăng nhập/Identity; ID nội bộ không phải xác thực hay phân quyền. Chỉ dùng
trên loopback trong môi trường Development với opt-in trên; không triển khai công khai.

Nguồn References giữ nguyên Missing/Unverified/Verified và không khẳng định đã kiểm
chứng pháp lý. Link chỉ lấy từ metadata HTTPS hợp lệ; kỳ không có luật được hỗ trợ
không fallback. Demo không có người phụ thuộc trong form, không hỗ trợ thưởng, nhiều
nguồn lương, quyết toán, làm tròn, so sánh hay tự cập nhật luật bằng AI. Không nhập
CCCD/MST. Tiền được parse và tính bằng decimal phía server, không tính bằng JS float.

Kiểm thử mục tiêu trong quá trình phát triển, sau restore cache:

```powershell
dotnet test tests/TaxManager.Web.Tests/TaxManager.Web.Tests.csproj --no-restore --disable-build-servers -m:1
dotnet build TaxManager.sln --no-restore --disable-build-servers -m:1
dotnet test tests/TaxManager.Core.Tests --no-restore --disable-build-servers -m:1
dotnet test tests/TaxManager.Data.Tests --no-restore --disable-build-servers -m:1
```

`scripts/dev/verify-web-mvp.ps1` còn kiểm tra EF pending model changes. Lệnh trong
phiếu đã được sửa thành `powershell -NoProfile -ExecutionPolicy RemoteSigned -File
scripts/dev/verify-web-mvp.ps1` để áp dụng policy cho riêng tiến trình PowerShell;
Bint ghi nhận lệnh này đã được xác nhận. Script khôi phục
local tool từ manifest với cache NuGet của dự án trước khi kiểm tra EF, vì Bint tạo
CLI home mới cho mỗi lượt. Ngày 24/11/2026, cổng Bint verify đã qua với build không
warning/error, 527 test (Core 197, Data 300, Web 30) và EF không lệch migration.
Review độc lập vòng 6 xác nhận lỗi tool resolver đã sửa và không ghi nhận phát hiện
mới. Không coi nghiệm thu kỹ thuật này là security approval cho dữ liệu thật.

QA Orca của root ngày 24/11/2026 sau sửa mobile dùng app thật với bản build sao chép:
nhập mẫu, xem 12 kết quả, mở căn cứ trên viewport 320px, 390px và desktop 1280px;
ảnh được xem lại, nhãn và số tiền ở 320px đọc được, không tràn ngang trang. Thao tác
Tab/Enter/Space/Escape và trả focus đã được kiểm. Viewport giả lập không phải thiết bị
mobile vật lý; chưa kiểm screenreader hay zoom native 200%. Review vòng 3 sau đó phát
hiện đường `dotnet run` chuẩn trả 404 cho CSS/JS; sau sửa, HTTP GET `/`, `/site.css`
và `/citation.js` đều trả 200 với nội dung. Root đã smoke lại đường chạy chuẩn ngày
24/11/2026; bằng chứng chi tiết nằm trong lịch sử browser-qa của phiếu Bint.
