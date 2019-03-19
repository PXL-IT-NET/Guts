package guts.client.auth;

import guts.client.http.GutsHttpClient;
import guts.client.http.IHttpClient;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

import static org.junit.jupiter.api.Assertions.*;

class AuthorizationHandlerTest {

    private static final Path LOCAL_TOKEN_DIRECTORY = Path.of(System.getProperty("user.home") + File.separator + ".guts");
    private static final Path LOCAL_TOKEN_FILE = Path.of(LOCAL_TOKEN_DIRECTORY.toAbsolutePath() + File.separator + "_cache.txt");
    private static final String TEST_TOKEN = "token test";

    private static IAuthorizationalHandler authorizationalHandler;

    @BeforeAll
    static void setup() {
        IHttpClient httpClient = new GutsHttpClient();
        authorizationalHandler = new SwingAuthorizationHandler(httpClient);
    }

    @Test
    void checkIfTokenCanStoreLocally() {
        if(Files.exists(LOCAL_TOKEN_DIRECTORY)) {
            try {
                Files.delete(LOCAL_TOKEN_DIRECTORY);
            } catch (IOException e) {
                e.printStackTrace();
            }
        }

        authorizationalHandler.storeTokenLocally(TEST_TOKEN);

        boolean result = false;

        assertTrue(Files.exists(LOCAL_TOKEN_FILE), "Token file should exists, but does not!");
    }

    @Test
    void checkIfLocalTokenCanBeRetrieved() {
        String result = authorizationalHandler.retrieveLocalAccessToken();
        assertEquals(TEST_TOKEN, result, "Token from local file should match!");
    }

    @Test
    void checkIfRemoteTokenCanBeRetrieved() {
        String result = authorizationalHandler.retrieveRemoteAccessToken();
        assertNotEquals("", result, "Token was not loaded correctly");
    }
}