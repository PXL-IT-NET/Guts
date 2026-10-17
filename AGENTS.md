# GUTS — Agent Guide

**GUTS** (**G**rowing towards a **U**nit **T**esting **S**trategy) is an educational platform developed at PXL University of Applied Sciences and Arts. It integrates automated testing into programming courses by collecting student and team test results from IDEs (Visual Studio, Rider, IntelliJ) and centralizing progress tracking, anti-tampering validation, exam scoring, and peer assessments in a central web portal and REST API.

For functional background, see the [GUTS Teacher Concepts Documentation](https://guts-web.pxl.be/teacher-docs/concepts) (source located in `Backoffice/Guts.Web/ClientApp/src/app/components/teacher-docs-concepts/`).

---

## Core Domain Concepts

- **Courses & Periods**: Courses are organized across academic periods (`PeriodAggregate`, `CourseAggregate`).
- **Topics & Assignments**:
  - **Individual Work (Chapters & Exercises)**: Courses contain _Chapters_ (`Chapter`), each containing one or more _Exercises_ (`Assignment`). Test results are accumulated per student per exercise.
  - **Team Work (Projects & Components)**: Courses can also contain _Projects_ (`Project`) split into _Components_ (`Assignment`). Students work in _Project Teams_ (`ProjectTeamAggregate`), and test results are accumulated per team per component.
- **Automatic Provisioning & Anti-Tampering**:
  - When a **teacher** runs monitored tests, missing chapters/projects, exercises/components, and tests (`TestAggregate`) are automatically created in the Backoffice, and a hash of the test code is registered.
  - When a **student** runs monitored tests, their test results (`TestRunAggregate`) and solution source files (`SolutionFile`) are sent to the Backoffice API and accepted if the test code hash matches a valid teacher hash.
- **Peer Assessments & Exams**:
  - Teams evaluate each other via **Project Team Assessments** (`ProjectTeamAssessmentAggregate`, `AssessmentResultAggregate`), yielding peer scores alongside self-evaluation.

---

## Repository Structure

This repository is split into three top-level areas:

1. **[`Backoffice/`](Backoffice/Agents.md)** — Central web application and REST API (see [`Backoffice/Agents.md`](Backoffice/Agents.md) for detailed architecture and rules).
   - **Backend (ASP.NET, EF Core)**: Layered DDD architecture (`Guts.Domain`, `Guts.Business`, `Guts.Infrastructure`, `Guts.Bootstrapper`, `Guts.Api`, `Guts.Common`) + MCP server resources/prompts in `Guts.Api/Mcp`.
   - **Frontend (`Guts.Web/ClientApp`)**: Angular Single-Page Application.
2. **`DotNetClient/`** — .NET NuGet client libraries (`DotNetClient/Guts.sln`) used inside student and teacher C# test projects (e.g., from Visual Studio):
   - `Guts.Client.Core`: Core HTTP communication (`HttpClient`), authentication token retrieval/caching, solution source code extraction (`Solution`), code stripping (`CodeCleaner`), and test result reporting.
   - `Guts.Client.NUnit`: NUnit extensions (`[ExerciseTestFixture]`, `[ProjectComponentTestFixture]`, `[MonitoredTest]`).
   - `Guts.Client.XUnit`: xUnit extensions (`[ExerciseTestClass]`, `[ProjectComponentTestClass]`, `[MonitoredFact]`, `[MonitoredTheory]`).
   - `Guts.Client.WPF`: Helpers and visual-tree extension methods (`FindVisualChildren<T>()`, `FireClickEvent()`) for testing WPF exercises.
   - Unit and integration test projects: `Guts.Client.Core.Tests`, `Guts.Client.NUnit.Tests`, `Guts.Client.IntegrationTests`, `Guts.Client.NUnit.IntegrationTests`, `Guts.Client.XUnit.IntegrationTests`.
3. **`JavaClients/`** — Gradle multi-project build (`JavaClients/settings.gradle`) for accumulating and sending test results from IntelliJ / Maven / Gradle projects:
   - `guts-client-core`: Test-engine-independent core library for collecting results, hashing test code, reading `guts.json`, and communicating with the GUTS API.
   - `guts-client-junit-5`: JUnit 5 (Jupiter) extension (`@GutsFixture` annotation and test execution listener).
   - `guts-client-junit-5-example`: Sample JUnit 5 exercise test project demonstrating usage.

---

## Core Engineering Guidelines

1. **Code Quality & SOLID Principles**:
   - Produce modular, testable, and expandable code that respects the **SOLID** principles.
   - Favor dependency injection (via constructor injection and narrow interfaces) over static state or tight coupling.
   - Keep classes focused on a single responsibility and design abstractions so new test runners, domain rules, or UI features can be added without modifying unrelated components.
2. **Keep code DRY**:
   - Avoid duplicating logic. If a new feature requires changes in multiple areas, consider encapsulating the common behavior.
3. **Domain-Driven Design (DDD) in the Backoffice**:
   - Strive to apply DDD principles in the Backoffice API and domain model. Encapsulate business rules and invariants inside Aggregates, Entities, Value Objects, and Domain Factories in `Guts.Domain` rather than leaking domain logic into controllers or data repositories.
   - Read [`Backoffice/Agents.md`](Backoffice/Agents.md) before making any changes to `Backoffice/`.
4. **Verify Changes by Running Tests**:
   - Always run the relevant automated test suite(s) after making changes and ensure all tests pass before considering a task complete.
   - Add or update unit tests whenever introducing new features, fixing bugs, or changing domain/business behavior.
5. **Test Frontend Functionality**:
   - Every new or modified Angular component, service, guard, or pipe in `Backoffice/Guts.Web/ClientApp` must include corresponding unit tests (`*.spec.ts`) verified via Karma/Jasmine.

---

## Build & Test Commands

### Backoffice (.NET & Angular)

- **Build & run all backend unit tests**:
  ```powershell
  dotnet test Backoffice/Guts.slnx
  ```
- **Run a single backend test project**:
  ```powershell
  dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj
  dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj
  dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj
  dotnet test Backoffice/Guts.Common.Tests/Guts.Common.Tests.csproj
  ```
- **Run Angular frontend tests** (from `Backoffice/Guts.Web/ClientApp`):
  ```powershell
  npm test -- --watch=false
  ```
- **Build Angular frontend** (from `Backoffice/Guts.Web/ClientApp`):
  ```powershell
  npm run build
  ```

### .NET Client (`DotNetClient/`)

- **Build the solution**:
  ```powershell
  dotnet build DotNetClient/Guts.sln
  ```
- **Run unit tests** (avoid integration test projects that require a live local backend unless testing full end-to-end flows):
  ```powershell
  dotnet test DotNetClient/Guts.Client.Core.Tests/Guts.Client.Core.Tests.csproj
  dotnet test DotNetClient/Guts.Client.NUnit.Tests/Guts.Client.NUnit.Tests.csproj
  ```

### Java Clients (`JavaClients/`)

- **Build & test with Gradle** (from `JavaClients/`):
  ```powershell
  ./gradlew test
  ```
