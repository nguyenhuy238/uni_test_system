# Employee Survey System

> Hệ thống Khảo sát & Đánh giá Năng lực Nhân viên — xây dựng trên nền tảng ASP.NET Core 8 MVC + WPF Desktop Admin.

---

## Mục lục

- [1. Tổng quan dự án](#1-tổng-quan-dự-án)
- [2. Kiến trúc hệ thống](#2-kiến-trúc-hệ-thống)
- [3. Công nghệ sử dụng](#3-công-nghệ-sử-dụng)
- [4. Cấu trúc thư mục](#4-cấu-trúc-thư-mục)
- [5. Domain Models](#5-domain-models)
- [6. Các chức năng đã phát triển](#6-các-chức-năng-đã-phát-triển)
- [7. Các chức năng cần phát triển tiếp](#7-các-chức-năng-cần-phát-triển-tiếp)
- [8. Quy trình phát triển & Timeline](#8-quy-trình-phát-triển--timeline)
- [9. Hướng dẫn cài đặt & chạy dự án](#9-hướng-dẫn-cài-đặt--chạy-dự-án)
- [10. Tài khoản mặc định](#10-tài-khoản-mặc-định)
- [11. Cấu hình](#11-cấu-hình)

---

## 1. Tổng quan dự án

**Employee Survey** là hệ thống quản lý khảo sát và đánh giá năng lực nhân viên trong tổ chức. Hệ thống cho phép:

- **Quản trị viên (Admin)**: Tạo & quản lý ngân hàng câu hỏi, tạo đề thi/khảo sát, giao bài cho nhân viên, xem báo cáo & thống kê.
- **Nhân sự/Quản lý (Staff)**: Quản lý phòng ban, nhóm, giám sát tiến độ, duyệt bài, xuất báo cáo.
- **Nhân viên (User)**: Làm bài thi/khảo sát trực tuyến, xem kết quả, phản hồi.

Hệ thống bao gồm 2 ứng dụng:
1. **Web App** (ASP.NET Core MVC) — Giao diện chính cho tất cả người dùng.
2. **Desktop Admin App** (WPF) — Ứng dụng quản trị dành riêng cho Admin, giao tiếp qua REST API/JWT.

---

## 2. Kiến trúc hệ thống

Dự án được xây dựng theo mô hình **Clean Architecture** phân lớp:

```
┌──────────────────────────────────────────────────┐
│                  Presentation                     │
│  ┌──────────────┐    ┌────────────────────────┐  │
│  │  Razor Views │    │  WPF Admin App (MVVM)  │  │
│  └──────┬───────┘    └──────────┬─────────────┘  │
│         │                       │ (REST API/JWT)  │
│  ┌──────▼───────────────────────▼─────────────┐  │
│  │          Controllers (MVC + API)            │  │
│  └──────────────────┬─────────────────────────┘  │
├─────────────────────┼────────────────────────────┤
│              Application Layer                    │
│  ┌──────────────────▼─────────────────────────┐  │
│  │   Services: Auth, Test, Report, Export...   │  │
│  │   Interfaces: IRepository, IAudit...        │  │
│  └──────────────────┬─────────────────────────┘  │
├─────────────────────┼────────────────────────────┤
│               Domain Layer                        │
│  ┌──────────────────▼─────────────────────────┐  │
│  │   Entities: User, Question, Test, Session   │  │
│  │   Enums: Role, QType, TestType, Status      │  │
│  └────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────┤
│            Infrastructure Layer                   │
│  ┌────────────────────────────────────────────┐  │
│  │   EF Core (AppDbContext) → SQL Server       │  │
│  │   SmtpEmailSender, NotificationService      │  │
│  │   Seeder (Data Seed mặc định)               │  │
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### Luồng xác thực kép (Dual Authentication)

| Client | Phương thức | Mô tả |
|--------|-------------|-------|
| Web Browser | Cookie Auth (`emp_survey_auth`) | HttpOnly, SameSite=Lax, SlidingExpiration |
| WPF Admin / API | JWT Bearer Token | Validate Issuer, Audience, Lifetime, SigningKey |

Mật khẩu được mã hóa bằng **BCrypt**.

---

## 3. Công nghệ sử dụng

| Thành phần | Công nghệ | Phiên bản |
|------------|-----------|-----------|
| Backend Framework | ASP.NET Core | .NET 8.0 |
| ORM | Entity Framework Core | 8.0.12 |
| Database | SQL Server (LocalDB) | — |
| Frontend Web | Razor Views + CSS + JavaScript | — |
| Desktop App | WPF (Windows Presentation Foundation) | .NET 8.0 |
| Authentication | Cookie + JWT Bearer | — |
| Password Hashing | BCrypt.Net-Next | 4.0.3 |
| Excel Export | ClosedXML | 0.105.0 |
| PDF Export | QuestPDF | 2025.7.1 |
| API Docs | Swagger (Swashbuckle) | 9.0.3 |
| Email | SMTP (Gmail) | — |

---

## 4. Cấu trúc thư mục

```
Employee Survey/
├── Employee Survey.sln              # Solution file chính
│
├── Employee Survey/                 # Web Application (ASP.NET Core MVC)
│   ├── Domain/                      # 17 Entity classes
│   │   ├── User.cs                  # Nhân viên (id, name, email, role, skill...)
│   │   ├── Question.cs              # Câu hỏi (MCQ, TF, Essay, Matching, DragDrop)
│   │   ├── Test.cs                  # Đề thi/khảo sát
│   │   ├── Session.cs               # Phiên làm bài (tracking time, score)
│   │   ├── Assignment.cs            # Giao bài cho nhân viên/nhóm
│   │   ├── Answer.cs                # Câu trả lời
│   │   ├── Department.cs            # Phòng ban
│   │   ├── Team.cs                  # Nhóm (thuộc phòng ban)
│   │   ├── Feedback.cs              # Phản hồi sau bài thi
│   │   ├── Notification.cs          # Thông báo
│   │   ├── Result.cs                # Kết quả tổng hợp
│   │   ├── Option.cs                # Tùy chọn câu hỏi
│   │   ├── UserAnswer.cs            # Câu trả lời chi tiết
│   │   ├── RolePermissionMapping.cs  # RBAC phân quyền
│   │   ├── SystemSettings.cs        # Cài đặt hệ thống (logo, tên...)
│   │   ├── PasswordReset.cs         # Token reset mật khẩu
│   │   └── Enums.cs                 # Role, QType, TestType, SessionStatus
│   │
│   ├── Application/                 # Business Logic Layer
│   │   ├── AuthService.cs           # Đăng nhập, JWT, Claims
│   │   ├── TestService.cs           # Start/Submit bài, chấm điểm tự động
│   │   ├── TestGenerationService.cs # Auto-generate đề thi từ ngân hàng câu hỏi
│   │   ├── ReportService.cs         # Dashboard, thống kê theo Role/Level/Skill
│   │   ├── ExportService.cs         # Xuất Excel/PDF
│   │   ├── AssignmentService.cs     # Giao bài, xác định đối tượng
│   │   ├── PasswordResetService.cs  # Quên mật khẩu qua email
│   │   ├── PermissionService.cs     # RBAC quản lý quyền
│   │   ├── SettingsService.cs       # Cài đặt hệ thống
│   │   ├── AuditReaderService.cs    # Đọc nhật ký hệ thống
│   │   ├── ScoringAllocator.cs      # Phân bổ điểm cho câu hỏi
│   │   ├── AutoTestOptions.cs       # Cấu hình auto-generate
│   │   └── Interfaces/              # 6 interface files (IAudit, IQuestion...)
│   │
│   ├── Infrastructure/              # Data Access & External Services
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs       # EF Core DbContext (12 DbSets)
│   │   │   ├── EfRepository.cs      # Generic Repository pattern
│   │   │   └── Migrations/          # EF Core migrations
│   │   ├── IRepository.cs           # Generic repository interface
│   │   ├── Seeder.cs                # Seed dữ liệu mặc định
│   │   ├── SmtpEmailSender.cs       # Gửi email qua SMTP
│   │   ├── NotificationService.cs   # Quản lý thông báo
│   │   └── *Options.cs              # Configuration classes
│   │
│   ├── Controllers/                 # 21 MVC Controllers
│   │   ├── AuthController.cs        # Login/Logout/Register
│   │   ├── TestsController.cs       # CRUD đề thi (lớn nhất: ~29KB)
│   │   ├── QuestionsController.cs   # CRUD câu hỏi + Import Excel
│   │   ├── SessionsController.cs    # Quản lý phiên thi
│   │   ├── UsersController.cs       # Quản lý người dùng
│   │   ├── DepartmentsController.cs # Quản lý phòng ban
│   │   ├── TeamsController.cs       # Quản lý nhóm
│   │   ├── ReportsController.cs     # Báo cáo & thống kê
│   │   ├── GradingController.cs     # Chấm bài tự luận
│   │   ├── FeedbackController.cs    # Phản hồi
│   │   ├── AutoTestsController.cs   # Tự động sinh đề
│   │   ├── ProfileController.cs     # Quản lý profile cá nhân
│   │   ├── RolesController.cs       # Phân quyền RBAC
│   │   ├── SettingsController.cs    # Cài đặt hệ thống
│   │   ├── AuditLogsController.cs   # Nhật ký hệ thống
│   │   ├── MyTestsController.cs     # Bài thi của nhân viên
│   │   └── Api/                     # REST API Controllers
│   │       ├── Admin/               # API cho WPF Admin (6 controllers)
│   │       ├── SessionsApiController.cs
│   │       └── User/
│   │
│   ├── Views/                       # 23 View folders (Razor .cshtml)
│   │   ├── Auth/                    # Login, Register, ForgotPassword, ResetPassword
│   │   ├── Tests/                   # CRUD, Detail, Preview, TakeTest, Result
│   │   ├── Questions/               # Index, Create, Edit, Import, Detail
│   │   ├── Reports/                 # Dashboard, Export
│   │   ├── Users/                   # Index, Create, Edit
│   │   ├── Departments/             # Index, Create, Edit
│   │   ├── Teams/                   # Index, Create, Edit
│   │   ├── Sessions/                # Index, Detail
│   │   ├── Grading/                 # Index, Grade
│   │   ├── Feedback/                # Index, Create
│   │   ├── Profile/                 # Index, Edit
│   │   ├── Home/                    # Dashboard tổng quan
│   │   ├── Shared/                  # _Layout, _LoginLayout, Partials (8 files)
│   │   └── ...                      # HR, Manager, Admin, Survey, AutoTests...
│   │
│   ├── Models/                      # ViewModels (10 files)
│   ├── wwwroot/                     # Static files (CSS, JS, uploads)
│   ├── Program.cs                   # Startup & DI configuration
│   ├── appsettings.json             # Configuration
│   └── README.md                    # (File này)
│
└── EmployeeSurvey.AdminApp/         # WPF Desktop Admin Application
    ├── Models/                      # DTO Models
    ├── Services/                    # API Client Services
    ├── ViewModels/                  # MVVM ViewModels
    ├── Views/                       # XAML Views
    ├── MainWindow.xaml              # Main UI Window
    └── App.xaml                     # Application Entry
```

---

## 5. Domain Models

### Sơ đồ quan hệ (Entity Relationship)

```
Department (1) ──── (N) Team (1) ──── (N) User
                                          │
                         ┌────────────────┤
                         │                │
                    Session (N)      Notification (N)
                    │       │
              ┌─────┘       └─────┐
              │                   │
         Test (1)            Feedback (N)
              │
         Assignment (N)
              
Question ──── (embedded in Test via QuestionIds/Items)
              ──── (snapshot in Session)

RolePermissionMapping ── RBAC (Admin, Staff, User)
SystemSettings ── Singleton config (logo, name)
PasswordReset ── Token-based password recovery
```

### Chi tiết các Entity chính

| Entity | Mô tả | Trường quan trọng |
|--------|--------|-------------------|
| **User** | Nhân viên | Id, Name, Email, Role, Level, Skill, TeamId, PasswordHash |
| **Question** | Câu hỏi (5 loại) | Content, Type (MCQ/TF/Essay/Matching/DragDrop), Options, CorrectKeys, Skill, Difficulty, Media |
| **Test** | Đề thi/Khảo sát | Title, Type (Test/Survey), DurationMinutes, PassScore, Items (điểm từng câu), TotalMaxScore |
| **Session** | Phiên làm bài | UserId, TestId, Status (Draft/Submitted/Graded), Answers, Snapshot, Scores, Timer |
| **Assignment** | Giao bài | TestId, TargetType (Role/Team/User), TargetValue, StartAt, EndAt |
| **Department** | Phòng ban | Name, Description |
| **Team** | Nhóm | Name, DepartmentId |
| **Feedback** | Phản hồi | SessionId, Content, Rating |
| **Result** | Kết quả | UserId, TestId, Score, MaxScore, Status |
| **RolePermissionMapping** | Phân quyền | Role, Permissions[] |

### Hệ thống phân quyền (RBAC)

```
Admin   → Reports.View, Reports.Export, Settings.Edit, Permissions.Manage, 
          Audit.View, Org.View, Org.Manage, Tests.View, Tests.Submit
Staff   → Reports.View, Reports.Export, Org.View, Tests.View
User    → Tests.View, Tests.Submit
```

---

## 6. Các chức năng đã phát triển

### ✅ Phase 1: Nền tảng hệ thống (Foundation)

- [x] Thiết lập dự án ASP.NET Core 8 MVC
- [x] Thiết kế Domain Models (17 entities)
- [x] Cấu hình EF Core + SQL Server (LocalDB) với Fluent API
- [x] Triển khai Generic Repository Pattern (`IRepository<T>`, `EfRepository<T>`)
- [x] Database Migrations (InitialCreate)
- [x] Seeder — dữ liệu mặc định (Admin, Staff, User, Department, Team, Questions, Test)
- [x] Dependency Injection cho toàn bộ services & repositories trong `Program.cs`

### ✅ Phase 2: Xác thực & Phân quyền (Authentication & Authorization)

- [x] Đăng nhập/Đăng xuất bằng Cookie Authentication (HttpOnly)
- [x] JWT Bearer Authentication cho API endpoints (WPF Admin App)
- [x] Mã hóa mật khẩu BCrypt
- [x] RBAC (Role-Based Access Control) với 3 role: Admin, Staff, User
- [x] Hệ thống Permission Codes (9 quyền) + PermissionService
- [x] Quên mật khẩu qua Email (PasswordResetService + SMTP)

### ✅ Phase 3: Quản lý tổ chức (Organization Management)

- [x] CRUD Phòng ban (Department) — tạo, sửa, xóa, xem danh sách
- [x] CRUD Nhóm (Team) — gán vào phòng ban
- [x] CRUD Người dùng (User) — tạo tài khoản, gán role, team
- [x] Quan hệ phân cấp: Department → Team → User

### ✅ Phase 4: Ngân hàng câu hỏi (Question Bank)

- [x] CRUD Câu hỏi với 5 loại: Trắc nghiệm (MCQ), Đúng/Sai (TrueFalse), Tự luận (Essay), Nối cặp (Matching), Kéo thả (DragDrop)
- [x] Phân loại theo Skill (C#, .NET, ASP.NET, Web...) và Difficulty (Junior, Mid, Senior)
- [x] Hỗ trợ Tags, Media files (ảnh, audio, video, PDF)
- [x] Import câu hỏi từ Excel (QuestionExcelService)
- [x] Export câu hỏi ra Excel
- [x] Audit trail (CreatedBy, UpdatedBy, timestamps)

### ✅ Phase 5: Quản lý đề thi & Khảo sát (Test & Survey Management)

- [x] Tạo đề thi thủ công — chọn câu hỏi, phân điểm (TestItem.Points)
- [x] Tự động sinh đề (TestGenerationService):
  - Generate chung cho nhóm
  - Generate cá nhân hóa (mỗi user 1 đề riêng theo Skill)
  - Generate + Assign tự động
- [x] Cấu hình: thời gian, điểm đậu, tổng điểm, shuffle câu hỏi
- [x] Phân biệt Test vs Survey (TestType enum)
- [x] Publish/Unpublish đề thi (IsPublished, PublishedAt)
- [x] Frozen Random Config — lưu cấu hình random khi publish

### ✅ Phase 6: Hệ thống làm bài (Test Taking)

- [x] Start Session — tạo phiên làm bài, snapshot câu hỏi
- [x] Submit Session — nộp bài, auto-scoring cho MCQ/TF
- [x] Timer — đếm ngược thời gian, pause/resume (ConsumedSeconds, TimerStartedAt)
- [x] Scoring: AutoScore (tự động) + ManualScore (chấm thủ công) = TotalScore
- [x] ScoringAllocator — phân bổ điểm tự động theo trọng số
- [x] Trạng thái: Draft → Submitted → Graded

### ✅ Phase 7: Giao bài & Thông báo (Assignment & Notification)

- [x] Assignment — giao bài theo Role, Team, hoặc User cụ thể
- [x] Thời hạn: StartAt — EndAt
- [x] AssignmentNotifyTarget — xác định đối tượng nhận bài
- [x] NotificationService — tạo thông báo in-app
- [x] SMTP Email Sender — gửi email thông báo

### ✅ Phase 8: Báo cáo & Xuất dữ liệu (Reports & Export)

- [x] HR Dashboard — tổng quan: số user, test, session, tỷ lệ pass
- [x] Báo cáo theo Role (GetRoleReportAsync)
- [x] Báo cáo theo Level (GetLevelReportAsync)
- [x] Thống kê năng lực cá nhân theo Skill (GetUserSkillReportAsync)
- [x] Xuất Excel (ClosedXML) — Role/Level Report, Skill Report
- [x] Xuất PDF (QuestPDF) — Role/Level Report, Skill Report
- [x] Export CSV nhanh (submissions gần nhất)

### ✅ Phase 9: Chấm bài & Phản hồi (Grading & Feedback)

- [x] Chấm bài tự luận thủ công (GradingController)
- [x] Feedback — nhân viên gửi phản hồi sau bài thi (rating + nội dung)
- [x] Quản lý feedback cho Admin/Staff

### ✅ Phase 10: Quản trị hệ thống (System Administration)

- [x] Cài đặt hệ thống (SystemSettings) — tên, logo
- [x] Audit Logging — ghi nhật ký các request POST/PUT/DELETE (middleware)
- [x] Audit Reader — xem & lọc nhật ký
- [x] Quản lý Profile cá nhân — đổi mật khẩu, cập nhật thông tin
- [x] Swagger UI cho API documentation (Development mode)

### ✅ Phase 11: WPF Admin Desktop App (Cơ bản)

- [x] Khung ứng dụng WPF + MVVM (App.xaml, MainWindow)
- [x] Models, Services, ViewModels cơ bản
- [x] Kết nối REST API qua JWT Authentication

### ✅ Phase 12: REST API cho Admin App

- [x] AdminUsersController — CRUD users qua API
- [x] AdminTestsController — CRUD tests qua API
- [x] AdminQuestionsController — CRUD questions qua API
- [x] AdminSessionsController — quản lý sessions qua API
- [x] AuthController (API) — login, lấy JWT token
- [x] DashboardApiController — thống kê cho WPF
- [x] AutoTestApiController — auto-generate qua API
- [x] ResultsController (API) — API kết quả
- [x] SessionsApiController — API phiên thi

---

## 7. Các chức năng cần phát triển tiếp

### 🔶 Phase 13: Dashboard & Trực quan hóa (ƯU TIÊN CAO)

- [ ] Biểu đồ thống kê trên Dashboard (Chart.js hoặc ApexCharts)
  - Phổ điểm (distribution chart)
  - Tỷ lệ hoàn thành theo thời gian (line chart)
  - So sánh kết quả giữa các phòng ban (bar chart)
  - Top nhân viên xuất sắc (leaderboard)
- [ ] Dashboard cá nhân cho nhân viên (lịch sử, tiến trình)
- [ ] Real-time statistics (SignalR hoặc polling)

### 🔶 Phase 14: Hoàn thiện WPF Admin App (ƯU TIÊN CAO)

- [ ] UI quản lý Users — CRUD hoàn chỉnh
- [ ] UI quản lý Tests — tạo/sửa đề thi
- [ ] UI quản lý Questions — duyệt/sửa câu hỏi
- [ ] UI xem Reports & Dashboard
- [ ] UI quản lý Sessions — theo dõi phiên thi
- [ ] Export từ WPF (gọi API export)

### 🔶 Phase 15: Nâng cao UX làm bài (ƯU TIÊN CAO)

- [ ] UI/UX cải tiến trang làm bài (progress bar, navigation panel)
- [ ] Auto-save đáp án (periodic save)
- [ ] Xác nhận trước khi nộp bài
- [ ] Review mode — xem lại bài sau khi nộp
- [ ] Hiển thị đáp án đúng sau khi chấm (tùy cấu hình)

### 🔷 Phase 16: Lập lịch & Nhắc nhở (ƯU TIÊN TRUNG BÌNH)

- [ ] Scheduler — lập lịch khảo sát định kỳ (Hangfire hoặc Background Service)
- [ ] Hệ thống Reminder — tự động gửi email nhắc nhở chưa hoàn thành
- [ ] Notification bell — thông báo real-time trên Web (SignalR)
- [ ] Email template đẹp (HTML email)

### 🔷 Phase 17: Quản lý nâng cao (ƯU TIÊN TRUNG BÌNH)

- [ ] Quản lý phiên bản câu hỏi (Question Versioning)
- [ ] Template đề thi — lưu & tái sử dụng bộ đề mẫu
- [ ] Workflow phê duyệt đề thi (Staff review → Admin approve)
- [ ] Bulk operations — thao tác hàng loạt (xóa, giao bài, đổi role)
- [ ] API Key Management — cấp quyền cho hệ thống bên ngoài

### 🔷 Phase 18: Bảo mật nâng cao (ƯU TIÊN TRUNG BÌNH)

- [ ] Rate Limiting cho Login/API (chống brute-force)
- [ ] Refresh Token cho JWT
- [ ] Multi-Factor Authentication (MFA) — OTP qua email
- [ ] Session management — giới hạn thiết bị đăng nhập
- [ ] CSRF protection nâng cao
- [ ] Input validation & Sanitization toàn diện

### ⬜ Phase 19: Chống gian lận (ƯU TIÊN THẤP)

- [ ] Lock screen khi làm bài (fullscreen mode)
- [ ] Phát hiện chuyển tab (focus detection)
- [ ] Randomize thứ tự đáp án (đã có shuffle question)
- [ ] IP tracking & device fingerprint
- [ ] Giám sát camera (nâng cao)

### ⬜ Phase 20: Tích hợp & Mở rộng (ƯU TIÊN THẤP)

- [ ] Responsive design (mobile-friendly)
- [ ] Đa ngôn ngữ (i18n — Tiếng Việt / English)
- [ ] Dark mode
- [ ] Backup & Restore database từ giao diện
- [ ] Import users từ Excel/CSV
- [ ] Tích hợp AI phân tích câu trả lời tự luận
- [ ] Lộ trình đào tạo (IDP — Individual Development Plan)

---

## 8. Quy trình phát triển & Timeline

### Tóm tắt tiến độ

| Phase | Nội dung | Trạng thái | Ưu tiên |
|-------|----------|------------|---------|
| 1 | Nền tảng hệ thống | ✅ Hoàn thành | — |
| 2 | Xác thực & Phân quyền | ✅ Hoàn thành | — |
| 3 | Quản lý tổ chức | ✅ Hoàn thành | — |
| 4 | Ngân hàng câu hỏi | ✅ Hoàn thành | — |
| 5 | Đề thi & Khảo sát | ✅ Hoàn thành | — |
| 6 | Hệ thống làm bài | ✅ Hoàn thành | — |
| 7 | Giao bài & Thông báo | ✅ Hoàn thành | — |
| 8 | Báo cáo & Xuất dữ liệu | ✅ Hoàn thành | — |
| 9 | Chấm bài & Phản hồi | ✅ Hoàn thành | — |
| 10 | Quản trị hệ thống | ✅ Hoàn thành | — |
| 11 | WPF Admin App (cơ bản) | ✅ Hoàn thành | — |
| 12 | REST API cho Admin | ✅ Hoàn thành | — |
| 13 | Dashboard & Trực quan hóa | ⬜ Chưa bắt đầu | 🔶 Cao |
| 14 | Hoàn thiện WPF Admin | ⬜ Chưa bắt đầu | 🔶 Cao |
| 15 | Nâng cao UX làm bài | ⬜ Chưa bắt đầu | 🔶 Cao |
| 16 | Lập lịch & Nhắc nhở | ⬜ Chưa bắt đầu | 🔷 Trung bình |
| 17 | Quản lý nâng cao | ⬜ Chưa bắt đầu | 🔷 Trung bình |
| 18 | Bảo mật nâng cao | ⬜ Chưa bắt đầu | 🔷 Trung bình |
| 19 | Chống gian lận | ⬜ Chưa bắt đầu | ⬜ Thấp |
| 20 | Tích hợp & Mở rộng | ⬜ Chưa bắt đầu | ⬜ Thấp |

### Quy trình phát triển đề xuất

```
1. Nhận yêu cầu → 2. Phân tích → 3. Thiết kế → 4. Lập trình → 5. Kiểm thử → 6. Review → 7. Deploy
```

### Đề xuất phân chia Sprint

| Sprint | Thời gian (ước lượng) | Nội dung |
|--------|----------------------|----------|
| Sprint 1 | 1-2 tuần | Phase 13: Dashboard + Charts |
| Sprint 2 | 2-3 tuần | Phase 14: WPF Admin hoàn chỉnh |
| Sprint 3 | 1-2 tuần | Phase 15: UX làm bài nâng cao |
| Sprint 4 | 2 tuần | Phase 16: Lập lịch & Nhắc nhở |
| Sprint 5 | 2-3 tuần | Phase 17: Quản lý nâng cao |
| Sprint 6 | 1-2 tuần | Phase 18: Bảo mật nâng cao |
| Sprint 7 | 1-2 tuần | Phase 19–20: Chống gian lận & Mở rộng |

> **Tổng ước lượng**: ~10-16 tuần cho toàn bộ Phase 13–20 (tùy số lượng thành viên).

---

## 9. Hướng dẫn cài đặt & chạy dự án

### Yêu cầu

- .NET 8.0 SDK
- SQL Server (hoặc LocalDB)
- Visual Studio 2022+ (khuyến nghị)

### Bước chạy

```bash
# 1. Clone repository
git clone <repository-url>
cd "Employee Survey"

# 2. Restore NuGet packages
dotnet restore

# 3. Cập nhật connection string trong appsettings.json (nếu cần)
# Mặc định: Server=(localdb)\\mssqllocaldb;Database=EmployeeSurveyDb

# 4. Chạy migration tạo database
cd "Employee Survey"
dotnet ef database update

# 5. Chạy ứng dụng
dotnet run

# Truy cập: https://localhost:7158
# Swagger: https://localhost:7158/swagger (Development mode)
```

### Chạy WPF Admin App

```bash
cd EmployeeSurvey.AdminApp
dotnet run
```

---

## 10. Tài khoản mặc định

Hệ thống tự động seed 3 tài khoản khi database trống:

| Vai trò | Email | Mật khẩu | Mô tả |
|---------|-------|-----------|-------|
| Admin | admin@local | admin123 | Quản trị viên — toàn quyền |
| Staff | staff@local | staff123 | HR/Quản lý — xem báo cáo, quản lý tổ chức |
| User | alice@local | alice123 | Nhân viên — làm bài, xem kết quả |

---

## 11. Cấu hình

### appsettings.json — Các mục cấu hình chính

| Key | Mô tả | Giá trị mặc định |
|-----|--------|-------------------|
| `ConnectionStrings:DefaultConnection` | Connection string SQL Server | `(localdb)\\mssqllocaldb` |
| `Jwt:Key` | JWT signing key (≥32 chars) | Auto-generated |
| `Jwt:Issuer` | JWT issuer | `EmployeeSurveyApi` |
| `Jwt:Audience` | JWT audience | `EmployeeSurveyClients` |
| `Jwt:ExpireMinutes` | JWT token lifetime | `1440` (24h) |
| `App:BaseUrl` | Base URL ứng dụng | `https://localhost:7158` |
| `Email:Host` | SMTP server | `smtp.gmail.com` |
| `Email:Port` | SMTP port | `587` |
| `MaxUploadFileSizeBytes` | Giới hạn upload | `10MB` |

> ⚠️ **Lưu ý**: Thay đổi `Jwt:Key` và `Email:Pass` trước khi deploy lên production.

---

*Cập nhật lần cuối: 24/02/2026*