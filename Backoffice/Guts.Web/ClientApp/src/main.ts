/// <reference types="@angular/localize" />

import { platformBrowser } from "@angular/platform-browser";

import { AppModule } from "./app/app.module";

export function getBaseUrl() {
  return document.getElementsByTagName("base")[0].href;
}

const providers = [{ provide: "BASE_URL", useFactory: getBaseUrl, deps: [] }];

platformBrowser(providers)
  .bootstrapModule(AppModule)
  .catch((err) => console.error(err));
