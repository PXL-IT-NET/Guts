# GUTS Backoffice — Agent Guide

This directory contains the central GUTS Backoffice backend (.NET REST API + MCP server) and frontend (Angular SPA).
For a high-level overview of the entire GUTS repository and client libraries, see the root [`../Agents.md`](../Agents.md).

---

## Guiding Principles

1. **Code Quality & SOLID Principles**:
   - Produce **modular, testable, and expandable** code that respects the **SOLID** principles:
     - **Single Responsibility**: Keep controllers thin, application services focused on use-case orchestration, and domain classes focused on business logic and invariants.
     - **Open/Closed & Liskov Substitution**: Program against interfaces so behavior can be extended or substituted cleanly.
     - **Interface Segregation & Dependency Inversion**: Inject narrow abstractions via constructor injection rather than instantiating concrete dependencies directly.
2. **Domain-Driven Design (DDD)**:
   - Strive to apply **DDD principles** throughout the Backoffice backend.
   - Encapsulate business rules, calculations, state transitions, and invariant checks inside **Aggregates, Entities, Value Objects, and Domain Factories** in `Guts.Domain`—not in API controllers or application services.
   - Enforce preconditions and domain invariants using `Contracts.Require(...)` from `Guts.Common`.
   - It is acceptable to use properties that serve as a Foreign Key (FK) property for EF Core. Avoid using navigation properties in domain classes if they are not used in the domain logic.
3. **Use Test-Driven Development (TDD)**:
   - Write unit tests before implementing new functionality or fixing bugs.
   - Ensure that all new code is covered by unit tests, and that existing tests pass after changes.
4. **Verify Changes by Running Tests**:
   - Always verify backend changes by running `dotnet test` across `Guts.slnx` and ensuring all tests pass.
   - Add or update unit tests in the corresponding `*.Tests` project whenever introducing or modifying backend functionality.
5. **Write Tests for Frontend Functionality**:
   - Changes to the Angular SPA (`Guts.Web/ClientApp`) must include unit tests (`*.spec.ts`) covering component behavior, user interactions, and service logic.
   - Verify frontend changes by running `npm test -- --watch=false` inside `Guts.Web/ClientApp`.
6. **Fix bugs by writing a failing test first**:
   - When fixing a bug, first write a unit test that reproduces the bug, then implement the fix and verify that the test passes.
7. **Build, Test, and SonarQube Quality Checks**:
   - The solution must build cleanly, and all tests must pass.
   - Start the SonarQube container, run Sonar checks, and fix any reported issues or code smells before completing the task.

---

## Solution Architecture (`Guts.slnx`)

The Backoffice backend follows a layered architecture:

```
Guts.Api / Guts.Web
       │
Guts.Bootstrapper   ──► Wires dependency injection across layers
       │
Guts.Business       ──► Application services, repository contracts, DTOs, converters
       │
Guts.Infrastructure ──► EF Core DbContext, migrations, repository implementations, external adapters
       │
Guts.Domain         ──► Aggregates, Entities, Value Objects, Domain Factories (pure domain)
       │
Guts.Common         ──► Design-by-Contract utilities & shared extensions
```

### Layer Responsibilities

- **`Guts.Common` (`Guts.Common.Tests`)**: Cross-cutting utilities, shared extensions, and Design-by-Contract primitives (`Contracts.Require`, `ContractException`).
- **`Guts.Domain` (`Guts.Domain.Tests`)**: Core domain model organized by aggregate folders.
  - Inherit from `AggregateRoot`, `Entity`, or `ValueObject<T>`.
  - Protect domain state by exposing read-only collections and mutating state through explicit domain methods.
  - Enforce valid object creation via domain factories (`I*Factory`) and restricted constructors.
- **`Guts.Business` (`Guts.Business.Tests`)**: Application and business orchestration layer.
  - Coordinates repositories, domain factories, and domain entities to fulfill use cases.
  - Defines repository interfaces (`Repositories/`), application services (`Services/`), DTOs, converters, and external communication abstractions.
- **`Guts.Infrastructure`**: Data persistence and infrastructure concerns.
  - Contains the EF Core `DbContext`, entity configurations, migrations, and concrete database repositories (`Repositories/`).
- **`Guts.Bootstrapper`**: Dependency injection setup (`StartUpExtensions.cs`).
  - Uses naming conventions to auto-register services, repositories, factories, converters, and loaders across layers. Follow the existing naming and interface conventions when introducing new components so they are registered automatically.
- **`Guts.Api` (`Guts.Api.Tests`)**: REST API and MCP server.
  - Contains Web API controllers, request/response models, model mappers/converters, and Model Context Protocol (`Mcp/`) resources and prompts.
- **`Guts.Web` (`Guts.Web/ClientApp`)**: Frontend host and Angular SPA.
  - Organizes frontend code under `src/app/` into `components/`, `services/`, `viewmodels/`, `guards/`, `interceptors/`, `directives/`, and `util/`.

---

## Testing Guidelines

### Backend Unit Tests

- Keep unit tests fast and isolated by mocking external dependencies and repositories.
- Reuse or extend the test builder pattern (`Builders/`) and random value generators (`RandomExtensions`) to construct test data without brittle hardcoded values.
- Name test methods clearly to express the scenario and expected outcome.
- Run all backend tests from the repository root:
  ```powershell
  dotnet test Backoffice/Guts.slnx
  ```

### Frontend Unit Tests

- Place unit tests (`*.spec.ts`) alongside the Angular components, services, guards, or pipes they test.
- Mock service dependencies using Angular's `TestBed` so components and services are tested in isolation.
- Run all frontend tests from `Backoffice/Guts.Web/ClientApp`:
  ```powershell
  npm test -- --watch=false
  ```

### SonarQube Quality Analysis

- Start the SonarQube container (if not already running) and execute code quality & coverage analysis from `Backoffice/`:
  ```powershell
  .\run-sonarqube.ps1 -StartServer
  ```
- Review the analysis results and resolve any reported bugs, vulnerabilities, security hotspots, or code smells.
