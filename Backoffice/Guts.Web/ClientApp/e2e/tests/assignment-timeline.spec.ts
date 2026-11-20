import { test, expect } from "../fixtures/auth.fixture";
import { setupCourseApiMocks } from "../mocks/course.mocks";

const latestDetailMock = {
  assignmentId: 101,
  code: "ex1",
  description: "Exercise 1: Calculator",
  topicCode: "ch1",
  courseName: ".NET Advanced",
  courseId: 1,
  tests: [],
  testResults: [
    { testName: "Add_ShouldReturnSum", passed: true, runned: true, message: "" },
    {
      testName: "Divide_ByZero_ShouldThrow",
      passed: true,
      runned: true,
      message: "",
    },
  ],
  firstRun: "2026-11-20T10:00:00Z",
  lastRun: "2026-11-20T10:30:00Z",
  numberOfRuns: 3,
  solutionFiles: [
    {
      filePath: "Calculator.cs",
      content: "// v3: Add + Divide implemented",
      modifyDateTime: "2026-11-20T10:30:00Z",
    },
  ],
};

const run2DetailMock = {
  ...latestDetailMock,
  lastRun: "2026-11-20T10:15:00Z",
  numberOfRuns: 2,
  testResults: [
    { testName: "Add_ShouldReturnSum", passed: true, runned: true, message: "" },
    {
      testName: "Divide_ByZero_ShouldThrow",
      passed: false,
      runned: true,
      message: "Expected DivideByZeroException",
    },
  ],
  solutionFiles: [
    {
      filePath: "Calculator.cs",
      content: "// v2: refactored Add, Divide still missing",
      modifyDateTime: "2026-11-20T10:15:00Z",
    },
  ],
};

const run1DetailMock = {
  ...latestDetailMock,
  lastRun: "2026-11-20T10:00:00Z",
  numberOfRuns: 1,
  testResults: [
    { testName: "Add_ShouldReturnSum", passed: true, runned: true, message: "" },
    {
      testName: "Divide_ByZero_ShouldThrow",
      passed: false,
      runned: true,
      message: "NotImplementedException",
    },
  ],
  solutionFiles: [
    {
      filePath: "Calculator.cs",
      content: "// v1: initial Add implementation",
      modifyDateTime: "2026-11-20T10:00:00Z",
    },
  ],
};

const timelineMock = {
  assignmentId: 101,
  totalTestsCount: 2,
  runs: [
    {
      testRunId: 1001,
      runNumber: 1,
      createDateTimeUtc: "2026-11-20T10:00:00Z",
      passedTestsCount: 1,
      failedTestsCount: 1,
      totalTestsCount: 2,
      testResultsChanged: true,
      solutionFilesChanged: true,
      testResultsDelta: 0,
    },
    {
      testRunId: 1002,
      runNumber: 2,
      createDateTimeUtc: "2026-11-20T10:15:00Z",
      passedTestsCount: 1,
      failedTestsCount: 1,
      totalTestsCount: 2,
      testResultsChanged: false,
      solutionFilesChanged: true,
      testResultsDelta: 0,
    },
    {
      testRunId: 1003,
      runNumber: 3,
      createDateTimeUtc: "2026-11-20T10:30:00Z",
      passedTestsCount: 2,
      failedTestsCount: 0,
      totalTestsCount: 2,
      testResultsChanged: true,
      solutionFilesChanged: true,
      testResultsDelta: 1,
    },
  ],
};

test.describe("Assignment Test Run Timeline", () => {
  test("loads timeline asynchronously, scrubs back in time, preserves expanded source file, caches snapshots, and toggles scale", async ({
    studentPage,
  }) => {
    await setupCourseApiMocks(studentPage);

    let detailRequestCount = 0;
    let releaseTimeline!: () => void;
    const timelineGate = new Promise<void>((resolve) => {
      releaseTimeline = resolve;
    });

    await studentPage.route("**/api/assignments/101/foruser/202/timeline**", async (route) => {
      await timelineGate;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(timelineMock),
      });
    });

    await studentPage.route("**/api/assignments/101/foruser/202**", async (route) => {
      const url = route.request().url();
      if (url.includes("/timeline")) {
        await route.fallback();
        return;
      }
      detailRequestCount++;
      if (url.includes("2026-11-20T10:15:00Z")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(run2DetailMock),
        });
        return;
      }
      if (url.includes("2026-11-20T10:00:00Z")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(run1DetailMock),
        });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(latestDetailMock),
      });
    });

    await studentPage.goto(
      "/courses/1/chapters/ch1/testresults?assignmentId=101",
    );

    // Stage 1: Latest detail is rendered immediately while timeline shows loading state
    await expect(studentPage.getByText("Add_ShouldReturnSum")).toBeVisible();
    await expect(studentPage.getByTestId("timeline-loading")).toBeVisible();
    // Obsolete static run summary inputs and Status on filter are removed on detail view
    await expect(studentPage.locator("#firstrun")).toHaveCount(0);
    await expect(studentPage.locator("#lastrun")).toHaveCount(0);
    await expect(studentPage.locator("#numberofruns")).toHaveCount(0);
    await expect(studentPage.getByText("Status on")).toHaveCount(0);

    // Release delayed timeline response
    releaseTimeline();

    // Stage 2: Timeline card, canvas, readout, and legend appear
    await expect(studentPage.getByTestId("assignment-timeline")).toBeVisible();
    await expect(studentPage.getByTestId("timeline-loading")).toHaveCount(0);
    await expect(studentPage.getByTestId("timeline-readout")).toContainText(
      "Run #3 of 3",
    );
    await expect(studentPage.getByTestId("timeline-legend")).toContainText(
      "Source code only",
    );
    await expect(studentPage.getByTestId("historical-snapshot-banner")).toHaveCount(
      0,
    );

    // Expand Calculator.cs source panel
    await studentPage
      .locator("li.list-group-item", { hasText: "Calculator.cs" })
      .locator("button")
      .click();
    const codeTextarea = studentPage.locator("textarea#solutionFileContent");
    await expect(codeTextarea).toBeVisible();
    await expect(codeTextarea).toHaveValue("// v3: Add + Divide implemented");

    // Step back to Run #2 via Prev button
    await studentPage.getByTestId("timeline-prev-btn").click();
    await expect(
      studentPage.getByTestId("historical-snapshot-banner"),
    ).toContainText("Run #2 of 3");
    await expect(studentPage.getByTestId("timeline-readout")).toContainText(
      "Source code changed",
    );
    await expect(
      studentPage.getByText("Expected DivideByZeroException"),
    ).toBeVisible();
    // Expanded source file stays open and updates to v2
    await expect(codeTextarea).toBeVisible();
    await expect(codeTextarea).toHaveValue(
      "// v2: refactored Add, Divide still missing",
    );

    // Step back to Run #1 via keyboard ArrowLeft on the slider
    const slider = studentPage.getByTestId("timeline-slider");
    await slider.focus();
    await slider.press("ArrowLeft");
    await expect(
      studentPage.getByTestId("historical-snapshot-banner"),
    ).toContainText("Run #1 of 3");
    await expect(codeTextarea).toHaveValue("// v1: initial Add implementation");

    const requestsBeforeBackToLatest = detailRequestCount;
    // Click "Back to latest" -> restores v3 from snapshot cache without new HTTP call
    await studentPage.getByTestId("back-to-latest-btn").click();
    await expect(studentPage.getByTestId("historical-snapshot-banner")).toHaveCount(
      0,
    );
    await expect(codeTextarea).toHaveValue("// v3: Add + Divide implemented");
    expect(detailRequestCount).toBe(requestsBeforeBackToLatest);

    // Drag beam with mouse pointer across Chart.js canvas to the left edge (Run #1)
    const chartBox = await studentPage
      .getByTestId("timeline-chart-area")
      .boundingBox();
    expect(chartBox).not.toBeNull();
    if (chartBox) {
      const startX = chartBox.x + chartBox.width * 0.85;
      const endX = chartBox.x + chartBox.width * 0.1;
      const midY = chartBox.y + chartBox.height * 0.5;
      await studentPage.mouse.move(startX, midY);
      await studentPage.mouse.down();
      await studentPage.mouse.move(endX, midY, { steps: 5 });
      await studentPage.mouse.up();
      await expect(
        studentPage.getByTestId("historical-snapshot-banner"),
      ).toContainText("Run #1 of 3");
      await expect(codeTextarea).toHaveValue("// v1: initial Add implementation");
      // Return to latest again
      await studentPage.getByTestId("back-to-latest-btn").click();
      await expect(
        studentPage.getByTestId("historical-snapshot-banner"),
      ).toHaveCount(0);
    }

    // Toggle Sequence / Real-time X-axis scale
    const realtimeBtn = studentPage.getByTestId("timeline-scale-realtime");
    await realtimeBtn.click();
    await expect(realtimeBtn).toHaveAttribute("aria-pressed", "true");
    const sequenceBtn = studentPage.getByTestId("timeline-scale-sequence");
    await sequenceBtn.click();
    await expect(sequenceBtn).toHaveAttribute("aria-pressed", "true");
  });

  test("hides timeline when assignment has only 1 run and handles timeline failure gracefully", async ({
    studentPage,
  }) => {
    await setupCourseApiMocks(studentPage);

    let timelineCalled = false;
    await studentPage.route("**/api/assignments/101/foruser/202/timeline**", async (route) => {
      timelineCalled = true;
      await route.fulfill({
        status: 500,
        contentType: "application/json",
        body: JSON.stringify({ message: "Server error" }),
      });
    });

    await studentPage.route("**/api/assignments/101/foruser/202**", async (route) => {
      if (route.request().url().includes("/timeline")) {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ ...run1DetailMock, numberOfRuns: 1 }),
      });
    });

    await studentPage.goto(
      "/courses/1/chapters/ch1/testresults?assignmentId=101",
    );

    await expect(studentPage.getByText("Add_ShouldReturnSum")).toBeVisible();
    await expect(studentPage.getByTestId("assignment-timeline")).toHaveCount(0);
    await expect(studentPage.getByTestId("timeline-loading")).toHaveCount(0);
    expect(timelineCalled).toBe(false);

    // Now simulate numberOfRuns = 3 with failing timeline API -> detail stays intact + non-blocking warning
    await studentPage.unroute("**/api/assignments/101/foruser/202**");
    await studentPage.route("**/api/assignments/101/foruser/202**", async (route) => {
      if (route.request().url().includes("/timeline")) {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(latestDetailMock),
      });
    });

    await studentPage.reload();
    await expect(studentPage.getByText("Add_ShouldReturnSum")).toBeVisible();
    await expect(studentPage.getByTestId("timeline-error")).toBeVisible();
    await expect(studentPage.getByTestId("assignment-timeline")).toHaveCount(0);
  });
});
