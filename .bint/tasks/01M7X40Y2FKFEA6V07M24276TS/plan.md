# Kế hoạch

## Hướng triển khai
Chỉ S11/BL-10..15, một đơn vị Core và kiểm thử đủ nhỏ, không tách thêm phiếu. Chọn model immutable gọn và calculator thuần, thay vì framework validation hay engine tra luật trực tuyến: dễ kiểm số học, tái hiện snapshot và giữ Core không phụ thuộc Web/DB/mạng. Không có caller hiện hữu. Dùng dữ liệu PLAN làm fixture tham khảo chưa xác minh; không nghiên cứu hoặc khẳng định pháp luật ở lượt lập kế hoạch.

## Tệp và thứ tự
1. BL-10: tạo TaxManager.sln (chỉ định định dạng sln khi dùng SDK10), src/TaxManager.Core/TaxManager.Core.csproj và tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj, đều net10.0. Core không PackageReference; test ProjectReference tới Core. Không để Class1/test mẫu rỗng. Implementer đối chiếu tài liệu xUnit/NuGet chính thức để chọn một bộ xUnit + runner/test SDK tương thích, pin version cụ thể và ghi nguồn; không cần coverage/package sản phẩm.
2. BL-11/13: dưới src/TaxManager.Core/, dự kiến Bracket.cs, TaxRuleSet.cs, LegalBasis.cs và RuleReference.cs (có thể gom các type nhỏ liên quan). Bracket biểu diễn trần tích lũy nullable cho bậc cuối và rate decimal; cận dưới suy từ bậc trước. TaxRuleSet giữ FromYear, ID/version/kỳ nhãn, giảm trừ, bậc và căn cứ được snapshot. Vietnam2026 chứa đúng năm/giảm trừ/5 bậc trong requirement, ID/version nội bộ ghi rõ tham khảo; ngày hiệu lực/ngày xác minh và thông tin điều khoản/URL chưa được cung cấp để trống, trạng thái Unverified/Missing rõ ràng.
3. BL-12: PitCalculator.cs và MonthlySalaryCalculator.cs; record kết quả/chi tiết đặt ở CalculationResult.cs hoặc cùng calculator. PIT duyệt từng bậc, phần chịu thuế = min(TNTT còn lại, độ rộng bậc), thuế phần = phần chịu thuế * rate; tổng các phần, không round. Monthly: tổng giảm trừ phụ thuộc = dependents * mức rule; TNTT trước chặn = grossIncome - insurance - giảm trừ bản thân - tổng giảm trừ phụ thuộc; TNTT = max(0, trước chặn). Giữ cả hai TNTT và tổng/chi tiết PIT; không thêm thu nhập miễn thuế/khoản giảm trừ khác chưa thuộc API.
4. BL-14/15: dưới tests/TaxManager.Core.Tests/, dự kiến PitCalculatorTests.cs, MonthlySalaryCalculatorTests.cs, TaxRuleSetTests.cs, LegalBasisTests.cs; fixture decimal MemberData/TheoryData dùng expected cố định tính riêng. Test rule giả khác ngưỡng/rate/giảm trừ để phát hiện thuật toán hardcode; test snapshot và metadata qua kết quả thật.
5. docs/decisions/0002-core-monthly-tax.md: API và ví dụ, quyết định lỗi/snapshot/căn cứ, nguồn/version package, lệnh + kết quả test thật, 3 ca tự bấm bên dưới, giới hạn số học decimal và phần pháp lý/người dùng chưa xác nhận. Không sửa 0001-dev-setup.md.
6. Implementer ghi bằng chứng AC qua bint task criteria; reviewer kiểm diff đúng task, tệp mới và bằng chứng độc lập phù hợp. Planner chỉ viết requirement/plan/tasks của chính phiếu qua CLI. Core/orchestrator quản lý trạng thái/review/finalize; agent không commit.

## Hợp đồng validation và snapshot
Dùng ArgumentNullException cho tham số bắt buộc null; ArgumentOutOfRangeException cho tiền âm/dependents âm, giảm trừ âm và rate ngoài [0,1]; ArgumentException cho biểu rỗng, phần tử null, trần hữu hạn không dương/không tăng, bậc vô hạn không đúng vị trí hoặc không có đúng một bậc cuối vô hạn, định danh/version/kỳ bắt buộc rỗng. Giữ thứ tự cấu hình, không tự sort/sửa dữ liệu sai. PIT trực tiếp từ TNTT âm báo lỗi; chỉ Monthly chặn TNTT âm do khấu trừ. Insurance không âm được nhận như số nhập, kể cả lớn hơn gross; không tự áp trần hay tỷ lệ. Dependents int không âm, không tự cho phép số lẻ hoặc đặt giới hạn người phụ thuộc. Overflow số học decimal dùng OverflowException, không bão hòa/làm tròn âm thầm; ghi giới hạn này trong tài liệu.

Record chỉ bất biến bề mặt chưa đủ: copy collection lúc tạo, giữ phần tử/metadata immutable và không lộ backing array/list có thể sửa; kết quả lưu snapshot immutable cùng ID/version/kỳ thực dùng. Mỗi bước gross/insurance/giảm trừ/TNTT/tổng PIT và mỗi phần bậc liên kết căn cứ tương ứng; dữ liệu người dùng hoặc căn cứ chưa có phải biểu diễn Missing/Unverified, không nhận căn cứ giảm trừ cho insurance. Test hai rule giả khác ID/version/kỳ/basis và sửa list đầu vào sau tạo rule/sau tính; kết quả cũ giữ nguyên.

LegalBasis có trường tùy chọn cho dữ liệu thiếu; summary 2-3 câu chỉ khi được cung cấp, không dựng nội dung luật. Phân biệt trạng thái thiếu/chưa xác minh và thông tin xác minh tường minh do bên nạp dữ liệu cung cấp. Không có nhánh tự nâng Verified từ URL hợp lệ. Khi khai báo Verified, kiểm metadata xác minh nhất quán, có ngày xác minh và URL tuyệt đối HTTPS host vbpl.vn hoặc subdomain có ranh giới tên miền; thiếu hoặc URL sai/ngoài nguồn phải từ chối trạng thái Verified bằng ArgumentException. Với Unverified/Missing có thể giữ URL đầu vào để mô tả vấn đề, nhưng không coi đó là nguồn đã xác minh. Test chuỗi sai, thiếu URL, nguồn khác, vbpl.vn.evil.example, URL đúng nhưng chưa xác minh và khai báo Verified không nhất quán; fixture Verified hợp lệ về mô hình không chứng minh luật thật. Core không gọi mạng để kiểm URL.

## Ma trận số học
Mọi giá trị dưới đây là dữ liệu PLAN/đối chứng số học, CHƯA XÁC MINH pháp lý.

| TNTT | PIT kỳ vọng |
|---|---|
| 0 | 0 |
| 10.000.000 | 500.000 |
| 30.000.000 | 2.500.000 |
| 60.000.000 | 8.500.000 |
| 100.000.000 | 20.500.000 |
| 120.000.000 | 27.500.000 |
| 10.000.001 | 500.000,1 |

| Gross / insurance / dependents | TNTT trước chặn / sau chặn | PIT |
|---|---|---|
| 30.000.000 / 3.150.000 / 0 | 11.350.000 / 11.350.000 | 635.000 |
| 20.000.000 / 2.100.000 / 1 | -3.800.000 / 0 | 0 |
| 50.000.000 / 5.000.000 / 2 | 17.100.000 / 17.100.000 | 1.210.000 |

Thêm quanh cả 4 trần hữu hạn: trần - 1 / trần / trần + 1, zero, số lẻ 10.000.000,01 -> 500.000,001 và thuế bằng tổng các phần; không dùng calculator sinh expected. Kiểm validation cả calculator/rule, null, rate/trần sai, overflow đại diện, phụ thuộc 0/âm, insurance âm/vượt gross; test metadata/snapshot như trên.

Ba ca cho người dùng tự bấm (chưa được họ xác nhận): TNTT 25.000.000 -> 500.000 + 15.000.000 * 0,10 = 2.000.000; TNTT 75.000.000 -> 8.500.000 + 15.000.000 * 0,30 = 13.000.000; gross 40.000.000, insurance 4.000.000, 1 phụ thuộc -> TNTT 14.300.000 -> 500.000 + 4.300.000 * 0,10 = 930.000. Dùng làm test đối chứng bổ sung và ghi tài liệu, không gọi đây là đối chiếu pháp lý.

## Chiến lược chạy test và bằng chứng
Chỉ lượt thực thi mới chạy lệnh dưới; lượt b-req không restore/build/test. Luôn gọi host project-local, cache package .dotnet/nuget-packages đã ignore. Từ root PowerShell:
```powershell
$env:NUGET_PACKAGES = Join-Path (Get-Location).Path '.dotnet/nuget-packages'
& ./.dotnet/dotnet.exe restore TaxManager.sln --packages $env:NUGET_PACKAGES
& ./.dotnet/dotnet.exe test tests/TaxManager.Core.Tests/TaxManager.Core.Tests.csproj --no-restore
& ./.dotnet/dotnet.exe test TaxManager.sln --no-restore
```
Mỗi lệnh phải kiểm exit code trước bước sau. Giữ verify.command trống/chưa được xác nhận trong Bint; đây là lệnh thực thi đã được yêu cầu, không tự đăng ký/approve hoặc gọi task verify. AC-1..4 kiểm cấu trúc/API, test thực chất và review; AC-5 chỉ đạt khi cả hai lệnh test thật thoát 0 và có số test/log, không skipped toàn bộ. AC-6 đọc tài liệu; AC-7 kiểm scope/hash và kết quả review.

Nếu restore bị chặn mạng, ghi package/version, nguồn đã chọn, lệnh/exit code/lỗi và source đã tạo để orchestrator cấp dependency từ phiên có mạng. Không đổi sandbox/harness/model hoặc cài SDK; không báo test xanh và chưa đánh dấu AC-5 đạt. Khi có dependency chạy lại đúng thứ tự. Nguồn package chưa chốt ở planner; phải xác minh chính thức trong lượt chọn version, không đoán latest. Chưa kiểm được build/test/dependency hay xác minh pháp luật ở lượt này; lint chỉ kiểm hợp đồng tài liệu.

## Rủi ro, phạm vi và bàn giao
Rủi ro chính: metadata đẹp bị hiểu là đã xác minh, collection bị sửa làm đổi lịch sử, hardcode/nhầm biên và nhầm host hệ thống. Dùng trạng thái tường minh, defensive snapshot, test rule giả/biên và host local để phát hiện. Chỉ thêm solution/Core/tests/tài liệu mới trong allow; không đụng setup S0, Web/Data, roadmap, CI/migration. Không quét toàn .dotnet; chỉ cache NuGet thuộc scope. Trước triển khai chụp hash PLAN.md/config/global.json/.gitignore/helper/docs S0, so sau cùng vì đầu vào còn có tệp untracked. Rollback chỉ hoàn tác tệp do S11 tạo theo snapshot, giữ SDK/setup và dữ liệu có trước; không git reset/xóa đệ quy diện rộng.

AC-1 được TSK-1; AC-2 được TSK-2; AC-3 được TSK-3; AC-4 được TSK-4/6; AC-5 được TSK-5/7; AC-6 được TSK-8; AC-7 được TSK-9. Mọi TSK có điều kiện xong đo được và tệp dự kiến nằm trong allow đã có; không cần thay scope/criteria. Không có Q cản triển khai; việc người dùng tra nguồn và tự đối chiếu vẫn đang chờ, chưa hoàn thành phần pháp lý.

