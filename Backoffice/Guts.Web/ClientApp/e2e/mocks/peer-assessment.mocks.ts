import { Page, Route } from "@playwright/test";
import { mockActivePeriod } from "./course.mocks";
import { IProjectDetailsModel } from "../../src/app/viewmodels/project.model";
import {
  IProjectAssessmentModel,
  IProjectTeamAssessmentStatusModel,
  IPeerAssessmentModel,
  IAssessmentResultModel,
} from "../../src/app/viewmodels/projectassessment.model";

export const mockProjectWithAssessments: IProjectDetailsModel = {
  id: 10,
  code: "proj1",
  description: "Project 1: Web App",
  components: [],
  teams: [
    { id: 1, name: "Team 1" },
    { id: 2, name: "Team 2" },
    { id: 3, name: "Team 3" },
  ],
  assignments: [],
};

export const mockCourseContentsWithProject = {
  id: 1,
  code: "NET-ADV",
  name: ".NET Advanced",
  chapters: [],
  projects: [
    {
      id: 10,
      code: "proj1",
      description: "Project 1: Web App",
      assignments: [],
    },
  ],
};

export const mockAssessmentList: IProjectAssessmentModel[] = [
  {
    id: 101,
    description: "Sprint 1 Peer Assessment",
    openOnUtc: new Date(Date.now() - 3600000 * 24).toISOString(), // 1 day ago (open)
    deadlineUtc: new Date(Date.now() + 3600000 * 24 * 7).toISOString(), // 7 days in future
    teamStatus: {
      id: 501,
      teamId: 1,
      isComplete: false,
      peersThatNeedToEvaluateOthers: [
        { id: 202, fullName: "Student Guts" },
        { id: 203, fullName: "Alice TeamMate" },
      ],
    },
  },
  {
    id: 102,
    description: "Sprint 2 Peer Assessment (Completed)",
    openOnUtc: new Date(Date.now() - 3600000 * 24 * 14).toISOString(),
    deadlineUtc: new Date(Date.now() - 3600000 * 24 * 2).toISOString(),
    teamStatus: {
      id: 502,
      teamId: 1,
      isComplete: true,
      peersThatNeedToEvaluateOthers: [],
    },
  },
];

export const mockStatusAssessment101Team1: IProjectTeamAssessmentStatusModel = {
  id: 501,
  teamId: 1,
  isComplete: false,
  peersThatNeedToEvaluateOthers: [
    { id: 202, fullName: "Student Guts" },
    { id: 203, fullName: "Alice TeamMate" },
  ],
};

export const mockStatusAssessment102Team1: IProjectTeamAssessmentStatusModel = {
  id: 502,
  teamId: 1,
  isComplete: true,
  peersThatNeedToEvaluateOthers: [],
};

export const mockStatusAssessment102Team2: IProjectTeamAssessmentStatusModel = {
  id: 504,
  teamId: 2,
  isComplete: false,
  peersThatNeedToEvaluateOthers: [
    { id: 301, fullName: "Bob Incomplete" },
  ],
};

export const mockStatusAssessment102Team3: IProjectTeamAssessmentStatusModel = {
  id: 505,
  teamId: 3,
  isComplete: true,
  peersThatNeedToEvaluateOthers: [],
};

export const mockEvaluationList101: IPeerAssessmentModel[] = [
  {
    subject: { id: 202, fullName: "Student Guts" },
    user: { id: 202, fullName: "Student Guts" },
    contributionScore: 3,
    cooperationScore: 3,
    effortScore: 3,
    isSelfAssessment: true,
    explanation: "",
  },
  {
    subject: { id: 203, fullName: "Alice TeamMate" },
    user: { id: 202, fullName: "Student Guts" },
    contributionScore: -1,
    cooperationScore: -1,
    effortScore: -1,
    isSelfAssessment: false,
    explanation: "",
  },
];

export const mockMyResult102: IAssessmentResultModel = {
  assessmentDescription: "Sprint 2 Peer Assessment (Completed)",
  subject: { id: 202, fullName: "Student Guts" },
  selfAssessment: {
    subject: { id: 202, fullName: "Student Guts" },
    user: { id: 202, fullName: "Student Guts" },
    contributionScore: 4,
    cooperationScore: 3,
    effortScore: 4,
    isSelfAssessment: true,
    explanation: "High effort and technical contribution",
  },
  peerAssessments: [],
  averageResult: {
    value: 3.5,
    selfValue: 3.67,
    peerValue: 3.33,
    score: 4,
    selfScore: 4,
    peerScore: 3,
    averageValue: 3.0,
    averageSelfValue: 3.2,
    averagePeerValue: 3.0,
  },
  effortResult: {
    value: 3.5,
    selfValue: 4.0,
    peerValue: 3.0,
    score: 4,
    selfScore: 4,
    peerScore: 3,
    averageValue: 3.0,
    averageSelfValue: 3.0,
    averagePeerValue: 3.0,
  },
  cooperationResult: {
    value: 3.0,
    selfValue: 3.0,
    peerValue: 3.0,
    score: 3,
    selfScore: 3,
    peerScore: 3,
    averageValue: 3.0,
    averageSelfValue: 3.0,
    averagePeerValue: 3.0,
  },
  contributionResult: {
    value: 3.5,
    selfValue: 4.0,
    peerValue: 3.0,
    score: 4,
    selfScore: 4,
    peerScore: 3,
    averageValue: 3.0,
    averageSelfValue: 3.0,
    averagePeerValue: 3.0,
  },
  individualGrade: 0,
};

export const mockDetailedResults102: IAssessmentResultModel[] = [
  {
    assessmentDescription: "Sprint 2 Peer Assessment (Completed)",
    subject: { id: 202, fullName: "Student Guts" },
    selfAssessment: {
      subject: { id: 202, fullName: "Student Guts" },
      user: { id: 202, fullName: "Student Guts" },
      contributionScore: 4,
      cooperationScore: 3,
      effortScore: 4,
      isSelfAssessment: true,
      explanation: "Did most of the frontend architecture",
    },
    peerAssessments: [
      {
        subject: { id: 202, fullName: "Student Guts" },
        user: { id: 203, fullName: "Alice TeamMate" },
        contributionScore: 4,
        cooperationScore: 4,
        effortScore: 3,
        isSelfAssessment: false,
        explanation: "Great initiative on UI components",
      },
    ],
    averageResult: {
      value: 3.67,
      selfValue: 3.67,
      peerValue: 3.67,
      score: 4,
      selfScore: 4,
      peerScore: 4,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    effortResult: {
      value: 3.5,
      selfValue: 4.0,
      peerValue: 3.0,
      score: 4,
      selfScore: 4,
      peerScore: 3,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    cooperationResult: {
      value: 3.5,
      selfValue: 3.0,
      peerValue: 4.0,
      score: 4,
      selfScore: 3,
      peerScore: 4,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    contributionResult: {
      value: 4.0,
      selfValue: 4.0,
      peerValue: 4.0,
      score: 4,
      selfScore: 4,
      peerScore: 4,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    individualGrade: 0,
  },
  {
    subject: { id: 203, fullName: "Alice TeamMate" },
    selfAssessment: {
      subject: { id: 203, fullName: "Alice TeamMate" },
      user: { id: 203, fullName: "Alice TeamMate" },
      contributionScore: 3,
      cooperationScore: 4,
      effortScore: 3,
      isSelfAssessment: true,
      explanation: "Kept team communication active",
    },
    peerAssessments: [
      {
        subject: { id: 203, fullName: "Alice TeamMate" },
        user: { id: 202, fullName: "Student Guts" },
        contributionScore: 3,
        cooperationScore: 4,
        effortScore: 3,
        isSelfAssessment: false,
        explanation: "Very responsive and helpful",
      },
    ],
    averageResult: {
      value: 3.33,
      selfValue: 3.33,
      peerValue: 3.33,
      score: 3,
      selfScore: 3,
      peerScore: 3,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    effortResult: {
      value: 3.0,
      selfValue: 3.0,
      peerValue: 3.0,
      score: 3,
      selfScore: 3,
      peerScore: 3,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    cooperationResult: {
      value: 4.0,
      selfValue: 4.0,
      peerValue: 4.0,
      score: 4,
      selfScore: 4,
      peerScore: 4,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    contributionResult: {
      value: 3.0,
      selfValue: 3.0,
      peerValue: 3.0,
      score: 3,
      selfScore: 3,
      peerScore: 3,
      averageValue: 3.0,
      averageSelfValue: 3.0,
      averagePeerValue: 3.0,
    },
    individualGrade: 0,
  },
];

export interface SetupPeerAssessmentOptions {
  assessments?: IProjectAssessmentModel[];
  emptyAssessments?: boolean;
}

async function handleStatusRoute(route: Route, url: string): Promise<void> {
  if (url.includes("/of-team/2/")) {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockStatusAssessment102Team2),
    });
    return;
  }
  if (url.includes("/of-team/3/")) {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockStatusAssessment102Team3),
    });
    return;
  }
  const statusBody = url.includes("/of-project-assessment/102/")
    ? mockStatusAssessment102Team1
    : mockStatusAssessment101Team1;
  await route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(statusBody),
  });
}

async function handleDetailedResultsRoute(route: Route, url: string): Promise<void> {
  if (url.includes("/of-team/3/")) {
    const team3Results = mockDetailedResults102.map((r) => ({
      ...r,
      subject: { ...r.subject, fullName: r.subject.fullName + " (Team 3)" },
    }));
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(team3Results),
    });
    return;
  }
  await route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(mockDetailedResults102),
  });
}

/**
 * Registers network route mocks for peer assessments, projects, and related evaluation APIs.
 */
export async function setupPeerAssessmentApiMocks(
  page: Page,
  options: SetupPeerAssessmentOptions = {},
) {
  let assessments: IProjectAssessmentModel[];
  if (options.emptyAssessments) {
    assessments = [];
  } else if (options.assessments) {
    assessments = [...options.assessments];
  } else {
    assessments = [...mockAssessmentList];
  }

  // 1. Periods endpoint
  await page.route("**/api/periods", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockActivePeriod),
    });
  });

  // 2. Course details endpoint
  await page.route("**/api/courses/1**", async (route) => {
    const url = route.request().url();

    if (url.includes("/projects/proj1")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockProjectWithAssessments),
      });
      return;
    }

    if (url.includes("/gamification/config")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isGamificationEnabled: false }),
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(mockCourseContentsWithProject),
    });
  });

  // 3. Project assessments CRUD
  await page.route("**/api/project-assessments**", async (route) => {
    const method = route.request().method();
    const url = route.request().url();

    if (method === "GET") {
      if (url.includes("/of-project/10")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(assessments),
        });
        return;
      }
      const singleMatch = /\/api\/project-assessments\/(\d+)$/.exec(url);
      if (singleMatch) {
        const assessmentId = Number(singleMatch[1]);
        const found = assessments.find((a) => a.id === assessmentId);
        if (found) {
          await route.fulfill({
            status: 200,
            contentType: "application/json",
            body: JSON.stringify(found),
          });
          return;
        }
      }
    }

    if (method === "POST") {
      const data = route.request().postDataJSON();
      const newAssessment: IProjectAssessmentModel = {
        id: 103,
        description: data.description,
        openOnUtc: data.openOn || new Date().toISOString(),
        deadlineUtc: data.deadline || new Date(Date.now() + 86400000 * 7).toISOString(),
        teamStatus: {
          id: 503,
          teamId: 1,
          isComplete: false,
          peersThatNeedToEvaluateOthers: [
            { id: 202, fullName: "Student Guts" },
            { id: 203, fullName: "Alice TeamMate" },
          ],
        },
      };
      assessments.push(newAssessment);
      await route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(newAssessment),
      });
      return;
    }

    if (method === "DELETE") {
      const idMatch = /\/api\/project-assessments\/(\d+)/.exec(url);
      if (idMatch) {
        const idToDelete = Number(idMatch[1]);
        assessments = assessments.filter((a) => a.id !== idToDelete);
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({}),
      });
      return;
    }

    if (method === "PUT") {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({}),
      });
      return;
    }

    await route.fallback();
  });

  // 4. Project team assessment status, results, and evaluations
  await page.route("**/api/project-team-assessments/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/status")) {
      await handleStatusRoute(route, url);
      return;
    }

    if (url.includes("/peer-assessments")) {
      if (method === "GET") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(mockEvaluationList101),
        });
        return;
      }
      if (method === "POST") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({}),
        });
        return;
      }
    }

    if (url.includes("/my-result")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockMyResult102),
      });
      return;
    }

    if (url.includes("/detailed-results")) {
      await handleDetailedResultsRoute(route, url);
      return;
    }

    await route.fallback();
  });
}
