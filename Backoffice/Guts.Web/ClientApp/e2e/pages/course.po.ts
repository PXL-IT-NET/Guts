import { Locator, Page } from "@playwright/test";

export class CoursePage {
  readonly page: Page;
  readonly courseHeading: Locator;
  readonly courseStatisticsButton: Locator;
  readonly chapterSettingsTab: Locator;
  readonly testOutcomesHeader: Locator;
  readonly assignmentTitle: Locator;
  readonly assignmentTestRatio: Locator;

  constructor(page: Page) {
    this.page = page;
    this.courseHeading = page.locator("h4");
    this.courseStatisticsButton = page.getByRole("link", {
      name: /course statistics/i,
    });
    this.chapterSettingsTab = page.getByRole("link", {
      name: /chapter settings/i,
    });
    this.testOutcomesHeader = page.locator(".card-header", {
      hasText: "Your test outcomes",
    });
    this.assignmentTitle = page.locator("assignment-summary h5 a");
    this.assignmentTestRatio = page.locator("assignment-summary p");
  }

  async goto(courseId = 1) {
    await this.page.goto(`/courses/${courseId}`);
  }
}

