import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  signal,
} from "@angular/core";
import { GamificationService } from "../../services/gamification.service";
import { AuthService } from "../../services/auth.service";
import { ToastrService } from "ngx-toastr";
import {
  IStudentGamificationProfileModel,
  ICourseClassGroupModel,
  IEnrollStudentModel,
  AVAILABLE_AVATARS,
  IAvatarModel,
} from "../../viewmodels/gamification.model";
import { UserProfile } from "../../viewmodels/user.model";

@Component({
  selector: "app-course-gamification-hero",
  templateUrl: "./course-gamification-hero.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: false,
})
export class CourseGamificationHeroComponent implements OnInit, OnChanges {
  @Input() public courseId: number = 0;
  @Input() public periodId?: number;
  @Input() public classGroups: ICourseClassGroupModel[] = [];

  @Output() public profileLoaded = new EventEmitter<IStudentGamificationProfileModel>();
  @Output() public profileUpdated = new EventEmitter<IStudentGamificationProfileModel>();

  private _profile = signal<IStudentGamificationProfileModel | null>(null);
  @Input()
  public get profile(): IStudentGamificationProfileModel | null {
    return this._profile();
  }
  public set profile(v: IStudentGamificationProfileModel | null) {
    this._profile.set(v);
  }

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private _showSettingsModal = signal<boolean>(false);
  public get showSettingsModal(): boolean {
    return this._showSettingsModal();
  }
  public set showSettingsModal(v: boolean) {
    this._showSettingsModal.set(v);
  }

  private _editModel = signal<IEnrollStudentModel>({
    showInLeaderBoard: true,
    usePseudonym: true,
    pseudonym: "",
    avatar: "CodeCat",
    classGroupId: null,
  });
  public get editModel(): IEnrollStudentModel {
    return this._editModel();
  }
  public set editModel(v: IEnrollStudentModel) {
    this._editModel.set(v);
  }

  public availableAvatars: IAvatarModel[] = AVAILABLE_AVATARS;

  constructor(
    private gamificationService: GamificationService,
    private authService: AuthService,
    private toastr: ToastrService,
  ) {}

  public ngOnInit(): void {
    this.authService.getUserProfile().subscribe((p) => {
      this.userProfile = p;
    });
    if (!this.profile) {
      this.loadProfile();
    }
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if ((changes["courseId"] && !changes["courseId"].firstChange) ||
        (changes["periodId"] && !changes["periodId"].firstChange)) {
      if (!this.profile) {
        this.loadProfile();
      }
    }
  }

  public loadProfile(): void {
    if (this.courseId <= 0) return;
    this.gamificationService.getProfile(this.courseId, this.periodId).subscribe((res) => {
      if (res.success && res.value) {
        this.profile = res.value;
        this.profileLoaded.emit(res.value);
      }
    });
  }

  public openSettings(): void {
    if (!this.profile) return;
    this.editModel = {
      showInLeaderBoard: this.profile.showInLeaderBoard,
      usePseudonym: this.profile.usePseudonym,
      pseudonym: this.profile.pseudonym,
      avatar: this.profile.avatar,
      classGroupId: this.profile.classGroupId,
    };
    this.showSettingsModal = true;
  }

  public saveSettings(): void {
    if (this.courseId <= 0) return;
    this.gamificationService.enroll(this.courseId, this.editModel, this.periodId).subscribe((res) => {
      if (res.success && res.value) {
        if (!res.value.classGroupName && res.value.classGroupId) {
          const matchedGroup = this.classGroups.find((cg) => cg.id === res.value.classGroupId);
          if (matchedGroup) {
            res.value.classGroupName = matchedGroup.name;
          }
        }
        this.profile = res.value;
        this.showSettingsModal = false;
        this.toastr.success("Gamer settings updated successfully!", "Success");
        this.profileUpdated.emit(res.value);
      } else {
        this.toastr.error(res.message || "Failed to save settings.", "Error");
      }
    });
  }

  public getAvatarIcon(avatarId?: string): string {
    const found = this.availableAvatars.find((a) => a.id === avatarId);
    return found ? found.icon : "🐱";
  }

  public getDisplayName(): string {
    if (!this.profile) return "";
    if (this.profile.usePseudonym) {
      return this.profile.pseudonym || "";
    }
    const fullName = `${this.profile.firstName || ""} ${this.profile.lastName || ""}`.trim();
    if (fullName) {
      return fullName;
    }
    return this.userProfile?.email || this.profile.pseudonym || "";
  }
}

