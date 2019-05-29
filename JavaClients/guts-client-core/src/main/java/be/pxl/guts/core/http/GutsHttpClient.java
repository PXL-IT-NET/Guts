package be.pxl.guts.core.http;

import be.pxl.guts.core.config.IConfig;
import com.google.gson.Gson;
import org.apache.http.HttpHeaders;
import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.HttpClientBuilder;

import java.util.logging.Logger;

/**
 * Class for handling http connections to Guts api
 * implementation of {@link IHttpClient}
 */
public class GutsHttpClient implements IHttpClient {

    private IConfig config;
    private Logger logger;
    private HttpClient httpClient;

    private String token = null;

    /**
     * Construct a new GutsHttpClient
     *
     * @param config config containing api base url
     * @param logger logger used for logging
     */
    public GutsHttpClient(IConfig config, Logger logger) {
        this.config = config;
        this.logger = logger;
        this.httpClient = HttpClientBuilder.create().build();
    }

    @Override
    public void useBearerToken(String token) {
        this.token = token;
    }

    @Override
    public HttpResponse post(ApiRoutes route, String data) {
        try {
            HttpPost post = new HttpPost(this.config.getApiUrl() + route.getApiPath());
            post.setEntity(new StringEntity(data));
            post.setHeader(HttpHeaders.CONTENT_TYPE, "application/json");
            if(token != null && !token.isEmpty())
                post.setHeader(HttpHeaders.AUTHORIZATION, "Bearer " + token);

            return httpClient.execute(post);
        } catch (Exception e) {
            this.logger.severe("Failed to post to " + this.config.getApiUrl() + route.getApiPath());
            this.logger.severe(e.getMessage());
        }

        return null;
    }

    @Override
    public HttpResponse post(ApiRoutes route, Object object) {
        return post(route, new Gson().toJson(object));
    }

}
