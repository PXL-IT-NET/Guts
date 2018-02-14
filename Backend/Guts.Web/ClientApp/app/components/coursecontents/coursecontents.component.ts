import { Component } from '@angular/core';
import { CourseService } from '../../services/course.service';
import { ICourseContentsModel } from '../../viewmodels/course.model';
import { ActivatedRoute } from '@angular/router';


@Component({
    templateUrl: './coursecontents.component.html'
})
export class CourseContentsComponent {
    public course: ICourseContentsModel;

    constructor(private route: ActivatedRoute,
        private courseService: CourseService) {
        this.course = {
            id: 0,
            code: '',
            name: '',
            chapters: []
        };
    }

    ngOnInit() {
        this.route.params.subscribe(params => {
            let courseId = +params['courseId']; // (+) converts string 'courseId' to a number
            this.courseService.getCourseContentsById(courseId).subscribe((courseContents: ICourseContentsModel) => {
                this.course = courseContents;
            });
        });
    }
}
