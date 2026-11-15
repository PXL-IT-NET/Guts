# TDD Implementation Plan: [Feature Title]

- **Feature ID**: `NNN-feature-slug`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [ ] **[T1.1]** `[AC-1, DR-1]` Create/update test builder `[Entity]Builder` in `Guts.Domain.Tests/Builders/` if needed.
- [ ] **[T1.2]** `[AC-1, DR-1]` Write failing unit tests in `Guts.Domain.Tests/[AggregateFolder]/[Entity]Tests.cs` covering valid state transitions and `ContractException` invariant checks.
- [ ] **[T1.3]** `[AC-1, DR-1]` Implement domain logic and `Contracts.Require(...)` checks in `Guts.Domain/[AggregateFolder]/[Entity].cs` and `[Entity]Factory.cs`.
- [ ] **[T1.4]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [ ] **[T2.1]** `[AC-1]` Define/update repository contract `I[Entity]Repository` and service interface `I[Feature]Service`.
- [ ] **[T2.2]** `[AC-1]` Write failing unit tests in `Guts.Application.Tests/Services/[Feature]ServiceTests.cs` (and converter tests if applicable) using mocked repositories/factories.
- [ ] **[T2.3]** `[AC-1]` Implement `[Feature]Service` (and DTOs/converters) in `Guts.Application/`.
- [ ] **[T2.4]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [ ] **[T3.1]** `[AC-1]` Update EF Core configuration in `Guts.Infrastructure/` and add migration (if schema changed).
- [ ] **[T3.2]** `[AC-1]` Write/update repository tests in `Guts.Infrastructure.Tests/` (if custom queries are introduced).
- [ ] **[T3.3]** `[AC-1]` Implement `[Entity]DatabaseRepository` methods in `Guts.Infrastructure/Repositories/`.
- [ ] **[T3.4]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API & MCP Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [ ] **[T4.1]** `[AC-1, AC-2]` Write failing unit tests for controller action(s) and model converters in `Guts.Api.Tests/Controllers/[Name]ControllerTests.cs`.
- [ ] **[T4.2]** `[AC-1, AC-2]` Implement request/response models, converters, and controller endpoint(s) in `Guts.Api/`.
- [ ] **[T4.3]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`) _(if applicable)_

- [ ] **[T5.1]** `[AC-1]` Write failing Jasmine unit tests (`*.spec.ts`) covering as many lines and branches as possible for every new or modified Angular component, service, guard, viewmodel helper, and utility.
- [ ] **[T5.2]** `[AC-1]` Implement Angular service methods, viewmodels, guards, and component template/logic in `Guts.Web/ClientApp/src/app/`.
- [ ] **[T5.3]** `[AC-1]` Add/update Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/` covering all new and updated user workflows.
- [ ] **[T5.4]** Run `npm run lint`, `npm test -- --watch=false`, `npm run build`, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp` and verify all pass.

---

## Phase 6: Final Verification & Quality Gate

- [ ] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [ ] **[T6.2]** Run frontend production build, lint, unit tests, and relevant Playwright E2E tests (if frontend was modified).
- [ ] **[T6.3]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and inspect per-file new-code coverage; add missing tests for uncovered lines/branches across backend and frontend until Quality Gate is `OK` (with maximum achievable new-code line & branch coverage, `0` open issues, and `0` unreviewed hotspots).
- [ ] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
