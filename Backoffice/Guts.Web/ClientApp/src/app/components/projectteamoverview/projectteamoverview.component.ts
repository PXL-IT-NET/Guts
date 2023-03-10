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
  templateUrl: './projectteamoverview.component.html'
})
export class ProjectTeamOverviewComponent implements OnInit, OnDestroy {

  public loading: boolean = false;
  public teams: ITeamDetailsModel[];
  public userProfile: UserProfile;

  public teamBaseName: string;
  public numberOfTeamsToGenerate: number;

  private userProfileSubscription: Subscription;

  private courseId: number;
  private projectCode: string;

  constructor(private projectService: ProjectService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
    this.teams = [];
    this.teamBaseName = '';
    this.numberOfTeamsToGenerate = 0;
    this.courseId = 0;
    this.projectCode = '';
  }

  ngOnInit() {
    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
    });

    this.courseId = +this.route.parent.snapshot.params['courseId']
    this.projectCode = this.route.snapshot.params['code'];
    this.loadTeams();
  }

  ngOnDestroy() {
    this.userProfileSubscription.unsubscribe();
  }

  public joinTeam(teamId: number) {
    this.loading = true;
    this.projectService.joinTeam(this.courseId, this.projectCode, teamId)
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.router.navigate(['/home']); //qsdf
        } else {
          this.toastr.error(result.message || "unknown error", "Could not join team");
        }
      });
  }

  public onGenerateTeamsSubmit() {
    this.loading = true;
    var model = new TeamGenerationModel(this.teamBaseName, this.numberOfTeamsToGenerate);
    this.projectService.generateTeams(this.courseId, this.projectCode, model)
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.loadTeams();
        } else {
          this.toastr.error(result.message || "unknown error", "Could not generate teams");
        }
      });
  }

  private loadTeams() {
    this.loading = true;
    this.projectService.getTeams(this.courseId, this.projectCode).subscribe((result: GetResult<ITeamDetailsModel[]>) => {
      this.loading = false;
      if (result.success) {
        this.teams = result.value;
      } else {
        this.toastr.error("Could not load teams from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }
}
