import { ITestResultModel } from './testresult.model';
import { IExerciseModel } from './exercise.model';

export interface IExerciseDetailModel extends IExerciseModel {
  chapterNumber: number;
  courseName: string;
  courseId: number;
  testResults: ITestResultModel[];
  firstRun: string;
  lastRun: string;
  numberOfRuns: number;
  sourceCode: string;
}
