# Đối chiếu lại worktree Bint đã lưu archive — 28/11/2026

Đây là báo cáo đối chiếu mới từ dữ liệu còn trên máy, không phải khôi phục nguyên văn tài liệu `2026-11-27-bint-worktrees.md` đã mất. Không chạy lại thao tác di chuyển/xóa worktree hoặc bộ test sản phẩm.

## Nguồn đã kiểm

Thư mục trạng thái: `C:/Users/LENOVO/AppData/Local/bint/state/taxxManager-59e6ed325af2`.
- `runtime/tasks/<task>/state.json`: trạng thái, landed.tree, landed.files.
- `runtime/tasks/01M89PFQ3ETQDTCSGPMAN0DEPW/review.md:187` và `runtime/tasks/01M8BK6R1EC84DFJ05Z8FH31C2/review.md:192`: biên bản đối chiếu ngày 27/11, ghi đã chuyển hai thư mục sang archive, giữ junction SDK và doctor hết cảnh báo worktree mồ côi. Đây là bằng chứng lịch sử ghi lại, không phải thao tác được thực hiện lại hôm nay.
- `archive/worktree-reconciliation-20261127/<task>`: hai thư mục hiện vẫn có thật.
- Git object database của repository: các snapshot và commit tích hợp dưới đây vẫn đọc được.

## Kết quả đối chiếu từng tệp

| Tác vụ | Snapshot landed.tree | Commit tích hợp | Số tệp | Archive so với snapshot | Snapshot so với commit |
|---|---|---|---:|---|---|
| `01M89PFQ3ETQDTCSGPMAN0DEPW` | `b6ddfe529305fc705085f0e59fb156ae9ffd3a8a` | `baf2c9d7c5be324028a6f9d98211670ace7a44c3` | 15 | 15/15 khớp | 14/15 khớp; khác PLAN.md |
| `01M8BK6R1EC84DFJ05Z8FH31C2` | `b416e5539ed6de41b73b5b39e0eafb874b058a68` | `f4f3133dd035f70caa56172e7fe40bf6ac85c3fd` | 30 | 30/30 khớp | 30/30 khớp |

Đối chiếu Git blob ID theo từng đường dẫn trong landed.files. Nội dung archive được đưa qua `git hash-object --stdin --path=<path>` để áp dụng quy tắc lưu Git của repository; đây là so sánh biểu diễn Git, không khẳng định mọi tệp CRLF/LF giống từng byte trên đĩa. Không ghi object mới và không sửa migration.

Khác biệt PLAN.md của tác vụ đầu đã được đọc bằng `git diff <snapshot> <commit> -- PLAN.md`: commit bổ sung roadmap sáu chặng, cập nhật trạng thái 3.1b và diễn giải phụ thuộc. Đây là tài liệu đã được cập nhật ở cây tích hợp; mã sản phẩm trong tập landed.files không khác snapshot. Vì vậy không được khẳng định cả 15 tệp đều giống commit, cũng không nên khôi phục PLAN.md cũ từ archive.

## Junction SDK và đăng ký worktree

Cả hai thư mục archive có `.dotnet` mang thuộc tính Windows ReparsePoint; đích junction là `D:/Project/taxxManager/.dotnet` và vẫn tồn tại. Chỉ đọc metadata của link, không duyệt đệ quy qua junction.

Tệp `.git` trong từng archive vẫn trỏ tới `.git/worktrees/<task>` của repository, nhưng `git worktree list --porcelain` hiện chỉ liệt kê cây chính. `bint doctor --json` hiện không có cảnh báo worktree mồ côi; các cảnh báo khác về cấu hình/sandbox không thuộc kết luận này.

## Hướng dẫn rút ra

Khi cần dọn worktree Bint còn sót, kiểm landed.tree, landed.files, commit đã tích hợp và các tệp còn lại; status done hoặc metadata Git còn sót không đủ chứng minh an toàn. Giải thích riêng các khác biệt tài liệu đã bị thay thế, giữ lại mọi thay đổi chưa được đối chiếu. Kiểm reparse point/junction trước thao tác filesystem; tránh xóa đệ quy xuyên `.dotnet`. Với thư mục đã đối chiếu, lưu archive ngoài vùng worktrees giúp giữ khả năng khôi phục; việc dọn thực tế cần xác định rõ thư mục đích và không thực hiện trong lượt sửa knowledge này.

## Cách đối chiếu lại

1. Đọc state.json và lấy đúng danh sách landed.files, landed.tree; không suy từ status đơn thuần.
2. Với từng tệp, dùng `git rev-parse <snapshot>:<path>` và `git rev-parse <commit>:<path>`; đọc diff khi blob khác.
3. Đọc tệp archive, tính Git blob bằng `git hash-object --stdin --path=<path>` rồi đối chiếu snapshot.
4. Dùng Get-Item -LiteralPath cho chính `.dotnet` để xem Attributes/LinkType/Target; kiểm git worktree list và bint doctor. Các lệnh này chỉ đọc, không có bước dọn/xóa.
