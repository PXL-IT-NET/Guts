using Guts.Client.Shared.Utility;
using Microsoft.Extensions.Configuration;

namespace Guts.Client.Core
{
    public class LoginWindowFactory : ILoginWindowFactory
    {
        public ILoginWindow Create()
        {
            var gutsSettingsConfig = new ConfigurationBuilder()
                .AddJsonFile("gutssettings.json", optional: false, reloadOnChange: false)
                .Build();

            var gutsSection = gutsSettingsConfig.GetSection("Guts");
            var apiBaseUrl = gutsSection.GetValue<string>("apiBaseUrl");
            var webAppBaseUrl = gutsSection.GetValue<string>("webAppBaseUrl");

            return new LoginWindow(new GuidSessionIdGenerator(), apiBaseUrl, webAppBaseUrl);
        }
    }
}