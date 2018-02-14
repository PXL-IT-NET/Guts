import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LocalStorageModule, LocalStorageService } from 'angular-2-local-storage';

import { AppComponent } from './components/app/app.component';
import { NavMenuComponent } from './components/navmenu/navmenu.component';
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
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';


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
        CommonModule,
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
    ]
})
export class AppModuleShared {
}
