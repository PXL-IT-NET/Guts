package guts.client.http;

import com.google.gson.Gson;
import org.apache.http.HttpHeaders;
import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.HttpClientBuilder;
import guts.client.Configuration;
import guts.client.GutsJUnit5;

public class GutsHttpClient implements IHttpClient {

    private String token = null;
    private HttpClient httpClient;

    public GutsHttpClient() {
        httpClient = HttpClientBuilder.create().build();
    }

    @Override
    public void useBearerToken(String token) {
        this.token = token;
    }

    @Override
    public HttpResponse post(String url, String data) {
        try {
            HttpPost post = new HttpPost(Configuration.getBaseUrl() + url);
            post.setEntity(new StringEntity(data));
            post.setHeader(HttpHeaders.CONTENT_TYPE, "application/json");
            if(token != null && !token.isEmpty())
                post.setHeader(HttpHeaders.AUTHORIZATION, "Bearer " + token);

            return httpClient.execute(post);
        } catch (Exception e) {
            GutsJUnit5.getLogger().severe("Failed to post to " + url);
            GutsJUnit5.getLogger().severe(e.getMessage());
        }

        return null;
    }

    @Override
    public HttpResponse post(String url, Object object) {
        return post(url, new Gson().toJson(object));
    }

    @Override
    public HttpResponse post(String url) {
        return post(url, "");
    }

}
