import { TestBed } from "@angular/core/testing";
import { SidebarMenuComponent } from "./sidebar-menu.component";
import { AuthService } from "../../services/auth.service";
import { CourseService } from "../../services/course.service";
import { ScheduledJobsService } from "../../services/scheduled-jobs.service";
import { GetResult } from "../../util/result";
import { UserProfile } from "../../viewmodels/user.model";
import { BehaviorSubject, of } from "rxjs";
import { ToastrService } from "ngx-toastr";
import {
  createRouterSpy,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("SidebarMenuComponent", () => {
  let component: SidebarMenuComponent;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: ReturnType<typeof createRouterSpy>;
  let courseServiceSpy: jasmine.SpyObj<CourseService>;
  let scheduledJobsServiceSpy: jasmine.SpyObj<ScheduledJobsService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;
  let loggedInSubject: BehaviorSubject<boolean>;

  beforeEach(() => {
    loggedInSubject = new BehaviorSubject<boolean>(false);
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getLoggedInState",
      "getUserProfile",
      "isImpersonating",
    ]);
    authServiceSpy.getLoggedInState.and.returnValue(
      loggedInSubject.asObservable(),
    );

    routerSpy = createRouterSpy("/");
    courseServiceSpy = jasmine.createSpyObj<CourseService>("CourseService", [
      "getCourses",
    ]);
    scheduledJobsServiceSpy = jasmine.createSpyObj<ScheduledJobsService>(
      "ScheduledJobsService",
      ["createDashboardTicket"],
    );
    toastrSpy = createToastrSpy();

    component = TestBed.runInInjectionContext(() => new SidebarMenuComponent(
      authServiceSpy,
      routerSpy,
      courseServiceSpy,
      scheduledJobsServiceSpy,
      toastrSpy,
    ));
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

    (routerSpy as any).url = "/admin/users";
    expect(component.isAdminRouteActive).toBeTrue();

    authServiceSpy.isImpersonating.and.returnValue(true);
    expect(component.isImpersonating).toBeTrue();
  });

  it("should open Quartz dashboard in a new tab when ticket request succeeds", () => {
    const openSpy = spyOn(window, "open");
    scheduledJobsServiceSpy.createDashboardTicket.and.returnValue(
      of(GetResult.success({ loginUrl: "https://api.test/api/scheduled-jobs/dashboard-login?ticket=xyz" })),
    );

    component.openScheduledJobsDashboard();

    expect(scheduledJobsServiceSpy.createDashboardTicket).toHaveBeenCalledTimes(1);
    expect(openSpy).toHaveBeenCalledWith(
      "https://api.test/api/scheduled-jobs/dashboard-login?ticket=xyz",
      "_blank",
      "noopener,noreferrer",
    );
    expect(component.isOpeningScheduledJobs).toBeFalse();
  });

  it("should show error toast when ticket request fails", () => {
    const openSpy = spyOn(window, "open");
    const failureResult = new GetResult<{ loginUrl: string }>();
    failureResult.success = false;
    failureResult.message = "Forbidden";
    scheduledJobsServiceSpy.createDashboardTicket.and.returnValue(
      of(failureResult),
    );

    component.openScheduledJobsDashboard();

    expect(openSpy).not.toHaveBeenCalled();
    expect(toastrSpy.error).toHaveBeenCalledWith("Forbidden", "Error");
    expect(component.isOpeningScheduledJobs).toBeFalse();
  });

  it("should show default error toast when failure message is empty", () => {
    const openSpy = spyOn(window, "open");
    const failureResult = new GetResult<{ loginUrl: string }>();
    failureResult.success = false;
    scheduledJobsServiceSpy.createDashboardTicket.and.returnValue(
      of(failureResult),
    );

    component.openScheduledJobsDashboard();

    expect(openSpy).not.toHaveBeenCalled();
    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Could not open Scheduled Jobs dashboard.",
      "Error",
    );
  });
});


