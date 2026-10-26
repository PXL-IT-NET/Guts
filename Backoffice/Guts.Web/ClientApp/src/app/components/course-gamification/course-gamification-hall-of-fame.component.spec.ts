import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of, Subject } from "rxjs";
import { CourseGamificationHallOfFameComponent } from "./course-gamification-hall-of-fame.component";
import { GamificationService } from "../../services/gamification.service";
import { GetResult } from "../../util/result";
import { ILeaderboardEntryModel } from "../../viewmodels/gamification.model";

describe("CourseGamificationHallOfFameComponent", () => {
  let component: CourseGamificationHallOfFameComponent;
  let fixture: ComponentFixture<CourseGamificationHallOfFameComponent>;
  let gamificationServiceSpy: jasmine.SpyObj<GamificationService>;

  const mockHallOfFame: ILeaderboardEntryModel[] = [
    {
      rank: 1,
      userId: 5,
      displayName: "PixelMage",
      avatar: "Wizard",
      classGroupName: "1TIN-A",
      score: 180,
      level: 3,
      weeklyStreak: 2,
      greenTestsCount: 18,
      isCurrentUser: true,
    },
  ];

  beforeEach(() => {
    gamificationServiceSpy = jasmine.createSpyObj<GamificationService>(
      "GamificationService",
      ["getHallOfFame"],
    );
    gamificationServiceSpy.getHallOfFame.and.returnValue(
      of(GetResult.success(mockHallOfFame)),
    );

    TestBed.configureTestingModule({
      declarations: [CourseGamificationHallOfFameComponent],
      providers: [
        { provide: GamificationService, useValue: gamificationServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationHallOfFameComponent);
    component = fixture.componentInstance;
    component.courseId = 10;
    fixture.detectChanges();
  });

  it("should create and load hall of fame data and reload on ngOnChanges", () => {
    expect(component).toBeTruthy();
    expect(component.hallOfFame).toHaveSize(1);
    expect(component.loading).toBeFalse();
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledWith(10, undefined);
    expect(component.getAvatarIcon("Unknown")).toBe("🐱");

    component.ngOnChanges({
      courseId: new SimpleChange(10, 20, false),
    });
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledTimes(2);

    component.ngOnChanges({
      periodId: new SimpleChange(1, 2, false),
    });
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledTimes(3);
  });

  it("should update the DOM when hall of fame data arrives asynchronously", () => {
    const hallOfFameSubject = new Subject<GetResult<ILeaderboardEntryModel[]>>();
    gamificationServiceSpy.getHallOfFame.and.returnValue(hallOfFameSubject);

    const asyncFixture = TestBed.createComponent(CourseGamificationHallOfFameComponent);
    const asyncComp = asyncFixture.componentInstance;
    asyncComp.courseId = 10;
    asyncFixture.detectChanges(); // initial detection with empty array

    expect(asyncFixture.nativeElement.textContent).toContain("No students enrolled in the Hall of Fame yet.");

    // Async response arrives from backend
    hallOfFameSubject.next(GetResult.success(mockHallOfFame));
    asyncFixture.detectChanges();

    // With OnPush change detection, if signals/markForCheck are not used, the DOM is not updated!
    expect(asyncFixture.nativeElement.textContent).toContain("PixelMage");
  });
});
