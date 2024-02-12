using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Guts.Client.Core.Models;
using NUnit.Framework;

namespace Guts.Client.Core.Utility
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

            TestContext.Progress.WriteLine("Sending data...");
            HttpResponseMessage? response = null;
            bool sendFailed = false;
            try
            {
                response = await _httpHandler.PostAsJsonAsync(webApiTestRunsUrl, testRun);
            }
            catch (Exception e)
            {
                //HACK: for some reason the post times out when the token isn't valid (expired) anymore
                TestContext.Error.WriteLine(e);
                sendFailed = true;
            }

            if (sendFailed || response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                TestContext.Progress.WriteLine("First try failed (unauthorized).");
                //retry with token retrieved remotely
                await RefreshAccessToken(allowCachedToken: false);
                response = await _httpHandler.PostAsJsonAsync(webApiTestRunsUrl, testRun);
            }

            var result = new Result(response!.IsSuccessStatusCode);
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
                TestContext.Progress.WriteLine("Retrieved authentication token from cache.");
            }

            if (string.IsNullOrEmpty(token))
            {
                TestContext.Progress.WriteLine("Retrieving an authentication token online...");
                token = await _authorizationHandler.RetrieveRemoteAccessTokenAsync();
                TestContext.Progress.WriteLine("Retrieved authentication token.");
            }

            _httpHandler.UseBearerToken(token);
        }
    }
}