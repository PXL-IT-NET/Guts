//angular
import { BrowserModule } from "@angular/platform-browser";
import { NgModule, provideZoneChangeDetection } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import {
  provideHttpClient,
  withInterceptors,
  withXhr
} from "@angular/common/http";
import { provideAnimations } from "@angular/platform-browser/animations";
import {
  BaseChartDirective,
  provideCharts,
  withDefaultRegisterables,
} from "ng2-charts";

//own
import { AppRoutingModule } from "./app-routing.module";
import * as c from "./components";
import { tokenInterceptor } from "./interceptors/tokeninterceptor";
import { relativeInterceptor } from "./interceptors/relative.interceptor";
import { PositiveNumberValidatorDirective } from "./util/positive-number.directive";
import { NgDatetimeComponent } from "./components/ng-datetime/ng-datetime.component";
import { RecaptchaDirective } from "./directives/recaptcha.directive";

// 3th party
import {
  NgbModule,
  NgbAccordionConfig,
  NgbTypeahead,
  NgbTypeaheadConfig,
} from "@ng-bootstrap/ng-bootstrap";
import { ToastrModule } from "ngx-toastr";
import { ModalModule } from "ngx-bootstrap/modal";
import { TooltipModule } from "ngx-bootstrap/tooltip";

@NgModule({
  declarations: [
    c.AppComponent,
    c.NavMenuComponent,
    c.SidebarMenuComponent,
    c.HomeComponent,
    c.LoginComponent,
    c.RegisterComponent,
    c.ConfirmEmailComponent,
    c.ChangePasswordComponent,
    c.ForgotPasswordComponent,
    c.ResetPasswordComponent,
    c.CourseComponent,
    c.CourseConfigComponent,
    c.CourseStatisticsComponent,
    c.CourseGamificationComponent,
    c.CourseGamificationHeroComponent,
    c.CourseGamificationSprintComponent,
    c.CourseGamificationHallOfFameComponent,
    c.CourseGamificationClassChampionshipComponent,
    c.CourseGamificationBadgesComponent,
    c.CourseGamificationAdminComponent,
    c.ChapterComponent,
    c.ProjectComponent,
    c.ChapterSettingsComponent,
    c.ChapterSummaryComponent,
    c.AssignmentDetailComponent,
    c.AssignmentSummaryComponent,
    c.AssignmentStatisticsComponent,
    c.ProjectAddComponent,
    c.ProjectSettingsComponent,
    c.ProjectTeamComponent,
    c.ProjectTeamAddComponent,
    c.ProjectTeamEditComponent,
    c.ProjectSummaryComponent,
    c.ProjectAssessmentOverviewComponent,
    c.ProjectAssessmentAddComponent,
    c.ProjectAssessmentEditComponent,
    c.ExamComponent,
    c.ExampartComponent,
    PositiveNumberValidatorDirective,
    NgDatetimeComponent,
    c.ProjectTeamAssessmentEvaluationFormComponent,
    c.ProjectTeamAssessmentDetailedResultsComponent,
    c.ProjectTeamAssessmentMyResultComponent,
    c.AssessmentScoreDropdownComponent,
    c.AssignmentSettingsComponent,
    c.TypeaheadComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    CommonModule,
    BaseChartDirective,
    RecaptchaDirective,
    c.EmptyComponent,
    c.LoadingOverlayComponent,
    FormsModule,
    ReactiveFormsModule,
    NgbModule,
    ToastrModule.forRoot(),
    ModalModule,
    TooltipModule,
  ],
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideAnimations(),
    NgbAccordionConfig,
    NgbTypeahead,
    NgbTypeaheadConfig,
    provideHttpClient(
      withXhr(),
      withInterceptors([relativeInterceptor, tokenInterceptor]),
    ),
    provideCharts(withDefaultRegisterables()),
  ],
  bootstrap: [c.AppComponent],
})
export class AppModule {}
