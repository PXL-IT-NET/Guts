import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";

import { ChapterSettingsComponent } from "./chapter-settings.component";
import { ChapterService } from "src/app/services";

describe("ChapterSettingsComponent", () => {
  let component: ChapterSettingsComponent;
  let fixture: ComponentFixture<ChapterSettingsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ChapterSettingsComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({}),
            snapshot: { params: {} },
            parent: { snapshot: { params: {} } },
          },
        },
        {
          provide: ChapterService,
          useValue: {
            getChapterDetails: () => of({ success: false, message: "" }),
            updateChapter: () => of({ success: false, message: "" }),
          },
        },
        {
          provide: ToastrService,
          useValue: { error: () => {}, success: () => {}, warning: () => {} },
        },
      ],
    });
    fixture = TestBed.createComponent(ChapterSettingsComponent);
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
