import {
  Component,
  Input,
  OnInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  signal,
} from "@angular/core";
import { GamificationService } from "../../services/gamification.service";
import {
  ILeaderboardEntryModel,
  AVAILABLE_AVATARS,
  IAvatarModel,
} from "../../viewmodels/gamification.model";

@Component({
  selector: "app-course-gamification-sprint",
  templateUrl: "./course-gamification-sprint.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationSprintComponent implements OnInit, OnChanges {
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

  private readonly _perspectiveUserId = signal<number | null | undefined>(undefined);
  @Input()
  public get perspectiveUserId(): number | null | undefined {
    return this._perspectiveUserId();
  }
  public set perspectiveUserId(v: number | null | undefined) {
    this._perspectiveUserId.set(v);
  }

  private readonly _weeklySprint = signal<ILeaderboardEntryModel[]>([]);
  public get weeklySprint(): ILeaderboardEntryModel[] {
    return this._weeklySprint();
  }
  public set weeklySprint(v: ILeaderboardEntryModel[]) {
    this._weeklySprint.set(v);
  }

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public get isCurrentUserOnBoard(): boolean {
    return this.weeklySprint.some((e) => e.isCurrentUser);
  }

  public get shouldShowGetOnBoardTrigger(): boolean {
    return (
      !this.loading &&
      (!this.isTeacher || !!this.perspectiveUserId) &&
      !this.isCurrentUserOnBoard
    );
  }

  public availableAvatars: IAvatarModel[] = AVAILABLE_AVATARS;

  constructor(private readonly gamificationService: GamificationService) {}

  public ngOnInit(): void {
    this.loadSprint();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if (
      (changes["courseId"] && !changes["courseId"].firstChange) ||
      (changes["periodId"] && !changes["periodId"].firstChange) ||
      (changes["perspectiveUserId"] && !changes["perspectiveUserId"].firstChange)
    ) {
      this.loadSprint();
    }
  }

  public loadSprint(): void {
    if (this.courseId <= 0) return;
    this.loading = true;
    this.gamificationService
      .getWeeklySprint(this.courseId, this.periodId, this.perspectiveUserId)
      .subscribe((res) => {
        this.loading = false;
        if (res.success && res.value) {
          this.weeklySprint = res.value.filter((e) => e.score > 0);
        }
      });
  }

  public getAvatarIcon(avatarId?: string): string {
    const found = this.availableAvatars.find((a) => a.id === avatarId);
    return found ? found.icon : "🐱";
  }
}
