import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { AdminCoursesComponent } from "./admin-courses.component";
import { CourseService } from "../../services/course.service";
import { GetResult } from "../../util/result";
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
    expect(component.courses().length).toBe(1);
    expect(component.courses()[0].code).toBe("NET1");
  });
});
