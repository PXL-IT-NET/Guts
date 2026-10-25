import {
  Component,
  OnInit,
  OnDestroy,
  ViewChild,
  ChangeDetectionStrategy,
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

  private _courseId = signal<number>(0);
  public get courseId(): number {
    return this._courseId();
  }
  public set courseId(v: number) {
    this._courseId.set(v);
  }

  private _periodId = signal<number | undefined>(undefined);
  public get periodId(): number | undefined {
    return this._periodId();
  }
  public set periodId(v: number | undefined) {
    this._periodId.set(v);
  }

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private _activeTab = signal<string>("sprint");
  public get activeTab(): string {
    return this._activeTab();
  }
  public set activeTab(v: string) {
    this._activeTab.set(v);
  }

  private _profile = signal<IStudentGamificationProfileModel | null>(null);
  public get profile(): IStudentGamificationProfileModel | null {
    return this._profile();
  }
  public set profile(v: IStudentGamificationProfileModel | null) {
    this._profile.set(v);
  }

  private _config = signal<ICourseGamificationConfigModel | null>(null);
  public get config(): ICourseGamificationConfigModel | null {
    return this._config();
  }
  public set config(v: ICourseGamificationConfigModel | null) {
    this._config.set(v);
  }

  private _classGroups = signal<ICourseClassGroupModel[]>([]);
  public get classGroups(): ICourseClassGroupModel[] {
    return this._classGroups();
  }
  public set classGroups(v: ICourseClassGroupModel[]) {
    this._classGroups.set(v);
  }

  private _selectedClassGroupId = signal<number | null>(null);
  public get selectedClassGroupId(): number | null {
    return this._selectedClassGroupId();
  }
  public set selectedClassGroupId(v: number | null) {
    this._selectedClassGroupId.set(v);
  }

  private _needsClassSelection = signal<boolean>(false);
  public get needsClassSelection(): boolean {
    return this._needsClassSelection();
  }
  public set needsClassSelection(v: boolean) {
    this._needsClassSelection.set(v);
  }

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private userSub?: Subscription;
  private periodSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private gamificationService: GamificationService,
    private authService: AuthService,
    private periodProvider: PeriodProvider,
    private toastr: ToastrService,
  ) {}

  public ngOnInit(): void {
    this.userSub = this.authService.getUserProfile().subscribe((p) => {
      this.userProfile = p;
      this.checkClassSelectionRequirement();
      this.adjustActiveTabForConfig();
    });

    const parentId = this.route.parent?.snapshot.params["courseId"];
    const ownId = this.route.snapshot.params["courseId"];
    this.courseId = +(parentId ?? ownId ?? 0);

    let initialLoadDone = false;
    this.periodSub = this.periodProvider.period$.subscribe((period) => {
      if (period) {
        const periodChanged = this.periodId !== period.id;
        this.periodId = period.id;
        if (this.courseId > 0 && (periodChanged || !initialLoadDone)) {
          initialLoadDone = true;
          this.loadData();
        }
      }
    });

    if (!initialLoadDone && this.courseId > 0) {
      initialLoadDone = true;
      this.loadData();
    } else if (this.courseId <= 0) {
      this.route.params.subscribe((params) => {
        this.courseId = +params["courseId"] || 0;
        if (this.courseId > 0 && !initialLoadDone) {
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
    this.loadProfile();
  }

  public loadConfig(): void {
    if (this.courseId <= 0) return;
    this.gamificationService.getConfig(this.courseId).subscribe((res) => {
      if (res.success && res.value) {
        this.config = res.value;
        this.adjustActiveTabForConfig();
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
        }
      });
  }

  public loadProfile(): void {
    if (this.courseId <= 0) return;
    this.gamificationService
      .getProfile(this.courseId, this.periodId)
      .subscribe((res) => {
        if (res.success && res.value) {
          this.profile = res.value;
          this.checkClassSelectionRequirement();
        }
      });
  }

  public onProfileLoaded(profile: IStudentGamificationProfileModel): void {
    this.profile = profile;
    this.checkClassSelectionRequirement();
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
  }

  public onClassesChanged(): void {
    this.loadClasses();
  }

  public onConfigChanged(newConfig?: ICourseGamificationConfigModel): void {
    if (newConfig) {
      this.config = newConfig;
      this.adjustActiveTabForConfig();
    } else {
      this.loadConfig();
    }
  }

  public checkClassSelectionRequirement(): void {
    const isStudent = !this.userProfile.isTeacher();
    const hasClasses = this.classGroups.length > 0;
    const notInClass = !this.profile || !this.profile.classGroupId;

    this.needsClassSelection = isStudent && hasClasses && notInClass;
  }

  public adjustActiveTabForConfig(): void {
    if (!this.config) return;

    if (!this.config.isGamificationEnabled) {
      if (this.userProfile.isTeacher()) {
        this.activeTab = "admin";
      }
      return;
    }

    if (this.activeTab === "sprint" && !this.config.isWeeklyStreakEnabled) {
      this.activeTab = this.getFirstEnabledTab();
    } else if (
      this.activeTab === "hallOfFame" &&
      !this.config.isLeaderboardEnabled
    ) {
      this.activeTab = this.getFirstEnabledTab();
    } else if (
      this.activeTab === "classes" &&
      (!this.config.isClassCompetitionEnabled || !this.hasClasses)
    ) {
      this.activeTab = this.getFirstEnabledTab();
    } else if (this.activeTab === "badges" && !this.config.isBadgesEnabled) {
      this.activeTab = this.getFirstEnabledTab();
    }
  }

  public getFirstEnabledTab(): string {
    if (this.config?.isWeeklyStreakEnabled) return "sprint";
    if (this.config?.isLeaderboardEnabled) return "hallOfFame";
    if (this.config?.isClassCompetitionEnabled && this.hasClasses)
      return "classes";
    if (this.config?.isBadgesEnabled) return "badges";
    if (this.userProfile.isTeacher()) return "admin";
    return "sprint";
  }

  public confirmClassSelection(): void {
    if (!this.selectedClassGroupId || this.courseId <= 0) return;

    const enrollDto: IEnrollStudentModel = {
      showInLeaderBoard: this.profile ? this.profile.showInLeaderBoard : true,
      usePseudonym: this.profile ? this.profile.usePseudonym : true,
      pseudonym: this.profile
        ? this.profile.pseudonym
        : `Student_${this.userProfile.id || ""}`,
      avatar: this.profile ? this.profile.avatar : "CodeCat",
      classGroupId: this.selectedClassGroupId,
    };

    this.gamificationService
      .enroll(this.courseId, enrollDto, this.periodId)
      .subscribe((res) => {
        if (res.success && res.value) {
          this.profile = res.value;
          this.needsClassSelection = false;
          this.toastr.success(
            "Class selected! Welcome to the leaderboard.",
            "Success",
          );
          this.heroComponent?.loadProfile?.();
        } else {
          this.toastr.error(res.message || "Failed to assign class.", "Error");
        }
      });
  }

  public get hasClasses(): boolean {
    return this.classGroups.length > 0;
  }
}
