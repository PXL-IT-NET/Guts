import { ITopicModel } from "./topic.model";

export interface ICourseModel {
  id: number;
  code: string;
  name: string;
}

export interface ICourseContentsModel {
  id: number;
  code: string;
  name: string;
  chapters: ITopicModel[];
  projects: ITopicModel[];
}

export interface ICourseStudentAssignmentStatisticsModel {
  assignmentId: number;
  code: string;
  description: string;
  numberOfPassedTests: number;
  numberOfTests: number;
}

export interface ICourseStudentChapterStatisticsModel {
  chapterId: number;
  code: string;
  description: string;
  assignments: ICourseStudentAssignmentStatisticsModel[];
}

export interface ICourseStudentStatisticsModel {
  studentId: number;
  firstName: string;
  lastName: string;
  fullName: string;
  assignmentsWorkedOn: number;
  totalPassedTests: number;
  totalTests: number;
  completionPercentage: number;
  chapters: ICourseStudentChapterStatisticsModel[];
}

export interface ICourseStatisticsModel {
  courseId: number;
  courseCode: string;
  courseName: string;
  totalAssignments: number;
  totalTests: number;
  students: ICourseStudentStatisticsModel[];
}
