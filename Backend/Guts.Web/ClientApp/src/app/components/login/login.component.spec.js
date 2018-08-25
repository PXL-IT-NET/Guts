"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
require("jasmine");
var testing_1 = require("@angular/core/testing");
var login_component_1 = require("./login.component");
describe('Login component', function () {
    var component;
    var fixture;
    beforeEach(testing_1.async(function () {
        testing_1.TestBed.configureTestingModule({
            declarations: [login_component_1.LoginComponent]
        })
            .compileComponents();
    }));
    beforeEach(function () {
        fixture = testing_1.TestBed.createComponent(login_component_1.LoginComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('Should do something', testing_1.async(function () {
        expect(true).toBeTruthy();
    }));
});
//# sourceMappingURL=login.component.spec.js.map