# Yêu cầu

## Mục tiêu và phạm vi
S12, chỉ Bước 1.2/BL-20. S11 (01M7X40Y2FKFEA6V07M24276TS) đã done; đã đọc requirement/plan của phụ thuộc, PLAN, backlog và docs0002. Core có TaxRuleSet/LegalBasis bất biến và calculator nhận rule tường minh; chưa có provider hoặc Vietnam2025. MCP không có tri thức liên quan. Phân tích standard; lượt này chỉ viết ba artifact, không sửa source/chạy test/commit.

## Yêu cầu chức năng
1. Thêm TaxRuleProvider thuần C# nhận danh sách đăng ký rule với khoảng kỳ thuế tường minh; hỗ trợ chọn theo năm, tháng và DateOnly thuộc tháng tính thuế. Không tìm rule gần nhất, không suy ngày từ FromYear hoặc lọc kỳ thuế bằng LegalBasis.EffectiveFrom/To. Chọn đúng 2026 cho cả tháng 01–06 và 07–12/2026 theo đăng ký, giữ seed Unverified.
2. Khoảng kỳ dùng hai đầu bao gồm, trọn tháng. Cấu hình null, khoảng đảo/ngắt giữa tháng, duplicate hoặc overlap phải báo lỗi; gap hoặc kỳ ngoài đăng ký không được fallback. Chọn năm chỉ thành công khi đủ 12 tháng cùng một snapshot; năm thiếu tháng hoặc có nhiều snapshot báo lỗi rõ, hướng caller chọn tháng.
3. Provider sao chép collection đầu vào; đăng ký và rule/basis không sửa được. Truyền rule đã chọn vào API S11 hiện hữu; mỗi khoản và phần thuế giữ đúng ID/version/period/basis. Gross và insurance vẫn Missing. Không thay thuật toán/làm tròn hoặc API S11.
4. Q-1 đã đủ dữ liệu để thêm TaxRuleSet.Vietnam2025 cho kỳ 2025: trần tháng 5.000.000/10.000.000/18.000.000/32.000.000/52.000.000/80.000.000/không giới hạn, thuế suất tương ứng 5%/10%/15%/20%/25%/30%/35%; giảm trừ bản thân 11.000.000 và mỗi người phụ thuộc 4.400.000 đồng/tháng. Số luật/thuế suất dùng decimal trong TaxRuleSet; lưu nguồn người dùng cung cấp, kỳ đúng và Unverified, VerifiedOn null cho tới khi đối chiếu đầy đủ. Người dùng dẫn công thức rút gọn theo TT111/2013/TT-BTC với số trừ 0/0,25/0,75/1,65/3,25/5,85/9,85 triệu; chỉ dùng đối chứng, engine giữ tính từng phần, không thêm offset vào bộ quy tắc. Không tạo seed rỗng/0, không dựng URL hoặc tự nâng 2026 Verified. Fixture giả phải mang định danh test, không giả làm luật Việt Nam.
5. Thêm test mới cho selection, validation, biên, snapshot và số học độc lập; giữ nguyên hai tệp test cũ cùng 27 case. Bổ sung đối chứng 2025/2026 theo dữ liệu đã được người dùng cung cấp, expected tính độc lập; không nhận đó là xác minh pháp lý đầy đủ. docs0003 ghi API/quyết định/nguồn/giới hạn và lệnh/kết quả test thực chạy; thiếu dữ liệu thì ghi S12 chưa hoàn tất.

## Tiêu chí chấp nhận
- [ ] AC-1: AC-1: Hợp đồng chọn năm/kỳ rõ, không tự fallback năm lạ; phân biệt kỳ thuế và ngày hiệu lực, không đổi engineS11.
- [ ] AC-2: AC-2: Vietnam2025 được thêm sau khi có dữ liệu người dùng cung cấp với nguồn/kỳ/trạng thái đúng, số luật chỉ trong TaxRuleSet bằng decimal; không bịa nguồn hoặc Verified.
- [ ] AC-3: AC-3: Chọn đúng rule và căn cứ immutable theo kỳ/ngày; kiểm duplicate/overlap/gap/range không hợp lệ và năm ngoài hỗ trợ.
- [ ] AC-4: AC-4: Test số học độc lập khác2025/2026, biên ngày/kỳ và mapping/snapshot có ý nghĩa; project và toàn solution test xanh,27case cũ giữ nguyên.
- [ ] AC-5: AC-5: Docs0003 ghi API/quyết định/nguồn và kiểm thử thật, phạm vi đúng, review không còn lỗi nghiêm trọng; thiếu dữ liệu phải ghi chưa hoàn tất.

## Ngoài phạm vi
Bước 1.3 trở đi: tự tính BH/làm tròn, tổng hợp/quyết toán năm, đánh giá người phụ thuộc, kinh doanh/CIT/vãng lai, DB/UI/mạng, framework versioning. Không đổi PLAN/config/global.json/solution/package/model/setup; không sửa 27 case cũ, docs0002 hay roadmap; agent không commit/approve/verify/status. verify.command giữ trống. Q-1 đã trả lời đủ, không còn câu hỏi cản b-feat. Việc đối chiếu đầy đủ văn bản gốc vẫn chưa hoàn tất nên không nâng Verified; không đánh dấu hoàn tất AC-2/AC-4 hoặc toàn S12 bằng fixture.

## Câu hỏi mở
- [x] Q-1: Với kỳ tính thuế 2025, ngưỡng thu nhập tính thuế theo tháng của 7 bậc (kèm thuế suất tương ứng), mức giảm trừ bản thân và mỗi người phụ thuộc là bao nhiêu theo nguồn chính thức bạn đã tra; vui lòng gửi số liệu và nguồn áp dụng 2025 đang chờ để root chuyển qua Bint? — Trả lời: Người dùng đã cung cấp bảng 7 bậc theo tháng: đến 5 triệu/5%; trên 5–10 triệu/10%; trên 10–18 triệu/15%; trên 18–32 triệu/20%; trên 32–52 triệu/25%; trên 52–80 triệu/30%; trên 80 triệu/35%. Công thức rút gọn được người dùng dẫn theo TT111/2013/TT-BTC có số trừ lần lượt 0;0,25;0,75;1,65;3,25;5,85;9,85 triệu, chỉ dùng đối chứng. Người dùng gửi nguồn https://baochinhphu.vn/cai-cach-thue-thu-nhap-ca-nhan-nguoi-lao-dong-co-the-co-them-hang-chuc-trieu-dong-moi-nam-102261113121825969.htm xác nhận giảm trừ cũ bản thân11 triệu và NPT4,4 triệu/tháng. Được dùng các số liệu này để thêm Vietnam2025 theo kỳ2025. Giữ Unverified, không nhận là đã xác minh đầy đủ pháp lý, không bịa pinpoint/URLvbpl; engine vẫn tính từng phần. Q-1 đã đủ dữ liệu để triển khai.



