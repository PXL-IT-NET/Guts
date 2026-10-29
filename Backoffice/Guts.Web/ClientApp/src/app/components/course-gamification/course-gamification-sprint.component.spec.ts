import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
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

  it("should create and load weekly sprint data and reload on ngOnChanges", () => {
    expect(component).toBeTruthy();
    expect(component.weeklySprint).toHaveSize(1);
    expect(component.loading).toBeFalse();
    expect(component.isCurrentUserOnBoard).toBeTrue();
    expect(component.shouldShowGetOnBoardTrigger).toBeFalse();
    expect(fixture.nativeElement.querySelector("#weeklySprintGetOnBoardCallout")).toBeNull();
    expect(gamificationServiceSpy.getWeeklySprint).toHaveBeenCalledWith(
      10,
      undefined,
      undefined,
    );
    expect(component.getAvatarIcon("Unknown")).toBe("🐱");

    component.ngOnChanges({
      courseId: new SimpleChange(10, 20, false),
    });
    expect(gamificationServiceSpy.getWeeklySprint).toHaveBeenCalledTimes(2);

    component.ngOnChanges({
      periodId: new SimpleChange(1, 2, false),
    });
    expect(gamificationServiceSpy.getWeeklySprint).toHaveBeenCalledTimes(3);

    component.perspectiveUserId = 42;
    component.ngOnChanges({
      perspectiveUserId: new SimpleChange(undefined, 42, false),
    });
    expect(gamificationServiceSpy.getWeeklySprint).toHaveBeenCalledTimes(4);
    expect(gamificationServiceSpy.getWeeklySprint).toHaveBeenCalledWith(
      10,
      undefined,
      42,
    );
  });

  it("should filter out any entries with 0 XP and show trigger callout when student is not on the weekly sprint board, and hide it for teachers without student perspective", () => {
    gamificationServiceSpy.getWeeklySprint.and.returnValue(
      of(
        GetResult.success([
          {
            rank: 1,
            userId: 99,
            displayName: "OtherStudent",
            avatar: "Wizard",
            classGroupName: "1TIN-A",
            score: 50,
            level: 3,
            weeklyStreak: 2,
            greenTestsCount: 5,
            isCurrentUser: false,
          },
          {
            rank: 2,
            userId: 5,
            displayName: "ZeroStudent",
            avatar: "CodeCat",
            classGroupName: "1TIN-A",
            score: 0,
            level: 1,
            weeklyStreak: 0,
            greenTestsCount: 0,
            isCurrentUser: true,
          },
        ]),
      ),
    );
    component.loadSprint();
    fixture.detectChanges();

    expect(component.weeklySprint).toHaveSize(1);
    expect(component.isCurrentUserOnBoard).toBeFalse();
    expect(component.shouldShowGetOnBoardTrigger).toBeTrue();
    const callout = fixture.nativeElement.querySelector("#weeklySprintGetOnBoardCallout");
    expect(callout).toBeTruthy();
    expect(callout.textContent).toContain("You're not on the Weekly Sprint board yet!");

    // Teacher without selected perspective should not see the student trigger
    component.isTeacher = true;
    component.perspectiveUserId = null;
    fixture.detectChanges();
    expect(component.shouldShowGetOnBoardTrigger).toBeFalse();
    expect(fixture.nativeElement.querySelector("#weeklySprintGetOnBoardCallout")).toBeNull();

    // Teacher viewing a selected student's perspective should see the trigger if that student is not on the board
    component.perspectiveUserId = 12;
    fixture.detectChanges();
    expect(component.shouldShowGetOnBoardTrigger).toBeTrue();
    expect(fixture.nativeElement.querySelector("#weeklySprintGetOnBoardCallout")).toBeTruthy();
  });
});
