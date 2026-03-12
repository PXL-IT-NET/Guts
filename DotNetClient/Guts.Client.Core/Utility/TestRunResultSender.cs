using Guts.Client.Core.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace Guts.Client.Core.Utility;

public class TestRunResultSender(IHttpHandler httpHandler, IAuthorizationHandler authorizationHandler, ITestOutputWriter outputWriter)
{
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

        outputWriter.WriteProgress("Sending data...");
        HttpResponseMessage? response = null;
        bool sendFailed = false;
        try
        {
            response = await httpHandler.PostAsJsonAsync(webApiTestRunsUrl, testRun);
        }
        catch (Exception e)
        {
            //HACK: for some reason the post times out when the token isn't valid (expired) anymore
            outputWriter.WriteError(e);
            sendFailed = true;
        }

        if (sendFailed || response?.StatusCode == HttpStatusCode.Unauthorized)
        {
            outputWriter.WriteProgress("First try failed (unauthorized).");
            //retry with token retrieved remotely
            await RefreshAccessToken(allowCachedToken: false);
            response = await httpHandler.PostAsJsonAsync(webApiTestRunsUrl, testRun);
        }

        //Expect status code 201 Created
        bool operationSucceeded = response?.StatusCode == HttpStatusCode.Created;
        if (operationSucceeded) return new Result(true);

        string errorMessage = await response!.Content.ReadAsStringAsync();

        // Check if this is a fake success response from firewall (PXL firewall rejection HTML page with status code 200 OK)
        if (response.IsSuccessStatusCode)
        {
            Match match = Regex.Match(errorMessage, @"support ID is:\s*(\d+)");
            if (match.Success)
            {
                string supportId = match.Groups[1].Value;
                errorMessage = $"Your request was blocked by the PXL firewall. Make sure you are sending from within the PXL network (VPN). Your support ID is: {supportId}.";
            }
        }

        return new Result(false) { Message = errorMessage };
    }

    private async Task RefreshAccessToken(bool allowCachedToken = true)
    {
        var token = string.Empty;

        if (allowCachedToken)
        {
            token = authorizationHandler.RetrieveLocalAccessToken();
            outputWriter.WriteProgress("Retrieved authentication token from cache.");
        }

        if (string.IsNullOrEmpty(token))
        {
            outputWriter.WriteProgress("Retrieving an authentication token online...");
            token = await authorizationHandler.RetrieveRemoteAccessTokenAsync();
            outputWriter.WriteProgress("Retrieved authentication token.");
        }

        httpHandler.UseBearerToken(token);
    }
}