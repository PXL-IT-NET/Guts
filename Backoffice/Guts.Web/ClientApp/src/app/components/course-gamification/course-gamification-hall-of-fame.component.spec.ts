import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of, Subject } from "rxjs";
import { CourseGamificationHallOfFameComponent } from "./course-gamification-hall-of-fame.component";
import { GamificationService } from "../../services/gamification.service";
import { GetResult } from "../../util/result";
import { IHallOfFameLeaderboardModel } from "../../viewmodels/gamification.model";

describe("CourseGamificationHallOfFameComponent", () => {
  let component: CourseGamificationHallOfFameComponent;
  let fixture: ComponentFixture<CourseGamificationHallOfFameComponent>;
  let gamificationServiceSpy: jasmine.SpyObj<GamificationService>;

  const mockHallOfFame: IHallOfFameLeaderboardModel = {
    entries: [
      {
        rank: 1,
        userId: 4,
        displayName: "Leader",
        avatar: "Dragon",
        classGroupName: "1TIN-A",
        score: 200,
        level: 4,
        weeklyStreak: 3,
        greenTestsCount: 20,
        isCurrentUser: false,
        tier: 8,
        tierName: "Obsidian",
        tierIcon: "🌋",
      },
      {
        rank: 2,
        userId: 5,
        displayName: "PixelMage",
        avatar: "Wizard",
        classGroupName: "1TIN-A",
        score: 180,
        level: 3,
        weeklyStreak: 2,
        greenTestsCount: 18,
        isCurrentUser: true,
        tier: 8,
        tierName: "Obsidian",
        tierIcon: "🌋",
      },
    ],
    currentTier: {
      tier: 8,
      name: "Obsidian",
      icon: "🌋",
      minRank: 1,
      maxRank: 15,
      isCurrentTier: true,
      studentCount: 2,
    },
    visibleTiers: [
      { tier: 5, name: "Silver", icon: "⚔️", minRank: 56, maxRank: 75, isCurrentTier: false, studentCount: 0 },
      { tier: 6, name: "Gold", icon: "👑", minRank: 36, maxRank: 55, isCurrentTier: false, studentCount: 0 },
      { tier: 7, name: "Diamond", icon: "💎", minRank: 16, maxRank: 35, isCurrentTier: false, studentCount: 0 },
      { tier: 8, name: "Obsidian", icon: "🌋", minRank: 1, maxRank: 15, isCurrentTier: true, studentCount: 2 },
    ],
    allTiers: [
      { tier: 8, name: "Obsidian", icon: "🌋", minRank: 1, maxRank: 15, isCurrentTier: true, studentCount: 2 },
      { tier: 7, name: "Diamond", icon: "💎", minRank: 16, maxRank: 35, isCurrentTier: false, studentCount: 0 },
    ],
    nextTargetStudent: {
      rank: 1,
      userId: 4,
      displayName: "Leader",
      avatar: "Dragon",
      score: 200,
      xpNeededToOvertake: 20,
      tierName: "Obsidian",
      tierIcon: "🌋",
    },
    currentUserRank: 2,
    studentOptions: [],
  };

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
    expect(component.hallOfFame).toHaveSize(2);
    expect(component.visibleTiers).toHaveSize(4);
    expect(component.currentTier?.name).toBe("Obsidian");
    expect(component.nextTargetStudent?.displayName).toBe("Leader");
    expect(component.loading).toBeFalse();
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledWith(
      10,
      undefined,
      null,
      null,
    );
    expect(component.getAvatarIcon("Unknown")).toBe("🐱");

    const progressionEl = fixture.nativeElement.querySelector("#hallOfFameTierProgression");
    expect(progressionEl).toBeTruthy();
    expect(progressionEl.querySelectorAll(".hall-of-fame-tier-current")).toHaveSize(1);
    expect(progressionEl.querySelectorAll(".hall-of-fame-tier-neighbor")).toHaveSize(3);
    expect(fixture.nativeElement.textContent).toContain("Obsidian League");
    expect(fixture.nativeElement.textContent).not.toContain("#1–#15");

    const calloutEl = fixture.nativeElement.querySelector("#hallOfFameOvertakeCallout");
    expect(calloutEl).toBeTruthy();
    expect(calloutEl.textContent).toContain("Leader");
    expect(calloutEl.textContent).toContain("+4 XP");

    component.ngOnChanges({
      courseId: new SimpleChange(10, 20, false),
    });
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledTimes(2);

    component.ngOnChanges({
      periodId: new SimpleChange(1, 2, false),
    });
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledTimes(3);

    component.perspectiveUserId = 5;
    component.ngOnChanges({
      perspectiveUserId: new SimpleChange(null, 5, false),
    });
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledTimes(4);
  });

  it("should render teacher tier filter and reload when selecting a tier in All Levels mode", () => {
    component.isTeacher = true;
    component.perspectiveUserId = null;
    fixture.detectChanges();

    const filterEl = fixture.nativeElement.querySelector("#teacherAllTiersFilter");
    expect(filterEl).toBeTruthy();

    gamificationServiceSpy.getHallOfFame.calls.reset();
    component.selectTierFilter(7);
    expect(component.selectedTierFilter).toBe(7);
    expect(gamificationServiceSpy.getHallOfFame).toHaveBeenCalledWith(
      10,
      undefined,
      null,
      7,
    );
  });

  it("should update the DOM when hall of fame data arrives asynchronously", () => {
    const hallOfFameSubject = new Subject<GetResult<IHallOfFameLeaderboardModel>>();
    gamificationServiceSpy.getHallOfFame.and.returnValue(hallOfFameSubject);

    const asyncFixture = TestBed.createComponent(CourseGamificationHallOfFameComponent);
    const asyncComp = asyncFixture.componentInstance;
    asyncComp.courseId = 10;
    asyncFixture.detectChanges();

    expect(asyncFixture.nativeElement.textContent).toContain(
      "No students enrolled in the Hall of Fame yet.",
    );

    hallOfFameSubject.next(GetResult.success(mockHallOfFame));
    asyncFixture.detectChanges();

    expect(asyncFixture.nativeElement.textContent).toContain("PixelMage");
  });
});
