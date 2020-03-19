import { Component } from '@angular/core';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../../util/localstorage.keys';
import { Router } from '@angular/router';
import { CourseService } from '../../services/course.service';
import { ICourseModel } from '../../viewmodels/course.model';
import { AuthService } from '../../services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html'
})
export class NavMenuComponent {

  public isNavbarCollapsed: boolean;
  public courses: ICourseModel[];
  
  private coursesLoaded: boolean;

  constructor(private localStorageService: LocalStorageService,
    private courseService: CourseService,
    private router: Router,
    private authService: AuthService,
    private toastr: ToastrService) {
    this.coursesLoaded = false;
    this.courses = [];
    this.isNavbarCollapsed = true;

    this.authService.getLoggedInState().subscribe((isLoggedIn) => {
      if (!this.coursesLoaded && isLoggedIn) {
        this.loadCourses();
      }
    });
  }

  ngOnInit() {
    this.loadCourses();
  }

  public isLoggedIn(): boolean {
    if (this.localStorageService.get(LocalStorageKeys.currentToken)) {
      return true;
    } else {
      return false;
    }
  }

  public logout() {
    this.localStorageService.set(LocalStorageKeys.currentToken, null);
    this.router.navigate(['/']);
  }

  private loadCourses() {
    this.courseService.getCourses().subscribe((result) => {
      if (result.success) {
        this.courses = result.value;
        this.coursesLoaded = true;
      } else if(this.isLoggedIn()) {
        this.toastr.error("Could not load courses from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }
}
