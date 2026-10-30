import {
  Component,
  OnInit,
  OnDestroy,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  signal,
} from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { Subscription } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { GamificationService } from "../../services/gamification.service";
import { AuthService } from "../../services/auth.service";
import { PeriodProvider } from "../../services/period.provider";
import {
  IStudentGamificationProfileModel,
  ICourseClassGroupModel,
  ICourseGamificationConfigModel,
  IEnrollStudentModel,
  IHallOfFameStudentOptionModel,
} from "../../viewmodels/gamification.model";
import { UserProfile } from "../../viewmodels/user.model";
import { CourseGamificationHeroComponent } from "./course-gamification-hero.component";

@Component({
  selector: "app-course-gamification",
  templateUrl: "./course-gamification.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationComponent implements OnInit, OnDestroy {
  @ViewChild(CourseGamificationHeroComponent)
  public heroComponent?: CourseGamificationHeroComponent;

  private readonly _courseId = signal<number>(0);
  public get courseId(): number {
    return this._courseId();
  }
  public set courseId(v: number) {
    this._courseId.set(v);
  }

  private readonly _periodId = signal<number | undefined>(undefined);
  public get periodId(): number | undefined {
    return this._periodId();
  }
  public set periodId(v: number | undefined) {
    this._periodId.set(v);
  }

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private readonly _activeTab = signal<string>("sprint");
  public get activeTab(): string {
    return this._activeTab();
  }
  public set activeTab(v: string) {
    this._activeTab.set(v);
  }

  private readonly _profile = signal<IStudentGamificationProfileModel | null>(null);
  public get profile(): IStudentGamificationProfileModel | null {
    return this._profile();
  }
  public set profile(v: IStudentGamificationProfileModel | null) {
    this._profile.set(v);
  }

  private readonly _config = signal<ICourseGamificationConfigModel | null>(null);
  public get config(): ICourseGamificationConfigModel | null {
    return this._config();
  }
  public set config(v: ICourseGamificationConfigModel | null) {
    this._config.set(v);
  }

  private readonly _classGroups = signal<ICourseClassGroupModel[]>([]);
  public get classGroups(): ICourseClassGroupModel[] {
    return this._classGroups();
  }
  public set classGroups(v: ICourseClassGroupModel[]) {
    this._classGroups.set(v);
  }

  private readonly _selectedClassGroupId = signal<number | null>(null);
  public get selectedClassGroupId(): number | null {
    return this._selectedClassGroupId();
  }
  public set selectedClassGroupId(v: number | null) {
    this._selectedClassGroupId.set(v);
  }

  private readonly _needsClassSelection = signal<boolean>(false);
  public get needsClassSelection(): boolean {
    return this._needsClassSelection();
  }
  public set needsClassSelection(v: boolean) {
    this._needsClassSelection.set(v);
  }

  private readonly _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private readonly _perspectiveUserId = signal<number | null>(null);
  public get perspectiveUserId(): number | null {
    return this._perspectiveUserId();
  }
  public set perspectiveUserId(v: number | null) {
    this._perspectiveUserId.set(v);
  }

  private readonly _studentOptions = signal<IHallOfFameStudentOptionModel[]>([]);
  public get studentOptions(): IHallOfFameStudentOptionModel[] {
    return this._studentOptions();
  }
  public set studentOptions(v: IHallOfFameStudentOptionModel[]) {
    this._studentOptions.set(v);
  }

  private userLoaded: boolean = false;
  private userSub?: Subscription;
  private periodSub?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly gamificationService: GamificationService,
    private readonly authService: AuthService,
    private readonly periodProvider: PeriodProvider,
    private readonly toastr: ToastrService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  public ngOnInit(): void {
    let initialLoadDone = false;

    const parentId = this.route.parent?.snapshot.params["courseId"];
    const ownId = this.route.snapshot.params["courseId"];
    this.courseId = +(parentId ?? ownId ?? 0);
    this.periodId = this.periodProvider.period?.id;

    this.userSub = this.authService.getUserProfile().subscribe((p) => {
      const wasTeacher = this.userProfile.isTeacher();
      this.userProfile = p;
      this.userLoaded = true;
      if (this.userProfile.isTeacher()) {
        if (!wasTeacher) {
          this.profile = null;
          this.loadTeacherStudentOptions();
        }
      }
      this.checkClassSelectionRequirement();
      this.adjustActiveTabForConfig();
      if (!initialLoadDone && this.courseId > 0) {
        initialLoadDone = true;
        this.loadData();
      }
      this.cdr.markForCheck();
    });

    this.periodSub = this.periodProvider.period$.subscribe((period) => {
      if (period) {
        const periodChanged = this.periodId !== period.id;
        this.periodId = period.id;
        if (this.courseId > 0 && this.userLoaded && (periodChanged || !initialLoadDone)) {
          initialLoadDone = true;
          this.loadData();
        }
      }
    });

    if (this.courseId <= 0) {
      this.route.params.subscribe((params) => {
        this.courseId = +params["courseId"] || 0;
        if (this.courseId > 0 && this.userLoaded && !initialLoadDone) {
          initialLoadDone = true;
          this.loadData();
        }
      });
    }
  }

  public ngOnDestroy(): void {
    this.userSub?.unsubscribe();
    this.periodSub?.unsubscribe();
  }

  public loadData(): void {
    if (this.courseId <= 0) return;
    this.loadConfig();
    this.loadClasses();
    if (this.userProfile.isTeacher()) {
      this.loadTeacherStudentOptions();
    } else {
      this.loadProfile();
    }
  }

  public loadTeacherStudentOptions(): void {
    if (this.courseId <= 0 || !this.userProfile.isTeacher()) return;
    this.gamificationService
      .getHallOfFame(this.courseId, this.periodId)
      .subscribe((res) => {
        if (res.success && res.value?.studentOptions) {
          this.studentOptions = res.value.studentOptions;
          this.cdr.markForCheck();
        }
      });
  }

  public onStudentOptionsLoaded(options: IHallOfFameStudentOptionModel[]): void {
    if (options && options.length > 0) {
      this.studentOptions = options;
      this.cdr.markForCheck();
    }
  }

  public loadConfig(): void {
    if (this.courseId <= 0) return;
    this.gamificationService.getConfig(this.courseId).subscribe((res) => {
      if (res.success && res.value) {
        this.config = res.value;
        this.adjustActiveTabForConfig();
        this.cdr.markForCheck();
      }
    });
  }

  public loadClasses(): void {
    if (this.courseId <= 0) return;
    this.gamificationService
      .getClasses(this.courseId, this.periodId)
      .subscribe((res) => {
        if (res.success && res.value) {
          this.classGroups = res.value;
          this.checkClassSelectionRequirement();
          this.adjustActiveTabForConfig();
          this.cdr.markForCheck();
        }
      });
  }

  public onPerspectiveChanged(userId: number | string | null): void {
    const parsed =
      userId !== null && userId !== undefined && userId !== "" && userId !== "null"
        ? Number(userId)
        : null;
    this.perspectiveUserId = parsed !== null && !isNaN(parsed) ? parsed : null;
    if (this.userProfile.isTeacher()) {
      this.profile = null;
    }
    this.loadProfile();
    this.cdr.markForCheck();
  }

  public loadProfile(): void {
    if (this.courseId <= 0 || !this.userLoaded) return;
    if (this.userProfile.isTeacher()) {
      if (this.perspectiveUserId) {
        this.gamificationService
          .getProfile(this.courseId, this.periodId, this.perspectiveUserId)
          .subscribe((res) => {
            if (res.success && res.value) {
              this.profile = res.value;
              this.cdr.markForCheck();
            }
          });
      } else {
        this.profile = null;
        this.cdr.markForCheck();
      }
      return;
    }
    this.gamificationService
      .getProfile(this.courseId, this.periodId)
      .subscribe((res) => {
        if (res.success && res.value) {
          this.profile = res.value;
          this.checkClassSelectionRequirement();
          this.cdr.markForCheck();
        }
      });
  }

  public onProfileLoaded(profile: IStudentGamificationProfileModel): void {
    if (this.userProfile.isTeacher()) return;
    this.profile = profile;
    this.checkClassSelectionRequirement();
    this.cdr.markForCheck();
  }

  public onProfileUpdated(profile: IStudentGamificationProfileModel): void {
    if (!profile.classGroupName && profile.classGroupId) {
      const group = this.classGroups.find((cg) => cg.id === profile.classGroupId);
      if (group) {
        profile.classGroupName = group.name;
      }
    }
    this.profile = profile;
    this.checkClassSelectionRequirement();
    this.cdr.markForCheck();
  }

  public onClassesChanged(): void {
    this.loadClasses();
  }

  public onConfigChanged(newConfig?: ICourseGamificationConfigModel): void {
    if (newConfig) {
      this.config = newConfig;
      this.adjustActiveTabForConfig();
      this.cdr.markForCheck();
    } else {
      this.loadConfig();
    }
  }

  public checkClassSelectionRequirement(): void {
    const isStudent = !this.userProfile.isTeacher();
    const hasClasses = this.classGroups.length > 0;
    const notInClass = !this.profile?.classGroupId;

    this.needsClassSelection = isStudent && hasClasses && notInClass;
  }

  public confirmClassSelection(): void {
    if (!this.selectedClassGroupId || !this.courseId) return;

    const enrollDto: IEnrollStudentModel = {
      showInLeaderBoard: this.profile?.showInLeaderBoard ?? true,
      usePseudonym: this.profile?.usePseudonym ?? true,
      pseudonym: this.profile?.pseudonym ?? "",
      avatar: this.profile?.avatar ?? "CodeCat",
      classGroupId: this.selectedClassGroupId,
    };

    this.gamificationService
      .enroll(this.courseId, enrollDto, this.periodId)
      .subscribe((res) => {
        if (res.success && res.value) {
          this.toastr.success(
            "You have joined your class! Welcome to the arena.",
            "Class Selected",
          );
          this.profile = res.value;
          this.needsClassSelection = false;
          if (this.heroComponent) {
            this.heroComponent.loadProfile();
          }
          this.cdr.markForCheck();
        } else {
          this.toastr.error(
            res.message || "Failed to save class group.",
            "Error",
          );
        }
      });
  }

  public adjustActiveTabForConfig(): void {
    if (!this.config) return;

    if (!this.config.isGamificationEnabled) {
      if (this.userProfile.isTeacher()) {
        this.activeTab = "admin";
      }
      return;
    }

    const currentTabValid = this.isTabEnabled(this.activeTab);
    if (!currentTabValid) {
      this.activeTab = this.getFirstEnabledTab();
    }
  }

  private isTabEnabled(tab: string): boolean {
    if (!this.config) return false;
    switch (tab) {
      case "sprint":
        return this.config.isWeeklyStreakEnabled;
      case "hallOfFame":
        return this.config.isLeaderboardEnabled;
      case "classes":
        return this.config.isClassCompetitionEnabled && this.hasClasses;
      case "badges":
        return this.config.isBadgesEnabled;
      case "admin":
        return this.userProfile.isTeacher();
      default:
        return false;
    }
  }

  private getFirstEnabledTab(): string {
    if (!this.config) return "sprint";
    if (this.config.isWeeklyStreakEnabled) return "sprint";
    if (this.config.isLeaderboardEnabled) return "hallOfFame";
    if (this.config.isClassCompetitionEnabled && this.hasClasses) return "classes";
    if (this.config.isBadgesEnabled) return "badges";
    if (this.userProfile.isTeacher()) return "admin";
    return "sprint";
  }

  public get hasClasses(): boolean {
    return this.classGroups && this.classGroups.length > 0;
  }
}
