import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssessmentScoreDropdownComponent } from './assessment-score-dropdown.component';

describe('AssessmentScoreDropDownComponent', () => {
  let component: AssessmentScoreDropdownComponent;
  let fixture: ComponentFixture<AssessmentScoreDropdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AssessmentScoreDropdownComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AssessmentScoreDropdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
