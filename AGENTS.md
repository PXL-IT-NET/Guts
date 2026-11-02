# GUTS — Agent Guide

**GUTS** (**G**rowing towards a **U**nit **T**esting **S**trategy) is an educational platform developed at PXL University of Applied Sciences and Arts. It integrates automated testing into programming courses by collecting student and team test results from IDEs (Visual Studio, Rider, IntelliJ) and centralizing progress tracking, anti-tampering validation, exam scoring, and peer assessments in a central web portal and REST API.

For functional background, see the [GUTS Teacher Concepts Documentation](https://guts-web.pxl.be/teacher-docs/concepts) (source in `Backoffice/Guts.Web/ClientApp/src/app/components/teacher-docs-concepts/`).

---

## Core Domain Concepts

- **Courses & Periods**: Courses belong to academic periods (`PeriodAggregate`, `CourseAggregate`).
- **Topics & Assignments**:
  - **Individual Work (Chapters & Exercises)**: Courses contain _Chapters_ (`Chapter`), each containing one or more _Exercises_ (`Assignment`). Test results are accumulated per student per exercise.
  - **Team Work (Projects & Components)**: Courses can also contain _Projects_ (`Project`) split into _Components_ (`Assignment`). Students work in _Project Teams_ (`ProjectTeamAggregate`), and test results are accumulated per team per component.
- **Automatic Provisioning & Anti-Tampering**:
  - When a **teacher** runs monitored tests, missing chapters/projects, exercises/components, and tests (`TestAggregate`) are automatically provisioned in the Backoffice, and a cryptographic hash of the test code is registered.
  - When a **student** runs monitored tests, their test results (`TestRunAggregate`) and solution source files (`SolutionFile`) are sent to the Backoffice API and accepted only if the test code hash matches a valid teacher hash.
- **Peer Assessments & Exams**:
  - Teams evaluate peers via **Project Team Assessments** (`ProjectTeamAssessmentAggregate`, `AssessmentResultAggregate`), generating individual contribution factors and peer scores.

---

## Repository Structure

The repository is organized into three main areas:

1. **[`Backoffice/`](Backoffice/AGENTS.md)** — Central web application, REST API, and MCP server (see [`Backoffice/AGENTS.md`](Backoffice/AGENTS.md) for architecture and rules).
   - **Backend (.NET 10, EF Core)**: Layered DDD architecture (`Guts.Domain`, `Guts.Business`, `Guts.Infrastructure`, `Guts.Bootstrapper`, `Guts.Api`, `Guts.Common`) + MCP server in `Guts.Api/Mcp`.
   - **Frontend (`Guts.Web/ClientApp`)**: Angular SPA with Karma/Jasmine unit tests and Playwright E2E suites.
2. **`DotNetClient/`** — .NET client libraries (`DotNetClient/Guts.sln`) used in C# test projects:
   - `Guts.Client.Core`: HTTP communication, auth caching, code extraction (`Solution`), and code stripping (`CodeCleaner`).
   - `Guts.Client.NUnit` / `Guts.Client.XUnit`: Test framework extensions (`[ExerciseTestFixture]`, `[MonitoredTest]`, `[MonitoredFact]`).
   - `Guts.Client.WPF`: Helpers for testing WPF exercises (`FindVisualChildren<T>()`, `FireClickEvent()`).
   - Test projects: `Guts.Client.Core.Tests`, `Guts.Client.NUnit.Tests`, `Guts.Client.IntegrationTests`, etc.
3. **`JavaClients/`** — Gradle multi-project build (`JavaClients/settings.gradle`) for JVM test runners:
   - `guts-client-core`: Core engine for test result accumulation and hashing.
   - `guts-client-junit-5`: JUnit 5 Jupiter extension (`@GutsFixture` and test execution listener).
   - `guts-client-junit-5-example`: Sample exercise project demonstrating usage.

---

## Core Engineering Guidelines

1. **Code Quality & SOLID Principles**:
   - Produce modular, testable, and expandable code.
   - Favor dependency injection (constructor injection and narrow interfaces) over static state or tight coupling.
   - Design abstractions so new test runners, domain rules, or UI features can be added without modifying unrelated components.
2. **Keep Code DRY**:
   - Encapsulate shared behavior; avoid duplicating validation, DTO mapping, or assertions.
3. **Domain-Driven Design (DDD)**:
   - Encapsulate business rules, calculations, and state invariants inside **Aggregates, Entities, Value Objects, and Domain Factories** in `Guts.Domain`—never leak domain rules into controllers or repositories.
   - Enforce preconditions using `Contracts.Require(...)` from `Guts.Common`.
4. **Test-Driven Verification (TDD)**:
   - Always run relevant automated test suites after making changes and ensure all tests pass before completing a task.
   - When fixing a bug, write a failing reproduction test first.
5. **Frontend Testing Standards**:
   - Every Angular component, service, guard, or pipe must include unit tests (`*.spec.ts`) verified via Karma/Jasmine.
   - Integration journeys and user flows are verified with Playwright E2E tests (`e2e/tests/`).
6. **Code Quality & Security (SonarQube/SonarCloud)**:
   - Run quality analysis locally or via SonarCloud (`PXL-IT-NET_Guts`) and resolve all reported bugs, vulnerabilities, and code smells.

---

## Build & Test Commands

### Backoffice (.NET & Angular)

- **Backend tests (all)**:
  ```powershell
  dotnet test Backoffice/Guts.slnx
  ```
- **Backend tests (single project)**:
  ```powershell
  dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj
  dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj
  dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj
  dotnet test Backoffice/Guts.Common.Tests/Guts.Common.Tests.csproj
  ```
- **Frontend unit tests** (from `Backoffice/Guts.Web/ClientApp`):
  ```powershell
  npm test -- --watch=false
  ```
- **Frontend E2E tests (Playwright)** (from `Backoffice/Guts.Web/ClientApp`):
  ```powershell
  npm run test:e2e          # Headless run
  npm run test:e2e:ui       # Interactive UI mode
  npm run test:e2e:headed   # Headed run with visible browser
  ```
- **Frontend production build** (from `Backoffice/Guts.Web/ClientApp`):
  ```powershell
  npm run build
  ```
- **SonarQube local analysis** (from `Backoffice/`):
  ```powershell
  .\run-sonarqube.ps1 -UseSonarCloud
  ```

### .NET Client (`DotNetClient/`)

- **Build solution**:
  ```powershell
  dotnet build DotNetClient/Guts.sln
  ```
- **Run unit tests**:
  ```powershell
  dotnet test DotNetClient/Guts.Client.Core.Tests/Guts.Client.Core.Tests.csproj
  dotnet test DotNetClient/Guts.Client.NUnit.Tests/Guts.Client.NUnit.Tests.csproj
  ```

### Java Clients (`JavaClients/`)

- **Build & test** (from `JavaClients/`):
  ```powershell
  ./gradlew test
  ```
