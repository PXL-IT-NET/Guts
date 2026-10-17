import { FormControl } from "@angular/forms";
import { PositiveNumberValidatorDirective } from "./positive-number.directive";

describe("PositiveNumberValidatorDirective", () => {
  let directive: PositiveNumberValidatorDirective;

  beforeEach(() => {
    directive = new PositiveNumberValidatorDirective();
  });

  it("should return null for positive numbers (>= 1)", () => {
    expect(directive.validate(new FormControl(1))).toBeNull();
    expect(directive.validate(new FormControl(42))).toBeNull();
  });

  it("should return min validation error for 0 or negative numbers", () => {
    expect(directive.validate(new FormControl(0))).toEqual({
      min: { min: 1, actual: 0 },
    });
    expect(directive.validate(new FormControl(-5))).toEqual({
      min: { min: 1, actual: -5 },
    });
  });
});

