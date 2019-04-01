package be.pxl.guts.core.auth;

import be.pxl.guts.core.config.IConfig;
import be.pxl.guts.core.http.IHttpClient;

import javax.annotation.Nonnull;
import java.io.IOException;
import java.nio.file.Files;
import java.util.logging.Logger;

/**
 * Abstract authorization handler implementing the generic methods of {@link IAuthorizationHandler}
 */
public abstract class AbstractAuthorizationHandler implements IAuthorizationHandler {

    protected Logger logger;
    protected IHttpClient httpClient;
    protected IConfig config;

    public AbstractAuthorizationHandler(Logger logger, IHttpClient httpClient, IConfig config)
    {
        this.logger = logger;
        this.httpClient = httpClient;
        this.config = config;
    }

    @Override
    public String retrieveLocalAccessToken() {
        if(!Files.exists(LOCAL_TOKEN_FILE))
            return "";

        try {
            return Files.readString(LOCAL_TOKEN_FILE);
        } catch (IOException e) {
            this.logger.warning("Unable to read local token from file.");
        }

        return "";
    }

    @Override
    public void storeTokenLocally(@Nonnull String token) {
        try {
            if(!Files.exists(LOCAL_TOKEN_DIRECTORY))
                Files.createDirectory(LOCAL_TOKEN_DIRECTORY);

            Files.writeString(LOCAL_TOKEN_FILE, token);
        } catch (IOException e) {
            this.logger.warning("Failed to write token to file " + LOCAL_TOKEN_FILE.toAbsolutePath());
            this.logger.warning(e.getLocalizedMessage());
        }
    }

}