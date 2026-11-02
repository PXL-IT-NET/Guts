import { test as base, Page } from "@playwright/test";
import {
  MockUserProfile,
  mockTeacherProfile,
  mockStudentProfile,
} from "../mocks/user.mocks";

/**
 * Injects a valid token into localStorage before page scripts execute,
 * and sets up route interceptions for user profile, periods, and courses.
 */
export async function authenticatePage(
  page: Page,
  profile: MockUserProfile,
  token = "mock-jwt-token",
) {
  // Inject token into localStorage before any page script executes
  await page.addInitScript(
    ({ currentTokenKey, tokenValue }) => {
      window.localStorage.setItem(
        currentTokenKey,
        JSON.stringify({
          token: tokenValue,
          expiration: new Date(Date.now() + 86400000).toISOString(),
        }),
      );
    },
    { currentTokenKey: "currentToken", tokenValue: token },
  );

  // Intercept profile endpoint to return the desired user profile
  await page.route("**/api/users/current/profile", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(profile),
    });
  });

  // Provide safe defaults for background calls
  await page.route("**/api/periods", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify([]),
    });
  });

  await page.route("**/api/courses", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify([]),
    });
  });
}

type AuthFixtures = {
  teacherPage: Page;
  studentPage: Page;
  loginAs: (profile: MockUserProfile) => Promise<Page>;
};

export const test = base.extend<AuthFixtures>({
  teacherPage: async ({ page }, use) => {
    await authenticatePage(page, mockTeacherProfile);
    await use(page);
  },

  studentPage: async ({ page }, use) => {
    await authenticatePage(page, mockStudentProfile);
    await use(page);
  },

  loginAs: async ({ page }, use) => {
    const helper = async (profile: MockUserProfile) => {
      await authenticatePage(page, profile);
      return page;
    };
    await use(helper);
  },
});

export { expect } from "@playwright/test";

