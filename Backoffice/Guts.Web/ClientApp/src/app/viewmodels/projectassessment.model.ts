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
  teamStatus: IProjectTeamAssessmentStatusModel;
}

export class ProjectAssessmentModel implements IProjectAssessmentModel {
  public id: number;
  public description: string;
  public openOnUtc: moment.Moment;
  public get openOnDisplay() : string {
    return this.openOnUtc.local().format("dddd, DD/MM/YYYY, HH:mm");
  }
  public get isOpen() : boolean {
    let now = moment();
    return now.isAfter(this.openOnUtc) && this.deadlineUtc.isAfter(now);
  }

  public deadlineUtc: moment.Moment;
  public get deadlineDisplay() : string {
    return this.deadlineUtc.local().format("dddd, DD/MM/YYYY, HH:mm");
  }

  public teamStatus: IProjectTeamAssessmentStatusModel;

  constructor(source?: IProjectAssessmentModel) {
    this.id = 0;
    this.description = '';
    this.openOnUtc = null;
    this.deadlineUtc = null;
    this.teamStatus = new ProjectTeamAssessmentStatusModel();

    if (source) {
      this.id = source.id;
      this.description = source.description;
      this.openOnUtc = moment.utc(source.openOnUtc);
      this.deadlineUtc = moment.utc(source.deadlineUtc);
    }
  }
}

export interface IProjectTeamAssessmentStatusModel {
  id: number;
  teamId: number;
  isComplete: boolean;
  peersThatNeedToEvaluateOthers: IPeerModel[];
}

export class ProjectTeamAssessmentStatusModel implements IProjectTeamAssessmentStatusModel {
  public id: number;
  public teamId: number;
  public isComplete: boolean;
  public peersThatNeedToEvaluateOthers: IPeerModel[];

  constructor(source?: IProjectTeamAssessmentStatusModel) {
    this.id = 0;
    this.teamId = 0;
    this.isComplete = false;
    this.peersThatNeedToEvaluateOthers = [];

    if (source) {
      Object.assign(this, source);
      this.peersThatNeedToEvaluateOthers = source.peersThatNeedToEvaluateOthers.map(peer => new PeerModel(peer));
    }
  }
}

export interface IPeerModel {
  userId: number;
  fullName: string;
}

export class PeerModel implements IPeerModel {
  public userId: number;
  public fullName: string;

  constructor(source?: IPeerModel) {
    this.userId = 0;
    this.fullName = '';

    if (source) {
      Object.assign(this, source);
    }
  }
}

