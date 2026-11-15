import { test, expect } from "../fixtures/auth.fixture";

test.describe("Admin Chunked Deletion with Progress Bar", () => {
  test("course deletion locks modal, polls progress bar to completion, and refreshes list", async ({
    adminPage,
  }) => {
    const courses = [
      { id: 1, code: "NET1", name: ".NET Essentials" },
      { id: 2, code: "JAVA1", name: "Java Basics" },
    ];
    let pollCount = 0;

    await adminPage.route("**/api/courses**", async (route) => {
      const request = route.request();
      if (request.method() === "GET") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(courses),
        });
      } else if (request.method() === "DELETE") {
        await route.fulfill({
          status: 202,
          contentType: "application/json",
          body: JSON.stringify({
            jobId: "course-job-1",
            targetType: "Course",
            targetId: 2,
            targetLabel: "JAVA1 - Java Basics",
            status: "Pending",
            totalRows: 250,
            deletedRows: 0,
            percentage: 0,
            currentPhase: "Queued for deletion",
            createdAtUtc: "2026-11-15T12:00:00Z",
          }),
        });
      }
    });

    await adminPage.route("**/api/deletion-jobs/course-job-1", async (route) => {
      pollCount += 1;
      if (pollCount === 1) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            jobId: "course-job-1",
            targetType: "Course",
            targetId: 2,
            targetLabel: "JAVA1 - Java Basics",
            status: "InProgress",
            totalRows: 250,
            deletedRows: 100,
            percentage: 40,
            currentPhase: "Deleting test results",
            createdAtUtc: "2026-11-15T12:00:00Z",
          }),
        });
      } else {
        const idx = courses.findIndex((c) => c.id === 2);
        if (idx >= 0) {
          courses.splice(idx, 1);
        }
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            jobId: "course-job-1",
            targetType: "Course",
            targetId: 2,
            targetLabel: "JAVA1 - Java Basics",
            status: "Completed",
            totalRows: 250,
            deletedRows: 250,
            percentage: 100,
            currentPhase: "Completed",
            createdAtUtc: "2026-11-15T12:00:00Z",
          }),
        });
      }
    });

    await adminPage.goto("/admin/courses");
    await expect(adminPage.locator("tbody tr")).toHaveCount(2);

    await adminPage
      .locator("tbody tr")
      .nth(1)
      .getByRole("button", { name: "Delete" })
      .click();
    await expect(adminPage.getByText("Danger: Delete Course")).toBeVisible();
    await adminPage
      .locator("[data-testid='danger-confirm-input']")
      .fill("JAVA1");
    await adminPage
      .getByRole("button", { name: "Confirm Delete Course" })
      .click();

    const progressBar = adminPage.locator("[data-testid='deletion-progress-bar']");
    await expect(progressBar).toBeVisible();
    await expect(progressBar).toHaveAttribute("value", "40");
    await expect(
      adminPage.locator("[data-testid='deletion-progress-status']"),
    ).toContainText("Deleting test results");
    await expect(
      adminPage.locator("[data-testid='deletion-progress-details']"),
    ).toContainText("100 / 250 rows removed");

    // Modal controls remain locked while deletion is running
    await expect(adminPage.getByRole("button", { name: "Cancel" })).toBeDisabled();
    await expect(adminPage.getByRole("button", { name: "Close" })).toBeDisabled();
    await adminPage.keyboard.press("Escape");
    await expect(progressBar).toBeVisible();

    // Completes on next poll, closes modal, refreshes course list
    await expect(progressBar).toBeHidden();
    await expect(adminPage.locator("tbody tr")).toHaveCount(1);
    await expect(adminPage.getByText(/deleted\./i)).toBeVisible();
  });

  test("period deletion shows progress bar and handles job failure", async ({
    adminPage,
  }) => {
    await adminPage.route("**/api/periods**", async (route) => {
      const request = route.request();
      if (request.method() === "GET") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([
            {
              id: 1,
              description: "2020-2021",
              from: "2020-09-01T00:00:00Z",
              until: "2021-06-30T00:00:00Z",
              isActive: false,
            },
          ]),
        });
      } else if (request.method() === "DELETE") {
        await route.fulfill({
          status: 202,
          contentType: "application/json",
          body: JSON.stringify({
            jobId: "period-job-fail",
            targetType: "Period",
            targetId: 1,
            targetLabel: "2020-2021",
            status: "Pending",
            totalRows: 80,
            deletedRows: 0,
            percentage: 0,
            currentPhase: "Queued for deletion",
            createdAtUtc: "2026-11-15T12:00:00Z",
          }),
        });
      }
    });

    await adminPage.route("**/api/deletion-jobs/period-job-fail", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          jobId: "period-job-fail",
          targetType: "Period",
          targetId: 1,
          targetLabel: "2020-2021",
          status: "Failed",
          totalRows: 80,
          deletedRows: 10,
          percentage: 12,
          currentPhase: "Failed",
          errorMessage: "Database connection interrupted",
          createdAtUtc: "2026-11-15T12:00:00Z",
        }),
      });
    });

    await adminPage.goto("/admin/periods");
    await adminPage
      .locator("tbody tr")
      .first()
      .getByRole("button", { name: "Delete" })
      .click();
    await adminPage
      .locator("[data-testid='danger-confirm-input']")
      .fill("2020-2021");
    await adminPage
      .getByRole("button", { name: "Confirm Delete Period" })
      .click();

    await expect(
      adminPage.getByText("Database connection interrupted"),
    ).toBeVisible();
    await expect(
      adminPage.locator("[data-testid='deletion-progress-bar']"),
    ).toBeHidden();
  });

  test("reload recovers active course deletion job and completes cleanly", async ({
    adminPage,
  }) => {
    const courses = [{ id: 1, code: "NET1", name: ".NET Essentials" }];
    let activeChecks = 0;
    let pollCount = 0;

    await adminPage.route("**/api/courses**", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(courses),
      });
    });

    await adminPage.route("**/api/deletion-jobs/active**", async (route) => {
      activeChecks += 1;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          activeChecks === 1
            ? [
                {
                  jobId: "course-resume-1",
                  targetType: "Course",
                  targetId: 1,
                  targetLabel: "NET1 - .NET Essentials",
                  status: "InProgress",
                  totalRows: 100,
                  deletedRows: 65,
                  percentage: 65,
                  currentPhase: "Deleting chapters",
                  createdAtUtc: "2026-11-15T12:00:00Z",
                },
              ]
            : [],
        ),
      });
    });

    await adminPage.route("**/api/deletion-jobs/course-resume-1", async (route) => {
      pollCount += 1;
      if (pollCount === 1) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            jobId: "course-resume-1",
            targetType: "Course",
            targetId: 1,
            targetLabel: "NET1 - .NET Essentials",
            status: "InProgress",
            totalRows: 100,
            deletedRows: 85,
            percentage: 85,
            currentPhase: "Deleting chapters",
            createdAtUtc: "2026-11-15T12:00:00Z",
          }),
        });
      } else {
        courses.length = 0;
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            jobId: "course-resume-1",
            targetType: "Course",
            targetId: 1,
            targetLabel: "NET1 - .NET Essentials",
            status: "Completed",
            totalRows: 100,
            deletedRows: 100,
            percentage: 100,
            currentPhase: "Completed",
            createdAtUtc: "2026-11-15T12:00:00Z",
          }),
        });
      }
    });

    await adminPage.goto("/admin/courses");

    await expect(adminPage.getByText(/Deleting Course: NET1/)).toBeVisible();
    await expect(
      adminPage.locator("[data-testid='deletion-progress-bar']"),
    ).toHaveAttribute("value", "85");
    await expect(adminPage.locator("tbody tr")).toHaveCount(0);
    await expect(adminPage.getByText(/deleted\./i)).toBeVisible();
  });
});
