import { IAssignmentModel } from "./assignment.model";

export interface IAssignmentSummaryModel extends IAssignmentModel {
  numberOfTests: number;
  numberOfPassedTests: number;
  numberOfFailedTests: number;
  numberOfUsers: number;
}

export class AssignmentSummaryModel implements IAssignmentSummaryModel {
  public assignmentId: number;
  public code: string;
  public description: string;
  public numberOfTests: number;
  public numberOfPassedTests: number;
  public numberOfFailedTests: number;
  public numberOfUsers: number;

  constructor(source?: IAssignmentSummaryModel) {
    this.assignmentId = 0;
    this.code = '';
    this.description = '';
    this.numberOfTests = 0;
    this.numberOfPassedTests = 0;
    this.numberOfFailedTests = 0;
    this.numberOfUsers = 0;

    if (source) {
      this.assignmentId = source.assignmentId;
      this.code = source.code;
      this.description = source.description;
      this.numberOfTests = source.numberOfTests;
      this.numberOfPassedTests = source.numberOfPassedTests;
      this.numberOfFailedTests = source.numberOfFailedTests;
      this.numberOfUsers = source.numberOfUsers;
    }
  }

  private _chartData: any | null = null;
  get chartData(): any {
    if (!this._chartData) {
      var numberOfNotRunnedTests = this.numberOfTests - this.numberOfPassedTests - this.numberOfFailedTests;
      this._chartData = {
        datasets: [{
          data: [this.numberOfPassedTests, this.numberOfFailedTests, numberOfNotRunnedTests],
          backgroundColor: ['#00ff00', '#ff0000', '#ffa500'] 
        }],
        data: [this.numberOfPassedTests, this.numberOfFailedTests, numberOfNotRunnedTests],
        labels: ['Passed tests', 'Failed tests', 'Not runned tests'],
        colors: [{ backgroundColor: ['#00ff00', '#ff0000', '#ffa500'] }]
      };
    }
    return this._chartData;
  }
}
