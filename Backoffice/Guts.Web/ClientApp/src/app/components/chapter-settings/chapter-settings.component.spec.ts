import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { ChapterService } from "src/app/services";
import {
  createActivatedRouteProvider,
  createToastrSpy,
} from "src/app/testing/test-helpers";
import { GetResult, PostResult } from "src/app/util/result";
import { ChapterSettingsComponent } from "./chapter-settings.component";

describe("ChapterSettingsComponent", () => {
  let component: ChapterSettingsComponent;
  let fixture: ComponentFixture<ChapterSettingsComponent>;
  let chapterServiceSpy: jasmine.SpyObj<ChapterService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    chapterServiceSpy = jasmine.createSpyObj<ChapterService>("ChapterService", [
      "getChapterDetails",
      "updateChapter",
    ]);
    toastrSpy = createToastrSpy();

    TestBed.configureTestingModule({
      declarations: [ChapterSettingsComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider({ chapterCode: "Ch1" }, { courseId: "3" }),
        { provide: ChapterService, useValue: chapterServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ChapterSettingsComponent);
    component = fixture.componentInstance;
  });

  it("ngOnInit should load chapter details into editChapterForm", () => {
    chapterServiceSpy.getChapterDetails.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "Ch1",
          description: "Chapter One",
          exercises: [],
          assignments: [],
          users: [],
        }),
      ),
    );

    component.ngOnInit();

    expect(chapterServiceSpy.getChapterDetails).toHaveBeenCalledWith(3, "Ch1");
    expect(component.editChapterForm.value).toEqual({
      code: "Ch1",
      description: "Chapter One",
    });
  });

  it("ngOnInit should show error toastr when loading chapter fails", () => {
    chapterServiceSpy.getChapterDetails.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Err", value: null }),
    );

    component.ngOnInit();

    expect(toastrSpy.error).toHaveBeenCalled();
  });

  it("onSubmit should update chapter when form is valid and handle success/error", () => {
    chapterServiceSpy.getChapterDetails.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "Ch1",
          description: "Chapter One",
          exercises: [],
          assignments: [],
          users: [],
        }),
      ),
    );
    component.ngOnInit();

    chapterServiceSpy.updateChapter.and.returnValue(of(PostResult.success()));
    component.editChapterForm.controls.description.setValue("Updated Desc");
    component.onSubmit();

    expect(chapterServiceSpy.updateChapter).toHaveBeenCalledWith(3, "Ch1", {
      description: "Updated Desc",
    });
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Chapter updated successfully",
    );

    chapterServiceSpy.updateChapter.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Bad" }),
    );
    component.onSubmit();
    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Bad",
      "Could not update chapter",
    );
  });

  it("isInvalid and onAssignmentChanged should validate controls and reload chapter", () => {
    chapterServiceSpy.getChapterDetails.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "Ch1",
          description: "Chapter One",
          exercises: [],
          assignments: [],
          users: [],
        }),
      ),
    );
    component.ngOnInit();

    const ctrl = component.editChapterForm.controls.description;
    ctrl.setValue("");
    ctrl.markAsTouched();
    expect(component.isInvalid(ctrl)).toBeTrue();

    chapterServiceSpy.getChapterDetails.calls.reset();
    component.onAssignmentChanged(null);
    expect(chapterServiceSpy.getChapterDetails).toHaveBeenCalled();
  });
});
