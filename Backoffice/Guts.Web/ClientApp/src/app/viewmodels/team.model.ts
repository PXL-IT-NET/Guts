export interface ITeamModel {
  id: number;
  name: string;
}

export interface ITeamDetailsModel extends  ITeamModel {
  members: ITeamMemberModel[];
}

export interface ITeamMemberModel {
  userId: number;
  name: string;
}

export class TeamGenerationModel {
  teamBaseName: string;
  teamNumberFrom: number;
  teamNumberTo: number;

  constructor(baseName: string, from: number, to: number) {
    this.teamBaseName = baseName;
    this.teamNumberFrom = from;
    this.teamNumberTo = to;
  }
}
