import { IAssignmentModel, ITestModel } from "./assignment.model";

export interface IAssignmentStatisticsModel extends IAssignmentModel {
  totalNumberOfUnits: number;
  unit: string;
  testPassageStatistics: ITestPassageStatisticModel[];
}

export interface ITestPassageStatisticModel {
  amountOfPassedTestsRange: string;
  numberOfUsers: number;
}

export class AssignmentStatisticsModel implements IAssignmentStatisticsModel {
  public assignmentId = 0;
  public code = "";
  public description = "";
  public totalNumberOfUnits = 0;
  public unit = "";
  public testPassageStatistics: ITestPassageStatisticModel[] = [];
  public tests: ITestModel[] = [];

  constructor(source?: IAssignmentStatisticsModel) {
    if (source) {
      this.assignmentId = source.assignmentId;
      this.code = source.code;
      this.description = source.description;
      this.totalNumberOfUnits = source.totalNumberOfUnits;
      this.unit = source.unit;
      this.testPassageStatistics = source.testPassageStatistics;
      this.tests = source.tests;
    }
  }

  private _chartData: any = null;
  get chartData(): any {
    if (!this._chartData) {
      let data = [];
      let labels = [];
      for (const statistic of this.testPassageStatistics) {
        labels.push(statistic.amountOfPassedTestsRange);
        data.push(statistic.numberOfUsers);
      }

      this._chartData = {
        labels: labels,
        datasets: [
          {
            data: data,
            label: this.unit,
          },
        ],
      };
    }
    return this._chartData;
  }
}
