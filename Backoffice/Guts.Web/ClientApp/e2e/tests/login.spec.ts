import { test, expect } from "@playwright/test";
import { LoginPage } from "../pages/login.po";

test.describe("Login Flow", () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test("should show validation errors when submitting empty form", async () => {
    await loginPage.submitButton.click();

    await expect(loginPage.emailRequiredError).toBeVisible();
    await expect(loginPage.passwordRequiredError).toBeVisible();
  });

  test("should show error alert when API returns invalid credentials (401)", async ({
    page,
  }) => {
    await page.route("**/api/auth/token", async (route) => {
      await route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ message: "Invalid credentials" }),
      });
    });

    await loginPage.login("wrong@pxl.be", "WrongPassword123!");

    await expect(loginPage.serverErrorAlert).toBeVisible();
    await expect(loginPage.serverErrorAlert).toContainText(
      "Wrong email-password combination",
    );
  });

  test("should authenticate successfully and navigate home with valid response", async ({
    page,
  }) => {
    await page.route("**/api/auth/token", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          token: "mock-jwt-token",
        }),
      });
    });

    // Also mock user profile endpoint when logged in
    await page.route("**/api/users/current/profile", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          email: "teacher@pxl.be",
          firstName: "John",
          lastName: "Doe",
          roles: ["Teacher"],
        }),
      });
    });

    await loginPage.login("teacher@pxl.be", "CorrectPassword123!");

    await expect(page).toHaveURL(/.*\/home/);
  });
});

