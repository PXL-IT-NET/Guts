import { IAssignmentModel } from "./assignment.model";

export interface IAssignmentStatisticsModel extends IAssignmentModel {
  totalNumberOfUsers: number;
  testPassageStatistics: ITestPassageStatisticModel[];
}

export interface ITestPassageStatisticModel {
  amountOfPassedTestsRange: string;
  numberOfUsers: number;
}

export class AssignmentStatisticsModel implements IAssignmentStatisticsModel {
  public assignmentId: number;
  public code: string;
  public description: string;
  public totalNumberOfUsers: number;
  public testPassageStatistics: ITestPassageStatisticModel[];

  constructor(source?: IAssignmentStatisticsModel) {
    this.assignmentId = 0;
    this.code = '';
    this.description = '';
    this.totalNumberOfUsers = 0;
    this.testPassageStatistics = [];

    if (source) {
      this.assignmentId = source.assignmentId;
      this.code = source.code;
      this.description = source.description;
      this.totalNumberOfUsers = source.totalNumberOfUsers;
      this.testPassageStatistics = source.testPassageStatistics;
    }
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
