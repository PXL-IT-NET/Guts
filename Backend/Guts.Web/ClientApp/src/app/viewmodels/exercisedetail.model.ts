import { ITestResultModel } from './testresult.model';

export interface IExerciseDetailModel {
  exerciseId: number;
  number: number;
  chapterNumber: number;
  courseName: string;
  courseId: number;
  testResults: ITestResultModel[];
  firstRun: string;
  lastRun: string;
  numberOfRuns: number;
}
