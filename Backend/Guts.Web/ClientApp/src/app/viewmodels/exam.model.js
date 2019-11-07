"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var ExamModel = /** @class */ (function () {
    function ExamModel(source) {
        this.id = 0;
        this.courseId = 0;
        this.name = '';
        this.maximumScore = 0;
        this.parts = [];
        this.isCollapsed = true;
        if (source) {
            this.id = source.id;
            this.courseId = source.courseId;
            this.name = source.name;
            this.maximumScore = source.maximumScore;
            this.parts = source.parts;
        }
    }
    return ExamModel;
}());
exports.ExamModel = ExamModel;
//# sourceMappingURL=exam.model.js.map