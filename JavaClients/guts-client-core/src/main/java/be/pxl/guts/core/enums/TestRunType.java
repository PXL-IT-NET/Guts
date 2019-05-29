package be.pxl.guts.core.enums;

import be.pxl.guts.core.http.ApiRoutes;

/**
 * Types of test runs
 */
public enum TestRunType {

    /**
     * For exercises
     * Endpoint: {@link be.pxl.guts.core.http.ApiRoutes#TESTRUNS_FOREXERCISE}
     */
    FOR_EXERCISES(ApiRoutes.TESTRUNS_FOREXERCISE),

    /**
     * For project
     * Endpoint: {@link be.pxl.guts.core.http.ApiRoutes#TESTRUNS_FORPROJECT}
     */
    FOR_PROJECT(ApiRoutes.TESTRUNS_FORPROJECT);

    private ApiRoutes apiEndPoint;

    TestRunType(ApiRoutes apiEndPoint) {
        this.apiEndPoint = apiEndPoint;
    }

    /**
     * Get {@link ApiRoutes} for test run type
     * @return {@link ApiRoutes} of type
     */
    public ApiRoutes getApiEndPoint() {
        return apiEndPoint;
    }

}
