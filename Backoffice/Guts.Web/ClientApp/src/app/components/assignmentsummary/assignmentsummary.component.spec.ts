import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import {
  createActivatedRouteProvider,
  createRouterSpy,
} from "../../testing/test-helpers";
import { AssignmentSummaryComponent } from "./assignmentsummary.component";

describe("AssignmentSummaryComponent", () => {
  let component: AssignmentSummaryComponent;
  let fixture: ComponentFixture<AssignmentSummaryComponent>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    routerSpy = createRouterSpy();
    TestBed.overrideComponent(AssignmentSummaryComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [AssignmentSummaryComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider(),
        { provide: Router, useValue: routerSpy },
      ],
    });
    fixture = TestBed.createComponent(AssignmentSummaryComponent);
    component = fixture.componentInstance;
    component.courseId = 3;
    component.topicCode = "Topic1";
    component.model.assignmentId = 7;
  });

  it("navigateToDetail should navigate to chapter testresults when topicType is chapter", () => {
    component.topicType = "chapter";

    component.navigateToDetail();

    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ["courses", 3, "chapters", "Topic1", "testresults"],
      { queryParams: { assignmentId: 7 } },
    );
  });

  it("navigateToDetail should navigate to project testresults when topicType is project", () => {
    component.topicType = "project";

    component.navigateToDetail();

    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ["courses", 3, "projects", "Topic1", "testresults"],
      { queryParams: { assignmentId: 7 } },
    );
  });
});

