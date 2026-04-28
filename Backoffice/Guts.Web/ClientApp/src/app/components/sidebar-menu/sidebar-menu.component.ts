import { ChangeDetectorRef, Component, OnDestroy, OnInit } from "@angular/core";
import { AuthService } from "../../services/auth.service";
import { Router } from "@angular/router";
import { CourseService } from "../../services/course.service";
import { ICourseModel } from "../../viewmodels/course.model";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { UserProfile } from "src/app/viewmodels/user.model";

@Component({
  standalone: false,
  selector: "app-sidebar-menu",
  templateUrl: "./sidebar-menu.component.html",
  styleUrls: ["./sidebar-menu.component.scss"],
})
export class SidebarMenuComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  public courses: ICourseModel[];
  public userProfile: UserProfile;

  constructor(
    private authService: AuthService,
    private router: Router,
    private courseService: CourseService,
    private cdr: ChangeDetectorRef,
  ) {
    this.courses = [];
    this.userProfile = new UserProfile();
  }

  ngOnInit(): void {
    this.authService
      .getLoggedInState()
      .pipe(takeUntil(this.destroy$))
      .subscribe((isLoggedIn) => {
        if (isLoggedIn) {
          this.loadCourses();

          // retrieve the user profile when logged in
          this.authService
            .getUserProfile()
            .pipe(takeUntil(this.destroy$))
            .subscribe((profile) => {
              this.userProfile = profile;

              this.cdr.detectChanges();
            });
        } else {
          this.courses = [];
        }

        this.cdr.detectChanges();
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
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        if (result.success) {
          this.courses = result.value;
        }

        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
