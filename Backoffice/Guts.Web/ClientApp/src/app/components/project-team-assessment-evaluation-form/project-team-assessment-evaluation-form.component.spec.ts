import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectTeamAssessmentEvaluationFormComponent } from './project-team-assessment-evaluation-form.component';

describe('ProjectTeamAssessmentEvaluationFormComponent', () => {
  let component: ProjectTeamAssessmentEvaluationFormComponent;
  let fixture: ComponentFixture<ProjectTeamAssessmentEvaluationFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ProjectTeamAssessmentEvaluationFormComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ProjectTeamAssessmentEvaluationFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
