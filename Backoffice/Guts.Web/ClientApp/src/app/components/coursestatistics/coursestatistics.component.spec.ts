import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ActivatedRoute, Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { CourseStatisticsComponent } from "./coursestatistics.component";
import { CourseService } from "../../services/course.service";
import { AuthService } from "../../services/auth.service";
import { PeriodProvider } from "../../services/period.provider";
import {
  createRouterSpy,
  createToastrSpy,
} from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import { UserProfile } from "../../viewmodels/user.model";
import { ICourseStatisticsModel } from "../../viewmodels/course.model";

describe("CourseStatisticsComponent", () => {
  let component: CourseStatisticsComponent;
  let fixture: ComponentFixture<CourseStatisticsComponent>;
  let routerSpy: ReturnType<typeof createRouterSpy>;
  let courseServiceSpy: jasmine.SpyObj<CourseService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let periodProvider: PeriodProvider;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  const mockStats: ICourseStatisticsModel = {
    courseId: 1,
    courseCode: "CS101",
    courseName: "Algorithms",
    totalAssignments: 4,
    totalTests: 10,
    students: [
      {
        studentId: 1,
        firstName: "Bob",
        lastName: "Zeta",
        fullName: "Bob Zeta",
        assignmentsWorkedOn: 2,
        totalPassedTests: 4,
        totalTests: 10,
        completionPercentage: 40.0,
        chapters: [
          {
            chapterId: 10,
            code: "ch01",
            description: "Intro",
            assignments: [
              {
                assignmentId: 100,
                code: "ex01",
                description: "Basic",
                numberOfPassedTests: 2,
                numberOfTests: 5,
              },
            ],
          },
        ],
      },
      {
        studentId: 2,
        firstName: "Alice",
        lastName: "Alpha",
        fullName: "Alice Alpha",
        assignmentsWorkedOn: 4,
        totalPassedTests: 10,
        totalTests: 10,
        completionPercentage: 100.0,
        chapters: [
          {
            chapterId: 10,
            code: "ch01",
            description: "Intro",
            assignments: [
              {
                assignmentId: 100,
                code: "ex01",
                description: "Basic",
                numberOfPassedTests: 5,
                numberOfTests: 5,
              },
            ],
          },
        ],
      },
    ],
  };

  beforeEach(() => {
    routerSpy = createRouterSpy("/courses/1/statistics");
    courseServiceSpy = jasmine.createSpyObj<CourseService>("CourseService", [
      "getCourseStatistics",
    ]);
    courseServiceSpy.getCourseStatistics.and.returnValue(
      of(GetResult.success(mockStats)),
    );

    const lectorProfile = new UserProfile();
    lectorProfile.roles = ["Lector"];

    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getUserProfile",
    ]);
    authServiceSpy.getUserProfile.and.returnValue(of(lectorProfile));

    periodProvider = new PeriodProvider();
    periodProvider.period = {
      id: 5,
      description: "2026-2027",
      from: null,
      until: null,
      isActive: true,
    };

    toastrSpy = createToastrSpy();

    TestBed.configureTestingModule({
      declarations: [CourseStatisticsComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: CourseService, useValue: courseServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: PeriodProvider, useValue: periodProvider },
        { provide: ToastrService, useValue: toastrSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === "courseId" ? "1" : null),
              },
            },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(CourseStatisticsComponent);
    component = fixture.componentInstance;
  });

  it("should create and load statistics on init", () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(component.courseId).toBe(1);
    expect(courseServiceSpy.getCourseStatistics).toHaveBeenCalledWith(1, 5);
    expect(component.model).toEqual(mockStats);
  });

  it("should redirect non-lectors to course page", () => {
    const studentProfile = new UserProfile();
    studentProfile.roles = ["Student"];
    authServiceSpy.getUserProfile.and.returnValue(of(studentProfile));

    fixture = TestBed.createComponent(CourseStatisticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(toastrSpy.error).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(["/courses", 1]);
  });

  it("should sort students by lastName ascending by default", () => {
    fixture.detectChanges();
    const sorted = component.sortedStudents;
    expect(sorted).toHaveSize(2);
    expect(sorted[0].lastName).toBe("Alpha");
    expect(sorted[1].lastName).toBe("Zeta");
  });

  it("should toggle lastName sort direction when clicked again", () => {
    fixture.detectChanges();
    component.setSort("lastName");
    expect(component.sortDirection).toBe("desc");
    const sorted = component.sortedStudents;
    expect(sorted[0].lastName).toBe("Zeta");
    expect(sorted[1].lastName).toBe("Alpha");
  });

  it("should sort by firstName ascending and descending", () => {
    fixture.detectChanges();
    component.setSort("firstName");
    expect(component.sortField).toBe("firstName");
    expect(component.sortDirection).toBe("asc");

    let sorted = component.sortedStudents;
    expect(sorted[0].firstName).toBe("Alice");
    expect(sorted[1].firstName).toBe("Bob");

    component.setSort("firstName");
    expect(component.sortDirection).toBe("desc");
    sorted = component.sortedStudents;
    expect(sorted[0].firstName).toBe("Bob");
    expect(sorted[1].firstName).toBe("Alice");
  });

  it("should sort by assignmentsWorkedOn ascending and descending", () => {
    fixture.detectChanges();
    component.setSort("assignmentsWorkedOn");
    expect(component.sortField).toBe("assignmentsWorkedOn");
    expect(component.sortDirection).toBe("asc");

    let sorted = component.sortedStudents;
    expect(sorted[0].assignmentsWorkedOn).toBe(2);
    expect(sorted[1].assignmentsWorkedOn).toBe(4);

    component.setSort("assignmentsWorkedOn");
    expect(component.sortDirection).toBe("desc");
    sorted = component.sortedStudents;
    expect(sorted[0].assignmentsWorkedOn).toBe(4);
    expect(sorted[1].assignmentsWorkedOn).toBe(2);
  });

  it("should sort by completionPercentage ascending and descending", () => {
    fixture.detectChanges();
    component.setSort("completionPercentage");
    expect(component.sortField).toBe("completionPercentage");
    expect(component.sortDirection).toBe("asc");

    let sorted = component.sortedStudents;
    expect(sorted[0].completionPercentage).toBe(40.0);
    expect(sorted[1].completionPercentage).toBe(100.0);

    component.setSort("completionPercentage");
    expect(component.sortDirection).toBe("desc");
    sorted = component.sortedStudents;
    expect(sorted[0].completionPercentage).toBe(100.0);
    expect(sorted[1].completionPercentage).toBe(40.0);
  });

  it("should toggle student accordion expansion", () => {
    fixture.detectChanges();
    expect(component.isExpanded(1)).toBeFalse();

    component.toggleStudent(1);
    expect(component.isExpanded(1)).toBeTrue();

    component.toggleStudent(1);
    expect(component.isExpanded(1)).toBeFalse();
  });

  it("should return correct progress bar classes", () => {
    expect(component.getProgressBarClass(100)).toBe("bg-success");
    expect(component.getProgressBarClass(75)).toBe("bg-primary");
    expect(component.getProgressBarClass(20)).toBe("bg-warning");
    expect(component.getProgressBarClass(0)).toBe("bg-secondary");
  });

  it("should reload statistics when period changes", () => {
    fixture.detectChanges();
    expect(courseServiceSpy.getCourseStatistics).toHaveBeenCalledTimes(1);

    periodProvider.period = {
      id: 6,
      description: "2027-2028",
      from: null,
      until: null,
      isActive: true,
    };

    expect(courseServiceSpy.getCourseStatistics).toHaveBeenCalledWith(1, 6);
    expect(courseServiceSpy.getCourseStatistics).toHaveBeenCalledTimes(2);
  });
});
