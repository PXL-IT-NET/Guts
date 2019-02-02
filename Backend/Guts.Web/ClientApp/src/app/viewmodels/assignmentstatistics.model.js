"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var AssignmentStatisticsModel = /** @class */ (function () {
    function AssignmentStatisticsModel(source) {
        this._chartData = null;
        this.assignmentId = 0;
        this.code = '';
        this.description = '';
        this.totalNumberOfUsers = 0;
        this.testPassageStatistics = [];
        if (source) {
            this.assignmentId = source.assignmentId;
            this.code = source.code;
            this.description = source.description;
            this.totalNumberOfUsers = source.totalNumberOfUsers;
            this.testPassageStatistics = source.testPassageStatistics;
        }
    }
    Object.defineProperty(AssignmentStatisticsModel.prototype, "chartData", {
        get: function () {
            if (!this._chartData) {
                var data = [];
                var labels = [];
                for (var _i = 0, _a = this.testPassageStatistics; _i < _a.length; _i++) {
                    var statistic = _a[_i];
                    labels.push(statistic.amountOfPassedTestsRange);
                    data.push(statistic.numberOfUsers);
                }
                this._chartData = {
                    labels: labels,
                    datasets: [
                        {
                            data: data,
                            label: 'Students'
                        }
                    ],
                };
            }
            return this._chartData;
        },
        enumerable: true,
        configurable: true
    });
    return AssignmentStatisticsModel;
}());
exports.AssignmentStatisticsModel = AssignmentStatisticsModel;
//# sourceMappingURL=assignmentstatistics.model.js.map