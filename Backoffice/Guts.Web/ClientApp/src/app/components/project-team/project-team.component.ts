import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProjectService } from '../../services/project.service';
import { GetResult } from "../../util/Result";
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { ITeamDetailsModel, TeamGenerationModel } from "../../viewmodels/team.model";
import { PostResult } from "../../util/Result";
import { AuthService } from "../../services/auth.service";
import { UserProfile } from "../../viewmodels/user.model";

@Component({
  templateUrl: './project-team.component.html'
})
export class ProjectTeamComponent implements OnInit, OnDestroy {

  public loading: boolean = false;
  public teams: ITeamDetailsModel[];
  public myTeam: ITeamDetailsModel;
  public userProfile: UserProfile;

  public teamBaseName: string;
  public teamNumberFrom: number;
  public teamNumberTo: number;

  private userProfileSubscription: Subscription;

  private courseId: number;
  private projectCode: string;

  constructor(private projectService: ProjectService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
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
    this.loadData(false);
  }

  ngOnDestroy() {
    this.userProfileSubscription.unsubscribe();
  }

  public leaveMyTeam() {
    let confirmMessage = "Are you sure you want to leave '" + this.myTeam.name + "'? All your submitted test results and peer assessments for this project will be deleted.";
    if(confirm(confirmMessage)){
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

  public onGenerateTeamsSubmit() {
    this.loading = true;
    var model = new TeamGenerationModel(this.teamBaseName, this.teamNumberFrom, this.teamNumberTo);
    this.projectService.generateTeams(this.courseId, this.projectCode, model)
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.toastr.success("Teams generated");
          this.loadData(false);
        } else {
          this.toastr.error(result.message || "unknown error", "Could not generate teams");
        }
      });
  }

  private loadData(clearCachedUserProfile: boolean){
    this.loading = true;
    this.userProfileSubscription?.unsubscribe();
    if(clearCachedUserProfile){
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
        this.teams = result.value;
        this.myTeam = this.teams.find(team => team.members.some(member => member.userId == this.userProfile.id)) || null;
      } else {
        this.toastr.error("Could not load teams from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }
}
