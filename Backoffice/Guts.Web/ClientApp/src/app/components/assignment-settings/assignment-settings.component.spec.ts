import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { AssignmentService, TestService } from "src/app/services";
import { createToastrSpy } from "src/app/testing/test-helpers";
import { PostResult } from "src/app/util/result";
import { IAssignmentModel, ITestModel } from "src/app/viewmodels/assignment.model";
import { AssignmentSettingsComponent } from "./assignment-settings.component";

describe("AssignmentSettingsComponent", () => {
  let component: AssignmentSettingsComponent;
  let fixture: ComponentFixture<AssignmentSettingsComponent>;
  let assignmentServiceSpy: jasmine.SpyObj<AssignmentService>;
  let testServiceSpy: jasmine.SpyObj<TestService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  const sampleAssignment: IAssignmentModel = {
    assignmentId: 5,
    code: "Ex1",
    description: "Exercise 1",
    tests: [],
  };
  const sampleTest: ITestModel = {
    id: 12,
    testName: "ShouldPass",
  };

  beforeEach(() => {
    assignmentServiceSpy = jasmine.createSpyObj<AssignmentService>(
      "AssignmentService",
      ["deleteAssignment"],
    );
    testServiceSpy = jasmine.createSpyObj<TestService>("TestService", [
      "deleteTest",
    ]);
    toastrSpy = createToastrSpy();

    TestBed.configureTestingModule({
      declarations: [AssignmentSettingsComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: TestService, useValue: testServiceSpy },
        { provide: AssignmentService, useValue: assignmentServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(AssignmentSettingsComponent);
    component = fixture.componentInstance;
    component.assignment = sampleAssignment;
    component.assignmentKind = "exercise";
  });

  it("deleteAssignment should delete assignment and emit assignmentChanged when confirmed", () => {
    spyOn(window, "confirm").and.returnValue(true);
    assignmentServiceSpy.deleteAssignment.and.returnValue(
      of(PostResult.success()),
    );
    const emitted: IAssignmentModel[] = [];
    component.assignmentChanged.subscribe((a) => emitted.push(a));

    component.deleteAssignment(sampleAssignment);

    expect(assignmentServiceSpy.deleteAssignment).toHaveBeenCalledWith(5);
    expect(toastrSpy.info).toHaveBeenCalledWith(
      "The exercise 'Exercise 1' is succesfully deleted",
      "Exercise deleted",
    );
    expect(emitted).toEqual([sampleAssignment]);
  });

  it("deleteAssignment should show error toastr when deletion fails", () => {
    spyOn(window, "confirm").and.returnValue(true);
    assignmentServiceSpy.deleteAssignment.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Forbidden" }),
    );

    component.deleteAssignment(sampleAssignment);

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Forbidden",
      "Could not delete exercise",
    );
  });

  it("deleteTest should delete test and emit assignmentChanged when confirmed", () => {
    spyOn(window, "confirm").and.returnValue(true);
    testServiceSpy.deleteTest.and.returnValue(of(PostResult.success()));
    const emitted: IAssignmentModel[] = [];
    component.assignmentChanged.subscribe((a) => emitted.push(a));

    component.deleteTest(sampleAssignment, sampleTest);

    expect(testServiceSpy.deleteTest).toHaveBeenCalledWith(12);
    expect(toastrSpy.info).toHaveBeenCalled();
    expect(emitted).toEqual([sampleAssignment]);
  });

  it("deleteTest should show error toastr when deletion fails", () => {
    spyOn(window, "confirm").and.returnValue(true);
    testServiceSpy.deleteTest.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Error" }),
    );

    component.deleteTest(sampleAssignment, sampleTest);

    expect(toastrSpy.error).toHaveBeenCalledWith("Error", "Could not delete test");
  });
});
