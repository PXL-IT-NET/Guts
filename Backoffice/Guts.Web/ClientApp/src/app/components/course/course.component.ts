import { Component, OnInit, OnDestroy  } from '@angular/core';
import { CourseService } from '../../services/course.service';
import { AuthService } from "../../services/auth.service";
import { ICourseContentsModel } from '../../viewmodels/course.model';
import { ITopicModel } from '../../viewmodels/topic.model';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { UserProfile } from "../../viewmodels/user.model";
import { Subscription } from 'rxjs';

@Component({
  templateUrl: './course.component.html'
})
export class CourseComponent implements OnInit, OnDestroy  {
  public course: ICourseContentsModel;
  public selectedChapter: ITopicModel;
  public selectedProject: ITopicModel;
  public loading: boolean = false;
  public userProfile: UserProfile;

  private userProfileSubscription: Subscription;

  constructor(private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService,
    private authService: AuthService,
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

    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
    });

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
            this.onProjectChanged();
          }
        } else {
          this.toastr.error("Could not course details from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
    });
  }

  ngOnDestroy() {
    this.userProfileSubscription.unsubscribe();
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
