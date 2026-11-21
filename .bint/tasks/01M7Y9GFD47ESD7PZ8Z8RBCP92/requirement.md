# Yêu cầu

## Mục tiêu và phạm vi
S14 / Bước 1.4 / BL-22: Core ước tính quyết toán lương thông thường năm dương lịch 2026 cho một cá nhân Việt Nam cư trú, một hồ sơ và một nguồn lương. Caller xác nhận phạm vi, cung cấp thu nhập chịu thuế lương theo tháng nhận/trả, các tháng phụ thuộc ĐƯỢC giảm trừ đã đủ điều kiện, bảo hiểm thực được trừ và thuế thực tạm nộp. Gross trong API là đầu vào lương chịu thuế trước BH/giảm trừ; engine không tự phân loại khoản miễn.

Kế thừa roadmap 01M7WZSJJDNS1D97A7J19YN37T và phụ thuộc S13 01M7Y733A040Z7XRKCHT815N61 đã done; đã đọc requirement/plan của cả hai. HEAD khi khảo sát: 00bb4502ac6becc5cdc712f6f7aea16236b4d68d. 55 ca xanh là bằng chứng bàn giao S13/docs0004, chưa chạy lại trong b-req. MCP context_for_task không có tri thức liên quan để cite. Lượt này chỉ ghi ba tài liệu và tiêu chí của phiếu; chưa triển khai hay nghiệm thu S14.

## Yêu cầu chức năng
1. Một DependentPeriod gồm ID ổn định và hai tháng được giảm trừ bao hàm hai đầu; dùng DateOnly ngày 1 biểu diễn tháng, không suy từ ngày đăng ký. ID Trim rồi so Ordinal phân biệt hoa/thường; rỗng bị từ chối. Cùng ID được có các khoảng không giao nhau để biểu diễn gián đoạn; trùng/chồng tháng bị từ chối, khác ID được đồng thời. Cắt khoảng qua năm theo giao với 01–12/2026, không nhân đôi một ID trong cùng tháng. Kết quả giữ ID, khoảng gốc, phần trong năm, tháng thực dùng và căn cứ quy tắc; đây không phải bằng chứng hồ sơ đủ điều kiện.
2. Có đúng một bản ghi cho mỗi tháng 1–12/2026, kể cả gross/BH bằng 0; nhận danh sách không theo thứ tự rồi sắp kết quả tăng dần. Không tự điền tháng thiếu, không gộp tháng trùng. Tạm nộp thực tế mỗi tháng bắt buộc, cho phép 0, không mặc định bằng thuế tháng.
3. Mỗi tháng chọn đúng một chế độ: InsuranceAmount decimal nhập tay, hoặc InsuranceInput tự tính qua API S13. Số 0 nhập tay dùng được cho tháng không đóng/không thu nhập, không ép qua guard full-month của S13. Nhánh tự tính giữ InsuranceResult và chỉ lấy DeductibleTotal một lần. Không suy base bằng gross, không nới phạm vi BH S13, không thêm ràng buộc insurance <= gross.
4. Resolve provider.GetForMonth cho cả 12 tháng và cấu hình quyết toán năm đi kèm. Có biểu năm riêng 120/360/720/1.200 triệu rồi không giới hạn, thuế suất 5/10/20/30/35%; GTBT 186 triệu/năm, GTNPT 6,2 triệu cho mỗi dependent-month. Không nhân/suy biểu năm trong engine, không lấy snapshot tháng 1 thay GetForYear. Cho phép snapshot BH khác nhau khi phần thuế và hợp đồng năm tương thích; thay đổi thực sự bậc/rate/giảm trừ thuế giữa năm chưa có hợp đồng thì báo unsupported.
5. Trả 12 kết quả tháng gồm gross, BH nhập/tự tính, giảm trừ bản thân và phụ thuộc, ID/count phụ thuộc, TNTT trước/sau clamp, PIT lũy tiến tháng tham khảo và tạm nộp thực. Các tháng được tính lại theo lịch giảm trừ đủ điều kiện cho quyết toán và rule 2026, không tái dựng số đã khai/khấu trừ lịch sử theo tháng đăng ký.
6. Tổng năm: gross, BH được trừ, GTBT đủ 12 tháng, GTNPT theo tổng dependent-month, tổng giảm trừ, TNTT trước clamp = gross - BH - GTBT - GTNPT, TNTT = max(0, trước clamp), PIT dùng biểu NĂM. Không cộng PIT tháng hay TNTT tháng đã clamp làm thuế/TNTT năm. Delta = PIT năm - tổng tạm nộp; AdditionalPayable = max(0, Delta), EstimatedOverpaid = max(0, -Delta).
7. Tiền/rate dùng decimal; giữ nguyên số lẻ theo quyết định NoRounding S13. Mọi rule/basis/snapshot và collection kết quả bất biến, giữ provenance theo tháng kể cả BH đổi tháng 7; tổng năm giữ các đóng góp tháng và căn cứ năm, không gán một snapshot BH cho cả năm. Input tự khai Missing; seed có nguồn vẫn Unverified, VerifiedOn null.
8. Lỗi rõ cho null, âm, tháng/năm sai, range sai, thiếu/trùng tháng, mode BH thiếu/cùng có, thiếu annual rule/policy, gap hoặc cấu hình sai kỳ. Không fallback năm gần nhất, không che OverflowException. Giữ constructor/lời gọi cũ qua tham số tùy chọn cuối nếu cần.

## Căn cứ và giả định đã đối chiếu
Đã đọc bài Chính phủ [Điều 8/9/10 tại VBHN112](https://xaydungchinhsach.chinhphu.vn/cach-tinh-thue-thu-nhap-ca-nhan-tu-tien-luong-tien-cong-119260623094516526.htm): thu nhập theo kỳ trừ BH/giảm trừ, biểu tháng/năm và mức gia cảnh. [Trang toàn văn VBHN112](https://xaydungchinhsach.chinhphu.vn/luat-thue-thu-nhap-ca-nhan-119260623093630882.htm) xác định lương cá nhân cư trú áp dụng từ kỳ 2026. Luật gốc 109/2025/QH15 hợp nhất với sửa đổi 09/2026/QH16; không dùng TT111 cũ làm căn cứ chính S14.

Đã xem scan [NĐ253/2026/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf) trang 33–34, 56–57: Điều 48(1)b GTBT đủ 12 tháng khi quyết toán; 48(2)a tháng phát sinh nghĩa vụ nuôi dưỡng khác 48(2)b tháng đăng ký tạm giảm; 48(2)b,c mỗi NPT chỉ cho một NNT trong năm. Điều 68(2)a,b nói nộp lớn hơn nghĩa vụ/chưa đến mức nộp, thủ tục tại khoản 4 ngoài phạm vi. Điều 69(1)a áp lương cá nhân cư trú từ kỳ 2026; chuyển tiếp nằm tại Điều 70(2), điều chỉnh khi quyết toán, không yêu cầu khai lại tháng/quý. Điều 46(2),(3), 51(1), 66(1)a theo khảo sát scan trước trong phiếu: BH năm nào trừ năm đó, thu nhập theo lúc trả/nhận, quyết toán và kỳ năm dương lịch. Chỉ diễn giải đủ cho phạm vi số học; chưa hoàn tất rà soát pháp lý.

## Tiêu chí chấp nhận
- [ ] AC-1: Khoảng giảm trừ người phụ thuộc có ID ổn định, bao hàm tháng đầu/cuối, hỗ trợ gián đoạn và cắt theo năm; từ chối khoảng sai, trùng hoặc chồng cùng ID và trả ID/khoảng/căn cứ theo từng tháng.
- [ ] AC-2: Đầu vào quyết toán có đúng 12 tháng duy nhất của năm 2026, tháng zero tường minh và tạm nộp thực tế bắt buộc; chuẩn hóa thứ tự, chọn đúng một cách nhập bảo hiểm tay hoặc tự tính S13 và không khấu trừ hai lần.
- [ ] AC-3: Thuế năm dùng tổng gross trừ tổng bảo hiểm và giảm trừ, giữ TNTT trước clamp, clamp một lần rồi áp biểu năm; trả tổng tạm nộp, chênh lệch thuế năm trừ tạm nộp, nộp thêm/nộp thừa dự kiến không âm và không cùng dương.
- [ ] AC-4: Rule năm 2026 tường minh nằm trong/liên kết TaxRuleSet, có biểu năm và căn cứ đúng kỳ; giữ GetForYear S12, chọn snapshot từng tháng, chấp nhận khác biệt bảo hiểm tháng 6/7 và từ chối thiếu/gap/mismatch hoặc thay đổi thuế giữa năm chưa hỗ trợ.
- [ ] AC-5: Kết quả bất biến giữ đủ 12 tháng, gross/bảo hiểm/giảm trừ/thuế tháng/tạm nộp, ID người phụ thuộc và provenance thực dùng; tổng năm truy được các snapshot tháng và rule năm, nguồn Unverified và số tự khai Missing, không tự rounding.
- [ ] AC-6: Validation từ chối null, ID rỗng, tiền âm, tháng/năm sai, đầu vào bảo hiểm mơ hồ và cấu hình thiếu/không khớp bằng lỗi rõ; decimal overflow không bị che và sửa collection đầu vào không đổi snapshot/kết quả.
- [ ] AC-7: Kiểm thử có expected độc lập bao phủ lịch phụ thuộc, đủ tháng, số học năm khác tổng tháng, tháng TNTT âm, ba chiều chênh lệch, biên thuế năm, bảo hiểm 6/7, lỗi và bất biến; giữ 55 ca cũ, test Core rồi solution thực chạy xanh.
- [ ] AC-8: Docs0005 ghi API, nguồn/điều khoản/kỳ, ví dụ độc lập, giới hạn ước tính và decimal, lựa chọn user tự commit cùng kết quả test thật; diff chỉ thuộc S14, không thêm package hay sửa API cũ/PLAN/config/setup/ledger cũ, review độc lập không còn lỗi nghiêm trọng.

## Ngoài phạm vi
Tự xác định cư trú, điều kiện NPT/hồ sơ/chứng từ/đăng ký hay một NPT đã khai ở hồ sơ khác; miễn thu nhập, nhiều nguồn lương, năm 2025/2027, thu nhập kinh doanh/NĐ398, thủ tục/ngưỡng miễn quyết toán và quyết định hoàn tiền thực tế. Các khoản giảm trừ khác thuộc S15. Không UI/DB/network trong Core, package/csproj/setup/PLAN/config/solution hay ledger cũ. User tự commit: tuyệt đối không git add/commit/push; chỉ Core, tests Core, docs0005 và ledger phiếu này ở b-feat. Bint Core quản lý trạng thái; verify.command giữ trống.

## Câu hỏi mở
Không có câu hỏi cản trở triển khai.

