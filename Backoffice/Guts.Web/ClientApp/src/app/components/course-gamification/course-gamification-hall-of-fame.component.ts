import {
  Component,
  EventEmitter,
  Input,
  Output,
  OnInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  signal,
} from "@angular/core";
import { GamificationService } from "../../services/gamification.service";
import {
  ILeaderboardEntryModel,
  IHallOfFameTierModel,
  IHallOfFameTargetStudentModel,
  IHallOfFameStudentOptionModel,
  AVAILABLE_AVATARS,
  IAvatarModel,
} from "../../viewmodels/gamification.model";

@Component({
  selector: "app-course-gamification-hall-of-fame",
  templateUrl: "./course-gamification-hall-of-fame.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationHallOfFameComponent implements OnInit, OnChanges {
  @Input() public courseId: number = 0;
  @Input() public periodId?: number;

  private readonly _isTeacher = signal<boolean>(false);
  @Input()
  public get isTeacher(): boolean {
    return this._isTeacher();
  }
  public set isTeacher(v: boolean) {
    this._isTeacher.set(v);
  }

  private readonly _perspectiveUserId = signal<number | null | undefined>(null);
  @Input()
  public get perspectiveUserId(): number | null | undefined {
    return this._perspectiveUserId();
  }
  public set perspectiveUserId(v: number | null | undefined) {
    this._perspectiveUserId.set(v);
  }

  @Output() public readonly studentOptionsLoaded =
    new EventEmitter<IHallOfFameStudentOptionModel[]>();

  private readonly _hallOfFame = signal<ILeaderboardEntryModel[]>([]);
  public get hallOfFame(): ILeaderboardEntryModel[] {
    return this._hallOfFame();
  }
  public set hallOfFame(v: ILeaderboardEntryModel[]) {
    this._hallOfFame.set(v);
  }

  private readonly _currentTier = signal<IHallOfFameTierModel | null>(null);
  public get currentTier(): IHallOfFameTierModel | null {
    return this._currentTier();
  }
  public set currentTier(v: IHallOfFameTierModel | null) {
    this._currentTier.set(v);
  }

  private readonly _visibleTiers = signal<IHallOfFameTierModel[]>([]);
  public get visibleTiers(): IHallOfFameTierModel[] {
    return this._visibleTiers();
  }
  public set visibleTiers(v: IHallOfFameTierModel[]) {
    this._visibleTiers.set(v);
  }

  private readonly _allTiers = signal<IHallOfFameTierModel[]>([]);
  public get allTiers(): IHallOfFameTierModel[] {
    return this._allTiers();
  }
  public set allTiers(v: IHallOfFameTierModel[]) {
    this._allTiers.set(v);
  }

  private readonly _nextTargetStudent =
    signal<IHallOfFameTargetStudentModel | null>(null);
  public get nextTargetStudent(): IHallOfFameTargetStudentModel | null {
    return this._nextTargetStudent();
  }
  public set nextTargetStudent(v: IHallOfFameTargetStudentModel | null) {
    this._nextTargetStudent.set(v);
  }

  private readonly _currentUserRank = signal<number | null>(null);
  public get currentUserRank(): number | null {
    return this._currentUserRank();
  }
  public set currentUserRank(v: number | null) {
    this._currentUserRank.set(v);
  }

  private readonly _selectedTierFilter = signal<number | null>(null);
  public get selectedTierFilter(): number | null {
    return this._selectedTierFilter();
  }
  public set selectedTierFilter(v: number | null) {
    this._selectedTierFilter.set(v);
  }

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public availableAvatars: IAvatarModel[] = AVAILABLE_AVATARS;

  constructor(private readonly gamificationService: GamificationService) {}

  public ngOnInit(): void {
    this.loadHallOfFame();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if (
      (changes["courseId"] && !changes["courseId"].firstChange) ||
      (changes["periodId"] && !changes["periodId"].firstChange) ||
      (changes["perspectiveUserId"] &&
        !changes["perspectiveUserId"].firstChange)
    ) {
      if (changes["perspectiveUserId"] && this.perspectiveUserId) {
        this.selectedTierFilter = null;
      }
      this.loadHallOfFame();
    }
  }

  public selectTierFilter(tier: number | null): void {
    this.selectedTierFilter = tier;
    this.loadHallOfFame();
  }

  public loadHallOfFame(): void {
    if (this.courseId <= 0) return;
    this.loading = true;
    const effectiveTier =
      this.isTeacher && !this.perspectiveUserId ? this.selectedTierFilter : null;

    this.gamificationService
      .getHallOfFame(
        this.courseId,
        this.periodId,
        this.perspectiveUserId,
        effectiveTier,
      )
      .subscribe((res) => {
        this.loading = false;
        if (res.success && res.value) {
          this.hallOfFame = res.value.entries || [];
          this.currentTier = res.value.currentTier || null;
          this.visibleTiers = res.value.visibleTiers || [];
          this.allTiers = res.value.allTiers || [];
          this.nextTargetStudent = res.value.nextTargetStudent || null;
          this.currentUserRank = res.value.currentUserRank ?? null;
          if (res.value.studentOptions) {
            this.studentOptionsLoaded.emit(res.value.studentOptions);
          }
        }
      });
  }

  public getAvatarIcon(avatarId?: string): string {
    const found = this.availableAvatars.find((a) => a.id === avatarId);
    return found ? found.icon : "🐱";
  }
}
