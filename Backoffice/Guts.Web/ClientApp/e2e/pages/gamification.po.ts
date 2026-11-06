import { Locator, Page } from "@playwright/test";

export class CourseGamificationPage {
  readonly page: Page;
  readonly disabledAlert: Locator;
  readonly teacherDisabledBanner: Locator;
  readonly teacherPerspectiveBar: Locator;
  readonly studentPerspectiveSelect: Locator;
  readonly heroCard: Locator;
  readonly heroPseudonym: Locator;
  readonly tabWeeklySprint: Locator;
  readonly tabHallOfFame: Locator;
  readonly tabBadges: Locator;
  readonly tabTeacherControls: Locator;
  readonly hallOfFameProgression: Locator;
  readonly hallOfFameOvertakeCallout: Locator;
  readonly hallOfFameTableHeaders: Locator;
  readonly hallOfFameTableRows: Locator;
  readonly weeklySprintTableRows: Locator;
  readonly adminNewClassNameInput: Locator;
  readonly adminAddClassButton: Locator;
  readonly adminClassList: Locator;

  constructor(page: Page) {
    this.page = page;
    this.disabledAlert = page.locator("#gamificationDisabledAlert");
    this.teacherDisabledBanner = page.locator("#teacherGamificationDisabledBanner");
    this.teacherPerspectiveBar = page.locator("#teacherStudentPerspectiveBar");
    this.studentPerspectiveSelect = page.locator("#studentPerspectiveSelect");
    this.heroCard = page.locator("app-course-gamification-hero");
    this.heroPseudonym = page.locator("app-course-gamification-hero h3");
    this.tabWeeklySprint = page.getByRole("button", { name: /Weekly Sprint/i });
    this.tabHallOfFame = page.getByRole("button", { name: /Course Hall of Fame/i });
    this.tabBadges = page.getByRole("button", { name: /Badges/i });
    this.tabTeacherControls = page.getByRole("button", { name: /Teacher Controls/i });
    this.hallOfFameProgression = page.locator("#hallOfFameTierProgression");
    this.hallOfFameOvertakeCallout = page.locator("#hallOfFameOvertakeCallout");
    this.hallOfFameTableHeaders = page.locator("app-course-gamification-hall-of-fame thead th");
    this.hallOfFameTableRows = page.locator("app-course-gamification-hall-of-fame tbody tr");
    this.weeklySprintTableRows = page.locator("app-course-gamification-sprint tbody tr");
    this.adminNewClassNameInput = page.locator("#adminNewClassNameInput");
    this.adminAddClassButton = page.getByRole("button", { name: /Add Class/i });
    this.adminClassList = page.locator("app-course-gamification-admin .list-group-item");
  }

  async goto(courseId = 1) {
    await this.page.goto(`/courses/${courseId}/gamification`);
  }
}
