"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    }
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
var Result = /** @class */ (function () {
    function Result() {
        this.success = false;
    }
    Result.fromHttpErrorResponse = function (response) {
        var result = {
            success: false
        };
        var message = '';
        if (response.error instanceof Object) {
            var messageContainer = response.error;
            for (var propertyName in messageContainer) {
                if (messageContainer.hasOwnProperty(propertyName)) {
                    if (propertyName === '0') {
                        message = messageContainer;
                    }
                    else if (messageContainer[propertyName] instanceof Array) {
                        var lines = messageContainer[propertyName];
                        for (var _i = 0, lines_1 = lines; _i < lines_1.length; _i++) {
                            var line = lines_1[_i];
                            message += line + '\n';
                        }
                    }
                }
            }
        }
        else if (response.error instanceof String) {
            message = response.error;
        }
        else if (response.status >= 500) {
            message = "There is a technical problem with the Guts server. (status: " + response.status + " " + response.statusText + ")";
            console.log("API error:");
            console.log(response);
        }
        result.message = message;
        return result;
    };
    return Result;
}());
exports.Result = Result;
var PostResult = /** @class */ (function (_super) {
    __extends(PostResult, _super);
    function PostResult() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    PostResult.success = function () {
        var result = new PostResult();
        result.success = true;
        return result;
    };
    PostResult.fromHttpErrorResponse = function (response) {
        return Result.fromHttpErrorResponse(response);
    };
    return PostResult;
}(Result));
exports.PostResult = PostResult;
var GetResult = /** @class */ (function (_super) {
    __extends(GetResult, _super);
    function GetResult() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    GetResult.success = function (value) {
        var result = new GetResult();
        result.success = true;
        result.value = value;
        return result;
    };
    GetResult.fromHttpErrorResponse = function (response) {
        var result = Result.fromHttpErrorResponse(response);
        var getResult = new GetResult();
        getResult.success = result.success;
        getResult.message = result.message;
        return getResult;
    };
    return GetResult;
}(Result));
exports.GetResult = GetResult;
//# sourceMappingURL=Result.js.map