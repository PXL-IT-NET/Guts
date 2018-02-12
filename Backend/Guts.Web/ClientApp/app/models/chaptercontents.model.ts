import { ExerciseSummaryModel } from "./exercisesummary.model"

export class ChapterContentsModel {
    public exercises: ExerciseSummaryModel[];

    constructor() {
        this.exercises = [];
    }
}