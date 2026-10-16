import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, fakeAsync, TestBed, tick } from "@angular/core/testing";
import { ToastrService } from "ngx-toastr";
import { of, Subject } from "rxjs";
import { ChapterService } from "../../services/chapter.service";
import {
  ActivatedRouteStub,
  createActivatedRouteProvider,
  createToastrSpy,
} from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import { ChapterComponent } from "./chapter.component";

describe("ChapterComponent", () => {
  let component: ChapterComponent;
  let fixture: ComponentFixture<ChapterComponent>;
  let chapterServiceSpy: jasmine.SpyObj<ChapterService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;
  let routeStub: ActivatedRouteStub;

  beforeEach(() => {
    chapterServiceSpy = jasmine.createSpyObj<ChapterService>("ChapterService", [
      "getChapterDetails",
    ]);
    chapterServiceSpy.getChapterDetails.and.returnValue(
      of(
        GetResult.success({
          id: 10,
          code: "Ch1",
          description: "Chapter 1",
          exercises: [],
          users: [],
          assignments: [],
        }),
      ),
    );
    toastrSpy = createToastrSpy();
    const routeProvider = createActivatedRouteProvider(
      { chapterCode: "Ch1" },
      { courseId: "2" },
      { assignmentId: "9" },
    );
    routeStub = routeProvider.useValue;

    TestBed.overrideComponent(ChapterComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [ChapterComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        routeProvider,
        { provide: ChapterService, useValue: chapterServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ChapterComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("ngOnInit should load chapter details and set selectedUser and selectedAssignmentId", () => {
    const users = [
      { id: 1, fullName: "Alice" },
      { id: 2, fullName: "Bob" },
    ];
    chapterServiceSpy.getChapterDetails.and.returnValue(
      of(
        GetResult.success({
          id: 10,
          code: "Ch1",
          description: "Chapter 1",
          exercises: [],
          users,
          assignments: [],
        }),
      ),
    );

    component.ngOnInit();

    expect(chapterServiceSpy.getChapterDetails).toHaveBeenCalledWith(2, "Ch1");
    expect(component.selectedUser).toEqual(users[0]);
    expect(component.selectedAssignmentId).toBe(9);

    routeStub.setQueryParams({});
    expect(component.selectedAssignmentId).toBe(0);
  });

  it("ngOnInit should display error toastr when getChapterDetails fails", () => {
    chapterServiceSpy.getChapterDetails.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Load error", value: null }),
    );

    component.ngOnInit();

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Could not load chapter details from API. Message: Load error",
      "System error",
    );
  });

  it("searchUsers, onUserClick, and formatUser should filter and format users", fakeAsync(() => {
    component.model.users = [
      { id: 1, fullName: "Alice Smith" },
      { id: 2, fullName: "Bob Jones" },
    ];
    component.userTypeAheadInstance = { isPopupOpen: () => false } as any;

    const text$ = new Subject<string>();
    let filtered: any[] = [];
    component.searchUsers(text$).subscribe((res) => (filtered = res));

    component.onUserClick();
    expect(component.selectedUser).toBeNull();
    expect(filtered.length).toBe(2);

    text$.next("bob");
    tick(200);
    expect(filtered).toEqual([{ id: 2, fullName: "Bob Jones" }]);

    expect(component.formatUser({ id: 1, fullName: "Alice Smith" })).toBe(
      "Alice Smith",
    );
  }));
});

