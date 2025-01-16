import { Component, OnInit, OnDestroy } from "@angular/core";
import { CourseService } from "../../services/course.service";
import { AuthService } from "../../services/auth.service";
import { ICourseContentsModel } from "../../viewmodels/course.model";
import { ITopicModel } from "../../viewmodels/topic.model";
import { NavigationEnd, Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { UserProfile } from "../../viewmodels/user.model";
import { of, Subscription } from "rxjs";
import { IAssignmentModel } from "src/app/viewmodels/assignment.model";
import { filter, map } from "rxjs/operators";
import { GetResult } from "src/app/util/result";
import { BsModalRef, BsModalService, ModalOptions } from "ngx-bootstrap/modal";
import { ProjectAddComponent } from "../project-add/project-add.component";
import { IProjectDetailsModel } from "src/app/viewmodels/project.model";
import { PeriodProvider } from "src/app/services";

@Component({
  templateUrl: "./course.component.html",
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
  public activePeriod: boolean = true;

  private userProfileSubscription: Subscription;
  private navigationSubscription: Subscription;

  public modalRef: BsModalRef;

  constructor(
    private router: Router,
    private courseService: CourseService,
    private periodProvider: PeriodProvider,
    private authService: AuthService,
    private toastr: ToastrService,
    private modalService: BsModalService
  ) {
    this.course = {
      id: 0,
      code: "",
      name: "",
      chapters: [],
      projects: [],
    };
    this.selectedChapter = null;
    this.selectedExercise = null;
    this.selectedProject = null;
    this.selectedComponent = null;
    this.hasContent = true;
  }

  ngOnInit() {
    this.navigationSubscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.handleNavigationEvent();
      });

    this.periodProvider.period$.subscribe((period) => {
      if(period) {
        this.activePeriod = period.isActive;
        this.handleNavigationEvent(true);
      }
    });

    // Manually trigger the event handler on initial load
    //this.handleNavigationEvent();

    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService
      .getUserProfile()
      .subscribe((profile) => {
        this.userProfile = profile;
      });
  }

  ngOnDestroy() {
    this.navigationSubscription?.unsubscribe();
    this.userProfileSubscription?.unsubscribe();
  }
  public isChapterTestResultOverviewRouteActive() {
    return (
      this.isChapterRouteActive() && this.router.url.endsWith("/testresults")
    );
  }

  public isChapterTestResultDetailRouteActive() {
    return (
      this.isChapterRouteActive() &&
      this.router.url.includes("/testresults?assignmentId=")
    );
  }

  public navigateToChapter(chapter: ITopicModel) {
    this.router.navigate([
      "courses",
      this.course.id,
      "chapters",
      chapter.code,
      "testresults",
    ]);
  }

  public navigateToExercise(exercise: IAssignmentModel) {
    this.router.navigate(
      [
        "courses",
        this.course.id,
        "chapters",
        this.selectedChapter.code,
        "testresults",
      ],
      {
        queryParams: { assignmentId: exercise.assignmentId },
      }
    );
  }

  private handleNavigationEvent(forceReload: boolean = false) {
    const url = this.router.url;
    //console.log("Navigated to: " + url);

    const courseIdMatch = url.match(/courses\/(?<courseId>\d+)/);
    const courseId = courseIdMatch ? +courseIdMatch.groups.courseId : 0;

    const chapterCodeMatch = url.match(/chapters\/(?<chapterCode>[^\/]+)/);
    const chapterCode = chapterCodeMatch
      ? chapterCodeMatch.groups.chapterCode
      : "";

    const projectCodeMatch = url.match(/projects\/(?<projectCode>[^\/]+)/);
    const projectCode = projectCodeMatch
      ? projectCodeMatch.groups.projectCode
      : "";

    const topicCode = chapterCode || projectCode;

    const assignmentIdMatch = url.match(/assignmentId=(?<assignmentId>\d+)/);
    const assignmentId = assignmentIdMatch
      ? +assignmentIdMatch.groups.assignmentId
      : 0;

    this.loadCourseContents(courseId, topicCode, assignmentId, forceReload);
  }

  private isChapterRouteActive() {
    return this.selectChapter != null;
  }

  private selectChapter(chapter: ITopicModel) {
    this.selectedChapter = chapter;
    this.selectedExercise = null;
    this.selectedProject = null;
    this.selectedComponent = null;
  }

  public navigateToProject(
    project: ITopicModel,
    showTestResults: boolean = false
  ) {
    this.router.navigate([
      "courses",
      this.course.id,
      "projects",
      project.code,
      showTestResults ? "testresults" : "assessments",
    ]);
  }

  public get projectTestResultsOverviewActive(): boolean {
    if (!this.selectedProject) return false;
    if (this.selectedComponent) return false;
    return this.router.url.indexOf("testresults") > 0;
  }

  public get projectTestResultsDetailActive(): boolean {
    if (!this.selectedProject) return false;
    if (!this.selectedComponent) return false;
    return this.router.url.indexOf("testresults") > 0;
  }

  public navigateToComponent(component: IAssignmentModel) {
    this.router.navigate(
      [
        "courses",
        this.course.id,
        "projects",
        this.selectedProject.code,
        "testresults",
      ],
      {
        queryParams: { assignmentId: component.assignmentId },
      }
    );
  }

  private selectProject(project: ITopicModel) {
    this.selectedProject = project;
    this.selectedChapter = null;
    this.selectedExercise = null;
    this.selectedComponent = null;
  }

  private loadCourseContents(
    courseId: number,
    selectedTopicCode: string = null,
    selectedAssignmentId: number = 0,
    forceReload: boolean = false
  ) {
    this.loading = true;
    this.hasContent = true;
    this.selectedChapter = null;
    this.selectedProject = null;

    let courseContents$ = of(this.course).pipe(
      map((c) => GetResult.success<ICourseContentsModel>(c))
    );
    if (this.course.id != courseId || forceReload) {
      courseContents$ = this.courseService.getCourseContentsById(courseId, this.periodProvider.period.id);
    }

    courseContents$.subscribe((result) => {
      this.loading = false;
      if (result.success) {
        this.course = result.value;
        if (this.course.chapters.length > 0) {
          //Set selected chapter
          let selectedChapter = this.course.chapters.find(
            (c) => c.code == selectedTopicCode
          );
          if (selectedChapter) {
            this.selectChapter(selectedChapter);
          } else {
            this.navigateToChapter(this.course.chapters[0]);
            return;
          }

          //Set selected assignment
          if (selectedAssignmentId > 0) {
            let selectedAssignment = selectedChapter.assignments.find(
              (a) => a.assignmentId == selectedAssignmentId
            );
            if (selectedAssignment) {
              this.selectedExercise = selectedAssignment;
              this.selectedComponent = null;
            }
          }
        } else if (this.course.projects.length > 0) {
          //Set selected project
          let selectedProject = this.course.projects.find(
            (p) => p.code == selectedTopicCode
          );
          if (selectedProject) {
            this.selectProject(selectedProject);
          } else {
            this.navigateToProject(this.course.projects[0]);
          }

          //Set selected assignment
          if (selectedAssignmentId > 0) {
            let selectedAssignment = selectedProject.assignments.find(
              (a) => a.assignmentId == selectedAssignmentId
            );
            if (selectedAssignment) {
              this.selectedComponent = selectedAssignment;
              this.selectedExercise = null;
            }
          }
        } else {
          this.hasContent = false;
          this.router.navigate([
            "courses",
            this.course.id
          ]);
        }
      } else {
        this.toastr.error(
          "Could not course details from API. Message: " +
            (result.message || "unknown error"),
          "System error"
        );
      }
    });
  }

  public showAddProjectModal() {
    let modalState: ModalOptions = {
      initialState: {
        courseId: this.course.id,
      },
    };
    this.modalRef = this.modalService.show(ProjectAddComponent, modalState);
    this.modalRef.setClass("modal-lg");
    this.modalRef.content.projectAdded.subscribe(
      (addedProject: IProjectDetailsModel) => {
        this.navigateToProject(addedProject);
        this.course.id = 0; //force reload of course contents
      }
    );
  }
}
