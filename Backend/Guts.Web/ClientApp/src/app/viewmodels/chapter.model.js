"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var exercisesummary_model_1 = require("./exercisesummary.model");
var ChapterSummaryModel = /** @class */ (function () {
    function ChapterSummaryModel(source) {
        this.id = source.id;
        this.number = source.number;
        this.userExerciseSummaries = [];
        this.averageExerciseSummaries = [];
        if (source.userExerciseSummaries) {
            for (var _i = 0, _a = source.userExerciseSummaries; _i < _a.length; _i++) {
                var exerciseSummary = _a[_i];
                var summary = new exercisesummary_model_1.ExerciseSummaryModel(exerciseSummary);
                this.userExerciseSummaries.push(summary);
            }
        }
        if (source.averageExerciseSummaries) {
            for (var _b = 0, _c = source.averageExerciseSummaries; _b < _c.length; _b++) {
                var exerciseSummary = _c[_b];
                var summary = new exercisesummary_model_1.ExerciseSummaryModel(exerciseSummary);
                this.averageExerciseSummaries.push(summary);
            }
        }
    }
    return ChapterSummaryModel;
}());
exports.ChapterSummaryModel = ChapterSummaryModel;
//# sourceMappingURL=chapter.model.js.map