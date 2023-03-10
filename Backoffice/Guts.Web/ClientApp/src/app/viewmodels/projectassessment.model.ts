import * as moment from "moment";

export class ProjectAssessmentCreateModel {
    projectId: number;
    description: string;
    openOn: moment.Moment;
    deadline: moment.Moment;
  
    constructor(projectId: number) {
      this.projectId = projectId;
      this.description = '';
      this.openOn = null;
      this.deadline = null;
    }
  }

  export interface IProjectAssessmentModel {
    id: number;
    description: string;
    openOnUtc: moment.Moment;
    deadlineUtc: moment.Moment;
  }

  export class ProjectAssessmentModel implements IProjectAssessmentModel {
    public id: number;
    public description: string;
    public openOnUtc: moment.Moment;
    public get openOnDisplay(){
        return this.openOnUtc.local().format("dddd, DD/MM/YYYY, HH:mm");
    }

    public deadlineUtc: moment.Moment;
    public get deadlineDisplay(){
        return this.deadlineUtc.local().format("dddd, DD/MM/YYYY, HH:mm");
    }
  
    constructor(source? : IProjectAssessmentModel) {
      this.id = 0;
      this.description = '';
      this.openOnUtc = null;
      this.deadlineUtc = null;

      if(source){
        this.id = source.id;
        this.description = source.description;
        this.openOnUtc = moment.utc(source.openOnUtc);
        this.deadlineUtc = moment.utc(source.deadlineUtc);
      }
    }
  }
