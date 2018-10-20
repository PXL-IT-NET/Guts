using System.Net;
using System.Threading.Tasks;
using Guts.Client.Shared.Models;

namespace Guts.Client.Shared.Utility
{
    public class TestRunResultSender
    {
        private readonly IHttpHandler _httpHandler;
        private readonly IAuthorizationHandler _authorizationHandler;

        public TestRunResultSender(IHttpHandler httpHandler, IAuthorizationHandler authorizationHandler)
        {
            _httpHandler = httpHandler;
            _authorizationHandler = authorizationHandler;
        }

        public async Task<bool> SendAsync(TestRunBase testRun)
        {
            await RefreshAccessToken();

            var webApiTestRunsUrl = "api/testruns";
            switch (testRun)
            {
                case ExerciseTestRun _:
                    webApiTestRunsUrl += "/forexercise";
                    break;
                case ProjectComponentTestRun _:
                    webApiTestRunsUrl += "/forproject";
                    break;
            }

            var response = await _httpHandler.PostAsJsonAsync(webApiTestRunsUrl, testRun);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                //retry with token retrieved remotely
                await RefreshAccessToken(allowCachedToken: false);
                response = await _httpHandler.PostAsJsonAsync(webApiTestRunsUrl, testRun);
            }

            return response.IsSuccessStatusCode;
        }

        private async Task RefreshAccessToken(bool allowCachedToken = true)
        {
            var token = string.Empty;

            if (allowCachedToken)
            {
                token = _authorizationHandler.RetrieveLocalAccessToken();
            }

            if (string.IsNullOrEmpty(token))
            {
                token = await _authorizationHandler.RetrieveRemoteAccessTokenAsync();
            }

            _httpHandler.UseBearerToken(token);
        }
    }
}