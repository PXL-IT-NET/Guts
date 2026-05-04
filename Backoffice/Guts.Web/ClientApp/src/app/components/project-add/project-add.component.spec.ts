import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BsModalRef } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";

import { ProjectAddComponent } from "./project-add.component";
import { ProjectService } from "src/app/services";

describe("ProjectAddComponent", () => {
  let component: ProjectAddComponent;
  let fixture: ComponentFixture<ProjectAddComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ProjectAddComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: BsModalRef, useValue: { hide: () => {} } },
        { provide: ProjectService, useValue: {} },
        { provide: ToastrService, useValue: {} },
      ],
    });
    fixture = TestBed.createComponent(ProjectAddComponent);
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
