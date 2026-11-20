import { test, expect } from "../fixtures/auth.fixture";

test.describe("Admin Scheduled Jobs (Quartz Dashboard)", () => {
  test("admin sees Scheduled Jobs in Admin menu and clicking requests a dashboard ticket", async ({
    adminPage,
  }) => {
    let ticketRequested = false;
    await adminPage.route("**/api/scheduled-jobs/dashboard-ticket", async (route) => {
      ticketRequested = true;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          loginUrl: "https://localhost:5001/api/scheduled-jobs/dashboard-login?ticket=e2e-ticket",
        }),
      });
    });

    await adminPage.addInitScript(() => {
      (window as unknown as { __openedUrl?: string }).__openedUrl = undefined;
      window.open = ((url?: string | URL) => {
        (window as unknown as { __openedUrl?: string }).__openedUrl = String(url ?? "");
        return null;
      }) as typeof window.open;
    });

    await adminPage.goto("/home");

    const adminToggle = adminPage.locator("#sidebarMenu").getByText("Admin", { exact: true });
    await expect(adminToggle).toBeVisible();
    await adminToggle.click();

    const scheduledJobsBtn = adminPage.locator("#sidebarMenu").getByRole("button", { name: /Scheduled Jobs/i });
    await expect(scheduledJobsBtn).toBeVisible();
    await scheduledJobsBtn.click();

    await expect.poll(() => ticketRequested).toBe(true);
    await expect
      .poll(() =>
        adminPage.evaluate(
          () => (window as unknown as { __openedUrl?: string }).__openedUrl,
        ),
      )
      .toBe("https://localhost:5001/api/scheduled-jobs/dashboard-login?ticket=e2e-ticket");
  });

  test("shows error notification when dashboard ticket request fails", async ({
    adminPage,
  }) => {
    await adminPage.route("**/api/scheduled-jobs/dashboard-ticket", async (route) => {
      await route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({ message: "Forbidden" }),
      });
    });

    await adminPage.goto("/home");
    await adminPage.locator("#sidebarMenu").getByText("Admin", { exact: true }).click();
    await adminPage.locator("#sidebarMenu").getByRole("button", { name: /Scheduled Jobs/i }).click();

    await expect(adminPage.locator(".toast-error")).toBeVisible();
  });

  test("student and teacher do not see Scheduled Jobs in sidebar", async ({
    studentPage,
    teacherPage,
  }) => {
    await studentPage.goto("/home");
    await expect(studentPage.locator("#sidebarMenu").getByText("Scheduled Jobs")).toHaveCount(0);

    await teacherPage.goto("/home");
    await expect(teacherPage.locator("#sidebarMenu").getByText("Scheduled Jobs")).toHaveCount(0);
  });
});
