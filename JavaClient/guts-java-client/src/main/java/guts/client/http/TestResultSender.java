package guts.client.http;

import guts.client.enums.TestRunType;
import guts.client.models.AssignmentTestRun;
import guts.client.models.Result;
import org.apache.http.HttpResponse;
import org.apache.http.HttpStatus;
import org.apache.http.util.EntityUtils;
import guts.client.Configuration;
import guts.client.GutsJUnit5;
import guts.client.auth.IAuthorizationalHandler;
import guts.client.auth.SwingAuthorizationHandler;

import java.io.IOException;

public class TestResultSender {

    private IHttpClient httpClient;
    private IAuthorizationalHandler authorizationalHandler;

    public TestResultSender(IHttpClient httpClient) {
        this.httpClient = httpClient;
        this.authorizationalHandler = new SwingAuthorizationHandler(httpClient);
    }

    public Result send(AssignmentTestRun testRun, TestRunType type) {
        String webApiTestRunsUrl = "api/testruns";
        switch (type) {
            case ForExercises:
                webApiTestRunsUrl += "/forexercise";
                break;
            case ForProject:
                webApiTestRunsUrl += "/forproject";
                break;
        }

        httpClient.useBearerToken(getToken());
        HttpResponse response = httpClient.post(webApiTestRunsUrl, testRun);

        if(response == null) {
            Result result = new Result(false);
            result.setMessage("Could not connect to " + Configuration.getBaseUrl() + webApiTestRunsUrl);
            return result;
        }

        if(response.getStatusLine().getStatusCode() == HttpStatus.SC_UNAUTHORIZED) {
            // retry with token retrieved remotely
            httpClient.useBearerToken(getToken(false));
            response = httpClient.post(webApiTestRunsUrl, testRun);
        }

        Result result = new Result(response.getStatusLine().getStatusCode() == HttpStatus.SC_CREATED);
        if(result.isSuccessful()) {
            try {
                result.setMessage(EntityUtils.toString(response.getEntity()));
            } catch (IOException e) {
                result.setMessage(null);
                GutsJUnit5.getLogger().warning("Failed to parse response from api!");
            }
        } else {
            if(response.getEntity() != null) {
                try {
                    result.setMessage(EntityUtils.toString(response.getEntity()));
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
            // result.setMessage("code: " + response.getStatusLine().getStatusCode() + "\nReasonPhrase: " + response.getStatusLine().getReasonPhrase());
        }


        return result;
    }

    public String getToken() {
        return getToken(true);
    }

    public String getToken(boolean allowedCachedToken) {
        String token = "";
        if(allowedCachedToken) {
            token = authorizationalHandler.retrieveLocalAccessToken();
        }

        if(token == null || token.isEmpty()) {
            token = authorizationalHandler.retrieveRemoteAccessToken();
        }

        return token;
    }

}
