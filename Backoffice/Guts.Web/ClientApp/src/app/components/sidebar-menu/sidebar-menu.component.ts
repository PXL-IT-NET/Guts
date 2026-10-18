import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { AuthService } from "../../services/auth.service";
import { Router } from "@angular/router";
import { CourseService } from "../../services/course.service";
import { ICourseModel } from "../../viewmodels/course.model";
import { UserProfile } from "src/app/viewmodels/user.model";

@Component({
  standalone: false,
  selector: "app-sidebar-menu",
  templateUrl: "./sidebar-menu.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ["./sidebar-menu.component.scss"],
})
export class SidebarMenuComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);

  private _courses = signal<ICourseModel[]>([]);
  public get courses(): ICourseModel[] {
    return this._courses();
  }
  public set courses(v: ICourseModel[]) {
    this._courses.set(v);
  }

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  constructor(
    private authService: AuthService,
    private router: Router,
    private courseService: CourseService,
  ) {}

  ngOnInit(): void {
    this.authService
      .getLoggedInState()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isLoggedIn) => {
        if (isLoggedIn) {
          this.loadCourses();

          // retrieve the user profile when logged in
          this.authService
            .getUserProfile()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((profile) => {
              this.userProfile = profile;
            });
        } else {
          this.courses = [];
          this.userProfile = new UserProfile();
        }
      });
  }

  public get isTeacherDocsRouteActive(): boolean {
    return this.router.url.includes("/teacher-docs");
  }

  public isCourseRouteActive(courseId: number) {
    return (
      this.router.url.includes("/courses/" + courseId + "/") ||
      this.router.url.endsWith("/courses/" + courseId)
    );
  }

  private loadCourses() {
    this.courseService
      .getCourses()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.success) {
          this.courses = result.value;
        }
      });
  }

  ngOnDestroy(): void {}
}
