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
    expect(component.classGroups.length).toBe(2);
    expect(component.hasClasses).toBeTrue();
    expect(component.config?.isGamificationEnabled).toBeTrue();
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

  it("should confirm class selection and unlock leaderboard", () => {
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

  it("should adjust active tab if current tab is disabled by switch", () => {
    component.activeTab = "sprint";
    component.config = {
      ...mockConfig,
      isWeeklyStreakEnabled: false,
      isLeaderboardEnabled: true,
    };
    component.adjustActiveTabForConfig();

    expect(component.activeTab).toBe("hallOfFame");
  });
});
