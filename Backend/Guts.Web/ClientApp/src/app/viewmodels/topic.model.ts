import { IAssignmentStatisticsModel, AssignmentStatisticsModel } from "./assignmentstatistics.model";
import { IAssignmentSummaryModel, AssignmentSummaryModel } from "./assignmentsummary.model"

export interface ITopicModel {
  id: number;
  code: string;
  description: string;
}

export interface ITopicStatisticsModel extends ITopicModel {
  assignmentStatistics: IAssignmentStatisticsModel[];
}

export class TopicStatisticsModel implements ITopicStatisticsModel {
  public id: number;
  public code: string;
  public description: string;
  public assignmentStatistics: AssignmentStatisticsModel[];

  constructor(source: ITopicStatisticsModel) {
    this.id = source.id;
    this.code = source.code;
    this.description = source.description;
    this.assignmentStatistics = [];

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
  public id: number;
  public code: string;
  public description: string;
  public assignmentSummaries: AssignmentSummaryModel[];

  constructor(source: ITopicSummaryModel) {
    this.id = source.id;
    this.code = source.code;
    this.description = source.description;
    this.assignmentSummaries = [];

    if (source.assignmentSummaries) {
      for (let assignmentSummary of source.assignmentSummaries) {
        let summary = new AssignmentSummaryModel(assignmentSummary);
        this.assignmentSummaries.push(summary);
      }
    }
  }
}
