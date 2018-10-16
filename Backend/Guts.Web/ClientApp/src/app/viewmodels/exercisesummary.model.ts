export interface IExerciseSummaryModel {
    exerciseId: number;
    code: string;
    numberOfTests: number;
    numberOfPassedTests: number;
    numberOfFailedTests: number;
    numberOfUsers: number;
}

export class ExerciseSummaryModel implements IExerciseSummaryModel {
    public exerciseId: number;
    public code: string;
    public numberOfTests: number;
    public numberOfPassedTests: number;
    public numberOfFailedTests: number;
    public numberOfUsers: number;

    constructor(source: IExerciseSummaryModel) {
        this.exerciseId = source.exerciseId;
        this.code = source.code;
        this.numberOfTests = source.numberOfTests;
        this.numberOfPassedTests = source.numberOfPassedTests;
        this.numberOfFailedTests = source.numberOfFailedTests;
        this.numberOfUsers = source.numberOfUsers;
    }

    private _chartData: Object | null = null;
    get chartData(): Object {
        if (!this._chartData) {
            var numberOfNotRunnedTests = this.numberOfTests - this.numberOfPassedTests - this.numberOfFailedTests;
            this._chartData = {
                data: [this.numberOfPassedTests, this.numberOfFailedTests, numberOfNotRunnedTests],
                labels: ['Passed tests', 'Failed tests', 'Not runned tests'],
                colors: [{ backgroundColor: ['#00ff00', '#ff0000', '#ffa500'] }]
            };
        }
        return this._chartData;
    }
}
