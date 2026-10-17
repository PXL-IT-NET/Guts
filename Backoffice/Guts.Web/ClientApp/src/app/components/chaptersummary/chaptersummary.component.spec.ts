import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import moment from "moment";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { ChapterService } from "../../services/chapter.service";
import {
  createActivatedRouteProvider,
  createToastrSpy,
} from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import {
  TopicStatisticsModel,
  TopicSummaryModel,
} from "../../viewmodels/topic.model";
import { ChapterSummaryComponent } from "./chaptersummary.component";

describe("ChapterSummaryComponent", () => {
  let component: ChapterSummaryComponent;
  let fixture: ComponentFixture<ChapterSummaryComponent>;
  let chapterServiceSpy: jasmine.SpyObj<ChapterService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    chapterServiceSpy = jasmine.createSpyObj<ChapterService>("ChapterService", [
      "getChapterStatistics",
      "getChapterSummary",
    ]);
    toastrSpy = createToastrSpy();

    TestBed.overrideComponent(ChapterSummaryComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [ChapterSummaryComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider(),
        { provide: ChapterService, useValue: chapterServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ChapterSummaryComponent);
    component = fixture.componentInstance;
  });

  it("ngOnChanges should load statistics and summary when inputs are valid", () => {
    const stats = new TopicStatisticsModel({
      id: 1,
      code: "Ch1",
      description: "Desc",
      assignments: [],
      assignmentStatistics: [],
    });
    const summary = new TopicSummaryModel({
      id: 1,
      code: "Ch1",
      description: "Desc",
      assignments: [],
      assignmentSummaries: [],
    });

    chapterServiceSpy.getChapterStatistics.and.returnValue(
      of(GetResult.success(stats)),
    );
    chapterServiceSpy.getChapterSummary.and.returnValue(
      of(GetResult.success(summary)),
    );

    component.courseId = 2;
    component.chapterCode = "Ch1";
    component.userId = 5;
    component.statusDate = moment();

    component.ngOnChanges({
      chapterCode: new SimpleChange("", "Ch1", true),
    });

    expect(chapterServiceSpy.getChapterStatistics).toHaveBeenCalled();
    expect(chapterServiceSpy.getChapterSummary).toHaveBeenCalled();
    expect(component.statistics).toBe(stats);
    expect(component.model).toBe(summary);
  });

  it("ngOnChanges should show error toastr when statistics or summary calls fail", () => {
    chapterServiceSpy.getChapterStatistics.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Stats err", value: null }),
    );
    chapterServiceSpy.getChapterSummary.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Summary err", value: null }),
    );

    component.courseId = 2;
    component.chapterCode = "Ch1";
    component.userId = 5;

    component.ngOnChanges({
      chapterCode: new SimpleChange("", "Ch1", true),
    });

    expect(toastrSpy.error).toHaveBeenCalledTimes(2);
  });

  it("ngOnChanges should not call getChapterSummary when userId is undefined (e.g. selectedUser is null)", () => {
    component.courseId = 2;
    component.chapterCode = "Ch1";
    component.userId = undefined;

    component.ngOnChanges({
      userId: new SimpleChange(5, undefined, false),
    });

    expect(chapterServiceSpy.getChapterSummary).not.toHaveBeenCalled();
  });
});

