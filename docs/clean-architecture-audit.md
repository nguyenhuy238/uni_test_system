# Clean Architecture Audit (Current State)

## 1) Snapshot (2026-03-25)

Current solution structure:
- `UniTestSystem.Domain`
- `UniTestSystem.Application`
- `UniTestSystem.Infrastructure`
- `UniTestSystem` (Web/Presentation)

Completed architectural changes:
- Repository contract no longer exposes `IQueryable` (`IRepository<T>.Query()` removed).
- Specification-based query mechanism is in place (`ISpecification<T>`, `Specification<T>`, `ListAsync(...)`).
- No remaining `.Query()` calls in codebase.
- `TestsController` migrated to use-case service (`ITestAdministrationService`).
- `UsersController` migrated to use-case service (`IUserAdministrationService`).
- `ReportsController` migrated to use-case service (`IReportsUseCaseService`).
- All remaining controllers no longer inject `IRepository<T>` directly (migrated to `IEntityStore<T>` transitional adapter).
- Presentation `*ViewModel*` classes moved from `UniTestSystem.Application/Models` to `UniTestSystem/ViewModels`.

## 2) What is fixed vs still open

Fixed:
- `IQueryable` leakage from repository contract.
- Feature-level controller-to-repository coupling for `Tests` and `Users`.
- Application-layer `*ViewModel*` naming and placement issues.

Still open:
- Many controllers still use transitional `IEntityStore<T>` and need feature-level use-case services.
- `ReportService` remains large and should be split by sub-domain use cases.
- Application still contains export implementation dependencies (`ClosedXML`, `QuestPDF`) that should eventually move behind pure contracts to Infrastructure.

## 3) Enforced rules source of truth

All new architecture rules are formalized in:
- `UNITSYSTEM_SOFTWARE_QUALITY_RULEBOOK.yaml` (v1.3.0)

Key rule IDs for current refactor direction:
- `MAIN_005`: no direct repository injection in new/modified controllers.
- `MAIN_017`: no `IRepository<T>` at presentation boundary; `IEntityStore<T>` is transitional only.
- `MAIN_018`: `ReportsController` must stay on `IReportsUseCaseService`.
- `MAIN_009`: Presentation `ViewModel` classes stay in presentation projects.
- `MAIN_012`: repository contract must not expose `IQueryable`.
- `MAIN_013`: business workflow logic belongs in Application use-case services.
- `MAIN_014`: Application contracts use DTO/Command/Query/Result naming.
- `MAIN_015`: migrated features must use feature-level service interfaces.
- `MAIN_016`: PR must pass Application + Web project builds.

## 4) PR guard commands

Run these checks before merging architecture-related changes:

```powershell
rg "IRepository<" UniTestSystem/Controllers -g "*.cs"
rg "IEntityStore<" UniTestSystem/Controllers -g "*.cs"
rg "\.Query\(\)" UniTestSystem UniTestSystem.Application UniTestSystem.Infrastructure -g "*.cs"
Get-ChildItem UniTestSystem.Application/Models -Recurse -Filter *ViewModel*.cs
dotnet build UniTestSystem.Application/UniTestSystem.Application.csproj
dotnet build UniTestSystem/UniTestSystem.csproj
```

Expected result:
- first command returns no results
- second command returns migration backlog controllers still on transitional store (to be moved to use-case services)
- third/fourth commands return no results
- both build commands succeed

## 5) Next migration targets

Recommended next order:
1. `TranscriptsController` use-case service
2. `QuestionsController` use-case service
3. `SessionsController` / `TestApiController` use-case service
4. Remaining `Api/Admin/*` feature services
5. Split `ReportService` into smaller report modules
