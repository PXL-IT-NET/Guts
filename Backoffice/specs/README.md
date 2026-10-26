# GUTS Backoffice — Spec-Driven Development (SDD)

This directory contains feature specifications, technical designs, and TDD implementation task lists for the GUTS Backoffice project.

---

## Workflow Overview

Every non-trivial feature or architectural change in the Backoffice follows a 4-phase Spec-Driven Development lifecycle:

```
1. Specify (spec.md) ──► 2. Design (design.md) ──► 3. Tasks (tasks.md) ──► 4. Implement (TDD)
```

### Phase 1: Specify (`spec.md`)
Define **what** needs to be built and **why** before deciding how to code it.
- Capture user stories (Student, Teacher/Lector, Admin).
- Define testable **Acceptance Criteria (`AC-1`, `AC-2`, ...)** using `Given / When / Then` scenarios.
- Explicitly list domain invariants, permissions, edge cases, and what is out of scope.
- **Gate**: Review and resolve any open questions before moving to technical design.

### Phase 2: Design (`design.md`)
Map the requirements onto the GUTS Backoffice layered DDD architecture (`Guts.slnx` + Angular SPA):
1. **`Guts.Domain`**: Aggregates, Entities, Value Objects, `Contracts.Require(...)` preconditions, and `I*Factory` interfaces.
2. **`Guts.Business`**: Application services, repository interfaces (`I*Repository`), DTOs, and converters.
3. **`Guts.Infrastructure`**: EF Core entity configurations, migrations, and repository implementations.
4. **`Guts.Api`**: REST API endpoints, request/response models, authorization attributes, and MCP server tools/resources (if applicable).
5. **`Guts.Web/ClientApp`**: Angular components, services, viewmodels, routes, and guards.
- Note any naming convention requirements so `Guts.Bootstrapper` auto-registers new components via `StartUpExtensions.cs`.
- **Gate**: Verify the design respects DDD boundaries and SOLID principles before breaking down tasks.

### Phase 3: Task Breakdown (`tasks.md`)
Translate the design into small, ordered **Test-Driven Development (TDD)** slices.
- Reference the relevant Acceptance Criteria (`[AC-1]`) on each task.
- Order tasks from the inside out:
  1. **Domain Layer** (`Guts.Domain.Tests` ➔ `Guts.Domain`)
  2. **Business Layer** (`Guts.Business.Tests` ➔ `Guts.Business`)
  3. **Infrastructure Layer** (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure` + EF Migrations)
  4. **API Layer** (`Guts.Api.Tests` ➔ `Guts.Api`)
  5. **Frontend SPA** (`*.spec.ts` ➔ Angular components/services in `Guts.Web/ClientApp`)
  6. **Quality & Verification** (`dotnet test`, `npm test`, and SonarQube analysis)

### Phase 4: TDD Implementation
Execute the tasks in `tasks.md` one by one:
- Write the failing unit test first (using builders in `Builders/` and `RandomExtensions` where applicable).
- Implement the minimal production code to pass the test.
- Check off completed items (`- [x]`) in `tasks.md` to maintain live progress tracking.

---

## Directory Structure & Naming Convention

```text
Backoffice/specs/
├── README.md                  # This guide
├── _templates/                # Reusable Markdown templates
│   ├── spec.md                # Functional requirements & acceptance criteria
│   ├── design.md              # Layered DDD & Angular technical design
│   └── tasks.md               # Ordered TDD implementation checklist
└── NNN-feature-slug/          # Active or completed feature folder (e.g., 001-exam-part-export)
    ├── spec.md
    ├── design.md
    └── tasks.md
```

- Use a 3-digit zero-padded prefix (`001-`, `002-`, ...) followed by a short `kebab-case` feature name.
- Small bug fixes or single-layer tweaks can combine `spec.md`, `design.md`, and `tasks.md` or omit unaffected layer sections.

---

## Working with AI Coding Agents

You can drive each phase interactively with your AI assistant:

1. **Draft a spec**:
   > *"Create a new spec `001-<feature-name>` for [describe feature]. Draft `spec.md` using the template and ask me any clarifying questions."*
2. **Generate the technical design**:
   > *"Based on `Backoffice/specs/001-<feature-name>/spec.md`, inspect the codebase and create `design.md`."*
3. **Create the TDD task plan**:
   > *"Generate `tasks.md` for `001-<feature-name>` based on `spec.md` and `design.md`."*
4. **Implement step-by-step**:
   > *"Implement the Domain Layer tasks from `Backoffice/specs/001-<feature-name>/tasks.md` using TDD."*

