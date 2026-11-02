import { test, expect } from "../fixtures/auth.fixture";
import { setupPeerAssessmentApiMocks } from "../mocks/peer-assessment.mocks";
import {
  ProjectAssessmentOverviewPage,
  PeerEvaluationFormPage,
  StudentMyResultPage,
  LectorDetailedResultsPage,
} from "../pages/peer-assessment.po";

test.describe("Project Team Peer Assessments (Feature 001)", () => {
  test("student views project assessments list with completion and missing peer status (US-3, AC-3)", async ({
    studentPage,
  }) => {
    await setupPeerAssessmentApiMocks(studentPage);

    const overviewPage = new ProjectAssessmentOverviewPage(studentPage);
    await overviewPage.goto(1, "proj1");

    // Title and table presence
    await expect(overviewPage.pageTitle).toBeVisible();
    await expect(overviewPage.peerAssessmentsTab).toBeVisible();
    await expect(overviewPage.peerAssessmentsTab).toHaveClass(/active/);
    await expect(overviewPage.assessmentTable).toBeVisible();
    await expect(overviewPage.assessmentRows).toHaveCount(2);

    // Row 1: Sprint 1 (Open, Incomplete)
    const row1 = overviewPage.getRowByDescription("Sprint 1 Peer Assessment");
    await expect(row1).toContainText("Not completed");
    await expect(row1).toContainText("Peers that need to evaluate others:");
    await expect(row1).toContainText("Student Guts");
    await expect(row1).toContainText("Alice TeamMate");

    // Student is a member of Team 1, so they see "Evaluate self and others"
    const evaluateBtn = row1.getByRole("link", {
      name: "Evaluate self and others",
    });
    await expect(evaluateBtn).toBeVisible();

    // Row 2: Sprint 2 (Completed)
    const row2 = overviewPage.getRowByDescription(
      "Sprint 2 Peer Assessment (Completed)",
    );
    await expect(row2).toContainText("Completed");

    // Student sees "View my result" for completed assessment
    const viewMyResultBtn = row2.getByRole("link", {
      name: "View my result",
    });
    await expect(viewMyResultBtn).toBeVisible();

    // Student must NOT see lector controls
    await expect(overviewPage.addPeerAssessmentButton).toHaveCount(0);
    await expect(overviewPage.viewResultsButton).toHaveCount(0);
    await expect(row1.getByRole("button", { name: "Edit" })).toHaveCount(0);
    await expect(row1.getByRole("button", { name: "Delete" })).toHaveCount(0);
  });

  test("student fills peer evaluation and verifies non-average score explanation requirement (US-2, AC-4, DR-6)", async ({
    studentPage,
  }) => {
    await setupPeerAssessmentApiMocks(studentPage);

    const formPage = new PeerEvaluationFormPage(studentPage);
    await formPage.goto(1, "proj1", 101, 1);

    await expect(formPage.pageTitle).toBeVisible();
    await expect(formPage.table).toBeVisible();
    await expect(formPage.rows).toHaveCount(2);

    // Row for Alice TeamMate
    const aliceRow = formPage.getRowForSubject("Alice TeamMate");
    await expect(aliceRow).toBeVisible();

    const aliceDropdowns = formPage.getScoreDropdowns(aliceRow);
    const aliceExplanation = formPage.getExplanationTextarea(aliceRow);

    // Select "Above group average (4)" for contribution
    await aliceDropdowns.contribution.selectOption("4");
    await aliceDropdowns.effort.selectOption("3");
    await aliceDropdowns.cooperation.selectOption("3");

    // AC-4 / DR-6: Because score is 4 (not Average 3) and explanation is empty, it must be marked invalid
    await expect(aliceExplanation).toHaveClass(/is-invalid/);

    // Entering an explanation should resolve the invalid state
    await aliceExplanation.fill(
      "Alice took proactive leadership on backend API integrations.",
    );
    await expect(aliceExplanation).not.toHaveClass(/is-invalid/);

    // Save evaluations
    await formPage.saveButton.click();

    // After successful submission, user is navigated back to the assessment overview
    await studentPage.waitForURL("**/courses/1/projects/proj1/assessments");
    const overviewPage = new ProjectAssessmentOverviewPage(studentPage);
    await expect(overviewPage.pageTitle).toBeVisible();
  });

  test("student views own summary result with peer vs self comparison and privacy safeguards (US-4, AC-6)", async ({
    studentPage,
  }) => {
    await setupPeerAssessmentApiMocks(studentPage);

    const myResultPage = new StudentMyResultPage(studentPage);
    await myResultPage.goto(1, "proj1", 102, 1);

    await expect(myResultPage.pageTitle).toBeVisible();
    await expect(myResultPage.pageTitle).toContainText(
      "Sprint 2 Peer Assessment (Completed) - Your results",
    );

    // Self and peer score dropdown values in General card
    await expect(myResultPage.selfScoreSelect).toBeVisible();
    await expect(myResultPage.selfScoreSelect).toHaveValue("4");

    await expect(myResultPage.peerScoreSelect).toBeVisible();
    await expect(myResultPage.peerScoreSelect).toHaveValue("3");

    // Radar chart canvas is rendered
    await expect(myResultPage.radarChart).toBeVisible();

    // AC-6 / Authorization: Student should not see raw peer breakdown tables
    await expect(studentPage.locator("table")).toHaveCount(0);
  });

  test("lector views detailed results, navigates completed teams skipping incomplete teams, and inspects feedback (US-5, AC-7)", async ({
    teacherPage,
  }) => {
    await setupPeerAssessmentApiMocks(teacherPage);

    const resultsPage = new LectorDetailedResultsPage(teacherPage);
    await resultsPage.goto(1, "proj1", 102, 1);

    await expect(resultsPage.pageTitle).toBeVisible();
    await expect(resultsPage.pageTitle).toContainText(
      "Sprint 2 Peer Assessment (Completed) - Results",
    );

    // 1. Verify tab: "Peer assessments" is present (not active), new tab "Assessment results 'Team 1'" is active
    await expect(resultsPage.peerAssessmentsTab).toBeVisible();
    await expect(resultsPage.peerAssessmentsTab).not.toHaveClass(/active/);
    await expect(resultsPage.assessmentResultsTab).toBeVisible();
    await expect(resultsPage.assessmentResultsTab).toHaveClass(/active/);
    await expect(resultsPage.assessmentResultsTab).toContainText(
      "Assessment results 'Team 1'",
    );

    // 2. Verify team name is displayed between Previous and Next links
    await expect(resultsPage.teamTitle).toContainText("Team 1");
    await expect(resultsPage.previousLink).toBeVisible();
    await expect(resultsPage.previousLink).toHaveClass(/disabled/);
    await expect(resultsPage.nextLink).toBeVisible();
    await expect(resultsPage.nextLink).not.toHaveClass(/disabled/);

    // 3. Verify team grade input and calculation
    await expect(resultsPage.teamGradeInput).toBeVisible();
    await resultsPage.teamGradeInput.fill("15");
    await expect(resultsPage.individualScoreInputs.first()).toBeVisible();

    // 4. Inspect detailed accordion item
    const studentAccordionBtn = resultsPage.accordionButtons.filter({
      hasText: "About Student Guts",
    });
    await expect(studentAccordionBtn).toBeVisible();
    await studentAccordionBtn.click();

    const feedbackTable = resultsPage.page.locator(
      'table[aria-label="Assessements about Student Guts"]',
    );
    await expect(feedbackTable).toBeVisible();
    await expect(feedbackTable).toContainText("Alice TeamMate");
    const peerExplanation = feedbackTable.locator(
      "textarea#peerAssessmentExplanation",
    );
    await expect(peerExplanation).toHaveValue(
      "Great initiative on UI components",
    );

    // 5. Navigate to Next completed team:
    // Team 2 is incomplete (mockStatusAssessment102Team2.isComplete === false),
    // so Next link must skip Team 2 and point directly to Team 3!
    await resultsPage.nextLink.click();
    await teacherPage.waitForURL(
      "**/courses/1/projects/proj1/assessments/102/teams/3/detailed-results",
    );

    // Verify Team 3 is displayed in header tab and between Previous/Next
    await expect(resultsPage.teamTitle).toContainText("Team 3");
    await expect(resultsPage.assessmentResultsTab).toContainText(
      "Assessment results 'Team 3'",
    );
    await expect(resultsPage.nextLink).toHaveClass(/disabled/);
    await expect(resultsPage.previousLink).not.toHaveClass(/disabled/);

    // 6. Navigate Previous from Team 3 -> skips incomplete Team 2 and returns to Team 1
    await resultsPage.previousLink.click();
    await teacherPage.waitForURL(
      "**/courses/1/projects/proj1/assessments/102/teams/1/detailed-results",
    );
    await expect(resultsPage.teamTitle).toContainText("Team 1");
    await expect(resultsPage.assessmentResultsTab).toContainText(
      "Assessment results 'Team 1'",
    );

    // 7. Clicking "Peer assessments" tab navigates back to overview
    await resultsPage.peerAssessmentsTab.click();
    await teacherPage.waitForURL("**/courses/1/projects/proj1/assessments");
    const overviewPage = new ProjectAssessmentOverviewPage(teacherPage);
    await expect(overviewPage.pageTitle).toBeVisible();
    await expect(overviewPage.peerAssessmentsTab).toHaveClass(/active/);
  });

  test("lector creates and deletes project assessment (US-1, AC-1)", async ({
    teacherPage,
  }) => {
    await setupPeerAssessmentApiMocks(teacherPage);

    const overviewPage = new ProjectAssessmentOverviewPage(teacherPage);
    await overviewPage.goto(1, "proj1");

    // Lector sees "Add peer assessment"
    await expect(overviewPage.addPeerAssessmentButton).toBeVisible();

    // Completed assessment has "View results" link for lector
    const row2 = overviewPage.getRowByDescription(
      "Sprint 2 Peer Assessment (Completed)",
    );
    await expect(
      row2.getByRole("link", { name: "View results" }),
    ).toBeVisible();

    // 1. Create a new peer assessment
    await overviewPage.addPeerAssessmentButton.click();
    await expect(overviewPage.modalTitle).toContainText("Add peer assessment");

    await overviewPage.fillAddModal(
      "Sprint 3 Final Evaluation",
      "2026-12-01T08:00",
      "2026-12-15T23:59",
    );
    await overviewPage.modalSaveButton.click();

    // Modal closes and new assessment appears in the list
    const row3 = overviewPage.getRowByDescription("Sprint 3 Final Evaluation");
    await expect(row3).toBeVisible();

    // 2. Delete an assessment
    // Handle the browser confirm dialog
    teacherPage.once("dialog", async (dialog) => {
      expect(dialog.message()).toContain("Sprint 1 Peer Assessment");
      await dialog.accept();
    });

    const row1 = overviewPage.getRowByDescription("Sprint 1 Peer Assessment");
    const deleteBtn = row1.getByRole("button", { name: "Delete" });
    await deleteBtn.click();

    // Verify row 1 is removed
    await expect(
      overviewPage.getRowByDescription("Sprint 1 Peer Assessment"),
    ).toHaveCount(0);
  });

  test("displays empty state when project has no assessments configured", async ({
    studentPage,
  }) => {
    await setupPeerAssessmentApiMocks(studentPage, { emptyAssessments: true });

    const overviewPage = new ProjectAssessmentOverviewPage(studentPage);
    await overviewPage.goto(1, "proj1");

    await expect(overviewPage.emptyMessage).toBeVisible();
    await expect(overviewPage.assessmentTable).toHaveCount(0);
  });
});
