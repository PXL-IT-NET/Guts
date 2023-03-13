import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import * as c from './components';
import { AuthGuard } from './guards/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'login', component: c.LoginComponent },
  { path: 'register', component: c.RegisterComponent },
  { path: 'confirmemail', component: c.ConfirmEmailComponent },
  { path: 'forgotpassword', component: c.ForgotPasswordComponent },
  { path: 'resetpassword', component: c.ResetPasswordComponent },
  { path: 'home', component: c.HomeComponent },
  {
    path: 'courses/:courseId', component: c.CourseComponent, canActivate: [AuthGuard],
    children: [
      { path: 'chapters/:chapterCode', redirectTo: 'chapters/:chapterCode/testresults', pathMatch: 'full' },
      { path: 'chapters/:chapterCode/testresults', component: c.ChapterComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code', redirectTo: 'projects/:code/testresults', pathMatch: 'full' },
      { path: 'projects/:code/testresults', component: c.ProjectComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/teams', component: c.ProjectTeamOverviewComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/assessments', component: c.ProjectAssessmentOverviewComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/assessments/:assessmentId/teams/:teamId/evaluate', component: c.ProjectTeamAssessmentEvaluationFormComponent, canActivate: [AuthGuard] }
    ],
  },
  {
    path: 'courses/:courseId/config', component: c.CourseConfigComponent, canActivate: [AuthGuard],
  },
  { path: '**', redirectTo: 'home' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
