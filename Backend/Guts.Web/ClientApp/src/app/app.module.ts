import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { LocalStorageModule, LocalStorageService } from 'angular-2-local-storage';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppComponent } from './components/app.component';
import { EmptyComponent } from './components/empty.component';
import { NavMenuComponent } from './components/nav-menu/nav-menu.component';
import { HomeComponent } from './components/home/home.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ConfirmEmailComponent } from './components/confirmemail/confirmemail.component';
import { ForgotPasswordComponent } from './components/forgotpassword/forgotpassword.component';
import { ResetPasswordComponent } from './components/resetpassword/resetpassword.component';

import { CourseComponent } from './components/course/course.component';
import { ChapterComponent } from "./components/chapter/chapter.component";
import { ChapterSummaryComponent } from "./components/chaptersummary/chaptersummary.component";
import { ExerciseDetailComponent } from './components/exercisedetail/exercisedetail.component';

import { AuthGuard } from './guards/auth.guard';
import { AuthService } from './services/auth.service';
import { ChartsModule } from 'ng2-charts/ng2-charts';
import { ClientSettingsService } from './services/client.settings.service';
import { CourseService } from './services/course.service';
import { ChapterService } from './services/chapter.service';
import { ChapterContextProvider } from './services/chapter.context.provider';
import { ExerciseService } from './services/exercise.service';
import { RecaptchaModule } from 'ng-recaptcha';
import { TokenInterceptor } from './util/tokeninterceptor';
import 'rxjs/Rx';
import { AngularDateTimePickerModule } from 'angular2-datetimepicker';
import { NgxLoadingModule, ngxLoadingAnimationTypes } from 'ngx-loading';
import { ToastrModule } from 'ngx-toastr';

@NgModule({
  declarations: [
    AppComponent,
    EmptyComponent,
    NavMenuComponent,
    HomeComponent,
    LoginComponent,
    RegisterComponent,
    ConfirmEmailComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent,
    CourseComponent,
    ChapterComponent,
    ChapterSummaryComponent,
    ExerciseDetailComponent
  ],
  imports: [
    CommonModule,
    BrowserAnimationsModule,
    ChartsModule,
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    RouterModule.forRoot([
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent },
      { path: 'confirmemail', component: ConfirmEmailComponent },
      { path: 'forgotpassword', component: ForgotPasswordComponent },
      { path: 'resetpassword', component: ResetPasswordComponent },
      { path: 'home', component: HomeComponent },
      {
        path: 'courses/:courseId', component: CourseComponent, canActivate: [AuthGuard],
        children: [
          {
            path: 'chapters/:chapterNumber', component: ChapterComponent, canActivate: [AuthGuard],
            children: [
              { path: 'users/:userId', component: EmptyComponent, canActivate: [AuthGuard] },
              { path: 'users/:userId/summary', component: ChapterSummaryComponent, canActivate: [AuthGuard] },
              { path: 'users/:userId/exercises/:exerciseId', component: ExerciseDetailComponent, canActivate: [AuthGuard] }
            ]
          }
        ]
      },
      { path: '**', redirectTo: 'home' }
    ]),
    LocalStorageModule.withConfig({
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
    AuthService,
    CourseService,
    ChapterService,
    ExerciseService,
    ClientSettingsService,
    LocalStorageService,
    ChapterContextProvider,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: TokenInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
