import { IAssignmentModel } from "./assignment.model";
import {
  IAssignmentStatisticsModel,
  AssignmentStatisticsModel,
} from "./assignmentstatistics.model";
import {
  IAssignmentSummaryModel,
  AssignmentSummaryModel,
} from "./assignmentsummary.model";

export interface ITopicModel {
  id: number;
  code: string;
  description: string;
  assignments: IAssignmentModel[];
}

export interface ITopicStatisticsModel extends ITopicModel {
  assignmentStatistics: IAssignmentStatisticsModel[];
}

export class TopicStatisticsModel implements ITopicStatisticsModel {
  public id: number;
  public code: string;
  public description: string;
  public assignments: IAssignmentModel[];
  public assignmentStatistics: AssignmentStatisticsModel[] = [];

  constructor(source: ITopicStatisticsModel) {
    this.id = source.id;
    this.code = source.code;
    this.description = source.description;
    this.assignments = source.assignments;

    if (source.assignmentStatistics) {
      for (let exerciseStatistic of source.assignmentStatistics) {
        let summary = new AssignmentStatisticsModel(exerciseStatistic);
        this.assignmentStatistics.push(summary);
      }
    }
  }
}

export interface ITopicSummaryModel extends ITopicModel {
  assignmentSummaries: IAssignmentSummaryModel[];
}

export class TopicSummaryModel implements ITopicSummaryModel {
  public id = 0;
  public code = "";
  public description = "";
  public assignmentSummaries: AssignmentSummaryModel[] = [];
  public totalGreenTests = 0;
  public totalTests = 0;
  public assignments: IAssignmentModel[] = [];

  constructor(source?: ITopicSummaryModel) {
    if (source) {
      this.id = source.id;
      this.code = source.code;
      this.description = source.description;

      if (source.assignmentSummaries) {
        for (let assignmentSummary of source.assignmentSummaries) {
          let summary = new AssignmentSummaryModel(assignmentSummary);
          this.totalTests += assignmentSummary.numberOfTests;
          this.totalGreenTests += assignmentSummary.numberOfPassedTests;
          this.assignmentSummaries.push(summary);
        }
      }

      if (source.assignments) {
        this.assignments = source.assignments;
      }
    }
  }
}

export interface ITopicUpdateModel {
  description: string;
}

export interface ITopicAddModel {
  code: string;
  description: string;
}
