import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { AssignmentService, ExamService } from "src/app/services";
import {
  createActivatedRouteProvider,
  createToastrSpy,
} from "src/app/testing/test-helpers";
import { CreateResult, GetResult, PostResult } from "src/app/util/result";
import {
  AssignmentEvaluation,
  ExamPartModel,
} from "src/app/viewmodels/exam.model";
import { ExampartComponent } from "./exampart.component";

describe("ExampartComponent", () => {
  let component: ExampartComponent;
  let fixture: ComponentFixture<ExampartComponent>;
  let assignmentServiceSpy: jasmine.SpyObj<AssignmentService>;
  let examServiceSpy: jasmine.SpyObj<ExamService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    assignmentServiceSpy = jasmine.createSpyObj<AssignmentService>(
      "AssignmentService",
      ["getAssignmentsOfCourse"],
    );
    examServiceSpy = jasmine.createSpyObj<ExamService>("ExamService", [
      "saveExamPart",
      "deleteExamPart",
    ]);
    toastrSpy = createToastrSpy();

    TestBed.overrideComponent(ExampartComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [ExampartComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider({ courseId: "2" }),
        { provide: AssignmentService, useValue: assignmentServiceSpy },
        { provide: ExamService, useValue: examServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ExampartComponent);
    component = fixture.componentInstance;
    component.examId = 8;
  });

  it("ngOnInit should load and sort assignments and format assignment names", () => {
    assignmentServiceSpy.getAssignmentsOfCourse.and.returnValue(
      of(
        GetResult.success([
          {
            assignmentId: 2,
            code: "Ex2",
            description: "B Exercise",
            topicCode: "Ch1",
            topicDescription: "Chapter 1",
            numberOfTests: 3,
            tests: [],
          },
          {
            assignmentId: 1,
            code: "Ex1",
            description: "A Exercise",
            topicCode: "Ch1",
            topicDescription: "Chapter 1",
            numberOfTests: 2,
            tests: [],
          },
        ]),
      ),
    );

    component.ngOnInit();

    expect(component.allAssignments[0].assignmentId).toBe(1);
    expect(component.getAssignmentName(1)).toBe("Chapter 1 - A Exercise");
    expect(component.getAssignmentName(999)).toBe("???");
    expect(component.assignmentId).toBeTruthy();
    expect(component.maximumScore).toBeTruthy();
    expect(component.numberOfTestsAlreadyGreenAtStart).toBeTruthy();
  });

  it("addAssignment, saveExampart, and deleteExampart should manage exam part", () => {
    assignmentServiceSpy.getAssignmentsOfCourse.and.returnValue(
      of(GetResult.success([])),
    );
    component.ngOnInit();

    // Saving without assignmentEvaluations shows error
    component.saveExampart();
    expect(toastrSpy.error).toHaveBeenCalledWith(
      "An exam part must have at least one assignment evaluation",
    );

    component.newAssigmnentEvaluation = new AssignmentEvaluation({
      id: 0,
      assignmentId: 1,
      maximumScore: 5,
      numberOfTestsAlreadyGreenAtStart: 0,
    });
    component.addAssignment();
    expect(component.model.assignmentEvaluations.length).toBe(1);

    const savedPart = new ExamPartModel();
    savedPart.id = 15;
    examServiceSpy.saveExamPart.and.returnValue(
      of(CreateResult.success(savedPart)),
    );
    let addedEmitted: ExamPartModel;
    component.examPartAdded.subscribe((p) => (addedEmitted = p));

    component.saveExampart();
    expect(addedEmitted).toBe(savedPart);

    examServiceSpy.deleteExamPart.and.returnValue(of(PostResult.success()));
    let deletedEmitted: ExamPartModel;
    component.examPartDeleted.subscribe((p) => (deletedEmitted = p));
    component.model = savedPart;
    component.deleteExampart();
    expect(deletedEmitted).toBe(savedPart);
  });
});
