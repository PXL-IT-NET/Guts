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

  private _weeklySprint = signal<ILeaderboardEntryModel[]>([]);
  public get weeklySprint(): ILeaderboardEntryModel[] {
    return this._weeklySprint();
  }
  public set weeklySprint(v: ILeaderboardEntryModel[]) {
    this._weeklySprint.set(v);
  }

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public availableAvatars: IAvatarModel[] = AVAILABLE_AVATARS;

  constructor(private gamificationService: GamificationService) {}

  public ngOnInit(): void {
    this.loadSprint();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if ((changes["courseId"] && !changes["courseId"].firstChange) ||
        (changes["periodId"] && !changes["periodId"].firstChange)) {
      this.loadSprint();
    }
  }

  public loadSprint(): void {
    if (this.courseId <= 0) return;
    this.loading = true;
    this.gamificationService.getWeeklySprint(this.courseId, this.periodId).subscribe((res) => {
      this.loading = false;
      if (res.success && res.value) {
        this.weeklySprint = res.value;
      }
    });
  }

  public getAvatarIcon(avatarId?: string): string {
    const found = this.availableAvatars.find((a) => a.id === avatarId);
    return found ? found.icon : "🐱";
  }
}

