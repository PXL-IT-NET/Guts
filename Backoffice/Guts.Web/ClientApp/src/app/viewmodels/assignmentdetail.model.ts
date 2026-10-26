import { ITestResultModel } from "./testresult.model";
import { IAssignmentModel, ITestModel } from "./assignment.model";
import { formatDateTime } from "../util/date.util";
import { ISolutionFileModel, SolutionFileModel } from "./solutionfile.model";

export interface IAssignmentDetailModel extends IAssignmentModel {
  topicCode: string;
  courseName: string;
  courseId: number;
  testResults: ITestResultModel[];
  firstRun: any;
  lastRun: any;
  numberOfRuns: number;
  solutionFiles: ISolutionFileModel[];
}

export class AssignmentDetailModel implements IAssignmentDetailModel {
  public assignmentId = 0;
  public code = "";
  public description = "";
  public topicCode = "";
  public courseName = "";
  public courseId = 0;
  public tests: ITestModel[] = [];
  public testResults: ITestResultModel[] = [];
  public firstRun = "";
  public lastRun = "";
  public numberOfRuns = 0;
  public solutionFiles: SolutionFileModel[] = [];

  constructor(source?: IAssignmentDetailModel) {
    if (source) {
      this.assignmentId = source.assignmentId;
      this.code = source.code;
      this.description = source.description;
      this.topicCode = source.topicCode;
      this.courseName = source.courseName;
      this.courseId = source.courseId;
      this.tests = source.tests;
      this.testResults = source.testResults;
      this.numberOfRuns = source.numberOfRuns;
      this.solutionFiles = source.solutionFiles.map(
        (file) => new SolutionFileModel(file),
      );

      if (source.firstRun) {
        this.firstRun = formatDateTime(source.firstRun);
      }
      if (source.lastRun) {
        this.lastRun = formatDateTime(source.lastRun);
      }
    }
  }
}
