# Danh mục pstack và Noodle tại commit đã đọc

Nhãn chung: **[a, artifact]**. Mục đích và bước được người viết diễn đạt lại từ tệp gốc; không suy rằng mọi tệp đều do Lauren tự viết hoặc cô đang dùng hết chúng. Không lấy README của một fork để suy cấu trúc upstream.

pstack: commit `b7b93c36be9bcc36a74db438e0fd385d660bc559`, manifest `0.15.48`, ngày commit 26/11/2026. Noodle: commit `82d2921c52370f23f29086de81ccfb600939c037`, ngày commit 19/03/2026. Ngày commit không phải lịch sử phát hành của từng skill.

## pstack: 35 skill workflow

Đường nguồn: `pstack/skills/<tên>/SKILL.md`. Tất cả trong nhóm này có `disable-model-invocation: true` trừ `setup-pstack`; vì vậy trigger ở bảng là yêu cầu/lệnh phù hợp, không khẳng định agent tự động tìm thấy skill. Router có thể đọc tệp liên quan. Các model mặc định của source không được coi là model khả dụng ở môi trường khác.

| Skill | Mục đích / khi gọi | Tuyến bước chính |
|---|---|---|
| [architect](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/architect/SKILL.md) | Thiết kế trước khi code qua boundary | Xem cách caller dùng → kiểu/giao diện/module → đồng hành triển khai |
| [arena](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/arena/SKILL.md) | N thử cùng một bài | Định bài → chạy độc lập → chấm chéo → chọn nền → ghép → kiểm |
| [automate-me](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/automate-me/SKILL.md) | Tạo/sửa skill theo phong cách của người dùng | Tìm skill cũ → đọc lịch sử phù hợp → hỏi sở thích → gom → nháp → sửa |
| [benchmark-checklist](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/benchmark-checklist/SKILL.md) | Có số đo tăng/giảm hiệu năng | Kiểm limiter, sai số, lặp lại, ý nghĩa end-to-end → báo số có căn cứ |
| [blast-radius](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/blast-radius/SKILL.md) | Diff nhỏ nhưng có thể ảnh hưởng xa | Truy dependency/caller → tìm giả định an toàn → chạy chứng minh |
| [bro](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/bro/SKILL.md) | Cần diễn đạt lại câu trả lời dễ hiểu | Viết lại thông điệp trước bằng lời thông thường |
| [correct](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/correct/SKILL.md) | Lỗi agent được sửa đi sửa lại | Gom lớp lỗi → thiết kế/kiểu/check/test → chứng minh → bảng enforcement |
| [create-formal-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/create-formal-verification-skill/SKILL.md) | Property khó kiểm hết bằng test | Chọn property → chọn tool → tạo model/harness/skill → chạy chứng minh |
| [create-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/create-verification-skill/SKILL.md) | Chưa có cách điều khiển app để kiểm | Khảo sát repo → launch/doctor/drive/evidence/cleanup → feature map → chạy thử |
| [figure-it-out](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/figure-it-out/SKILL.md) | Không có playbook hẹp phù hợp | Định bài → thiết kế quy trình kiểm được → vòng giả thuyết → log → kiểm |
| [how](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/how/SKILL.md) | Hỏi cơ chế, quyền sở hữu hoặc vị trí code | Truy cấu trúc và runtime → giải thích theo lớp |
| [in-your-own-words](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/in-your-own-words/SKILL.md) | Cần kiểm agent hiểu đúng mục tiêu | Phát biểu lại vấn đề và kết quả mong muốn |
| [interrogate](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/interrogate/SKILL.md) | Review đối kháng / tìm điểm mù | Định scope → reviewer thử phá → chạy lại finding → tổng hợp lỗi thật |
| [ketchup](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/ketchup/SKILL.md) | Cập nhật từ lần người dùng nhắn gần nhất | Gom thay đổi → trình bày rõ → liệt kê quyết định còn chờ |
| [ketchup-video](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/ketchup-video/SKILL.md) | Cần bản ketchup bằng video ngắn | Nội dung ketchup → slide → phụ đề → render |
| [maintain-formal-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/maintain-formal-verification-skill/SKILL.md) | Code được model hóa đã đổi | Tìm drift → sửa model/harness/property → chạy → phân biệt drift với bug |
| [maintain-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/maintain-verification-skill/SKILL.md) | Feature map/driver đã lệch app | Đọc source từng feature → chạy một lượt sống → sửa điều đã chứng minh |
| [make-bot-ui](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/make-bot-ui/SKILL.md) | UI kích hoạt Grok Bot bằng webhook | Tạo routine → lấy endpoint/key → phục vụ UI → Tailscale → xử lý wake |
| [no-comments](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/no-comments/SKILL.md) | Trước review hoặc yêu cầu kiểm comment | Gọi Comment Sicko → đánh giá finding → sửa phần chấp nhận → đề xuất encoding |
| [poteto-help](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-help/SKILL.md) | Chưa biết chọn skill/playbook nào | Tìm nhu cầu → giải thích phần cần → đưa prompt để người dùng gọi |
| [poteto-mode](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/SKILL.md) | Điểm vào phong cách/workflow của poteto | Mục tiêu → nguyên tắc → chọn playbook → điều phối → kiểm → báo |
| [recall](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/recall/SKILL.md) | Bắt đầu/tiếp tục một chủ đề trước đây | Ghép lịch sử được phép, trạng thái hiện tại, record chung → brief |
| [reflect](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/reflect/SKILL.md) | Rút bài học từ phiên hiện tại | Đọc transcript → lọc bài học bền → đề xuất đích sửa → chờ chấp thuận trước sửa skill |
| [reframe](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/reframe/SKILL.md) | Cần thuật ngữ chuyên môn để diễn đạt bài toán | Tìm từ chuyên môn và miền gần → viết lại mô tả |
| [setup-pstack](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/setup-pstack/SKILL.md) | Chọn model và mức ngân sách | Phát hiện model → đọc lựa chọn cũ → chọn vai trò → xác thực → ghi MDC |
| [sharpen](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/sharpen/SKILL.md) | Làm prompt sâu hơn trước khi thực hiện | Làm rõ chuyên môn/phương pháp/chứng cứ → trả prompt mới |
| [show-me-your-work](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/show-me-your-work/SKILL.md) | Công việc dài hoặc không giám sát | Ghi quyết định dạng TSV → nối bằng chứng/kết quả → audit |
| [statechart](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/statechart/SKILL.md) | Xem hành vi như đường đi qua trạng thái | Viết spec TLA+ → chạy TLC → view theo người dùng → chart và đường lỗi |
| [swarm](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/swarm/SKILL.md) | Chia coverage hoặc chạy race | Định kết quả/luật chọn → cô lập worker → chạy → gom verdict/gap |
| [tdd](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/tdd/SKILL.md) | Được yêu cầu hoặc bug có test hẹp rẻ | Test đỏ → sửa → chạy; nếu không khả thi giữ chứng cứ khác |
| [teach](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/teach/SKILL.md) | Cần hiểu subsystem hoặc thay đổi | Kết hợp how và why → giải thích cơ chế, lý do theo thứ tự dễ hiểu |
| [technical-writing](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/technical-writing/SKILL.md) | Tài liệu, RFC, PR, commit | Chọn loại tài liệu → tổ chức nội dung → câu rõ → chỉnh giọng |
| [typescript-best-practices](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/typescript-best-practices/SKILL.md) | Đọc/sửa TypeScript | Áp nguyên tắc kiểu vào cú pháp và boundary |
| [unslop](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/unslop/SKILL.md) | Chỉnh văn bản | Bỏ mẫu câu AI, nói cụ thể và trực tiếp |
| [why](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/why/SKILL.md) | Hỏi lý do/lịch sử quyết định | Truy commit/PR và hồ sơ gốc → dẫn căn cứ → đánh dấu suy luận/thiếu |

## pstack: 24 skill nguyên tắc

Đường nguồn vẫn là `pstack/skills/<tên>/SKILL.md`. Cả nhóm có `disable-model-invocation: true`. Đây là quy tắc quyết định theo tình huống, không phải playbook nhiều bước; router đọc leaf được áp dụng.

| Skill | Trigger / mục đích | Hành động chính |
|---|---|---|
| [principle-attack-the-premise](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-attack-the-premise/SKILL.md) | Nhiều sửa lỗi cùng tiền đề đều thất bại | Kiểm tiền đề trực tiếp |
| [principle-boundary-discipline](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-boundary-discipline/SKILL.md) | Validation, adapter, lỗi | Đặt guard ở boundary, giữ logic nội bộ rõ |
| [principle-build-the-lever](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-build-the-lever/SKILL.md) | Công việc đáng tự động hóa/kiểm lại | Tạo công cụ có thể chạy lại |
| [principle-encode-lessons-in-structure](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-encode-lessons-in-structure/SKILL.md) | Sắp viết lại cùng một chỉ dẫn | Biến thành check/cấu trúc |
| [principle-exhaust-the-design-space](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-exhaust-the-design-space/SKILL.md) | Thiết kế chưa có tiền lệ | Thử vài phương án để so sánh |
| [principle-experience-first](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-experience-first/SKILL.md) | Đánh đổi UX/phạm vi | Ưu tiên trải nghiệm hoàn thiện |
| [principle-explain-the-number](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-explain-the-number/SKILL.md) | Đo hoặc báo một con số | Xác định đang đo đúng công việc và limiter |
| [principle-fix-root-causes](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-fix-root-causes/SKILL.md) | Debug | Truy nơi đầu tiên hệ thống sai |
| [principle-foundational-thinking](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-foundational-thinking/SKILL.md) | Chọn kiểu, dữ liệu, scaffold | Chốt nền tảng để logic đơn giản |
| [principle-guard-the-context-window](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-guard-the-context-window/SKILL.md) | Context tăng bởi payload lớn | Tách xử lý lớn, giữ phần tổng hợp |
| [principle-laziness-protocol](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-laziness-protocol/SKILL.md) | Diff/abstraction phình | Chọn thay đổi nhỏ giải được việc |
| [principle-make-operations-idempotent](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-make-operations-idempotent/SKILL.md) | Retry, crash, chạy lại | Thiết kế hội tụ cùng trạng thái |
| [principle-migrate-callers-then-delete-legacy-apis](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-migrate-callers-then-delete-legacy-apis/SKILL.md) | Thay API nội bộ | Chuyển caller và bỏ API cũ |
| [principle-minimize-reader-load](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-minimize-reader-load/SKILL.md) | Code khó lần theo | Giảm lớp và trạng thái phải nhớ |
| [principle-model-the-domain](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-model-the-domain/SKILL.md) | Nhiều nhánh/giả định hình dạng | Thể hiện miền bằng cấu trúc |
| [principle-never-block-on-the-human](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-never-block-on-the-human/SKILL.md) | Việc hoàn tác được chờ quyết định thừa | Tiến hành trong phạm vi được giao |
| [principle-outcome-oriented-execution](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-outcome-oriented-execution/SKILL.md) | Migration có phase | Hội tụ kiến trúc đích |
| [principle-prove-it-works](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-prove-it-works/SKILL.md) | Sắp tuyên bố hoàn tất | Kiểm artifact thực |
| [principle-redesign-from-first-principles](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-redesign-from-first-principles/SKILL.md) | Gắn yêu cầu mới vào thiết kế | Xem lại như yêu cầu nền tảng |
| [principle-separate-before-serializing-shared-state](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-separate-before-serializing-shared-state/SKILL.md) | Nhiều actor cùng ghi | Loại chia sẻ trước khi khóa/tuần tự hóa |
| [principle-sequence-verifiable-units](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-sequence-verifiable-units/SKILL.md) | Nhiều bước/commit/PR | Mỗi đơn vị kết thúc bằng kiểm |
| [principle-subtract-before-you-add](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-subtract-before-you-add/SKILL.md) | Thêm/refactor | Dọn phần thừa trước |
| [principle-test-behavior-not-implementation](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-test-behavior-not-implementation/SKILL.md) | Viết/sửa/giữ test | Đo điều người dùng code quan sát được |
| [principle-type-system-discipline](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-type-system-discipline/SKILL.md) | Chọn type/chữ ký | Biểu diễn trạng thái hợp lệ, parse ở biên |

## pstack: định nghĩa agent, playbook và automation

| Thành phần | Trigger | Nội dung và định dạng |
|---|---|---|
| [poteto-agent](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/agents/poteto-agent.md) | Delegate của một bước | Markdown + YAML `name`, `description`, `is_background`, `model`; đọc poteto-mode trước |
| [comment-sicko](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/agents/comment-sicko.md) | [no-comments](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/no-comments/SKILL.md) | Reviewer comment; danh sách ngoại lệ; báo cáo, không tự sửa code |
| [Playbooks](https://github.com/cursor/plugins/tree/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks) | Router theo loại công việc | 22 tệp Markdown; bước thực hiện và hợp đồng báo cáo |
| [worker.md](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/worker.md) | Bắt đầu worker | Truyền quy tắc operator và cách phối hợp |
| [setup-benny](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/automations/benny/skills/setup-benny/SKILL.md) | Cài hoặc đổi automation Benny | Copy pack, cấu hình ngoài, tạo automation khi được yêu cầu |
| [triage-issue-reports](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/automations/benny/skills/triage-issue-reports/SKILL.md) | Chỉ từ automation triage đã cấu hình | Đóng băng thread → đọc report → đối chiếu → dedupe → verdict/ticket đúng phạm vi |
| [reproduce-and-fix-issues](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/automations/benny/skills/reproduce-and-fix-issues/SKILL.md) | Automation repro sau dấu triage hợp lệ | Tái hiện UI → kiểm fix có sẵn hoặc sửa có giới hạn → chứng minh trước/sau → draft PR |

Ba skill Benny là pack **dormant**, không tự trở thành slash skill từ manifest pstack. Không coi việc đọc chúng là yêu cầu gửi Slack hay tạo automation.

22 playbook: `authoring-a-skill`, `autonomous-run`, `autopilot-full`, `autopilot-stack`, `babysit`, `bug-fix`, `eval`, `feature`, `hillclimb`, `investigation`, `multi-phase-plan`, `opening-a-pr`, `pause-safely`, `perf-issue`, `prototype`, `refactoring`, `runtime-forensics`, `session-pickup`, `shipping`, `trace-forensics`, `visual-parity`, `worktree-cleanup`.

## Noodle: 29 skill nội bộ

Đường nguồn: `.agents/skills/<tên>/SKILL.md`. Markdown + YAML `name`, `description`; các dòng có **S** còn có `schedule`. Tín hiệu schedule là văn xuôi cho scheduler. Các skill khác được agent gọi theo nhu cầu; không vì có description mà tự được Noodle dispatch.

| Skill | Trigger / mục đích | Tuyến bước chính |
|---|---|---|
| [adversarial-review](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/adversarial-review/SKILL.md) | **S**: sau diff lớn, phase hoặc plan | Chọn scope → đọc nguyên tắc → reviewer khác model → kiểm finding → kết luận |
| [ast-grep](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/ast-grep/SKILL.md) | Tìm theo hình dạng AST | Viết pattern/rule → thử trên mẫu → truy vấn/codemod |
| [brain](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/brain/SKILL.md) | Đọc/ghi tri thức bền | Đọc trước → một chủ đề/tệp → wikilink → cập nhật index → dọn cũ |
| [codex](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/codex/SKILL.md) | Giao việc cho Codex CLI | Chọn profile/model → exec/resume → theo dõi → kiểm kết quả |
| [commit](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/commit/SKILL.md) | Người dùng yêu cầu commit | Kiểm thay đổi/nhánh → thông điệp conventional → commit phạm vi |
| [debugging](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/debugging/SKILL.md) | Có lỗi hoặc hành vi lạ | Tái hiện → giả thuyết → cô lập nguyên nhân → sửa → verify |
| [execute](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/execute/SKILL.md) | **S**: backlog có plan sẵn sàng | Scope → chia việc → worktree/triển khai → verify → commit → yield |
| [find-skills](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/find-skills/SKILL.md) | Tìm năng lực chuyên biệt có sẵn | Tìm skill → xem độ phù hợp → cài theo quy ước dự án |
| [frontend-design](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/frontend-design/SKILL.md) | Xây UI mới | Chọn hướng thiết kế → triển khai → tinh chỉnh thẩm mỹ |
| [go-best-practices](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/go-best-practices/SKILL.md) | Sửa/review Go | Áp quy tắc lifecycle, concurrency, config, test |
| [interaction-design](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/interaction-design/SKILL.md) | Chuyển động và phản hồi tương tác | Chọn timing/pattern → triển khai → accessibility |
| [make-interfaces-feel-better](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/make-interfaces-feel-better/SKILL.md) | Độ tinh của UI | Đối chiếu nguyên tắc thị giác/motion → chỉnh chi tiết |
| [meditate](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/meditate/SKILL.md) | **S**: sau nhiều reflect, không quá thường | Snapshot → audit → review → lọc → sửa/dọn kho nhớ |
| [noodle](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/noodle/SKILL.md) | CLI/config/viết skill cho Noodle | Đọc schema/reference phù hợp → thao tác CLI/config/orders |
| [oops](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/oops/SKILL.md) | **S**: build/test/state/môi trường hỏng | Tái hiện → kiểm state/môi trường/config → sửa → verify → commit |
| [plan](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/plan/SKILL.md) | **S**: backlog cần plan và chưa có | Phân loại độ lớn → scope → khám phá → domain skill → phase plan → trình bày/yield |
| [quality](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/quality/SKILL.md) | **S**: sau execute | Scope → chất lượng → nguyên tắc → test → runtime → stage_message chặn hoặc thông qua |
| [react-best-practices](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/react-best-practices/SKILL.md) | Code/review React và hiệu năng | Chọn nhóm quy tắc → đọc reference → áp vào thay đổi |
| [refine](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/refine/SKILL.md) | Todo chưa rõ | Đọc backlog → tìm chỗ mơ hồ → hỏi gộp → viết lại tự đủ ngữ cảnh |
| [reflect](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/reflect/SKILL.md) | **S**: sau quality | Đọc index/phiên → lọc → enforcement/brain/skill/backlog → cập nhật |
| [review](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/review/SKILL.md) | Review code hoặc plan | Scope → context → kiến trúc → chất lượng → test → hiệu năng → phương án |
| [ruminate](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/ruminate/SKILL.md) | Khai thác lịch sử phiên cũ | Snapshot brain → tìm/extract phiên → phân tích → đối chiếu tri thức → ghi nhận |
| [schedule](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/schedule/SKILL.md) | **S**: orders rỗng/backlog đổi/cần xét lại | Đọc mise → chọn plan và dependency → viết orders-next → loop promote |
| [skill-creator](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/skill-creator/SKILL.md) | Tạo/sửa skill | Chọn cấu trúc → viết chỉ dẫn/tài nguyên → kiểm/hiệu chỉnh |
| [testing](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/testing/SKILL.md) | Test/fixture/bug | Tái hiện → test đỏ → commit test → sửa → verify → commit fix |
| [todo](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/todo/SKILL.md) | Thêm/hoàn tất/xem backlog | Giữ ID bền → sửa checkbox → tăng next-id → archive khi hoàn tất |
| [ts-best-practices](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/ts-best-practices/SKILL.md) | Viết/review TypeScript | Variant/union rõ → validate boundary → narrowing/exhaustiveness |
| [unslop](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/unslop/SKILL.md) | Biên tập văn bản | Bỏ cách viết chung chung và khuôn mẫu |
| [worktree](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/worktree/SKILL.md) | Session song song/cần checkout riêng | Kiểm worktree → tạo → chạy bằng exec → merge → dọn |

Các skill frontend/React/skill-creator có nội dung tích hợp hoặc nguồn tham khảo khác; kho thuộc poteto không đủ để quy toàn bộ học thuyết trong đó cho cô. Việc kiểm tác giả từng đoạn: **Tôi không biết, tôi không có thông tin về điều này.**

## Noodle: config, hook, lint và dữ liệu

| Tệp / thư mục | Nội dung trực tiếp đọc được |
|---|---|
| [CLAUDE.md](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/CLAUDE.md) | Brain, workflow, `pnpm build` và `pnpm check`, quy ước Go |
| [.codex/config.toml](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.codex/config.toml) | Model và reasoning; sandbox/approval; fallback CLAUDE; cache env; edit profile. Đọc như artifact lịch sử, không khuyến nghị chép quyền thực thi |
| [Configuration](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/reference/configuration.md) | `.noodle.toml`: mode, routing, skills paths, concurrency, agents, runtime, server, adapters |
| [schedule](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/schedule/SKILL.md) | `.noodle/mise.json` là đầu vào; ghi `.noodle/orders-next.json`; loop mới promote sang orders chính |
| [.claude/settings.json](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.claude/settings.json) | Hook PreToolUse, SessionStart, PostToolUse và matcher |
| [inject-brain.sh](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/hooks/inject-brain.sh) | Đưa index kho nhớ vào đầu phiên |
| [auto-index-brain.sh](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/hooks/auto-index-brain.sh) | Đối chiếu tệp với wikilink rồi dựng lại index |
| [block-sleep.sh](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/hooks/block-sleep.sh) | Đọc đầu vào hook, chặn mẫu lệnh sleep đơn giản; không phải bộ phân tích shell đầy đủ |
| [scripts/lint-arch.sh](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/scripts/lint-arch.sh) | Kiểm giới hạn kích thước, fixture legacy, thông điệp lỗi trong một phạm vi; diagnostic có hướng sửa |

Các shell hook dùng đường dẫn/môi trường của repo gốc. Chưa chạy những hook Noodle này ở Windows; không khẳng định tương thích khi sao chép nguyên trạng.
