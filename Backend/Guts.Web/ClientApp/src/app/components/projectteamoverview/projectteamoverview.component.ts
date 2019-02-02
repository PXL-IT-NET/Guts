import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProjectService } from '../../services/project.service';
import { TopicContextProvider } from "../../services/topic.context.provider";
import { GetResult } from "../../util/Result";
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { ITeamDetailsModel } from "../../viewmodels/team.model";
import { PostResult } from "../../util/Result";


@Component({
  templateUrl: './projectteamoverview.component.html'
})
export class ProjectTeamOverviewComponent implements OnInit, OnDestroy {

  public loading: boolean = false;
  public teams: ITeamDetailsModel[];

  private topicContextSubscription: Subscription;

  constructor(private projectService: ProjectService,
    private topicContextProvider: TopicContextProvider,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
    this.teams = [];

    this.topicContextSubscription = this.topicContextProvider.topicChanged$.subscribe(() => {
      this.loadTeams();
    });
  }

  ngOnInit() {

  }

  ngOnDestroy() {
    this.topicContextSubscription.unsubscribe();
  }

  public joinTeam(teamId: number) {
    this.loading = true;
    this.projectService.joinTeam(this.topicContextProvider.currentContext.courseId,
      this.topicContextProvider.currentContext.topic.code,
      teamId)
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.router.navigate(['/courses', this.topicContextProvider.currentContext.courseId]);
        } else {
          this.toastr.error(result.message || "unknown error", "Could not join team");
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
