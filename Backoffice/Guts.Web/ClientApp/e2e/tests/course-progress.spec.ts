import { test, expect } from "../fixtures/auth.fixture";
import { setupCourseApiMocks } from "../mocks/course.mocks";
import { CoursePage } from "../pages/course.po";

test.describe("Course & Exercise Progress Dashboard", () => {
  test("student can view course, chapters, and exercise test results", async ({
    studentPage,
  }) => {
    await setupCourseApiMocks(studentPage);

    const coursePage = new CoursePage(studentPage);
    await coursePage.goto(1);

    // Course heading displays course name and code
    await expect(coursePage.courseHeading).toContainText(".NET Advanced");
    await expect(coursePage.courseHeading).toContainText("(NET-ADV)");

    // Test outcomes card displays aggregate passed/total test count
    await expect(coursePage.testOutcomesHeader).toBeVisible();
    await expect(coursePage.testOutcomesHeader).toContainText(
      "(4 / 5 tests)",
    );

    // Exercise summary card shows exercise description and ratio
    await expect(coursePage.assignmentTitle).toHaveText("Exercise 1: Calculator");
    await expect(coursePage.assignmentTestRatio).toContainText("4 / 5 tests");

    // Student should NOT see management buttons
    await expect(coursePage.courseStatisticsButton).toHaveCount(0);
    await expect(coursePage.chapterSettingsTab).toHaveCount(0);
  });

  test("teacher sees management options for course statistics and chapter settings", async ({
    teacherPage,
  }) => {
    await setupCourseApiMocks(teacherPage);

    const coursePage = new CoursePage(teacherPage);
    await coursePage.goto(1);

    // Course heading is rendered
    await expect(coursePage.courseHeading).toContainText(".NET Advanced");

    // Teacher has access to Course statistics button
    await expect(coursePage.courseStatisticsButton).toBeVisible();

    // Teacher has access to Chapter settings tab
    await expect(coursePage.chapterSettingsTab).toBeVisible();
  });
});

