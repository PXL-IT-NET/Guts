import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { CourseService } from '../../services/course.service';
import { ICourseModel } from '../../viewmodels/course.model';

@Component({
  selector: 'app-sidebar-menu',
  templateUrl: './sidebar-menu.component.html',
  styleUrls: ['./sidebar-menu.component.scss']
})
export class SidebarMenuComponent {
  public courses: ICourseModel[];

  constructor(private authService: AuthService,
    private router: Router,
    private courseService: CourseService) {
    this.courses = [];

    this.authService.getLoggedInState().subscribe((isLoggedIn) => {
      if (isLoggedIn) {
        this.loadCourses();
      } else {
        this.courses = [];
      }
    });
  }

  public isCourseRouteActive(courseId: number) {
    return this.router.url.includes('/courses/' + courseId + '/') || this.router.url.endsWith('/courses/' + courseId);
  }

  private loadCourses() {
    this.courseService.getCourses().subscribe((result) => {
      if (result.success) {
        this.courses = result.value;
      }
    });
  }

}
