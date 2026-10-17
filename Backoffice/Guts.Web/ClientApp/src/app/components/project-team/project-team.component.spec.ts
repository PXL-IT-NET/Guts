import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { BsModalService } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { of, Subject } from "rxjs";
import { PeriodProvider } from "src/app/services/period.provider";
import { AuthService } from "../../services/auth.service";
import { ProjectService } from "../../services/project.service";
import {
  createActivatedRouteProvider,
  createModalRefSpy,
  createModalServiceSpy,
  createToastrSpy,
} from "../../testing/test-helpers";
import { GetResult, PostResult } from "../../util/result";
import { UserProfile } from "../../viewmodels/user.model";
import { ProjectTeamComponent } from "./project-team.component";

describe("ProjectTeamComponent", () => {
  let component: ProjectTeamComponent;
  let fixture: ComponentFixture<ProjectTeamComponent>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let periodProvider: PeriodProvider;
  let toastrSpy: jasmine.SpyObj<ToastrService>;
  let modalServiceSpy: jasmine.SpyObj<BsModalService>;

  beforeEach(() => {
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "getTeams",
      "joinTeam",
      "leaveTeam",
      "deleteTeam",
    ]);
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getUserProfile",
      "invalidateUserProfileCache",
    ]);
    periodProvider = new PeriodProvider();
    periodProvider.period = {
      id: 1,
      description: "Active",
      from: null,
      until: null,
      isActive: true,
    };
    toastrSpy = createToastrSpy();
    modalServiceSpy = createModalServiceSpy(
      createModalRefSpy({ teamsAdded: new Subject<void>() }),
    );

    TestBed.overrideComponent(ProjectTeamComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [ProjectTeamComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider({ code: "P1" }, { courseId: "2" }),
        { provide: ProjectService, useValue: projectServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: PeriodProvider, useValue: periodProvider },
        { provide: ToastrService, useValue: toastrSpy },
        { provide: BsModalService, useValue: modalServiceSpy },
      ],
    });
    fixture = TestBed.createComponent(ProjectTeamComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("ngOnInit should load user profile, sort teams, and determine myTeam", () => {
    authServiceSpy.getUserProfile.and.returnValue(
      of(
        new UserProfile({
          id: 10,
          email: "s@pxl.be",
          roles: ["student"],
          teams: [2],
          isAuthenticated: true,
        }),
      ),
    );
    const teamB = {
      id: 2,
      name: "Team B",
      members: [{ userId: 10, name: "Student" }],
    };
    const teamA = { id: 1, name: "Team A", members: [] };
    projectServiceSpy.getTeams.and.returnValue(
      of(GetResult.success([teamB, teamA])),
    );

    component.ngOnInit();

    expect(component.teams).toEqual([teamA, teamB]);
    expect(component.myTeam).toEqual(teamB);
  });

  it("joinTeam, leaveMyTeam, deleteTeam, and modal openers should invoke services", () => {
    authServiceSpy.getUserProfile.and.returnValue(
      of(
        new UserProfile({
          id: 10,
          email: "s@pxl.be",
          roles: ["student"],
          teams: [2],
          isAuthenticated: true,
        }),
      ),
    );
    const team = {
      id: 2,
      name: "Team B",
      members: [{ userId: 10, name: "Student" }],
    };
    projectServiceSpy.getTeams.and.returnValue(of(GetResult.success([team])));
    component.ngOnInit();

    spyOn(window, "confirm").and.returnValue(true);

    projectServiceSpy.joinTeam.and.returnValue(of(PostResult.success()));
    component.joinTeam(2);
    expect(projectServiceSpy.joinTeam).toHaveBeenCalledWith(2, "P1", 2);
    expect(authServiceSpy.invalidateUserProfileCache).toHaveBeenCalled();

    projectServiceSpy.leaveTeam.and.returnValue(of(PostResult.success()));
    component.leaveMyTeam();
    expect(projectServiceSpy.leaveTeam).toHaveBeenCalledWith(2, "P1", 2);

    projectServiceSpy.deleteTeam.and.returnValue(of(PostResult.success()));
    component.deleteTeam(team);
    expect(projectServiceSpy.deleteTeam).toHaveBeenCalledWith(2, "P1", 2);

    component.openTeamEditModal(team);
    component.openTeamAddModal();
    expect(modalServiceSpy.show).toHaveBeenCalledTimes(2);
  });
});

