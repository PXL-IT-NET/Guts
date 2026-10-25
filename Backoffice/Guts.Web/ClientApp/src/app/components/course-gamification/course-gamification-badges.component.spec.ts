import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { CourseGamificationBadgesComponent } from "./course-gamification-badges.component";
import { IStudentGamificationProfileModel } from "../../viewmodels/gamification.model";

describe("CourseGamificationBadgesComponent", () => {
  let component: CourseGamificationBadgesComponent;
  let fixture: ComponentFixture<CourseGamificationBadgesComponent>;

  const mockProfile: IStudentGamificationProfileModel = {
    userId: 1,
    courseId: 10,
    periodId: 1,
    showInLeaderBoard: true,
    usePseudonym: false,
    pseudonym: "",
    avatar: "Hero",
    totalXp: 120,
    level: 2,
    levelTitle: "Code Apprentice",
    currentWeeklyStreak: 2,
    bestWeeklyStreak: 2,
    isActiveThisWeek: true,
    totalGreenTests: 12,
    earnedBadges: [
      {
        badgeType: 1,
        name: "First Spark",
        description: "Passed your first test",
        icon: "🌱",
        earnedAt: "2026-10-25T10:00:00Z",
      },
      {
        badgeType: 5,
        name: "Night Owl",
        description: "Submitted at night",
        icon: "🦉",
        earnedAt: "2026-10-25T02:00:00Z",
      },
    ],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [CourseGamificationBadgesComponent],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationBadgesComponent);
    component = fixture.componentInstance;
    component.profile = mockProfile;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });

  it("should check if badge is earned", () => {
    expect(component.isBadgeEarned(1)).toBe(true);
    expect(component.isBadgeEarned(5)).toBe(true);
    expect(component.isBadgeEarned(2)).toBe(false);
  });

  it("should return badge earned date or null", () => {
    expect(component.getBadgeEarnedDate(1)).toBe("2026-10-25T10:00:00Z");
    expect(component.getBadgeEarnedDate(2)).toBeNull();
  });
});

