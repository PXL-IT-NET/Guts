import { ITestResultModel } from './testresult.model';
import { IAssignmentModel, ITestModel } from "./assignment.model";
import * as moment from 'moment';
import { ISolutionFileModel, SolutionFileModel } from './solutionfile.model';

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
  public assignmentId: number;
  public code: string;
  public description: string;
  public topicCode: string;
  public courseName: string;
  public courseId: number;
  public tests: ITestModel[];
  public testResults: ITestResultModel[];
  public firstRun: string;
  public lastRun: string;
  public numberOfRuns: number;
  public solutionFiles: SolutionFileModel[];

  constructor(source?: IAssignmentDetailModel) {
    this.assignmentId = 0;
    this.code = '';
    this.description = '';
    this.topicCode = '';
    this.courseName = '';
    this.courseId = 0;
    this.tests = [];
    this.testResults = [];
    this.firstRun = '';
    this.lastRun = '';
    this.numberOfRuns = 0;
    this.solutionFiles = [];

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
      this.solutionFiles = source.solutionFiles.map(file => new SolutionFileModel(file));

      if (source.firstRun) {
        this.firstRun = moment.utc(source.firstRun).local().format('DD/MM/YYYY HH:mm');
      }
      if (source.lastRun) {
        this.lastRun = moment.utc(source.lastRun).local().format('DD/MM/YYYY HH:mm');
      }
    }
  }
}
