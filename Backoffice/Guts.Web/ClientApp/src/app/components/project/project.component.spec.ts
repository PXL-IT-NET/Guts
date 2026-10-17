import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { ProjectService } from "../../services/project.service";
import {
  ActivatedRouteStub,
  createActivatedRouteProvider,
  createRouterSpy,
  createToastrSpy,
} from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import { ProjectComponent } from "./project.component";

describe("ProjectComponent", () => {
  let component: ProjectComponent;
  let fixture: ComponentFixture<ProjectComponent>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;
  let routeStub: ActivatedRouteStub;

  beforeEach(() => {
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "getProjectDetails",
    ]);
    toastrSpy = createToastrSpy();
    const routeProvider = createActivatedRouteProvider(
      { code: "P1" },
      { courseId: "3" },
      { assignmentId: "12" },
    );
    routeStub = routeProvider.useValue;

    TestBed.overrideComponent(ProjectComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [ProjectComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        routeProvider,
        { provide: Router, useValue: createRouterSpy() },
        { provide: ProjectService, useValue: projectServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ProjectComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("ngOnInit should load project details, select first team, and read assignmentId from queryParams", () => {
    projectServiceSpy.getProjectDetails.and.returnValue(
      of(
        GetResult.success({
          id: 5,
          code: "P1",
          description: "Project 1",
          components: [],
          teams: [{ id: 10, name: "Team 1" }],
          assignments: [],
        }),
      ),
    );

    component.ngOnInit();

    expect(projectServiceSpy.getProjectDetails).toHaveBeenCalledWith(3, "P1");
    expect(component.selectedTeamId).toBe(10);
    expect(component.selectedAssignmentId).toBe(12);

    routeStub.setQueryParams({});
    expect(component.selectedAssignmentId).toBe(0);
  });

  it("ngOnInit should show error toastr when getProjectDetails fails", () => {
    projectServiceSpy.getProjectDetails.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Err", value: null }),
    );

    component.ngOnInit();

    expect(toastrSpy.error).toHaveBeenCalled();
  });
});

