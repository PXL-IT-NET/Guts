import * as moment from 'moment';
import { NgbTimeStruct, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';

export interface IExamModel {
  id: number;
  courseId: number;
  name: string;
  maximumScore: number;
  parts: IExamPartModel[];
}

export interface IExamPartModel {
  id: number;
  name: string;
  deadline: Date;
  assignmentEvaluations: IAssignmentEvaluation[];
}

export interface IAssignmentEvaluation {
  id: number;
  assignmentId: number;
  maximumScore: number;
  numberOfTestsAlreadyGreenAtStart: number;
}

export class AssignmentEvaluation implements IAssignmentEvaluation{
  public id: number;  
  public assignmentId: number;
  public maximumScore: number;
  public numberOfTestsAlreadyGreenAtStart: number;

 constructor(source?: IAssignmentEvaluation){
   this.id = 0;
   this.assignmentId = 0;
   this.maximumScore = null;
   this.numberOfTestsAlreadyGreenAtStart = null;

   if(source){
     this.id = source.id;
     this.assignmentId = source.assignmentId;
     this.maximumScore = source.maximumScore;
     this.numberOfTestsAlreadyGreenAtStart = source.numberOfTestsAlreadyGreenAtStart;
   }
 }
}

export class ExamPartModel implements IExamPartModel {
  public id: number;
  public name: string;
  public assignmentEvaluations: IAssignmentEvaluation[];

  public deadlineDate: NgbDateStruct;
  public deadlineTime: NgbTimeStruct;
  public get deadline(): Date {
    var d = moment({
      year: this.deadlineDate.year,
      month: this.deadlineDate.month - 1,
      day: this.deadlineDate.day,
      hour: this.deadlineTime.hour,
      minute: this.deadlineTime.minute
    });
    return d.toDate();
  }
  public set deadline(value: Date) {
    var d = moment(value).local();
    this.deadlineDate = {year: d.year(), month: d.month() + 1, day: d.date() };
    this.deadlineTime = { hour: d.hour(), minute: d.minute(), second: 0};
  }

  constructor(source?: IExamPartModel) {
    this.id = 0;
    this.name = '';
    this.deadline = moment().startOf('day').toDate();
    this.assignmentEvaluations = [];

    if(source){
      this.id = source.id,
      this.name = source.name,
      this.deadline = source.deadline,
      this.assignmentEvaluations = source.assignmentEvaluations.map(ae => new AssignmentEvaluation(ae));
    }
  }
}

export class ExamModel implements IExamModel {
  public id: number;
  public courseId: number;
  public name: string;
  public maximumScore: number;
  public parts: ExamPartModel[];

  //UI
  public isCollapsed: boolean;

  constructor(source?: IExamModel) {
    this.id = 0;
    this.courseId = 0;
    this.name = '';
    this.maximumScore = 0;
    this.parts = [];
    this.isCollapsed = true;

    if (source) {
      this.id = source.id;
      this.courseId = source.courseId;
      this.name = source.name;
      this.maximumScore = source.maximumScore;
      this.parts = source.parts.map(p => new ExamPartModel(p));
    }
  }
}
