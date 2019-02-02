export interface ITeamModel {
  id: number;
  name: string;
}

export interface ITeamDetailsModel extends  ITeamModel {
  members: string[];
}
