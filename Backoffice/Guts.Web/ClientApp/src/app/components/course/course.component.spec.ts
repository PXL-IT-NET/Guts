import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NavigationEnd, Router } from "@angular/router";
import { BsModalService } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { of, Subject } from "rxjs";
import { PeriodProvider } from "src/app/services";
import { AuthService } from "../../services/auth.service";
import { CourseService } from "../../services/course.service";
import {
  createModalRefSpy,
  createModalServiceSpy,
  createRouterSpy,
  createToastrSpy,
} from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import { UserProfile } from "../../viewmodels/user.model";
import { CourseComponent } from "./course.component";

describe("CourseComponent", () => {
  let component: CourseComponent;
  let fixture: ComponentFixture<CourseComponent>;
  let routerSpy: ReturnType<typeof createRouterSpy>;
  let courseServiceSpy: jasmine.SpyObj<CourseService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let periodProvider: PeriodProvider;
  let toastrSpy: jasmine.SpyObj<ToastrService>;
  let modalServiceSpy: jasmine.SpyObj<BsModalService>;

  beforeEach(() => {
    routerSpy = createRouterSpy("/courses/1/chapters/Ch1/testresults");
    courseServiceSpy = jasmine.createSpyObj<CourseService>("CourseService", [
      "getCourseContentsById",
    ]);
    courseServiceSpy.getCourseContentsById.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "C1",
          name: "Course 1",
          chapters: [],
          projects: [],
        }),
      ),
    );
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getUserProfile",
    ]);
    authServiceSpy.getUserProfile.and.returnValue(of(new UserProfile()));
    periodProvider = new PeriodProvider();
    periodProvider.period = {
      id: 2,
      description: "25-26",
      from: null,
      until: null,
      isActive: true,
    };
    toastrSpy = createToastrSpy();
    modalServiceSpy = createModalServiceSpy();

    TestBed.overrideComponent(CourseComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [CourseComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: CourseService, useValue: courseServiceSpy },
        { provide: PeriodProvider, useValue: periodProvider },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
        { provide: BsModalService, useValue: modalServiceSpy },
      ],
    });
    fixture = TestBed.createComponent(CourseComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("ngOnInit should load course contents and select matching chapter and exercise", () => {
    routerSpy.url = "/courses/1/chapters/Ch1/testresults?assignmentId=10";
    const chapter = {
      id: 5,
      code: "Ch1",
      description: "Chapter 1",
      assignments: [{ assignmentId: 10, code: "Ex1", description: "Ex 1", tests: [] }],
    };
    courseServiceSpy.getCourseContentsById.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "C1",
          name: "Course 1",
          chapters: [chapter],
          projects: [],
        }),
      ),
    );

    component.ngOnInit();

    expect(courseServiceSpy.getCourseContentsById).toHaveBeenCalledWith(1, 2);
    expect(component.selectedChapter).toEqual(chapter);
    expect(component.selectedExercise).toEqual(chapter.assignments[0]);
  });

  it("should select project and component when course contains projects", () => {
    routerSpy.url = "/courses/1/projects/P1/testresults?assignmentId=20";
    const project = {
      id: 6,
      code: "P1",
      description: "Project 1",
      assignments: [{ assignmentId: 20, code: "Comp1", description: "C1", tests: [] }],
    };
    courseServiceSpy.getCourseContentsById.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "C1",
          name: "Course 1",
          chapters: [],
          projects: [project],
        }),
      ),
    );

    component.ngOnInit();
    routerSpy.events$.next(new NavigationEnd(1, routerSpy.url, routerSpy.url));

    expect(component.selectedProject).toEqual(project);
    expect(component.selectedComponent).toEqual(project.assignments[0]);
    expect(component.projectTestResultsDetailActive).toBeTrue();
    expect(component.projectTestResultsOverviewActive).toBeFalse();
  });

  it("should set hasContent=false when course has no chapters and no projects", () => {
    routerSpy.url = "/courses/1";
    courseServiceSpy.getCourseContentsById.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "C1",
          name: "Empty Course",
          chapters: [],
          projects: [],
        }),
      ),
    );

    component.ngOnInit();

    expect(component.hasContent).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(["courses", 1]);
  });

  it("navigation helpers and showAddProjectModal should navigate and open modal", () => {
    component.course.id = 1;
    const chapter = { id: 1, code: "Ch1", description: "C", assignments: [] };
    const project = { id: 2, code: "P1", description: "P", assignments: [] };
    const assignment = { assignmentId: 5, code: "A1", description: "A", tests: [] };

    component.navigateToChapter(chapter);
    expect(routerSpy.navigate).toHaveBeenCalledWith([
      "courses",
      1,
      "chapters",
      "Ch1",
      "testresults",
    ]);

    component.selectedChapter = chapter;
    component.navigateToExercise(assignment);
    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ["courses", 1, "chapters", "Ch1", "testresults"],
      { queryParams: { assignmentId: 5 } },
    );

    component.navigateToProject(project, true);
    expect(routerSpy.navigate).toHaveBeenCalledWith([
      "courses",
      1,
      "projects",
      "P1",
      "testresults",
    ]);

    component.selectedProject = project;
    component.navigateToComponent(assignment);
    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ["courses", 1, "projects", "P1", "testresults"],
      { queryParams: { assignmentId: 5 } },
    );

    const projectAdded$ = new Subject<any>();
    const modalRef = createModalRefSpy({ projectAdded: projectAdded$ });
    modalServiceSpy.show.and.returnValue(modalRef);

    component.showAddProjectModal();
    projectAdded$.next(project);

    expect(modalServiceSpy.show).toHaveBeenCalled();
    expect(component.course.id).toBe(0);
  });

  it("isChapterTestResultOverviewRouteActive should return false when no chapter is selected", () => {
    component.selectedChapter = null;
    routerSpy.url = "/courses/1/projects/P1/testresults";

    expect(component.isChapterTestResultOverviewRouteActive()).toBeFalse();
  });
});

