# Kiểm chứng bộ nháp — 27/11/2026

Đây là kiểm tra artifact do người viết tạo, không phải số liệu về năng suất của Lauren hay kết quả thử nghiệm agent.

| Phép kiểm đã thực chạy | Kết quả | Giới hạn |
|---|---|---|
| `quick_validate.py` của skill-creator, chạy Python với `-X utf8` | **PASS**: `Skill is valid!` | Kiểm frontmatter/định dạng, không đo hiệu quả hành vi |
| Python `tomllib` đọc `draft/noodle.supervised.toml` | **PASS**; mode supervised, concurrency/process limit đều bằng 1 | Chưa chạy Noodle, chưa xác nhận schema của một bản cài khác |
| ESLint **9.39.5** với config mẫu, trong thư mục TEMP | **PASS theo kỳ vọng**: 4 ca hợp lệ qua; 8 ca sai bị báo đúng `no-restricted-imports` | Chỉ kiểm JavaScript ES module và các đường tên đã khai báo |
| Hai ca ngoài phạm vi lint: dynamic import, `require` | Cả hai không bị báo | Xác nhận giới hạn; không coi là chứng minh boundary toàn graph |
| Git POSIX `sh -n draft/pre-commit.example` | **PASS** | Kiểm cú pháp shell, chưa cài hay chạy hook trong Git |
| SHA-256 của nguồn tải về so với manifest | **PASS**, khớp **305/305** tệp tại cache | Chứng minh tính nhất quán của bản lưu, không kiểm toán tuyên bố vận hành |
| Đếm cây nguồn và đọc YAML frontmatter | **PASS**: pstack 59 skill chính, gồm 24 nguyên tắc; 22 playbook; Noodle 29 skill, 8 có schedule | Chỉ phản ánh commit đã ghim |
| Kiểm đường dẫn Markdown nội bộ sau khi hoàn tất | **PASS** | Không chứng minh mọi URL bên ngoài truy cập được |
| `bint test affected --run` | **BỊ CHẶN**: `lỗi: bgraph đòi chạy cả bộ test nhưng chưa có eval.test_command` | Không có kết quả test ứng dụng từ lệnh này; không thay cấu hình dự án để vượt cổng |

Fixture lint được tạo dưới TEMP, không thêm dependency vào dự án. Tám ca sai gồm hai chiều main/renderer, import tương đối, alias gốc, alias con và re-export. ESLint trả exit code 1 do các vi phạm cố ý; từng diagnostic và số lỗi được đối chiếu với kỳ vọng.

Lượt đầu validator skill gặp lỗi giải mã Windows cp1252; chạy lại với `python -X utf8` thành công. Không sửa validator hệ thống để vượt lỗi.

Không có phép eval hành vi so sánh trước/sau, đo token, kiểm thử runtime Noodle, kiểm thử CI/merge thật hoặc kiểm chứng toàn bộ hook của repo Noodle. **Tôi không biết, tôi không có thông tin về điều này.** — áp dụng cho hiệu quả thực nghiệm của bộ nháp.

Mọi file vẫn là bản nháp trong thư mục nghiên cứu; chưa kích hoạt skill, sửa AGENTS/CLAUDE đang dùng, cài hook hay đổi quyền tự merge.
