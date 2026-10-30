import {
  Component,
  Input,
  OnInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  signal,
} from "@angular/core";
import { GamificationService } from "../../services/gamification.service";
import {
  IStudentGamificationProfileModel,
  IBadgeMetadataModel,
} from "../../viewmodels/gamification.model";

@Component({
  selector: "app-course-gamification-badges",
  templateUrl: "./course-gamification-badges.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationBadgesComponent implements OnInit {
  private readonly _profile = signal<IStudentGamificationProfileModel | null>(null);
  @Input()
  public get profile(): IStudentGamificationProfileModel | null {
    return this._profile();
  }
  public set profile(v: IStudentGamificationProfileModel | null) {
    this._profile.set(v);
    this.cdr.markForCheck();
  }

  private readonly _allBadges = signal<IBadgeMetadataModel[]>([]);
  public get allBadges(): IBadgeMetadataModel[] {
    return this._allBadges();
  }
  public set allBadges(v: IBadgeMetadataModel[]) {
    this._allBadges.set(v);
    this.cdr.markForCheck();
  }

  constructor(
    private readonly gamificationService: GamificationService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  public ngOnInit(): void {
    this.gamificationService.getBadgeMetadata().subscribe((result) => {
      if (result.success && result.value) {
        this.allBadges = result.value;
        this.cdr.markForCheck();
      }
    });
  }

  public isBadgeEarned(badgeType: number): boolean {
    return !!this.profile?.earnedBadges?.some((b) => b.badgeType === badgeType);
  }

  public getBadgeEarnedDate(badgeType: number): string | null {
    const found = this.profile?.earnedBadges?.find((b) => b.badgeType === badgeType);
    return found ? found.earnedAt : null;
  }
}
