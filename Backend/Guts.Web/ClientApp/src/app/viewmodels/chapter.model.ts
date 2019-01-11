import { IUserModel } from "./user.model"
import { IAssignmentModel} from "./assignment.model";
import { IAssignmentStatisticsModel, AssignmentStatisticsModel } from "./assignmentstatistics.model";
import { IAssignmentSummaryModel, AssignmentSummaryModel } from "./assignmentsummary.model"

export interface IChapterModel {
  id: number;
  code: string;
  description: string;
}

export interface IChapterDetailsModel extends IChapterModel {
  exercises: IAssignmentModel[];
  users: IUserModel[];
}

export interface IChapterSummaryModel extends IChapterModel {
  exerciseSummaries: IAssignmentSummaryModel[];
}

export class ChapterSummaryModel implements IChapterSummaryModel {
  public id: number;
  public code: string;
  public description: string;
  public exerciseSummaries: AssignmentSummaryModel[];

  constructor(source: IChapterSummaryModel) {
    this.id = source.id;
    this.code = source.code;
    this.description = source.description;
    this.exerciseSummaries = [];

    if (source.exerciseSummaries) {
      for (let exerciseSummary of source.exerciseSummaries) {
        let summary = new AssignmentSummaryModel(exerciseSummary);
        this.exerciseSummaries.push(summary);
      }
    }
  }
}

export interface IChapterStatisticsModel extends IChapterModel {
  exerciseStatistics: IAssignmentStatisticsModel[];
}

export class ChapterStatisticsModel implements IChapterStatisticsModel {
  public id: number;
  public code: string;
  public description: string;
  public exerciseStatistics: AssignmentStatisticsModel[];

  constructor(source: IChapterStatisticsModel) {
    this.id = source.id;
    this.code = source.code;
    this.description = source.description;
    this.exerciseStatistics = [];

    if (source.exerciseStatistics) {
      for (let exerciseStatistic of source.exerciseStatistics) {
        let summary = new AssignmentStatisticsModel(exerciseStatistic);
        this.exerciseStatistics.push(summary);
      }
    }
  }
}

