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
      { path: 'chapters/:chapterCode/settings', component: c.ChapterSettingsComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code', redirectTo: 'projects/:code/assessments', pathMatch: 'full' },
      { path: 'projects/:code/testresults', component: c.ProjectComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/teams', component: c.ProjectTeamComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/assessments', component: c.ProjectAssessmentOverviewComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/assessments/:assessmentId/teams/:teamId/evaluate', component: c.ProjectTeamAssessmentEvaluationFormComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/assessments/:assessmentId/teams/:teamId/detailed-results', component: c.ProjectTeamAssessmentDetailedResultsComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/assessments/:assessmentId/teams/:teamId/my-result', component: c.ProjectTeamAssessmentMyResultComponent, canActivate: [AuthGuard] },
      { path: 'projects/:code/settings', component: c.ProjectSettingsComponent, canActivate: [AuthGuard] }
    ],
  },
  {
    path: 'courses/:courseId/config', component: c.CourseConfigComponent, canActivate: [AuthGuard],
  },
  { path: '**', redirectTo: 'home' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {enableTracing: false})],
  exports: [RouterModule]
})
export class AppRoutingModule { }
