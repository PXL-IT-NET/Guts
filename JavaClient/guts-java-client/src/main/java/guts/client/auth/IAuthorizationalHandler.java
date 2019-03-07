package guts.client.auth;

public interface IAuthorizationalHandler {

    String retrieveLocalAccessToken();

    String retrieveRemoteAccessToken();

    void storeTokenLocally(String token);

}
