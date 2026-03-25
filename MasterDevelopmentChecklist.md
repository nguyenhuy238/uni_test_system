# UniTestSystem - Danh Sách Kiểm Tra Phát Triển Toàn Diện (Master Development Checklist)

Tài liệu này đóng vai trò là danh sách kiểm tra chủ chốt cho toàn bộ quá trình phát triển hệ thống quản lý thi Đại học (UniTestSystem). Được thiết kế theo chuẩn Enterprise, bao gồm mọi khía cạnh từ chức năng, phi chức năng đến quy trình triển khai và sẵn sàng vận hành.

## Cập nhật gần nhất (2026-03-24)
- Hoàn thiện cụm **J. WPF AdminApp**: bổ sung chế độ Admin/Staff theo role-based UI, tab Audit Log, cấu hình hệ thống qua API, backup/restore DB (SQL `.bak`), lập lịch thi đơn lẻ & đồng loạt (bulk queue + export CSV), và export báo cáo định kỳ.
- Nâng cấp bảo mật phiên AdminApp: lưu token cục bộ bằng DPAPI (Windows), hỗ trợ refresh token rotation và tự làm mới access token khi gần hết hạn/401.
- Nâng cấp UI/UX AdminApp: thêm Dark/Light mode và hiệu ứng chuyển tab (fade) mượt.
- Hoàn thiện thêm kiểm tra lịch thi: xung đột phòng/sinh viên/giảng viên, kiểm tra quá tải theo `ExamScheduling:RoomCapacities`, và export CSV lịch thi.
- Bổ sung cơ chế token truy cập bài thi cho luồng vào bài từ lịch thi sinh viên (`/mytests/start` với `scheduleId + accessToken`).
- Bổ sung guard chống đa thiết bị cho phiên thi đang làm (`DeviceFingerprint`) trên luồng start/resume/save/submit.
- Sửa tổng hợp báo cáo khoa/năm học để trả đúng `StudentCount` và `PassRatePercent`.
- Bổ sung khôi phục phiên bản câu hỏi từ Audit Log (UI + service).
- Bổ sung kiểm tra trùng lặp nâng cao bằng độ tương đồng nội dung (Jaccard).
- Bổ sung quản lý bảng điểm có lọc theo khoa/lớp/kỳ trên web và export transcript Excel/PDF (QuestPDF).
- Bổ sung chấm điểm từng phần cho câu hỏi Matching/DragDrop trong auto-grading.
- Bổ sung luồng phúc khảo (regrade request) cho sinh viên và moderation xử lý bởi giảng viên/staff.
- Bổ sung khóa/mở khóa điểm (grade locking) trên phiên chấm.
- Bổ sung vô hiệu hóa phiên đăng nhập ngay khi vai trò người dùng thay đổi (cookie/JWT validation + revoke session/refresh token).
- Bổ sung công thức tính điểm trọng số (điểm quá trình + điểm thi, tùy chỉnh trọng số) trong luồng finalize bảng điểm.
- Bổ sung khóa/mở bảng điểm cấp khoa/trường và chặn finalize khi đang bị khóa.
- Hoàn thiện Widget Dashboard trên Web Reports: tỷ lệ đạt/trượt theo môn, điểm trung bình theo kỳ, và biểu đồ phổ điểm.
- Hoàn thiện Phân tích Câu hỏi trên Web Reports: độ khó thực tế (difficulty index) và độ phân biệt (discrimination index) theo dữ liệu làm bài thật.
- Hoàn thiện Báo cáo giảng dạy (Lecturer performance) trên Web Reports với thống kê theo giảng viên (số môn/số đề/số lượt nộp/điểm TB/tỷ lệ đạt).
- Hoàn thiện email xác nhận đăng ký (optional): gửi link xác nhận sau đăng ký, endpoint xác nhận token và gửi lại email xác nhận.

---

## 1️⃣ Yêu cầu Chức năng (Functional Requirements - FR)

Tổ chức theo các mô-đun chính của hệ thống.

### A. Mô-đun Xác thực & Quản trị Người dùng (Authentication & User Management)
Mô tả: Quản lý vòng đời người dùng, phân quyền và bảo mật truy cập.
- **Ứng dụng:** Web App (User access), WPF AdminApp (Management).
- **Vai trò:** Toàn bộ (Đăng nhập/Hồ sơ), Admin/Staff (Quản trị).

- [x] **Đăng ký (Register)** [Web App] [Student/Lecturer]
    - [x] Tạo tài khoản mới với các trường bắt buộc (Email, Password, Role).
    - [x] Xác thực định dạng email (miền nội bộ trường đại học).
    - [x] Kiểm tra độ mạnh mật khẩu (Tối thiểu 8 ký tự, chữ hoa, chữ thường, số, ký tự đặc biệt).
    - [x] Gửi email xác nhận (Optional).
- [x] **Đăng nhập (Login)** [Web App/WPF App] [All Roles]
    - [x] Đăng nhập bằng Cookie (Web) và JWT (API).
    - [x] Lưu trữ mật khẩu bằng hashing BCrypt.
    - [x] Tích hợp Remember Me.
    - [x] Chống tấn công Brute Force (Lockout sau 5 lần thử sai).
- [/] **Đăng xuất (Logout)** [Web App/WPF App] [All Roles]
    - [x] Thu hồi session cookie.
    - [/] Vô hiệu hóa JWT/Refresh Token (đã revoke Refresh Token qua API logout; chưa triển khai blacklist Access Token tức thời).
- [x] **Refresh Token** [Web App/WPF App] [All Roles]
    - [x] Cấp mới Access Token bằng Refresh Token (cho AdminApp/API).
    - [x] Refresh Token quay vòng (Rotation) để tăng tính bảo mật.
- [x] **Quản lý Mật khẩu (Password Management)** [Web App] [All Roles]
    - [x] Khôi phục mật khẩu qua email (Reset Password).
    - [x] Thay đổi mật khẩu (Yêu cầu mật khẩu cũ).
- [x] **Phân quyền & Vai trò (Role & Policy-based Authorization)** [WPF AdminApp] [Admin]
    - [x] Phân quyền dựa trên Role: Admin, Staff, Lecturer, Student.
    - [x] Phân quyền dựa trên Policy (ví dụ: `CanEditQuestion`, `CanGradeExam`).
    - [x] Bảo vệ API Endpoints và UI Components theo quyền hạn.
- [x] **Quản lý Hồ sơ (Profile Management)** [Web App] [All Roles]
    - [x] Xem và cập nhật thông tin cá nhân (Họ tên, ngày sinh, ảnh đại diện).
- [x] **Xử lý Đa thiết bị & Phiên làm việc (Multi-device & Session Handling)** [Web App] [All Roles]
    - [x] Hiển thị danh sách các phiên làm việc đang hoạt động.
    - [x] Chức năng "Đăng xuất khỏi tất cả các thiết bị".
    - [x] Vô hiệu hóa phiên làm việc ngay lập tức khi vai trò người dùng thay đổi.

---

### B. Mô-đun Quản lý Học vụ (Academic Management)
Mô tả: Quản lý cấu trúc tổ chức và dữ liệu nền tảng của trường học.
- **Ứng dụng:** WPF AdminApp (Quản lý), Web App (Giảng viên/Sinh viên xem).
- **Vai trò:** Admin, Staff.

- [x] **Quản lý Khoa (Faculty Management)** [WPF AdminApp] [Admin/Staff]
    - [x] Thêm, Sửa, Xóa khoa (Soft Delete).
    - [x] Gán mã khoa duy nhất.
- [x] **Quản lý Lớp (Student Class Management)** [WPF AdminApp] [Admin/Staff]
    - [x] Tạo lớp học, gán thuộc khoa.
    - [x] Quản lý danh sách sinh viên trong lớp.
- [x] **Quản lý Khóa học/Học phần (Course Management)** [WPF AdminApp] [Admin/Staff]
    - [x] Quản lý thông tin học phần (Mã HP, Tên HP, Số tín chỉ).
    - [x] Phân loại học phần theo Khoa/Bộ môn.
- [x] **Quản lý Giảng viên (Lecturer Assignment)** [WPF AdminApp] [Admin/Staff]
    - [x] Gán giảng viên phụ trách học phần theo học kỳ.
    - [x] Hoàn thiện giao diện, chức năng trong màn hình admin app
- [x] **Quản lý Học kỳ & Năm học (Semester & Academic Year)** [WPF AdminApp] [Admin]
    - [x] Định nghĩa các học kỳ (Học kỳ 1, 2, Hè) và năm học.
    - [x] Thiết lập học kỳ hiện tại cho hệ thống.
- [x] **Quản lý Nhập dữ liệu hàng loạt (Bulk Import)** [WPF AdminApp] [Admin/Staff]
    - [x] Nhập danh sách sinh viên từ file Excel (OpenXML).
    - [x] Nhập danh sách môn học từ file Excel.
    - [x] Quy tắc Validation: Kiểm tra trùng mã, kiểm tra định dạng dữ liệu, báo lỗi chi tiết theo từng dòng.
- [x] **Enrollment Management** [WPF AdminApp] [Admin/Staff]
    - [x] Đăng ký sinh viên vào các lớp học phần/kỳ thi.

---

### C. Ngân hàng Câu hỏi (Question Bank)
Mô tả: Kho lưu trữ câu hỏi phong phú, hỗ trợ đa dạng loại hình và quy trình kiểm duyệt.
- **Ứng dụng:** Web App (Giảng viên soạn bài), WPF AdminApp (Quản lý/Phê duyệt).
- **Vai trò:** Lecturer, Staff.

- [x] **Tạo & Quản lý Câu hỏi (CRUD)** [Web App] [Lecturer]
    - [x] Tạo câu hỏi theo từng loại:
        - [x] MCQ (Single/Multiple choice).
        - [x] Đúng/Sai (True/False).
        - [x] Tự luận (Essay).
        - [x] Nối câu (Matching).
        - [x] Kéo thả (Drag & Drop).
    - [x] Đính kèm phương tiện (Media: Hình ảnh, âm thanh, video).
    - [x] Gắn nhãn độ khó (Dễ, Trung bình, Khó).
    - [x] Gắn tag kỹ năng/kiến thức (Skill tagging).
- [x] **Quy trình Phê duyệt (Approval Workflow)** [WPF AdminApp/Web App] [Staff/Admin]
    - [x] Trạng thái câu hỏi: Draft (Nháp) -> Pending (Chờ duyệt) -> Approved (Đã duyệt) -> Rejected (Từ chối).
    - [x] Chỉ giảng viên/staff có quyền mới được phê duyệt. (Note: Đăng ký quyền trong PermissionService)
- [x] **Phiên bản & Lịch sử (Versioning)** [Web App] [Lecturer/Staff]
    - [x] Lưu vết các thay đổi của câu hỏi thông qua Audit Log/Versioning logic.
    - [x] Khôi phục phiên bản cũ nếu cần (Restore từ Audit trên màn hình chỉnh sửa câu hỏi).
- [/] **Import/Export Hàng loạt** [WPF AdminApp/Web App] [Lecturer/Staff]
    - [x] Import từ Excel theo template chuẩn (Hỗ trợ Question, Options, Matching, DragDrop).
    - [/] Xử lý hình ảnh nhúng trong Excel (Hiện tại hỗ trợ qua URL/Meta, chưa hỗ trợ binary embedded).
- [x] **Phát hiện Trùng lặp (Duplicate Detection)** [Web App] [Lecturer]
    - [x] Kiểm tra trùng lặp cơ bản (Exact match on Content + Type + Subject) khi Import.
    - [x] Kiểm tra độ tương đồng nội dung nâng cao (Jaccard token similarity, chặn tạo/sửa/import câu hỏi tương đồng cao).

---

### D. Quản lý Đề thi (Test Management)
Mô tả: Thiết lập cấu trúc đề thi và các quy tắc liên quan.
- **Ứng dụng:** Web App (Giảng viên tạo đề), WPF AdminApp (Quản trị).
- **Vai trò:** Lecturer, Staff.

- [x] **Tạo đề thi thủ công (Manual Test Creation)** [Web App] [Lecturer]
    - [x] Chọn câu hỏi cụ thể từ ngân hàng câu hỏi.
- [x] **Tạo đề thi tự động (Auto Generation)** [Web App] [Lecturer]
    - [x] Cấu hình ma trận đề thi: Chọn số lượng câu hỏi theo độ khó, chương/bài, loại câu hỏi.
    - [x] Thuật toán bốc thăm ngẫu nhiên đảm bảo phân phối độ khó.
- [x] **Cấu hình Đề thi** [Web App] [Lecturer]
    - [x] Thiết lập thời gian làm bài (phút).
    - [x] Thiết lập điểm chuẩn (Passing score).
    - [x] Cơ chế Shuffle (Hoán vị câu hỏi và hoán vị đáp án).
- [x] **Quản lý Snapshot** [Server-side] [System]
    - [x] Khi xuất bản đề thi, tạo bản sao (Snapshot) cố định của tất cả câu hỏi để tránh thay đổi sau này ảnh hưởng đến bài thi đã làm.
- [x] **Quản lý Trạng thái** [Web App/WPF AdminApp] [Lecturer/Staff]
    - [x] Publish/Unpublish đề thi.
    - [x] Nhân bản đề thi (Clone) và Lưu trữ (Archive).

---

### E. Lập Lịch thi (Exam Scheduling)
Mô tả: Sắp xếp ca thi, phòng thi và thí sinh.
- **Ứng dụng:** Web App (Giao đề), WPF AdminApp (Lập lịch tập trung).
- **Vai trò:** Staff, Admin, Lecturer.

- [x] **Tạo Lịch thi (Exam Schedule)** [Web App/WPF AdminApp] [Staff/Lecturer]
    - [x] Chọn đề thi, ngày thi, giờ thi.
    - [x] Gán phòng thi và sức chứa (Capacity theo cấu hình phòng).
- [x] **Kiểm tra Xung đột (Conflict Detection)** [System] [Staff]
    - [x] Cảnh báo nếu một sinh viên/giảng viên bị trùng lịch thi.
    - [x] Cảnh báo nếu phòng thi bị quá tải hoặc trùng giờ.
- [/] **Khóa/Mở Lịch thi** [WPF AdminApp/Web App] [Staff/Admin]
    - [/] Cho phép hoặc ngăn chặn sinh viên truy cập bài thi ngoài khung giờ thi (đang chặn theo khung giờ + assignment; chưa có toggle khóa/mở thủ công theo lịch).
- [/] **Xuất báo cáo lịch thi** [WPF AdminApp/Web App] [Staff]
    - [/] Xuất CSV lịch thi (chưa có PDF/Excel template chính thức).

---

### F. Thực hiện Bài thi - Sinh viên (Test Taking)
Mô tả: Giao diện và logic cho sinh viên làm bài trực tuyến.
- **Ứng dụng:** Web App.
- **Vai trò:** Student.

- [/] **Quá trình Làm bài** [Web App] [Student]
    - [/] Truy cập bài thi bằng Token bảo mật (đã áp dụng cho luồng vào bài từ lịch thi).
    - [x] Đồng hồ đếm ngược (Timer) chính xác từng giây.
    - [x] Tự động lưu bài thi (Auto-save) sau mỗi X giây hoặc khi chuyển câu hỏi.
    - [x] Hỗ trợ xem lại danh sách câu hỏi và đánh dấu câu chưa làm.
- [/] **Bảo mật & Chống gian lận (Anti-cheat)** [Web App] [Student]
    - [x] Phát hiện chuyển Tab/Cửa sổ (Window Blur event).
    - [x] Ngăn chặn phím tắt (F12, Ctr+C, Ctrl+V, v.v.).
    - [x] Giới hạn mỗi tài khoản chỉ được thực hiện trên 1 thiết bị/phiên duy nhất.
- [/] **Nộp bài (Submission)** [Web App] [Student]
    - [x] Xác nhận trước khi nộp.
    - [x] Tự động nộp bài khi hết giờ (Timeout auto-submit).
    - [/] Xử lý mất kết nối: Lưu trạng thái bài thi locally và đồng bộ lại khi có mạng (đã có local + auto-save server; chưa có cơ chế retry queue offline đầy đủ).

---

### G. Hệ thống Chấm điểm (Grading System)
Mô tả: Chấm điểm tự động và thủ công.
- **Ứng dụng:** Web App (Giảng viên chấm), WPF AdminApp (Giám sát).
- **Vai trò:** Lecturer (Chấm), Staff/Admin (Xem).

- [x] **Chấm điểm tự động (Auto Grading)** [Server-side] [Student]
    - [x] Áp dụng ngay sau khi nộp bài cho MCQ, Đúng/Sai.
    - [x] Hỗ trợ chấm điểm từng phần (Partial scoring) cho các loại câu hỏi phức tạp.
- [x] **Chấm thủ công (Manual Grading - Essay)** [Web App] [Lecturer]
    - [x] Giảng viên truy cập giao diện chấm bài tự luận.
    - [x] Nhận xét và cho điểm từng câu hỏi.
- [x] **Moderation & Regrade** [Web App] [Lecturer/Staff]
    - [x] Tính năng phúc khảo (Regrade request).
    - [x] Nhật ký thay đổi điểm (Audit trail).
    - [x] Khóa điểm (Grade locking) sau khi hoàn tất.

---

### H. Bảng điểm & GPA (Transcript & GPA)
Mô tả: Tổng hợp kết quả học tập.
- **Ứng dụng:** Web App (Sinh viên/Giảng viên xem), WPF AdminApp (Quản lý).
- **Vai trò:** Student (Xem), Lecturer (Xem), Staff/Admin (Quản lý).

- [/] **Tính toán Điểm** [Server-side/WPF AdminApp] [Staff]
    - [/] Tính điểm trung bình (GPA) theo học kỳ và năm học (đã có GPA tích lũy + hiển thị GPA theo kỳ; chưa tách pipeline year-end hoàn chỉnh).
    - [x] Xử lý công thức tính điểm phức tạp (trọng số thi, trọng số bài tập).
- [x] **Quản lý Bảng điểm** [WPF AdminApp] [Staff/Admin]
    - [x] Khóa/Mở bảng điểm cấp khoa/trường.
    - [x] Lọc bảng điểm theo khoa, lớp, kỳ học.
- [x] **Xuất Bảng điểm** [Web App/WPF AdminApp] [Student/Staff]
    - [x] Xuất PDF chuyên nghiệp bằng QuestPDF.

---

### I. Báo cáo & Phân tích (Reporting & Analytics)
Mô tả: Cung cấp góc nhìn số liệu cho nhà quản lý.
- **Ứng dụng:** Web App (Dashboard), WPF AdminApp (Báo cáo chi tiết/Export).
- **Vai trò:** Lecturer, Staff, Admin.

- [x] **Widget Dashboard** [Web App] [Lecturer/Staff/Admin]
    - [x] Tỷ lệ đạt/trượt theo môn.
    - [x] Điểm trung bình qua các kỳ.
    - [x] Biểu đồ phổ điểm.
- [x] **Phân tích Câu hỏi** [Web App/WPF AdminApp] [Lecturer/Staff]
    - [x] Đánh giá độ khó thực tế của câu hỏi dựa trên kết quả thi.
    - [x] Độ phân biệt của câu hỏi.
- [x] **Báo cáo Hiệu suất** [WPF AdminApp/Web App] [Staff/Admin]
    - [x] Báo cáo giảng dạy (Lecturer performance).
    - [x] Báo cáo chất lượng đào tạo theo Khoa.

---

### J. WPF AdminApp (Dành cho Cán bộ & Quản trị)
Mô tả: Ứng dụng Desktop quản lý chuyên sâu.
- **Ứng dụng:** WPF AdminApp.
- **Vai trò:** Admin, Staff.

- [x] **Chế độ Admin:** [WPF AdminApp] [Admin]
    - [x] Quản lý toàn bộ người dùng và vai trò.
    - [x] Xem nhật ký hệ thống (Audit log).
    - [x] Cấu hình tham số hệ thống.
    - [x] Sao lưu và Phục hồi cơ sở dữ liệu (Backup/Restore).
- [x] **Chế độ Staff:** [WPF AdminApp] [Staff]
    - [x] Thực hiện mọi thao tác học vụ (Quản lý khoa, lớp, sinh viên).
    - [x] Lập lịch thi đồng loạt.
    - [x] Export báo cáo định kỳ.
- [x] **UI/UX & Kỹ thuật:** [WPF AdminApp] [Developer]
    - [x] MVVM Pattern chuẩn chỉnh.
    - [x] Quản lý Token (JWT/Refresh Token) an toàn.
    - [x] Điều khiển hiển thị menu dựa trên quyền hạn Role-based.
    - [x] Hiệu ứng chuyển cảnh mượt mà, Dark/Light mode.

---

## 2️⃣ Yêu cầu Phi chức năng (Non-Functional Requirements - NFR)
... (Phần còn lại giữ nguyên)

---

## 2️⃣ Yêu cầu Phi chức năng (Non-Functional Requirements - NFR)

### A. Hiệu năng (Performance)
- [ ] Hỗ trợ tối thiểu 10.000 người dùng đồng thời (Concurrent users).
- [ ] API Response Time: < 200ms cho các truy vấn thông thường; < 1s cho các truy vấn dữ liệu lớn.
- [ ] DB Query Tuning: Mọi truy vấn sử dụng Index, không có full table scan trên các bảng lớn.
- [ ] Độ trễ nộp bài thi: Khoảng thời gian từ khi click nộp đến khi xác nhận < 2s.
- [ ] Auto-save Interval: Mặc định mỗi 30 - 60 giây.

### B. Bảo mật (Security)
- [ ] Mã hóa mật khẩu: BCrypt với Work Factor tối ưu.
- [ ] Bảo vệ JWT: Sử dụng Secret Key mạnh, thuật toán RS256 hoặc HS256.
- [ ] Bảo mật Cookies: `HttpOnly`, `Secure`, `SameSite=Strict`.
- [ ] Ngăn chặn các lỗ hổng OWASP Top 10:
    - [ ] XSS Prevention (Data encoding).
    - [ ] SQL Injection Prevention (EF Core Parameterized queries).
    - [ ] CSRF Protection (Antiforgery tokens).
    - [ ] IDOR Prevention (Kiểm tra quyền sở hữu Resource trước khi thao tác).
- [ ] Rate Limiting: Chống DoS và brute force tại các Endpoint đăng nhập/quên mật khẩu.
- [ ] Privilege Escalation: Kiểm tra chặt chẽ Business Logic để tránh leo thang đặc quyền.

### C. Khả năng Sẵn sàng (Availability)
- [ ] Mục tiêu Uptime: 99.9%.
- [ ] Cơ chế Backup: Backup Database hàng ngày (Daily), lưu trữ off-site.
- [ ] Kế hoạch khắc phục thảm họa (Disaster Recovery): RTO < 4h, RPO < 24h.
- [ ] Health Checks: Triển khai các Endpoint kiểm tra trạng thái dịch vụ (DB, API, Storage).

### D. Khả năng Mở rộng (Scalability)
- [ ] Thiết kế theo Stateless API để dễ dàng Scale-out (Horizontal Scaling).
- [ ] Caching Strategy: Sử dụng Redis hoặc In-memory cache cho các dữ liệu ít thay đổi (Danh mục, Tham số cấu hình).
- [ ] Tối ưu hóa Database: Phân mảnh dữ liệu hoặc Replication nếu dữ liệu vượt ngưỡng.

### E. Khả năng Bảo trì (Maintainability)
- [ ] Tuân thủ Clean Architecture (Domain, Application, Infrastructure, Web/API).
- [ ] Logging Standard: Sử dụng Serilog, ghi log vào File/Database/ElasticSearch.
- [ ] Error Handling: Xử lý Exception tập trung (Global Exception Middleware/Filter).
- [ ] Tài liệu hóa API: Swagger/OpenAPI đầy đủ mô tả, ví dụ và schema.

### F. Kiểm toán (Audit & Compliance)
- [ ] Ghi lại mọi hành động nhạy cảm (Tạo/Sửa/Xóa đề thi, Đổi điểm, Thay đổi vai trò).
- [ ] Lưu vết địa chỉ IP, thời gian và ID người dùng thực hiện hành động.
- [ ] Theo dõi các phiên đăng nhập thất bại liên tục.

---

## 3️⃣ Danh sách Kiểm thử (Testing Checklist)

- [ ] **Unit Tests:** Bao phủ tối thiểu 80% logic nghiệp vụ tại lớp Application.
- [ ] **Integration Tests:** Kiểm tra sự tương tác giữa EF Core và SQL Server.
- [ ] **API Tests:** Verify mã trạng thái (200, 400, 401, 403, 500) và dữ liệu trả về.
- [ ] **Role Permission Tests:** Thử nghiệm dùng tài khoản Student để truy cập API của Staff/Admin và ngược lại.
- [ ] **Load Tests:** Giả lập môi trường thi thật với hàng ngàn request đồng thời lên API làm bài.
- [ ] **Security Tests:** Kiểm tra Penetration cơ bản, thử nghiệm SQLi, XSS.
- [ ] **Edge Case Tests:** Nộp bài khi mất mạng, đổi mật khẩu khi đang làm bài, nhập liệu ký tự đặc biệt lạ.

---

## 4️⃣ Danh sách Triển khai (Deployment Checklist)

- [ ] **Environment Configuration:** Thiết lập biến môi trường riêng cho Dev, Staging, Production.
- [ ] **Database Migration:** Chạy các lệnh migration cập nhật Schema trên Production.
- [ ] **Seed Data:** Chạy script tạo tài khoản Admin mặc định và dữ liệu hệ thống nền (Khoa, Loại câu hỏi).
- [ ] **Production Settings:** Tắt hiển thị lỗi chi tiết (Custom error pages), bật nén Gzip/Brotli.
- [ ] **SSL/TLS:** Cài đặt chứng chỉ SSL hợp lệ cho mọi Domain.
- [ ] **Monitoring Setup:** Cài đặt công cụ giám sát hiệu năng (Application Insights/Prometheus).

---

## 5️⃣ Danh sách Sẵn sàng Vận hành (Production Readiness)

- [ ] **Về Bảo mật:** Review lại toàn bộ phân quyền API và Token Security.
- [ ] **Về Hiệu năng:** Đã vượt qua bài kiểm tra tải tối thiểu (Minimal Load Test).
- [ ] **Về Dữ liệu:** Đảm bảo toàn vẹn dữ liệu (Data Integrity) và cơ chế Backup đã hoạt động.
- [ ] **Về Người dùng:** Tài liệu hướng dẫn sử dụng cho Staff, Lecturer và Student đã hoàn thiện.
- [ ] **Về Hệ thống:** Mọi logging và cảnh báo đã được cấu hình và kiểm tra.

**Ký tên xác nhận:**

---------------------------
*Project Lead / Architect*
