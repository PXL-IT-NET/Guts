import { test, expect } from "../fixtures/auth.fixture";
import {
  mockCourseStatistics,
  setupCourseApiMocks,
} from "../mocks/course.mocks";
import { CoursePage } from "../pages/course.po";
import { CourseStatisticsPage } from "../pages/course-statistics.po";

test.describe("Course Statistics Visualization", () => {
  test("lector views statistics, sorts students, and sees chapters and assignments sorted by description with code fallback", async ({
    teacherPage,
  }) => {
    await setupCourseApiMocks(teacherPage);

    const coursePage = new CoursePage(teacherPage);
    await coursePage.goto(1);
    await coursePage.courseStatisticsButton.click();

    await expect(teacherPage).toHaveURL(/\/courses\/1\/statistics/);

    const statsPage = new CourseStatisticsPage(teacherPage);

    // Overview cards display course totals
    await expect(statsPage.overviewCards).toHaveCount(3);
    await expect(statsPage.overviewCards.nth(0)).toContainText("Assignments");
    await expect(statsPage.overviewCards.nth(0)).toContainText("4");
    await expect(statsPage.overviewCards.nth(1)).toContainText("Total tests");
    await expect(statsPage.overviewCards.nth(1)).toContainText("10");
    await expect(statsPage.overviewCards.nth(2)).toContainText(
      "Students with results",
    );
    await expect(statsPage.overviewCards.nth(2)).toContainText("2");

    // Default sorting: Last name ascending (Alpha before Zeta)
    await expect(statsPage.studentRows).toHaveCount(2);
    await expect(statsPage.studentRows.nth(0)).toContainText("Alpha");
    await expect(statsPage.studentRows.nth(1)).toContainText("Zeta");

    // Toggle last name sort to descending (Zeta before Alpha)
    await statsPage.sortByLastNameButton.click();
    await expect(statsPage.studentRows.nth(0)).toContainText("Zeta");
    await expect(statsPage.studentRows.nth(1)).toContainText("Alpha");

    // Sort by course completion ascending (40% Zeta before 100% Alpha)
    await statsPage.sortByCompletionButton.click();
    await expect(statsPage.studentRows.nth(0)).toContainText("40%");
    await expect(statsPage.studentRows.nth(1)).toContainText("100%");

    // Expand Bob Zeta's accordion row
    await statsPage.studentRowByLastName("Zeta").click();
    await expect(statsPage.expandedBreakdownCard).toBeVisible();
    await expect(statsPage.expandedBreakdownCard).toContainText(
      "Assignment breakdown for Zeta Bob",
    );

    // Chapters sorted by description (with code fallback for empty description)
    await expect(statsPage.expandedChapterHeadings).toHaveText([
      "Alpha Chapter",
      "ch02",
      "Zeta Chapter",
    ]);

    // Alpha Chapter has no assignments
    await expect(statsPage.chapterSection("Alpha Chapter")).toContainText(
      "No assignments in this chapter.",
    );

    // Chapter ch02 (fallback title): assignments sorted by description with code fallback -> ex03 (Alpha Exercise) before ex09 (empty description -> ex09)
    const ch02AssignmentCodes = statsPage
      .chapterSection("ch02")
      .locator("tbody tr td:first-child strong");
    await expect(ch02AssignmentCodes).toHaveText(["ex03", "ex09"]);

    // Zeta Chapter: assignments sorted by description -> ex02 (First Exercise) before ex01 (Second Exercise)
    const zetaAssignmentCodes = statsPage
      .chapterSection("Zeta Chapter")
      .locator("tbody tr td:first-child strong");
    await expect(zetaAssignmentCodes).toHaveText(["ex02", "ex01"]);
  });

  test("lector sees empty state alert when no students have submitted results", async ({
    teacherPage,
  }) => {
    await setupCourseApiMocks(teacherPage, {
      ...mockCourseStatistics,
      students: [],
    });

    const statsPage = new CourseStatisticsPage(teacherPage);
    await statsPage.goto(1);

    await expect(statsPage.emptyStateAlert).toBeVisible();
    await expect(statsPage.emptyStateAlert).toContainText(
      "No student test results have been submitted for this course in this period yet.",
    );
    await expect(statsPage.studentRows).toHaveCount(0);
  });

  test("student navigating directly to course statistics is redirected away", async ({
    studentPage,
  }) => {
    await setupCourseApiMocks(studentPage);

    const statsPage = new CourseStatisticsPage(studentPage);
    await statsPage.goto(1);

    await expect(studentPage).not.toHaveURL(/\/courses\/1\/statistics/);
    await expect(statsPage.studentRows).toHaveCount(0);
  });
});
