import { test, expect } from "../fixtures/auth.fixture";

test.describe("Role-Based Access Control", () => {
  test("teacher is recognized and has access to teacher documentation in sidebar", async ({
    teacherPage,
  }) => {
    await teacherPage.goto("/home");

    // Navbar displays the teacher's email
    const userMenu = teacherPage.locator("header .bi-person").locator("..");
    await expect(userMenu).toContainText("teacher.guts@pxl.be");

    // Anonymous log in/register buttons are hidden
    await expect(
      teacherPage.getByRole("button", { name: "Log in" }),
    ).toHaveCount(0);
    await expect(
      teacherPage.getByRole("button", { name: "Register" }),
    ).toHaveCount(0);

    // Sidebar includes the Teacher docs section
    const teacherDocsNav = teacherPage
      .locator("#sidebarMenu")
      .getByText("Teacher docs");
    await expect(teacherDocsNav).toBeVisible();

    // Clicking user menu shows authenticated options: Change password and Afmelden (Logout)
    await userMenu.click();
    await expect(
      teacherPage.getByRole("link", { name: /change password/i }),
    ).toBeVisible();
    await expect(
      teacherPage.getByRole("button", { name: /afmelden/i }),
    ).toBeVisible();
  });

  test("student is recognized and does NOT have Teacher docs in sidebar", async ({
    studentPage,
  }) => {
    await studentPage.goto("/home");

    // Navbar displays the student's email
    const userMenu = studentPage.locator("header .bi-person").locator("..");
    await expect(userMenu).toContainText("student.guts@student.pxl.be");

    // Anonymous log in/register buttons are hidden
    await expect(
      studentPage.getByRole("button", { name: "Log in" }),
    ).toHaveCount(0);

    // Sidebar does NOT include Teacher docs for a student
    const teacherDocsNav = studentPage
      .locator("#sidebarMenu")
      .getByText("Teacher docs");
    await expect(teacherDocsNav).toHaveCount(0);
  });

  test("custom authenticated profile can be loaded dynamically with loginAs", async ({
    loginAs,
  }) => {
    const page = await loginAs({
      id: 999,
      email: "custom.admin@pxl.be",
      roles: ["lector", "admin"],
      teams: [],
      isAuthenticated: true,
    });

    await page.goto("/home");

    const userMenu = page.locator("header .bi-person").locator("..");
    await expect(userMenu).toContainText("custom.admin@pxl.be");
  });
});

