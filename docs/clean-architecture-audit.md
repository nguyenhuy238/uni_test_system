# Clean Architecture Audit (Current State)

## 1) Current status summary

The solution already has 4 projects that match Clean Architecture layers:

- `UniTestSystem.Domain`
- `UniTestSystem.Application`
- `UniTestSystem.Infrastructure`
- `UniTestSystem` (Presentation/Web)

However, boundaries are still mixed in several places.

## 2) Main boundary issues found

- `Application` still depends on framework/data access details (`Microsoft.EntityFrameworkCore`) and document/export libs (`ClosedXML`, `QuestPDF`).
- `IRepository<T>` exposes `IQueryable<T>`, leaking query/infrastructure concerns into use-case and presentation layers.
- Many controllers in `UniTestSystem/Controllers` still inject `IRepository<T>` directly instead of use-case services.
- Some classes are oversized and bundle multiple responsibilities:
  - `UniTestSystem.Application/ReportService.cs` (~580 lines)
  - `UniTestSystem/Controllers/TestsController.cs` (~837 lines)
- Runtime artifacts and local operational files were tracked in source tree (logs/build dumps/backups).

## 3) Cleanup done in this pass

- Removed tracked build/error artifacts in repo root and `UniTestSystem/`.
- Removed tracked database backup file in `UniTestSystem/App_Data/Backups/`.
- Updated `.gitignore` to block runtime/build artifacts from being re-tracked.
- Reduced direct Infrastructure coupling in `AdminController` by introducing:
  - `ISystemMaintenanceService` (Application abstraction)
  - `SystemMaintenanceService` (Infrastructure implementation)
- Renamed confusing maintenance API in `Seeder`:
  - `ResetDatabaseAsync(...)` as the main method
  - old `ResetAllJsonFilesAsync(...)` kept as obsolete compatibility wrapper
- Replaced placeholder domain class intent (`Class1`) with a meaningful marker type.

## 4) Recommended target folder structure

### `UniTestSystem.Domain`

- `Entities/`
- `ValueObjects/`
- `Enums/`
- `DomainServices/`
- `Events/`
- `Exceptions/`

### `UniTestSystem.Application`

- `Abstractions/`
  - `Persistence/` (repositories, unit of work, query ports)
  - `Integrations/` (email, file storage, external gateways)
- `UseCases/`
  - `<Feature>/Commands/`
  - `<Feature>/Queries/`
  - `<Feature>/Handlers/`
- `DTOs/`
- `Mappings/`
- `Validation/`
- `DependencyInjection/`

### `UniTestSystem.Infrastructure`

- `Persistence/`
  - `DbContext/`
  - `Configurations/`
  - `Repositories/`
  - `Migrations/`
- `Integrations/`
  - `Email/`
  - `Export/`
  - `Storage/`
- `Security/`
- `DependencyInjection/`

### `UniTestSystem` (Web/Presentation)

- `Controllers/`
  - `Mvc/`
  - `Api/Admin/`
  - `Api/User/`
- `ViewModels/` (move UI-specific models from Application to here)
- `Views/`
- `Authorization/`
- `Middleware/`
- `Extensions/`

## 5) Priority refactor order (safe and incremental)

1. Stop controller-to-repository usage:
   - Create use-case services per feature and move query/write logic out of controllers.
2. Split giant classes:
   - break `TestsController` and `ReportService` into focused use cases.
3. Remove `IQueryable<T>` from `IRepository<T>`:
   - replace with explicit query methods or specification/query objects.
4. Push infra libraries out of Application:
   - keep `ClosedXML`/`QuestPDF` implementations in Infrastructure behind interfaces.
5. Move UI models:
   - relocate `Application/Models/*ViewModel*` into Web `ViewModels/`.
6. Add tests:
   - at minimum `Application` unit tests for core use-cases.
