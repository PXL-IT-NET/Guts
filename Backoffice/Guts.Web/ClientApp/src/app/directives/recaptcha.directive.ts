import {
  Directive,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  Renderer2,
} from "@angular/core";

declare global {
  interface Window {
    grecaptcha: any;
    onRecaptchaLoad: () => void;
  }
}

@Directive({
  selector: "[appRecaptcha]",
  standalone: true,
})
export class RecaptchaDirective implements OnInit, OnDestroy {
  @Input() siteKey!: string;
  @Output() resolved = new EventEmitter<string>();

  private widgetId: number | null = null;
  private checkInterval: any;

  constructor(
    private el: ElementRef,
    private renderer: Renderer2,
  ) {}

  ngOnInit(): void {
    this.loadRecaptcha();
  }

  ngOnDestroy(): void {
    if (this.checkInterval) {
      clearInterval(this.checkInterval);
    }
  }

  private loadRecaptcha(): void {
    // Check if script is already loaded
    if (window.grecaptcha) {
      this.renderRecaptcha();
      return;
    }

    // Load script if not present
    const script = this.renderer.createElement("script");
    script.src =
      "https://www.google.com/recaptcha/api.js?onload=onRecaptchaLoad&render=explicit";
    script.async = true;
    script.defer = true;

    // Set up callback for when script loads
    window.onRecaptchaLoad = () => {
      this.renderRecaptcha();
    };

    this.renderer.appendChild(document.head, script);
  }

  private renderRecaptcha(): void {
    // Wait for grecaptcha to be fully ready
    this.checkInterval = setInterval(() => {
      if (window.grecaptcha && window.grecaptcha.render) {
        clearInterval(this.checkInterval);

        this.widgetId = window.grecaptcha.render(this.el.nativeElement, {
          sitekey: this.siteKey,
          callback: (response: string) => {
            this.resolved.emit(response);
          },
          "expired-callback": () => {
            this.resolved.emit("");
          },
          "error-callback": () => {
            this.resolved.emit("");
          },
        });
      }
    }, 100);
  }

  public reset(): void {
    if (window.grecaptcha && this.widgetId !== null) {
      window.grecaptcha.reset(this.widgetId);
    }
  }
}
