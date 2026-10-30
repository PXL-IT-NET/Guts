import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { CourseGamificationBadgesComponent } from "./course-gamification-badges.component";
import {
  IStudentGamificationProfileModel,
  IBadgeMetadataModel,
} from "../../viewmodels/gamification.model";
import { GamificationService } from "../../services/gamification.service";
import { GetResult } from "../../util/result";

describe("CourseGamificationBadgesComponent", () => {
  let component: CourseGamificationBadgesComponent;
  let fixture: ComponentFixture<CourseGamificationBadgesComponent>;
  let gamificationServiceMock: any;

  const mockBadges: IBadgeMetadataModel[] = [
    { badgeType: 1, type: 1, name: "First Spark", icon: "🌱", description: "Passed your first test in the course" },
    { badgeType: 2, type: 2, name: "Speed Demon", icon: "⚡", description: "Completed an assignment within 24 hours" },
    { badgeType: 4, type: 4, name: "Clean Sweep", icon: "🧹", description: "Solved 100% of all exercises in a chapter" },
    { badgeType: 5, type: 5, name: "Night Owl", icon: "🦉", description: "Submitted a passing test between 00:00 and 05:00" },
    { badgeType: 6, type: 6, name: "Early Bird", icon: "🌅", description: "Submitted a passing test between 05:00 and 08:00" },
    { badgeType: 7, type: 7, name: "Weekend Warrior", icon: "⚔️", description: "Passed a test during the weekend" },
    { badgeType: 8, type: 8, name: "Century Club", icon: "💯", description: "Achieved 100 unique passing tests" },
    { badgeType: 9, type: 9, name: "On Fire", icon: "🔥", description: "Maintained a 4-week coding streak" },
    { badgeType: 10, type: 10, name: "Iron Will", icon: "🛡️", description: "Maintained an 8-week coding streak" },
    { badgeType: 11, type: 11, name: "Sprint Gold", icon: "🥇", description: "Finished #1 on the Weekly Sprint" },
    { badgeType: 13, type: 13, name: "Sprint Silver", icon: "🥈", description: "Finished in the top 2 of the Weekly Sprint" },
    { badgeType: 14, type: 14, name: "Sprint Bronze", icon: "🥉", description: "Finished in the top 3 of the Weekly Sprint" },
    { badgeType: 15, type: 15, name: "Sprint Top 4", icon: "🎖️", description: "Finished in the top 4 of the Weekly Sprint" },
    { badgeType: 16, type: 16, name: "Sprint Top 5", icon: "⭐", description: "Finished in the top 5 of the Weekly Sprint" },
    { badgeType: 12, type: 12, name: "Team Powerhouse", icon: "🤝", description: "All project team members have passing tests" },
  ];

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
    gamificationServiceMock = {
      getBadgeMetadata: jasmine.createSpy("getBadgeMetadata").and.returnValue(
        of(GetResult.success(mockBadges))
      ),
    };

    TestBed.configureTestingModule({
      declarations: [CourseGamificationBadgesComponent],
      providers: [
        { provide: GamificationService, useValue: gamificationServiceMock },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationBadgesComponent);
    component = fixture.componentInstance;
    component.profile = mockProfile;
    fixture.detectChanges();
  });

  it("should create and load badges from backend", () => {
    expect(component).toBeTruthy();
    expect(gamificationServiceMock.getBadgeMetadata).toHaveBeenCalled();
    expect(component.allBadges).toHaveSize(mockBadges.length);
    expect(component.allBadges.some((b) => b.badgeType === 3)).toBe(false);
    expect(component.allBadges.some((b) => b.badgeType === 11 && b.name === "Sprint Gold")).toBe(true);
    expect(component.allBadges.some((b) => b.badgeType === 13 && b.name === "Sprint Silver")).toBe(true);
    expect(component.allBadges.some((b) => b.badgeType === 14 && b.name === "Sprint Bronze")).toBe(true);
    expect(component.allBadges.some((b) => b.badgeType === 15 && b.name === "Sprint Top 4")).toBe(true);
    expect(component.allBadges.some((b) => b.badgeType === 16 && b.name === "Sprint Top 5")).toBe(true);
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

  it("should update earned badges when profile input changes to another student", () => {
    const studentTwoProfile: IStudentGamificationProfileModel = {
      ...mockProfile,
      userId: 2,
      earnedBadges: [
        {
          badgeType: 2,
          name: "Speed Demon",
          description: "Completed fast",
          icon: "⚡",
          earnedAt: "2026-10-26T14:00:00Z",
        },
      ],
    };

    component.profile = studentTwoProfile;
    fixture.detectChanges();

    expect(component.isBadgeEarned(1)).toBe(false);
    expect(component.isBadgeEarned(5)).toBe(false);
    expect(component.isBadgeEarned(2)).toBe(true);
    expect(component.getBadgeEarnedDate(2)).toBe("2026-10-26T14:00:00Z");
  });
});
