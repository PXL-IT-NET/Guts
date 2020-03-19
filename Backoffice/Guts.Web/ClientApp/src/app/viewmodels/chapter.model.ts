import { ITopicModel } from "./topic.model"
import { IUserModel } from "./user.model"
import { IAssignmentModel} from "./assignment.model";

export interface IChapterDetailsModel extends ITopicModel {
  exercises: IAssignmentModel[];
  users: IUserModel[];
}

