import { Locator, Page } from "@playwright/test";

export class LoginPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly emailRequiredError: Locator;
  readonly passwordRequiredError: Locator;
  readonly serverErrorAlert: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole("heading", { name: "Login", level: 2 });
    this.emailInput = page.getByLabel("Email");
    this.passwordInput = page.getByLabel("Password");
    this.submitButton = page.getByRole("button", { name: /^Login$/i });
    this.emailRequiredError = page.locator(".invalid-feedback", {
      hasText: "Email is required",
    });
    this.passwordRequiredError = page.locator(".invalid-feedback", {
      hasText: "Password is required",
    });
    this.serverErrorAlert = page.locator(".alert-danger");
  }

  async goto() {
    await this.page.goto("/login");
  }

  async login(email?: string, password?: string) {
    if (email !== undefined) {
      await this.emailInput.fill(email);
    }
    if (password !== undefined) {
      await this.passwordInput.fill(password);
    }
    await this.submitButton.click();
  }
}

