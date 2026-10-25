import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { CourseGamificationHeroComponent } from "./course-gamification-hero.component";
import { GamificationService } from "../../services/gamification.service";
import { AuthService } from "../../services/auth.service";
import { ToastrService } from "ngx-toastr";
import { createToastrSpy } from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import { IStudentGamificationProfileModel } from "../../viewmodels/gamification.model";
import { UserProfile } from "../../viewmodels/user.model";

describe("CourseGamificationHeroComponent", () => {
  let component: CourseGamificationHeroComponent;
  let fixture: ComponentFixture<CourseGamificationHeroComponent>;
  let gamificationServiceSpy: jasmine.SpyObj<GamificationService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  const mockProfile: IStudentGamificationProfileModel = {
    userId: 5,
    firstName: "Alice",
    lastName: "Smith",
    courseId: 10,
    periodId: 1,
    showInLeaderBoard: true,
    usePseudonym: true,
    pseudonym: "PixelMage",
    avatar: "Wizard",
    classGroupId: 1,
    classGroupName: "1TIN-A",
    totalXp: 180,
    level: 3,
    levelTitle: "Logic Apprentice",
    currentWeeklyStreak: 2,
    bestWeeklyStreak: 4,
    isActiveThisWeek: true,
    totalGreenTests: 18,
    earnedBadges: [],
  };

  beforeEach(() => {
    gamificationServiceSpy = jasmine.createSpyObj<GamificationService>(
      "GamificationService",
      ["getProfile", "enroll"],
    );
    gamificationServiceSpy.getProfile.and.returnValue(
      of(GetResult.success(mockProfile)),
    );
    gamificationServiceSpy.enroll.and.returnValue(
      of(GetResult.success(mockProfile)),
    );

    const userProfile = new UserProfile();
    userProfile.id = 5;
    userProfile.email = "student@pxl.be";

    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getUserProfile",
    ]);
    authServiceSpy.getUserProfile.and.returnValue(of(userProfile));
    toastrSpy = createToastrSpy();

    TestBed.configureTestingModule({
      declarations: [CourseGamificationHeroComponent],
      providers: [
        { provide: GamificationService, useValue: gamificationServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationHeroComponent);
    component = fixture.componentInstance;
    component.courseId = 10;
    fixture.detectChanges();
  });

  it("should create and load profile", () => {
    expect(component).toBeTruthy();
    expect(component.profile).toEqual(mockProfile);
    expect(gamificationServiceSpy.getProfile).toHaveBeenCalledWith(10, undefined);
  });

  it("should open settings modal and populate editModel", () => {
    component.openSettings();
    expect(component.showSettingsModal).toBeTrue();
    expect(component.editModel.pseudonym).toBe("PixelMage");
    expect(component.editModel.showInLeaderBoard).toBeTrue();
  });

  it("should save settings and emit updated profile", () => {
    spyOn(component.profileUpdated, "emit");
    component.openSettings();
    component.editModel.pseudonym = "NewHero";
    component.saveSettings();

    expect(gamificationServiceSpy.enroll).toHaveBeenCalled();
    expect(toastrSpy.success).toHaveBeenCalled();
    expect(component.showSettingsModal).toBeFalse();
    expect(component.profileUpdated.emit).toHaveBeenCalledWith(mockProfile);
  });

  it("should have centering classes on the avatar element", () => {
    const avatarEl = fixture.nativeElement.querySelector(".rounded-circle");
    expect(avatarEl).toBeTruthy();
    expect(avatarEl.classList.contains("d-flex")).toBeTrue();
    expect(avatarEl.classList.contains("align-items-center")).toBeTrue();
    expect(avatarEl.classList.contains("justify-content-center")).toBeTrue();
    expect(avatarEl.classList.contains("mx-auto")).toBeTrue();
  });

  it("should display recent XP gains alert with event breakdown when recent gains exist", () => {
    component.profile = {
      ...mockProfile,
      recentGains: {
        totalXpGained: 60,
        previousVisitDate: "2026-10-20T10:00:00Z",
        events: [
          { description: "Completed assignment bonus", xpEarned: 50, createdAt: "2026-10-22T10:00:00Z" },
          { description: "Passed test #1", xpEarned: 10, createdAt: "2026-10-22T10:05:00Z" },
        ],
      },
    };
    fixture.detectChanges();

    const alertEl = fixture.nativeElement.querySelector("#recentGainsAlert");
    expect(alertEl).toBeTruthy();
    expect(alertEl.textContent).toContain("+60 XP earned since your last visit");
    expect(alertEl.textContent).toContain("Completed assignment bonus");
    expect(alertEl.textContent).toContain("Passed test #1");
  });

  it("should display no recent gains info when visited before but 0 XP gained", () => {
    component.profile = {
      ...mockProfile,
      recentGains: {
        totalXpGained: 0,
        previousVisitDate: "2026-10-20T10:00:00Z",
        events: [],
      },
    };
    fixture.detectChanges();

    const infoEl = fixture.nativeElement.querySelector("#noRecentGainsInfo");
    expect(infoEl).toBeTruthy();
    expect(infoEl.textContent).toContain("Last leaderboard visit");
  });

  it("should display pseudonym when usePseudonym is true", () => {
    component.profile = {
      ...mockProfile,
      usePseudonym: true,
      pseudonym: "PixelMage",
    };
    fixture.detectChanges();

    const nameEl = fixture.nativeElement.querySelector("h3");
    expect(nameEl.textContent.trim()).toBe("PixelMage");
  });

  it("should display firstname and lastname instead of email when usePseudonym is false", () => {
    component.profile = {
      ...mockProfile,
      usePseudonym: false,
      firstName: "Alice",
      lastName: "Smith",
    };
    fixture.detectChanges();

    const nameEl = fixture.nativeElement.querySelector("h3");
    expect(nameEl.textContent.trim()).toBe("Alice Smith");
    expect(nameEl.textContent).not.toContain("student@pxl.be");
  });
});

