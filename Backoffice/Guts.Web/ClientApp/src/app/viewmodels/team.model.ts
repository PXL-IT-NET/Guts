export interface ITeamModel {
  id: number;
  name: string;
}

export interface ITeamDetailsModel extends  ITeamModel {
  members: string[];
}

export class TeamGenerationModel {
  teamBaseName: string;
  numberOfTeams: number;

  constructor(baseName: string, number: number) {
    this.teamBaseName = baseName;
    this.numberOfTeams = number;
  }
}
