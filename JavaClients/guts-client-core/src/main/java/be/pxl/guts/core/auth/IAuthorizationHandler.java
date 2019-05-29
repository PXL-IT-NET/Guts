package be.pxl.guts.core.auth;

import java.io.File;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Interface for authorization handler used by guts java clients
 * Used to retrieve and save JWT token
 */
public interface IAuthorizationHandler {

    Path LOCAL_TOKEN_DIRECTORY = Paths.get(System.getProperty("user.home") + File.separator + ".guts");
    Path LOCAL_TOKEN_FILE = Paths.get(LOCAL_TOKEN_DIRECTORY.toAbsolutePath() + File.separator + "_cache.txt");

    /**
     * Retrieve a JWT token from local storage
     *
     * Local storage location: {@link IAuthorizationHandler#LOCAL_TOKEN_FILE}
     *
     * @return JWT token (empty if no local token)
     */
    String retrieveLocalAccessToken();

    /**
     * Retrieve a JWT token from remote (login via api)
     * @return JWT token (empty if unable to retrieve token)
     */
    String retrieveRemoteAccessToken();

    /**
     * Store a JWT token on local computer.
     *
     * Local storage location: {@link IAuthorizationHandler#LOCAL_TOKEN_FILE}
     *
     * @param token JWT token to save
     */
    void storeTokenLocally(String token);

}
