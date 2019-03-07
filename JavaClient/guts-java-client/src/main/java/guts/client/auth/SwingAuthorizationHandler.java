package guts.client.auth;

import guts.client.GutsJUnit5;
import guts.client.auth.swing.SwingLoginWindow;
import guts.client.http.IHttpClient;

import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

public class SwingAuthorizationHandler implements IAuthorizationalHandler {

    private static final Path LOCAL_TOKEN_DIRECTORY = Path.of(System.getProperty("user.home") + File.separator + ".guts");
    private static final Path LOCAL_TOKEN_FILE = Path.of(LOCAL_TOKEN_DIRECTORY.toAbsolutePath() + File.separator + "_cache.txt");

    private Object lock = new Object();
    private SwingLoginWindow loginWindow;
    private IHttpClient httpClient;

    public SwingAuthorizationHandler(IHttpClient httpClient) {
        this.httpClient = httpClient;
    }

    @Override
    public String retrieveLocalAccessToken() {
        if(!Files.exists(LOCAL_TOKEN_FILE))
            return "";

        try {
            return Files.readString(LOCAL_TOKEN_FILE);
        } catch (IOException e) {
            GutsJUnit5.getLogger().warning("Unable to read local token from file.");
        }

        return "";
    }

    @Override
    public String retrieveRemoteAccessToken() {
        loginWindow = new SwingLoginWindow(httpClient);

        Thread waitingThread = new Thread(() -> {
            synchronized (lock) {
                while(loginWindow.isVisible()) {
                    try {
                        lock.wait();
                    } catch (InterruptedException e) {
                        GutsJUnit5.getLogger().warning("Failed locking thread!");
                        GutsJUnit5.getLogger().warning(e.getMessage());
                    }
                }
            }
        });
        waitingThread.start();

        loginWindow.addWindowListener(new WindowAdapter() {
            @Override
            public void windowClosing(WindowEvent e) {
                loginWindow.setVisible(false);
                synchronized (lock) {
                    lock.notify();
                }
            }
        });

        try {
            waitingThread.join();
        } catch (InterruptedException e) {
            GutsJUnit5.getLogger().warning("Failed joining thread!");
            GutsJUnit5.getLogger().warning(e.getMessage());
        }

        String token = loginWindow.getToken();
        storeTokenLocally(token);
        return token;
    }

    @Override
    public void storeTokenLocally(String token) {
        try {
            if(!Files.exists(LOCAL_TOKEN_DIRECTORY))
                Files.createDirectory(LOCAL_TOKEN_DIRECTORY);

            Files.writeString(LOCAL_TOKEN_FILE, token);
        } catch (IOException e) {
            GutsJUnit5.getLogger().warning("Failed to write token to file " + LOCAL_TOKEN_FILE.toAbsolutePath());
            GutsJUnit5.getLogger().warning(e.getLocalizedMessage());
            e.printStackTrace(); // TODO remove
        }
    }

}
