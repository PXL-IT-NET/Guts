"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var ExerciseStatisticsModel = /** @class */ (function () {
    function ExerciseStatisticsModel(source) {
        this._chartData = null;
        this.exerciseId = source.exerciseId;
        this.code = source.code;
        this.totalNumberOfUsers = source.totalNumberOfUsers;
        this.testPassageStatistics = source.testPassageStatistics;
    }
    Object.defineProperty(ExerciseStatisticsModel.prototype, "chartData", {
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
    return ExerciseStatisticsModel;
}());
exports.ExerciseStatisticsModel = ExerciseStatisticsModel;
//# sourceMappingURL=exercisestatistics.model.js.map