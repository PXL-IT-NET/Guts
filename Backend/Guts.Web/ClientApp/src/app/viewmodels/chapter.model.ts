import { IUserModel } from "./user.model"
import { IExerciseModel } from './exercise.model';
import { IExerciseStatisticsModel, ExerciseStatisticsModel } from './exercisestatistics.model';
import { IExerciseSummaryModel, ExerciseSummaryModel } from "./exercisesummary.model"

export interface IChapterModel {
  id: number;
  number: number;
}

export interface IChapterDetailsModel extends IChapterModel {
  exercises: IExerciseModel[];
  users: IUserModel[];
}

export interface IChapterSummaryModel extends IChapterModel {
  exerciseSummaries: IExerciseSummaryModel[];
}

export class ChapterSummaryModel implements IChapterSummaryModel {
  public id: number;
  public number: number;
  public exerciseSummaries: ExerciseSummaryModel[];

  constructor(source: IChapterSummaryModel) {
    this.id = source.id;
    this.number = source.number;
    this.exerciseSummaries = [];

    if (source.exerciseSummaries) {
      for (let exerciseSummary of source.exerciseSummaries) {
        let summary = new ExerciseSummaryModel(exerciseSummary);
        this.exerciseSummaries.push(summary);
      }
    }
  }
}

export interface IChapterStatisticsModel extends IChapterModel {
  exerciseStatistics: IExerciseStatisticsModel[];
}

export class ChapterStatisticsModel implements IChapterStatisticsModel {
  public id: number;
  public number: number;
  public exerciseStatistics: ExerciseStatisticsModel[];

  constructor(source: IChapterStatisticsModel) {
    this.id = source.id;
    this.number = source.number;
    this.exerciseStatistics = [];

    if (source.exerciseStatistics) {
      for (let exerciseStatistic of source.exerciseStatistics) {
        let summary = new ExerciseStatisticsModel(exerciseStatistic);
        this.exerciseStatistics.push(summary);
      }
    }
  }
}

