import { AssignmentDetailModel } from "./assignmentdetail.model";
import { AssignmentStatisticsModel } from "./assignmentstatistics.model";
import { AssignmentSummaryModel } from "./assignmentsummary.model";
import {
  AssignmentEvaluation,
  ExamModel,
  ExamPartModel,
} from "./exam.model";
import { PeriodModel } from "./period.model";
import {
  PeerAssessmentModel,
  ProjectAssessmentCreateModel,
  ProjectAssessmentModel,
  ProjectAssessmentUpdateModel,
  ProjectTeamAssessmentStatusModel,
} from "./projectassessment.model";
import { SolutionFileModel } from "./solutionfile.model";
import { TeamGenerationModel } from "./team.model";
import { TopicStatisticsModel, TopicSummaryModel } from "./topic.model";
import { UserProfile } from "./user.model";

describe("ViewModels", () => {
  describe("UserProfile", () => {
    it("should initialize defaults when no source is provided", () => {
      const profile = new UserProfile();
      expect(profile.isAuthenticated).toBeFalse();
      expect(profile.isLector()).toBeFalse();
      expect(profile.isMemberOf(1)).toBeFalse();
    });

    it("should copy source properties and evaluate isLector and isMemberOf", () => {
      const profile = new UserProfile({
        id: 5,
        email: "lector@pxl.be",
        roles: ["lector"],
        teams: [10, 20],
        isAuthenticated: false,
      });

      expect(profile.id).toBe(5);
      expect(profile.isAuthenticated).toBeTrue();
      expect(profile.isLector()).toBeTrue();
      expect(profile.isMemberOf(10)).toBeTrue();
      expect(profile.isMemberOf(99)).toBeFalse();
    });
  });

  describe("AssignmentSummaryModel & TopicSummaryModel", () => {
    it("AssignmentSummaryModel should compute and cache chartData", () => {
      const summary = new AssignmentSummaryModel({
        assignmentId: 1,
        code: "Ex1",
        description: "Exercise 1",
        numberOfTests: 10,
        numberOfPassedTests: 6,
        numberOfFailedTests: 3,
        numberOfUsers: 25,
        tests: [],
      });

      const chartData = summary.chartData;
      expect(chartData.data).toEqual([6, 3, 1]);
      expect(summary.chartData).toBe(chartData);
    });

    it("TopicSummaryModel should aggregate totalTests and totalGreenTests across assignments", () => {
      const topic = new TopicSummaryModel({
        id: 2,
        code: "Ch1",
        description: "Chapter 1",
        assignments: [],
        assignmentSummaries: [
          {
            assignmentId: 1,
            code: "Ex1",
            description: "Ex 1",
            numberOfTests: 5,
            numberOfPassedTests: 4,
            numberOfFailedTests: 1,
            numberOfUsers: 10,
            tests: [],
          },
          {
            assignmentId: 2,
            code: "Ex2",
            description: "Ex 2",
            numberOfTests: 8,
            numberOfPassedTests: 5,
            numberOfFailedTests: 2,
            numberOfUsers: 10,
            tests: [],
          },
        ],
      });

      expect(topic.totalTests).toBe(13);
      expect(topic.totalGreenTests).toBe(9);
      expect(topic.assignmentSummaries.length).toBe(2);
    });
  });

  describe("AssignmentStatisticsModel & TopicStatisticsModel", () => {
    it("AssignmentStatisticsModel should build and cache chartData from testPassageStatistics", () => {
      const stats = new AssignmentStatisticsModel({
        assignmentId: 1,
        code: "Ex1",
        description: "Exercise 1",
        totalNumberOfUnits: 20,
        unit: "students",
        tests: [],
        testPassageStatistics: [
          { amountOfPassedTestsRange: "0-2", numberOfUsers: 5 },
          { amountOfPassedTestsRange: "3-5", numberOfUsers: 15 },
        ],
      });

      const chartData = stats.chartData;
      expect(chartData.labels).toEqual(["0-2", "3-5"]);
      expect(chartData.datasets[0]).toEqual({
        data: [5, 15],
        label: "students",
      });
      expect(stats.chartData).toBe(chartData);
    });

    it("TopicStatisticsModel should map assignmentStatistics into AssignmentStatisticsModel instances", () => {
      const topicStats = new TopicStatisticsModel({
        id: 1,
        code: "Ch1",
        description: "Chapter 1",
        assignments: [],
        assignmentStatistics: [
          {
            assignmentId: 3,
            code: "Ex3",
            description: "Exercise 3",
            totalNumberOfUnits: 10,
            unit: "teams",
            tests: [],
            testPassageStatistics: [],
          },
        ],
      });

      expect(topicStats.assignmentStatistics.length).toBe(1);
      expect(
        topicStats.assignmentStatistics[0] instanceof AssignmentStatisticsModel,
      ).toBeTrue();
    });
  });

  describe("AssignmentDetailModel & SolutionFileModel", () => {
    it("should format firstRun and lastRun and map solutionFiles", () => {
      const detail = new AssignmentDetailModel({
        assignmentId: 1,
        code: "Ex1",
        description: "Exercise 1",
        topicCode: "Ch1",
        courseName: ".NET",
        courseId: 4,
        tests: [],
        testResults: [],
        firstRun: "2026-05-10T12:30:00Z",
        lastRun: "2026-05-11T14:45:00Z",
        numberOfRuns: 3,
        solutionFiles: [{ filePath: "Program.cs", content: "class Program {}" }],
      });

      expect(detail.firstRun).toContain("10/05/2026");
      expect(detail.lastRun).toContain("11/05/2026");
      expect(detail.solutionFiles.length).toBe(1);
      expect(detail.solutionFiles[0] instanceof SolutionFileModel).toBeTrue();
      expect(detail.solutionFiles[0].isCollapsed).toBeTrue();
    });
  });

  describe("ExamModel, ExamPartModel & AssignmentEvaluation", () => {
    it("should convert deadline to and from NgbDateStruct and NgbTimeStruct", () => {
      const date = new Date(2026, 5, 15, 14, 30, 0);
      const exam = new ExamModel({
        id: 1,
        courseId: 2,
        name: "June Exam",
        maximumScore: 20,
        parts: [
          {
            id: 10,
            name: "Part 1",
            deadline: date,
            assignmentEvaluations: [
              {
                id: 100,
                assignmentId: 5,
                maximumScore: 10,
                numberOfTestsAlreadyGreenAtStart: 1,
              },
            ],
          },
        ],
      });

      expect(exam.isCollapsed).toBeTrue();
      expect(exam.parts.length).toBe(1);
      const part: ExamPartModel = exam.parts[0];
      expect(part.deadlineDate).toEqual({ year: 2026, month: 6, day: 15 });
      expect(part.deadlineTime).toEqual({ hour: 14, minute: 30, second: 0 });
      expect(part.deadline.getFullYear()).toBe(2026);
      expect(
        part.assignmentEvaluations[0] instanceof AssignmentEvaluation,
      ).toBeTrue();
    });
  });

  describe("ProjectAssessment models", () => {
    it("ProjectAssessmentModel should compute isOpen, isAfterOpenOn, isOver, and display strings", () => {
      const past = new Date(Date.now() - 2 * 24 * 60 * 60 * 1000);
      const future = new Date(Date.now() + 2 * 24 * 60 * 60 * 1000);

      const openAssessment = new ProjectAssessmentModel({
        id: 1,
        description: "Sprint 1",
        openOnUtc: past,
        deadlineUtc: future,
        teamStatus: null,
      });

      expect(openAssessment.isOpen).toBeTrue();
      expect(openAssessment.isAfterOpenOn).toBeTrue();
      expect(openAssessment.isOver).toBeFalse();
      expect(openAssessment.openOnDisplay.length).toBeGreaterThan(0);
      expect(openAssessment.deadlineDisplay.length).toBeGreaterThan(0);
    });

    it("ProjectTeamAssessmentStatusModel and PeerAssessmentModel should map nested UserModels", () => {
      const status = new ProjectTeamAssessmentStatusModel({
        id: 1,
        teamId: 2,
        isComplete: true,
        peersThatNeedToEvaluateOthers: [{ id: 7, fullName: "Jane Doe" }],
      });
      expect(status.peersThatNeedToEvaluateOthers[0].fullName).toBe("Jane Doe");

      const peerAssessment = new PeerAssessmentModel({
        subject: { id: 7, fullName: "Jane Doe" },
        user: { id: 8, fullName: "John Smith" },
        contributionScore: 4,
        cooperationScore: 3,
        effortScore: 5,
        isSelfAssessment: false,
        explanation: "Great work",
      });
      expect(peerAssessment.subject.fullName).toBe("Jane Doe");
      expect(peerAssessment.user.fullName).toBe("John Smith");

      const createModel = new ProjectAssessmentCreateModel(9);
      expect(createModel.projectId).toBe(9);

      const updateModel = new ProjectAssessmentUpdateModel();
      expect(updateModel.id).toBe(0);
    });
  });

  describe("PeriodModel & TeamGenerationModel", () => {
    it("PeriodModel should parse dates from source", () => {
      const period = new PeriodModel({
        id: 3,
        description: "2025-2026",
        from: new Date("2025-09-01"),
        until: new Date("2026-08-31"),
        isActive: true,
      });

      expect(period.id).toBe(3);
      expect(period.isActive).toBeTrue();
      expect(period.from instanceof Date).toBeTrue();
    });

    it("TeamGenerationModel should assign constructor arguments", () => {
      const gen = new TeamGenerationModel("Team", 1, 10);
      expect(gen.teamBaseName).toBe("Team");
      expect(gen.teamNumberFrom).toBe(1);
      expect(gen.teamNumberTo).toBe(10);
    });
  });
});

