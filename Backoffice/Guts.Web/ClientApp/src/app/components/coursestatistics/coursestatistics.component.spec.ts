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

  it("should create and load statistics on init and handle API error", () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(component.courseId).toBe(1);
    expect(courseServiceSpy.getCourseStatistics).toHaveBeenCalledWith(1, 5);
    expect(component.model).toEqual(mockStats);

    courseServiceSpy.getCourseStatistics.and.returnValue(of({ success: false, message: "Stats fail" } as any));
    component.loadStatistics();
    expect(toastrSpy.error).toHaveBeenCalledWith("Stats fail", "Error");
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

  it("should sort students by lastName ascending by default and handle ties and getSortIcon", () => {
    fixture.detectChanges();
    expect(component.getSortIcon("lastName")).toBe("bi-sort-down text-primary");
    expect(component.getSortIcon("firstName")).toBe("bi-arrow-down-up text-muted");

    const sorted = component.sortedStudents;
    expect(sorted).toHaveSize(2);
    expect(sorted[0].lastName).toBe("Alpha");
    expect(sorted[1].lastName).toBe("Zeta");

    component.setSort("lastName");
    expect(component.getSortIcon("lastName")).toBe("bi-sort-up-alt text-primary");

    // Test tie-breakers when two students share same values
    component.model = {
      ...mockStats,
      students: [
        { ...mockStats.students[0], firstName: "Bob", lastName: "Same", assignmentsWorkedOn: 2, completionPercentage: 50 },
        { ...mockStats.students[1], firstName: "Alice", lastName: "Same", assignmentsWorkedOn: 2, completionPercentage: 50 },
      ],
    };
    component.sortField = "lastName";
    component.sortDirection = "asc";
    expect(component.sortedStudents[0].firstName).toBe("Alice");

    component.model = {
      ...mockStats,
      students: [
        { ...mockStats.students[0], firstName: "Same", lastName: "Zeta", assignmentsWorkedOn: 2, completionPercentage: 50 },
        { ...mockStats.students[1], firstName: "Same", lastName: "Alpha", assignmentsWorkedOn: 2, completionPercentage: 50 },
      ],
    };
    component.sortField = "firstName";
    expect(component.sortedStudents[0].lastName).toBe("Alpha");
    component.sortField = "assignmentsWorkedOn";
    expect(component.sortedStudents[0].lastName).toBe("Alpha");
    component.sortField = "completionPercentage";
    expect(component.sortedStudents[0].lastName).toBe("Alpha");

    component.model = null;
    expect(component.sortedStudents).toEqual([]);
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

  it("should sort chapters by description (case-insensitive) with code fallback and tie-breaker", () => {
    const student = {
      ...mockStats.students[0],
      chapters: [
        { chapterId: 1, code: "ch01", description: "Zeta", assignments: [] },
        { chapterId: 2, code: "ch02", description: "beta", assignments: [] },
        { chapterId: 3, code: "ch03", description: "   ", assignments: [] },
        { chapterId: 4, code: "ch04", description: "", assignments: [] },
        { chapterId: 5, code: "ch05", description: "Alpha", assignments: [] },
        { chapterId: 6, code: "ch00", description: "Alpha", assignments: [] },
      ],
    };

    const sorted = component.getSortedChapters(student);
    expect(sorted.map((c) => c.code)).toEqual([
      "ch00",
      "ch05",
      "ch02",
      "ch03",
      "ch04",
      "ch01",
    ]);
    expect(component.getSortedChapters({ ...student, chapters: [] })).toEqual([]);
    expect(
      component.getSortedChapters({
        ...student,
        chapters: undefined as unknown as typeof student.chapters,
      }),
    ).toEqual([]);
  });

  it("should sort assignments by description (case-insensitive) with code fallback and tie-breaker", () => {
    const chapter = {
      chapterId: 10,
      code: "ch01",
      description: "Chapter 1",
      assignments: [
        { assignmentId: 1, code: "ex01", description: "Zebra", numberOfPassedTests: 1, numberOfTests: 2 },
        { assignmentId: 2, code: "ex02", description: "apple", numberOfPassedTests: 2, numberOfTests: 2 },
        { assignmentId: 3, code: "ex03", description: "", numberOfPassedTests: 0, numberOfTests: 2 },
        { assignmentId: 4, code: "ex04", description: "   ", numberOfPassedTests: 0, numberOfTests: 0 },
        { assignmentId: 5, code: "ex05", description: "Banana", numberOfPassedTests: 1, numberOfTests: 2 },
        { assignmentId: 6, code: "ex00", description: "Banana", numberOfPassedTests: 1, numberOfTests: 2 },
      ],
    };

    const sorted = component.getSortedAssignments(chapter);
    expect(sorted.map((a) => a.code)).toEqual([
      "ex02",
      "ex00",
      "ex05",
      "ex03",
      "ex04",
      "ex01",
    ]);
    expect(component.getSortedAssignments({ ...chapter, assignments: [] })).toEqual([]);
    expect(
      component.getSortedAssignments({
        ...chapter,
        assignments: undefined as unknown as typeof chapter.assignments,
      }),
    ).toEqual([]);
    expect(
      component.getSortedAssignments({
        ...chapter,
        assignments: [
          { assignmentId: 1, code: undefined as unknown as string, description: "", numberOfPassedTests: 0, numberOfTests: 0 },
          { assignmentId: 2, code: undefined as unknown as string, description: "", numberOfPassedTests: 0, numberOfTests: 0 },
        ],
      }),
    ).toHaveSize(2);
  });

  it("should return trimmed chapter description or fallback to code in getChapterTitle", () => {
    expect(
      component.getChapterTitle({
        chapterId: 1,
        code: "ch01",
        description: "  Chapter 1: Intro  ",
        assignments: [],
      }),
    ).toBe("Chapter 1: Intro");

    expect(
      component.getChapterTitle({
        chapterId: 2,
        code: "ch02",
        description: "   ",
        assignments: [],
      }),
    ).toBe("ch02");

    expect(
      component.getChapterTitle({
        chapterId: 3,
        code: "ch03",
        description: "",
        assignments: [],
      }),
    ).toBe("ch03");
  });

  it("should render sorted chapters, fallback chapter title, and sorted assignments in expanded accordion", () => {
    const unsortedStats: ICourseStatisticsModel = {
      ...mockStats,
      students: [
        {
          ...mockStats.students[0],
          studentId: 42,
          chapters: [
            {
              chapterId: 20,
              code: "ch01",
              description: "Zeta Chapter",
              assignments: [
                { assignmentId: 1, code: "ex01", description: "Second Task", numberOfPassedTests: 1, numberOfTests: 2 },
                { assignmentId: 2, code: "ex02", description: "First Task", numberOfPassedTests: 2, numberOfTests: 2 },
              ],
            },
            {
              chapterId: 21,
              code: "ch02",
              description: "",
              assignments: [
                { assignmentId: 3, code: "ex09", description: "", numberOfPassedTests: 0, numberOfTests: 1 },
                { assignmentId: 4, code: "ex03", description: "Alpha Task", numberOfPassedTests: 1, numberOfTests: 1 },
              ],
            },
            {
              chapterId: 22,
              code: "ch03",
              description: "Alpha Chapter",
              assignments: [],
            },
          ],
        },
      ],
    };

    courseServiceSpy.getCourseStatistics.and.returnValue(
      of(GetResult.success(unsortedStats)),
    );
    fixture.detectChanges();

    component.toggleStudent(42);
    fixture.detectChanges();

    const chapterHeaders = Array.from(
      fixture.nativeElement.querySelectorAll(".text-primary span") as NodeListOf<HTMLElement>,
    ).map((el) => el.textContent?.trim());
    expect(chapterHeaders).toEqual(["Alpha Chapter", "ch02", "Zeta Chapter"]);

    const assignmentCodes = Array.from(
      fixture.nativeElement.querySelectorAll("td strong") as NodeListOf<HTMLElement>,
    )
      .map((el) => el.textContent?.trim())
      .filter((text) => text?.startsWith("ex"));
    expect(assignmentCodes).toEqual(["ex03", "ex09", "ex02", "ex01"]);
  });
});
