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
  Với adapter hiện tại, chỉ lưu luật lương tháng có bảo hiểm nhập tay; snapshot có optional graph bị từ chối. Xem đây là giới hạn triển khai cần mở rộng có kiểm thử, không phải giới hạn lâu dài của sản phẩm.
  Dùng SeedReferencesAsync/LoadReferencesAsync cho đúng bộ tham khảo Core 2025/2026; đường này không nhận tùy ý luật mới hoặc mọi snapshot Unverified.
  Giữ nguyên trạng thái nguồn cho tới khi đáp ứng quy trình kiểm tra đã xác định; không đổi Verified hay thêm fallback chỉ để vượt cổng. Triển khai riêng các thay đổi về trạng thái xác minh, nguồn và graph được hỗ trợ.

Khi làm việc với `src/TaxManager.Data/Migrations/**`, `src/TaxManager.Data/Upgrades/**`, `tests/TaxManager.Data.Tests/**`:

- **Không ép migration mã hóa chạy trên DB legacy có dữ liệu nhạy cảm** [k:01M85QH3AFYXW6P76BVMC5B43N]
  Giữ guard RequireCopyForwardForPopulatedDatabase của AddDataProtectionAndAudit: direct migration chỉ chấp nhận cả bốn bảng nhạy cảm legacy trống.
  Với thử nghiệm copy-forward, dùng SyntheticDatabaseUpgrader chỉ cho file synthetic absolute dưới TEMP, không links, syntheticDataOnly=true và target mới riêng biệt.
  Đừng bỏ guard hoặc dùng upgrader này cho DB vận hành; API hiện tại chỉ dành cho development.

Khi làm việc với `src/TaxManager.Data/Migrations/**`, `tests/TaxManager.Data.Tests/**`:

- **Thêm migration phải đối chiếu cả kỳ vọng schema mới và mốc downgrade cũ** [k:01M8CBFWCGN1RHN8ZHFV2P3M9Z]
  Khi thêm migration, rà các kỳ vọng GetAppliedMigrations ở MigrationTests, ProfilePersistenceTests và HistoryMigrationTests; test tính năng mới không bao phủ hết các kỳ vọng danh sách/số lượng cũ.
  Cập nhật kỳ vọng chạy tới schema mới nhất theo migration thực tế; giữ kỳ vọng cho các mốc migrate/downgrade tường minh và kiểm dữ liệu, audit, sentinel, trigger, idempotency.
  HistoryMigrationTests hiện lên 6 migration rồi gỡ hai migration forecast trước khi AddCalculationHistory từ chối Down, nên sau lỗi còn 4; không thay đồng loạt mọi số 4 thành số mới nhất.

Khi làm việc với `src/TaxManager.Web/Demo/**`, `src/TaxManager.Web/DemoApplication.cs`, `tests/TaxManager.Web.Tests/**`:

- **Kiểm chứng mở lại forecast bằng host mới dùng đúng kho synthetic cũ** [k:01M8CBFTQHZAEP0A2M9H7GWSHN]
  Để chứng minh lưu qua lần khởi động lại, dispose host đầu rồi tạo host mới trỏ cùng RootPath bằng SyntheticStore (DemoHost.StartAsync(syntheticRoot: root)); mở client mới trong cùng host chỉ kiểm một vòng đời app.
  Giữ cùng DB và synthetic-key.json: SyntheticDatabase không nhận existingRoot sẽ tạo kho và khóa ngẫu nhiên mới.
  Chỉ mở kho synthetic hợp lệ ngay dưới TEMP, giữ kiểm tra marker, reparse point và khóa; đừng tự tạo lại kho/khóa khi mở lại thất bại.
  Kiểm cả metadata/ngày nhập, thiếu khác 0 và cập nhật qua các host; giữ ca thiếu/sai khóa sau khi xóa hồ sơ.

Khi làm việc với `src/TaxManager.Web/Pages/Forecast*`, `src/TaxManager.Core/Forecasting/**`, `tests/TaxManager.Web.Tests/Forecasting/**`:

- **Giữ hợp đồng nhập tiền dự báo: thiếu khác 0, dấu phẩy là thập phân** [k:01M8BMXR829S6QRCBBJWYSB8SF]
  Giữ số tiền dự báo nullable: ô trống là thiếu, còn 0 là giá trị đã nhập; không điền 0 để thay dữ liệu chưa biết.
  Dùng hợp đồng ForecastAmountBinder cho các trường tiền: một dấu phẩy hoặc chấm là dấu thập phân, không cho cú pháp phân nhóm hàng nghìn.
  Không thay bằng parser theo culture với AllowThousands/NumberStyles.Number; cách đó có thể diễn giải khác số người dùng nhập.
  Khi POST sai, giữ nguyên chuỗi nhập và lỗi ModelState để người dùng sửa; các test HTTP kiểm cả việc từ chối dấu phân nhóm lẫn số lẻ dấu phẩy.

Khi làm việc với `src/TaxManager.Web/Pages/Forecast*`, `tests/TaxManager.Web.Tests/Forecasting/**`:

- **Xóa giai đoạn forecast phải dịch cả dấu thời gian trong DraftToken** [k:01M8CBFS1DPP02R61NX2E264B6]
  Khi bỏ một phần tử Periods, dịch đồng bộ chỉ số trong danh sách, ModelState và các khóa period/{index} của EntryStamp trong DraftToken; bỏ dấu của phần tử bị xóa.
  Giữ EnteredAt của giai đoạn còn lại nếu dữ liệu không đổi; chỉ dịch danh sách sẽ làm mất liên kết fingerprint và ghi lại ngày nhập ở lần Preview sau.
  Dùng đồng hồ điều khiển được, tăng thời gian giữa Preview → RemovePeriod → Preview để bắt hồi quy; hai lượt cùng thời điểm có thể che lỗi.
- **Probe forecast đọc textarea từ HTML thô và gửi lại cả xuống dòng** [k:01M8CBFY0W4W2J2AWVTG0TFGMK]
  Khi đổi markup ghi chú forecast hoặc gặp nhiều test persistence cùng sai chuỗi, kiểm Fields/Browser.Send của ForecastPersistenceBlackBoxTests trước khi kết luận dữ liệu lưu sai.
  Fields lấy toàn bộ nội dung giữa hai thẻ textarea rồi HtmlDecode, không đọc DOM; Browser.Send gửi lại chuỗi đó, nên xuống dòng do markup có thể trở thành dữ liệu của lần POST tiếp theo.
  Đối chiếu HTML thô, giá trị textarea trong trình duyệt và dữ liệu POST để định vị lỗi; đừng Trim ghi chú người dùng hoặc nới assertion để làm xanh test.
  Nếu đổi textarea thủ công, giữ mã hóa HTML và ưu tiên ModelState.AttemptedValue khi POST lỗi; kiểm vòng lưu/mở/xem trước với ghi chú nhiều dòng.
<!-- bint:end -->
