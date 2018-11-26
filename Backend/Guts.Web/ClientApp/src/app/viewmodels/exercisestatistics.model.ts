import { IExerciseModel } from './exercise.model';

export interface IExerciseStatisticsModel extends IExerciseModel {
  totalNumberOfUsers: number;
  testPassageStatistics: ITestPassageStatisticModel[];
}

export interface ITestPassageStatisticModel {
  amountOfPassedTestsRange: string;
  numberOfUsers: number;
}

export class ExerciseStatisticsModel implements IExerciseStatisticsModel {
  public exerciseId: number;
  public code: string;
  public totalNumberOfUsers: number;
  public testPassageStatistics: ITestPassageStatisticModel[];

  constructor(source: IExerciseStatisticsModel) {
    this.exerciseId = source.exerciseId;
    this.code = source.code;
    this.totalNumberOfUsers = source.totalNumberOfUsers;
    this.testPassageStatistics = source.testPassageStatistics;
  }

  private _chartData: Object | null = null;
  get chartData(): Object {
    if (!this._chartData) {

      let data = [];
      let labels = [];
      for (var statistic of this.testPassageStatistics) {
        labels.push(statistic.amountOfPassedTestsRange);
        data.push(statistic.numberOfUsers);
      }

      this._chartData = {
        labels: labels,
        datasets: [
          {
            data: data,
            label: 'Students'
          }],
      };

    }
    return this._chartData;
  }
}
