import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectTeamAssessmentMyResultComponent } from './project-team-assessment-my-result.component';

describe('ProjectTeamAssessmentMyResultComponent', () => {
  let component: ProjectTeamAssessmentMyResultComponent;
  let fixture: ComponentFixture<ProjectTeamAssessmentMyResultComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ProjectTeamAssessmentMyResultComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ProjectTeamAssessmentMyResultComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
