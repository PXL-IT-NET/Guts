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
import { ILeaderboardEntryModel } from "../../viewmodels/gamification.model";

@Component({
  selector: "app-course-gamification-hall-of-fame",
  templateUrl: "./course-gamification-hall-of-fame.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationHallOfFameComponent implements OnInit, OnChanges {
  @Input() public courseId: number = 0;
  @Input() public periodId?: number;

  private _hallOfFame = signal<ILeaderboardEntryModel[]>([]);
  public get hallOfFame(): ILeaderboardEntryModel[] {
    return this._hallOfFame();
  }
  public set hallOfFame(v: ILeaderboardEntryModel[]) {
    this._hallOfFame.set(v);
  }

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public availableAvatars = [
    { id: "CodeCat", label: "Code Cat", icon: "🐱" },
    { id: "DebugOwl", label: "Debug Owl", icon: "🦉" },
    { id: "BitBot", label: "Bit Bot", icon: "🤖" },
    { id: "SyntaxFox", label: "Syntax Fox", icon: "🦊" },
    { id: "LaunchRocket", label: "Launch Rocket", icon: "🚀" },
    { id: "Wizard", label: "Code Wizard", icon: "🧙" },
    { id: "JavaBeast", label: "Coffee Beast", icon: "☕" },
    { id: "SharpCoder", label: "Sharp Coder", icon: "⚡" },
  ];

  constructor(private gamificationService: GamificationService) {}

  public ngOnInit(): void {
    this.loadHallOfFame();
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if ((changes["courseId"] && !changes["courseId"].firstChange) ||
        (changes["periodId"] && !changes["periodId"].firstChange)) {
      this.loadHallOfFame();
    }
  }

  public loadHallOfFame(): void {
    if (this.courseId <= 0) return;
    this.loading = true;
    this.gamificationService.getHallOfFame(this.courseId, this.periodId).subscribe((res) => {
      this.loading = false;
      if (res.success && res.value) {
        this.hallOfFame = res.value;
      }
    });
  }

  public getAvatarIcon(avatarId?: string): string {
    const found = this.availableAvatars.find((a) => a.id === avatarId);
    return found ? found.icon : "🐱";
  }
}

