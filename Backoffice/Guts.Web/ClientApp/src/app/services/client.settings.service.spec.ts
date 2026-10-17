import { environment } from "../../environments/environment";
import { ClientSettingsService } from "./client.settings.service";

describe("ClientSettingsService", () => {
  it("get should return ClientSettings with environment.apiBaseUrl", () => {
    const service = new ClientSettingsService("https://localhost/");
    let apiBaseUrl = "";

    service.get().subscribe((settings) => {
      apiBaseUrl = settings.apiBaseUrl;
    });

    expect(apiBaseUrl).toBe(environment.apiBaseUrl);
  });
});

