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

        public async Task<Result> SendAsync(AssignmentTestRun testRun, TestRunType type)
        {
            await RefreshAccessToken();

            var webApiTestRunsUrl = "api/testruns";
            switch (type)
            {
                case TestRunType.ForExercise:
                    webApiTestRunsUrl += "/forexercise";
                    break;
                case TestRunType.ForProject:
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

            var result = new Result(response.IsSuccessStatusCode);
            if (!result.Success)
            {
                result.Message = await response.Content.ReadAsStringAsync();
            }

            return result;
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