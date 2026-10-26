import { ComponentFixture, fakeAsync, TestBed, tick } from "@angular/core/testing";
import { FormsModule } from "@angular/forms";
import { NgbModule } from "@ng-bootstrap/ng-bootstrap";
import { Subject } from "rxjs";
import { TypeaheadComponent } from "./typeahead.component";
import { SimpleChange } from "@angular/core";

interface TestUser {
  id: number;
  fullName: string;
  email: string;
}

describe("TypeaheadComponent", () => {
  let component: TypeaheadComponent<TestUser>;
  let fixture: ComponentFixture<TypeaheadComponent<TestUser>>;

  const sampleUsers: TestUser[] = [
    { id: 1, fullName: "Alice Smith", email: "alice@test.com" },
    { id: 2, fullName: "Bob Jones", email: "bob@test.com" },
    { id: 3, fullName: "Charlie Brown", email: "charlie@test.com" },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TypeaheadComponent],
      imports: [FormsModule, NgbModule],
    }).compileComponents();

    fixture = TestBed.createComponent(TypeaheadComponent) as ComponentFixture<
      TypeaheadComponent<TestUser>
    >;
    component = fixture.componentInstance;
    component.items = [...sampleUsers];
    fixture.detectChanges();
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  describe("data and items input", () => {
    it("should set and get items via data alias", () => {
      component.data = sampleUsers;
      expect(component.items).toEqual(sampleUsers);
      expect(component.data).toEqual(sampleUsers);

      component.data = null as any;
      expect(component.items).toEqual([]);
    });

    it("should synchronize innerValue on ngOnChanges with selectedItem", () => {
      component.selectedItem = sampleUsers[0];
      component.ngOnChanges({
        selectedItem: new SimpleChange(null, sampleUsers[0], false),
      });
      expect(component.innerValue).toBe(sampleUsers[0]);
    });
  });

  describe("formatter", () => {
    it("should return empty string when item is null or undefined", () => {
      expect(component.formatter(null)).toBe("");
      expect(component.formatter(undefined)).toBe("");
    });

    it("should use formatItem when provided", () => {
      component.formatItem = (u) => `${u.id}: ${u.fullName}`;
      expect(component.formatter(sampleUsers[0])).toBe("1: Alice Smith");
    });

    it("should use displayProperty when formatItem is not provided", () => {
      component.displayProperty = "fullName";
      expect(component.formatter(sampleUsers[0])).toBe("Alice Smith");

      component.displayProperty = "email";
      expect(component.formatter(sampleUsers[0])).toBe("alice@test.com");
    });

    it("should format string items directly", () => {
      const stringComponent = TestBed.runInInjectionContext(() => new TypeaheadComponent<string>());
      expect(stringComponent.formatter("Hello World")).toBe("Hello World");
    });

    it("should fallback to custom toString if available and not default object string", () => {
      const customObj = {
        name: "Custom",
        toString: () => "Custom ToString",
      };
      const anyComponent = TestBed.runInInjectionContext(() => new TypeaheadComponent<any>());
      expect(anyComponent.formatter(customObj)).toBe("Custom ToString");

      const plainObj = { name: "Plain" };
      expect(anyComponent.formatter(plainObj)).toBe("");
    });
  });

  describe("search", () => {
    beforeEach(() => {
      component.typeaheadInstance = {
        isPopupOpen: () => false,
      } as any;
    });

    it("should return all items when term is empty or undefined", fakeAsync(() => {
      component.displayProperty = "fullName";
      const text$ = new Subject<string>();
      let results: TestUser[] = [];

      component.search(text$).subscribe((res) => (results = res));

      component.onClick();
      expect(results).toHaveSize(3);
      expect(results).toEqual(sampleUsers);
    }));

    it("should filter items by formatted text with debounce", fakeAsync(() => {
      component.displayProperty = "fullName";
      const text$ = new Subject<string>();
      let results: TestUser[] = [];

      component.search(text$).subscribe((res) => (results = res));

      text$.next("bob");
      tick(199);
      expect(results).toHaveSize(0);

      tick(1);
      expect(results).toHaveSize(1);
      expect(results[0].fullName).toBe("Bob Jones");

      text$.next("SMI");
      tick(200);
      expect(results).toHaveSize(1);
      expect(results[0].fullName).toBe("Alice Smith");
    }));

    it("should use searchPredicate when provided", fakeAsync(() => {
      component.searchPredicate = (user, term) =>
        user.email.toLowerCase().includes(term);

      const text$ = new Subject<string>();
      let results: TestUser[] = [];

      component.search(text$).subscribe((res) => (results = res));

      text$.next("charlie@test");
      tick(200);
      expect(results).toHaveSize(1);
      expect(results[0].fullName).toBe("Charlie Brown");
    }));

    it("should handle empty items array gracefully", fakeAsync(() => {
      component.items = [];
      const text$ = new Subject<string>();
      let results: TestUser[] = [];

      component.search(text$).subscribe((res) => (results = res));
      text$.next("test");
      tick(200);
      expect(results).toEqual([]);
    }));
  });

  describe("onClick", () => {
    it("should clear selection and emit null when clearOnClick is true", () => {
      spyOn(component.selectedItemChange, "emit");
      let changedValue: any = "unset";
      component.registerOnChange((val) => (changedValue = val));

      component.selectedItem = sampleUsers[0];
      component.innerValue = sampleUsers[0];
      component.clearOnClick = true;

      component.onClick();

      expect(component.selectedItem).toBeNull();
      expect(component.innerValue).toBeNull();
      expect(component.selectedItemChange.emit).toHaveBeenCalledWith(null);
      expect(changedValue).toBeNull();
    });

    it("should not clear selection when clearOnClick is false", () => {
      spyOn(component.selectedItemChange, "emit");
      component.selectedItem = sampleUsers[0];
      component.innerValue = sampleUsers[0];
      component.clearOnClick = false;

      component.onClick();

      expect(component.selectedItem).toBe(sampleUsers[0]);
      expect(component.selectedItemChange.emit).not.toHaveBeenCalled();
    });

    it("should do nothing when readonly or disabled", () => {
      spyOn(component.click$, "next");
      component.readonly = true;
      component.onClick();
      expect(component.click$.next).not.toHaveBeenCalled();

      component.readonly = false;
      component.disabled = true;
      component.onClick();
      expect(component.click$.next).not.toHaveBeenCalled();
    });
  });

  describe("selection and model change", () => {
    it("onSelectItem should update value and emit selectedItemChange and itemSelected", () => {
      spyOn(component.selectedItemChange, "emit");
      spyOn(component.itemSelected, "emit");
      let changedValue: any = null;
      component.registerOnChange((val) => (changedValue = val));

      component.onSelectItem({
        item: sampleUsers[1],
        preventDefault: () => {},
      });

      expect(component.selectedItem).toBe(sampleUsers[1]);
      expect(component.innerValue).toBe(sampleUsers[1]);
      expect(changedValue).toBe(sampleUsers[1]);
      expect(component.selectedItemChange.emit).toHaveBeenCalledWith(sampleUsers[1]);
      expect(component.itemSelected.emit).toHaveBeenCalledWith(sampleUsers[1]);
    });

    it("onModelChange should emit null when value is cleared", () => {
      spyOn(component.selectedItemChange, "emit");
      component.selectedItem = sampleUsers[0];

      component.onModelChange("");

      expect(component.selectedItem).toBeNull();
      expect(component.selectedItemChange.emit).toHaveBeenCalledWith(null);
    });

    it("onModelChange should update value when an object is set", () => {
      spyOn(component.selectedItemChange, "emit");

      component.onModelChange(sampleUsers[2]);

      expect(component.selectedItem).toBe(sampleUsers[2]);
      expect(component.selectedItemChange.emit).toHaveBeenCalledWith(sampleUsers[2]);
    });

    it("onModelChange should clear selectedItem when user types a non-matching string and allowCustomValue is false", () => {
      spyOn(component.selectedItemChange, "emit");
      component.selectedItem = sampleUsers[0];
      component.allowCustomValue = false;

      component.onModelChange("unmatched query");

      expect(component.selectedItem).toBeNull();
      expect(component.innerValue).toBe("unmatched query");
      expect(component.selectedItemChange.emit).toHaveBeenCalledWith(null);
    });

    it("onModelChange should allow matching string items when items are strings", () => {
      const stringComponent = TestBed.runInInjectionContext(() => new TypeaheadComponent<string>());
      stringComponent.items = ["Apple", "Banana"];
      spyOn(stringComponent.selectedItemChange, "emit");

      stringComponent.onModelChange("Banana");

      expect(stringComponent.selectedItem).toBe("Banana");
      expect(stringComponent.selectedItemChange.emit).toHaveBeenCalledWith("Banana");
    });

    it("onModelChange should accept custom string when allowCustomValue is true", () => {
      const anyComponent = TestBed.runInInjectionContext(() => new TypeaheadComponent<any>());
      anyComponent.allowCustomValue = true;
      spyOn(anyComponent.selectedItemChange, "emit");

      anyComponent.onModelChange("custom string");

      expect(anyComponent.selectedItem).toBe("custom string");
      expect(anyComponent.selectedItemChange.emit).toHaveBeenCalledWith("custom string");
    });
  });

  describe("ControlValueAccessor", () => {
    it("writeValue should set innerValue and selectedItem", () => {
      component.writeValue(sampleUsers[0]);
      expect(component.innerValue).toBe(sampleUsers[0]);
      expect(component.selectedItem).toBe(sampleUsers[0]);
    });

    it("onBlur should invoke onTouched callback", () => {
      let touched = false;
      component.registerOnTouched(() => (touched = true));

      component.onBlur();

      expect(touched).toBeTrue();
    });

    it("setDisabledState should update disabled flag", () => {
      component.setDisabledState!(true);
      expect(component.disabled).toBeTrue();

      component.setDisabledState!(false);
      expect(component.disabled).toBeFalse();
    });
  });
});

