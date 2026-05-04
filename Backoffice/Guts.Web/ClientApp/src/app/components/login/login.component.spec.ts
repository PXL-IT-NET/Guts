import { waitForAsync, ComponentFixture, TestBed } from "@angular/core/testing";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { of } from "rxjs";
import { LoginComponent } from "./login.component";
import { AuthService } from "src/app/services";

describe("Login component", () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [LoginComponent],
      imports: [FormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            queryParams: of({}),
            params: of({}),
            snapshot: { params: {} },
            parent: { snapshot: { params: {} } },
          },
        },
        { provide: Router, useValue: { navigate: () => {} } },
        {
          provide: AuthService,
          useValue: {
            logout: () => {},
            login: () => of({ success: true }),
            cancelLoginSession: () => of({ success: true }),
          },
        },
      ],
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
  });

  it("Should do something", waitForAsync(() => {
    expect(true).toBeTruthy();
  }));
});
