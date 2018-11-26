"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var exercisestatistics_model_1 = require("./exercisestatistics.model");
var exercisesummary_model_1 = require("./exercisesummary.model");
var ChapterSummaryModel = /** @class */ (function () {
    function ChapterSummaryModel(source) {
        this.id = source.id;
        this.number = source.number;
        this.exerciseSummaries = [];
        if (source.exerciseSummaries) {
            for (var _i = 0, _a = source.exerciseSummaries; _i < _a.length; _i++) {
                var exerciseSummary = _a[_i];
                var summary = new exercisesummary_model_1.ExerciseSummaryModel(exerciseSummary);
                this.exerciseSummaries.push(summary);
            }
        }
    }
    return ChapterSummaryModel;
}());
exports.ChapterSummaryModel = ChapterSummaryModel;
var ChapterStatisticsModel = /** @class */ (function () {
    function ChapterStatisticsModel(source) {
        this.id = source.id;
        this.number = source.number;
        this.exerciseStatistics = [];
        if (source.exerciseStatistics) {
            for (var _i = 0, _a = source.exerciseStatistics; _i < _a.length; _i++) {
                var exerciseStatistic = _a[_i];
                var summary = new exercisestatistics_model_1.ExerciseStatisticsModel(exerciseStatistic);
                this.exerciseStatistics.push(summary);
            }
        }
    }
    return ChapterStatisticsModel;
}());
exports.ChapterStatisticsModel = ChapterStatisticsModel;
//# sourceMappingURL=chapter.model.js.map