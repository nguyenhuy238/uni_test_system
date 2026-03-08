# UniTestSystem - Danh Sách Kiểm Tra Phát Triển Toàn Diện (Master Development Checklist)

Tài liệu này đóng vai trò là danh sách kiểm tra chủ chốt cho toàn bộ quá trình phát triển hệ thống quản lý thi Đại học (UniTestSystem). Được thiết kế theo chuẩn Enterprise, bao gồm mọi khía cạnh từ chức năng, phi chức năng đến quy trình triển khai và sẵn sàng vận hành.

---

## 1️⃣ Yêu cầu Chức năng (Functional Requirements - FR)

Tổ chức theo các mô-đun chính của hệ thống.

### A. Mô-đun Xác thực & Quản trị Người dùng (Authentication & User Management)
Mô tả: Quản lý vòng đời người dùng, phân quyền và bảo mật truy cập.

- [ ] **Đăng ký (Register)**
    - [ ] Tạo tài khoản mới với các trường bắt buộc (Email, Password, Role).
    - [ ] Xác thực định dạng email (miền nội bộ trường đại học).
    - [ ] Kiểm tra độ mạnh mật khẩu (Tối thiểu 8 ký tự, chữ hoa, chữ thường, số, ký tự đặc biệt).
    - [ ] Gửi email xác nhận (Optional).
- [ ] **Đăng nhập (Login)**
    - [ ] Đăng nhập bằng Cookie (Web) và JWT (API).
    - [ ] Lưu trữ mật khẩu bằng hashing BCrypt.
    - [ ] Tích hợp Remember Me.
    - [ ] Chống tấn công Brute Force (Lockout sau 5 lần thử sai).
- [ ] **Đăng xuất (Logout)**
    - [ ] Thu hồi session cookie.
    - [ ] Vô hiệu hóa JWT/Refresh Token (Blacklisting).
- [ ] **Refresh Token**
    - [ ] Cấp mới Access Token bằng Refresh Token (cho AdminApp/API).
    - [ ] Refresh Token quay vòng (Rotation) để tăng tính bảo mật.
- [ ] **Quản lý Mật khẩu (Password Management)**
    - [ ] Khôi phục mật khẩu qua email (Reset Password).
    - [ ] Thay đổi mật khẩu (Yêu cầu mật khẩu cũ).
- [ ] **Phân quyền & Vai trò (Role & Policy-based Authorization)**
    - [ ] Phân quyền dựa trên Role: Admin, Staff, Lecturer, Student.
    - [ ] Phân quyền dựa trên Policy (ví dụ: `CanEditQuestion`, `CanGradeExam`).
    - [ ] Bảo vệ API Endpoints và UI Components theo quyền hạn.
- [ ] **Quản lý Hồ sơ (Profile Management)**
    - [ ] Xem và cập nhật thông tin cá nhân (Họ tên, ngày sinh, ảnh đại diện).
- [ ] **Xử lý Đa thiết bị & Phiên làm việc (Multi-device & Session Handling)**
    - [ ] Hiển thị danh sách các phiên làm việc đang hoạt động.
    - [ ] Chức năng "Đăng xuất khỏi tất cả các thiết bị".
    - [ ] Vô hiệu hóa phiên làm việc ngay lập tức khi vai trò người dùng thay đổi.

**Vai trò truy cập:**
- Admin: Toàn quyền quản lý người dùng và vai trò.
- Staff: Quản lý tài khoản sinh viên và giảng viên thuộc khoa.
- Lecturer/Student: Chỉ quản lý hồ sơ và mật khẩu cá nhân.

---

### B. Mô-đun Quản lý Học vụ (Academic Management)
Mô tả: Quản lý cấu trúc tổ chức và dữ liệu nền tảng của trường học.

- [ ] **Quản lý Khoa (Faculty Management)**
    - [ ] Thêm, Sửa, Xóa khoa (Soft Delete).
    - [ ] Gán mã khoa duy nhất.
- [ ] **Quản lý Lớp (Student Class Management)**
    - [ ] Tạo lớp học, gán thuộc khoa.
    - [ ] Quản lý danh sách sinh viên trong lớp.
- [ ] **Quản lý Khóa học/Học phần (Course Management)**
    - [ ] Quản lý thông tin học phần (Mã HP, Tên HP, Số tín chỉ).
    - [ ] Phân loại học phần theo Khoa/Bộ môn.
- [ ] **Quản lý Giảng viên (Lecturer Assignment)**
    - [ ] Gán giảng viên phụ trách học phần theo học kỳ.
- [ ] **Quản lý Học kỳ & Năm học (Semester & Academic Year)**
    - [ ] Định nghĩa các học kỳ (Học kỳ 1, 2, Hè) và năm học.
    - [ ] Thiết lập học kỳ hiện tại cho hệ thống.
- [ ] **Quản lý Nhập dữ liệu hàng loạt (Bulk Import)**
    - [ ] Nhập danh sách sinh viên từ file Excel (OpenXML).
    - [ ] Nhập danh sách môn học từ file Excel.
    - [ ] Quy tắc Validation: Kiểm tra trùng mã, kiểm tra định dạng dữ liệu, báo lỗi chi tiết theo từng dòng.
- [ ] **Enrollment Management**
    - [ ] Đăng ký sinh viên vào các lớp học phần/kỳ thi.

**Vai trò truy cập:**
- Staff: Thực hiện các nghiệp vụ quản lý học vụ chính.
- Admin: Giám sát và cấu hình hệ thống.

---

### C. Ngân hàng Câu hỏi (Question Bank)
Mô tả: Kho lưu trữ câu hỏi phong phú, hỗ trợ đa dạng loại hình và quy trình kiểm duyệt.

- [ ] **Tạo & Quản lý Câu hỏi (CRUD)**
    - [ ] Tạo câu hỏi theo từng loại:
        - [ ] MCQ (Single/Multiple choice).
        - [ ] Đúng/Sai (True/False).
        - [ ] Tự luận (Essay).
        - [ ] Nối câu (Matching).
        - [ ] Kéo thả (Drag & Drop).
    - [ ] Đính kèm phương tiện (Media: Hình ảnh, âm thanh, video).
    - [ ] Gắn nhãn độ khó (Dễ, Trung bình, Khó).
    - [ ] Gắn tag kỹ năng/kiến thức (Skill tagging).
- [ ] **Quy trình Phê duyệt (Approval Workflow)**
    - [ ] Trạng thái câu hỏi: Draft (Nháp) -> Pending (Chờ duyệt) -> Approved (Đã duyệt) -> Rejected (Từ chối).
    - [ ] Chỉ giảng viên/staff có quyền mới được phê duyệt.
- [ ] **Phiên bản & Lịch sử (Versioning)**
    - [ ] Lưu vết các thay đổi của câu hỏi.
    - [ ] Khôi phục phiên bản cũ nếu cần.
- [ ] **Import/Export Hàng loạt**
    - [ ] Import từ Excel theo template chuẩn.
    - [ ] Xử lý hình ảnh nhúng trong Excel (nếu có).
- [ ] **Phát hiện Trùng lặp (Duplicate Detection)**
    - [ ] Kiểm tra độ tương đồng nội dung câu hỏi để tránh trùng lặp.

**Vai trò truy cập:**
- Lecturer: Soạn thảo câu hỏi.
- Staff: Phê duyệt câu hỏi.

---

### D. Quản lý Đề thi (Test Management)
Mô tả: Thiết lập cấu trúc đề thi và các quy tắc liên quan.

- [ ] **Tạo đề thi thủ công (Manual Test Creation)**
    - [ ] Chọn câu hỏi cụ thể từ ngân hàng câu hỏi.
- [ ] **Tạo đề thi tự động (Auto Generation)**
    - [ ] Cấu hình ma trận đề thi: Chọn số lượng câu hỏi theo độ khó, chương/bài, loại câu hỏi.
    - [ ] Thuật toán bốc thăm ngẫu nhiên đảm bảo phân phối độ khó.
- [ ] **Cấu hình Đề thi**
    - [ ] Thiết lập thời gian làm bài (phút).
    - [ ] Thiết lập điểm chuẩn (Passing score).
    - [ ] Cơ chế Shuffle (Hoán vị câu hỏi và hoán vị đáp án).
- [ ] **Quản lý Snapshot**
    - [ ] Khi xuất bản đề thi, tạo bản sao (Snapshot) cố định của tất cả câu hỏi để tránh thay đổi sau này ảnh hưởng đến bài thi đã làm.
- [ ] **Quản lý Trạng thái**
    - [ ] Publish/Unpublish đề thi.
    - [ ] Nhân bản đề thi (Clone) và Lưu trữ (Archive).

---

### E. Lập Lịch thi (Exam Scheduling)
Mô tả: Sắp xếp ca thi, phòng thi và thí sinh.

- [ ] **Tạo Lịch thi (Exam Schedule)**
    - [ ] Chọn đề thi, ngày thi, giờ thi.
    - [ ] Gán phòng thi và sức chứa (Capacity).
- [ ] **Kiểm tra Xung đột (Conflict Detection)**
    - [ ] Cảnh báo nếu một sinh viên/giảng viên bị trùng lịch thi.
    - [ ] Cảnh báo nếu phòng thi bị quá tải hoặc trùng giờ.
- [ ] **Khóa/Mở Lịch thi**
    - [ ] Cho phép hoặc ngăn chặn sinh viên truy cập bài thi ngoài khung giờ thi.
- [ ] **Xuất báo cáo lịch thi**
    - [ ] Xuất PDF/Excel lịch thi cho sinh viên và cán bộ coi thi.

---

### F. Thực hiện Bài thi - Sinh viên (Test Taking)
Mô tả: Giao diện và logic cho sinh viên làm bài trực tuyến.

- [ ] **Quá trình Làm bài**
    - [ ] Truy cập bài thi bằng Token bảo mật.
    - [ ] Đồng hồ đếm ngược (Timer) chính xác từng giây.
    - [ ] Tự động lưu bài thi (Auto-save) sau mỗi X giây hoặc khi chuyển câu hỏi.
    - [ ] Hỗ trợ xem lại danh sách câu hỏi và đánh dấu câu chưa làm.
- [ ] **Bảo mật & Chống gian lận (Anti-cheat)**
    - [ ] Phát hiện chuyển Tab/Cửa sổ (Window Blur event).
    - [ ] Ngăn chặn phím tắt (F12, Ctr+C, Ctrl+V, v.v.).
    - [ ] Giới hạn mỗi tài khoản chỉ được thực hiện trên 1 thiết bị/phiên duy nhất.
- [ ] **Nộp bài (Submission)**
    - [ ] Xác nhận trước khi nộp.
    - [ ] Tự động nộp bài khi hết giờ (Timeout auto-submit).
    - [ ] Xử lý mất kết nối: Lưu trạng thái bài thi locally và đồng bộ lại khi có mạng.

---

### G. Hệ thống Chấm điểm (Grading System)
Mô tả: Chấm điểm tự động và thủ công.

- [ ] **Chấm điểm tự động (Auto Grading)**
    - [ ] Áp dụng ngay sau khi nộp bài cho MCQ, Đúng/Sai.
    - [ ] Hỗ trợ chấm điểm từng phần (Partial scoring) cho các loại câu hỏi phức tạp.
- [ ] **Chấm thủ công (Manual Grading - Essay)**
    - [ ] Giảng viên truy cập giao diện chấm bài tự luận.
    - [ ] Nhận xét và cho điểm từng câu hỏi.
- [ ] **Moderation & Regrade**
    - [ ] Tính năng phúc khảo (Regrade request).
    - [ ] Nhật ký thay đổi điểm (Audit trail).
    - [ ] Khóa điểm (Grade locking) sau khi hoàn tất.

---

### H. Bảng điểm & GPA (Transcript & GPA)
Mô tả: Tổng hợp kết quả học tập.

- [ ] **Tính toán Điểm**
    - [ ] Tính điểm trung bình (GPA) theo học kỳ và năm học.
    - [ ] Xử lý công thức tính điểm phức tạp (trọng số thi, trọng số bài tập).
- [ ] **Quản lý Bảng điểm**
    - [ ] Khóa/Mở bảng điểm cấp khoa/trường.
    - [ ] Lọc bảng điểm theo khoa, lớp, kỳ học.
- [ ] **Xuất Bảng điểm**
    - [ ] Xuất PDF chuyên nghiệp bằng QuestPDF.

---

### I. Báo cáo & Phân tích (Reporting & Analytics)
Mô tả: Cung cấp góc nhìn số liệu cho nhà quản lý.

- [ ] **Widget Dashboard**
    - [ ] Tỷ lệ đạt/trượt theo môn.
    - [ ] Điểm trung bình qua các kỳ.
    - [ ] Biểu đồ phổ điểm.
- [ ] **Phân tích Câu hỏi**
    - [ ] Đánh giá độ khó thực tế của câu hỏi dựa trên kết quả thi.
    - [ ] Độ phân biệt của câu hỏi.
- [ ] **Báo cáo Hiệu suất**
    - [ ] Báo cáo giảng dạy (Lecturer performance).
    - [ ] Báo cáo chất lượng đào tạo theo Khoa.

---

### J. WPF AdminApp (Dành cho Cán bộ & Quản trị)
Mô tả: Ứng dụng Desktop quản lý chuyên sâu.

- [ ] **Chế độ Admin:**
    - [ ] Quản lý toàn bộ người dùng và vai trò.
    - [ ] Xem nhật ký hệ thống (Audit log).
    - [ ] Cấu hình tham số hệ thống.
    - [ ] Sao lưu và Phục hồi cơ sở dữ liệu (Backup/Restore).
- [ ] **Chế độ Staff:**
    - [ ] Thực hiện mọi thao tác học vụ (Quản lý khoa, lớp, sinh viên).
    - [ ] Lập lịch thi đồng loạt.
    - [ ] Export báo cáo định kỳ.
- [ ] **UI/UX & Kỹ thuật:**
    - [ ] MVVM Pattern chuẩn chỉnh.
    - [ ] Quản lý Token (JWT/Refresh Token) an toàn.
    - [ ] Điều khiển hiển thị menu dựa trên quyền hạn Role-based.
    - [ ] Hiệu ứng chuyển cảnh mượt mà, Dark/Light mode.

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
