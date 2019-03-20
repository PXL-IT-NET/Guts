"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var UserProfile = /** @class */ (function () {
    function UserProfile(source) {
        this.id = 0;
        this.roles = [];
        if (source) {
            this.id = source.id;
            this.roles = source.roles;
        }
    }
    UserProfile.prototype.isLector = function () {
        return this.roles.indexOf('lector') >= 0;
    };
    return UserProfile;
}());
exports.UserProfile = UserProfile;
//# sourceMappingURL=user.model.js.map