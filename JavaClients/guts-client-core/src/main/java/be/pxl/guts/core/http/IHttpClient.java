package be.pxl.guts.core.http;

import org.apache.http.HttpResponse;

/**
 * Interface for handling http connections to Guts api
 */
public interface IHttpClient {

    /**
     * Set the JWT bearer token to be used during communication
     * @param token token to use as bearer token
     */
    void useBearerToken(String token);

    /**
     * Post data to an api endpoint
     *
     * @param route route to post data
     * @param data data to post to route
     * @return response from api
     */
    HttpResponse post(ApiRoutes route, String data);

    /**
     * Post object as json to api endpoint
     *
     * @param route route to post data
     * @param object object tot post to route
     * @return response from api
     */
    HttpResponse post(ApiRoutes route, Object object);

}
