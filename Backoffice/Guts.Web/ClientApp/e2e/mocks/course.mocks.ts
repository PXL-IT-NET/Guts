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

export const mockCourseStatistics = {
  courseId: 1,
  courseCode: "NET-ADV",
  courseName: ".NET Advanced",
  totalAssignments: 4,
  totalTests: 10,
  students: [
    {
      studentId: 1,
      firstName: "Bob",
      lastName: "Zeta",
      fullName: "Bob Zeta",
      assignmentsWorkedOn: 2,
      totalPassedTests: 4,
      totalTests: 10,
      completionPercentage: 40,
      chapters: [
        {
          chapterId: 30,
          code: "ch01",
          description: "Zeta Chapter",
          assignments: [
            {
              assignmentId: 301,
              code: "ex01",
              description: "Second Exercise",
              numberOfPassedTests: 1,
              numberOfTests: 3,
            },
            {
              assignmentId: 302,
              code: "ex02",
              description: "First Exercise",
              numberOfPassedTests: 2,
              numberOfTests: 2,
            },
          ],
        },
        {
          chapterId: 20,
          code: "ch02",
          description: "",
          assignments: [
            {
              assignmentId: 201,
              code: "ex09",
              description: "",
              numberOfPassedTests: 0,
              numberOfTests: 3,
            },
            {
              assignmentId: 202,
              code: "ex03",
              description: "Alpha Exercise",
              numberOfPassedTests: 1,
              numberOfTests: 2,
            },
          ],
        },
        {
          chapterId: 10,
          code: "ch03",
          description: "Alpha Chapter",
          assignments: [],
        },
      ],
    },
    {
      studentId: 2,
      firstName: "Alice",
      lastName: "Alpha",
      fullName: "Alice Alpha",
      assignmentsWorkedOn: 4,
      totalPassedTests: 10,
      totalTests: 10,
      completionPercentage: 100,
      chapters: [
        {
          chapterId: 30,
          code: "ch01",
          description: "Zeta Chapter",
          assignments: [
            {
              assignmentId: 301,
              code: "ex01",
              description: "Second Exercise",
              numberOfPassedTests: 3,
              numberOfTests: 3,
            },
            {
              assignmentId: 302,
              code: "ex02",
              description: "First Exercise",
              numberOfPassedTests: 2,
              numberOfTests: 2,
            },
          ],
        },
        {
          chapterId: 20,
          code: "ch02",
          description: "",
          assignments: [
            {
              assignmentId: 201,
              code: "ex09",
              description: "",
              numberOfPassedTests: 3,
              numberOfTests: 3,
            },
            {
              assignmentId: 202,
              code: "ex03",
              description: "Alpha Exercise",
              numberOfPassedTests: 2,
              numberOfTests: 2,
            },
          ],
        },
        {
          chapterId: 10,
          code: "ch03",
          description: "Alpha Chapter",
          assignments: [],
        },
      ],
    },
  ],
};

/**
 * Registers network route mocks for courses, periods, chapters, and exercise summaries.
 */
export async function setupCourseApiMocks(
  page: Page,
  courseStatisticsOverride?: typeof mockCourseStatistics,
) {
  const courseStatistics = courseStatisticsOverride ?? mockCourseStatistics;
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

    if (url.includes("/api/courses/1/statistics")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(courseStatistics),
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

