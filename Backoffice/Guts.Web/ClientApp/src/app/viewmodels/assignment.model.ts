export interface IAssignmentModel {
  assignmentId: number;
  code: string;
  description: string;
  tests: ITestModel[];
}

export interface ITopicAssignmentModel extends IAssignmentModel{
  topicCode: string;
  topicDescription: string;
  numberOfTests: number;
}

export interface ITestModel{
  id: number;
  testName: string;
}
