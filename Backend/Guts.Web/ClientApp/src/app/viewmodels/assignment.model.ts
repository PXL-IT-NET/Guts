export interface IAssignmentModel {
  assignmentId: number;
  code: string;
  description: string;
}

export interface ITopicAssignmentModel extends IAssignmentModel{
  topicCode: string;
  topicDescription: string;
  numberOfTests: number;
}
