import { Component, OnInit, OnDestroy } from '@angular/core';
import { CourseService } from '../../services/course.service';
import { AuthService } from "../../services/auth.service";
import { ICourseContentsModel } from '../../viewmodels/course.model';
import { ITopicModel } from '../../viewmodels/topic.model';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { UserProfile } from "../../viewmodels/user.model";
import { Subscription } from 'rxjs';
import { IAssignmentModel } from 'src/app/viewmodels/assignment.model';

@Component({
  templateUrl: './course.component.html'
})
export class CourseComponent implements OnInit, OnDestroy {
  public course: ICourseContentsModel;
  public selectedChapter: ITopicModel;
  public selectedExercise: IAssignmentModel;

  public selectedProject: ITopicModel;
  public selectedComponent: IAssignmentModel;

  public loading: boolean = false;
  public userProfile: UserProfile;
  public hasContent: boolean;

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
    this.selectedExercise = null;
    this.selectedProject = null;
    this.selectedComponent = null;
    this.hasContent = true;

    this.route.params.subscribe(params => {
      let courseId = +params['courseId']; // (+) converts 'courseId' to a number
      this.loadCourseContents(courseId);
    });

  }

  ngOnInit() {
    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
    });
  }

  ngOnDestroy() {
    this.userProfileSubscription.unsubscribe();
  }

  public selectChapter(chapter: ITopicModel) {
    this.selectedChapter = chapter;
    this.selectedExercise = null;
    this.selectedProject = null;
    this.selectedComponent = null;
    if (this.selectedChapter) {
      this.selectedProject = null;
      this.router.navigate(['chapters', this.selectedChapter.code, 'testresults'], { relativeTo: this.route });
    }
  }

  public selectExercise(exercise: IAssignmentModel) {
    this.selectedExercise = exercise;
    this.router.navigate(
      ['chapters', this.selectedChapter.code, 'testresults'], 
      { relativeTo: this.route, queryParams: { assignmentId: exercise.assignmentId } });
  }

  public selectProject(project: ITopicModel, showTestResults: boolean = false) {
    this.selectedProject = project;
    this.selectedChapter = null;
    this.selectedExercise = null;
    this.selectedComponent = null;
    if (this.selectedProject) {
      this.selectedChapter = null;
      if (showTestResults) {
        this.router.navigate(['projects', this.selectedProject.code, 'testresults'], { relativeTo: this.route });
      } else{
        this.router.navigate(['projects', this.selectedProject.code, 'assessments'], { relativeTo: this.route });
      } 
    }
  }

  public get projectTestResultsOverviewActive() : boolean {
    if(!this.selectedProject) return false;
    if(this.selectedComponent) return false;
    return this.router.url.indexOf('testresults') > 0;
  }

  public get projectTestResultsDetailActive() : boolean {
    if(!this.selectedProject) return false;
    if(!this.selectedComponent) return false;
    return this.router.url.indexOf('testresults') > 0;
  }

  public selectComponent(component: IAssignmentModel) {
    this.selectedComponent = component;
    this.router.navigate(
      ['projects', this.selectedProject.code, 'testresults'], 
      { relativeTo: this.route, queryParams: { assignmentId: component.assignmentId } });
  }

  private loadCourseContents(courseId: number) {
    this.loading = true;
    this.hasContent = true;
    this.selectedChapter = null;
    this.selectedProject = null;
    this.courseService.getCourseContentsById(courseId).subscribe((result) => {
      this.loading = false;
      if (result.success) {
        this.course = result.value;
        if (this.course.chapters.length > 0) {
          this.selectChapter(this.course.chapters[0]);
        } else if (this.course.projects.length > 0) {
          this.selectProject(this.course.projects[0]);
        } else {
          this.hasContent = false;
        }
      } else {
        this.toastr.error("Could not course details from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }

}
