# Kế hoạch

## Phạm vi thực hiện của phiếu này
Chỉ khảo sát và ghi ba tài liệu Bint. requirement.md giữ yêu cầu/AC; tài liệu này giải thích hướng đi và các điểm chuyển chặng; tasks.md chứa checklist lập kế hoạch cùng backlog BL chưa thực hiện. Không có parent hay depends_on trên phiếu hiện tại. Không tạo phiếu mới, không chạy giai đoạn triển khai.

## Giả định và mâu thuẫn
PLAN.md là đầu vào định hướng, không chứng minh luật hiện hành. Các tên văn bản, ngày hiệu lực, tỷ lệ, mức giảm trừ và bảng kết quả cần đối chiếu lại; không nạp thành dữ liệu thật chỉ vì đã nằm trong PLAN.md. Dữ liệu giả lập có nhãn chỉ kiểm thử thuật toán, không xác nhận luật.

PLAN.md yêu cầu test và commit ở cuối mỗi bước, nhưng Bước 0 chưa có solution và lượt này chỉ lập kế hoạch. Bước 0 được kiểm bằng môi trường và ignore; test ứng dụng bắt đầu ở Bước 1.1; không tạo test rỗng để báo xanh. Không commit ở lượt này. Git đã tồn tại nên bỏ thao tác khởi tạo lại. FromYear một mình không đủ biểu diễn thay đổi giữa năm: giữ ý nghĩa năm theo PLAN.md và dự kiến bổ sung version/kỳ/khoảng áp dụng cho rule và căn cứ, không tự suy luận ngày áp dụng.

MVP và các lựa chọn chưa chốt bên dưới là đề xuất, không phải quyết định người dùng đã đưa ra. Mỗi lựa chọn được chốt trước phiếu thực thi liên quan, không chặn roadmap.

## Lựa chọn và lý do
Có thể làm web và DB trước để sớm có màn hình, hoặc làm Core thuần C# trước. Chọn Core trước theo trọng tâm PLAN.md: kiểm độc lập số học, kỳ áp dụng và căn cứ; tránh gắn nghiệp vụ với ORM/giao diện. SQLite phù hợp hướng local ban đầu trong PLAN.md; PostgreSQL chỉ cân nhắc khi cần triển khai. Razor Pages và MVC đều khả dĩ; đề xuất Razor Pages cho luồng form nhỏ, người dùng chốt trước Bước 3.1.

Với căn cứ pháp lý có thể lưu toàn văn/tìm trực tuyến khi tính, hoặc lưu metadata gắn rule/version và đóng gói dữ liệu đã kiểm tra. Chọn metadata theo yêu cầu: không kho luật toàn văn, không I/O mạng trong engine, có thể tái hiện kết quả cũ đúng kỳ. Xác minh nguồn là công việc nạp dữ liệu riêng; test runtime dùng fixture, không phụ thuộc mạng.

## Hiện trạng có bằng chứng
Các lệnh chỉ đọc đã dùng: rg --files --hidden, Get-ChildItem, Get-Content PLAN.md/.gitignore/.bint/config.yml, git status --short, git log -1, git rev-parse --is-inside-work-tree, dotnet --list-sdks, bint task show/list; MCP context_for_task trả không có tri thức. Bằng chứng chi tiết tại requirement.md. Git status toàn tệp chưa theo dõi nên git diff một mình không chứng minh giữ nguyên nội dung.

Mốc SHA256 trước lập tài liệu: PLAN.md = 24C8613968B7B5F3CF5A7A68B07AB4307D0935F52EB2A6C058070B6F3AE19953; .bint/config.yml = 1DCA80D30C76A7528DD1A59F5CAC16F1748BE64D693B50D5DDE5FF616F59E147. Đã chụp hash các tệp repository ngoài .git và .bint/tasks để so cuối lượt, kể cả .gitignore/skills.

## Roadmap và mốc bàn giao
Ưu tiên P0: nền tảng/điều kiện an toàn hoặc đúng nghiệp vụ; P1: hoàn thiện MVP; P2: mở rộng. MVP-L là bản local tham khảo, dữ liệu giả khi còn thử nghiệm; MVP-D là mốc đủ điều kiện xem xét deploy/dùng dữ liệu thật. Chưa triển khai mốc nào.

| Chặng | Bám PLAN.md | Kết quả và ranh giới | Phụ thuộc/mốc kiểm |
|---|---|---|---|
| G0 chuẩn bị | Bước 0 | SDK mục tiêu .NET 10 theo PLAN.md, ignore .NET, ghi quyết định; chưa Core/Web/DB | Môi trường liệt kê SDK đã chọn; Git sẵn có |
| G1 lõi tháng | 1.1 | Core, xUnit, lương tháng, metadata/mapping căn cứ; bảo hiểm nhập tiền; chưa tự tính BH, chưa UI | G0; bộ dữ liệu tháng được đối chiếu trước nạp thật |
| G2 kỳ và năm | 1.2 → 1.3 → 1.4 | Chọn luật 2025/2026 hoặc kỳ đã xác minh, BH/làm tròn, phụ thuộc theo tháng, tổng hợp năm | G1; dữ liệu L2-L4 và bộ ca đối chiếu |
| G2E mở rộng thu nhập | 1.5 | Từng loại một, gồm vãng lai khi đã xác minh; không chặn lương MVP | Năng lực lõi tương ứng; dữ liệu riêng từng loại |
| G3 dữ liệu/bảo mật | 2.1 → 2.2 → 2.3 | SQLite/EF, luật+căn cứ theo version, mã hóa/audit | G2; nếu chưa có 1.5 thì không lưu nghiệp vụ đó |
| G4 web MVP | 3.1 → 3.2 → 3.3 | Form 12 tháng, kết quả từng bước và ?, Identity, năm | G3; 3.1 chỉ local/dữ liệu giả khi chưa Identity |
| G5 quyền dữ liệu | phần quyền dữ liệu của 4.1 | Đồng ý xử lý, xuất dữ liệu cá nhân, xóa dữ liệu/tài khoản | G4; review bảo mật trước dữ liệu thật |
| G6 vận hành | 4.2 | CI, backup/restore, cấu hình deploy; đạt MVP-D khi kiểm đủ | G5; xác minh điều kiện hosting tại thời điểm thực hiện |
| GE xuất báo cáo | phần Excel/PDF của 4.1; 4.3 | Báo cáo tham khảo; XML tùy chọn, cần mẫu chính thức và thử import | Sau MVP-L; XML sau dữ liệu/mẫu được xác minh |

MVP-L đầy đủ gồm G0-G4 cho nghiệp vụ lương/tháng/năm và trích dẫn; không tuyên bố hỗ trợ mọi đối tượng hay mọi loại thu nhập. G5-G6 là điều kiện trước khi mở cho người dùng/dữ liệu thật. Đề xuất tách phần báo cáo của 4.1 khỏi quyền dữ liệu để không trì hoãn các kiểm soát cần trước deploy.

## Chi tiết chặng đầu: Bước 0 và Bước 1.1
Thực hiện tuần tự: xong Bước 0 mới bắt đầu 1.1. Đây là kế hoạch cho phiếu tương lai, không phải thao tác được thực hiện trong lượt này.

1. BL-00 kiểm công cụ, chọn phiên bản SDK .NET 10 cụ thể và cách làm việc bằng Codex; cài SDK trong lượt triển khai nếu còn thiếu. Không khởi tạo lại repository. Ghi trong docs/decisions/ cách kiểm SDK và quyết định solution .sln để bám PLAN.md. Không nâng/đổi model đã đặt.
2. BL-01 bổ sung .gitignore cho bin/, obj/, đầu ra IDE/test và secrets; giữ khối Bint, không ignore mã/test hay global.json. Pin global.json sau khi SDK đã chọn. Kiểm git check-ignore với tệp mẫu đường dẫn đầu ra và bảo đảm tệp cấu hình/mã cần theo dõi không bị ignore.
3. BL-10 tạo TaxManager.sln, src/TaxManager.Core/TaxManager.Core.csproj, tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj; chỉ Core và xUnit. Test tham chiếu Core; Core không tham chiếu EF/Web hay thư viện ngoài. Không tạo Data/Web ở bước này.
4. BL-11 định nghĩa Bracket, TaxRuleSet và mẫu Vietnam2026 sau khi L1 đủ; chỉ dùng decimal cho tiền. Các con số luật trong bộ quy tắc; các giá trị kỳ vọng trong fixture test là dữ liệu kiểm chứng riêng, không phải hardcode trong thuật toán. Bổ sung định danh phiên bản/kỳ áp dụng để căn cứ không phụ thuộc mỗi FromYear. Chốt cách báo lỗi đầu vào trước khi viết API (L0); không âm thầm đổi dữ liệu nhập sai.
5. BL-12 PitCalculator tính từng phần, không làm tròn; MonthlySalaryCalculator nhận grossIncome, insurance là số tiền nhập, dependents, ruleSet và trả record các bước, chặn thu nhập tính thuế âm về 0. Không thêm tự tính BH/thu nhập vãng lai/năm ở bước này.
6. BL-13 mô hình LegalBasis/RuleReference (tên đề xuất), chứa metadata mục 1.8; mỗi thành phần kết quả giữ ID/version căn cứ đã dùng. Trạng thái thiếu/chưa xác minh có cấu trúc rõ ràng; không suy từ năm hiện tại, không gọi HTTP, không phụ thuộc UI/DB.
7. BL-14 xUnit Theory: 7 ca thu nhập tính thuế và 3 ca lương được liệt kê tại Bước 1.1 PLAN.md, sau khi đối chiếu L1; thêm biên quanh mỗi ngưỡng, âm sau giảm trừ, không làm tròn, đầu vào sai theo quyết định L0. So 2-3 ca độc lập do người dùng tính, không lấy calculator sinh expected.
8. BL-15 test mỗi thành phần kết quả có mapping đúng rule/version/kỳ; thay version dùng căn cứ tương ứng, thiếu căn cứ/chưa xác minh không thành verified, fixture URL sai/thiếu được phát hiện. Chưa xây provider chọn nhiều năm (1.2) hay kiểm liên kết mạng trong test Core. Dữ liệu giả vẫn ghi rõ chỉ kiểm mô hình.
9. Bàn giao 1.1 khi suite Core xanh, người dùng hiểu/đối chiếu PitCalculator và nguồn L1 đủ để gọi dữ liệu Vietnam2026 là đã xác minh. Nếu chưa đủ L1, chỉ có thể bàn giao khung/fixture giả với trạng thái chưa hoàn tất phần luật thật, không tuyên bố 1.1 đã xong. Test/commit là việc của lượt triển khai theo PLAN.md.

## Dữ liệu pháp lý và quyết định cần cung cấp theo chặng
Nguồn cần đối chiếu là văn bản gốc trên vbpl.vn và cổng chính thức của Bộ Tài chính/cơ quan thuế/cơ quan ban hành, cơ quan BHXH cho phần bảo hiểm. Đây là danh sách nguồn cần dùng, chưa phải kết quả tra cứu. Không dùng bài tổng hợp thay văn bản gốc; không bịa URL chi tiết/itemid. Mỗi gói dữ liệu cần văn bản/điểm/khoản/điều, URL gốc, phạm vi đối tượng, kỳ áp dụng/ngày hiệu lực, sửa đổi/thay thế/chuyển tiếp, người/ngày đối chiếu và ca kỳ vọng.

| Mã đầu vào | Cần trước | Người dùng cần cung cấp/chốt | Khi chưa có |
|---|---|---|---|
| L0 kỹ thuật/phạm vi | BL-00; hoàn tất trước API BL-11/12 | SDK/pin cụ thể; .sln; phạm vi lương/cư trú; hợp đồng lỗi cho số âm/số phụ thuộc không hợp lệ; cách review bằng Codex/người dùng | Chuẩn bị và ghi quyết định; không gọi là luật đã chốt |
| L1 lương tháng | BL-11/14 nạp luật thật | Đối chiếu văn bản được PLAN.md nêu là Luật 109/2025/QH15, NQ 110/2025/UBTVQH15 và văn bản liên quan; bậc/ngưỡng/giảm trừ, kỳ/đối tượng, công thức, căn cứ từng bước, hiệu lực thực tế và ca 1.1 | Chỉ fixture giả có nhãn; không seed Vietnam2026 thật |
| L2 lịch sử/chuyển kỳ | BL-20 | Ngưỡng và giảm trừ cũ năm 2025; văn bản thay thế/chuyển tiếp; cách giải quyết thay đổi giữa năm và ngày dùng để chọn rule | Từ chối kỳ không hỗ trợ, không tự chọn gần nhất |
| L3 BH/làm tròn | BL-21 | Nguồn BHXH/BHYT/BHTN, đối tượng, tỷ lệ/trần/sàn/lương đóng theo kỳ; làm tròn ở bước nào và chế độ làm tròn từ nguồn chính thức | Bước 1.1 tiếp tục nhận insurance là tiền; chưa tự tính |
| L4 phụ thuộc/năm | BL-22 | Điều kiện/tháng giảm trừ, gián đoạn, công thức năm/tạm nộp/hoàn-nộp thêm, ca độc lập từ nguồn thuế chính thức | Chưa nhận hoàn-nộp thêm là đúng nghiệp vụ |
| L5 từng thu nhập | BL-23a đến BL-23f | Quy tắc thưởng; phụ cấp/làm đêm; vãng lai; nhiều nơi; từ thiện; hưu trí, căn cứ/ngưỡng/miễn/giảm theo kỳ | Hoãn riêng loại thiếu dữ liệu; không chặn MVP lương |
| L5-VL ví dụ UX | BL-23c | Đối chiếu điểm i khoản 1 Điều 25 Thông tư 111/2013/TT-BTC do người dùng dẫn, sửa đổi/thay thế và hiệu lực theo kỳ; URL gốc và phạm vi đối tượng | Ví dụ 2.500.000 → 250.000 (10%) chỉ minh họa chờ xác minh; điều kiện/ngưỡng trong PLAN.md cũng chưa được xác nhận |
| L6 dữ liệu/bảo mật | BL-30/32 | Dữ liệu tối thiểu; có cần MST/CCCD không; mô hình người dùng, quyền truy cập, vòng đời dữ liệu/khóa, audit; đối chiếu văn bản bảo vệ dữ liệu PLAN.md nêu | Chỉ dữ liệu giả; chưa coi mã hóa là đủ cho vận hành |
| L7 web | BL-40/41/42 | Razor Pages/MVC; luồng 12 tháng; mục tiêu mobile; cách diễn đạt giải thích 2-3 câu, disclaimer; tiêu chí review truy cập 2 tài khoản | Chưa công bố web nhiều người |
| L8 quyền/vận hành | BL-50/51 | Căn cứ đồng ý/xuất/xóa, thời gian lưu; nơi deploy/chi phí/giới hạn hiện hành; mục tiêu backup/restore, cách giữ khóa và xử lý khôi phục | Không đưa dữ liệu thật lên cloud |
| L9 báo cáo/XML | BL-60/61 | Cột/mẫu Excel/PDF tham khảo; mẫu/xsd XML và phần mềm import chính thức đang áp dụng | Hoãn xuất XML; không đoán schema hay coi PDF là tờ khai |

Những tên/số văn bản trong bảng chỉ được nhắc lại để người dùng tìm và đối chiếu, không khẳng định tồn tại, nội dung hay hiệu lực. Việc xác minh liên kết và hiệu lực thuộc BL-11/20/23/31 trước nạp thật; runtime chỉ dùng snapshot đã kiểm.

## Đề xuất tách giai đoạn
Chỉ đề xuất, không tạo phiếu trong run này. Nên tạo phiếu cha triển khai R mới, tham chiếu ba tài liệu của 01M7WZSJJDNS1D97A7J19YN37T. Không dùng phiếu lập kế hoạch hiện tại làm cha triển khai vì scope.deny src/**, tests/** và ràng buộc chỉ lập kế hoạch sẽ không phù hợp. Với từng con của R, đặt scope rõ ràng bằng --clear-scope rồi --allow/--deny; không để vô tình kế thừa giới hạn của cha. Mọi scope dưới đây dự kiến có thêm .bint/tasks/** và deny PLAN.md, .bint/config.yml; không thay đổi phạm vi phiếu hiện tại.

| Thứ tự / phiếu đề xuất | Mục tiêu; backlog | --allow dự kiến | depends_on |
|---|---|---|---|
| R | Cha theo dõi triển khai toàn roadmap; không tự thực hiện các con | .bint/tasks/**, docs/decisions/** | Phiếu lập kế hoạch hiện tại hoàn tất |
| S0 | Bước 0; BL-00/01 | .gitignore, global.json, docs/decisions/** | Phiếu lập kế hoạch hiện tại |
| S11 | Bước 1.1; BL-10 đến 15 | TaxManager.sln, src/TaxManager.Core/**, tests/TaxManager.Core.Tests/**, docs/decisions/** | S0 |
| S12 | Bước 1.2; BL-20 | src/TaxManager.Core/**, tests/TaxManager.Core.Tests/**, docs/decisions/** | S11 |
| S13 | Bước 1.3; BL-21 | src/TaxManager.Core/**, tests/TaxManager.Core.Tests/**, docs/decisions/** | S12 |
| S14 | Bước 1.4; BL-22 | src/TaxManager.Core/**, tests/TaxManager.Core.Tests/**, docs/decisions/** | S13 |
| S15a-f | Bước 1.5, một phiếu mỗi loại; BL-23a-f | src/TaxManager.Core/**, tests/TaxManager.Core.Tests/**, docs/decisions/** | S14; làm tuần tự a → b → c → d → e → f nếu chọn |
| S21 | Bước 2.1; BL-30 | TaxManager.sln, src/TaxManager.Data/**, tests/TaxManager.Data.Tests/**, migrations/**, docs/decisions/** | S14; không bắt buộc S15 |
| S22 | Bước 2.2; BL-31 | src/TaxManager.Data/**, tests/TaxManager.Data.Tests/**, migrations/**, docs/decisions/** | S21 |
| S23 | Bước 2.3; BL-32 | src/TaxManager.Data/**, tests/TaxManager.Data.Tests/**, docs/decisions/** | S22 |
| S31 | Bước 3.1; BL-40 | TaxManager.sln, src/TaxManager.Web/**, tests/TaxManager.Web.Tests/**, docs/decisions/** | S23 |
| S32 | Bước 3.2; BL-41 | src/TaxManager.Web/**, src/TaxManager.Data/**, tests/TaxManager.Web.Tests/**, tests/TaxManager.Data.Tests/**, migrations/**, docs/decisions/** | S31 |
| S33 | Bước 3.3; BL-42 | src/TaxManager.Web/**, tests/TaxManager.Web.Tests/**, docs/decisions/** | S32 |
| S41 | Quyền dữ liệu thuộc 4.1; BL-50 | src/TaxManager.Web/**, src/TaxManager.Data/**, tests/TaxManager.Web.Tests/**, tests/TaxManager.Data.Tests/**, docs/decisions/** | S33 |
| S42 | Bước 4.2; BL-51 | .github/workflows/**, deploy/**, docs/operations/** | S41 |
| S41E | Báo cáo Excel/PDF thuộc 4.1; BL-60 | src/TaxManager.Web/**, tests/TaxManager.Web.Tests/**, docs/decisions/** | S33; tùy chọn |
| S43E | Bước 4.3 XML; BL-61 | src/TaxManager.Web/**, tests/TaxManager.Web.Tests/**, docs/decisions/** | S41E; có L9; tùy chọn |

migrations/** và .github/workflows/** là đường dẫn được bảo vệ trong cấu hình hiện tại; phiếu tương lai phải nêu phạm vi và cách kiểm chứng cho người duyệt. Không yêu cầu phê duyệt chúng trong phiếu chỉ viết tài liệu này. Vị trí migration đề xuất là migrations/** để phạm vi được bảo vệ rõ ràng; nếu chọn vị trí khác ở lúc thiết kế Data, cập nhật scope và quy tắc bảo vệ trước khi sửa, không coi bảng này là quyền ghi sẵn có.

Ví dụ lệnh để người dùng/orchestrator dùng SAU KHI xem roadmap; không được chạy trong lượt này. Các biến được người chạy điền bằng ID thật vừa nhận từ CLI, không phải ID đã tồn tại:
```powershell
bint task new "Triển khai roadmap từ phiếu 01M7WZSJJDNS1D97A7J19YN37T, điều phối các chặng riêng" --workflow b-do --plan --clear-scope --allow '.bint/tasks/**' --allow 'docs/decisions/**' --deny 'PLAN.md' --deny '.bint/config.yml' --depends-on 01M7WZSJJDNS1D97A7J19YN37T --json
$roadmapId = Read-Host "Nhập ID phiếu cha R vừa nhận"
bint task new "S0: chỉ thực hiện Bước 0 theo roadmap, chưa tạo Core" --parent $roadmapId --depends-on 01M7WZSJJDNS1D97A7J19YN37T --workflow b-do --plan --clear-scope --allow '.bint/tasks/**' --allow '.gitignore' --allow 'global.json' --allow 'docs/decisions/**' --deny 'PLAN.md' --deny '.bint/config.yml' --json
$stage0Id = Read-Host "Nhập ID phiếu S0 vừa nhận"
bint task new "S11: chỉ thực hiện Bước 1.1, Core và trích dẫn theo ngữ cảnh" --parent $roadmapId --depends-on $stage0Id --workflow b-do --plan --clear-scope --allow '.bint/tasks/**' --allow 'TaxManager.sln' --allow 'src/TaxManager.Core/**' --allow 'tests/TaxManager.Core.Tests/**' --allow 'docs/decisions/**' --deny 'PLAN.md' --deny '.bint/config.yml'
```
Lấy ID từ kết quả thực tế rồi điền vào --parent/--depends-on. Các chặng tiếp theo áp dụng đúng thứ tự/scope trong bảng; không gắn depends_on vào R vì R là cha theo dõi toàn bộ, tránh chặn các con chờ cha hoàn tất. Không giao cả roadmap cho một lượt b-feat.

## Vùng ảnh hưởng
Lượt này chỉ ghi .bint/tasks/01M7WZSJJDNS1D97A7J19YN37T/{requirement,plan,tasks}.md và bản runtime tương ứng do CLI quản lý; không có API đang chạy hay dữ liệu người dùng bị thay đổi. Các ký hiệu PitCalculator, MonthlySalaryCalculator, TaxRuleSet, TaxRuleProvider và LegalBasis/RuleReference đều là mục tiêu tương lai, chưa có caller thật.

Khi triển khai: Web gọi calculator, calculator chỉ nhận rule snapshot; Data cung cấp rule/metadata qua tầng ghép nối, Core không biết EF/Web. Result mang tham chiếu căn cứ để UI hiển thị đúng snapshot, không lấy luật mới nhất thay cho kỳ đã tính. DB cần giữ luật+căn cứ gắn version để đọc lại; mọi thay đổi version cần xét ảnh hưởng lịch sử kết quả và audit. Những người dùng tương lai chịu ảnh hưởng chính là người nhập lương, xem năm và quản lý hồ sơ cá nhân.

## Rủi ro và rollback
Rủi ro lớn là hiểu nhầm bảng luật tham khảo thành dữ liệu đã xác minh: ngăn qua L1-L5, nhãn dữ liệu giả và kiểm mapping/kỳ; phát hiện bằng đối chiếu nguồn và ca độc lập. Không sửa bộ quy tắc đã dùng trong lịch sử khi chưa có chính sách version; rollback cấu hình về snapshot đã kiểm và giữ lịch sử, không tự chọn luật thay thế.

Metadata đúng hình thức chưa bảo đảm nguồn đúng nội dung/hiệu lực: kiểm đích URL và văn bản thật khi nạp, lưu ngày/trạng thái xác minh; khi thiếu/chưa xác minh không trình bày như đã xác minh. Không dùng HTTP 200 hay đuôi tên miền đơn thuần làm bằng chứng pháp lý. Kiểm link/redirect khi xác minh dữ liệu; tính thuế và test Core không truy cập mạng.

Rủi ro lộ dữ liệu/khóa và truy cập chéo: dùng dữ liệu giả đến khi review bảo mật, test hai tài khoản, kiểm log và thử restore. Migration tương lai phải sao lưu và thử khôi phục trước chạy thật; rollback schema/dữ liệu dựa vào backup và tương thích ứng dụng, không chỉ hoàn tác commit. Quyết định khóa/backup thuộc L6/L8.

Rollback tài liệu lượt này chỉ cần ghi lại nội dung trước bằng bint task write nếu có lỗi, không xóa lịch sử hay động đến PLAN.md. Vì chưa có commit gốc, không dùng git reset/checkout để giả định có thể phục hồi tệp chưa theo dõi. So hash để phát hiện sửa ngoài phạm vi.

## Chiến lược kiểm thử
Hiện tại: chạy bint task lint 01M7WZSJJDNS1D97A7J19YN37T --strict theo yêu cầu trực tiếp; đọc lại đủ 5 AC và tất cả dòng BL; so hash/số tệp ngoài ledger. Đây là kiểm hợp đồng tài liệu, không phải xác nhận nghiệp vụ hay test ứng dụng. verify.command của phiếu đang trống/chưa xác nhận: không đặt rồi tự chạy lệnh kiểm chứng, không gọi bint task verify/status/approve dưới bint run.

Tương lai Bước 0: dotnet --list-sdks có phiên bản đã chọn; git check-ignore xác nhận ignore đúng, pin SDK khớp, Git sẵn có. Bước 1.1: đề xuất dotnet test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj cho kiểm hẹp, dotnet test TaxManager.sln cho cổng toàn solution khi solution đã tồn tại. Những lệnh này chỉ đề xuất, chưa chạy và chưa được đăng ký là đã xác nhận.

Các chặng sau: chọn kỳ/không hỗ trợ/chồng khoảng; BH trần/sàn và làm tròn; phụ thuộc giữa năm, hoàn/nộp thêm; DB roundtrip/version và URL sai; mã hóa/tamper/khóa và audit không lộ plaintext; Web keyboard/mobile, ?, trạng thái nguồn, CSRF/phân quyền hai tài khoản; xuất/xóa dữ liệu; backup/restore và CI. Chỉ thêm test theo chức năng của từng phiếu, không tạo toàn bộ test tương lai lúc khởi tạo.

Chưa kiểm được ở lượt này: build/test .NET, tính đúng nghiệp vụ, hiệu lực luật/URL chi tiết, UI, an toàn dữ liệu hay khả năng deploy. Không suy ra những điều đó từ lint xanh.

## Việc nên bắt đầu ngay
Sau khi xem ba tài liệu, đề xuất bắt đầu S0 với BL-00/01: chốt/cài SDK trong lượt thực thi và hoàn thiện ignore. Đồng thời người dùng chuẩn bị L1 cho S11; thiếu L1 không cản hoàn tất S0 hay roadmap, nhưng cản nạp luật thật. Chưa mở Bước 1.2, Data hoặc Web. Không chạy /b-feat cho toàn roadmap dưới scope phiếu lập kế hoạch này; tạo/chọn phiếu đúng chặng rồi dùng bint run b-feat --task ID_THUC_TE sau khi phần chuẩn bị của phiếu đó đáp ứng quy trình Bint.

## Checklist nhất quán
- AC-1 được TSK-1 bao phủ; AC-2 được TSK-2/3; AC-3 được TSK-4; AC-4 được TSK-6; AC-5 được TSK-5.
- Mỗi TSK là công việc lập kế hoạch có điều kiện xong khi đo được. BL là backlog tương lai riêng, có ưu tiên, loại, phụ thuộc, scope, bắt đầu/xong khi; không dùng trạng thái TSK để tuyên bố BL đã làm.
- Các tệp ghi thật chỉ thuộc .bint/tasks/**; deny PLAN.md/src/**/tests/** được giữ. Scope trong bảng giai đoạn là đề xuất cho phiếu tương lai, không mở quyền ghi hiện tại.
- Đủ sáu nội dung phân tích deep; không có Q chặn roadmap, không tự xác minh luật, không lập phiếu con hay chạy triển khai.

