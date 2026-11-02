import { Page } from "@playwright/test";

export const mockActivePeriod = [
  {
    id: 1,
    description: "2025-2026",
    from: "2025-09-01T00:00:00",
    until: "2026-08-31T23:59:59",
    isActive: true,
  },
];

export const mockCourseList = [
  {
    id: 1,
    code: "NET-ADV",
    name: ".NET Advanced",
  },
];

export const mockCourseContents = {
  id: 1,
  code: "NET-ADV",
  name: ".NET Advanced",
  chapters: [
    {
      id: 11,
      code: "ch1",
      description: "Chapter 1: C# Internals",
      assignments: [
        {
          assignmentId: 101,
          code: "ex1",
          description: "Exercise 1: Calculator",
          tests: [],
        },
      ],
    },
  ],
  projects: [],
};

export const mockChapterDetails = {
  id: 11,
  code: "ch1",
  description: "Chapter 1: C# Internals",
  assignments: [
    {
      assignmentId: 101,
      code: "ex1",
      description: "Exercise 1: Calculator",
      tests: [],
    },
  ],
  exercises: [
    {
      assignmentId: 101,
      code: "ex1",
      description: "Exercise 1: Calculator",
      tests: [],
    },
  ],
  users: [
    {
      id: 202,
      fullName: "Student Guts",
    },
  ],
};

export const mockChapterSummary = {
  id: 11,
  code: "ch1",
  description: "Chapter 1: C# Internals",
  totalGreenTests: 4,
  totalTests: 5,
  assignmentSummaries: [
    {
      assignmentId: 101,
      code: "ex1",
      description: "Exercise 1: Calculator",
      numberOfTests: 5,
      numberOfPassedTests: 4,
      numberOfFailedTests: 1,
      numberOfUsers: 1,
      tests: [],
    },
  ],
};

export const mockChapterStatistics = {
  id: 11,
  code: "ch1",
  description: "Chapter 1: C# Internals",
  assignments: [],
  assignmentStatistics: [],
};

/**
 * Registers network route mocks for courses, periods, chapters, and exercise summaries.
 */
export async function setupCourseApiMocks(page: Page) {
  await page.route("**/api/periods", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockActivePeriod),
    });
  });

  await page.route("**/api/courses", async (route) => {
    if (route.request().url().includes("/api/courses/")) {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockCourseList),
    });
  });

  await page.route("**/api/courses/1**", async (route) => {
    const url = route.request().url();

    if (url.includes("/gamification/config")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isGamificationEnabled: false }),
      });
      return;
    }

    if (url.includes("/chapters/ch1/statistics")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockChapterStatistics),
      });
      return;
    }

    if (url.includes("/chapters/ch1/users/")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockChapterSummary),
      });
      return;
    }

    if (url.includes("/chapters/ch1")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockChapterDetails),
      });
      return;
    }

    // Default: getCourseContentsById(1)
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockCourseContents),
    });
  });
}

