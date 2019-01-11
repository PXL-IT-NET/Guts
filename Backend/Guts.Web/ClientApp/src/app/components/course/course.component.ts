import { Component } from '@angular/core';
import { CourseService } from '../../services/course.service';
import { ICourseContentsModel } from '../../viewmodels/course.model';
import { IChapterModel } from '../../viewmodels/chapter.model';
import { IProjectModel } from '../../viewmodels/project.model';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({
  templateUrl: './course.component.html'
})
export class CourseComponent {
  public course: ICourseContentsModel;
  public selectedChapter: IChapterModel;
  public selectedProject: IProjectModel;
  public loading: boolean = false;

  constructor(private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService,
    private toastr: ToastrService) {
    this.course = {
      id: 0,
      code: '',
      name: '',
      chapters: [],
      projects: []
    };
    this.selectedChapter = null;
    this.selectedProject = null;
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      let courseId = +params['courseId']; // (+) converts 'courseId' to a number

      this.loading = true;
      this.courseService.getCourseContentsById(courseId).subscribe((result) => {
        this.loading = false;
        if (result.success) {
          this.course = result.value;
          if (this.course.chapters.length > 0) {
            this.selectedChapter = this.course.chapters[0];
            this.onChapterChanged();
          } else if (this.course.projects.length > 0) {
            this.selectedProject = this.course.projects[0];
          }
        } else {
          this.toastr.error("Could not course details from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
    });
  }

  public onChapterChanged() {
    if (this.selectedChapter) {
      this.selectedProject = null;
      this.router.navigate(['chapters', this.selectedChapter.code], { relativeTo: this.route });
    }
  }

  public onProjectChanged() {
    if (this.selectedProject) {
      this.selectedChapter = null;
      this.router.navigate(['projects', this.selectedProject.code], { relativeTo: this.route });
    }
  }
}
