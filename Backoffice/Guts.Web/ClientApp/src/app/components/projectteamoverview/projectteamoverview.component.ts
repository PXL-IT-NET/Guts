import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProjectService } from '../../services/project.service';
import { TopicContextProvider } from "../../services/topic.context.provider";
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

  private topicContextSubscription: Subscription;
  private userProfileSubscription: Subscription;

  constructor(private projectService: ProjectService,
    private authService: AuthService,
    private topicContextProvider: TopicContextProvider,
    private router: Router,
    private toastr: ToastrService) {
    this.teams = [];
    this.teamBaseName = '';
    this.numberOfTeamsToGenerate = 0;

    this.topicContextSubscription = this.topicContextProvider.topicChanged$.subscribe(() => {
      this.loadTeams();
    });
  }

  ngOnInit() {
    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
    });
  }

  ngOnDestroy() {
    this.topicContextSubscription.unsubscribe();
    this.userProfileSubscription.unsubscribe();
  }

  public joinTeam(teamId: number) {
    this.loading = true;
    this.projectService.joinTeam(this.topicContextProvider.currentContext.courseId,
      this.topicContextProvider.currentContext.topic.code,
      teamId)
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
    this.projectService.generateTeams(this.topicContextProvider.currentContext.courseId,
      this.topicContextProvider.currentContext.topic.code, model)
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
    this.projectService.getTeams(this.topicContextProvider.currentContext.courseId, this.topicContextProvider.currentContext.topic.code).subscribe((result: GetResult<ITeamDetailsModel[]>) => {
      this.loading = false;
      if (result.success) {
        this.teams = result.value;
      } else {
        this.toastr.error("Could not load teams from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }
}
