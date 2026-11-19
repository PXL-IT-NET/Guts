import { Locator, Page } from "@playwright/test";

export class CourseStatisticsPage {
  readonly page: Page;
  readonly overviewCards: Locator;
  readonly emptyStateAlert: Locator;
  readonly studentRows: Locator;
  readonly sortByLastNameButton: Locator;
  readonly sortByCompletionButton: Locator;
  readonly expandedBreakdownCard: Locator;
  readonly expandedChapterHeadings: Locator;

  constructor(page: Page) {
    this.page = page;
    this.overviewCards = page.locator("app-course-statistics .card.bg-light");
    this.emptyStateAlert = page.locator("app-course-statistics .alert-info");
    this.studentRows = page.locator("app-course-statistics tr.student-row");
    this.sortByLastNameButton = page.getByRole("button", {
      name: /last name/i,
    });
    this.sortByCompletionButton = page.getByRole("button", {
      name: /course completion/i,
    });
    this.expandedBreakdownCard = page.locator(
      "app-course-statistics tr.bg-light .card-body",
    );
    this.expandedChapterHeadings = this.expandedBreakdownCard.locator(
      ".text-primary span",
    );
  }

  async goto(courseId = 1) {
    await this.page.goto(`/courses/${courseId}/statistics`);
  }

  studentRowByLastName(lastName: string): Locator {
    return this.studentRows.filter({ hasText: lastName });
  }

  chapterSection(chapterTitle: string): Locator {
    return this.expandedBreakdownCard
      .locator("> div.mb-3")
      .filter({
        has: this.page.locator(".text-primary span", {
          hasText: new RegExp(`^${chapterTitle}$`),
        }),
      });
  }
}
