import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NgDatetimeComponent } from "./ng-datetime.component";

describe("NgDatetimeComponent", () => {
  let component: NgDatetimeComponent;
  let fixture: ComponentFixture<NgDatetimeComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [NgDatetimeComponent],
    });
    fixture = TestBed.createComponent(NgDatetimeComponent);
    component = fixture.componentInstance;
  });

  it("writeValue and date setter should format date into datetime-local string", () => {
    const d = new Date(2026, 4, 7, 9, 5);
    component.writeValue(d);

    expect(component._dateString).toBe("2026-05-07T09:05");
  });

  it("onDateChange should emit dateChange and call registered onChange for valid date strings", () => {
    const changeSpy = jasmine.createSpy("onChange");
    component.registerOnChange(changeSpy);
    component.registerOnTouched(() => {});
    component.setDisabledState(false);

    let emittedDate: Date = null;
    component.dateChange.subscribe((d) => (emittedDate = d));

    component.onDateChange("2026-06-15T14:30");

    expect(component._dateString).toBe("2026-06-15T14:30");
    expect(emittedDate instanceof Date).toBeTrue();
    expect(changeSpy).toHaveBeenCalled();

    // Invalid date string should be ignored
    changeSpy.calls.reset();
    component.onDateChange("not-a-date");
    expect(changeSpy).not.toHaveBeenCalled();
  });
});
