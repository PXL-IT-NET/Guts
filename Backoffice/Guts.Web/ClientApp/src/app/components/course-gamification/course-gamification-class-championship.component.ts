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
import { IClassGroupLeaderboardEntryModel } from "../../viewmodels/gamification.model";

@Component({
  selector: "app-course-gamification-class-championship",
  templateUrl: "./course-gamification-class-championship.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationClassChampionshipComponent implements OnInit, OnChanges {
  @Input() public courseId: number = 0;
  @Input() public periodId?: number;

  private _classLeaderboard = signal<IClassGroupLeaderboardEntryModel[]>([]);
  public get classLeaderboard(): IClassGroupLeaderboardEntryModel[] {
    return this._classLeaderboard();
  }
  public set classLeaderboard(v: IClassGroupLeaderboardEntryModel[]) {
    this._classLeaderboard.set(v);
  }

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  constructor(private gamificationService: GamificationService) {}

  public ngOnInit(): void {
    this.loadClassLeaderboard();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if ((changes["courseId"] && !changes["courseId"].firstChange) ||
        (changes["periodId"] && !changes["periodId"].firstChange)) {
      this.loadClassLeaderboard();
    }
  }

  public loadClassLeaderboard(): void {
    if (this.courseId <= 0) return;
    this.loading = true;
    this.gamificationService.getClassLeaderboard(this.courseId, this.periodId).subscribe((res) => {
      this.loading = false;
      if (res.success && res.value) {
        this.classLeaderboard = res.value;
      }
    });
  }
}

