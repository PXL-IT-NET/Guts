import { ITestResultModel } from './testresult.model';
import { IAssignmentModel } from "./assignment.model";
import * as moment from 'moment';

export interface IAssignmentDetailModel extends IAssignmentModel {
  topicCode: string;
  courseName: string;
  courseId: number;
  testResults: ITestResultModel[];
  firstRun: any;
  lastRun: any;
  numberOfRuns: number;
  sourceCode: string;
}

export class AssignmentDetailModel implements IAssignmentDetailModel {
  public assignmentId: number;
  public code: string;
  public description: string;
  public topicCode: string;
  public courseName: string;
  public courseId: number;
  public testResults: ITestResultModel[];
  public firstRun: string;
  public lastRun: string;
  public numberOfRuns: number;
  public sourceCode: string;

  constructor(source?: IAssignmentDetailModel) {
    this.assignmentId = 0;
    this.code = '';
    this.description = '';
    this.topicCode = '';
    this.courseName = '';
    this.courseId = 0;
    this.testResults = [];
    this.firstRun = '';
    this.lastRun = '';
    this.numberOfRuns = 0;
    this.sourceCode = '';

    if (source) {
      this.assignmentId = source.assignmentId;
      this.code = source.code;
      this.description = source.description;
      this.topicCode = source.topicCode;
      this.courseName = source.courseName;
      this.courseId = source.courseId;
      this.testResults = source.testResults;
      this.numberOfRuns = source.numberOfRuns;
      this.sourceCode = source.sourceCode;

      if (source.firstRun) {
        this.firstRun = moment(source.firstRun).format('DD/MM/YYYY HH:mm');
      }
      if (source.lastRun) {
        this.lastRun = moment(source.lastRun).format('DD/MM/YYYY HH:mm');
      }
    }
  }
}
