import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { AssignmentService } from "../../services/assignment.service";
import { createActivatedRouteProvider } from "../../testing/test-helpers";
import { AssignmentDetailComponent } from "./assignmentdetail.component";

describe("AssignmentDetailComponent", () => {
  let component: AssignmentDetailComponent;
  let fixture: ComponentFixture<AssignmentDetailComponent>;
  let assignmentServiceSpy: jasmine.SpyObj<AssignmentService>;

  beforeEach(() => {
    assignmentServiceSpy = jasmine.createSpyObj<AssignmentService>(
      "AssignmentService",
      ["getAssignmentDetail"],
    );
    TestBed.overrideComponent(AssignmentDetailComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [AssignmentDetailComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider(),
        { provide: AssignmentService, useValue: assignmentServiceSpy },
      ],
    });
    fixture = TestBed.createComponent(AssignmentDetailComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it("ngOnChanges should load assignment detail when assignmentId > 0 and userId or teamId > 0", () => {
    assignmentServiceSpy.getAssignmentDetail.and.returnValue(
      of({
        assignmentId: 4,
        code: "Ex1",
        description: "Exercise 1",
        topicCode: "Ch1",
        courseName: ".NET",
        courseId: 1,
        tests: [],
        testResults: [],
        firstRun: null,
        lastRun: null,
        numberOfRuns: 2,
        solutionFiles: [],
      }),
    );

    component.assignmentId = 4;
    component.userId = 10;
    component.ngOnChanges({
      assignmentId: new SimpleChange(0, 4, true),
    });

    expect(assignmentServiceSpy.getAssignmentDetail).toHaveBeenCalledWith(
      4,
      10,
      0,
      component.statusDate,
    );
    expect(component.model.assignmentId).toBe(4);
  });

  it("ngOnChanges should not load assignment when userId and teamId are 0", () => {
    component.assignmentId = 4;
    component.userId = 0;
    component.teamId = 0;

    component.ngOnChanges({});

    expect(assignmentServiceSpy.getAssignmentDetail).not.toHaveBeenCalled();
  });
});
