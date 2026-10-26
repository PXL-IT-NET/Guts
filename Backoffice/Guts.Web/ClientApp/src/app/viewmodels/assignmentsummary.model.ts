import { IAssignmentModel, ITestModel } from "./assignment.model";

export interface IAssignmentSummaryModel extends IAssignmentModel {
  numberOfTests: number;
  numberOfPassedTests: number;
  numberOfFailedTests: number;
  numberOfUsers: number;
}

export class AssignmentSummaryModel implements IAssignmentSummaryModel {
  public assignmentId = 0;
  public code = "";
  public description = "";
  public numberOfTests = 0;
  public numberOfPassedTests = 0;
  public numberOfFailedTests = 0;
  public numberOfUsers = 0;
  public tests: ITestModel[] = [];

  constructor(source?: IAssignmentSummaryModel) {
    if (source) {
      this.assignmentId = source.assignmentId;
      this.code = source.code;
      this.description = source.description;
      this.numberOfTests = source.numberOfTests;
      this.numberOfPassedTests = source.numberOfPassedTests;
      this.numberOfFailedTests = source.numberOfFailedTests;
      this.numberOfUsers = source.numberOfUsers;
      this.tests = source.tests;
    }
  }

  private _chartData: any = null;
  get chartData(): any {
    if (!this._chartData) {
      const numberOfNotRunnedTests =
        this.numberOfTests -
        this.numberOfPassedTests -
        this.numberOfFailedTests;
      this._chartData = {
        datasets: [
          {
            data: [
              this.numberOfPassedTests,
              this.numberOfFailedTests,
              numberOfNotRunnedTests,
            ],
            backgroundColor: ["#00ff00", "#ff0000", "#ffa500"],
          },
        ],
        data: [
          this.numberOfPassedTests,
          this.numberOfFailedTests,
          numberOfNotRunnedTests,
        ],
        labels: ["Passed tests", "Failed tests", "Not runned tests"],
        colors: [{ backgroundColor: ["#00ff00", "#ff0000", "#ffa500"] }],
      };
    }
    return this._chartData;
  }
}
