import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { AdminCoursesComponent } from "./admin-courses.component";
import { CourseService } from "../../services/course.service";
import { GetResult, PostResult } from "../../util/result";
import { createToastrSpy } from "../../testing/test-helpers";

describe("AdminCoursesComponent", () => {
  let component: AdminCoursesComponent;
  let fixture: ComponentFixture<AdminCoursesComponent>;
  let courseServiceSpy: jasmine.SpyObj<CourseService>;
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
        { provide: CourseService, useValue: courseServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminCoursesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should load courses on init", () => {
    expect(component.courses()).toHaveSize(1);
    expect(component.courses()[0].code).toBe("NET1");
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

  it("should create, edit, update, and delete courses on success and error", () => {
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

    courseServiceSpy.deleteCourse.and.returnValue(of(PostResult.success()));
    component.requestDelete(course);
    component.confirmDelete();
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Course '.NET Essentials' deleted.",
      "Success",
    );

    courseServiceSpy.deleteCourse.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Delete fail" }),
    );
    component.requestDelete(course);
    component.confirmDelete();
    expect(toastrSpy.error).toHaveBeenCalledWith("Delete fail", "Error");
  });
});
