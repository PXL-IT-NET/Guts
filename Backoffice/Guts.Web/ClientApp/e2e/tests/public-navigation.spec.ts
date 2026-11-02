import { test, expect } from "@playwright/test";

test.describe("Public Navigation", () => {
  test("should redirect root to /home and display navigation navbar", async ({
    page,
  }) => {
    await page.goto("/");
    await expect(page).toHaveURL(/.*\/home/);

    const brand = page.locator(".navbar-brand");
    await expect(brand).toBeVisible();
    await expect(brand).toContainText("Guts");
  });

  test("should navigate to teacher documentation concepts page", async ({
    page,
  }) => {
    await page.goto("/teacher-docs/concepts");
    await expect(page).toHaveURL(/.*\/teacher-docs\/concepts/);

    const heading = page.getByRole("heading", {
      name: "Monitor your students' progress with GUTS",
    });
    await expect(heading).toBeVisible();

    await expect(page.getByText("Teacher docs", { exact: true })).toBeVisible();
    await expect(page.getByText("Concepts", { exact: true })).toBeVisible();
  });

  test("should load the login page with form elements", async ({ page }) => {
    await page.goto("/login");
    await expect(page).toHaveURL(/.*\/login/);

    await expect(
      page.getByRole("heading", { name: "Login", level: 2 }),
    ).toBeVisible();
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Password")).toBeVisible();
    await expect(page.getByRole("button", { name: /^Login$/i })).toBeVisible();
  });
});

