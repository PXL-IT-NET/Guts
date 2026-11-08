import { Page, Route } from "@playwright/test";
import { mockActivePeriod, mockCourseContents, mockCourseList } from "./course.mocks";
import {
  ICourseGamificationConfigModel,
  IStudentGamificationProfileModel,
  IHallOfFameLeaderboardModel,
  ILeaderboardEntryModel,
  IClassGroupLeaderboardEntryModel,
  ICourseClassGroupModel,
} from "../../src/app/viewmodels/gamification.model";

export const mockGamificationConfigEnabled: ICourseGamificationConfigModel = {
  courseId: 1,
  isGamificationEnabled: true,
  isWeeklyStreakEnabled: true,
  isLeaderboardEnabled: true,
  isClassCompetitionEnabled: true,
  isTeamSynergyEnabled: true,
  isBadgesEnabled: true,
};

export const mockGamificationConfigDisabled: ICourseGamificationConfigModel = {
  courseId: 1,
  isGamificationEnabled: false,
  isWeeklyStreakEnabled: true,
  isLeaderboardEnabled: true,
  isClassCompetitionEnabled: true,
  isTeamSynergyEnabled: true,
  isBadgesEnabled: true,
};

export const mockClassGroups: ICourseClassGroupModel[] = [
  { id: 10, name: "1TIN-A", courseId: 1, periodId: 1 },
  { id: 20, name: "1TIN-B", courseId: 1, periodId: 1 },
];

export const mockStudentGamificationProfile: IStudentGamificationProfileModel = {
  userId: 202,
  firstName: "Student",
  lastName: "Guts",
  courseId: 1,
  periodId: 1,
  showInLeaderBoard: true,
  usePseudonym: true,
  pseudonym: "SyntaxSlayer",
  avatar: "CodeCat",
  classGroupId: 10,
  classGroupName: "1TIN-A",
  totalXp: 180,
  level: 3,
  levelTitle: "Logic Apprentice",
  currentWeeklyStreak: 2,
  bestWeeklyStreak: 4,
  isActiveThisWeek: true,
  totalGreenTests: 18,
  earnedBadges: [
    {
      badgeType: 1,
      name: "First Spark",
      description: "Made your very first test green in the course.",
      icon: "⚡",
      earnedAt: "2026-10-01T10:00:00Z",
    },
    {
      badgeType: 9,
      name: "On Fire",
      description: "Maintained a streak of 4 consecutive coding weeks.",
      icon: "🔥",
      earnedAt: "2026-10-20T12:00:00Z",
    },
  ],
};

export const mockInspectedStudentProfile: IStudentGamificationProfileModel = {
  userId: 303,
  firstName: "Bob",
  lastName: "Coder",
  courseId: 1,
  periodId: 1,
  showInLeaderBoard: true,
  usePseudonym: false,
  pseudonym: "BobCoder",
  avatar: "DevDragon",
  classGroupId: 10,
  classGroupName: "1TIN-A",
  totalXp: 280,
  level: 4,
  levelTitle: "Code Crafter",
  currentWeeklyStreak: 3,
  bestWeeklyStreak: 3,
  isActiveThisWeek: true,
  totalGreenTests: 28,
  earnedBadges: [
    {
      badgeType: 1,
      name: "First Spark",
      description: "Made your very first test green in the course.",
      icon: "⚡",
      earnedAt: "2026-10-01T10:00:00Z",
    },
  ],
};

export const mockHallOfFameStudentView: IHallOfFameLeaderboardModel = {
  currentTier: {
    tier: 7,
    name: "Diamond",
    icon: "💎",
    minRank: 16,
    maxRank: 35,
    isCurrentTier: true,
    studentCount: 15,
  },
  visibleTiers: [
    {
      tier: 6,
      name: "Gold",
      icon: "👑",
      minRank: 36,
      maxRank: 55,
      isCurrentTier: false,
      studentCount: 20,
    },
    {
      tier: 7,
      name: "Diamond",
      icon: "💎",
      minRank: 16,
      maxRank: 35,
      isCurrentTier: true,
      studentCount: 15,
    },
    {
      tier: 8,
      name: "Obsidian",
      icon: "🌋",
      minRank: 1,
      maxRank: 15,
      isCurrentTier: false,
      studentCount: 15,
    },
  ],
  allTiers: [],
  nextTargetStudent: {
    userId: 105,
    displayName: "AliceDev",
    avatar: "DevDragon",
    rank: 1,
    score: 195,
    xpNeededToOvertake: 16,
    tierName: "Diamond",
    tierIcon: "💎",
  },
  currentUserRank: 2,
  entries: [
    {
      rank: 1,
      userId: 105,
      displayName: "AliceDev",
      avatar: "DevDragon",
      classGroupName: "1TIN-A",
      score: 195,
      level: 3,
      weeklyStreak: 3,
      greenTestsCount: 20,
      isCurrentUser: false,
      tier: 7,
      tierName: "Diamond",
      tierIcon: "💎",
    },
    {
      rank: 2,
      userId: 202,
      displayName: "SyntaxSlayer",
      avatar: "CodeCat",
      classGroupName: "1TIN-A",
      score: 180,
      level: 3,
      weeklyStreak: 2,
      greenTestsCount: 18,
      isCurrentUser: true,
      tier: 7,
      tierName: "Diamond",
      tierIcon: "💎",
    },
  ],
  studentOptions: [],
};

export const mockHallOfFameTeacherView: IHallOfFameLeaderboardModel = {
  currentTier: null,
  visibleTiers: [],
  allTiers: [
    {
      tier: 8,
      name: "Obsidian",
      icon: "🌋",
      minRank: 1,
      maxRank: 15,
      isCurrentTier: false,
      studentCount: 15,
    },
    {
      tier: 7,
      name: "Diamond",
      icon: "💎",
      minRank: 16,
      maxRank: 35,
      isCurrentTier: false,
      studentCount: 15,
    },
  ],
  nextTargetStudent: null,
  currentUserRank: null,
  entries: [
    {
      rank: 1,
      userId: 101,
      displayName: "SuperCoder",
      avatar: "CyberNinja",
      classGroupName: "1TIN-A",
      score: 450,
      level: 5,
      weeklyStreak: 5,
      greenTestsCount: 45,
      isCurrentUser: false,
      tier: 8,
      tierName: "Obsidian",
      tierIcon: "🌋",
    },
  ],
  studentOptions: [
    {
      userId: 303,
      displayName: "Bob Coder",
      rank: 1,
      score: 280,
      tierName: "Diamond",
      tierIcon: "💎",
    },
    {
      userId: 202,
      displayName: "SyntaxSlayer",
      rank: 2,
      score: 180,
      tierName: "Diamond",
      tierIcon: "💎",
    },
  ],
};

export const mockWeeklySprintEntries: ILeaderboardEntryModel[] = [
  {
    rank: 1,
    userId: 105,
    displayName: "AliceDev",
    avatar: "DevDragon",
    classGroupName: "1TIN-A",
    score: 80,
    level: 3,
    weeklyStreak: 3,
    greenTestsCount: 8,
    isCurrentUser: false,
  },
  {
    rank: 2,
    userId: 202,
    displayName: "SyntaxSlayer",
    avatar: "CodeCat",
    classGroupName: "1TIN-A",
    score: 50,
    level: 3,
    weeklyStreak: 2,
    greenTestsCount: 5,
    isCurrentUser: true,
  },
];

export const mockClassLeaderboardEntries: IClassGroupLeaderboardEntryModel[] = [
  {
    rank: 1,
    classGroupId: 10,
    classGroupName: "1TIN-A",
    studentCount: 5,
    totalGreenTests: 38,
    classScore: 385,
    totalXp: 375,
    isCurrentUserClass: true,
    currentUserContribution: 113,
  },
  {
    rank: 2,
    classGroupId: 20,
    classGroupName: "1TIN-B",
    studentCount: 4,
    totalGreenTests: 22,
    classScore: 240,
    totalXp: 220,
    isCurrentUserClass: false,
    currentUserContribution: null,
  },
];

export interface SetupGamificationOptions {
  enabled?: boolean;
  isTeacher?: boolean;
  isClassCompetitionEnabled?: boolean;
  classGroups?: ICourseClassGroupModel[];
}

const defaultGamificationOptions: SetupGamificationOptions = Object.freeze({
  enabled: true,
  isTeacher: false,
});

async function handleGamificationConfigRoute(
  route: Route,
  config: ICourseGamificationConfigModel,
): Promise<void> {
  if (route.request().method() === "PUT") {
    const updatedConfig = route.request().postDataJSON();
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(updatedConfig),
    });
    return;
  }
  await route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(config),
  });
}

async function handleGamificationClassesRoute(
  route: Route,
  classGroups: ICourseClassGroupModel[] = mockClassGroups,
): Promise<void> {
  if (route.request().method() === "POST") {
    const data = route.request().postDataJSON();
    const name = typeof data === "string" ? data : (data?.name || "1TIN-C");
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ id: 99, name, courseId: 1, periodId: 1 }),
    });
    return;
  }
  await route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(classGroups),
  });
}

async function handleGamificationProfileRoute(route: Route, url: string): Promise<void> {
  if (url.includes("perspectiveUserId=303")) {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockInspectedStudentProfile),
    });
    return;
  }
  await route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(mockStudentGamificationProfile),
  });
}

async function handleGamificationLeaderboardRoute(
  route: Route,
  url: string,
  isTeacher: boolean,
): Promise<void> {
  if (url.includes("/gamification/leaderboard/hall-of-fame")) {
    if (url.includes("perspectiveUserId=303")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockHallOfFameStudentView),
      });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(isTeacher ? mockHallOfFameTeacherView : mockHallOfFameStudentView),
    });
    return;
  }

  if (url.includes("/gamification/leaderboard/weekly")) {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockWeeklySprintEntries),
    });
    return;
  }

  if (url.includes("/gamification/leaderboard/classes")) {
    const showPersonalPerspective = !isTeacher || url.includes("perspectiveUserId=");
    const entries = showPersonalPerspective
      ? mockClassLeaderboardEntries
      : mockClassLeaderboardEntries.map((entry) => ({
          ...entry,
          isCurrentUserClass: false,
          currentUserContribution: null,
        }));
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(entries),
    });
  }
}

/**
 * Intercepts all gamification endpoints for course 1.
 */
export async function setupGamificationApiMocks(
  page: Page,
  options: SetupGamificationOptions = defaultGamificationOptions,
) {
  const isEnabled = options.enabled ?? true;
  const isTeacher = options.isTeacher ?? false;
  const baseConfig = isEnabled
    ? mockGamificationConfigEnabled
    : mockGamificationConfigDisabled;
  const config: ICourseGamificationConfigModel = {
    ...baseConfig,
    isClassCompetitionEnabled:
      options.isClassCompetitionEnabled ?? baseConfig.isClassCompetitionEnabled,
  };
  const classGroups = options.classGroups ?? mockClassGroups;

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
      await handleGamificationConfigRoute(route, config);
      return;
    }

    if (url.includes("/gamification/classes")) {
      await handleGamificationClassesRoute(route, classGroups);
      return;
    }

    if (url.includes("/gamification/profile")) {
      await handleGamificationProfileRoute(route, url);
      return;
    }

    if (url.includes("/gamification/leaderboard")) {
      await handleGamificationLeaderboardRoute(route, url, isTeacher);
      return;
    }

    // Default for /api/courses/1 or /api/courses/1?periodId=...
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockCourseContents),
    });
  });
}
