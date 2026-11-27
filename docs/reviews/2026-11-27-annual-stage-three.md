# Chặng 3 — kiểm chứng và review kỹ thuật

Phiếu `01M8DJDSFX7JJBRP9YZPVSG8YG`, cây chính, ngày 27/11/2026. Người dùng đã cho phép phiên gốc hoàn thiện trực tiếp sau khi workflow dừng. Phần đối chiếu ban đầu do phiên thực thi ghi; kết quả review độc lập vòng 3 được cập nhật bên dưới. Không tự accept/force/commit; trạng thái done do Bint finalize áp dụng sau khi đủ cổng. Cập nhật ngày 28/11: cổng đầy đủ đã qua sau khi người dùng cho phép sửa đúng hai assertion enum; người dùng đã tháo freeze; **Bint đã chuyển phiếu sang `done` ngày 28/11/2026**, sau review vòng 4 không có phát hiện. Lần kiểm đầy đủ cuối `verify-7.log` exit 0: Core209/Data319/Web101 và EF sạch.

## Bốn phát hiện review vòng 2

| Phát hiện | Thay đổi và bằng chứng |
|---|---|
| `.5` Thiếu luồng Web tính năm | `ForecastModel.OnPostCalculateAsync` chỉ tính revision đã lưu, chặn dữ liệu chưa lưu/stale. `_AnnualResult` hiển thị nghĩa vụ, từng bậc, metadata, nguồn và độ phủ. 28 kiểm tra `AnnualSavedProfileBlackBoxTests` nằm trong bộ Web đầy đủ. |
| `.6` Catalog thiếu graph năm | `AnnualForecastRuleCatalog` lưu References riêng cho 2026 H1/H2 từ snapshot Core hiện có, có annual/rounding/cash bonus. Kiểm catalog tháng và graph/kỳ năm đã lưu, giữ trạng thái nguồn, không fallback/reseed khi tính. Test HTTP bao phủ luật thiếu/lệch, thưởng included/excluded và thuế năm khác tổng tháng. |
| `.7` Thiếu service/history năm | `AnnualForecastService` tự đọc revision/catalog và gọi Core trong transaction, append kết quả cùng audit. `AnnualCalculationHistory` mã hóa input/result/rule snapshots, chống ghi trực tiếp/sửa/xóa qua SaveChanges, đọc được sau đổi catalog hoặc ordinary delete. Bốn test service bao phủ bất biến, khóa, sentinel, rollback và downgrade. |
| `.8` Hỏng schema legacy | `SnapshotRows` bổ sung cột NULL khi schema cũ, insert graph null bỏ cột mới. Các test legacy/copy-forward/migration đã qua; test Web mới mở host khác trên schema chặng 2, nâng cấp và tính năm, giữ nguyên payload cũ. |

Reviewer độc lập `codex/gpt-6.1-sol` đã đối chiếu lại và ghi vòng 3 ngày 28/11/2026: **findings: []**, không phát hiện bug/risk nghiêm trọng có bằng chứng; xác nhận .5–.8 đã có đường triển khai tương ứng. Chạy lại Data 16/16 và HTTP 44/44 đều qua. Reviewer đọc log cổng đầy đủ, markup/CSS và ảnh QA; không chạy lại toàn cổng hay trình duyệt tương tác trong lượt review này. Không coi findings rỗng là bảo đảm không còn lỗi.

## Kiểm chứng

- Cổng nguyên `scripts/dev/verify-web-mvp.ps1` qua `bint task verify`: **exit 0**, build 0 warning/0 error, **Core 209/209, Data 319/319, Web 101/101 (629 test)**; EF không có pending model changes. Bằng chứng Bint: `logs/verify-6.log`, thời điểm 27/11/2026 10:52:42 UTC. Không thay lệnh hoặc bỏ test khỏi cổng.
- Bộ Web chạy riêng: 101/101, bao gồm 28 test mục tiêu năm và ca nâng cấp qua host mới. EF: `No changes have been made to the model since the last migration.` Log ở `%TEMP%/taxmanager-web-final-direct.log` và `taxmanager-ef-final-direct.log`.
- Các test service mới xác nhận nghĩa vụ 11.400.000 đồng theo oracle độc lập, tính lại tạo ID mới, snapshot cũ giữ nguyên sau đổi input/catalog/xóa hồ sơ, không ghi `MonthlyIncome` hoặc lịch sử tháng, audit rollback nguyên tử, guard downgrade và thiếu/sai khóa/sentinel.
- Test Web mục tiêu giữ hash `308af8ba660e480af6cb22e4788373917a21ab92443371276940ec3cb413efc3`. Test Data chỉ sửa hai assertion theo xác nhận rõ của người dùng; hash mới `a836c3efdac06ae9fd15353b4211c7ac2984473c7f28fe779c85a0926383777a`. Toàn bộ test byte migration cũ qua trong bộ Data.

## QA trình duyệt thực

Orca embedded browser, host Development/SyntheticDemo chạy từ bản sao output trong TEMP, loopback 5088. Chỉ dữ liệu giả. Đã dừng host sau QA.

- Nhập gross 30 triệu, Enter để preview, lưu hồ sơ rồi tính. Thiếu bảo hiểm/người phụ thuộc báo chỉ tiêu thiếu. Sau khi bổ sung 0 tường minh và lưu, nghĩa vụ năm 11,4 triệu, khấu trừ 0/12 nên không có chênh lệch giả.
- Viewport 320×740, 390×844, desktop 1280×900, scale 1; ảnh đã xem. Ở 320px, `scrollWidth = innerWidth = 320`, số và nhãn xuống dòng đọc được. Scale 3 của công cụ tạo ảnh bị lặp tile, nên không dùng ảnh đó làm bằng chứng.
- Space mở phần bước tính, Tab đi tới phần dữ liệu, focus có outline. Link nguồn HTTPS nhận focus bàn phím. Escape giữ trang ổn định; disclosure native không có cơ chế đóng bằng Escape. Không khẳng định đã kiểm screenreader, thiết bị vật lý hoặc zoom native.
- Chuỗi tiền lỗi `1,234,567` được giữ nguyên, summary lỗi nhận focus (`role=alert`); không render kết quả tính từ chuỗi lỗi.

Ảnh: [kết quả 320px](annual-stage-three/result-320.png), [desktop](annual-stage-three/result-desktop.png), [lỗi và focus 390px](annual-stage-three/invalid-390.png).

## Vướng mắc test đóng băng

`AnnualStageThreeUpgradeBlackBoxTests` tự tạo payload bằng `JsonSerializer.Serialize(profile)` mặc định. `ForecastStatus` được serialize thành số (Planned=1, Assumed=2), nhưng dòng 68–69 đòi payload có hai tên enum. Test đồng thời yêu cầu payload sau nâng cấp bằng đúng payload gốc. Lỗi này xảy ra sau các assertion giữ payload/ciphertext/snapshot/audit/sentinel, không chứng minh mất trạng thái.

Ban đầu giữ test theo lựa chọn của người dùng. Sau khi xem diff cụ thể bên dưới, người dùng trả lời **“Cho phép sửa đúng hai assertion (đề xuất)”**. Đã áp dụng chính xác diff này và chạy lại nguyên cổng thành công; giữ mọi assertion payload equality, ciphertext, audit, sentinel và ngày nhập. Không thay serialization sản phẩm hoặc fingerprint/ngày nhập:

```diff
-        Assert.Contains("Planned", document.Payload);
-        Assert.Contains("Assumed", document.Payload);
         using var originalInput = JsonDocument.Parse(document.Payload!);
+        Assert.Equal(ForecastStatus.Planned, originalInput.RootElement.GetProperty("Periods")[0]
+            .GetProperty("Gross").GetProperty("Status").Deserialize<ForecastStatus>());
+        Assert.Equal(ForecastStatus.Assumed, originalInput.RootElement.GetProperty("Exceptions")[0]
+            .GetProperty("Insurance").GetProperty("Status").Deserialize<ForecastStatus>());
```

`b-feat/SKILL.md` quy định: “Kiểm tra mục tiêu đã đóng băng thì không viết lại.” Người dùng đã cho phép ngoại lệ đúng hai assertion. Người dùng đã tự chạy `bint run resume 01M8DJDSFX7JJBRP9YZPVSG8YG --drop-goal`; Bint xác nhận `goal_dropped=true` và chạy review vòng 3. Không xóa tệp test, cả hai vẫn nằm trong cổng đầy đủ. Phiên này không tự approve/drop-goal/accept/force.

## Tiến độ và phiên UI/UX

Chặng 1–2 đã tích hợp; mã chặng 3 đã triển khai, nguyên cổng kiểm chứng đã xanh và review độc lập vòng 3 không có phát hiện; Bint finalize đã hoàn thành phiếu. Sau review vòng 3, Bint giữ kết quả vì PLAN/UI_UX_PLAN/roadmap được phiên UIUX đồng bộ đồng thời; đã đối chiếu ba tài liệu, không có mã sản phẩm đổi trong bước review, giữ nguyên phần của phiên khác và tiếp tục từ review đã lưu. Bint yêu cầu kiểm lại cây đã đồng bộ: `verify-7.log` qua 629 test/EF; rồi chạy review vòng 4 (0 phát hiện, Data19/19 và HTTP44/44 chạy lại qua) trước khi tự chốt done. Cập nhật trạng thái tài liệu sau done không đổi mã sản phẩm/test hay nội dung UI_UX_PLAN của phiên khác. Chặng 4–6 chưa triển khai. Có thể chuẩn bị plan UI/UX song song ở phiên khác; trước khi code cần đối chiếu form thưởng/người phụ thuộc, kết quả năm và lịch sử mới để tránh đụng cùng tệp. `UI_UX_PLAN.md` và nghiên cứu Lauren Tan thuộc phiên khác, được giữ nguyên.
