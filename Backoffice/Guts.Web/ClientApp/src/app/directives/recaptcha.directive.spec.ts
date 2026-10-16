import { ElementRef, Renderer2 } from "@angular/core";
import { fakeAsync, tick } from "@angular/core/testing";
import { RecaptchaDirective } from "./recaptcha.directive";

describe("RecaptchaDirective", () => {
  let directive: RecaptchaDirective;
  let elementRef: ElementRef;
  let rendererSpy: jasmine.SpyObj<Renderer2>;
  let originalGrecaptcha: any;

  beforeEach(() => {
    originalGrecaptcha = window.grecaptcha;
    delete (window as any).grecaptcha;

    elementRef = new ElementRef(document.createElement("div"));
    rendererSpy = jasmine.createSpyObj<Renderer2>("Renderer2", [
      "createElement",
      "appendChild",
    ]);
    rendererSpy.createElement.and.callFake(() =>
      document.createElement("script"),
    );

    directive = new RecaptchaDirective(elementRef, rendererSpy);
    directive.siteKey = "test-site-key";
  });

  afterEach(() => {
    directive.ngOnDestroy();
    window.grecaptcha = originalGrecaptcha;
  });

  it("should append recaptcha script to document.head when window.grecaptcha is not loaded", () => {
    directive.ngOnInit();

    expect(rendererSpy.createElement).toHaveBeenCalledWith("script");
    expect(rendererSpy.appendChild).toHaveBeenCalledWith(
      document.head,
      jasmine.any(HTMLScriptElement),
    );
    expect(typeof window.onRecaptchaLoad).toBe("function");
  });

  it("should render recaptcha and emit resolved tokens on callback, expired, and error", fakeAsync(() => {
    let capturedConfig: any;
    const resetSpy = jasmine.createSpy("reset");
    window.grecaptcha = {
      render: jasmine
        .createSpy("render")
        .and.callFake((_el: any, config: any) => {
          capturedConfig = config;
          return 7;
        }),
      reset: resetSpy,
    };

    const emittedTokens: string[] = [];
    directive.resolved.subscribe((t) => emittedTokens.push(t));

    directive.ngOnInit();
    tick(100);

    expect(window.grecaptcha.render).toHaveBeenCalledWith(
      elementRef.nativeElement,
      jasmine.objectContaining({ sitekey: "test-site-key" }),
    );

    capturedConfig.callback("valid-token");
    capturedConfig["expired-callback"]();
    capturedConfig["error-callback"]();

    expect(emittedTokens).toEqual(["valid-token", "", ""]);

    directive.reset();
    expect(resetSpy).toHaveBeenCalledWith(7);
  }));
});

