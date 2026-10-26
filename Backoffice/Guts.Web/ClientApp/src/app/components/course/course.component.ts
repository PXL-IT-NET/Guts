import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, signal } from "@angular/core";
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
import { GamificationService } from "../../services/gamification.service";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./course.component.html",
})
export class CourseComponent implements OnInit, OnDestroy {
  private _course = signal<ICourseContentsModel>({
    id: 0,
    code: "",
    name: "",
    chapters: [],
    projects: [],
  });
  public get course(): ICourseContentsModel {
    return this._course();
  }
  public set course(v: ICourseContentsModel) {
    this._course.set(v);
  }

  private _selectedChapter = signal<ITopicModel | null>(null);
  public get selectedChapter(): ITopicModel | null {
    return this._selectedChapter();
  }
  public set selectedChapter(v: ITopicModel | null) {
    this._selectedChapter.set(v);
  }

  private _selectedExercise = signal<IAssignmentModel | null>(null);
  public get selectedExercise(): IAssignmentModel | null {
    return this._selectedExercise();
  }
  public set selectedExercise(v: IAssignmentModel | null) {
    this._selectedExercise.set(v);
  }

  private _selectedProject = signal<ITopicModel | null>(null);
  public get selectedProject(): ITopicModel | null {
    return this._selectedProject();
  }
  public set selectedProject(v: ITopicModel | null) {
    this._selectedProject.set(v);
  }

  private _selectedComponent = signal<IAssignmentModel | null>(null);
  public get selectedComponent(): IAssignmentModel | null {
    return this._selectedComponent();
  }
  public set selectedComponent(v: IAssignmentModel | null) {
    this._selectedComponent.set(v);
  }

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private _hasContent = signal<boolean>(true);
  public get hasContent(): boolean {
    return this._hasContent();
  }
  public set hasContent(v: boolean) {
    this._hasContent.set(v);
  }

  private _activePeriod = signal<boolean>(true);
  public get activePeriod(): boolean {
    return this._activePeriod();
  }
  public set activePeriod(v: boolean) {
    this._activePeriod.set(v);
  }

  private _isGamificationEnabled = signal<boolean>(false);
  public get isGamificationEnabled(): boolean {
    return this._isGamificationEnabled();
  }
  public set isGamificationEnabled(v: boolean) {
    this._isGamificationEnabled.set(v);
  }


  private userProfileSubscription: Subscription;
  private navigationSubscription: Subscription;

  public modalRef: BsModalRef;

  constructor(
    private router: Router,
    private courseService: CourseService,
    private periodProvider: PeriodProvider,
    private authService: AuthService,
    private toastr: ToastrService,
    private modalService: BsModalService,
    private gamificationService: GamificationService,
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
      if (period) {
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

  public isGamificationRouteActive(): boolean {
    return this.router.url.includes("/gamification");
  }

  public navigateToGamification() {
    this.selectedChapter = null;
    this.selectedExercise = null;
    this.selectedProject = null;
    this.selectedComponent = null;
    void this.router.navigate(["courses", this.course.id, "gamification"]);
  }

  public isChapterTestResultDetailRouteActive() {
    return (
      this.isChapterRouteActive() &&
      this.router.url.includes("/testresults?assignmentId=")
    );
  }

  public navigateToChapter(chapter: ITopicModel) {
    void this.router.navigate([
      "courses",
      this.course.id,
      "chapters",
      chapter.code,
      "testresults",
    ]);
  }

  public navigateToExercise(exercise: IAssignmentModel) {
    void this.router.navigate(
      [
        "courses",
        this.course.id,
        "chapters",
        this.selectedChapter.code,
        "testresults",
      ],
      {
        queryParams: { assignmentId: exercise.assignmentId },
      },
    );
  }

  private handleNavigationEvent(forceReload: boolean = false) {
    const url = this.router.url;
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
    return this.selectedChapter != null;
  }

  private selectChapter(chapter: ITopicModel) {
    this.selectedChapter = chapter;
    this.selectedExercise = null;
    this.selectedProject = null;
    this.selectedComponent = null;
  }

  public navigateToProject(
    project: ITopicModel,
    showTestResults: boolean = false,
  ) {
    void this.router.navigate([
      "courses",
      this.course.id,
      "projects",
      project.code,
      showTestResults ? "testresults" : "assessments",
    ]);
  }

  public get canShowCourseStatistics(): boolean {
    return this.userProfile.isLector() && (this.course.chapters?.length ?? 0) > 0;
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
    void this.router.navigate(
      [
        "courses",
        this.course.id,
        "projects",
        this.selectedProject.code,
        "testresults",
      ],
      {
        queryParams: { assignmentId: component.assignmentId },
      },
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
    forceReload: boolean = false,
  ) {
    this.loading = true;
    this.hasContent = true;
    this.selectedChapter = null;
    this.selectedProject = null;

    if (courseId <= 0) {
      return;
    }

    if (this.course.id !== courseId || forceReload) {
      this.gamificationService.getConfig(courseId).subscribe((configRes) => {
        if (configRes.success && configRes.value) {
          this.isGamificationEnabled = configRes.value.isGamificationEnabled;
        } else {
          this.isGamificationEnabled = false;
        }
      });
    }

    let courseContents$ = of(this.course).pipe(
      map((c) => GetResult.success<ICourseContentsModel>(c)),
    );
    if (this.course.id !== courseId || forceReload) {
      courseContents$ = this.courseService.getCourseContentsById(
        courseId,
        this.periodProvider.period.id,
      );
    }

    courseContents$.subscribe((result) => {
      this.loading = false;
      if (result.success) {
        this.course = result.value;
        if (this.isGamificationRouteActive()) {
          this.selectedChapter = null;
          this.selectedExercise = null;
          this.selectedProject = null;
          this.selectedComponent = null;
          return;
        }
        if (this.course.chapters.length > 0) {
          if (!this.handleCourseChapters(selectedTopicCode, selectedAssignmentId)) {
            return;
          }
        } else if (this.course.projects.length > 0) {
          this.handleCourseProjects(selectedTopicCode, selectedAssignmentId);
        } else {
          this.hasContent = false;
          void this.router.navigate(["courses", this.course.id]);
        }
      } else {
        this.toastr.error(
          "Could not course details from API. Message: " +
            (result.message || "unknown error"),
          "System error",
        );
      }
    });
  }

  private handleCourseChapters(selectedTopicCode: string, selectedAssignmentId: number): boolean {
    const selectedChapter = this.course.chapters.find((ch) => ch.code === selectedTopicCode);
    if (!selectedChapter) {
      this.navigateToChapter(this.course.chapters[0]);
      return false;
    }
    this.selectChapter(selectedChapter);

    if (selectedAssignmentId > 0) {
      const selectedAssignment = selectedChapter.assignments.find((a) => a.assignmentId === selectedAssignmentId);
      if (selectedAssignment) {
        this.selectedExercise = selectedAssignment;
        this.selectedComponent = null;
      }
    }
    return true;
  }

  private handleCourseProjects(selectedTopicCode: string, selectedAssignmentId: number): void {
    const selectedProject = this.course.projects.find((p) => p.code === selectedTopicCode);
    if (selectedProject) {
      this.selectProject(selectedProject);
    } else {
      this.navigateToProject(this.course.projects[0]);
    }

    if (selectedAssignmentId > 0 && selectedProject) {
      const selectedAssignment = selectedProject.assignments.find((a) => a.assignmentId === selectedAssignmentId);
      if (selectedAssignment) {
        this.selectedComponent = selectedAssignment;
        this.selectedExercise = null;
      }
    }
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
      },
    );
  }
}
