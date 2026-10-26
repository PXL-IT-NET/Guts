import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ProjectService } from "../../services/project.service";
import { ToastrService } from "ngx-toastr";
import { Subscription } from "rxjs";
import { ITeamDetailsModel } from "../../viewmodels/team.model";
import { GetResult, PostResult } from "../../util/result";
import { AuthService } from "../../services/auth.service";
import { UserProfile } from "../../viewmodels/user.model";
import { BsModalRef, BsModalService, ModalOptions } from "ngx-bootstrap/modal";
import { ProjectTeamEditComponent } from "../project-team-edit/project-team-edit.component";
import { ProjectTeamAddComponent } from "../project-team-add/project-team-add.component";
import { PeriodProvider } from "src/app/services/period.provider";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-team.component.html",
})
export class ProjectTeamComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private readonly _teams = signal<ITeamDetailsModel[]>([]);
  public get teams(): ITeamDetailsModel[] {
    return this._teams();
  }
  public set teams(v: ITeamDetailsModel[]) {
    this._teams.set(v);
  }

  private readonly _myTeam = signal<ITeamDetailsModel | null>(null);
  public get myTeam(): ITeamDetailsModel | null {
    return this._myTeam();
  }
  public set myTeam(v: ITeamDetailsModel | null) {
    this._myTeam.set(v);
  }

  private readonly _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private readonly _activePeriod = signal<boolean>(true);
  public get activePeriod(): boolean {
    return this._activePeriod();
  }
  public set activePeriod(v: boolean) {
    this._activePeriod.set(v);
  }

  public teamBaseName = "";
  public teamNumberFrom = 0;
  public teamNumberTo = 0;

  public modalRef: BsModalRef;

  private userProfileSubscription: Subscription;

  private courseId = 0;
  private projectCode = "";

  constructor(
    private readonly projectService: ProjectService,
    private readonly authService: AuthService,
    private readonly periodProvider: PeriodProvider,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService,
    private readonly modalService: BsModalService,
  ) {}

  ngOnInit() {
    this.courseId = +this.route.parent.snapshot.params["courseId"];
    this.projectCode = this.route.snapshot.params["code"];

    this.periodProvider.period$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((period) => {
        if (period) {
          this.activePeriod = period.isActive;
        }
      });

    this.loadData(false);
  }



  public leaveMyTeam() {
    let confirmMessage =
      "Are you sure you want to leave '" +
      this.myTeam.name +
      "'? All your submitted test results and peer assessments for this project will be deleted.";
    if (confirm(confirmMessage)) {
      this.projectService
        .leaveTeam(this.courseId, this.projectCode, this.myTeam.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.loadData(true);
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Could not leave team",
            );
          }
        });
    }
  }

  public deleteTeam(team: ITeamDetailsModel) {
    let confirmMessage =
      "Are you sure you want to remove '" +
      team.name +
      "'? All test results and peer assessments of this team will also be deleted.";
    if (confirm(confirmMessage)) {
      this.projectService
        .deleteTeam(this.courseId, this.projectCode, team.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.loadData(true);
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Could not delete team",
            );
          }
        });
    }
  }

  public joinTeam(teamId: number) {
    this.loading = true;
    this.projectService
      .joinTeam(this.courseId, this.projectCode, teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.loadData(true);
        } else {
          this.toastr.error(
            result.message || "unknown error",
            "Could not join team",
          );
        }
      });
  }

  public openTeamEditModal(team: ITeamDetailsModel) {
    let modalState: ModalOptions = {
      initialState: {
        courseId: this.courseId,
        projectCode: this.projectCode,
        team: team,
      },
    };
    this.modalRef = this.modalService.show(
      ProjectTeamEditComponent,
      modalState,
    );
    this.modalRef.setClass("modal-lg");
  }

  public openTeamAddModal() {
    let modalState: ModalOptions = {
      initialState: {
        courseId: this.courseId,
        projectCode: this.projectCode,
      },
    };
    this.modalRef = this.modalService.show(ProjectTeamAddComponent, modalState);
    this.modalRef.setClass("modal-lg");
    this.modalRef.content.teamsAdded
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadData(false);
      });
  }

  private loadData(clearCachedUserProfile: boolean) {
    this.loading = true;
    this.userProfileSubscription?.unsubscribe();
    if (clearCachedUserProfile) {
      this.authService.invalidateUserProfileCache();
    }
    this.userProfileSubscription = this.authService
      .getUserProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((profile) => {
        this.userProfile = profile;
        this.loading = false;
        this.loadTeams();
      });
  }

  private loadTeams() {
    this.loading = true;
    this.projectService
      .getTeams(this.courseId, this.projectCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result: GetResult<ITeamDetailsModel[]>) => {
        this.loading = false;
        if (result.success) {
          this.teams = [...result.value].sort((a, b) =>
            a.name.localeCompare(b.name),
          );
          this.myTeam =
            this.teams.find((team) =>
              team.members.some(
                (member) => member.userId === this.userProfile.id,
              ),
            ) || null;
        } else {
          this.toastr.error(
            "Could not load teams from API. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }
}
