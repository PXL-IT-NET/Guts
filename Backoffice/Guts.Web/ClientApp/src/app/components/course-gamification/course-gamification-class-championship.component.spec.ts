import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { CourseGamificationClassChampionshipComponent } from "./course-gamification-class-championship.component";
import { GamificationService } from "../../services/gamification.service";
import { GetResult } from "../../util/result";
import { IClassGroupLeaderboardEntryModel } from "../../viewmodels/gamification.model";

describe("CourseGamificationClassChampionshipComponent", () => {
  let component: CourseGamificationClassChampionshipComponent;
  let fixture: ComponentFixture<CourseGamificationClassChampionshipComponent>;
  let gamificationServiceSpy: jasmine.SpyObj<GamificationService>;

  const mockClasses: IClassGroupLeaderboardEntryModel[] = [
    {
      rank: 1,
      classGroupId: 1,
      classGroupName: "1TIN-A",
      studentCount: 5,
      totalGreenTests: 40,
      averageXp: 72.0,
      totalXp: 360,
    },
  ];

  beforeEach(() => {
    gamificationServiceSpy = jasmine.createSpyObj<GamificationService>(
      "GamificationService",
      ["getClassLeaderboard"],
    );
    gamificationServiceSpy.getClassLeaderboard.and.returnValue(
      of(GetResult.success(mockClasses)),
    );

    TestBed.configureTestingModule({
      declarations: [CourseGamificationClassChampionshipComponent],
      providers: [
        { provide: GamificationService, useValue: gamificationServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationClassChampionshipComponent);
    component = fixture.componentInstance;
    component.courseId = 10;
    fixture.detectChanges();
  });

  it("should create and load class leaderboard data", () => {
    expect(component).toBeTruthy();
    expect(component.classLeaderboard.length).toBe(1);
    expect(gamificationServiceSpy.getClassLeaderboard).toHaveBeenCalledWith(10, undefined);
  });
});

