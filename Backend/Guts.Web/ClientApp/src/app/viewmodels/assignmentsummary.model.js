"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var AssignmentSummaryModel = /** @class */ (function () {
    function AssignmentSummaryModel(source) {
        this._chartData = null;
        this.assignmentId = 0;
        this.code = '';
        this.description = '';
        this.numberOfTests = 0;
        this.numberOfPassedTests = 0;
        this.numberOfFailedTests = 0;
        this.numberOfUsers = 0;
        if (source) {
            this.assignmentId = source.assignmentId;
            this.code = source.code;
            this.description = source.description;
            this.numberOfTests = source.numberOfTests;
            this.numberOfPassedTests = source.numberOfPassedTests;
            this.numberOfFailedTests = source.numberOfFailedTests;
            this.numberOfUsers = source.numberOfUsers;
        }
    }
    Object.defineProperty(AssignmentSummaryModel.prototype, "chartData", {
        get: function () {
            if (!this._chartData) {
                var numberOfNotRunnedTests = this.numberOfTests - this.numberOfPassedTests - this.numberOfFailedTests;
                this._chartData = {
                    data: [this.numberOfPassedTests, this.numberOfFailedTests, numberOfNotRunnedTests],
                    labels: ['Passed tests', 'Failed tests', 'Not runned tests'],
                    colors: [{ backgroundColor: ['#00ff00', '#ff0000', '#ffa500'] }]
                };
            }
            return this._chartData;
        },
        enumerable: true,
        configurable: true
    });
    return AssignmentSummaryModel;
}());
exports.AssignmentSummaryModel = AssignmentSummaryModel;
//# sourceMappingURL=assignmentsummary.model.js.map