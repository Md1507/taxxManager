# Đầu việc

- [x] TSK-1 [AC-1]: Tạo input sổ năm, tháng, payer và khoản chi bất biến cùng các guard — xong khi: đủ 12 tháng, zero tường minh, xác nhận phạm vi, ID, trùng lặp, null, số âm, năm/ngày và subject đều có test; collection được copy và không ghi được.
- [x] TSK-2 [AC-2]: Tổng hợp khoản chi theo nguồn và tháng trước một lần gọi annual, giữ actual cùng chi tiết — xong khi: ca đồng thời, đổi việc và một payer có tổng literal đúng; PaymentId không trùng, actual null bị từ chối rõ và actual 0 được giữ.
- [x] TSK-3 [AC-3]: Ghép BH nhập tay cấp cá nhân/tháng và lịch phụ thuộc vào annual một lần — xong khi: ví dụ gross 360 triệu, BH 12 triệu, NPT 6 tháng cho PIT 6,48 triệu và nộp thừa 4,32 triệu hoặc nộp thêm 480 nghìn; tăng payer không nhân giảm trừ.
- [x] TSK-4 [AC-4]: Gắn rule nhiều nguồn, factory và provenance theo snapshot thực — xong khi: guard coverage/metadata có đối chứng hợp lệ, fixture rule khác cho PIT 37,2 triệu; nguồn Unverified, input Missing; tax decimal 0,025 và overflow được kiểm độc lập.
- [x] TSK-5 [AC-5]: Giữ tương thích S11–S15c và giới hạn contract thuần lương 2026 — xong khi: API cũ và 122 test giữ semantics; không auto BH, adapter bonus/extras/casual, miễn theo payer, S15e/f, Web/DB/config/package; trách nhiệm caller được ghi rõ.
- [x] TSK-6 [AC-6]: Hoàn thiện ma trận test có expected độc lập rồi chạy Core và solution tuần tự — xong khi: đủ các nhóm trong plan, mỗi fixture lỗi có đối chứng hợp lệ; hai lệnh SDK 10.0.404 no-restore exit 0 và ghi count passed/failed/skipped thật.
- [x] TSK-7 [AC-7]: Viết docs0009 và hoàn tất bằng chứng AC1–7 trước review — xong khi: docs đủ API, căn cứ, ví dụ, giới hạn, số test thật và việc người dùng tự commit; diff đúng phạm vi, hash index và HEAD đúng baseline; AC1–7 có chứng cứ.
- [x] TSK-8 [AC-8]: Review độc lập và root chốt bàn giao — xong khi: reviewer đọc diff full/stat/name-only, đối chiếu AC và chạy test thật; không còn lỗi nghiêm trọng chưa xử lý; root ghi đúng trạng thái nguồn và bàn giao chưa stage/commit.


