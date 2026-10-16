import { SimpleChange } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { AuthService, ExamService } from "src/app/services";
import {
  createActivatedRouteProvider,
  createToastrSpy,
} from "src/app/testing/test-helpers";
import { CreateResult, GetResult } from "src/app/util/result";
import { ExamModel, ExamPartModel } from "src/app/viewmodels/exam.model";
import { UserProfile } from "src/app/viewmodels/user.model";
import { ExamComponent } from "./exam.component";

describe("ExamComponent", () => {
  let component: ExamComponent;
  let fixture: ComponentFixture<ExamComponent>;
  let examServiceSpy: jasmine.SpyObj<ExamService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    examServiceSpy = jasmine.createSpyObj<ExamService>("ExamService", [
      "getExamsOfCourse",
      "saveExam",
      "getExamResultsDownloadUrl",
    ]);
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getUserProfile",
    ]);
    authServiceSpy.getUserProfile.and.returnValue(of(new UserProfile()));
    toastrSpy = createToastrSpy();

    TestBed.overrideComponent(ExamComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [ExamComponent],
      providers: [
        createActivatedRouteProvider(),
        { provide: ExamService, useValue: examServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ExamComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("ngOnInit and ngOnChanges should load user profile and exams", () => {
    const exam = new ExamModel();
    exam.id = 1;
    examServiceSpy.getExamsOfCourse.and.returnValue(
      of(GetResult.success([exam])),
    );

    component.ngOnInit();
    component.courseId = 4;
    component.ngOnChanges({ courseId: new SimpleChange(0, 4, true) });

    expect(examServiceSpy.getExamsOfCourse).toHaveBeenCalledWith(4);
    expect(component.exams).toEqual([exam]);

    component.courseId = 0;
    component.ngOnChanges({ courseId: new SimpleChange(4, 0, false) });
    expect(component.exams).toEqual([]);
  });

  it("saveNewExam, onExamPartAdded, onExamPartDeleted, and downloadResults should update state and handle errors", () => {
    component.courseId = 4;
    const savedExam = new ExamModel();
    savedExam.id = 10;
    examServiceSpy.saveExam.and.returnValue(of(CreateResult.success(savedExam)));

    component.saveNewExam();
    expect(component.exams).toContain(savedExam);

    const part = new ExamPartModel();
    component.onExamPartAdded(part, savedExam);
    expect(savedExam.parts).toContain(part);

    component.onExamPartDeleted(part, savedExam);
    expect(savedExam.parts).not.toContain(part);

    examServiceSpy.getExamResultsDownloadUrl.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Download err" }),
    );
    component.downloadResults(savedExam);
    expect(toastrSpy.error).toHaveBeenCalled();
  });
});
