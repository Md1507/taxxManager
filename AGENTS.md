<!-- bint:begin -->
<!-- Khối do Bint quản lý. Không sửa tay: sửa tệp .bint/knowledge/items/<id>.md
rồi chạy `bint bootstrap publish`. Mã [k:<id>] để trích dẫn mục tri thức. -->

## Quy ước của dự án (Bint)

Ghi lại điều bất thường vừa phát hiện (lệnh đặc biệt, chỗ dễ sai) bằng
`bint knowledge add --type warning --paths '<glob>' --evidence <tệp>:<dòng> "<nội dung>"`;
mục do agent ghi chờ người duyệt, không tự có hiệu lực.

Sau khi sửa mã ngoài tác vụ Bint, `bint test affected --run` chạy các test mà thay đổi chưa commit có thể ảnh hưởng
(bgraph chọn; không thu hẹp được thì chạy cả bộ) thay cho cả bộ test; CI vẫn là lần kiểm cuối.

### Quy tắc bắt buộc

Khi làm việc với `src/TaxManager.Data/**`, `tests/TaxManager.Data.Tests/**`:

- **Xác thực khóa tường minh khi mở lại DB đã khởi tạo** [k:01M85QGMTJVNX37D5XYH5161KM]
  Gọi `TaxManagerDbContext.ValidateProtection()` khi mở lại DB đã có sentinel, kể cả chỉ đọc Count/ID hoặc truy vấn không có kết quả; các truy vấn metadata không tự kiểm tra khóa.
  Với DB mới chưa có sentinel, để lần protected Save đầu tiên khởi tạo trong transaction; không gọi ValidateProtection trước lần ghi đó.
  Giữ lỗi thiếu/sai khóa hoặc thiếu sentinel; không reset metadata hay thay dữ liệu lỗi bằng giá trị mặc định.

Khi làm việc với `src/TaxManager.Data/**`, `tests/TaxManager.Data.Tests/CalculationHistory/**`:

- **Tạo lịch sử qua service và giữ bản ghi riêng cho mỗi lần tính lại** [k:01M85R5FV73D0YXHBS25P7X4XA]
  Tạo bản ghi bằng CalculationHistoryService.CreateMonthlyAsync với context không có pending writes; service tự đọc dữ liệu đã lưu và catalog trong cùng transaction rồi gọi Core.
  Không Add/Update/Remove CalculationRecord trực tiếp hoặc ghi kết quả do caller cung cấp; SaveChanges chặn các thao tác này.
  Đọc lại snapshot đã lưu để giữ nguyên kết quả và căn cứ lúc tính. Khi tính lại cùng tháng, tạo bản ghi mới qua service để có thể đối chiếu; không ghi đè bản cũ.

Khi làm việc với `src/TaxManager.Data/Migrations/**`, `tests/TaxManager.Data.Tests/CalculationHistory/**`:

- **Giữ đúng byte các migration cũ, kể cả xuống dòng** [k:01M8BMXQFP0DJEQN52P2Y97AQZ]
  Thêm migration mới khi đổi schema; giữ nguyên nội dung và byte của migration/designer cũ được kiểm bằng SHA-256.
  Không lưu lại hoặc format hàng loạt các tệp đó: chỉ đổi LF/CRLF cũng làm kiểm thử thất bại.
  Nếu checkout làm lệch hash, đối chiếu byte với bản gốc và xử lý cách checkout; không cập nhật hash kỳ vọng để che sai lệch.

### Điểm dễ sai

Khi làm việc với `docs/reviews/**`:

- **Ảnh báo cáo có thể nới affected test tới full khi thiếu ánh xạ bgraph** [k:01M8F671GP7FD4E6G4CVEW6E7R]
  Nếu bint test affected --run nới scope tới full vì ảnh PNG ngoài project chưa được ánh xạ, eval.test_command null sẽ khiến lệnh không chạy test. Đọc selection.widening bằng bint test affected --json; không báo cổng này đã qua, không bỏ ảnh hoặc thu hẹp test im lặng. Trong lượt UI 28/11/2026 đã chạy scripts/dev/verify-web-mvp.ps1 trực tiếp để kiểm đầy đủ Core/Data/Web và EF, giữ nguyên cấu hình Bint.

Khi làm việc với `scripts/dev/**`, `src/**/*.csproj`, `tests/**/*.csproj`, `global.json`:

- **Chuẩn bị SDK và restore trước script kiểm chứng Web MVP** [k:01M8BMXQW27NZWH4EKHABV6NZK]
  Trong mỗi worktree mới, chuẩn bị SDK local theo activate-dotnet.ps1 và cache .dotnet/nuget-packages; nếu thiếu project.assets.json thì restore solution với phiên bản hiện tại trước khi kiểm chứng.
  Script verify-web-mvp.ps1 chỉ restore dotnet tools, còn build/test dùng --no-restore; thiếu assets là lỗi chuẩn bị môi trường.
  Chạy script tới hết Core/Data/Web và EF has-pending-model-changes; một lượt dotnet test hoặc kiểm hẹp không thay thế toàn bộ cổng này.
  Giữ pin SDK/package khi sửa lỗi môi trường; cảnh báo NU1900 không chứng minh việc kiểm tra bảo mật package đã thành công.

Khi làm việc với `src/TaxManager.Core/**`, `src/TaxManager.Data/TaxRules/**`, `tests/TaxManager.Core.Tests/**`:

- **Chọn luật theo kỳ áp dụng đã xác định từ văn bản** [k:01M85R5MDZ79Y5GYVVXKABHFKY]
  Xác định registration PeriodFrom/PeriodTo từ nội dung văn bản, phạm vi đối tượng, hiệu lực và chuyển tiếp; dùng GetForMonth/GetForDate để chọn luật cho kỳ đang tính.
  Không suy registration chỉ từ FromYear, ngày xuất bản mới nhất hoặc riêng LegalBasis.EffectiveFrom/To; điều này không cho phép bỏ qua hiệu lực pháp lý.
  Với provider hiện tại, chỉ dùng GetForYear khi đủ 12 tháng cùng một instance snapshot; hai snapshot bằng giá trị vẫn không đáp ứng ReferenceEquals.
  Giữ lỗi khi tháng thiếu hoặc năm có nhiều snapshot; không fallback luật của kỳ gần nhất.

Khi làm việc với `src/TaxManager.Data/**`, `tests/TaxManager.Data.Tests/**`:

- **Chấp nhận hoặc bỏ tracking sau SaveChanges(false) trước lần Save tiếp** [k:01M85QGSCXRZR59Q7GF09F63X0]
  Sau SaveChanges(false) hoặc SaveChangesAsync(false), gọi ChangeTracker.AcceptAllChanges(), hoặc Clear/detach các entry đã lưu/mở context mới trước lần Save tiếp theo.
  Pipeline từ chối Save khi còn entry chờ acceptance để tránh ghi audit trùng; sau AcceptAllChanges hoặc Clear có thể sửa/thêm rồi Save ngay, không cần một lần Save no-op.
- **Xóa hồ sơ thông thường vẫn giữ lịch sử và audit** [k:01M85QH0MMV8Y56ZK918ZB7QBV]
  Phân biệt ordinary delete với purge: ProfileDeletionService hoặc Remove hồ sơ giữ lịch sử tính và audit mã hóa, kể cả dữ liệu chưa load.
  Khi cần xóa khỏi các bảng đang dùng, gọi ProfilePurgeService.PurgeAsync bằng context fresh không có tracked entities, kèm target/actor/purpose tường minh; purge xử lý cả hồ sơ đã ordinary-delete.
  Không thêm cascade từ profile sang history/audit để thay purge; purge không bảo đảm xóa vật lý trong WAL/free pages/backups.

Khi làm việc với `src/TaxManager.Data/**`, `tests/TaxManager.Data.Tests/TaxRules/**`:

- **Nhận biết giới hạn adapter hiện tại khi cập nhật catalog thuế** [k:01M85R5BA80S0DDJVNMBYQ00BS]
  Adapter lưu luật lương với bảo hiểm nhập tay và hỗ trợ AnnualSalaryRules, RoundingPolicy, CashBonusRules trong phạm vi AnnualRuleGraphCodec. Các graph bảo hiểm, lương bổ sung, khấu trừ vãng lai, nhiều nguồn lương, từ thiện và hưu trí vẫn bị từ chối; mở rộng chúng bằng thay đổi riêng có kiểm thử.
  Dùng SeedReferencesAsync/LoadReferencesAsync cho đúng bộ tham khảo Core 2025/2026; đường này không nhận tùy ý luật mới hoặc mọi snapshot Unverified.
  Giữ nguyên trạng thái nguồn qua lưu/mở lại; nguồn official phải qua ValidateOfficial. Không đổi Verified hay thêm fallback để vượt cổng. Đây là giới hạn triển khai hiện tại, không phải giới hạn lâu dài của sản phẩm.

Khi làm việc với `src/TaxManager.Data/Migrations/**`, `src/TaxManager.Data/Upgrades/**`, `tests/TaxManager.Data.Tests/**`:

- **Không ép migration mã hóa chạy trên DB legacy có dữ liệu nhạy cảm** [k:01M85QH3AFYXW6P76BVMC5B43N]
  Giữ guard RequireCopyForwardForPopulatedDatabase của AddDataProtectionAndAudit: direct migration chỉ chấp nhận cả bốn bảng nhạy cảm legacy trống.
  Với thử nghiệm copy-forward, dùng SyntheticDatabaseUpgrader chỉ cho file synthetic absolute dưới TEMP, không links, syntheticDataOnly=true và target mới riêng biệt.
  Đừng bỏ guard hoặc dùng upgrader này cho DB vận hành; API hiện tại chỉ dành cho development.

Khi làm việc với `src/TaxManager.Data/Migrations/**`, `tests/TaxManager.Data.Tests/**`:

- **Thêm migration phải đối chiếu cả kỳ vọng schema mới và mốc downgrade cũ** [k:01M8CBFWCGN1RHN8ZHFV2P3M9Z]
  Khi thêm migration, rà các kỳ vọng GetAppliedMigrations ở MigrationTests, ProfilePersistenceTests và HistoryMigrationTests; test tính năng mới không bao phủ hết các kỳ vọng danh sách/số lượng cũ.
  Cập nhật kỳ vọng chạy tới schema mới nhất theo danh sách migration thực tế; giữ kỳ vọng cho các mốc migrate/downgrade tường minh và kiểm dữ liệu, audit, sentinel, trigger, idempotency.
  Trong HistoryMigrationTests, downgrade về AddDataProtectionAndAudit gỡ các migration phía sau trước khi AddCalculationHistory từ chối Down, nên sau lỗi vẫn còn bốn migration đầu. Giữ mốc này khi bổ sung migration mới; không thay đồng loạt mọi số 4 thành số mới nhất.

Khi làm việc với `src/TaxManager.Data/TaxRules/**`, `src/TaxManager.Data/Upgrades/**`:

- **Model EF có cột AnnualGraph mới nhưng đường legacy/copy-forward vẫn dùng schema trước cột này. SnapshotRows cần project…** [k:01M8E9N50FHAQEK75JVNDYXN2J]
  Model EF có cột AnnualGraph mới nhưng đường legacy/copy-forward vẫn dùng schema trước cột này. SnapshotRows cần projection NULL AS AnnualGraph cho schema cũ; insert graph null phải bỏ cột khỏi INSERT. Giữ byte migration cũ và test legacy upgrade; không bỏ guard DB legacy populated để che lỗi missing column.

Khi làm việc với `src/TaxManager.Web/**`, `tests/TaxManager.Web.Tests/**`:

- **Tiền vi-VN cần dấu định dạng và xử lý cả trường tháng ẩn** [k:01M8F49W9Q99372V01ZZBZV6NK]
  Theo yêu cầu UI ngày 28/11/2026, ô tiền tự tách hàng nghìn bằng chấm, phần lẻ bằng phẩy. Form được JavaScript định dạng gửi AmountFormat=vi-VN; MoneyInput xác thực grammar tường minh. Giao thức cũ không marker vẫn nhận chấm hoặc phẩy là thập phân để không đổi dữ liệu cũ. Khi format phải xử lý cả input tiền ẩn của các tháng chưa mở; bỏ sót có thể diễn giải 1.234 cũ thành 1234. Không dùng JS Number hoặc parse theo culture với AllowThousands.

Khi làm việc với `src/TaxManager.Web/Demo/**`, `src/TaxManager.Web/DemoApplication.cs`, `tests/TaxManager.Web.Tests/**`:

- **Kiểm chứng mở lại forecast bằng host mới dùng đúng kho synthetic cũ** [k:01M8CBFTQHZAEP0A2M9H7GWSHN]
  Để chứng minh lưu qua lần khởi động lại, dispose host đầu rồi tạo host mới trỏ cùng RootPath bằng SyntheticStore (DemoHost.StartAsync(syntheticRoot: root)); mở client mới trong cùng host chỉ kiểm một vòng đời app.
  Giữ cùng DB và synthetic-key.json: SyntheticDatabase không nhận existingRoot sẽ tạo kho và khóa ngẫu nhiên mới.
  Chỉ mở kho synthetic hợp lệ ngay dưới TEMP, giữ kiểm tra marker, reparse point và khóa; đừng tự tạo lại kho/khóa khi mở lại thất bại.
  Kiểm cả metadata/ngày nhập, thiếu khác 0 và cập nhật qua các host; giữ ca thiếu/sai khóa sau khi xóa hồ sơ.

Khi làm việc với `src/TaxManager.Web/Pages/Forecast*`:

- **Khi chuyển thanh hành động xuống cuối form forecast, phải giữ submit mặc định là Preview đứng trước các nút bỏ giai đoạ…** [k:01M8G33DP7ZSB0MXMQ9PV1A68A]
  Khi chuyển thanh hành động xuống cuối form forecast, phải giữ submit mặc định là Preview đứng trước các nút bỏ giai đoạn/xóa tháng. Enter trong ô nhập có thể kích hoạt submit đầu tiên dù form action là Preview. Giữ test Default_submit_previews_without_removing_a_period và kiểm Enter trong trình duyệt; chỉ kiểm nhấp Lưu/Xem trước không phát hiện lỗi này.
<!-- bint:end -->

## Cách trình bày

- **Dùng hyperlink có chủ đích, tránh lạm dụng.** Áp dụng cho giao diện, tài liệu và phản hồi của agent. Ưu tiên văn bản thường; chỉ gắn liên kết khi người đọc cần điều hướng, mở tài nguyên hoặc kiểm chứng nguồn. Đặt nhãn rõ đích đến, mỗi đích chỉ liên kết một lần trong cùng ngữ cảnh. Không gắn link trang trí, phủ link lên mọi tên tệp/thuật ngữ, hoặc lặp cùng liên kết ở nhiều câu liền nhau. Giữ các trích dẫn nguồn bắt buộc.
