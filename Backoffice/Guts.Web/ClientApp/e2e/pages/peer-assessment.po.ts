import { Page, Locator } from "@playwright/test";

export class ProjectAssessmentOverviewPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly peerAssessmentsTab: Locator;
  readonly emptyMessage: Locator;
  readonly assessmentTable: Locator;
  readonly assessmentRows: Locator;
  readonly teamSelect: Locator;
  readonly evaluateButton: Locator;
  readonly viewMyResultButton: Locator;
  readonly viewResultsButton: Locator;
  readonly addPeerAssessmentButton: Locator;

  // Add/Edit modal locators
  readonly modalTitle: Locator;
  readonly modalDescriptionInput: Locator;
  readonly modalOpenOnInput: Locator;
  readonly modalDeadlineInput: Locator;
  readonly modalSaveButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator("h5", { hasText: "Peer assessments" });
    this.peerAssessmentsTab = page
      .locator(".nav-tabs")
      .getByRole("link", { name: "Peer assessments" });
    this.emptyMessage = page.getByText(
      "This project has no peer assessment configured yet.",
    );
    this.assessmentTable = page.locator('table[aria-label="peer assessments"]');
    this.assessmentRows = this.assessmentTable.locator("tbody tr");
    this.teamSelect = page.locator("select#teams");
    this.evaluateButton = page.getByRole("link", {
      name: "Evaluate self and others",
    });
    this.viewMyResultButton = page.getByRole("link", {
      name: "View my result",
    });
    this.viewResultsButton = page.getByRole("link", { name: "View results" });
    this.addPeerAssessmentButton = page.getByRole("button", {
      name: "Add peer assessment",
    });

    this.modalTitle = page.locator(".modal-title");
    this.modalDescriptionInput = page.locator("#description");
    this.modalOpenOnInput = page.locator(
      'app-ng-datetime[formcontrolname="openOn"] input',
    );
    this.modalDeadlineInput = page.locator(
      'app-ng-datetime[formcontrolname="deadline"] input',
    );
    this.modalSaveButton = page.locator(".modal-footer button", {
      hasText: "Save",
    });
  }

  async goto(courseId = 1, projectCode = "proj1") {
    await this.page.goto(
      `/courses/${courseId}/projects/${projectCode}/assessments`,
    );
    await this.page.waitForLoadState("networkidle");
  }

  getRowByDescription(description: string): Locator {
    return this.assessmentRows.filter({ hasText: description });
  }

  async fillAddModal(
    description: string,
    openOn = "2026-11-01T08:00",
    deadline = "2026-11-15T23:59",
  ) {
    await this.modalDescriptionInput.fill(description);
    await this.modalOpenOnInput.fill(openOn);
    await this.modalDeadlineInput.fill(deadline);
    await this.modalDeadlineInput.dispatchEvent("change");
  }
}

export class PeerEvaluationFormPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly table: Locator;
  readonly rows: Locator;
  readonly saveButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator("h5", { hasText: "Peer evaluation form" });
    this.table = page.locator('table[aria-label="peer assessments"]');
    this.rows = this.table.locator("tbody tr");
    this.saveButton = page.getByRole("button", { name: "Save" });
  }

  async goto(
    courseId = 1,
    projectCode = "proj1",
    assessmentId = 101,
    teamId = 1,
  ) {
    await this.page.goto(
      `/courses/${courseId}/projects/${projectCode}/assessments/${assessmentId}/teams/${teamId}/evaluate`,
    );
    await this.page.waitForLoadState("networkidle");
  }

  getRowForSubject(name: string): Locator {
    return this.rows.filter({ hasText: name });
  }

  getScoreDropdowns(row: Locator): {
    contribution: Locator;
    effort: Locator;
    cooperation: Locator;
  } {
    const dropdowns = row.locator("app-assessment-score-dropdown select");
    return {
      contribution: dropdowns.nth(0),
      effort: dropdowns.nth(1),
      cooperation: dropdowns.nth(2),
    };
  }

  getExplanationTextarea(row: Locator): Locator {
    return row.locator("textarea#peerAssessmentExplanation");
  }
}

export class StudentMyResultPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly selfScoreSelect: Locator;
  readonly peerScoreSelect: Locator;
  readonly radarChart: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator("h5", {
      hasText: /Your results/,
    });
    this.selfScoreSelect = page.locator("select#averageSelfcore");
    this.peerScoreSelect = page.locator("select#averagePeerScore");
    this.radarChart = page.locator("canvas");
  }

  async goto(
    courseId = 1,
    projectCode = "proj1",
    assessmentId = 102,
    teamId = 1,
  ) {
    await this.page.goto(
      `/courses/${courseId}/projects/${projectCode}/assessments/${assessmentId}/teams/${teamId}/my-result`,
    );
    await this.page.waitForLoadState("networkidle");
  }
}

export class LectorDetailedResultsPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly peerAssessmentsTab: Locator;
  readonly assessmentResultsTab: Locator;
  readonly previousLink: Locator;
  readonly nextLink: Locator;
  readonly teamTitle: Locator;
  readonly teamGradeInput: Locator;
  readonly correctionTypeSelect: Locator;
  readonly individualScoreInputs: Locator;
  readonly accordionButtons: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator("h5", {
      hasText: /Results/,
    });
    this.peerAssessmentsTab = page
      .locator(".nav-tabs")
      .getByRole("link", { name: "Peer assessments" });
    this.assessmentResultsTab = page
      .locator(".nav-tabs")
      .getByRole("link", { name: /Assessment results/ });
    this.previousLink = page.locator("a.nav-link", { hasText: "Previous" });
    this.nextLink = page.locator("a.nav-link", { hasText: "Next" });
    this.teamTitle = page.locator(
      "app-project-team-assessment-detailed-results span.nav-link.active",
    );
    this.teamGradeInput = page.locator("input#teamGrade");
    this.correctionTypeSelect = page.locator("select#correctionType");
    this.individualScoreInputs = page.locator("input#individualScore");
    this.accordionButtons = page.locator("button[ngbAccordionButton]");
  }

  async goto(
    courseId = 1,
    projectCode = "proj1",
    assessmentId = 102,
    teamId = 1,
  ) {
    await this.page.goto(
      `/courses/${courseId}/projects/${projectCode}/assessments/${assessmentId}/teams/${teamId}/detailed-results`,
    );
    await this.page.waitForLoadState("networkidle");
  }
}
