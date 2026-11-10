# Tasks: DDD Domain Deep Modeling & Deprecated Exam Removal

- **Feature ID**: `003-ddd-refactoring`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)
- **Status**: `Implemented`

---

## Phase 1: Deprecated Exam Purge & Database Migration

- [x] **[T1.1]** `[AC-1]` Delete `ExamAggregate` (`Exam.cs`, `ExamPart.cs`, `AssignmentEvaluation.cs`, `IExamRepository.cs`, `IExamPartRepository.cs`) from `Guts.Domain` and `Guts.Application`.
- [x] **[T1.2]** `[AC-1]` Delete `ExamService.cs`, `IExamService.cs`, and `ExamServiceTests.cs` from `Guts.Application` and `Guts.Application.Tests`.
- [x] **[T1.3]** `[AC-1]` Remove `Exam` references from `GutsContext.cs`, `EntityConfigurations/`, `TestDataSeeder.cs`, and repositories in `Guts.Infrastructure`.
- [x] **[T1.4]** `[AC-1]` Add EF Core migration `20261110194842_RemoveExamAggregate` dropping tables `Exams`, `ExamParts`, and `AssignmentEvaluation`.
- [x] **[T1.5]** `[AC-1]` Delete `ExamController.cs`, `ExamControllerTests.cs`, and `ExamModels/` from `Guts.Api` and `Guts.Api.Tests`.
- [x] **[T1.6]** `[AC-1]` Delete Angular exam components (`exam`, `exampart`), services (`exam.service.ts`), and viewmodels (`exam.model.ts`) from `Guts.Web/ClientApp`.
- [x] **[T1.7]** `[AC-1]` Verify `dotnet ef migrations has-pending-model-changes` returns 0 pending model changes.

---

## Phase 2: Course Progress & Statistics Domain Calculation

- [x] **[T2.1]** `[AC-2, DR-3]` Create `CourseProgressReport.cs`, `CourseStudentProgress.cs`, `CourseChapterProgress.cs`, and `CourseAssignmentProgress.cs` in `Guts.Domain.CourseAggregate`.
- [x] **[T2.2]** `[AC-2, DR-3]` Implement `CourseProgressCalculator.cs` and `ICourseProgressCalculator.cs` as a pure domain service in `Guts.Domain.CourseAggregate`.
- [x] **[T2.3]** `[AC-2]` Write comprehensive unit tests in `CourseProgressCalculatorTests.cs` and `CourseTests.cs` in `Guts.Domain.Tests`.
- [x] **[T2.4]** `[AC-2, DR-2]` Refactor `CourseService.GetCourseStatisticsAsync` to delegate calculation to `ICourseProgressCalculator`.
- [x] **[T2.5]** `[AC-2]` Update `CourseServiceTests.cs` in `Guts.Application.Tests`.

---

## Phase 3: Assignment Passage Distribution & TestRun Summary

- [x] **[T3.1]** `[AC-3, DR-1]` Create Value Objects `AssignmentPassageDistribution.cs` and `TestPassageCount.cs` in `Guts.Domain.AssignmentAggregate`.
- [x] **[T3.2]** `[AC-3]` Create Value Object `TestRunSummary.cs` in `Guts.Domain.TestRunAggregate`.
- [x] **[T3.3]** `[AC-3]` Write unit tests in `AssignmentPassageDistributionTests.cs` and `TestRunSummaryTests.cs` in `Guts.Domain.Tests`.
- [x] **[T3.4]** `[AC-3, DR-2]` Refactor `AssignmentWithResultsConverter.ToAssignmentStatisticsDto` and `AssignmentService.ConstructTestRunInfoFromTestRuns` to delegate to the new Value Objects.

---

## Phase 4: LoginSession Aggregate Encapsulation

- [x] **[T4.1]** `[AC-4, DR-1, DR-4]` Enforce encapsulation in `LoginSession.cs` (`internal set;` properties, factory method `Create`, methods `AttachLoginToken`, `Cancel`, `IsExpired`).
- [x] **[T4.2]** `[AC-4]` Write unit tests in `LoginSessionTests.cs` in `Guts.Domain.Tests`.
- [x] **[T4.3]** `[AC-4, DR-2]` Refactor `LoginSessionService.cs` (`CreateSessionAsync`, `CleanUpOldSessionsAsync`, `CancelSessionAsync`, `SetLoginTokenForSessionAsync`) to delegate directly to `LoginSession` methods.
- [x] **[T4.4]** `[AC-4]` Update `LoginSessionBuilder.cs` and `LoginSessionServiceTests.cs` in `Guts.Application.Tests`.

---

## Phase 5: Assignment Recommendation Domain Policy

- [x] **[T5.1]** `[AC-5, DR-3]` Create `RecommendedAssignment.cs` and pure domain policy `AssignmentRecommendationPolicy.cs` in `Guts.Domain.AssignmentAggregate`.
- [x] **[T5.2]** `[AC-5]` Write unit tests in `AssignmentRecommendationPolicyTests.cs` in `Guts.Domain.Tests`.
- [x] **[T5.3]** `[AC-5, DR-2]` Refactor `StreakReminderService.BuildRecommendedAssignments` to delegate candidate selection directly to `AssignmentRecommendationPolicy.RecommendAssignments`.
- [x] **[T5.4]** `[AC-5]` Update `StreakReminderServiceTests.cs` in `Guts.Application.Tests`.

---

## Phase 6: Project & ProjectTeam Encapsulation

- [x] **[T6.1]** `[AC-7, DR-1]` Add `Rename(string newName)`, `EnsureBelongsTo(int courseId, string projectCode)`, and `HasMember(int userId)` to `ProjectTeam.cs` and `IProjectTeam.cs`.
- [x] **[T6.2]** `[AC-7, DR-1]` Add `LoadAssignments` to `Topic.cs` and `AddTeam`, `GenerateTeams`, `FindTeamOfUser`, `ValidateUserCanJoinTeam`, `EnsureTeamBelongsToProject`, and `EnsureUserIsMemberOfProject` to `Project.cs`.
- [x] **[T6.3]** `[AC-7]` Write unit tests in `ProjectTeamTests.cs` and `ProjectTests.cs` in `Guts.Domain.Tests`.
- [x] **[T6.4]** `[AC-7, DR-2]` Refactor `ProjectService.UpdateProjectTeamAsync` to call `teamToUpdate.Rename(teamName)`.
- [x] **[T6.5]** `[AC-7]` Refactor `ProjectTeamController` and `AssignmentController` to use `team.HasMember` and `project.EnsureTeamBelongsToProject`.

---

## Phase 7: Gamification Event Aggregator (`RecentGainsSummary`)

- [x] **[T7.1]** `[AC-6, DR-1]` Create Value Objects `RecentGainsSummary.cs` and `AggregatedXpGain.cs` in `Guts.Domain.GamificationAggregate`.
- [x] **[T7.2]** `[AC-6]` Write unit tests in `RecentGainsSummaryTests.cs` in `Guts.Domain.Tests`.
- [x] **[T7.3]** `[AC-6, DR-2]` Refactor `GamificationService.GetStudentCourseProfileAsync` to delegate event summarization to `RecentGainsSummary`, eliminating the 64-line static procedural method `BuildRecentEventDtos`.
- [x] **[T7.4]** `[AC-6]` Update `GamificationServiceTests.cs` in `Guts.Application.Tests`.

---

## Phase 8: Intention-Revealing Entity Properties & Converter Sanitization

- [x] **[T8.1]** `[AC-8]` Encapsulate `Assignment.Description` to auto-format numeric assignment codes (`"1"`) as `"Assignment 1"` when no custom description is set or it matches the code.
- [x] **[T8.2]** `[AC-8]` Remove procedural `EnsureAssignmentDescription` method from `TopicConverter.cs`.
- [x] **[T8.3]** `[AC-8]` Add intention-revealing properties `HasPassingTests`, `PassedTestsCount`, and `FailedTestsCount` to `TestRun.cs`.
- [x] **[T8.4]** `[AC-8]` Add computed property `FullName => $"{FirstName} {LastName}".Trim()` to `User.cs`, updating `UserConverter.cs`, `Mapper.cs`, and `AssignmentService.cs`.
- [x] **[T8.5]** `[AC-8]` Write unit tests in `AssignmentTests.cs`, `TestRunTests.cs`, and `UserTests.cs` in `Guts.Domain.Tests`.

---

## Phase 9: Quality & Verification

- [x] **[T9.1]** Run `dotnet build Backoffice/Guts.slnx -warnaserror` and verify 0 warnings, 0 errors.
- [x] **[T9.2]** Run `dotnet ef migrations has-pending-model-changes` and verify 0 pending model changes.
- [x] **[T9.3]** Run `dotnet test Backoffice/Guts.slnx` and verify all 1,111 tests pass.
- [x] **[T9.4]** Run `npm --prefix Backoffice/Guts.Web/ClientApp test -- --watch=false` and verify all 293 Karma tests pass.

