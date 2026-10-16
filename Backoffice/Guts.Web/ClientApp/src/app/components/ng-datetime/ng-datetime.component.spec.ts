import { ComponentFixture, TestBed } from "@angular/core/testing";
import moment from "moment";
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
    component.ngOnInit();
  });

  it("writeValue and date setter should format moment into datetime-local string", () => {
    const m = moment({ year: 2026, month: 4, date: 7, hour: 9, minute: 5 });
    component.writeValue(m);

    expect(component._dateString).toBe("2026-05-07T09:05");
  });

  it("onDateChange should emit dateChange and call registered onChange for valid date strings", () => {
    const changeSpy = jasmine.createSpy("onChange");
    component.registerOnChange(changeSpy);
    component.registerOnTouched(() => {});
    component.setDisabledState(false);

    let emittedMoment: moment.Moment = null;
    component.dateChange.subscribe((m) => (emittedMoment = m));

    component.onDateChange("2026-06-15T14:30");

    expect(component._dateString).toBe("2026-06-15T14:30");
    expect(moment.isMoment(emittedMoment)).toBeTrue();
    expect(changeSpy).toHaveBeenCalled();

    // Invalid date string should be ignored
    changeSpy.calls.reset();
    component.onDateChange("not-a-date");
    expect(changeSpy).not.toHaveBeenCalled();
  });
});

