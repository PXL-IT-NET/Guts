---
name: ddd-deep-modeling
description: Eric Evans Domain-Driven Design (DDD) deep modeling, breakthrough discovery, and domain logic leakage refactoring skill for GUTS Backoffice. Activate whenever auditing domain models, hunting for domain breakthroughs, detecting leaked domain logic in Guts.Application or Guts.Api, or refactoring anemic aggregates into rich domain models.
---

# GUTS Backoffice — DDD Deep Modeling & Breakthrough Skill

Use this skill to evolve the GUTS Backoffice domain model (`Guts.Domain`) toward deeper insight (Eric Evans, *Domain-Driven Design*, Part III: *Refactoring Toward Deeper Insight*) and to eliminate domain logic that has leaked into the Application Layer (`Guts.Application`) or API Layer (`Guts.Api`).

---

## Core Architectural Mental Model

| Layer | Project | True Role (Evans DDD) | What MUST NOT Live Here |
| :--- | :--- | :--- | :--- |
| **Domain Layer** | `Guts.Domain` | Heart of the software. Houses Aggregates, Entities, Value Objects, Domain Services, Specifications/Policies, and Domain Factories. Encapsulates **all** business rules, calculations, and state invariants. | Persistence (`DbContext`), HTTP/MVC types, external I/O, DTO mapping. |
| **Application Layer** | `Guts.Application` | Thin Use-Case Orchestration Layer. Coordinates repositories, domain factories, and domain objects to fulfill application use cases. | Business rules, domain calculations, eligibility `if/else` trees, direct collection mutations (`entity.Items.Add(...)`), or domain invariant checks. |
| **Infrastructure Layer** | `Guts.Infrastructure` | Technical adapters (EF Core `GutsContext`, `IEntityTypeConfiguration<T>`, concrete `*DbRepository` classes, file/mail/logging adapters). | Domain rules or business branching outside query translation. |
| **API / Presentation Layer** | `Guts.Api` | HTTP Controllers, request/response models, Model Converters, and MCP tools. | Domain calculations, state mutations, or cross-aggregate business rules. |

---

## Two-Stage Workflow

Always follow this **two-stage interactive workflow** unless the user explicitly asks to skip straight to execution:

### Stage 1: Audit & Domain Breakthrough Proposal
Scan the target bounded context / aggregate / application service and present a **DDD Breakthrough & Leakage Report** organized around the four lenses below. Pause for user confirmation before refactoring.

#### Lens 1: Leaked Domain Logic in `Guts.Application` or `Guts.Api`
Hunt for domain rules masquerading as application or controller logic:
- **Symptom A — Procedural validation & state mutation in Application Services**:
  - *Anti-pattern*: An application service inspects an aggregate's internal state and mutates its properties/collections directly (e.g., checking `if (assignment.TestCodeHashes.Any(...))` and calling `assignment.TestCodeHashes.Add(...)` in `AssignmentService`, or checking duplicate team names in `ProjectService`).
  - *Fix*: Move the rule into an intention-revealing method on the Aggregate Root (`assignment.ValidateOrRegisterTestCodeHash(hash, isLector)`, `project.AddTeam(teamName)`).
- **Symptom B — Domain calculations in Application Services or API Converters**:
  - *Anti-pattern*: Multi-step LINQ aggregations, score/XP calculations, ranking algorithms, or date/period eligibility checks inside `*Service` or `*Converter` classes.
  - *Fix*: Move single-aggregate calculations onto the Aggregate/Entity, and multi-aggregate calculations into a pure, stateless **Domain Service** or **Policy** in `Guts.Domain`.
- **Symptom C — `Contracts.Require(...)` checking domain invariants in Application Services**:
  - *Anti-pattern*: `Guts.Application` services enforcing business preconditions that actually belong to the domain concept itself.
  - *Fix*: Push `Contracts.Require(...)` down into the Domain Aggregate method, Value Object constructor, or Domain Factory.

#### Lens 2: Making Implicit Concepts Explicit (Eric Evans, Chapter 9)
Look for hidden domain concepts buried in procedural code, primitive parameters, or comments, and give them a first-class type in the **Ubiquitous Language**:
1. **Explicit Value Objects (`ValueObject<T>`)**:
   - Look for primitive obsession: strings with format rules (`Code`, `TestCodeHash`), paired integers (`int year, int weekNumber` ➔ `IsoWeek`), bounded scores/percentages/factors (`ContributionFactor`, `AssessmentScore`), or date ranges (`DateRange`).
   - Value Objects must be **immutable** (no public setters) and encapsulate their own validation and side-effect-free operations.
2. **Explicit Specifications & Policies**:
   - Look for complex boolean expressions or eligibility rules repeated or buried in services (e.g., whether a student is eligible for rank defense, whether a streak is at risk, whether a test run is valid).
   - Extract them into named policy/specification methods or types in `Guts.Domain`.
3. **Explicit Processes / Pure Domain Services**:
   - When a domain rule coordinates multiple aggregates (e.g., competition ranking across all `StudentCourseProfile`s in a course, peer assessment score distribution across team members), do **not** leave it in `Guts.Application` just because multiple aggregates are involved.
   - Pass the loaded domain objects into a pure, stateless **Domain Service** in `Guts.Domain` (with zero repository dependencies) that performs the domain calculation and mutates/returns domain results.

#### Lens 3: Anemic Domain Model & Broken Aggregate Encapsulation
Inspect the target classes in `Guts.Domain`:
- **Public Setters**: Replace `public T Property { get; set; }` with `public T Property { get; internal set; }` or `private set;` so outside layers cannot bypass domain invariants.
- **Exposed Mutable Collections**: Replace `public ICollection<Child> Children { get; set; }` with a private backing field (`private readonly List<Child> _children = [];`) exposed as `public IReadOnlyCollection<Child> Children => _children;` and mutated exclusively through domain methods on the Aggregate Root.
- **Anemic Data Bags**: If an entity in `Guts.Domain` has only properties and zero behavior while `*Service` in `Guts.Application` has hundreds of lines manipulating it, propose moving that behavior into the entity.

#### Lens 4: Supple Design & Side-Effect-Free Functions (Eric Evans, Chapter 10)
Evaluate whether the design makes the code easier to test, maintain, and compose:
- **Intention-Revealing Interfaces**: Method and class names should state *what* business rule is being applied in the GUTS Ubiquitous Language, not *how* data structures are iterated.
- **Side-Effect-Free Functions**: Separate pure domain calculations (returning a Value Object or result tuple) from state mutation and I/O.
- **Testability Signal**: If testing a domain rule currently requires mocking 6–10 repositories in `Guts.Application.Tests`, that is a red flag that domain logic is trapped in the Application Layer. Moving it to `Guts.Domain` allows testing it in `Guts.Domain.Tests` with zero mocks.

---

### Stage 2: Approved Inside-Out TDD Refactoring
Once the user approves the refactoring scope from Stage 1:
1. **Domain Unit Tests First (`Guts.Domain.Tests`)**:
   - Write unit tests for the new/enriched Aggregate methods, Value Objects, or pure Domain Services using `Builders/` and `RandomExtensions`.
2. **Domain Implementation (`Guts.Domain`)**:
   - Implement the domain logic, Value Objects, and `Contracts.Require(...)` invariants in `Guts.Domain`.
   - Ensure EF Core compatibility in `Guts.Infrastructure` (e.g., backing field mappings in `EntityConfigurations/` if encapsulating collections).
3. **Slim Down the Application Service (`Guts.Application` & `Guts.Application.Tests`)**:
   - Refactor the `Guts.Application` service method into a thin orchestrator:
     1. Load aggregates/data from repositories.
     2. Delegate business rules/calculations to `Guts.Domain`.
     3. Persist changes via repositories and return DTOs.
   - Update `Guts.Application.Tests` to verify orchestration (that the application service calls the domain method/service and persists).
4. **Verify Architecture & Quality Gates**:
   - Run `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj`.
   - Run `dotnet test Backoffice/Guts.slnx`.

