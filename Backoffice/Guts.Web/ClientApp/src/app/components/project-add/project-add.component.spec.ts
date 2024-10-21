import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectAddComponent } from './project-add.component';

describe('ProjectAddComponent', () => {
  let component: ProjectAddComponent;
  let fixture: ComponentFixture<ProjectAddComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ProjectAddComponent]
    });
    fixture = TestBed.createComponent(ProjectAddComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
