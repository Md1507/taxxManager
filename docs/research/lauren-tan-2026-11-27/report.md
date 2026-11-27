# Lauren Tan (@poteto): nghiên cứu AI coding và bản nháp cấu hình

Ngày khảo sát: **27/11/2026**. Phạm vi: Lauren Tan gắn với React Compiler, pstack, Noodle và Grok Bot. Đây là báo cáo nguồn mở công khai; không phải kiểm toán kết quả nội bộ của cô.

**[c] Tổng hợp:** bằng chứng vững nhất chỉ về cách thiết kế môi trường làm việc: cho agent chạy sản phẩm và quan sát kết quả; chuyển lỗi lặp lại thành ràng buộc thực thi được; chỉ tăng tự động hóa khi vòng làm việc đã kiểm chứng được. Không có bằng chứng rằng chỉ cài pstack sẽ tái tạo năng suất của cô. Nguồn trực tiếp cho ba thành phần này là [create-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/create-verification-skill/SKILL.md), [correct](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/correct/SKILL.md) và [các chế độ Noodle](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/concepts/modes.md).

Quy ước bằng chứng:

- **[a]**: phát biểu trực tiếp trong transcript của bên tổ chức, hoặc nội dung cấu hình/mã nguồn trong dự án của cô. Với mã nguồn, đây là bằng chứng về artifact, không mặc nhiên chứng minh cô tự viết từng dòng hoặc dùng nó hàng ngày.
- **[b] [GIÁN TIẾP]**: bên thứ ba đăng lại, tóm tắt hoặc trích dẫn. Không trích nguyên văn từ nhóm này, không chốt số liệu bằng nó, không dùng nó làm căn cứ duy nhất cho skill/config.
- **[c]**: phân tích hoặc lựa chọn triển khai của người viết báo cáo. Không gán thành lời khuyên của Lauren.
- “Đã xác minh” chỉ có nghĩa xác minh nguồn chứa nhận định. Phát biểu về sản lượng và chất lượng vẫn là tự báo cáo, chưa được kiểm toán độc lập.

Các bản đăng lại cùng một bài/video **không phải các chứng cứ độc lập về sự kiện**. Mỗi nguyên tắc trong bộ nháp đều có nguồn GitHub trực tiếp; phần gián tiếp chỉ dùng đối chiếu và lập danh sách cần kiểm tra tiếp.

## 1. Bảng nguồn và cách tiếp cận

| ID | Nguồn, loại | Ngày / phiên bản | Link | Tin cậy và giới hạn |
|---|---|---|---|---|
| S01 | Hồ sơ GitHub của poteto; tự giới thiệu | Đọc 27/11/2026 | [GitHub](https://github.com/poteto) | Cao cho liên kết tài khoản; không có ngày chuyển công ty |
| S02 | React team; nguồn dự án | Trang hiện hành | [Meet the Team](https://react.dev/community/team) | Cao cho danh tính/compiler; chức danh công ty có thể chậm cập nhật |
| S03 | React Compiler 1.0; bài đồng tác giả | 07/10/2025 | [React](https://react.dev/blog/2025/10/07/react-compiler-1) | Cao cho đóng góp React; không suy từ đây ra quan điểm AI |
| S04 | Lauren: rời Netflix; blog cá nhân | Hiển thị 17/02/2020 | [Does It Spark Joy?](https://www.no.lol/2020-01-17-does-it-spark-joy/) | Trực tiếp; slug và ngày hiển thị khác tháng, giữ ngày hiển thị |
| S05 | Lauren: rời Meta, gia nhập Cursor; bài LinkedIn | Không chốt ngày tuyệt đối | [Bài đăng](https://www.linkedin.com/posts/laurenelizabethtan_i-left-meta-3-weeks-ago-after-6-fulfilling-activity-7447312270633725952-AFhF) | Đọc được đoạn bài trong kết quả tìm kiếm; không coi nhãn tuổi bài tương đối là ngày |
| S06 | All Code Is Slop (Except Mine); talk/video/transcript | Video công bố 14/11/2026, 17:31 | [Trang talk](https://ai.engineer/talks/O_zcc6Xl12g-all-code-is-slop-except-mine), [bản Markdown có Full transcript](https://ai.engineer/talks/O_zcc6Xl12g-all-code-is-slop-except-mine.md), [video](https://www.youtube.com/watch?v=O_zcc6Xl12g) | Nguồn trực tiếp của đơn vị tổ chức; phân biệt bài đọc biên tập với Full transcript có timestamp; ASR có lỗi tên riêng |
| S07 | Trang sự kiện AI Engineer CODE 2026 | Sự kiện 2026 | [Diễn giả/talk](https://ai.engineer/code/2026/talks/lauren-tan) | Xác nhận tên và SpaceXSI; chưa dùng để chốt ngày diễn thuyết cụ thể |
| S08 | How Cursor Turned AI Agents Into Better Engineers; Maven | 12/08/2026; 16:00 UTC, 60 phút | [Maven](https://maven.com/p/e23d9c/how-cursor-turned-ai-agents-into-better-engineers) | Đọc trực tiếp mô tả, thông tin sự kiện, các chương; chưa lấy được nội dung bản ghi đầy đủ |
| S09 | The Rise Of AI Coworkers; Lightcone/YC | 14/11/2026, 60:06 | [Video gốc](https://www.youtube.com/watch?v=y1-9yeYLJUo), [YC Library](https://www.ycombinator.com/library/n0-the-rise-of-ai-coworkers) | Đọc trực tiếp metadata/chapter video; chưa lấy được captions đầy đủ; không gán lời host cho Lauren |
| S10 | Matt Pocock phỏng vấn poteto | 03/10/2026 UTC, 65:36 | [Video gốc](https://www.youtube.com/watch?v=MN9dGgmLyso) | Metadata YouTube ghi 02/10 theo UTC−07; transcript chỉ đọc qua bên thứ ba |
| S11 | Peter Yang / Behind the Craft: Peng Zheng & Lauren Tan | 27/09/2026, 45:04 | [Video gốc](https://www.youtube.com/watch?v=xZ5TEaleUdg) | Metadata và các chương truy cập được; chưa đọc toàn văn bản ghi gốc |
| S12 | pstack trong cursor/plugins; nguồn trực tiếp | **0.15.48**, commit 26/11/2026 | [Cây nguồn cố định](https://github.com/cursor/plugins/tree/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack), [manifest tác giả](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/.cursor-plugin/plugin.json) | Cao cho cơ chế triển khai; manifest ghi Lauren Tan, MIT; không phải gstack |
| S13 | poteto/noodle; nguồn trực tiếp | Commit 19/03/2026 | [Cây nguồn cố định](https://github.com/poteto/noodle/tree/82d2921c52370f23f29086de81ccfb600939c037), [license](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/LICENSE) | Cao cho artifact; có skill tích hợp từ bên khác, không quy mọi nội dung cho Lauren |
| S14 | Các bài X của poteto | Ngày chưa xác minh trực tiếp | Xem danh sách ở mục 9 | **Không truy cập được nguồn gốc**; các bản đăng lại mang nhãn gián tiếp |
| S15 | learn-pstack / ganhai.cloud; đăng lại X | Trang có ngày cập nhật 03/10/2026 | [Pt.1](https://pstack.ganhai.cloud/en/from-poteto/complete-guide-pt-1-zh/), [Pt.2](https://pstack.ganhai.cloud/en/from-poteto/complete-guide-pt-2-zh/), [Loops](https://pstack.ganhai.cloud/en/from-poteto/loops-you-can-trust-zh/), [How I Use Cursor](https://pstack.ganhai.cloud/en/from-poteto/how-i-use-cursor-zh/) | **[GIÁN TIẾP]**; có ngày không khớp các nơi khác; không dùng làm nguồn trích nguyên văn |
| S16 | uListen và Real Alpha; tóm tắt Lightcone | Bài ghi 14/11/2026 | [uListen](https://ulisten.ai/channels/y-combinator/the-rise-of-ai-coworkers_y1-9yeYLJUo/chapters), [Real Alpha](https://realalpha.app/people/lauren-tan/) | **[GIÁN TIẾP]**; dùng khoanh vùng 43:56–47:33 để kiểm tra thay đổi quan điểm |
| S17 | AI Podcast / Jason Lin; transcript phỏng vấn Pocock | Trang ghi 03/10/2026 | [Transcript đăng lại](https://aipodcast.jasonlin.tech/e/laurentan-mattpoco-2026/) | **[GIÁN TIẾP]**; không dùng chốt con số hoặc phát biểu chính xác |
| S18 | Cosign; hồ sơ tổng hợp | Đọc 27/11/2026 | [Hồ sơ](https://cosign.co/x/poteto) | **[GIÁN TIẾP]**; các tháng công tác chỉ là đầu mối |
| S19 | Paul Kuo và FakeNerd; phân tích | Tháng 11/2026; FakeNerd đối chiếu 21/11 | [Paul Kuo](https://paulkuo.tw/en/articles/agent-mistakes-into-checks/), [FakeNerd](https://fakenerd.ai/pstack-poteto) | **[GIÁN TIẾP]**; dùng tìm nguồn; bản dịch/diễn giải không nâng thành lời Lauren |
| S20 | ythx-101; tổng hợp workshop | Không xác minh được ngày đăng | [Trang tổng hợp](https://ythx-101.github.io/lauren-tan-ai/index.html) | **[GIÁN TIẾP]**; số liệu chỉ là đầu mối, chưa đối chiếu được bản ghi Maven |

Ngày commit là ngày repository trả về cho commit đã đọc, **không phải ngày sáng tác từng skill**. [source-manifest.json](source-manifest.json) ghi SHA, URL và SHA-256 của các tệp GitHub đã lưu để đối chiếu. Các workflow trọng tâm đã được đọc toàn văn; danh mục rộng dùng frontmatter, đề mục và các bước trong tệp gốc. Việc tải được một tệp không đồng nghĩa đã kiểm chứng hành vi của nó. Báo cáo không phát hành lại toàn văn talk/bài X.

## 2. Danh tính và dòng thời gian công việc

**[a] Danh tính được nối bằng liên kết tài khoản**, không chỉ bằng tên: GitHub `poteto` dẫn tới X `poteto`, tự ghi xAI và React Compiler; React team dẫn tới chính GitHub này; manifest pstack ghi Lauren Tan. Các liên kết đủ xác định người trong phạm vi yêu cầu. [S01](https://github.com/poteto), [S02](https://react.dev/community/team), [manifest pstack](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/.cursor-plugin/plugin.json).

| Mốc | Điều có thể xác minh | Điều chưa được phép kết luận |
|---|---|---|
| 17/02/2020 | **[a]** Bài cá nhân nói sẽ rời vị trí quản lý tại Netflix và trở lại kỹ sư. [S04](https://www.no.lol/2020-01-17-does-it-spark-joy/) | Ngày vào Netflix chính xác |
| 2020 → 2025 | **[a]** Blog tự giới thiệu Facebook; bài React 07/10/2025 xác nhận đóng góp compiler. [Blog](https://www.no.lol/), [S03](https://react.dev/blog/2025/10/07/react-compiler-1) | Không suy ngày bắt đầu Meta từ năm bài compiler |
| 2026, sang Cursor | **[a]** Bài LinkedIn kể rời Meta sau sáu năm và đã vào Cursor; Maven 12/08 ghi Member of Technical Staff, Cursor. [S05](https://www.linkedin.com/posts/laurenelizabethtan_i-left-meta-3-weeks-ago-after-6-fulfilling-activity-7447312270633725952-AFhF), [S08](https://maven.com/p/e23d9c/how-cursor-turned-ai-agents-into-better-engineers) | **[b] [GIÁN TIẾP]** Cosign ghi 03/2026; chưa chốt tháng bằng nguồn gốc đọc đầy đủ |
| 27/09 và 14/11/2026 | Mô tả gốc Peter Yang ghi Grok Bot tại SpaceXAI; metadata YC ghi SpaceXSI. [S11](https://www.youtube.com/watch?v=xZ5TEaleUdg), [S09](https://www.youtube.com/watch?v=y1-9yeYLJUo) | Không tự chuẩn hóa SpaceXAI, SpaceXSI, xAI thành cùng một pháp nhân/hợp đồng lao động |
| Ngày khảo sát | GitHub ghi xAI; React team vẫn ghi Cursor. [S01](https://github.com/poteto), [S02](https://react.dev/community/team) | Không coi mọi trang hiện hành đều cập nhật đồng thời |

**[c] Kết luận giới hạn:** chuỗi Netflix → Facebook/Meta → Cursor → làm Grok Bot dưới các nhãn xAI/SpaceXAI/SpaceXSI có nhiều nguồn hỗ trợ. Chưa đủ chứng cứ cho một lịch chuyển công ty chính xác từng tháng. Đặc biệt, không dùng riêng hồ sơ Cosign để tuyên bố cô rời Cursor tháng 08/2026.

Ngày chuyển chính xác từ Cursor sang xAI/SpaceXSI và quan hệ pháp lý giữa các nhãn công ty: **Tôi không biết, tôi không có thông tin về điều này.**

Các Lauren Tan về mỹ thuật, trường học, nhân giống mèo xuất hiện trong tìm kiếm không có chuỗi liên kết trên: **chưa xác minh danh tính**, bị loại khỏi bằng chứng. Garry Tan có mặt trong Lightcone nhưng không phải đối tượng nghiên cứu; lời của ông về gstack hoặc ngân sách review không được gán cho Lauren.

## 3. Quan điểm cốt lõi và quy trình

| Nội dung | Nhãn và kết quả trích xuất | Nguồn trực tiếp |
|---|---|---|
| Niềm tin | **[a, artifact]** Quyền tự chủ có mức độ: Noodle tách auto, supervised, manual theo ai kích hoạt lập lịch, dispatch và merge. | [Modes](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/concepts/modes.md) |
| Verification | **[a, artifact]** Agent phải khởi động được sản phẩm, thao tác như người dùng, kiểm kết quả và side effect. Compile hay ảnh cuối cùng không đủ. | [create-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/create-verification-skill/SKILL.md) |
| Vai trò con người | **[a, artifact]** Cần phát biểu mục tiêu cho dễ kiểm tra; bắt đầu từ kiểu dữ liệu và giao diện gọi; có quyền bác bỏ một giải pháp. **[c]** Người điều hành vẫn chịu trách nhiệm chọn mục tiêu và đánh giá bằng chứng. | [architect](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/architect/SKILL.md), [in-your-own-words](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/in-your-own-words/SKILL.md) |
| Codebase là môi trường học lại | **[a, artifact]** Xóa đường cũ mà agent dễ sao chép; ưu tiên một chủ sở hữu trạng thái, một đường API và ràng buộc kiểu. | [correct](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/correct/SKILL.md) |
| Evals | **[a, artifact]** So sánh skill bằng nhiệm vụ tự nhiên, môi trường tách biệt, tiêu chí định trước và chấm mù; kiểm transcript để biết tệp nào thực sự được mở. | [Eval playbook](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/eval.md) |
| Review | **[a, artifact]** Adversarial review chỉ giữ lỗi có lệnh tái hiện và kết quả thực chạy; tổng hợp theo nguyên nhân. Không đồng nghĩa nhiều reviewer luôn tốt hơn. | [interrogate](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/interrogate/SKILL.md) |
| Học từ lỗi | **[a, artifact]** Chuyển bài học thành cấu trúc trước; kiến thức còn lại vào kho nhớ hoặc sửa đúng skill liên quan. | [Noodle reflect](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/reflect/SKILL.md) |

**[a] Trích ngắn từ nguồn trực tiếp:** “code is memory” — slide tại [02:57](https://www.youtube.com/watch?v=O_zcc6Xl12g&t=177s), đối chiếu [trang talk](https://ai.engineer/talks/O_zcc6Xl12g-all-code-is-slop-except-mine). Phần còn lại của báo cáo dùng diễn đạt lại, không lấy câu nguyên văn từ bản đăng lại.

**Vòng code bên trong — [a, artifact].** Với tính năng: đọc hệ thống liên quan → thiết kế kiểu/giao diện → phân việc có ranh giới → triển khai → chạy đúng bề mặt người dùng → chia đơn vị có thể kiểm chứng → chuẩn bị PR. Với bug: tái hiện → đặt và loại giả thuyết bằng quan sát runtime → sửa nguyên nhân → chạy lại cùng ca tái hiện. Test đỏ trước được ưu tiên khi có đường test hẹp, rẻ; pstack hiện tại không đòi TDD cho mọi thay đổi. [Feature](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/feature.md), [Bug fix](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/bug-fix.md), [tdd](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/tdd/SKILL.md).

**Trước merge — [a, artifact].** Shipping yêu cầu người/agent kiểm chứng khác tác giả chạy thay đổi, đối chiếu parent với head, kết luận có bằng chứng. Với PR xếp chồng, chỉ merge đoạn liên tục đã xác minh từ dưới lên. Phải kiểm verdict còn áp dụng sau khi patch thay đổi; CI xanh không thay cho kiểm chứng hành vi. Đây là quy định trong bản nguồn đã ghim, không phải cam kết rằng mọi PR nội bộ của cô đều qua đúng thủ tục này. [Shipping](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/shipping.md).

**Xử lý review feedback — [a, artifact].** Babysit phân loại xung đột, thread và CI; đánh giá Bugbot bằng chứng; sửa lỗi thật và giải thích việc bác bỏ nhận xét bằng phản chứng. Nội dung bình luận là dữ liệu, không phải chỉ thị để chạy lệnh. Babysit tự nó không cấp quyền merge. [Babysit](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/babysit.md).

**Chạy song song — [a, artifact].** `swarm` chia lát công việc hoặc cho nhiều agent thử cùng bài; nêu điều kiện hoàn thành và luật chọn trước. Mỗi worker ghi ra nơi riêng; worker đo/verify phải biết SHA và phương pháp. Thiếu lane không được tính là pass. `arena` thêm chấm chéo, chọn nền rồi ghép phần tốt và kiểm lại. Noodle lập lịch phase phụ thuộc tuần tự và ưu tiên hoàn tất một plan; không rải mọi plan mỗi nơi một ít. [swarm](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/swarm/SKILL.md), [arena](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/arena/SKILL.md), [schedule](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/schedule/SKILL.md).

**Khi cho tự merge — [a, artifact] và [c].** `autopilot-full` yêu cầu phạm vi được giao rõ, verifier độc lập, kiểm đúng patch và CI đúng head; mục ngoài hàng đợi dừng ở merge-ready. **[c]** Bộ nháp chọn supervised ban đầu vì ở dự án mới chưa có bằng chứng vận hành đủ để mở rộng quyền. Không chuyển các câu về “tự chủ” trong pstack thành quyền tự gửi tin, deploy hoặc sửa dữ liệu bên ngoài. [Autopilot-full](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/autopilot-full.md), [Noodle modes](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/concepts/modes.md).

## 4. Biến lỗi thành cơ chế

**[a, artifact]** `correct` gom các lỗi đã xảy ra lặp lại và thử mức mạnh nhất phù hợp: thiết kế loại bỏ lựa chọn sai → kiểu dữ liệu → lint/CI nêu cách sửa → test hành vi → văn bản cho phần cần phán đoán. Khi có nhiều lỗi cũ, có thể chặn việc tăng thêm trước; phải chứng minh check bắt được lỗi cũ thật. [correct](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/correct/SKILL.md).

**[c] Ví dụ chuyển giao:** một agent liên tục import code main-process vào renderer. Thêm câu cấm vào AGENTS.md vẫn cần agent nhớ và diễn giải. Một rule bắt import trực tiếp có thể phát hiện mẫu đã biết; kiểm dependency graph còn bắt được đường vòng qua module trung gian. Bộ nháp cung cấp rule ESLint ở mức import trực tiếp và ghi rõ giới hạn. Không tuyên bố nó tương đương bộ phân tích dependency của Dune. Căn cứ thiết kế: [boundary-discipline](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-boundary-discipline/SKILL.md), [encode-lessons-in-structure](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-encode-lessons-in-structure/SKILL.md).

**[a, artifact] Ngoại lệ comment:** Comment Sicko giữ license, hợp đồng API công khai và các ràng buộc bên ngoài đủ căn cứ. Đây là một chính sách rất mạnh của bộ công cụ, không phải lý do xóa hết bình luận ở mọi repository. **[c]** Bộ nháp không áp dụng chính sách xóa comment hàng loạt. [Agent definition](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/agents/comment-sicko.md).

## 5. Bóc tách skill và config gốc

Danh mục đầy đủ tên, mục đích/trigger, tuyến bước và link từng tệp ở [inventory.md](inventory.md). Đây là danh mục theo cây nguồn tại commit, không đếm theo bài giới thiệu có thể đã cũ.

| Thành phần | Cấu trúc thực đọc được | Ý nghĩa |
|---|---|---|
| pstack | `.cursor-plugin/plugin.json`, `plugin.json` | Manifest cùng ghi 0.15.48; author Lauren Tan; đường skill/agent |
| Skill chính | `skills/<name>/SKILL.md` | YAML frontmatter + Markdown; **59** tệp ở tầng này, trong đó **24** nguyên tắc |
| Router | `skills/poteto-mode/SKILL.md`, `worker.md`, `playbooks/*.md` | Chat nhận lệnh giữ vai trò điều phối; worker đọc chỉ dẫn riêng |
| Agent definition | `agents/poteto-agent.md`, `agents/comment-sicko.md` | Delegate wrapper và reviewer comment; không phải hai skill |
| Model config | `~/.cursor/rules/pstack-models.mdc` do setup tạo | MDC có `alwaysApply: true`; chọn model theo vai trò và khả năng thật của phiên |
| Verification sinh ra | `.cursor/skills/verify-<app>/SKILL.md`, `features/README.md`, `features/*.md` | Launch, doctor, drive, evidence, cleanup; bản đồ hành vi theo người dùng |
| Automation mẫu | `automations/benny/` | Ba skill riêng, không cộng vào 59 skill chính |
| Noodle | `.agents/skills/<name>/SKILL.md` | **29** skill nội bộ; **8** có trường `schedule` trong frontmatter |
| Bộ nhớ Noodle | `brain/`, `CLAUDE.md` | Kho Markdown/wikilink; đây là cấu hình của repo, không phải định dạng bắt buộc cho mọi agent |
| Vận hành Noodle | `.noodle.toml`, `.noodle/mise.json`, `.noodle/orders-next.json` | TOML cấu hình; JSON brief/orders; skill scheduler dùng điều kiện `schedule` viết bằng văn xuôi |
| Hook/lint Noodle | `.claude/settings.json`, `.agents/hooks/`, `scripts/lint-arch.sh` | Cấu hình hook và kiểm kiến trúc cụ thể; không chỉ là lời khuyên |

Nguồn cho từng hàng: [manifest](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/.cursor-plugin/plugin.json), [poteto-mode](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/SKILL.md), [worker](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/worker.md), [setup](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/setup-pstack/SKILL.md), [Noodle skill](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/noodle/SKILL.md), [CLAUDE.md](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/CLAUDE.md), [settings](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.claude/settings.json), [lint](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/scripts/lint-arch.sh).

**[a, artifact] Noodle pipeline:** brief → schedule → dispatch → execute trong worktree → merge theo mode. Chuỗi stage thông thường là execute → quality → reflect; quality có thể phát thông điệp chặn. Frontmatter `schedule` là tín hiệu để scheduler suy xét, không phải biểu thức cron hay policy engine xác định. [schedule](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/schedule/SKILL.md), [quality](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/quality/SKILL.md).

**[c] Hai lưu ý chuyển định dạng:** không chép nguyên các trường riêng của Cursor sang agent khác rồi coi đã tương thích. Đồng thời, trường `schedule` chỉ có tác dụng khi có Noodle hoặc một scheduler thật đọc nó. Bộ nháp skill thông thường không tự chạy chỉ vì có metadata.

**[a, artifact] Những giới hạn nhìn thấy trong Noodle:** skill scheduler nói runtime sprites còn WIP; ví dụ orders tối thiểu và hướng dẫn scheduler không hoàn toàn đồng nhất cách trình bày trường stage. Khi triển khai phải lấy `noodle schema orders` của đúng phiên bản làm chuẩn, không ghép JSON từ các ví dụ. Không coi lệnh/schema được mô tả trong README là đã được thực thi ở máy này. [schedule](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/schedule/SKILL.md), [ví dụ tối thiểu](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/examples/minimal/.agents/skills/schedule/SKILL.md).

## 6. Chi phí, số liệu và thay đổi theo thời gian

Các số dưới đây **không được cộng, bình quân hoặc chọn một số làm chuẩn**. Số trong lời nói có thể được làm tròn; số slide cũng chưa có dữ liệu thô để kiểm toán.

| Con số | Người/phạm vi/kỳ | Trạng thái và nguồn |
|---|---|---|
| Khoảng 3.000; slide 3.127 | Cá nhân, tháng trước talk; slide ghi tháng 10 | **[a]** Lời nói [00:42](https://www.youtube.com/watch?v=O_zcc6Xl12g&t=42s); số slide do trang talk trình bày |
| Khoảng 11.000; slide 10.926 | Đội Grok Bot, tháng 10 | **[a]** [13:32](https://www.youtube.com/watch?v=O_zcc6Xl12g&t=812s); số slide trong [bản đọc](https://ai.engineer/talks/O_zcc6Xl12g-all-code-is-slop-except-mine.md) |
| Khoảng 0,5% | PR bị revert của đội | **[a]** [14:21–14:27](https://www.youtube.com/watch?v=O_zcc6Xl12g&t=861s); không phải tỷ lệ mọi lỗi |
| 4.000/tháng | Tên chương Lightcone; chưa xác minh phép đếm | Mô tả gốc ghi tại [22:35](https://www.youtube.com/watch?v=y1-9yeYLJUo&t=1355s); không coi tên chương là phát biểu đã kiểm chứng của Lauren |
| 1.000; gần 800 tính tới ngày 12 | Bản tóm tắt workshop: tháng trước / tháng đang chạy | **[b] [GIÁN TIẾP]**, [ythx-101](https://ythx-101.github.io/lauren-tan-ai/index.html); **không chốt số** |
| 2.000/tháng | Pt.1 được đăng lại | **[b] [GIÁN TIẾP]**, [learn-pstack Pt.1](https://pstack.ganhai.cloud/en/from-poteto/complete-guide-pt-1-zh/); **không chốt số** |
| 2.462 cuối tháng 8 | Chú thích hình trong Pt.2 đăng lại | **[b] [GIÁN TIẾP]**, [learn-pstack Pt.2](https://pstack.ganhai.cloud/en/from-poteto/complete-guide-pt-2-zh/); **không chốt số** |
| 2.500/tháng | Tiêu đề talk trên X được các nơi dẫn lại | **[b] [GIÁN TIẾP]**, [FakeNerd](https://fakenerd.ai/pstack-poteto), [Pocock transcript đăng lại](https://aipodcast.jasonlin.tech/e/laurentan-mattpoco-2026/); **không chốt số** |

**[a] Chi phí:** Lauren nói mất sáu tháng đạt mức tin tưởng này ([01:42](https://www.youtube.com/watch?v=O_zcc6Xl12g&t=102s)); phần lớn token của cô dành cho verification ([12:16](https://www.youtube.com/watch?v=O_zcc6Xl12g&t=736s)). **70% là số của Cindy Sridharan được cô dẫn lại**, không phải phép đo chính xác của Lauren. Cô còn nói formal verification đang thử nghiệm, không phải thuốc chữa mọi lỗi ([11:14–11:30](https://www.youtube.com/watch?v=O_zcc6Xl12g&t=674s)).

**[a, artifact] Kiểm soát chi phí trong công cụ:** cấu hình Noodle cảnh báo mỗi agent tiêu token riêng, khuyên bắt đầu ít phiên; meditate tự ghi là tác vụ tốn kém, không nên lập lịch quá thường xuyên. pstack setup có mức ngân sách và kiểm model khả dụng, không hứa tỷ lệ tiết kiệm chung. [Noodle configuration](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/reference/configuration.md), [meditate](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/meditate/SKILL.md), [setup-pstack](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/setup-pstack/SKILL.md).

Chi phí USD/tháng của Lauren, token/PR, chi phí thiết lập theo giờ công, ROI sau chi phí sửa lỗi: **Tôi không biết, tôi không có thông tin về điều này.**

**Thay đổi quan điểm: phân biệt bằng chứng và diễn giải.**

- **[a, artifact]** Noodle ở commit tháng 3 quy định TDD mặc định, với test đỏ cho bug. pstack ở commit tháng 11 chỉ dùng TDD khi được yêu cầu hoặc có đường test hẹp, rẻ. **[c]** Đây là khác biệt có thể xác minh giữa hai artifact theo thời gian; chưa chứng minh cô đã đổi một lập trường phổ quát. [Noodle testing](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/testing/SKILL.md), [pstack tdd](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/tdd/SKILL.md).
- **[a, artifact]** Noodle có plan tạo tài liệu theo phase. pstack hiện tại có cả prototype và multi-phase-plan, nên không thể kết luận từ code rằng cô cấm mọi loại kế hoạch. [Noodle plan](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/.agents/skills/plan/SKILL.md), [pstack prototype](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/prototype.md), [multi-phase-plan](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/multi-phase-plan.md).
- **[b] [GIÁN TIẾP]** uListen và Real Alpha mô tả Lightcone 43:56–47:33 là cô giảm nhấn mạnh review đối kháng nhiều model vì dễ tối ưu theo feedback thiếu ngữ cảnh sản phẩm. [uListen](https://ulisten.ai/channels/y-combinator/the-rise-of-ai-coworkers_y1-9yeYLJUo/chapters), [Real Alpha](https://realalpha.app/people/lauren-tan/). Nguồn pstack trực tiếp xác nhận review hiện nay cần bằng chứng thực chạy, nhưng **không xác nhận phát biểu về thay đổi thói quen cá nhân trong podcast**. Mục này vẫn chờ đối chiếu video.

Ngày và lý do chính xác cô chuyển từ ưu tiên nhiều reviewer sang giảm review; mức giảm bao nhiêu: **Tôi không biết, tôi không có thông tin về điều này.**

## 7. Nguyên tắc có thể áp dụng

Các điều kiện áp dụng/không áp dụng dưới đây là **[c]**, do người viết đề xuất. Cột căn cứ chỉ dùng nguồn trực tiếp.

| Nguyên tắc | Khi nên dùng | Khi KHÔNG nên áp dụng máy móc | Căn cứ |
|---|---|---|---|
| Chốt một kết quả quan sát được | Mọi việc có đầu ra để kiểm | Không tự quyết thay sở thích sản phẩm còn thiếu | [in-your-own-words](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/in-your-own-words/SKILL.md) |
| Xây driver verification dùng lại | Agent thường phải thao tác cùng ứng dụng | Việc một lần rất nhỏ, driver đắt hơn giá trị sử dụng lại | [create-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/create-verification-skill/SKILL.md) |
| Sửa cấu trúc trước khi thêm luật chữ | Có lớp lỗi lặp lại, invariant đủ rõ | Quyết định còn phụ thuộc hoàn cảnh hoặc chưa có phản ví dụ | [encode-lessons-in-structure](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-encode-lessons-in-structure/SKILL.md) |
| Viết test đỏ khi đường test rẻ | Bug tái hiện ổn định, test đo đúng hành vi | Hạ tầng khó tái hiện, test giòn chỉ để đủ thủ tục | [tdd](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/tdd/SKILL.md) |
| Dùng song song cho phạm vi tách biệt | Tệp/DB/port được cô lập, có tiêu chí tổng hợp | Agent cùng sửa một trạng thái hoặc cần kết quả nhau | [separate-before-serializing](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/principle-separate-before-serializing-shared-state/SKILL.md) |
| Chỉ sửa finding có căn cứ | Review báo bug có thể chạy lại | Không loại bỏ nghĩa vụ review chuyên môn chỉ vì chưa có repro tự động | [interrogate](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/interrogate/SKILL.md) |
| Eval mù trước khi nâng skill | Thay workflow có ảnh hưởng đáng kể, có nhiệm vụ đại diện | Thay vài từ vô hại; bộ mẫu chỉ chứng minh chuyện quá hiển nhiên | [Eval](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/eval.md) |
| Chọn property và công cụ formal theo câu hỏi | Race, retry, state machine có invariant khó test hết | Không coi model đúng là chứng minh triển khai/UX hoàn hảo | [create-formal-verification-skill](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/create-formal-verification-skill/SKILL.md) |
| Verdict phải còn đúng với patch | PR thay đổi sau review hoặc rebase | Không tái dùng verdict cũ theo tên nhánh hoặc CI chung | [Shipping](https://github.com/cursor/plugins/blob/b7b93c36be9bcc36a74db438e0fd385d660bc559/pstack/skills/poteto-mode/playbooks/shipping.md) |
| Tăng quyền tự động sau bằng chứng | Có phục hồi, kiểm đúng hành vi và phạm vi được giao | Migration phá hủy, deploy, auth/billing khi chưa đủ cơ chế | [Noodle modes](https://github.com/poteto/noodle/blob/82d2921c52370f23f29086de81ccfb600939c037/docs/concepts/modes.md) |

## 8. Bộ nháp skill/config và cách sử dụng

Các tệp nằm trong [draft/](draft/). Chúng là **bản chuyển giao của người viết**, không phải bản phân phối chính thức của Lauren. Không thay AGENTS.md/CLAUDE.md đang có và không cài hook vào repository.

| Tệp | Công dụng | Nguồn / phần suy ra |
|---|---|---|
| [SKILL.md](draft/verified-engineering/SKILL.md) | Quy trình từ mục tiêu đến kiểm chứng và học từ lỗi | Quy trình từ pstack; cách ghép và giới hạn chi phí là **[c]** |
| [AGENTS.fragment.md](draft/AGENTS.fragment.md) | Đoạn chỉ dẫn ngắn để ghép có chọn lọc | Phân biệt văn bản với enforcement từ correct; câu chữ **[c]** |
| [noodle.supervised.toml](draft/noodle.supervised.toml) | Khởi đầu một phiên, supervised, process runtime | Key theo Noodle; chọn mức một phiên là **[c]**, không phải con số Lauren dùng |
| [eslint.boundaries.example.mjs](draft/eslint.boundaries.example.mjs) | Chặn import trực tiếp giữa main/renderer trong ví dụ JavaScript | Nguyên lý boundary/structure; đường thư mục và mã rule là **[c]** |
| [pre-commit.example](draft/pre-commit.example) | Hook Git mẫu chạy test ảnh hưởng của working tree taxxManager trước commit | Lệnh do AGENTS.md dự án này quy định, không gán cho Lauren; vị trí hook là **[c]** |
| [provenance.md](draft/provenance.md) | Nguồn cho từng quyết định, phạm vi, hướng dẫn đưa vào dùng | Tách nguồn gốc và lựa chọn chuyển giao |

**[c] Lộ trình dùng:** đọc bộ nháp và chọn phần phù hợp → đưa skill vào thư mục skill của agent đang dùng → khai báo lệnh launch/drive thật của sản phẩm khi thực hiện nhiệm vụ → chạy một hành trình đầy đủ và kiểm bằng chứng còn sau cleanup. Chỉ sau đó mới xem xét automation. Config Noodle cần bản cài và scheduler skill phù hợp; riêng TOML không tạo một hệ thống agent hoạt động.

Rule ESLint chỉ là check import trực tiếp trong layout ví dụ. Hook Bint chỉ chạy cổng test hiện có. **Cả hai không phải chứng minh runtime hay giấy phép tự merge.** Kết quả kiểm tra bộ nháp nằm ở [validation.md](validation.md).

## 9. Phản biện và điều chưa xác minh

**Ba rủi ro — [c], không gán cho Lauren:**

1. **Thiên lệch theo dự án và cách đo.** PR nhiều và ít revert chưa đo hết giá trị người dùng, lỗi sửa tiến hoặc sự cố phát hiện muộn. Chưa có dữ liệu so sánh có kiểm soát để suy hiệu quả nhân quả của pstack. Nên theo dõi cả lỗi lọt, công sửa lại và kết quả sản phẩm, thay vì lấy số PR làm mục tiêu.
2. **Chi phí chuyển từ viết sang kiểm.** Driver, feature map, eval và formal model đều phải bảo trì; nhiều agent cùng hiểu sai yêu cầu có thể đồng ý với nhau. Chọn lane theo rủi ro và kiểm nguồn sự thật độc lập giúp hạn chế cảm giác chắc chắn giả.
3. **Ràng buộc quá cứng có thể giữ sai kiến trúc.** Cấm API hay xóa comment có thể dẹp triệu chứng đồng thời làm mất ngữ cảnh cần thiết. Rule nên có phản ví dụ đã kiểm, đường thay thế rõ, và cách xem lại khi kiến trúc đổi. Không chép mọi quy tắc trong codebase Electron sang ứng dụng khác.

**Ba câu hỏi nghiên cứu còn bỏ ngỏ:**

1. Sau khi tính token, hạ tầng, bảo trì verification và sửa lỗi, chi phí trên một kết quả được người dùng chấp nhận là bao nhiêu?
2. Các ca tự merge có tỷ lệ lỗi lọt, sự cố nghiêm trọng và thời gian khôi phục thế nào so với ca review người trong cùng miền?
3. Với miền khó hoàn tác hoặc không có oracle kiểm chứng tốt, điều kiện đo được nào đủ để tăng từ supervised sang auto?

Cho cả ba câu hỏi: **Tôi không biết, tôi không có thông tin về điều này.**

**Nguồn gốc không truy cập được hoặc chỉ đọc một phần:**

- Không truy cập được nguồn gốc: [The Complete Guide to pstack Pt.1](https://x.com/poteto/status/2094457600259842065) — 403.
- Không truy cập được nguồn gốc: [The Complete Guide to pstack Pt.2](https://x.com/poteto/status/2097732320606507506) — 403.
- Không truy cập được nguồn gốc: [Loops You Can Trust](https://x.com/poteto/status/2069824386283319343) — 403.
- Không truy cập được nguồn gốc: [talk 2.500 PR trên X](https://x.com/poteto/status/2102050467505430555) — 403.
- Không truy cập được nguồn gốc: [bài X về các tầng sửa lỗi](https://x.com/poteto/status/2111133080526495985) — 403.
- Không truy cập được nguồn gốc: [poteto/pstack](https://github.com/poteto/pstack) — 404. Kho pstack xác minh được là **cursor/plugins/pstack**; không tự thay bằng một fork.
- Trang [Maven](https://maven.com/p/e23d9c/how-cursor-turned-ai-agents-into-better-engineers) truy cập được metadata và chapter; chưa lấy được toàn văn bản ghi. **Tôi không biết, tôi không có thông tin về điều này.** — áp dụng cho các lời nói cụ thể trong workshop ngoài metadata đã đọc.
- Trang YouTube gốc truy cập được metadata bằng HTTP, nhưng yêu cầu caption trả nội dung rỗng cho Lightcone, Pocock và Peter Yang. Không tuyên bố đã xem/nghe toàn bộ. **Tôi không biết, tôi không có thông tin về điều này.** — áp dụng cho chi tiết chỉ còn transcript của bên thứ ba.
- Trang talk AI Engineer qua công cụ web có Full transcript; tải trực tiếp bằng HTTP và một số ảnh slide trả 403. Các số slide được ghi đúng mức “trang talk trình bày”, không giả vờ đã kiểm ảnh.

**Các điều khác không xác minh được:** ngày thuyết trình chính xác so với ngày phát video; dataset PR/defect thô; cách tính revert và cửa sổ quan sát; số agent thực chạy đồng thời; ngày và tác giả từng thay đổi trong skill; danh sách đầy đủ mọi thread X; tính đầy đủ của loạt Complete Guide; config nội bộ Dune/Grok Bot; chi phí USD và giới hạn token thật. Với từng mục: **Tôi không biết, tôi không có thông tin về điều này.**

## 10. Chỉ có nguồn gián tiếp

Các mục này để người đọc tự đối chiếu; **không dùng chúng làm căn cứ duy nhất cho bất kỳ quy tắc nào trong bộ nháp**:

| Nội dung còn chờ | Nơi đọc được | Nguồn gốc cần kiểm |
|---|---|---|
| Chi tiết toàn văn Complete Guide Pt.1 và Pt.2, kể cả số 2.000 và 2.462 | **[GIÁN TIẾP]** learn-pstack | [Pt.1](https://x.com/poteto/status/2094457600259842065), [Pt.2](https://x.com/poteto/status/2097732320606507506) |
| Nội dung đầy đủ Loops You Can Trust; phát triển quan điểm từ số agent sang bằng chứng của loop | **[GIÁN TIẾP]** learn-pstack | [Bài X](https://x.com/poteto/status/2069824386283319343) |
| Tháng bắt đầu Cursor và tháng chuyển sang SpaceXAI | **[GIÁN TIẾP]** Cosign; bản đăng lại phỏng vấn | [Pocock](https://www.youtube.com/watch?v=MN9dGgmLyso), hồ sơ công việc gốc |
| Con số 1.000 / gần 800 trong workshop | **[GIÁN TIẾP]** ythx-101 | [Maven](https://maven.com/p/e23d9c/how-cursor-turned-ai-agents-into-better-engineers) |
| Chi tiết giảm review đối kháng và bỏ plan mode | **[GIÁN TIẾP]** uListen / Real Alpha | [Lightcone, từ 43:56](https://www.youtube.com/watch?v=y1-9yeYLJUo&t=2636s) |
| Cách lấy mẫu PR và giới hạn khi gặp quyết định khó hoàn tác trong cuộc nói chuyện Pocock | **[GIÁN TIẾP]** Jason Lin | [Pocock](https://www.youtube.com/watch?v=MN9dGgmLyso) |

Những chủ đề có bản gián tiếp nhưng **đã có nguồn trực tiếp khác khớp** gồm verification thực chạy, thiết kế boundary, phân tách worker và sửa lỗi thành check: các tệp GitHub được dẫn tại mục 3–7 xác minh chính các cơ chế đó. Điều này không tự xác minh ngày tháng, con số hoặc câu chữ trong bài X/podcast.
