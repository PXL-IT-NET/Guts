package be.pxl.guts.core.http;

/**
 * Api routes for Guts api endpoints
 */
public enum ApiRoutes {

    /**
     * Endpoint used for retrieving JWT token from api
     */
    AUTH_TOKEN("auth/token"),

    /**
     * Endpoint used for uploading test run results of exercises
     */
    TESTRUNS_FOREXERCISE("testruns/forexercise"),

    /**
     * Endpoint used for uploading test run results of projects
     */
    TESTRUNS_FORPROJECT("testruns/forproject");

    String apiPath;

    ApiRoutes(String apiPath) {
        this.apiPath = "api/" + apiPath;
    }

    /**
     * Get api endpoint
     * @return api endpoint
     */
    public String getApiPath() {
        return apiPath;
    }
}
