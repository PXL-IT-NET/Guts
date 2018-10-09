import { ITestResultModel } from './testresult.model';
import { IExerciseModel } from './exercise.model';
import * as moment from 'moment';

export interface IExerciseDetailModel extends IExerciseModel {
  chapterNumber: number;
  courseName: string;
  courseId: number;
  testResults: ITestResultModel[];
  firstRun: any;
  lastRun: any;
  numberOfRuns: number;
  sourceCode: string;
}

export class ExerciseDetailModel implements IExerciseDetailModel {
  public exerciseId: number;
  public number: number;
  public chapterNumber: number;
  public courseName: string;
  public courseId: number;
  public testResults: ITestResultModel[];
  public firstRun: string;
  public lastRun: string;
  public numberOfRuns: number;
  public sourceCode: string;

  constructor(source: ExerciseDetailModel) {
    this.exerciseId = source.exerciseId;
    this.number = source.number;
    this.chapterNumber = source.chapterNumber;
    this.courseName = source.courseName;
    this.courseId = source.courseId;
    this.testResults = source.testResults;
    this.firstRun = '';
    this.lastRun = '';
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
