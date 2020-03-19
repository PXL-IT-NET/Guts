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
          {
            path: 'chapters/:chapterCode', component: c.ChapterComponent, canActivate: [AuthGuard],
            children: [
              { path: 'users/:userId', component: c.EmptyComponent, canActivate: [AuthGuard] },
              { path: 'users/:userId/summary', component: c.ChapterSummaryComponent, canActivate: [AuthGuard] },
              { path: 'users/:userId/exercises/:assignmentId', component: c.AssignmentDetailComponent, canActivate: [AuthGuard] }
            ]
          },
          {
            path: 'projects/:code', component: c.ProjectComponent, canActivate: [AuthGuard],
            children: [
              { path: 'teams', component: c.ProjectTeamOverviewComponent, canActivate: [AuthGuard] },
              { path: 'teams/:teamId/summary', component: c.ProjectSummaryComponent, canActivate: [AuthGuard] },
              { path: 'teams/:teamId/components/:assignmentId', component: c.AssignmentDetailComponent, canActivate: [AuthGuard] }
            ]
          }
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
