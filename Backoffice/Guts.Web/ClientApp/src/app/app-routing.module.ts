import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import * as c from "./components";
import { authGuard } from "./guards/auth.guard";

const routes: Routes = [
  { path: "", redirectTo: "home", pathMatch: "full" },
  { path: "login", component: c.LoginComponent },
  { path: "register", component: c.RegisterComponent },
  { path: "confirmemail", component: c.ConfirmEmailComponent },
  { path: "forgotpassword", component: c.ForgotPasswordComponent },
  { path: "resetpassword", component: c.ResetPasswordComponent },
  {
    path: "changepassword",
    component: c.ChangePasswordComponent,
    canActivate: [authGuard],
  },
  {
    path: "teacher-docs/concepts",
    loadComponent: () =>
      import(
        "./components/teacher-docs-concepts/teacher-docs-concepts.component"
      ).then((m) => m.TeacherDocsConceptsComponent),
  },
  {
    path: "teacher-docs/xunit",
    loadComponent: () =>
      import(
        "./components/teacher-docs-xunit/teacher-docs-xunit.component"
      ).then((m) => m.TeacherDocsXunitComponent),
  },
  {
    path: "teacher-docs/nunit",
    loadComponent: () =>
      import(
        "./components/teacher-docs-nunit/teacher-docs-nunit.component"
      ).then((m) => m.TeacherDocsNunitComponent),
  },
  {
    path: "teacher-docs/junit",
    loadComponent: () =>
      import(
        "./components/teacher-docs-junit/teacher-docs-junit.component"
      ).then((m) => m.TeacherDocsJunitComponent),
  },
  { path: "home", component: c.HomeComponent },
  {
    path: "courses/:courseId",
    component: c.CourseComponent,
    canActivate: [authGuard],
    children: [
      {
        path: "chapters/:chapterCode",
        redirectTo: "chapters/:chapterCode/testresults",
        pathMatch: "full",
      },
      {
        path: "chapters/:chapterCode/testresults",
        component: c.ChapterComponent,
        canActivate: [authGuard],
      },
      {
        path: "chapters/:chapterCode/settings",
        component: c.ChapterSettingsComponent,
        canActivate: [authGuard],
      },
      {
        path: "projects/:code",
        redirectTo: "projects/:code/assessments",
        pathMatch: "full",
      },
      {
        path: "projects/:code/testresults",
        component: c.ProjectComponent,
        canActivate: [authGuard],
      },
      {
        path: "projects/:code/teams",
        component: c.ProjectTeamComponent,
        canActivate: [authGuard],
      },
      {
        path: "projects/:code/assessments",
        component: c.ProjectAssessmentOverviewComponent,
        canActivate: [authGuard],
      },
      {
        path: "projects/:code/assessments/:assessmentId/teams/:teamId/evaluate",
        component: c.ProjectTeamAssessmentEvaluationFormComponent,
        canActivate: [authGuard],
      },
      {
        path: "projects/:code/assessments/:assessmentId/teams/:teamId/detailed-results",
        component: c.ProjectTeamAssessmentDetailedResultsComponent,
        canActivate: [authGuard],
      },
      {
        path: "projects/:code/assessments/:assessmentId/teams/:teamId/my-result",
        component: c.ProjectTeamAssessmentMyResultComponent,
        canActivate: [authGuard],
      },
      {
        path: "projects/:code/settings",
        component: c.ProjectSettingsComponent,
        canActivate: [authGuard],
      },
    ],
  },
  {
    path: "courses/:courseId/config",
    component: c.CourseConfigComponent,
    canActivate: [authGuard],
  },
  {
    path: "courses/:courseId/statistics",
    component: c.CourseStatisticsComponent,
    canActivate: [authGuard],
  },
  { path: "**", redirectTo: "home" },
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, {
      enableTracing: false,
      paramsInheritanceStrategy: "emptyOnly",
      bindToComponentInputs: true,
    }),
  ],
  exports: [RouterModule],
})
export class AppRoutingModule {}
