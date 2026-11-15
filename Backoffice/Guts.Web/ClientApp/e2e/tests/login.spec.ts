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

  test("should redirect locked lector to verification-sent page, allow resend, and unlock via verify-lector link", async ({
    page,
  }) => {
    let resent = false;
    let verified = false;

    await page.route("**/api/auth/token", async (route) => {
      await route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          code: "LectorVerificationRequired",
          userId: 42,
          email: "locked.lector@pxl.be",
          resendToken: "resend-tok-1",
          message: "Annual lector verification required",
        }),
      });
    });

    await page.route("**/api/auth/resend-lector-verification", async (route) => {
      resent = true;
      await route.fulfill({ status: 200, body: "{}" });
    });

    await page.route("**/api/auth/verifylector", async (route) => {
      verified = true;
      await route.fulfill({ status: 200, body: "{}" });
    });

    await loginPage.login("locked.lector@pxl.be", "Password123!");

    await expect(page).toHaveURL(/.*\/lector-verification-sent/);
    await expect(
      page.locator("[data-testid='verification-email-target']"),
    ).toContainText("locked.lector@pxl.be");

    await page.locator("[data-testid='resend-verification-btn']").click();
    await expect.poll(() => resent).toBe(true);
    await expect(
      page.locator("[data-testid='resend-success-alert']"),
    ).toBeVisible();

    await page.goto("/verify-lector?userId=42&token=verify-tok-1");
    await expect.poll(() => verified).toBe(true);
    await expect(
      page.locator("[data-testid='verify-lector-status']"),
    ).toContainText("unlocked");

    await page.locator("[data-testid='verify-lector-login-link']").click();
    await expect(page).toHaveURL(/.*\/login/);
  });
});

