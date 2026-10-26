# Technical Design: [Feature Title]

- **Feature ID**: `NNN-feature-slug`
- **Specification**: [`spec.md`](./spec.md)
- **Status**: `Draft | Approved | Implemented`

---

## 1. Architectural Overview

_High-level summary of how this feature flows across the GUTS Backoffice layers (Angular SPA / MCP ➔ `Guts.Api` ➔ `Guts.Business` ➔ `Guts.Domain` & `Guts.Infrastructure`). Omit sections for layers that require no changes._

---

## 2. Domain Layer (`Guts.Domain`)

### Aggregates, Entities & Value Objects
- **Modified / New Types**:
  - `[AggregateOrEntityName]` (`Guts.Domain/[AggregateFolder]/...`)
    - New properties (use private setters and read-only collections where appropriate; FK properties allowed for EF Core).
    - New/updated domain methods and state transitions.
- **Invariants (`Contracts.Require`)**:
  - Enforce `[DR-1]`: `Contracts.Require(..., "...");`

### Domain Factories
- **Interface**: `I[Entity]Factory`
- **Implementation**: `[Entity]Factory` (auto-registered by `Guts.Bootstrapper` convention)

---

## 3. Business Layer (`Guts.Business`)

### Repository Contracts (`Guts.Business/Repositories/`)
- `I[Entity]Repository`:
  - `Task<[Entity]> GetBy...Async(...);`

### Application Services (`Guts.Business/Services/`)
- **Interface**: `I[Feature]Service`
- **Implementation**: `[Feature]Service`
  - Methods & orchestration steps (load via repository ➔ invoke domain method/factory ➔ persist changes).

### DTOs & Converters (`Guts.Business/Dtos/` & `Guts.Business/Converters/`)
- New/updated DTOs and `I*Converter` classes.

---

## 4. Infrastructure Layer (`Guts.Infrastructure`)

### EF Core Configuration & Migrations
- **`GutsContext` / Entity Configurations**:
  - Table mappings, indexes, foreign keys, and delete behaviors.
- **Migration Required?**: `Yes / No`
  - Migration name: `[MigrationName]`

### Repository Implementations (`Guts.Infrastructure/Repositories/`)
- `[Entity]DatabaseRepository`:
  - Query patterns, `Include(...)` eager loading requirements, and performance considerations.

---

## 5. API & MCP Layer (`Guts.Api`)

### REST Endpoints (`Guts.Api/Controllers/`)
- **Controller**: `[Name]Controller`
- **Endpoint**: `[HTTP Verb] /api/[route]`
- **Authorization**: `[Authorize(Policy = ...)]` or role checks
- **Request Model**: `[Name]RequestModel` (`Guts.Api/Models/`)
- **Response Model**: `[Name]ResponseModel` (`Guts.Api/Models/`)
- **Model Converter**: `I[Name]ModelConverter` (`Guts.Api/Models/Converters/`)

### MCP Server (`Guts.Api/Mcp/`) *(if applicable)*
- New/updated MCP prompts, resources, or tools.

---

## 6. Frontend Layer (`Guts.Web/ClientApp`) *(if applicable)*

### Services & ViewModels (`src/app/services/`, `src/app/viewmodels/`)
- Service methods and HTTP calls.
- TypeScript interfaces / classes.

### Components & Routing (`src/app/components/`)
- Component hierarchy, inputs/outputs, template interactions, and route guards.

---

## 7. Dependency Injection (`Guts.Bootstrapper`)

- Confirm all new Factories (`*Factory`), Services (`*Service`), Repositories (`*Repository`), and Converters (`*Converter`) follow naming conventions in `StartUpExtensions.cs` for automatic DI registration, or document explicit registrations needed.

