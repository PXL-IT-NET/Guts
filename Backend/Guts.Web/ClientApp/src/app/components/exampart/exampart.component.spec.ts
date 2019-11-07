import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ExampartComponent } from './exampart.component';

describe('ExampartComponent', () => {
  let component: ExampartComponent;
  let fixture: ComponentFixture<ExampartComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ExampartComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ExampartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
