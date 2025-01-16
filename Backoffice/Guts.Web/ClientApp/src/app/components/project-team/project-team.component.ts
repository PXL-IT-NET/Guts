import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ProjectService } from '../../services/project.service';
import { GetResult } from "../../util/result";
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { ITeamDetailsModel} from "../../viewmodels/team.model";
import { PostResult } from "../../util/result";
import { AuthService } from "../../services/auth.service";
import { UserProfile } from "../../viewmodels/user.model";
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ProjectTeamEditComponent } from '../project-team-edit/project-team-edit.component';
import { ProjectTeamAddComponent } from '../project-team-add/project-team-add.component';
import { PeriodProvider } from 'src/app/services/period.provider';

@Component({
  templateUrl: './project-team.component.html'
})
export class ProjectTeamComponent implements OnInit, OnDestroy {

  public loading: boolean = false;
  public teams: ITeamDetailsModel[];
  public myTeam: ITeamDetailsModel;
  public userProfile: UserProfile;
  public activePeriod: boolean = true;

  public teamBaseName: string;
  public teamNumberFrom: number;
  public teamNumberTo: number;

  public modalRef: BsModalRef;

  private userProfileSubscription: Subscription;

  private courseId: number;
  private projectCode: string;

  constructor(private projectService: ProjectService,
    private authService: AuthService,
    private periodProvider: PeriodProvider,
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private modalService: BsModalService) {
    this.teams = [];
    this.myTeam = null;
    this.teamBaseName = '';
    this.teamNumberFrom = 0;
    this.teamNumberTo = 0;
    this.courseId = 0;
    this.projectCode = '';
  }

  ngOnInit() {
    this.courseId = +this.route.parent.snapshot.params['courseId']
    this.projectCode = this.route.snapshot.params['code'];

    this.userProfile = new UserProfile();

    this.periodProvider.period$.subscribe((period) => {
      if(period) {
        this.activePeriod = period.isActive;
      }
    });

    this.loadData(false);
  }

  ngOnDestroy() {
    this.userProfileSubscription?.unsubscribe();
  }

  public leaveMyTeam() {
    let confirmMessage = "Are you sure you want to leave '" + this.myTeam.name + "'? All your submitted test results and peer assessments for this project will be deleted.";
    if (confirm(confirmMessage)) {
      this.projectService.leaveTeam(this.courseId, this.projectCode, this.myTeam.id)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.loadData(true);
          } else {
            this.toastr.error(result.message || "unknown error", "Could not leave team");
          }
        });
    }
  }

  public deleteTeam(team: ITeamDetailsModel) {
    let confirmMessage = "Are you sure you want to remove '" + team.name + "'? All test results and peer assessments of this team will also be deleted.";
    if (confirm(confirmMessage)) {
      this.projectService.deleteTeam(this.courseId, this.projectCode, team.id)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.loadData(true);
          } else {
            this.toastr.error(result.message || "unknown error", "Could not delete team");
          }
        });
    }
  }

  public joinTeam(teamId: number) {
    this.loading = true;
    this.projectService.joinTeam(this.courseId, this.projectCode, teamId)
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.loadData(true);
        } else {
          this.toastr.error(result.message || "unknown error", "Could not join team");
        }
      });
  }

  public openTeamEditModal(team: ITeamDetailsModel) {
    let modalState: ModalOptions = {
      initialState: {
        courseId: this.courseId,
        projectCode: this.projectCode,
        team: team
      }
    };
    this.modalRef = this.modalService.show(ProjectTeamEditComponent, modalState);
    this.modalRef.setClass('modal-lg')
  }

  public openTeamAddModal() {
    let modalState: ModalOptions = {
      initialState: {
        courseId: this.courseId,
        projectCode: this.projectCode,
      }
    };
    this.modalRef = this.modalService.show(ProjectTeamAddComponent, modalState);
    this.modalRef.setClass('modal-lg');
    this.modalRef.content.teamsAdded.subscribe(() => {
      this.loadData(false);
    });
  }

  private loadData(clearCachedUserProfile: boolean) {
    this.loading = true;
    this.userProfileSubscription?.unsubscribe();
    if (clearCachedUserProfile) {
      this.authService.invalidateUserProfileCache();
    }
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
      this.loading = false;
      this.loadTeams();
    });
  }

  private loadTeams() {
    this.loading = true;
    this.projectService.getTeams(this.courseId, this.projectCode).subscribe((result: GetResult<ITeamDetailsModel[]>) => {
      this.loading = false;
      if (result.success) {
        this.teams = result.value.sort((a, b) => a.name.localeCompare(b.name));
        this.myTeam = this.teams.find(team => team.members.some(member => member.userId == this.userProfile.id)) || null;
      } else {
        this.toastr.error("Could not load teams from API. Message: " + (result.message || "unknown error"), "System error");
      }
    });
  }
}
