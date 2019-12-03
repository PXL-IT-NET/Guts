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

// 3th party
import { ChartsModule } from 'ng2-charts';
import { NgbModule, NgbAccordionConfig, NgbTypeahead, NgbTypeaheadConfig } from '@ng-bootstrap/ng-bootstrap';
import { LocalStorageModule, LocalStorageService } from 'angular-2-local-storage';
import { RecaptchaModule } from 'ng-recaptcha';
import { AngularDateTimePickerModule } from 'angular2-datetimepicker';
import { NgxLoadingModule, ngxLoadingAnimationTypes } from 'ngx-loading';
import { ToastrModule } from 'ngx-toastr';

@NgModule({
  declarations: [
    c.AppComponent,
    c.EmptyComponent,
    c.NavMenuComponent,
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
    c.ProjectTeamOverviewComponent,
    c.ProjectSummaryComponent,
    c.ExampartComponent,
    PositiveNumberValidatorDirective
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    CommonModule,
    BrowserAnimationsModule,
    ChartsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModule,
    LocalStorageModule.forRoot({
      prefix: 'guts',
      storageType: 'localStorage'
    }),
    RecaptchaModule.forRoot(),
    AngularDateTimePickerModule,
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
