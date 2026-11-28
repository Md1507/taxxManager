# Web MVP 3.1 — demo local

## Increment 3.1b — xem trước đầu vào

Tại `/Forecast`, nhập gross theo một hoặc nhiều giai đoạn; tháng ngoại lệ được giữ,
tháng chồng giai đoạn được thông báo. Mở phần chi tiết để nhập bảo hiểm, người phụ
thuộc và khấu trừ từng tháng cùng trạng thái, nguồn và ghi chú. Preview phân biệt
chưa nhập với 0; thời điểm nhập UTC chỉ đổi khi giá trị hoặc metadata thay đổi.
Chặng 2 đã nghiệm thu kỹ thuật theo bốn tiêu chí của phiếu `01M8BK6R1EC84DFJ05Z8FH31C2`, done và land ngày 26/11/2026, tích hợp tại `f4f3133`: lưu, mở, sửa và xóa hồ sơ dự báo synthetic. Danh sách hồ sơ nằm ngay tại `/Forecast`; mở lại cùng kho qua `SyntheticStore` như hướng dẫn bên dưới.
Chặng 3 đã có luồng tính năm từ hồ sơ đã lưu, đã nghiệm thu kỹ thuật ngày 28/11/2026 (Bint done, 629 test và EF sạch, review vòng 4 không có phát hiện); xem [kiểm chứng](reviews/2026-11-27-annual-stage-three.md). Chỉ dùng dữ liệu giả.

## Tính năm từ hồ sơ đã lưu — chặng 3

Tại `/Forecast`, nhập lương và các khoản bảo hiểm/người phụ thuộc theo tháng. Có thể nhập người phụ thuộc theo giai đoạn; ngoại lệ tháng vẫn ưu tiên. Mở phần thưởng để nhập thưởng tiền thực tế/dự kiến từ nơi trả hiện tại, chọn đã gồm gross để tránh cộng trùng. Mỗi giá trị giữ trạng thái, nguồn, ghi chú và ngày nhập. Để trống là chưa biết; chỉ nhập 0 khi xác nhận không phát sinh.

Bấm **Lưu hồ sơ**, rồi **Tính thuế năm**. Thay đổi chưa lưu cần lưu trước khi tính. Thiếu gross, bảo hiểm hoặc người phụ thuộc sẽ hiện tháng/chỉ tiêu cần bổ sung và không lưu kết quả giả. Thiếu khấu trừ vẫn tính nghĩa vụ nhưng chưa có chênh lệch. Kết quả có bước tính năm, bậc thuế, dữ liệu thực tế/dự kiến/giả định, X/12 tháng đầy đủ thực tế và độ phủ khấu trừ riêng. Nguồn luật giữ trạng thái hiện có; nộp thừa ước tính không bảo đảm được hoàn.

Mỗi lần tính tạo kết quả mới. **Mở lịch sử tính năm** đọc snapshot đã lưu, kể cả sau khi xóa hồ sơ thông thường nếu giữ liên kết lịch sử. Lịch sử không bị tính lại theo catalog hay hồ sơ mới. Chỉ kho synthetic hợp lệ được mở lại/nâng cấp; khóa hoặc sentinel sai/thiếu gây lỗi, không tạo lại tự động. Báo cáo/checklist, sao lưu/khôi phục và kịch bản thưởng đã có cho local synthetic; đối chiếu nhập tay theo nguồn/kỳ đã có, chưa lưu phép đối chiếu.

## Hai kịch bản — chặng 5

Trong **Tổng quan năm** của hồ sơ đã lưu, chọn **So sánh khi thêm thưởng giả định**. Tại `/ho-so/{id}/kich-ban`, nhập tháng nhận và số tiền thưởng muốn thử, rồi chọn **So sánh**. Phép thử thêm một khoản thưởng tiền chịu thuế ngoài gross, giữ các khoản đã lưu và không cộng lại thưởng đã gồm trong gross. Hai kết quả dùng cùng revision và catalog; revision đổi thì mở lại bản mới trước khi so sánh.

Ô trống chưa tạo kịch bản thứ hai; nhập 0 hợp lệ và có thể cho nghĩa vụ không đổi. Thiếu gross/bảo hiểm/người phụ thuộc sẽ hiện chỉ tiêu/tháng cần bổ sung. Thiếu khấu trừ không chặn nghĩa vụ; không suy số nộp thêm/nộp thừa cho thưởng giả định. Phép thử không lưu vào hồ sơ hoặc lịch sử. Khi sửa khoản thưởng sau khi so sánh, nhãn nhắc cập nhật kết quả sẽ hiện. Xem [contract](design/forecast-scenarios.md), [kiểm chứng và giới hạn](reviews/2026-11-28-forecast-scenarios.md).

## Đối chiếu nguồn — chặng 6

Từ **Tổng quan năm**, chọn **Đối chiếu với số công ty hoặc eTax**, hoặc mở **Đối chiếu nguồn** ở sidebar của hồ sơ đã lưu. Nhập nguồn/tài liệu, năm, tháng đầu/cuối, chỉ tiêu, số tiền và trạng thái rồi chọn **Đối chiếu số liệu**. Nhập cùng tháng đầu/cuối để so một tháng; 1–12 để so cả năm. Số nguồn cần cùng nghĩa với chỉ tiêu: gross đã nhập không cộng thưởng ngoài gross; khấu trừ không phải thuế ứng dụng tính.

Kết quả dùng số nguồn trừ số hồ sơ; dấu + là nguồn cao hơn, dấu − là thấp hơn. Thiếu tháng hoặc khác năm không tạo chênh lệch giả. Nguồn không quyết định trạng thái Thực tế/Dự kiến/Giả định; số giả định cần ghi chú. Phép đối chiếu giữ metadata trong kết quả đang mở và không lưu số nguồn vào hồ sơ/lịch sử. Không kết luận công ty hoặc eTax sai hoặc coi chênh lệch là tiền hoàn/nộp thêm. Xem [contract](design/forecast-reconciliation.md) và [kiểm chứng](reviews/2026-11-28-forecast-reconciliation.md).

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
Chế độ mặc định không có đăng nhập; ID nội bộ không phải xác thực hay phân quyền. Chỉ dùng
trên loopback trong môi trường Development với opt-in trên; không triển khai công khai.

Đ6a có chế độ thử tài khoản riêng: thêm `--SyntheticAccounts=true` cùng `--SyntheticDemo=true`. App chỉ lắng nghe HTTPS loopback, mặc định `https://localhost:5088`, cần chứng chỉ development hợp lệ và được trình duyệt tin cậy. Agent không tự thay trust store. Mở `/tai-khoan/dang-ky`, tạo tên đăng nhập/mật khẩu thử rồi đăng nhập. Kho mỗi tài khoản bắt đầu trống; không tự nhận hồ sơ demo cũ. Để mở lại tài khoản ở lần chạy sau, giữ nguyên root đã in cùng `--SyntheticStore`, thư mục `accounts` và các kho/khóa riêng.

Đ6g thêm Google opt-in: Google mới tạo tài khoản không mật khẩu sau khi xác nhận dữ liệu thử; Google đã biết mở cùng kho. Tài khoản local liên kết Google từ trang tài khoản, không hỏi lại mật khẩu. Google-only xác nhận xuất/xóa bằng Google; không cho gỡ credential duy nhất. Chỉ có nút khi Google được bật; cần OAuth Web client và callback `/signin-google` đúng host/port HTTPS. Hướng dẫn giữ secret ngoài repository và giới hạn tại `docs/design/account-sign-in-options.md`. Email đặt lại mật khẩu được bổ sung ở Đ6h dưới đây; passkey chưa triển khai.

Đ6h (29/11): đã có quản lý/xác minh email và đặt lại mật khẩu qua liên kết 30 phút dùng một lần, thu hồi phiên cũ; email cũ giữ tới khi xác minh mới. Thêm/đổi/gỡ cần mật khẩu hoặc Google đúng chủ/mục đích; Google-only có thể tạo mật khẩu riêng qua email đã xác minh. Gửi thư opt-in qua Resend, cần sender/key/origin HTTPS loopback; mặc định tắt, kiểm chứng dùng sender giả. Giữ xanh/Inter, một primary mỗi trạng thái, kiểm mobile/desktop. Thông báo dữ liệu thử v5, export không có token/nonce. Contract/cấu hình `docs/design/account-sign-in-options.md`, kiểm chứng `docs/reviews/2026-11-29-email-recovery.md`. Liên kết mất hiệu lực sau restart do key ring development ephemeral; dữ liệu vẫn giữ. Passkey/mã dự phòng là bước tiếp theo; chưa nghiệm thu email thật hoặc vận hành qua mạng.

**Khi bàn giao bản thử toàn bộ tính năng, dùng chế độ tài khoản này.** Sau phần chuẩn bị môi trường ở trên, chạy:

```powershell
dotnet run --project src/TaxManager.Web -- --SyntheticDemo=true --SyntheticAccounts=true
```

Không đưa link demo chỉ có `SyntheticDemo=true` như bản thử đầy đủ: chế độ đó cố ý ẩn các route tài khoản và không thể kiểm đăng ký/đăng nhập, thông báo dữ liệu thử hoặc xuất dữ liệu tài khoản. Trước bàn giao, kiểm chuyển tới đăng nhập khi chưa xác thực, vào hồ sơ sau đăng nhập, liên kết Tài khoản và form Xuất dữ liệu tài khoản; đồng thời kiểm thay đổi UI mới trên chính host HTTPS đó. Nếu công cụ browser chưa tin chứng chỉ local, ghi rõ trở ngại này, không thay bằng ảnh mirror rồi gọi đó là bản ứng dụng đầy đủ chạy được. Bản mirror chỉ dùng kiểm visual.

Đ6c đã có `/tai-khoan/xuat-du-lieu`: chủ tài khoản xác nhận lại mật khẩu để tải JSON đọc được từ kho gốc; kho khôi phục và tệp sao lưu có mật khẩu là luồng riêng. Xóa toàn bộ tài khoản vẫn chưa có. [Contract và phạm vi](design/account-data-export.md).

Cookie phiên hết hạn sau 30 phút hoặc khi app khởi động lại. Đăng xuất ở `/tai-khoan` không xóa dữ liệu. Backup thuế chỉ chứa kho đang dùng của tài khoản, không chứa registry/tên đăng nhập/hash mật khẩu; chưa phải bản sao toàn bộ hệ thống tài khoản. Không có reset mật khẩu, email, MFA, xuất/xóa toàn bộ tài khoản hoặc triển khai qua mạng ở bước này. [Contract](design/local-accounts.md), [kiểm chứng và giới hạn](reviews/2026-11-28-local-accounts.md).

Đăng ký yêu cầu đọc và chọn đồng ý dùng dữ liệu thử; checkbox không chọn sẵn. Tài khoản cũ chưa xác nhận được chuyển tới `/tai-khoan/du-lieu-thu` trước khi mở hồ sơ; form dữ liệu gửi trước xác nhận bị chặn. Phiên bản/thời gian xác nhận lưu ở server, có thể đọc lại từ trang tài khoản. Đây là thông báo cho bản thử local, chưa phải nghiệm thu pháp lý cho dữ liệu thật. [Contract Đ6b](design/local-data-use.md).

Nguồn References giữ nguyên Missing/Unverified/Verified và không khẳng định đã kiểm
chứng pháp lý. Link chỉ lấy từ metadata HTTPS hợp lệ; kỳ không có luật được hỗ trợ
không fallback. Trang tính tháng `/` giữ phạm vi cũ; trang `/Forecast` hỗ trợ lương/thưởng tiền một nơi trả, bảo hiểm nhập tay và người phụ thuộc theo tháng/giai đoạn. Kịch bản thưởng và đối chiếu nhập tay đã có trên hồ sơ đã lưu. Không hỗ trợ nhiều nguồn lương hay tự cập nhật luật bằng AI. Không nhập
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


UI cập nhật ngày 28/11/2026: các route hiện có dùng layout/sidebar, Inter tự host và bộ token trong `wwwroot/css/tokens.css`. Ô tiền tự tách hàng nghìn ngay khi gõ, ví dụ `12344556` thành `12.344.556`; phần lẻ dùng dấu phẩy. Form JS gửi dấu định dạng vi-VN để server xác thực nhóm chấm đúng ba chữ số; form cũ/không JS giữ giao thức không phân nhóm, không đổi nghĩa dữ liệu đã lưu. Đã bổ sung chú thích gross/net, giảm vạch chia phần và giữ phân biệt thiếu với 0. Xem [báo cáo UI và giới hạn](reviews/2026-11-28-ui-phases-one-two.md).

Tại bảng 12 tháng của hồ sơ đã lưu, chọn **Chi tiết** ở hàng tháng tương ứng để đọc thuế tháng, thưởng, đầu vào và căn cứ. Route `/ho-so/{id}/thang/{month}?revision={revision}` dùng bản lưu tường minh; hồ sơ đổi revision thì mở lại qua bảng. **Sửa** quay về editor đúng tháng; lưu vẫn dùng nút Lưu hồ sơ. Kết quả tháng tính khi mở, chưa lưu vào lịch sử; lịch sử năm giữ nguyên. Thiếu khấu trừ không chặn nghĩa vụ nhưng không có chênh lệch; không cộng thuế tháng để thay nghĩa vụ năm. Xem [contract và kiểm chứng màn tháng](reviews/2026-11-28-forecast-month.md).

Sao lưu local: vào `/sao-luu`, đặt và nhập lại mật khẩu 12–128 ký tự, chọn **Tải bản sao**. Tệp `.taxbackup` giữ toàn bộ kho, gồm dữ liệu, phiên bản, kết quả, audit, luật và khóa đọc nằm trong gói mã hóa. Giữ mật khẩu ở nơi riêng; không có chức năng phục hồi mật khẩu. Không dùng CSV báo cáo để khôi phục.

Mở `/khoi-phuc`, chọn tệp và nhập mật khẩu → **Kiểm tra bản sao** → đối chiếu ngày/số lượng/ID trùng → **Mở kho khôi phục**. App mở kho riêng, không merge hay ghi đè kho ban đầu. Khi đang dùng kho khôi phục, vào Sao lưu để tải các chỉnh sửa hoặc **Quay lại kho ban đầu**. Form mở ở kho cũ bị từ chối sau chuyển kho; hãy mở lại trang trước khi gửi.

Bản xem trước hết hạn sau 10 phút hoặc khi hủy. Kho đã xác nhận được giữ dưới TEMP, nhưng lựa chọn trình duyệt không tồn tại qua restart; dùng bản sao hoặc SyntheticStore cùng root/key để mở lại. Vẫn chỉ synthetic/Development/loopback; không hứa xóa vật lý. [Contract](design/protected-backup.md), [kiểm chứng và giới hạn](reviews/2026-11-28-protected-backup.md).
