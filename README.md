# UniTestSystem - Hệ Thống Quản Lý Đào Tạo & Khảo Thí Toàn Diện

> **UniTestSystem** là một giải pháp chuyển đổi số toàn diện cho môi trường giáo dục đại học, tập trung vào quản lý cơ cấu tổ chức, học thuật và quy trình khảo thí tự động. Hệ thống được xây dựng trên nền tảng .NET 8 hiện đại, tuân thủ các nguyên tắc thiết kế phần mềm bền vững (Clean Architecture) và bảo mật doanh nghiệp.

---

## 📑 Mục lục
1. [Tổng quan dự án](#1-tổng-quan-dự-án)
2. [Mô hình nghiệp vụ & Bối cảnh Domain](#2-mô-hình-nghiệp-vụ--bối-cảnh-domain)
3. [Mô hình Vai trò & Phân quyền (RBAC)](#3-mô-hình-vai-trò--phân-quyền-rbac)
4. [Kiến trúc hệ thống](#4-kiến-trúc-hệ-thống)
5. [Xác thực & Ủy quyền](#5-xác-thực--ủy-quyền)
6. [Thiết kế Cơ sở dữ liệu](#6-thiết-kế-cơ-sở-dữ-liệu)
7. [Cấu trúc ứng dụng](#7-cấu-trúc-ứng-dụng)
8. [Quy trình nghiệp vụ cốt lõi](#8-quy-trình-nghiệp-vụ-cốt-lõi)
9. [Thiết kế Bảo mật](#9-thiết-kế-bảo-mật)
10. [Chiến lược Mở rộng & Sản xuất](#10-chiến-lược-mở-rộng--sản-xuất)
11. [Hướng dẫn triển khai](#11-hướng-dẫn-triển-khai)
12. [Lộ trình phát triển](#12-lộ-trình-phát-triển)

---

## 🎯 1. Tổng quan dự án

### Vấn đề giải quyết
Các cơ sở giáo dục thường gặp khó khăn trong việc quản lý ngân hàng câu hỏi tập trung, tổ chức thi cử khách quan và đồng bộ hóa kết quả học tập. **UniTestSystem** ra đời để:
- **Chuẩn hóa quy trình khảo thí**: Từ khâu soạn thảo câu hỏi đến khâu công bố bảng điểm.
- **Tự động hóa đánh giá**: Giảm thiểu sai sót con người trong chấm điểm và tính toán GPA.
- **Tăng cường bảo mật**: Đảm bảo tính minh bạch và chống gian lận trong thi cử.

### Đối tượng sử dụng
Hệ thống phục vụ 4 nhóm đối tượng chính: **Sinh viên**, **Giảng viên**, **Nhân viên Quản lý Học vụ (Staff)** và **Quản trị viên Hệ thống (IT Admin)**.

### Mô hình thực tế
Hệ thống mô phỏng chính xác cấu trúc phòng ban của một đại học hiện đại, với sự tách biệt rõ ràng giữa quản trị hạ tầng (IT) và quản trị học thuật (Phòng Đào tạo/Phòng Khảo thí).

---

## 💼 2. Mô hình nghiệp vụ & Bối cảnh Domain

Hệ thống xoay quanh các khái niệm cốt lõi:

- **Faculty (Khoa/Viện)**: Đơn vị quản lý cao nhất về chuyên môn (ví dụ: CNTT, Kinh tế).
- **StudentClass (Lớp sinh viên)**: Quản lý sinh viên theo khóa học và ngành học.
- **Course (Học phần)**: Định nghĩa môn học, số tín chỉ và yêu cầu đầu ra.
- **Enrollment (Đăng ký học)**: Liên kết sinh viên với các học phần cụ thể trong từng học kỳ.
- **Question Bank (Ngân hàng câu hỏi)**: Kho lưu trữ đa dạng (MCQ, Tự luận, Nối cặp, v.v.) có phân loại theo độ khó và kỹ năng.
- **Test Lifecycle**: Chu kỳ từ soạn đề, duyệt đề, xuất bản (Publish) đến thi và lưu trữ.
- **Session Tracking**: Theo dõi thời gian thực quá trình làm bài, lưu vết từng câu trả lời để ngăn ngừa mất dữ liệu.
- **Transcript & GPA**: Tự động tổng hợp điểm từ các bài thi để tính toán kết quả học tập kỳ và tích lũy.
- **ExamSchedule (Lịch thi)**: Quản lý ca thi, phòng thi và thời gian thi tập trung.

---

## 🔐 3. Mô hình Vai trò & Phân quyền (RBAC)

Hệ thống áp dụng mô hình phân quyền dựa trên quyền hạn (Permission-based) thay vì kiểm tra Role cứng, cho phép tùy chỉnh linh hoạt.

### Ma trận quyền hạn (Permission Matrix)

| Chức năng | Admin (IT) | Staff (Đào tạo) | Lecturer | Student |
| :--- | :---: | :---: | :---: | :---: |
| Quản trị Hệ thống (User/Role/Audit) | **Có** | Không | Không | Không |
| Quản lý Khoa/Lớp/Học phần | **Có** | **Có** | Xem | Không |
| Quản lý Ngân hàng câu hỏi | **Có** | Xem | **Có (Của mình)** | Không |
| Lên lịch thi/Khoá đề thi | **Có** | **Có** | Không | Không |
| Chấm bài / Review kết quả | **Có** | **Review** | **Chấm bài** | Xem |
| Làm bài thi | Không | Không | Không | **Có** |
| Thống kê/Báo cáo toàn trường | **Có** | **Có** | Xem (Lớp mình) | Không |

### Trách nhiệm và Ranh giới
- **Admin (System-level)**: Tập trung vào hạ tầng, phân quyền, cấu hình hệ thống, và bảo trì cơ sở dữ liệu. Admin không can thiệp vào chuyên môn giảng dạy.
- **Staff (Academic Affairs)**: Chịu trách nhiệm về "vận hành học thuật". Họ quản lý danh sách sinh viên, lớp học, lịch thi và giám sát tính tuân thủ của quy trình khảo thí.
- **Lecturer**: Chuyên gia nội dung. Họ xây dựng ngân hàng câu hỏi, thiết kế đề thi và thực hiện chấm điểm tự luận.
- **Student**: Người thụ hưởng. Thực hiện bài thi và theo dõi tiến độ học tập.

> [!IMPORTANT]
> Việc tách biệt Staff và Admin là cực kỳ quan trọng để đảm bảo **Phân tách trách nhiệm (SoC)**. Staff không có quyền can thiệp vào code hay cấu hình server, trong khi Admin không có quyền thay đổi điểm của sinh viên mà không có vết lưu lại.

---

## 🏗️ 4. Kiến trúc hệ thống

Dự án áp dụng mô hình **Clean Architecture** (Onion Architecture) với 4 lớp:

1.  **Domain (Core)**: Chứa Entities, Enums, và Logic nghiệp vụ nền tảng (Pure C#). Không phụ thuộc bất kỳ thư viện ngoài nào.
2.  **Application (Use Cases)**: Định nghĩa các Interface, DTOs, và Services xử lý luồng nghiệp vụ. Đây là nơi chứa logic "Test Generation" và "GPA Calculation".
3.  **Infrastructure (External)**: Triển khai Persistence (EF Core), Repository Pattern, Identity (BCrypt), và các Integration bên ngoài (Excel/PDF Services).
4.  **Presentation (API & UI)**:
    - **Web MVC**: Giao diện cho Sinh viên và Giảng viên.
    - **Web API**: Backend cho ứng dụng WPF.
    - **WPF AdminApp**: Công cụ quản trị Desktop mạnh mẽ cho Admin/Staff.

---

## 🔑 5. Xác thực & Ủy quyền

Hệ thống sử dụng cơ chế **Xác thực kép (Dual Auth strategy)**:

- **Cookie Authentication**: Dành cho giao diện Web, hỗ trợ bảo mật thông qua thuộc tính `HttpOnly` và `Secure`, ngăn chặn tấn công XSS.
- **JWT Bearer Token**: Dành cho kết nối từ WPF AdminApp đến Web API, cho phép giao tiếp không trạng thái (stateless) và bảo mật theo chuẩn hiện đại.
- **Policy-based Authorization**: Toàn bộ hệ thống được bảo vệ bởi các Policy ánh xạ trực tiếp đến `PermissionCodes`. Ví dụ: `[Authorize(Policy = PermissionCodes.Exam_Schedule)]`.
- **BCrypt**: Mật khẩu được băm (hash) bằng thuật toán BCrypt với Salt mạnh, đảm bảo an toàn trước các cuộc tấn công Brute-force/Rainbow Table.

---

## 📊 6. Thiết kế Cơ sở dữ liệu

Thiết kế DB đạt chuẩn chuẩn hóa để đảm bảo toàn vẹn dữ liệu:

- **Snapshot Mechanism**: Khi một sinh viên bắt đầu làm bài, hệ thống tạo một "bản chụp" của đề thi. Điều này đảm bảo kết quả không bị ảnh hưởng nếu ngân hàng câu hỏi gốc bị chỉnh sửa hoặc xóa sau đó.
- **Audit Trails**: Mọi thay đổi quan trọng (điểm số, gán quyền) đều được lưu vết: ai sửa, sửa lúc nào, giá trị cũ và mới.
- **Test Generation Model**: Hỗ trợ thuật toán chọn câu hỏi ngẫu nhiên dựa trên phân bố độ khó (Ví dụ: 30% Dễ, 50% Trung bình, 20% Khó).
- **Relational Integrity**: Sử dụng ràng buộc khoá ngoại nghiêm ngặt kết hợp với Fluent API để quản lý xoá (Cascade vs Restrict).

---

## 📂 7. Cấu trúc ứng dụng

```text
├── UniTestSystem.sln                  # Solution tổng thể
├── UniTestSystem/                     # Lớp Presentation & Backend chính
│   ├── Domain/                        # Core Entities & Domain Logic
│   ├── Application/                   # Service Layer & Interfaces
│   ├── Infrastructure/                # DBContext, Repositories, Migrations
│   ├── Controllers/                   # MVC & API Controllers
│   ├── Views/                         # Razor Pages cho Web
│   └── wwwroot/                       # Static assets (CSS, JS, Media)
└── UniTestSystem.AdminApp/            # Ứng dụng Desktop quản trị
    ├── Models/                        # DTOs ánh xạ từ API
    ├── ViewModels/                    # Logic xử lý giao diện (MVVM)
    ├── Services/                      # API Client & SignalR (nếu có)
    └── Views/                         # XAML Windows & UserControls
```

---

## 🔄 8. Quy trình nghiệp vụ cốt lõi

1.  **Thiết lập cấu trúc**: Admin/Staff tạo Khoa, Lớp và tài khoản người dùng hàng loạt qua Import Excel.
2.  **Xây dựng Ngân hàng**: Giảng viên soạn thảo câu hỏi. Staff có thể duyệt (Approve) câu hỏi để đưa vào sử dụng chính thức.
3.  **Tổ chức Kỳ thi**: Staff lên lịch thi (`ExamSchedule`), quy định thời gian bắt đầu và kết thúc cho từng lớp.
4.  **Thực hiện bài thi**: Sinh viên đăng nhập, làm bài. Hệ thống lưu câu trả lời tự động sau mỗi 30 giây (Auto-save).
5.  **Chuyển đổi kết quả**: Sau khi thi xong (hoặc giảng viên chấm xong bài tự luận), Staff thực hiện "Lock" kết quả để cập nhật vào bảng điểm chính thức (`Transcript`), ngăn chặn sửa đổi sau kỳ thi.

---

## 🛡️ 9. Thiết kế Bảo mật

Hệ thống được thiết kế theo nguyên lý **Defense in Depth**:
- **Anti-Cheat**: Chặn F12, Right-click và các thao tác chuyển tab trên trình duyệt trong lúc thi (Front-end bypass detection).
- **IDOR Prevention**: Kiểm tra quyền sở hữu Session bài thi ở mức Backend, đảm bảo Sinh viên A không thể xem bài của Sinh viên B qua ID.
- **Privilege Escalation**: Hệ thống kiểm tra vai trò người tạo yêu cầu (Requester) trước khi gán quyền mới, Admin chỉ có thể gán quyền trong phạm vi được định nghĩa sẵn.
- **Input Validation**: Sử dụng FluentValidation để làm sạch dữ liệu đầu vào, ngăn chặn SQL Injection và XSS.

---

## 📈 10. Chiến lược Mở rộng & Sản xuất

Để chuẩn bị cho quy mô 10,000+ sinh viên:
- **Horizontal Scaling**: Backend có thể triển khai trên nhiều Instance nhờ cơ chế Stateless JWT và Distributed Sessions (Redis).
- **Database Indexing**: Tối ưu hóa Index trên các cột tìm kiếm thường xuyên như `UserEmail`, `TestId`, `SessionStatus`.
- **Background Jobs**: Sử dụng Hangfire để xử lý các tác vụ nặng như tự động đóng ca thi quá hạn hoặc gửi thông báo điểm hàng loạt.
- **Media CDN**: Tải hình ảnh/video câu hỏi từ các Storage bên ngoài (Azure Blob/AWS S3) để giảm tải cho server chính.

---

## 🚀 11. Hướng dẫn triển khai

### Yêu cầu môi trường
- .NET 8.0 Runtime & SDK.
- SQL Server 2019+.
- IIS hoặc Kestrel (Production).

### Các bước cài đặt
1. **Clone & Restore**:
   ```bash
   git clone [repository-url]
   dotnet restore
   ```
2. **Cập nhật Database**:
   ```bash
   cd UniTestSystem
   dotnet ef database update
   ```
3. **Cấu hình**: Chỉnh sửa `appsettings.json` cho Connection String và JWT Secret Key.

### Tài khoản mặc định
- **Admin**: `admin@local` / `admin123`
- **Staff**: `staff@local` / `staff123`
- **Lecturer**: `lecturer@local` / `lecturer123`
- **Student**: `student@local` / `student123`

---

## 🗺️ 12. Lộ trình phát triển (Future Roadmap)

- [ ] **AI Proctoring**: Sử dụng Webcam và AI để phát hiện gian lận qua hành vi người dùng.
- [ ] **Mobile App**: Ứng dụng React Native dành cho Sinh viên làm bài và nhận thông báo.
- [ ] **Multi-Campus Support**: Hỗ trợ phân quyền theo cơ sở (Branch/Campus-level permissions).
- [ ] **Microservices Evolution**: Tách module "Test Engine" thành một dịch vụ riêng biệt để chịu tải tốt hơn trong thời điểm thi cao điểm.

---
© 2026 UniTestSystem Team. Kiến tạo tương lai số cho giáo dục.
