# GUTS Backoffice — Agent Guide

This directory contains the central GUTS Backoffice backend (.NET REST API + MCP server) and frontend (Angular SPA).
For a high-level overview of the entire GUTS repository, see the root [`../AGENTS.md`](../AGENTS.md).

---

## Guiding Principles

1. **Code Quality & SOLID Principles**:
   - Produce **modular, testable, and expandable** code.
   - Keep controllers thin, application services focused on use-case orchestration, and domain models focused on invariants and business rules.
   - Inject narrow abstractions via constructor injection rather than instantiating dependencies directly.
2. **Domain-Driven Design (DDD)**:
   - Encapsulate business rules, calculations, state transitions, and invariant checks inside **Aggregates, Entities, Value Objects, and Domain Factories** in `Guts.Domain`—never in API controllers or application services.
   - Enforce domain preconditions and invariants using `Contracts.Require(...)` from `Guts.Common`.
   - Protect aggregate roots by exposing read-only collections (`IReadOnlyCollection<T>`) and mutating state strictly through domain methods.
   - Use explicit Foreign Key (FK) properties for EF Core; avoid navigation properties in domain classes unless directly required by domain logic.
3. **Spec-Driven Development (SDD)**:
   - For new features or significant changes, follow the 4-phase SDD workflow (`spec.md` ➔ `design.md` ➔ `tasks.md` ➔ inside-out TDD implementation) using templates in [`specs/_templates/`](specs/_templates/).
   - Detailed SDD instructions are located in the `backoffice-sdd` skill (`.agent/skills/backoffice-sdd/SKILL.md`).
4. **Test-Driven Development (TDD) & Quality Gate**:
   - Write unit tests before implementing new functionality or fixing bugs (write a failing reproduction test first).
   - Ensure all tests pass across `Guts.slnx` before considering tasks complete.
   - Verify code quality locally with `.\run-sonarqube.ps1 -UseSonarCloud` and resolve any reported issues.

---

## Solution Architecture (`Guts.slnx`)

The Backoffice backend follows a layered DDD architecture:

```
Guts.Api / Guts.Web
       │
Guts.Bootstrapper   ──► Auto-registers dependency injection by convention
       │
Guts.Business       ──► Application services, repository interfaces, DTOs, mappers
       │
Guts.Infrastructure ──► EF Core DbContext, migrations, repository implementations
       │
Guts.Domain         ──► Aggregates, Entities, Value Objects, Domain Factories (pure domain)
       │
Guts.Common         ──► Design-by-Contract primitives (Contracts.Require) & extensions
```

### Layer Responsibilities

- **`Guts.Common` (`Guts.Common.Tests`)**: Design-by-Contract primitives (`Contracts.Require`, `ContractException`), common extensions, and utilities.
- **`Guts.Domain` (`Guts.Domain.Tests`)**: Core business domain organized by aggregate folders.
  - Inherit from `AggregateRoot`, `Entity`, or `ValueObject<T>`.
  - Restrict construction to domain factories (`I*Factory`) to enforce valid initial states.
- **`Guts.Business` (`Guts.Business.Tests`)**: Application orchestration layer.
  - Coordinates repositories, domain factories, and domain entities to fulfill use cases.
  - Houses repository contracts (`Repositories/`), application services (`Services/`), and DTOs.
- **`Guts.Infrastructure`**: Persistence and external adapters.
  - EF Core `DbContext`, entity type configurations, and concrete repositories (`Repositories/`).
- **`Guts.Bootstrapper`**: Auto-registers services, repositories, and factories into DI using naming conventions (`StartUpExtensions.cs`).
- **`Guts.Api` (`Guts.Api.Tests`)**: Web API controllers, request/response models, and Model Context Protocol (`Mcp/`) resources and tools.
- **`Guts.Web` (`Guts.Web/ClientApp`)**: Frontend host and Angular SPA.
  - Code under `src/app/` is partitioned into `components/`, `services/`, `viewmodels/`, `guards/`, `interceptors/`, and `util/`.
  - E2E testing framework lives under `e2e/`.

---

## Testing Guidelines & Commands

### Backend Unit Tests

- Keep unit tests fast and isolated by mocking dependencies and repositories.
- Reuse or extend the test builder pattern (`Builders/`) and random test generators.
- Execute all backend tests:
  ```powershell
  dotnet test Backoffice/Guts.slnx
  ```
- Run a single backend test project:
  ```powershell
  dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj
  dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj
  dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj
  dotnet test Backoffice/Guts.Common.Tests/Guts.Common.Tests.csproj
  ```

### Frontend Unit Tests (Karma / Jasmine)

- Unit tests (`*.spec.ts`) live alongside the components, services, or guards they test.
- Mock dependencies via Angular `TestBed`.
- Run tests from `Backoffice/Guts.Web/ClientApp`:
  ```powershell
  npm test -- --watch=false
  ```

### Frontend E2E Tests (Playwright)

- End-to-end user journeys live under `Backoffice/Guts.Web/ClientApp/e2e/tests/`.
- Page Object Models live under `e2e/pages/`.
- Mock data and network intercept helpers live under `e2e/mocks/`.
- Use the authenticated role fixtures in `e2e/fixtures/auth.fixture.ts` (`teacherPage`, `studentPage`, `loginAs`) to test protected routes without manual UI login flows.
- Commands (from `Backoffice/Guts.Web/ClientApp`):
  ```powershell
  npm run test:e2e                       # Run all E2E tests headless
  npm run test:e2e:ui                    # Interactive UI mode with time-travel debugger
  npm run test:e2e:headed                # Run headed with visible browser window
  npm run test:e2e -- e2e/tests/file.ts  # Run a specific spec file
  npm run test:e2e:report                # View HTML test report
  ```
- If the frontend dev server is already running (e.g. on port 44400 or 4200), supply `PLAYWRIGHT_BASE_URL`:
  ```powershell
  $env:PLAYWRIGHT_BASE_URL="https://localhost:44400"; npm run test:e2e
  ```

### SonarQube / SonarCloud Analysis

- Run quality & security analysis from `Backoffice/` and resolve all reported bugs, vulnerabilities, and code smells. Also aim for 100% code coverage on new features and critical bug fixes:
  ```powershell
  .\run-sonarqube.ps1 -UseSonarCloud
  ```
- Or run against a local SonarQube container:
  ```powershell
  .\run-sonarqube.ps1 -StartServer
  ```
