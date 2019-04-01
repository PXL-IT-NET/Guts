package be.pxl.guts.core.auth.swing;

import be.pxl.guts.core.auth.AbstractAuthorizationHandler;
import be.pxl.guts.core.config.IConfig;
import be.pxl.guts.core.http.IHttpClient;

import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;
import java.util.logging.Logger;

// TODO handle close without login

/**
 * Authorization handler implementation using Java Swing
 *
 * Used to retrieve JWT token from remote or local storage
 */
public class SwingAuthorizationHandler extends AbstractAuthorizationHandler {

    private final Object lock = new Object();
    private SwingLoginWindow loginWindow;

    public SwingAuthorizationHandler(Logger logger, IHttpClient httpClient, IConfig config) {
        super(logger, httpClient, config);
    }

    @Override
    public String retrieveRemoteAccessToken() {
        loginWindow = new SwingLoginWindow(this.logger, this.httpClient, this.config);

        Thread waitingThread = new Thread(() -> {
            synchronized (lock) {
                while(loginWindow.isVisible()) {
                    try {
                        lock.wait();
                    } catch (InterruptedException e) {
                        this.logger.warning("Failed locking thread!");
                        this.logger.warning(e.getMessage());
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
            this.logger.warning("Failed joining thread!");
            this.logger.warning(e.getMessage());
        }

        String token = loginWindow.getToken();
        storeTokenLocally(token);
        return token;
    }

}