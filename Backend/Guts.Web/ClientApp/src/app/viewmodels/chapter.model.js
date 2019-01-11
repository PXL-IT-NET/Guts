"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var assignmentstatistics_model_1 = require("./assignmentstatistics.model");
var assignmentsummary_model_1 = require("./assignmentsummary.model");
var ChapterSummaryModel = /** @class */ (function () {
    function ChapterSummaryModel(source) {
        this.id = source.id;
        this.code = source.code;
        this.description = source.description;
        this.exerciseSummaries = [];
        if (source.exerciseSummaries) {
            for (var _i = 0, _a = source.exerciseSummaries; _i < _a.length; _i++) {
                var exerciseSummary = _a[_i];
                var summary = new assignmentsummary_model_1.AssignmentSummaryModel(exerciseSummary);
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
        this.code = source.code;
        this.description = source.description;
        this.exerciseStatistics = [];
        if (source.exerciseStatistics) {
            for (var _i = 0, _a = source.exerciseStatistics; _i < _a.length; _i++) {
                var exerciseStatistic = _a[_i];
                var summary = new assignmentstatistics_model_1.AssignmentStatisticsModel(exerciseStatistic);
                this.exerciseStatistics.push(summary);
            }
        }
    }
    return ChapterStatisticsModel;
}());
exports.ChapterStatisticsModel = ChapterStatisticsModel;
//# sourceMappingURL=chapter.model.js.map