import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { CourseGamificationSprintComponent } from "./course-gamification-sprint.component";
import { GamificationService } from "../../services/gamification.service";
import { GetResult } from "../../util/result";
import { ILeaderboardEntryModel } from "../../viewmodels/gamification.model";

describe("CourseGamificationSprintComponent", () => {
  let component: CourseGamificationSprintComponent;
  let fixture: ComponentFixture<CourseGamificationSprintComponent>;
  let gamificationServiceSpy: jasmine.SpyObj<GamificationService>;

  const mockWeekly: ILeaderboardEntryModel[] = [
    {
      rank: 1,
      userId: 5,
      displayName: "PixelMage",
      avatar: "Wizard",
      classGroupName: "1TIN-A",
      score: 50,
      level: 3,
      weeklyStreak: 2,
      greenTestsCount: 5,
      isCurrentUser: true,
    },
  ];

  beforeEach(() => {
    gamificationServiceSpy = jasmine.createSpyObj<GamificationService>(
      "GamificationService",
      ["getWeeklySprint"],
    );
    gamificationServiceSpy.getWeeklySprint.and.returnValue(
      of(GetResult.success(mockWeekly)),
    );

    TestBed.configureTestingModule({
      declarations: [CourseGamificationSprintComponent],
      providers: [
        { provide: GamificationService, useValue: gamificationServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationSprintComponent);
    component = fixture.componentInstance;
    component.courseId = 10;
    fixture.detectChanges();
  });

  it("should create and load weekly sprint data", () => {
    expect(component).toBeTruthy();
    expect(component.weeklySprint.length).toBe(1);
    expect(gamificationServiceSpy.getWeeklySprint).toHaveBeenCalledWith(10, undefined);
  });
});

