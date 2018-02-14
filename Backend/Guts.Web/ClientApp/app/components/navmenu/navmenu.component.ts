import { Component } from '@angular/core';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../../util/localstorage.keys';
import { Router } from '@angular/router';
import { CourseService } from '../../services/course.service';
import { ICourseModel } from '../../viewmodels/course.model';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'nav-menu',
    templateUrl: './navmenu.component.html',
   // styleUrls: ['./navmenu.component.css']
})
export class NavMenuComponent {

    public courses: ICourseModel[];
    private coursesLoaded: boolean;

    constructor(private localStorageService: LocalStorageService,
        private courseService: CourseService,
        private router: Router,
        private authService: AuthService) {
        this.coursesLoaded = false;
        this.courses = [];

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
        this.courseService.getCourses().subscribe(
            (courses: ICourseModel[]) => {
                this.courses = courses;
                this.coursesLoaded = true;
            });
    }
}
