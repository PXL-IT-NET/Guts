import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { createActivatedRouteProvider } from "../../testing/test-helpers";
import { CourseConfigComponent } from "./courseconfig.component";

describe("CourseConfigComponent", () => {
  let component: CourseConfigComponent;
  let fixture: ComponentFixture<CourseConfigComponent>;

  beforeEach(() => {
    TestBed.overrideComponent(CourseConfigComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [CourseConfigComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [createActivatedRouteProvider({ courseId: "7" })],
    });
    fixture = TestBed.createComponent(CourseConfigComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it("ngOnInit should read courseId from route params", () => {
    component.ngOnInit();
    expect(component.courseId).toBe(7);
  });
});

