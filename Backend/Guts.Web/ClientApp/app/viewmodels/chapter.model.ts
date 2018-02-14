import { IExerciseSummaryModel, ExerciseSummaryModel } from "./exercisesummary.model"

export interface IChapterModel {
    id: number;
    number: number;
}

export interface IChapterContentsModel extends IChapterModel {
    userExerciseSummaries: IExerciseSummaryModel[];
    averageExerciseSummaries: IExerciseSummaryModel[];
}



export class ChapterContentsModel implements IChapterContentsModel {
    public id: number;
    public number: number;
    public userExerciseSummaries: ExerciseSummaryModel[];
    public averageExerciseSummaries: ExerciseSummaryModel[];

    constructor(source: IChapterContentsModel) {
        this.id = source.id;
        this.number = source.number;
        this.userExerciseSummaries = [];
        this.averageExerciseSummaries = [];

        if (source.userExerciseSummaries) {
            for (let exerciseSummary of source.userExerciseSummaries) {
                let summary = new ExerciseSummaryModel(exerciseSummary);
                this.userExerciseSummaries.push(summary);
            }
        }

        if (source.averageExerciseSummaries) {
            for (let exerciseSummary of source.averageExerciseSummaries) {
                let summary = new ExerciseSummaryModel(exerciseSummary);
                this.averageExerciseSummaries.push(summary);
            }
        }
    }
}

