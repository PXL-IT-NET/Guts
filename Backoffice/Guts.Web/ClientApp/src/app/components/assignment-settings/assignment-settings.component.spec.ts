import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssignmentSettingsComponent } from './assignment-settings.component';

describe('AssignmentSettingsComponent', () => {
  let component: AssignmentSettingsComponent;
  let fixture: ComponentFixture<AssignmentSettingsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AssignmentSettingsComponent]
    });
    fixture = TestBed.createComponent(AssignmentSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
