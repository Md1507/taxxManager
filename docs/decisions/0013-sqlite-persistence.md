# 0013 — SQLite persistence for synthetic profiles

Bước 2.1 dùng EF Core 10.0.15 và SQLite, độc lập với Core. UserProfiles có Guid opaque và tên hiển thị tùy chọn; không thu CCCD hoặc MST. MonthlyIncomes có khóa (ProfileId, Year, Month), lưu gross, bảo hiểm nhập tay và thuế đã khấu trừ; Dependents có khóa (ProfileId, Id); DependentEligibilityPeriods có khóa (ProfileId, DependentId, EligibleFromMonth). Khóa ngoại cascade; CHECK và trigger ngăn khoảng sai hoặc chồng lấn, kể cả hai đầu tháng. Mọi connection phải bật foreign keys.

Mapping sang Core chỉ hỗ trợ một nguồn lương gross thông thường chịu thuế hoàn toàn và số bảo hiểm nhập tay. Không hỗ trợ automatic insurance, bonus, extra, casual payment, multiple payers, charitable/retirement, chứng từ hay rule snapshot. Chiều nhập Core từ chối dữ liệu ngoài tập này. Không tự tạo tháng thiếu; annual calculator yêu cầu đủ 12 tháng và caller cung cấp TaxRuleProvider.

Tiền là decimal lưu TEXT theo SQLite provider, không dùng REAL hoặc làm tròn. Không cam kết tính tổng/sắp xếp SQL; tính toán tại Core. Validation không âm chạy khi SaveChanges/SaveChangesAsync, SQL trực tiếp có thể bỏ qua; SQLite không có kiểm tra decimal chính xác tương đương qua CAST REAL. Kiểm thử dùng SQLite file thật thay cho EF InMemory. Xem [giới hạn SQLite của EF](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations) và [chiến lược kiểm thử EF](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy).

Trigger được tạo thủ công trong migration; migration tương lai rebuild bảng phải bảo toàn hoặc tạo lại trigger. Lọc ProfileId là cách tách dữ liệu logic, không phải phân quyền: DbContext truy cập được toàn bộ DB. Chỉ dùng dữ liệu giả; chưa có mã hóa, audit, xác thực hay policy retention. Không thực hiện bước 2.2/2.3.
