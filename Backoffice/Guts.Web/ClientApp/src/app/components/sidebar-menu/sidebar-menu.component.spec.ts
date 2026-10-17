import { SidebarMenuComponent } from "./sidebar-menu.component";
import { AuthService } from "../../services/auth.service";
import { CourseService } from "../../services/course.service";
import { GetResult } from "../../util/result";
import { UserProfile } from "../../viewmodels/user.model";
import { BehaviorSubject, of } from "rxjs";
import {
  createRouterSpy,
} from "../../testing/test-helpers";

describe("SidebarMenuComponent", () => {
  let component: SidebarMenuComponent;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: ReturnType<typeof createRouterSpy>;
  let courseServiceSpy: jasmine.SpyObj<CourseService>;
  let loggedInSubject: BehaviorSubject<boolean>;

  beforeEach(() => {
    loggedInSubject = new BehaviorSubject<boolean>(false);
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getLoggedInState",
      "getUserProfile",
    ]);
    authServiceSpy.getLoggedInState.and.returnValue(
      loggedInSubject.asObservable(),
    );

    routerSpy = createRouterSpy("/");
    courseServiceSpy = jasmine.createSpyObj<CourseService>("CourseService", [
      "getCourses",
    ]);

    component = new SidebarMenuComponent(
      authServiceSpy,
      routerSpy,
      courseServiceSpy,
    );
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("should load courses and userProfile when logged in and clear them when logged out", () => {
    const courses = [{ id: 1, code: "C1", name: "Course 1" }];
    const profile = new UserProfile({
      id: 1,
      email: "lector@pxl.be",
      roles: ["Lector"],
      teams: [],
      isAuthenticated: true,
    });
    courseServiceSpy.getCourses.and.returnValue(of(GetResult.success(courses)));
    authServiceSpy.getUserProfile.and.returnValue(of(profile));

    component.ngOnInit();
    expect(component.courses).toEqual([]);

    loggedInSubject.next(true);
    expect(courseServiceSpy.getCourses).toHaveBeenCalled();
    expect(authServiceSpy.getUserProfile).toHaveBeenCalled();
    expect(component.courses).toEqual(courses);
    expect(component.userProfile).toBe(profile);

    loggedInSubject.next(false);
    expect(component.courses).toEqual([]);
    expect(component.userProfile.isLector()).toBeFalse();
  });

  it("should detect active teacher-docs and course routes", () => {
    (routerSpy as any).url = "/teacher-docs/concepts";
    expect(component.isTeacherDocsRouteActive).toBeTrue();
    expect(component.isCourseRouteActive(1)).toBeFalse();

    (routerSpy as any).url = "/courses/1";
    expect(component.isTeacherDocsRouteActive).toBeFalse();
    expect(component.isCourseRouteActive(1)).toBeTrue();
    expect(component.isCourseRouteActive(10)).toBeFalse();

    (routerSpy as any).url = "/courses/1/chapters/2";
    expect(component.isCourseRouteActive(1)).toBeTrue();
    expect(component.isCourseRouteActive(2)).toBeFalse();
  });
});

