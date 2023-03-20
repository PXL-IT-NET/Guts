import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectTeamAssessmentDetailedResultsComponent } from './project-team-assessment-detailed-results.component';

describe('ProjectTeamAssessmentDetailedResultsComponent', () => {
  let component: ProjectTeamAssessmentDetailedResultsComponent;
  let fixture: ComponentFixture<ProjectTeamAssessmentDetailedResultsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ProjectTeamAssessmentDetailedResultsComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ProjectTeamAssessmentDetailedResultsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
