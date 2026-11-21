# Yêu cầu

## Mục tiêu và phạm vi
Chỉ S15e/BL-23e sau S15d 01M7ZGFDSTMJRWBAH7SRD855KM đã done, theo roadmap 01M7WZSJJDNS1D97A7J19YN37T. Ước tính giảm trừ đóng góp từ thiện, nhân đạo, khuyến học cho một cá nhân Việt Nam cư trú, tiền VND trong nước thực đóng trong năm dương lịch 2026. Hỗ trợ sổ AnnualSalary một nguồn và sổ lương thông thường nhiều nguồn S15d, giữ khấu trừ thực tế và provenance. B-req deep chỉ viết ba artifact, chưa triển khai hay chạy test .NET.

## Yêu cầu chức năng
1. Input năm và từng khoản bất biến, ngày phát sinh/ID/số tiền/nhóm và định danh tổ chức/điều kiện/chứng từ tường minh; scope và xác nhận caller bắt buộc. ID Trim/Ordinal duy nhất toàn năm; reject null collection/item, số âm, ngày default/sai năm, enum default/không hỗ trợ. Zero hợp lệ; danh sách rỗng là khai không có đóng góp.
2. Ba nhóm recipient theo Điều 49(1)a,b,c; điều kiện cho phép thành lập hoặc công nhận, mục đích đúng và phi lợi nhuận được khai riêng. Chứng từ thu hợp pháp bản chụp HOẶC thanh toán không tiền mặt qua tổ chức tín dụng, hai nhánh độc lập. Tự chi/không do nguồn khác chi trả và không khai trừ trùng là xác nhận rõ của caller; không xác minh tổ chức/hồ sơ hoặc tạo duyệt từng khoản.
3. Thiếu chứng từ hoặc điều kiện hợp lệ trả line EligibleAmount=0 với mã lý do và căn cứ; dữ liệu cấu trúc sai reject tường minh. Không suy eligibility từ label/ID tổ chức hay suy ngày đóng góp từ ngày chứng từ. Chứng từ chỉ có tham chiếu, không nhận hoặc lưu tệp thật.
4. Mở overload từ input thật của AnnualSalaryCalculator và MultipleSalaryCalculator, tái sử dụng engine một lần ở cấp cá nhân/năm. Giảm thu nhập trước biểu thuế năm, cập nhật đồng bộ before-clamp, taxable, deductions, PIT, delta, nộp thêm/nộp thừa; không trừ thuế 1 đổi 1. Giữ gross/BH/giảm trừ gia cảnh và actual không đổi, không chia lại cho payer/tháng.
5. Phân biệt tổng khai, tổng đủ điều kiện và ảnh hưởng thực lên thu nhập; không bịa trần pháp lý. Khi đủ điều kiện vượt thu nhập còn lại, clamp taxable về 0, không thuế âm, không chuyển sang năm sau; nộp thừa chỉ từ actual đã khấu trừ. Thuế tháng vẫn là ước tính trước giảm trừ từ thiện quyết toán, không phải actual hay khấu trừ tính lại.
6. Rule mới gắn TaxRuleSet, chọn snapshot theo ngày đóng, kiểm coverage/kỳ/version/basis đầy đủ; nguồn mới Unverified/VerifiedOn null, input và chứng từ Missing. Dùng số thuế/giảm trừ từ seed cũ, giữ decimal và báo overflow, không mạng/fallback. Không đổi semantics API/seed cũ hoặc PitCalculator.
7. Mã/test/docs0010 và bằng chứng AC1–7 hoàn tất trước review độc lập; AC8 reviewer/root chốt sau. Expected test literal độc lập, giữ 143 test nền gồm staged S15d, chạy Core rồi solution no-restore ở b-feat. Người dùng tự stage/commit.

## Tiêu chí chấp nhận
- [ ] AC-1: AC1: Khoản đóng góp tiềnVND cá nhâncưtrú2026 input/date/ID/nhóm tổchức vàchứngtừ/điều kiện explicit, null/âm/default/unsupported/trùng bịchặn, collectioncopy/read-only.
- [ ] AC-2: AC2: Đủ3nhóm tổchức vàđiều kiện hợp pháp/phi lợi nhuận/tựchi; chứngtừreceipt HOẶCbank đúng; thiếuđiềukiệnkhôngtrừngầm córeject/reason; phầnđủđiềukiệnvàcăn cứ từngdòngđúng.
- [ ] AC-3: AC3: Tíchhợp giảmtrừ mộtlần vào thuếlươngnăm mộtnguồn/nhiềunguồn, tính lại PIT/beforeclamp/deductions/delta nhấtquán; giữactual, khôngtrừ1đổi1/thuếâm/carryover, khôngtrừlặp mỗi payer.
- [ ] AC-4: AC4: Rule/căn cứ gắnTaxRuleSet chọn đúngngày/kỳ/snapshot/coverage/version, sốluậtchỉseed, khônghardcode/gọimạng; inputMissing/nguồnUnverified, decimal/overflow giữđúng.
- [ ] AC-5: AC5: Giữsemantics API/seed và143testcũ S11–S15d gồm stagedS15d; khôngsửaindex/HEAD/stage/commit/config/PLAN/oldledger hoặc mởS15f/y tế/giáo dục/Web/DB/packages.
- [ ] AC-6: AC6: Testexpected literalđộclập đủconditions/proofbranches/years/duplicates/zero/below/equal/aboveincome/single/multiple/customrules/provenance/copy/fractions/overflow vàinvalidfixtures cóvalidcontrol; Core rồi solution thực chạy xanh no-restore.
- [ ] AC-7: AC7: Docs0010 đủAPI/côngthức/căn cứ/kỳ/no carryover/phạmvi/giớihạn/ví dụ/countthật/usercommit; AC1–7 hoàn tấttrướcreview, diffđúngscope so snapshotstagedS15d và index/HEADgiữynguyên.
- [ ] AC-8: AC8: Reviewđộclập đầyđủdiff/AC sauhoànthiện không còn lỗinghiêmtrọng chưa xửlý, rootchốtbằngchứng và bàngiao đúng nguồnUnverified/chưastagecommit S15e.

## Ngoài phạm vi
S15f hưu trí; giảm trừ y tế/giáo dục; hiện vật, ngoại tệ, kinh doanh, không cư trú, gross-up, từ thiện trực tiếp cho cá nhân; quyền hoàn/thủ tục/ủy quyền quyết toán. Không thêm bonus/extras/casual vào nhiều nguồn. Không Web/DB/packages, không sửa PLAN/config/global/solution/csproj/docs0001–0009/ledger cũ; test cũ chỉ sửa tương thích khi có bằng chứng cần và giữ assertions. Chỉ Core/tests Core/docs0010 và ledger phiếu hiện tại ở lượt triển khai. Giữ nguyên index S15d và HEAD, không stage/unstage/stash/reset/commit/push. verify.command giữ trống; không status/approve/verify dưới bint run.

## Câu hỏi mở
Không có câu hỏi cản trở triển khai. Các điều kiện và chứng từ caller khai là hợp đồng input, không phải câu hỏi cần người dùng trả lời lại trong phiên.
