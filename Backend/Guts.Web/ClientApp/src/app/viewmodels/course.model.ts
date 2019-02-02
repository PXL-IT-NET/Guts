import { ITopicModel } from "./topic.model"

export interface ICourseModel {
  id: number;
  code: string;
  name: string;
}

export interface ICourseContentsModel {
  id: number;
  code: string;
  name: string;
  chapters: ITopicModel[];
  projects: ITopicModel[];
}
