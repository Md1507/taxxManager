# Nguồn và cách đưa bộ nháp vào dùng

Đây là bản nháp do người viết tổng hợp **[c]**, không phải config cá nhân của Lauren. Chưa cài vào agent, Git hoặc Noodle của dự án.

| Tệp | Căn cứ trực tiếp | Phần tự thiết kế |
|---|---|---|
| `verified-engineering/SKILL.md` | [Bảng đối chiếu từng đoạn](verified-engineering/references/provenance.md) | Ghép workflow, giới hạn thẩm quyền, điều kiện dừng |
| `AGENTS.fragment.md` | [correct](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/correct/SKILL.md), [prove-it-works](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-prove-it-works/SKILL.md) | Toàn bộ câu chữ và cách đóng gói ngắn |
| `noodle.supervised.toml` | [Configuration](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/reference/configuration.md), [Modes](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/concepts/modes.md) | Chọn một phiên và supervised; không áp model cá nhân hay quyền bỏ sandbox |
| `eslint.boundaries.example.mjs` | [boundary-discipline](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-boundary-discipline/SKILL.md), [encode-lessons](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-encode-lessons-in-structure/SKILL.md) | Rule ESLint, đường `src/electron-*` và alias `@app/*` do người viết chọn làm ví dụ; không phải mã Dune |
| `pre-commit.example` | `AGENTS.md` của **taxxManager** yêu cầu `bint test affected --run` sau sửa mã ngoài tác vụ Bint | Chọn điểm chạy pre-commit; không phải lệnh hay hook của Lauren |

**Skill:** có thể chép nguyên thư mục `verified-engineering/` vào nơi agent đang dùng tìm skill, ví dụ `.agents/skills/` trong môi trường hỗ trợ đường này. Không thêm trường `schedule`: đây là skill theo nhiệm vụ, không tự đăng ký một automation. Xem các nguồn trong references khi sửa cơ chế.

**AGENTS/CLAUDE:** ghép đoạn cần dùng vào tài liệu đúng phạm vi và sửa đường dẫn skill cho nơi đã cài. Giữ nguyên khối Bint quản lý trong repo này; không thay cả tệp bằng đoạn mẫu. Đoạn chỉ dẫn là prose, chưa tự tạo cổng CI.

**Noodle:** TOML dùng key trong phiên bản nguồn đã đọc. Kiểm schema/cấu hình của bản Noodle thực cài, provider/model đang có và scheduler trước khi chạy. Chỉ có thư mục skill mới này chưa đủ để Noodle lập lịch; nó không thay `schedule`, adapter backlog hoặc skill domain của dự án. Chưa chạy thử vòng Noodle, chưa chứng minh supervised gate của bản cài tại máy này.

**Lint:** thêm các object config vào flat config của một dự án JavaScript có layout tương ứng và chạy ESLint trong CI. Cú pháp rule theo [tài liệu ESLint](https://eslint.org/docs/latest/rules/no-restricted-imports); cấu trúc flat config theo [configuration files](https://eslint.org/docs/latest/use/configure/configuration-files). Không thêm các rule JavaScript này vào ứng dụng .NET rồi coi đã kiểm kiến trúc.

Check mẫu bao phủ import/re-export ES module trực tiếp khớp các chuỗi tên đã liệt kê. Nó **không resolve toàn bộ dependency graph**, không bao phủ alias khác, `require`, dynamic import, file TypeScript hoặc đường vòng qua module trung gian. Muốn bảo đảm invariant toàn graph cần check tương ứng với resolver thật. Không cấm `useEffect` hay comment toàn dự án bằng bộ nháp này.

**Hook:** mẫu cần Git với shell POSIX và `bint` trên PATH. Ghép vào hook pre-commit hiện hữu hoặc cơ chế hook chung của đội; đừng ghi đè hook cũ. Bint chọn test từ thay đổi chưa commit, nên chạy ở thời điểm này phù hợp hơn pre-push trên branch sạch. Nó kiểm working tree, **không cô lập snapshot staged**, không chứng minh toàn bộ diff của branch so với trunk. Hook local có thể bị bỏ qua; required CI vẫn là cổng quyết định. Không dùng nó làm gate tự merge. Lệnh này được giữ vì là quy ước dự án, không sáng tạo một lệnh Bint theo commit chưa xác minh. Phải cấu hình test command của Bint trước khi dùng; kết quả ở máy khảo sát còn bị chặn như ghi trong validation.

Không có dữ liệu thực nghiệm rằng bộ nháp tăng năng suất hoặc giảm lỗi. [validation.md](../validation.md) chỉ báo các phép kiểm đã thực chạy.
