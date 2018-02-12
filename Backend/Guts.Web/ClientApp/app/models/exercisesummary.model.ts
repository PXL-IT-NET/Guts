export class ExerciseSummaryModel {
    public exerciseId: number;
    public number: number;
    public numberOfTests: number;
    public numberOfPassedTests: number;

    constructor() {
        this.exerciseId = 0;
        this.number = 0;
        this.numberOfTests = 0;
        this.numberOfPassedTests = 0;
    }
}