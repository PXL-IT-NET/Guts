# Specification: [Feature Title]

- **Feature ID**: `NNN-feature-slug`
- **Status**: `Draft | In Review | Approved | Implemented`
- **Created**: `YYYY-MM-DD`

---

## 1. Context & Problem Statement

_Describe the background, educational/operational problem, and why this feature or change is needed in GUTS._

---

## 2. User Stories & Actors

- **Actors**: `Student | Lector (Teacher) | Administrator | IDE Test Client | MCP Client`

- **US-1**: As a **[Actor]**, I want to **[action]** so that **[benefit/value]**.
- **US-2**: As a **[Actor]**, I want to **[action]** so that **[benefit/value]**.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: [Short descriptive title]
- **Given** [precondition / domain state]
- **When** [action / API call / UI interaction]
- **Then** [expected outcome / state transition / response]

### AC-2: [Short descriptive title]
- **Given** [precondition / domain state]
- **When** [action / API call / UI interaction]
- **Then** [expected outcome / state transition / response]

---

## 4. Domain Rules & Invariants

_List business rules that must be enforced by the domain model (`Guts.Domain`) via `Contracts.Require(...)` or aggregate methods:_

- **DR-1**: [e.g., A project team assessment cannot be created if the project has no components.]
- **DR-2**: [e.g., Deadline dates must be in UTC and cannot precede the period start date.]

---

## 5. Authorization & Security Rules

- **Students**: [What is permitted / forbidden?]
- **Lectors**: [What is permitted / forbidden?]
- **Unauthenticated / Client Token**: [Any special constraints?]

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Invalid domain state / precondition failure | Throws `ContractException` (`Contracts.Require`) | `400 Bad Request` |
| Entity not found | Throws `DataNotFoundException` or returns NotFound | `404 Not Found` |
| Unauthorized role or resource access | Forbidden response | `403 Forbidden` |

---

## 7. Non-Functional & Quality Gate Requirements

- **Test Coverage**: Unit and/or integration tests must cover as many lines and branches of all new and modified code as possible across every touched layer (`.NET` domain, application, infrastructure, API controllers/models/filters, and Angular components, services, guards, and utilities), satisfying the SonarCloud new-code coverage Quality Gate (`>= 80%`, targeting maximum achievable coverage).
- **Playwright E2E Coverage**: New or updated UI flows and acceptance criteria (`AC-*`) must have corresponding Playwright E2E test scenarios in `Backoffice/Guts.Web/ClientApp/e2e/tests/`.
- **Static Analysis & Security**: SonarCloud / SonarAnalyzer checks must pass with `0` open issues, `0` unreviewed security hotspots, and `0` ESLint errors.

---

## 8. Out of Scope

- [Explicitly list related items that will NOT be addressed in this spec]

---

## 9. Open Questions

- [ ] **Q1**: [Question needing clarification] — *Decision: [Pending]*

