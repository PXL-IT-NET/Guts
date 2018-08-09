import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { LocalStorageModule, LocalStorageService } from 'angular-2-local-storage';

import { AppComponent } from './components/app.component';
import { NavMenuComponent } from './components/nav-menu/nav-menu.component';
import { HomeComponent } from './components/home/home.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ConfirmEmailComponent } from './components/confirmemail/confirmemail.component';
import { ForgotPasswordComponent } from './components/forgotpassword/forgotpassword.component';
import { ResetPasswordComponent } from './components/resetpassword/resetpassword.component';
import { ChapterContentsComponent } from './components/chaptercontents/chaptercontents.component';
import { CourseContentsComponent } from './components/coursecontents/coursecontents.component';

import { AuthGuard } from './guards/auth.guard';
import { AuthService } from './services/auth.service';
import { ChartsModule } from 'ng2-charts/ng2-charts';
import { ClientSettingsService } from './services/client.settings.service';
import { CourseService } from './services/course.service';
import { ChapterService } from './services/chapter.service';
import { RecaptchaModule } from 'ng-recaptcha';
import { TokenInterceptor } from './util/tokeninterceptor';
import 'rxjs/Rx';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    LoginComponent,
    RegisterComponent,
    ConfirmEmailComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent,
    ChapterContentsComponent,
    CourseContentsComponent
  ],
  imports: [
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
      { path: 'courses/:courseId', component: CourseContentsComponent, canActivate: [AuthGuard] },
      { path: 'courses/:courseId/chapters/:chapterNumber', component: ChapterContentsComponent, canActivate: [AuthGuard] },
      { path: '**', redirectTo: 'home' }
    ]),
    LocalStorageModule.withConfig({
      prefix: 'guts',
      storageType: 'localStorage'
    }),
    RecaptchaModule.forRoot()
  ],
  providers: [
    AuthGuard,
    AuthService,
    CourseService,
    ChapterService,
    ClientSettingsService,
    LocalStorageService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: TokenInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
