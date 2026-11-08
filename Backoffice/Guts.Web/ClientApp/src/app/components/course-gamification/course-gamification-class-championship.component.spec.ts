import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
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
      classScore: 385,
      totalXp: 360,
      isCurrentUserClass: true,
      currentUserContribution: 85,
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

  it("should create, load class leaderboard data, expose currentUserClassEntry, and reload on ngOnChanges", () => {
    expect(component).toBeTruthy();
    expect(component.classLeaderboard).toHaveSize(1);
    expect(component.currentUserClassEntry?.classGroupName).toBe("1TIN-A");
    expect(component.currentUserClassEntry?.currentUserContribution).toBe(85);
    expect(component.loading).toBeFalse();
    expect(gamificationServiceSpy.getClassLeaderboard).toHaveBeenCalledWith(
      10,
      undefined,
      undefined,
    );

    const nativeEl: HTMLElement = fixture.nativeElement;
    expect(nativeEl.querySelector("#classPersonalContributionCallout")?.textContent).toContain("+85 pts");
    expect(nativeEl.querySelector("#yourClassBadge")?.textContent).toContain("Your Class");
    expect(nativeEl.textContent).toContain("385 pts");
    expect(nativeEl.textContent).not.toContain("Average XP");
    expect(nativeEl.textContent).not.toContain("Total XP");
    expect(nativeEl.textContent).not.toContain("Total Green Tests");

    component.ngOnChanges({
      courseId: new SimpleChange(10, 20, false),
    });
    expect(gamificationServiceSpy.getClassLeaderboard).toHaveBeenCalledTimes(2);

    component.ngOnChanges({
      periodId: new SimpleChange(1, 2, false),
    });
    expect(gamificationServiceSpy.getClassLeaderboard).toHaveBeenCalledTimes(3);

    component.perspectiveUserId = 42;
    component.ngOnChanges({
      perspectiveUserId: new SimpleChange(null, 42, false),
    });
    expect(gamificationServiceSpy.getClassLeaderboard).toHaveBeenCalledWith(
      10,
      undefined,
      42,
    );
  });
});
