import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { CourseGamificationComponent } from "./course-gamification.component";
import { GamificationService } from "../../services/gamification.service";
import { AuthService } from "../../services/auth.service";
import { ToastrService } from "ngx-toastr";
import { PeriodProvider } from "../../services/period.provider";
import {
  createActivatedRouteProvider,
  createToastrSpy,
} from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import {
  IStudentGamificationProfileModel,
  ICourseClassGroupModel,
  ICourseGamificationConfigModel,
  IHallOfFameLeaderboardModel,
} from "../../viewmodels/gamification.model";
import { UserProfile } from "../../viewmodels/user.model";

describe("CourseGamificationComponent", () => {
  let component: CourseGamificationComponent;
  let fixture: ComponentFixture<CourseGamificationComponent>;
  let gamificationServiceSpy: jasmine.SpyObj<GamificationService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;
  let periodProvider: PeriodProvider;

  const mockProfile: IStudentGamificationProfileModel = {
    userId: 5,
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
    earnedBadges: [
      {
        badgeType: 1,
        name: "First Spark",
        description: "Passed your first test",
        icon: "🌱",
        earnedAt: "2026-10-21T00:00:00Z",
      },
    ],
  };

  const mockClassGroups: ICourseClassGroupModel[] = [
    { id: 1, name: "1TIN-A", courseId: 10, periodId: 1 },
    { id: 2, name: "1TIN-B", courseId: 10, periodId: 1 },
  ];

  const mockConfig: ICourseGamificationConfigModel = {
    courseId: 10,
    isGamificationEnabled: true,
    isWeeklyStreakEnabled: true,
    isLeaderboardEnabled: true,
    isClassCompetitionEnabled: true,
    isTeamSynergyEnabled: true,
    isBadgesEnabled: true,
  };

  const mockHallOfFame: IHallOfFameLeaderboardModel = {
    entries: [],
    currentTier: null,
    visibleTiers: [],
    allTiers: [],
    nextTargetStudent: null,
    currentUserRank: null,
    studentOptions: [
      {
        userId: 5,
        displayName: "PixelMage",
        rank: 1,
        score: 180,
        tierName: "Obsidian",
        tierIcon: "🌋",
      },
    ],
  };

  beforeEach(() => {
    gamificationServiceSpy = jasmine.createSpyObj<GamificationService>(
      "GamificationService",
      [
        "getProfile",
        "enroll",
        "getWeeklySprint",
        "getHallOfFame",
        "getClassLeaderboard",
        "getConfig",
        "updateConfig",
        "getClasses",
        "addClass",
        "deleteClass",
      ],
    );

    gamificationServiceSpy.getClasses.and.returnValue(
      of(GetResult.success(mockClassGroups)),
    );
    gamificationServiceSpy.getProfile.and.returnValue(
      of(GetResult.success(mockProfile)),
    );
    gamificationServiceSpy.getConfig.and.returnValue(
      of(GetResult.success(mockConfig)),
    );
    gamificationServiceSpy.enroll.and.returnValue(
      of(GetResult.success(mockProfile)),
    );
    gamificationServiceSpy.getHallOfFame.and.returnValue(
      of(GetResult.success(mockHallOfFame)),
    );

    const userProfile = new UserProfile();
    userProfile.id = 5;
    userProfile.roles = ["student"];

    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getUserProfile",
    ]);
    authServiceSpy.getUserProfile.and.returnValue(of(userProfile));

    toastrSpy = createToastrSpy();
    periodProvider = new PeriodProvider();

    TestBed.configureTestingModule({
      declarations: [CourseGamificationComponent],
      providers: [
        createActivatedRouteProvider({ courseId: 10 }),
        { provide: GamificationService, useValue: gamificationServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
        { provide: PeriodProvider, useValue: periodProvider },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create component and load class groups and config", () => {
    expect(component).toBeTruthy();
    expect(component.courseId).toBe(10);
    expect(component.classGroups).toHaveSize(2);
    expect(component.hasClasses).toBeTrue();
    expect(component.config?.isGamificationEnabled).toBeTrue();
    component.loading = true;
    expect(component.loading).toBeTrue();
  });

  it("should only retrieve profile once on initialization even with active period", () => {
    component.ngOnDestroy();
    gamificationServiceSpy.getProfile.calls.reset();
    periodProvider.period = { id: 1, description: "2026-2027", isActive: true } as any;

    const testFixture = TestBed.createComponent(CourseGamificationComponent);
    testFixture.detectChanges();

    expect(gamificationServiceSpy.getProfile).toHaveBeenCalledTimes(1);
    testFixture.componentInstance.ngOnDestroy();
  });

  it("should not load personal profile or render hero for teacher, and should render student perspective bar", () => {
    const teacherProfile = new UserProfile();
    teacherProfile.id = 1;
    teacherProfile.roles = ["lector"];
    authServiceSpy.getUserProfile.and.returnValue(of(teacherProfile));
    gamificationServiceSpy.getProfile.calls.reset();

    const teacherFixture = TestBed.createComponent(CourseGamificationComponent);
    const teacherComp = teacherFixture.componentInstance;
    teacherFixture.detectChanges();

    expect(gamificationServiceSpy.getProfile).not.toHaveBeenCalled();
    expect(teacherComp.studentOptions).toHaveSize(1);
    expect(teacherFixture.nativeElement.querySelector("app-course-gamification-hero")).toBeNull();

    const perspectiveBar = teacherFixture.nativeElement.querySelector("#teacherStudentPerspectiveBar");
    expect(perspectiveBar).toBeTruthy();

    teacherComp.perspectiveUserId = 5;
    expect(teacherComp.perspectiveUserId).toBe(5);

    teacherComp.perspectiveUserId = null;
    expect(teacherComp.perspectiveUserId).toBeNull();
    teacherComp.ngOnDestroy();
  });

  it("should hide class championship if there are no classes", () => {
    component.classGroups = [];
    expect(component.hasClasses).toBeFalse();
  });

  it("should require class selection if classes exist and student has no class group", () => {
    component.profile = {
      ...mockProfile,
      classGroupId: null,
      classGroupName: null,
    };
    component.checkClassSelectionRequirement();

    expect(component.needsClassSelection).toBeTrue();
  });

  it("should not require class selection if student is already in a class", () => {
    component.profile = mockProfile;
    component.checkClassSelectionRequirement();

    expect(component.needsClassSelection).toBeFalse();
  });

  it("should not require class selection if user is a teacher", () => {
    const teacherProfile = new UserProfile();
    teacherProfile.id = 1;
    teacherProfile.roles = ["lector"];
    component.userProfile = teacherProfile;
    component.profile = {
      ...mockProfile,
      classGroupId: null,
      classGroupName: null,
    };
    component.checkClassSelectionRequirement();

    expect(component.needsClassSelection).toBeFalse();
  });

  it("should confirm class selection and unlock leaderboard or show error on failure", () => {
    component.heroComponent = {
      loadProfile: jasmine.createSpy("loadProfile"),
    } as any;

    component.profile = {
      ...mockProfile,
      classGroupId: null,
      classGroupName: null,
    };
    component.checkClassSelectionRequirement();
    expect(component.needsClassSelection).toBeTrue();

    component.selectedClassGroupId = 2;
    component.confirmClassSelection();

    expect(gamificationServiceSpy.enroll).toHaveBeenCalled();
    expect(component.needsClassSelection).toBeFalse();
    expect(toastrSpy.success).toHaveBeenCalled();
    expect(component.heroComponent.loadProfile).toHaveBeenCalled();

    // Test failure path and null profile fallback
    component.profile = null;
    gamificationServiceSpy.enroll.and.returnValue(of({ success: false, message: "Fail" } as any));
    component.confirmClassSelection();
    expect(toastrSpy.error).toHaveBeenCalledWith("Fail", "Error");
  });

  it("should handle onProfileLoaded, onProfileUpdated, onClassesChanged, and onConfigChanged", () => {
    component.onProfileLoaded({ ...mockProfile, totalXp: 300 });
    expect(component.profile?.totalXp).toBe(300);

    component.onProfileUpdated({
      ...mockProfile,
      classGroupId: 2,
      classGroupName: "",
    });
    expect(component.profile?.classGroupName).toBe("1TIN-B");

    gamificationServiceSpy.getClasses.calls.reset();
    component.onClassesChanged();
    expect(gamificationServiceSpy.getClasses).toHaveBeenCalled();

    component.onConfigChanged({ ...mockConfig, isWeeklyStreakEnabled: false });
    expect(component.config?.isWeeklyStreakEnabled).toBeFalse();

    gamificationServiceSpy.getConfig.calls.reset();
    component.onConfigChanged();
    expect(gamificationServiceSpy.getConfig).toHaveBeenCalled();
  });

  it("should show disabled alert when gamification is disabled and user is student", () => {
    component.config = {
      ...mockConfig,
      isGamificationEnabled: false,
    };
    fixture.detectChanges();

    const alertEl = fixture.nativeElement.querySelector("#gamificationDisabledAlert");
    expect(alertEl).toBeTruthy();
    expect(alertEl.textContent).toContain("Gamification is Disabled");
  });

  it("should show teacher warning and default to admin tab when gamification disabled and user is teacher", () => {
    const teacherProfile = new UserProfile();
    teacherProfile.id = 1;
    teacherProfile.roles = ["lector"];
    component.userProfile = teacherProfile;
    component.config = {
      ...mockConfig,
      isGamificationEnabled: false,
    };
    component.adjustActiveTabForConfig();
    fixture.detectChanges();

    expect(component.activeTab).toBe("admin");
    const bannerEl = fixture.nativeElement.querySelector("#teacherGamificationDisabledBanner");
    expect(bannerEl).toBeTruthy();
  });

  it("should adjust active tab and getFirstEnabledTab for all combinations", () => {
    component.activeTab = "sprint";
    component.config = {
      ...mockConfig,
      isWeeklyStreakEnabled: false,
      isLeaderboardEnabled: true,
    };
    component.adjustActiveTabForConfig();
    expect(component.activeTab).toBe("hallOfFame");

    component.config = {
      ...mockConfig,
      isWeeklyStreakEnabled: false,
      isLeaderboardEnabled: false,
      isClassCompetitionEnabled: true,
    };
    component.adjustActiveTabForConfig();
    expect(component.activeTab).toBe("classes");

    component.config = {
      ...mockConfig,
      isWeeklyStreakEnabled: false,
      isLeaderboardEnabled: false,
      isClassCompetitionEnabled: false,
      isBadgesEnabled: true,
    };
    component.adjustActiveTabForConfig();
    expect(component.activeTab).toBe("badges");

    const teacherProfile = new UserProfile();
    teacherProfile.id = 1;
    teacherProfile.roles = ["lector"];
    component.userProfile = teacherProfile;
    component.config = {
      ...mockConfig,
      isWeeklyStreakEnabled: false,
      isLeaderboardEnabled: false,
      isClassCompetitionEnabled: false,
      isBadgesEnabled: false,
    };
    component.adjustActiveTabForConfig();
    expect(component.activeTab).toBe("admin");
  });
});
