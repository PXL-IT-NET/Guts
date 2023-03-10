import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectAssessmentOverviewComponent } from './project-assessment-overview.component';

describe('ProjectAssessmentOverviewComponent', () => {
  let component: ProjectAssessmentOverviewComponent;
  let fixture: ComponentFixture<ProjectAssessmentOverviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ProjectAssessmentOverviewComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ProjectAssessmentOverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
