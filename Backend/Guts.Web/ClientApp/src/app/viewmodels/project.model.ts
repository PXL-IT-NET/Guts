import { ITopicModel } from "./topic.model"
import { IAssignmentModel } from "./assignment.model";
import { ITeamModel } from "./team.model";

export interface IProjectDetailsModel extends ITopicModel {
  components: IAssignmentModel[];
  teams: ITeamModel[];
}
