import { IExerciseSummaryModel, ExerciseSummaryModel } from "./exercisesummary.model"

export interface IChapterContentsModel {
    exercises: IExerciseSummaryModel[];
}

export class ChapterContentsModel implements IChapterContentsModel {
    public exercises: ExerciseSummaryModel[];

    constructor(source: IChapterContentsModel) {
        this.exercises = [];
        if (source.exercises) {
            for (var exerciseSummary of source.exercises) {
                var summary = new ExerciseSummaryModel(exerciseSummary);
                this.exercises.push(summary);
            }
        }
    }
}