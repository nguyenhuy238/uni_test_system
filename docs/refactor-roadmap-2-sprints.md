# Refactor Roadmap - 2 Sprint

## Muc tieu
- Dua he thong ve dung huong Clean Architecture hon (Web chi orchestration, Application la use-case, Infrastructure la adapter).
- Giam coupling framework trong Application.
- Chuan hoa DI composition root de de mo rong va test.

## Sprint 1 (Uu tien cao nhat): Boundary + DI foundation
### Pham vi
1. Tach DI registration theo module:
   - `AddApplicationServices()`
   - `AddInfrastructureServices(configuration)`
2. Loai bo phu thuoc ASP.NET tu `Application`:
   - Doi `SessionDeviceGuardService` khong nhan `HttpRequest`.
   - Chuyen `PermissionRequirement/PermissionHandler` sang Web layer.
3. Bat dau doi controller tu repo truc tiep sang service layer:
   - Refactor `AdminCoursesController` su dung `IAcademicService`.

### DoD Sprint 1
- `Program.cs` khong con dang ky service theo danh sach dai.
- `UniTestSystem.Application` khong con reference ASP.NET Authorization/Http abstractions.
- Co it nhat 1 controller API theo huong Application service.

### Trang thai
- [x] Hoan thanh

## Sprint 2 (Uu tien tiep theo): Use-case first + transaction boundary
### Pham vi
1. Tiep tuc doi cac controller dang dung `IRepository<T>` truc tiep sang use-case service:
   - Nhom `Questions`, `Results`, `Classes`, `Users`, `Tests`.
2. Tach use-case lon:
   - Chia `TestsController` thanh cac service/use-case nho (Create/Edit/Assign/Publish).
   - Chia `ReportService` thanh module report rieng (dashboard, analytics, exports).
3. Chuan hoa transaction boundary:
   - Thiet ke `IUnitOfWork` (hoac SaveChanges boundary) de tranh `SaveChanges` moi method repository.
4. AdminApp:
   - Dua WPF app ve `HostBuilder` + DI + `IHttpClientFactory`.

### DoD Sprint 2
- Controller chi goi service/use-case.
- Giam kich thuoc class lon, tang kha nang test.
- Co transaction boundary ro rang.
- AdminApp co DI composition root dung chuan.

### Trang thai
- [ ] Chua bat dau
