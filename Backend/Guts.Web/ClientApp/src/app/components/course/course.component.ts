import { Component } from '@angular/core';
import { CourseService } from '../../services/course.service';
import { ICourseContentsModel } from '../../viewmodels/course.model';
import { IChapterModel } from '../../viewmodels/chapter.model';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  templateUrl: './course.component.html'
})
export class CourseComponent {
  public course: ICourseContentsModel;
  public selectedChapter: IChapterModel;

  constructor(private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService) {
    this.course = {
      id: 0,
      code: '',
      name: '',
      chapters: []
    };
    this.selectedChapter = null;
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      let courseId = +params['courseId']; // (+) converts 'courseId' to a number
      
      this.courseService.getCourseContentsById(courseId).subscribe((courseContents: ICourseContentsModel) => {
        this.course = courseContents;
        if (courseContents.chapters.length > 0) {
          this.selectedChapter = courseContents.chapters[0];
          this.onChapterChanged();
        }
      });
    });
  }

  public onChapterChanged() {
    this.router.navigate(['chapters', this.selectedChapter.number], { relativeTo: this.route });
  }
}
