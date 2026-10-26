import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of, Subject } from "rxjs";
import { CourseGamificationAdminComponent } from "./course-gamification-admin.component";
import { GamificationService } from "../../services/gamification.service";
import { ToastrService } from "ngx-toastr";
import { GetResult, CreateResult, PostResult } from "../../util/result";
import {
  ICourseGamificationConfigModel,
  ICourseClassGroupModel,
} from "../../viewmodels/gamification.model";

describe("CourseGamificationAdminComponent", () => {
  let component: CourseGamificationAdminComponent;
  let fixture: ComponentFixture<CourseGamificationAdminComponent>;
  let gamificationServiceSpy: jasmine.SpyObj<GamificationService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  const mockConfig: ICourseGamificationConfigModel = {
    courseId: 10,
    isGamificationEnabled: true,
    isWeeklyStreakEnabled: true,
    isLeaderboardEnabled: true,
    isClassCompetitionEnabled: true,
    isTeamSynergyEnabled: true,
    isBadgesEnabled: true,
  };

  const mockClasses: ICourseClassGroupModel[] = [
    { id: 1, name: "1TIN-A", courseId: 10, periodId: 1 },
    { id: 2, name: "1TIN-B", courseId: 10, periodId: 1 },
  ];

  beforeEach(() => {
    gamificationServiceSpy = jasmine.createSpyObj<GamificationService>(
      "GamificationService",
      ["getConfig", "updateConfig", "getClasses", "addClass", "deleteClass"],
    );
    toastrSpy = jasmine.createSpyObj<ToastrService>("ToastrService", ["success", "error"]);

    gamificationServiceSpy.getConfig.and.returnValue(of(GetResult.success(mockConfig)));
    gamificationServiceSpy.getClasses.and.returnValue(of(GetResult.success(mockClasses)));

    TestBed.configureTestingModule({
      declarations: [CourseGamificationAdminComponent],
      providers: [
        { provide: GamificationService, useValue: gamificationServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });

    fixture = TestBed.createComponent(CourseGamificationAdminComponent);
    component = fixture.componentInstance;
    component.courseId = 10;
    component.periodId = 1;
    fixture.detectChanges();
  });

  it("should create and load config and class groups", () => {
    expect(component).toBeTruthy();
    expect(gamificationServiceSpy.getConfig).toHaveBeenCalledWith(10);
    expect(gamificationServiceSpy.getClasses).toHaveBeenCalledWith(10, 1);
    expect(component.config?.isGamificationEnabled).toBeTrue();
    expect(component.classGroups).toHaveSize(2);
    expect(component.loading).toBeFalse();
  });

  it("should save configuration and emit configChanged or show error", () => {
    gamificationServiceSpy.updateConfig.and.returnValue(of(GetResult.success(mockConfig)));
    spyOn(component.configChanged, "emit");
    component.saveConfig();
    expect(gamificationServiceSpy.updateConfig).toHaveBeenCalledWith(10, mockConfig);
    expect(toastrSpy.success).toHaveBeenCalled();
    expect(component.configChanged.emit).toHaveBeenCalledWith(mockConfig);

    gamificationServiceSpy.updateConfig.and.returnValue(of({ success: false, message: "Config err" } as any));
    component.saveConfig();
    expect(toastrSpy.error).toHaveBeenCalledWith("Config err", "Error");
  });

  it("should add class group and emit classesChanged or show error", () => {
    const newClass: ICourseClassGroupModel = { id: 3, name: "1TIN-C", courseId: 10, periodId: 1 };
    gamificationServiceSpy.addClass.and.returnValue(of(CreateResult.success(newClass)));
    spyOn(component.classesChanged, "emit");

    component.newClassName = "1TIN-C";
    component.addClass();

    expect(gamificationServiceSpy.addClass).toHaveBeenCalledWith(10, "1TIN-C", 1);
    expect(component.classGroups).toHaveSize(3);
    expect(component.newClassName).toBe("");
    expect(component.classesChanged.emit).toHaveBeenCalled();
    expect(toastrSpy.success).toHaveBeenCalled();

    gamificationServiceSpy.addClass.and.returnValue(of({ success: false, message: "Add err" } as any));
    component.newClassName = "1TIN-D";
    component.addClass();
    expect(toastrSpy.error).toHaveBeenCalledWith("Add err", "Error");
  });

  it("should delete class group when confirmed and emit classesChanged or handle cancel/error", () => {
    const confirmSpy = spyOn(window, "confirm").and.returnValue(true);
    gamificationServiceSpy.deleteClass.and.returnValue(of(PostResult.success()));
    spyOn(component.classesChanged, "emit");

    component.deleteClass(1);

    expect(gamificationServiceSpy.deleteClass).toHaveBeenCalledWith(10, 1);
    expect(component.classGroups).toHaveSize(1);
    expect(component.classGroups.find((c) => c.id === 1)).toBeUndefined();
    expect(component.classesChanged.emit).toHaveBeenCalled();
    expect(toastrSpy.success).toHaveBeenCalled();

    gamificationServiceSpy.deleteClass.and.returnValue(of({ success: false, message: "Del err" } as any));
    component.deleteClass(2);
    expect(toastrSpy.error).toHaveBeenCalledWith("Del err", "Error");

    gamificationServiceSpy.deleteClass.calls.reset();
    confirmSpy.and.returnValue(false);
    component.deleteClass(2);
    expect(gamificationServiceSpy.deleteClass).not.toHaveBeenCalled();
  });

  it("should reload data on ngOnChanges when courseId or periodId change", () => {
    component.ngOnChanges({
      courseId: new SimpleChange(10, 20, false),
    });
    expect(gamificationServiceSpy.getConfig).toHaveBeenCalledTimes(2);
  });

  it("should render feature switches when config arrives asynchronously without requiring input blur", () => {
    const configSubject = new Subject<GetResult<ICourseGamificationConfigModel>>();
    gamificationServiceSpy.getConfig.and.returnValue(configSubject);

    const asyncFixture = TestBed.createComponent(CourseGamificationAdminComponent);
    const asyncComp = asyncFixture.componentInstance;
    asyncComp.courseId = 10;
    asyncFixture.detectChanges(); // initial detection: config is null

    expect(asyncFixture.nativeElement.querySelector("#cfgGamification")).toBeNull();

    // Async response arrives from backend
    configSubject.next(GetResult.success(mockConfig));
    asyncFixture.detectChanges();

    // With OnPush + signals, the switch element is immediately rendered
    expect(asyncFixture.nativeElement.querySelector("#cfgGamification")).not.toBeNull();
  });
});
