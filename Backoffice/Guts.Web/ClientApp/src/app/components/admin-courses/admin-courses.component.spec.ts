import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting } from "@angular/common/http/testing";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of, Subject } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { AdminCoursesComponent } from "./admin-courses.component";
import { CourseService } from "../../services/course.service";
import { DeletionJobService } from "../../services/deletion-job.service";
import { IDeletionJobStatusModel } from "../../viewmodels/deletion-job.model";
import { GetResult } from "../../util/result";
import { createToastrSpy } from "../../testing/test-helpers";

function createCourseJob(
  overrides: Partial<IDeletionJobStatusModel> = {},
): IDeletionJobStatusModel {
  return {
    jobId: "course-job-1",
    targetType: "Course",
    targetId: 1,
    targetLabel: "NET1 - .NET Essentials",
    status: "Pending",
    totalRows: 100,
    deletedRows: 0,
    percentage: 0,
    currentPhase: "Queued for deletion",
    createdAtUtc: "2026-11-15T12:00:00Z",
    ...overrides,
  };
}

describe("AdminCoursesComponent", () => {
  let component: AdminCoursesComponent;
  let fixture: ComponentFixture<AdminCoursesComponent>;
  let courseServiceSpy: jasmine.SpyObj<CourseService>;
  let deletionJobService: DeletionJobService;
  let getActiveJobsSpy: jasmine.Spy;
  let pollUntilCompleteSpy: jasmine.Spy;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    courseServiceSpy = jasmine.createSpyObj("CourseService", [
      "getCourses",
      "createCourse",
      "updateCourse",
      "deleteCourse",
    ]);
    courseServiceSpy.getCourses.and.returnValue(
      of(GetResult.success([{ id: 1, code: "NET1", name: ".NET Essentials" }])),
    );
    toastrSpy = createToastrSpy();

    await TestBed.configureTestingModule({
      imports: [AdminCoursesComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: CourseService, useValue: courseServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    }).compileComponents();

    deletionJobService = TestBed.inject(DeletionJobService);
    getActiveJobsSpy = spyOn(deletionJobService, "getActiveJobs").and.returnValue(
      of(GetResult.success([])),
    );
    pollUntilCompleteSpy = spyOn(deletionJobService, "pollUntilComplete");

    fixture = TestBed.createComponent(AdminCoursesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should load courses on init", () => {
    expect(component.courses()).toHaveSize(1);
    expect(component.courses()[0].code).toBe("NET1");
    expect(getActiveJobsSpy).toHaveBeenCalledWith("Course");
  });

  it("should handle loadCourses error and guard no-ops", () => {
    courseServiceSpy.getCourses.and.returnValue(
      of({
        success: false,
        isAuthenticated: true,
        message: "Load failed",
      } as GetResult<never>),
    );
    component.loadCourses();
    expect(toastrSpy.error).toHaveBeenCalledWith("Load failed", "Error");

    component.saveCourse();
    expect(courseServiceSpy.createCourse).not.toHaveBeenCalled();

    component.confirmDelete();
    expect(courseServiceSpy.deleteCourse).not.toHaveBeenCalled();

    component.startCreate();
    component.cancelForm();
    expect(component.editingCourseId()).toBeNull();
  });

  it("should create, edit, update, and delete courses with progress polling and error handling", () => {
    const course = { id: 1, code: "NET1", name: ".NET Essentials" };
    courseServiceSpy.createCourse.and.returnValue(of(GetResult.success(course)));
    component.startCreate();
    component.formCode = "NET2";
    component.formName = "Advanced .NET";
    component.saveCourse();
    expect(toastrSpy.success).toHaveBeenCalledWith("Course created.", "Success");

    courseServiceSpy.createCourse.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Create fail" } as GetResult<never>),
    );
    component.startCreate();
    component.saveCourse();
    expect(toastrSpy.error).toHaveBeenCalledWith("Create fail", "Error");

    courseServiceSpy.updateCourse.and.returnValue(of(GetResult.success(course)));
    component.startEdit(course);
    expect(component.formCode).toBe("NET1");
    component.saveCourse();
    expect(toastrSpy.success).toHaveBeenCalledWith("Course updated.", "Success");

    courseServiceSpy.updateCourse.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Update fail" } as GetResult<never>),
    );
    component.startEdit(course);
    component.saveCourse();
    expect(toastrSpy.error).toHaveBeenCalledWith("Update fail", "Error");

    component.requestDelete(course);
    component.cancelDelete();
    expect(component.coursePendingDelete()).toBeNull();

    // Chunked delete happy path: 202 -> InProgress -> Completed
    const pollSubject = new Subject<GetResult<IDeletionJobStatusModel>>();
    pollUntilCompleteSpy.and.returnValue(pollSubject.asObservable());
    courseServiceSpy.deleteCourse.and.returnValue(
      of(GetResult.success(createCourseJob())),
    );

    component.requestDelete(course);
    component.confirmDelete();
    fixture.detectChanges();

    expect(component.deletionTracker.inProgress()).toBeTrue();
    pollSubject.next(
      GetResult.success(
        createCourseJob({
          status: "InProgress",
          deletedRows: 40,
          percentage: 40,
          currentPhase: "Deleting test results",
        }),
      ),
    );
    fixture.detectChanges();
    expect(component.deletionTracker.percentage()).toBe(40);

    pollSubject.next(
      GetResult.success(
        createCourseJob({
          status: "Completed",
          deletedRows: 100,
          percentage: 100,
          currentPhase: "Completed",
        }),
      ),
    );
    pollSubject.complete();
    fixture.detectChanges();

    expect(component.deletionTracker.inProgress()).toBeFalse();
    expect(component.coursePendingDelete()).toBeNull();
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Course '.NET Essentials' deleted.",
      "Success",
    );

    // HTTP error on initial DELETE
    courseServiceSpy.deleteCourse.and.returnValue(
      of({
        success: false,
        isAuthenticated: true,
        message: "Delete fail",
      } as GetResult<IDeletionJobStatusModel>),
    );
    component.requestDelete(course);
    component.confirmDelete();
    expect(component.coursePendingDelete()).toBeNull();
    expect(toastrSpy.error).toHaveBeenCalledWith("Delete fail", "Error");

    // Job Failed during background polling
    const failSubject = new Subject<GetResult<IDeletionJobStatusModel>>();
    pollUntilCompleteSpy.and.returnValue(failSubject.asObservable());
    courseServiceSpy.deleteCourse.and.returnValue(
      of(GetResult.success(createCourseJob())),
    );
    component.requestDelete(course);
    component.confirmDelete();
    failSubject.next(
      GetResult.success(
        createCourseJob({ status: "Failed", errorMessage: "Cleanup exploded" }),
      ),
    );
    failSubject.complete();

    expect(component.coursePendingDelete()).toBeNull();
    expect(component.deletionTracker.inProgress()).toBeFalse();
    expect(toastrSpy.error).toHaveBeenCalledWith("Cleanup exploded", "Error");
  });

  it("should resume active course deletion job on reload", () => {
    const activeSubject = new Subject<GetResult<IDeletionJobStatusModel>>();
    getActiveJobsSpy.and.returnValue(
      of(
        GetResult.success([
          createCourseJob({
            status: "InProgress",
            deletedRows: 25,
            percentage: 25,
            currentPhase: "Deleting test runs",
          }),
        ]),
      ),
    );
    pollUntilCompleteSpy.and.returnValue(activeSubject.asObservable());

    const reloadedFixture = TestBed.createComponent(AdminCoursesComponent);
    const reloadedComponent = reloadedFixture.componentInstance;
    reloadedFixture.detectChanges();

    expect(reloadedComponent.deletionTracker.inProgress()).toBeTrue();
    expect(reloadedComponent.deletionTracker.percentage()).toBe(25);

    activeSubject.next(
      GetResult.success(
        createCourseJob({ status: "Completed", deletedRows: 100, percentage: 100 }),
      ),
    );
    activeSubject.complete();

    expect(reloadedComponent.deletionTracker.inProgress()).toBeFalse();
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Course 'NET1 - .NET Essentials' deleted.",
      "Success",
    );
  });
});
