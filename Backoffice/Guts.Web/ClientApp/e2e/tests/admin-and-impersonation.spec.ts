import { test, expect } from "../fixtures/auth.fixture";
import { mockStudentProfile } from "../mocks/user.mocks";

test.describe("Admin Role, CRUD, User Cleanup & Impersonation", () => {
  test("admin sees Admin menu in sidebar; student does not", async ({
    adminPage,
  }) => {
    await adminPage.goto("/home");
    const adminNav = adminPage.locator("#sidebarMenu").getByText("Admin");
    await expect(adminNav).toBeVisible();
  });

  test("student cannot access /admin/periods and is redirected", async ({
    studentPage,
  }) => {
    await studentPage.goto("/admin/periods");
    await expect(studentPage).toHaveURL(/\/home/);
  });

  test("admin periods page disables delete for active period and shows danger warning for past period", async ({
    adminPage,
  }) => {
    await adminPage.route("**/api/periods", async (route) => {
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
          {
            id: 2,
            description: "Current",
            from: "2026-09-01T00:00:00Z",
            until: "2027-06-30T00:00:00Z",
            isActive: true,
          },
        ]),
      });
    });

    await adminPage.goto("/admin/periods");

    const rows = adminPage.locator("tbody tr");
    await expect(rows).toHaveCount(2);

    // Active period delete button is disabled
    await expect(rows.nth(1).getByRole("button", { name: "Delete" })).toBeDisabled();

    // Past period delete button opens confirmation modal requiring exact description
    await rows.nth(0).getByRole("button", { name: "Delete" }).click();
    await expect(adminPage.getByText("Danger: Delete Period")).toBeVisible();
    const confirmBtn = adminPage.getByRole("button", {
      name: "Confirm Delete Period",
    });
    await expect(confirmBtn).toBeDisabled();
    await adminPage
      .locator("[data-testid='danger-confirm-input']")
      .fill("2020-2021");
    await expect(confirmBtn).toBeEnabled();
  });

  test("admin users page shows extra teacher warning when deleting an Orphaned lector", async ({
    adminPage,
  }) => {
    await adminPage.route("**/api/admin/users**", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          {
            id: 10,
            email: "old.teacher@pxl.be",
            firstName: "Old",
            lastName: "Teacher",
            roles: ["lector"],
            isLinkedToCourse: false,
            canDelete: true,
            canImpersonate: true,
            canPromoteToAdmin: true,
            canDemoteFromAdmin: false,
          },
        ]),
      });
    });

    await adminPage.goto("/admin/users");
    await adminPage.getByRole("button", { name: "Delete" }).click();

    await expect(
      adminPage.locator("[data-testid='teacher-delete-warning']"),
    ).toBeVisible();
  });

  test("lector can impersonate a student in read-only mode and stop impersonating", async ({
    teacherPage,
  }) => {
    let impersonating = false;

    await teacherPage.unroute("**/api/users/current/profile");
    await teacherPage.route("**/api/users/current/profile", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          impersonating
            ? { ...mockStudentProfile, isImpersonating: true }
            : {
                id: 101,
                email: "teacher.guts@pxl.be",
                roles: ["lector"],
                teams: [],
                isAuthenticated: true,
                isImpersonating: false,
              },
        ),
      });
    });

    await teacherPage.route("**/api/impersonation/users**", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          {
            id: 202,
            email: "student.guts@student.pxl.be",
            firstName: "Stu",
            lastName: "Dent",
            roles: ["student"],
          },
        ]),
      });
    });

    await teacherPage.route("**/api/impersonation/202", async (route) => {
      impersonating = true;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          token: "impersonation-jwt-token",
          expiration: new Date(Date.now() + 3600000).toISOString(),
        }),
      });
    });

    await teacherPage.goto("/impersonate");
    await teacherPage.getByRole("button", { name: "Impersonate" }).click();

    const banner = teacherPage.locator("[data-testid='impersonation-banner']");
    await expect(banner).toBeVisible();
    await expect(banner).toContainText("Read-Only");
    await expect(banner).toContainText("student.guts@student.pxl.be");

    impersonating = false;
    await banner.getByRole("button", { name: "Stop Impersonating" }).click();
    await expect(banner).toHaveCount(0);
  });
});
