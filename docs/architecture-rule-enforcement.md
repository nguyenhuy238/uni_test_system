# Architecture Rule Enforcement Guide

This document is the operational guide to enforce architecture rules in daily development and PR review.

## 1) Mandatory rules for new/modified code

From `UNITSYSTEM_SOFTWARE_QUALITY_RULEBOOK.yaml`:
- `MAIN_005`: new/modified controllers must not inject `IRepository<T>`.
- `MAIN_017`: presentation boundary uses `IEntityStore<T>` only as temporary migration adapter.
- `MAIN_018`: reports feature stays on `IReportsUseCaseService`.
- `MAIN_009`: presentation `ViewModel` classes stay in presentation projects.
- `MAIN_012`: repository contract must not expose `IQueryable`.
- `MAIN_013`: business workflow logic belongs in Application use-case services.
- `MAIN_014`: Application contracts use DTO/Command/Query/Result naming.
- `MAIN_015`: migrated features must use feature-level service interfaces.
- `MAIN_016`: Application + Web build verification is mandatory.

## 2) Required implementation pattern

For each migrated feature:
1. Create `I<Feature>...Service` interface in `UniTestSystem.Application/Interfaces`.
2. Implement service in `UniTestSystem.Application`.
3. Register DI in `AddApplicationServices()`.
4. Refactor controller to depend on service interface only.
5. Keep controller logic limited to:
   - request parsing
   - model validation
   - calling use-case service
   - mapping result to HTTP/View

For backlog controllers not yet split into feature services:
1. Replace direct `IRepository<T>` injection with `IEntityStore<T>` to keep presentation boundary clean.
2. Track each controller in migration backlog and schedule next feature-service extraction.
3. Do not add new workflow logic in controllers while using `IEntityStore<T>`.

## 3) Naming and placement rules

- `UniTestSystem.Application`:
  - allowed: `*Dto`, `*Command`, `*Query`, `*Result`, service interfaces/implementations.
  - prohibited: `*ViewModel`.
- `UniTestSystem` (Web):
  - MVC view models must be under `UniTestSystem/ViewModels`.
- Repository contract:
  - prohibited: `IQueryable` return types or `Query()` method.
  - required: explicit methods (`ListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `AnyAsync`) and specification support.

## 4) PR checklist (copy into PR description)

- [ ] No new `IRepository<T>` injection in modified controllers.
- [ ] No `IRepository<T>` injection remains in `UniTestSystem/Controllers`.
- [ ] Any `IEntityStore<T>` usage is accompanied by a migration ticket to feature use-case service.
- [ ] No `.Query()` usage introduced.
- [ ] No `*ViewModel*` file created under `UniTestSystem.Application`.
- [ ] Feature logic moved to Application use-case service.
- [ ] DI registration added/updated.
- [ ] Build passed:
  - [ ] `dotnet build UniTestSystem.Application/UniTestSystem.Application.csproj`
  - [ ] `dotnet build UniTestSystem/UniTestSystem.csproj`

## 5) Verification commands

```powershell
rg "IRepository<" UniTestSystem/Controllers -g "*.cs"
rg "IEntityStore<" UniTestSystem/Controllers -g "*.cs"
rg "\.Query\(\)" UniTestSystem UniTestSystem.Application UniTestSystem.Infrastructure -g "*.cs"
Get-ChildItem UniTestSystem.Application/Models -Recurse -Filter *ViewModel*.cs
dotnet build UniTestSystem.Application/UniTestSystem.Application.csproj
dotnet build UniTestSystem/UniTestSystem.csproj
```

## 6) Known temporary exceptions

- Legacy branches/older commits may still contain repository-injected controllers; current mainline target is 0.
- Controllers still on `IEntityStore<T>` are accepted only as temporary migration state.
- `UniTestSystem.AdminApp` solution build may intermittently fail due missing generated `obj/...*.g.cs`.
  - This does not block Web/Application architecture refactor PRs.
  - Web/Application project builds must still pass (`MAIN_016`).
