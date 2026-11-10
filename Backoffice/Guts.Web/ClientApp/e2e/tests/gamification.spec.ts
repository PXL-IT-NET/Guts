import { test, expect } from "../fixtures/auth.fixture";
import { setupGamificationApiMocks } from "../mocks/gamification.mocks";
import { CourseGamificationPage } from "../pages/gamification.po";

test.describe("Course Gamification (Feature 002)", () => {
  test("student sees disabled alert when gamification is switched off", async ({
    studentPage,
  }) => {
    await setupGamificationApiMocks(studentPage, { enabled: false, isTeacher: false });

    const gamificationPage = new CourseGamificationPage(studentPage);
    await gamificationPage.goto(1);

    await expect(gamificationPage.disabledAlert).toBeVisible();
    await expect(gamificationPage.disabledAlert).toContainText(
      "Gamification features and the leaderboard are currently disabled",
    );

    // Hero card and leaderboards should not be visible
    await expect(gamificationPage.heroCard).toHaveCount(0);
    await expect(gamificationPage.tabWeeklySprint).toHaveCount(0);
  });

  test("teacher sees disabled notice banner and can access Teacher Controls when gamification is off", async ({
    teacherPage,
  }) => {
    await setupGamificationApiMocks(teacherPage, { enabled: false, isTeacher: true });

    const gamificationPage = new CourseGamificationPage(teacherPage);
    await gamificationPage.goto(1);

    await expect(gamificationPage.teacherDisabledBanner).toBeVisible();
    await expect(gamificationPage.teacherDisabledBanner).toContainText(
      "Gamification is currently disabled for students",
    );

    // Teacher still has access to Teacher Controls to enable it
    await expect(gamificationPage.tabTeacherControls).toBeVisible();
  });

  test("student views Hero Summary and tier-relative Hall of Fame with overtake callout", async ({
    studentPage,
  }) => {
    await setupGamificationApiMocks(studentPage, { enabled: true, isTeacher: false });

    const gamificationPage = new CourseGamificationPage(studentPage);
    await gamificationPage.goto(1);

    // 1. Hero Summary Card verification
    await expect(gamificationPage.heroCard).toBeVisible();
    await expect(gamificationPage.heroPseudonym).toContainText("SyntaxSlayer");
    await expect(gamificationPage.heroCard).toContainText("Logic Apprentice");
    await expect(gamificationPage.heroCard).toContainText("180 XP");

    // Student should not see teacher perspective bar or teacher controls
    await expect(gamificationPage.teacherPerspectiveBar).toHaveCount(0);
    await expect(gamificationPage.tabTeacherControls).toHaveCount(0);

    // 2. Hall of Fame tab verification
    await gamificationPage.tabHallOfFame.click();

    // 4-icon tier progression bar is rendered
    await expect(gamificationPage.hallOfFameProgression).toBeVisible();
    await expect(gamificationPage.hallOfFameProgression).toContainText("Diamond");

    // Overtake motivation callout banner is visible with daily overtake XP incentive
    await expect(gamificationPage.hallOfFameOvertakeCallout).toBeVisible();
    await expect(gamificationPage.hallOfFameOvertakeCallout).toContainText("AliceDev");
    await expect(gamificationPage.hallOfFameOvertakeCallout).toContainText("+6 daily bonus XP");

    // Tier rankings always start at #1 (🥇) and show Class column when class competition is enabled and classes exist
    await expect(gamificationPage.hallOfFameTableHeaders.filter({ hasText: "Class" })).toHaveCount(1);
    const rows = gamificationPage.hallOfFameTableRows;
    await expect(rows).toHaveCount(2);
    await expect(rows.first()).toContainText("AliceDev");
    await expect(rows.first()).toContainText("1TIN-A");
    await expect(rows.first()).toContainText("🥇");
    await expect(rows.nth(1)).toContainText("SyntaxSlayer");
    await expect(rows.nth(1)).toContainText("🥈");
  });

  test("Hall of Fame and Weekly Sprint hide Class column when class competition is disabled or no classes exist", async ({
    studentPage,
  }) => {
    await setupGamificationApiMocks(studentPage, {
      enabled: true,
      isTeacher: false,
      isClassCompetitionEnabled: false,
    });

    const gamificationPage = new CourseGamificationPage(studentPage);
    await gamificationPage.goto(1);

    // Weekly Sprint hides Class column
    await expect(gamificationPage.weeklySprintTableHeaders.filter({ hasText: "Class" })).toHaveCount(0);
    await expect(gamificationPage.weeklySprintTableRows.first()).toContainText("AliceDev");
    await expect(gamificationPage.weeklySprintTableRows.first()).not.toContainText("1TIN-A");

    // Hall of Fame hides Class column
    await gamificationPage.tabHallOfFame.click();
    await expect(gamificationPage.hallOfFameTableHeaders.filter({ hasText: "Class" })).toHaveCount(0);
    await expect(gamificationPage.hallOfFameTableRows.first()).toContainText("AliceDev");
    await expect(gamificationPage.hallOfFameTableRows.first()).not.toContainText("1TIN-A");
  });

  test("student views Weekly Sprint leaderboard with weekly scores and highlights", async ({
    studentPage,
  }) => {
    await setupGamificationApiMocks(studentPage, { enabled: true, isTeacher: false });

    const gamificationPage = new CourseGamificationPage(studentPage);
    await gamificationPage.goto(1);

    await gamificationPage.tabWeeklySprint.click();

    const rows = gamificationPage.weeklySprintTableRows;
    await expect(rows).toHaveCount(2);

    // Top weekly sprint student
    await expect(rows.first()).toContainText("AliceDev");
    await expect(rows.first()).toContainText("80 XP");

    // Current student highlighted in sprint
    const currentUserRow = rows.filter({ hasText: "SyntaxSlayer" });
    await expect(currentUserRow).toHaveClass(/table-primary/);
  });

  test("teacher uses student perspective selector to inspect student in read-only mode", async ({
    teacherPage,
  }) => {
    await setupGamificationApiMocks(teacherPage, { enabled: true, isTeacher: true });

    const gamificationPage = new CourseGamificationPage(teacherPage);
    await gamificationPage.goto(1);

    // Teacher Perspective bar is visible
    await expect(gamificationPage.teacherPerspectiveBar).toBeVisible();

    // In default 'All Levels' mode, teacher has no personal Hero card
    await expect(gamificationPage.heroCard).toHaveCount(0);

    // Select student Bob Coder (first student option in dropdown)
    await gamificationPage.studentPerspectiveSelect.selectOption({ index: 1 });

    // The selected student's Hero card is now rendered in read-only mode
    await expect(gamificationPage.heroCard).toBeVisible();
    await expect(gamificationPage.heroPseudonym).toContainText("Bob Coder");
    await expect(gamificationPage.heroCard).toContainText("Code Crafter");
    await expect(gamificationPage.heroCard).toContainText("280 XP");
  });

  test("teacher can manage class groups in Teacher Controls", async ({
    teacherPage,
  }) => {
    await setupGamificationApiMocks(teacherPage, { enabled: true, isTeacher: true });

    const gamificationPage = new CourseGamificationPage(teacherPage);
    await gamificationPage.goto(1);

    await gamificationPage.tabTeacherControls.click();

    // Existing classes are listed
    await expect(gamificationPage.adminClassList.filter({ hasText: "1TIN-A" })).toBeVisible();
    await expect(gamificationPage.adminClassList.filter({ hasText: "1TIN-B" })).toBeVisible();

    // Add a new class group
    await gamificationPage.adminNewClassNameInput.fill("1TIN-C");
    await gamificationPage.adminAddClassButton.click();

    // New class appears in list
    await expect(gamificationPage.adminClassList.filter({ hasText: "1TIN-C" })).toBeVisible();
  });

  test("student views Class Championship with Class Score, highlighted class card, and personal contribution in score points", async ({
    studentPage,
  }) => {
    await setupGamificationApiMocks(studentPage, { enabled: true, isTeacher: false });

    const gamificationPage = new CourseGamificationPage(studentPage);
    await gamificationPage.goto(1);

    await gamificationPage.tabClassChampionship.click();

    // Personal contribution callout shows class name, rank, and score points without exposing internal multipliers
    await expect(gamificationPage.classPersonalContributionCallout).toBeVisible();
    await expect(gamificationPage.classPersonalContributionCallout).toContainText("1TIN-A");
    await expect(gamificationPage.classPersonalContributionCallout).toContainText("+113 pts");
    await expect(gamificationPage.classPersonalContributionCallout).toContainText("Class Rank #1");

    // Student's class card has 'Your Class' badge and displays Class Score (never Average XP, Total XP, or Total Green Tests)
    await expect(gamificationPage.yourClassBadge).toBeVisible();
    await expect(gamificationPage.classChampionshipCards).toHaveCount(2);
    await expect(gamificationPage.classChampionshipCards.first()).toContainText("385 pts");
    await expect(gamificationPage.classChampionshipCards.first()).not.toContainText("Average XP");
    await expect(gamificationPage.classChampionshipCards.first()).not.toContainText("Total XP");
    await expect(gamificationPage.classChampionshipCards.first()).not.toContainText("Total Green Tests");
  });
});
