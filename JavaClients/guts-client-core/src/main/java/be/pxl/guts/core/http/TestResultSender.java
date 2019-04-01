package be.pxl.guts.core.http;

import be.pxl.guts.core.auth.IAuthorizationHandler;
import be.pxl.guts.core.config.IConfig;
import be.pxl.guts.core.enums.TestRunType;
import be.pxl.guts.core.models.AssignmentTestRun;
import com.google.gson.Gson;
import org.apache.http.HttpResponse;
import org.apache.http.HttpStatus;
import org.apache.http.util.EntityUtils;

import java.io.IOException;
import java.util.logging.Logger;

public class TestResultSender {

    private Logger logger;
    private IConfig config;
    private IHttpClient httpClient;
    private IAuthorizationHandler authorizationHandler;

    public TestResultSender(Logger logger, IConfig config, IHttpClient httpClient, IAuthorizationHandler authorizationHandler) {
        this.logger = logger;
        this.config = config;
        this.httpClient = httpClient;
        this.authorizationHandler = authorizationHandler;
    }

    public ApiResult send(AssignmentTestRun testRun, TestRunType runType) {
        httpClient.useBearerToken(getToken());
        HttpResponse response = httpClient.post(runType.getApiEndPoint(), testRun);

        if(response == null) {
            return new ApiResult(-1, false, "Response was null");
        }

        if(response.getStatusLine().getStatusCode() == HttpStatus.SC_UNAUTHORIZED) {
            // retry with token retrieved remotely
            httpClient.useBearerToken(getToken(false));
            response = httpClient.post(runType.getApiEndPoint(), testRun);
        }

        if(response == null) {
            return new ApiResult(-1, false, "Response was null");
        }

        ApiResult result = new ApiResult(response.getStatusLine().getStatusCode(), response.getStatusLine().getStatusCode() == HttpStatus.SC_CREATED);
        if(result.isSuccessful()) {
            try {
                result.setMessage(EntityUtils.toString(response.getEntity()));
            } catch (IOException e) {
                result.setMessage(null);
                this.logger.warning("Failed to parse response from api!");
            }
        } else {
            if(response.getEntity() != null) {
                try {
                    result.setMessage(EntityUtils.toString(response.getEntity()));
                } catch (IOException e) {
                    result.setMessage(null);
                    this.logger.warning("Failed to parse response from api!");
                }
            }
        }

        return result;
    }

    private String getToken() {
        return getToken(true);
    }

    private String getToken(boolean allowedCachedToken) {
        String token = "";
        if(allowedCachedToken) {
            token = authorizationHandler.retrieveLocalAccessToken();
        }

        if(token == null || token.isEmpty()) {
            token = authorizationHandler.retrieveRemoteAccessToken();
        }

        return token;
    }

}
