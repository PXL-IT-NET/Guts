//angular
import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations'

//own
import { AppRoutingModule } from './app-routing.module';
import * as c from './components';
import * as s from './services';
import { AuthGuard } from './guards/auth.guard';
import { TokenInterceptor } from './interceptors/tokeninterceptor';
import { RelativeInterceptor } from './interceptors/relative.interceptor';
import { PositiveNumberValidatorDirective } from './util/positive-number.directive';
import { NgDatetimeComponent } from './components/ng-datetime/ng-datetime.component';

// 3th party
import { NgChartsModule } from 'ng2-charts';
import { NgbModule, NgbAccordionConfig, NgbTypeahead, NgbTypeaheadConfig } from '@ng-bootstrap/ng-bootstrap';
import { LocalStorageModule, LocalStorageService } from 'angular-2-local-storage';
import { RecaptchaModule } from 'ng-recaptcha';
import { NgxLoadingModule, ngxLoadingAnimationTypes } from 'ngx-loading';
import { ToastrModule } from 'ngx-toastr';

@NgModule({
  declarations: [
    c.AppComponent,
    c.EmptyComponent,
    c.NavMenuComponent,
    c.SidebarMenuComponent,
    c.HomeComponent,
    c.LoginComponent,
    c.RegisterComponent,
    c.ConfirmEmailComponent,
    c.ForgotPasswordComponent,
    c.ResetPasswordComponent,
    c.CourseComponent,
    c.CourseConfigComponent,
    c.ChapterComponent,
    c.ProjectComponent,
    c.ChapterSummaryComponent,
    c.AssignmentDetailComponent,
    c.AssignmentSummaryComponent,
    c.AssignmentStatisticsComponent,
    c.ProjectTeamComponent,
    c.ProjectSummaryComponent,
    c.ProjectAssessmentOverviewComponent,
    c.ExampartComponent,
    PositiveNumberValidatorDirective,
    NgDatetimeComponent,
    c.ProjectAssessmentOverviewComponent,
    c.ProjectTeamAssessmentEvaluationFormComponent,
    c.ProjectTeamAssessmentDetailedResultsComponent,
    c.ProjectTeamAssessmentMyResultComponent,
    c.AssessmentScoreDropdownComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    CommonModule,
    BrowserAnimationsModule,
    NgChartsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModule,
    LocalStorageModule.forRoot({
      prefix: 'guts',
      storageType: 'localStorage'
    }),
    RecaptchaModule,
    NgxLoadingModule.forRoot({
      animationType: ngxLoadingAnimationTypes.threeBounce,
      backdropBackgroundColour: 'rgba(255,255,255,0.3)',
      backdropBorderRadius: '4px',
      primaryColour: '#f5f5f0',
      secondaryColour: '#e0e0d1',
      tertiaryColour: '#ccccb3'
    }),
    ToastrModule.forRoot()
  ],
  providers: [
    AuthGuard,
    s.AuthService,
    s.CourseService,
    s.ChapterService,
    s.ProjectService,
    s.ProjectAssessmentService,
    s.ProjectTeamAssessmentService,
    s.AssignmentService,
    s.ExamService,
    s.ClientSettingsService,
    LocalStorageService,
    NgbAccordionConfig,
    NgbTypeahead,
    NgbTypeaheadConfig,
    {provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true},
    {provide: HTTP_INTERCEPTORS, useClass: RelativeInterceptor, multi: true}
  ],
  bootstrap: [c.AppComponent]
})
export class AppModule { }
