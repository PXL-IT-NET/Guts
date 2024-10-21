import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChapterSettingsComponent } from './chapter-settings.component';

describe('ChapterSettingsComponent', () => {
  let component: ChapterSettingsComponent;
  let fixture: ComponentFixture<ChapterSettingsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ChapterSettingsComponent]
    });
    fixture = TestBed.createComponent(ChapterSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
