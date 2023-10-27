import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssessmentScoreDropDownComponent } from './assessment-score-dropdown.component';

describe('AssessmentScoreDropDownComponent', () => {
  let component: AssessmentScoreDropDownComponent;
  let fixture: ComponentFixture<AssessmentScoreDropDownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AssessmentScoreDropDownComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AssessmentScoreDropDownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
