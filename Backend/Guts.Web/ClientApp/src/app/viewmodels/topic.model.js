"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var assignmentstatistics_model_1 = require("./assignmentstatistics.model");
var assignmentsummary_model_1 = require("./assignmentsummary.model");
var TopicStatisticsModel = /** @class */ (function () {
    function TopicStatisticsModel(source) {
        this.id = source.id;
        this.code = source.code;
        this.description = source.description;
        this.assignmentStatistics = [];
        if (source.assignmentStatistics) {
            for (var _i = 0, _a = source.assignmentStatistics; _i < _a.length; _i++) {
                var exerciseStatistic = _a[_i];
                var summary = new assignmentstatistics_model_1.AssignmentStatisticsModel(exerciseStatistic);
                this.assignmentStatistics.push(summary);
            }
        }
    }
    return TopicStatisticsModel;
}());
exports.TopicStatisticsModel = TopicStatisticsModel;
var TopicSummaryModel = /** @class */ (function () {
    function TopicSummaryModel(source) {
        this.id = source.id;
        this.code = source.code;
        this.description = source.description;
        this.assignmentSummaries = [];
        if (source.assignmentSummaries) {
            for (var _i = 0, _a = source.assignmentSummaries; _i < _a.length; _i++) {
                var assignmentSummary = _a[_i];
                var summary = new assignmentsummary_model_1.AssignmentSummaryModel(assignmentSummary);
                this.assignmentSummaries.push(summary);
            }
        }
    }
    return TopicSummaryModel;
}());
exports.TopicSummaryModel = TopicSummaryModel;
//# sourceMappingURL=topic.model.js.map