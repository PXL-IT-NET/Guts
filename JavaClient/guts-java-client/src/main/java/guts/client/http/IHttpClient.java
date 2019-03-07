package guts.client.http;

import org.apache.http.HttpResponse;

public interface IHttpClient {

    void useBearerToken(String token);

    HttpResponse post(String url, String data);

    HttpResponse post(String url, Object object);

    HttpResponse post(String url);

}
