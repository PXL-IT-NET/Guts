package guts.client.auth.swing;

import com.google.gson.Gson;
import com.google.gson.JsonObject;
import guts.client.models.Credentials;
import org.apache.http.HttpResponse;
import org.apache.http.HttpStatus;
import org.apache.http.util.EntityUtils;
import guts.client.Configuration;
import guts.client.GutsJUnit5;
import guts.client.http.IHttpClient;

import javax.swing.*;
import java.awt.*;
import java.awt.event.WindowEvent;
import java.io.IOException;
import java.net.URI;

public class SwingLoginWindow extends JFrame {

    private JTextField emailField;
    private JPasswordField passwordField;
    private JButton loginButton;
    private JButton registerButton;
    private JLabel errorLabel;

    private IHttpClient httpClient;
    private String token = "";

    public SwingLoginWindow(IHttpClient httpClient) {
        super("GUTS Login");
        this.httpClient = httpClient;

        setSize(525, 350);
        setAlwaysOnTop(true);
        setResizable(false);
        setLocationRelativeTo(null);

        createView();

        // pack();
        setVisible(true);
    }

    public void createView() {
        JPanel layout = new JPanel(new BorderLayout());
        layout.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 10));
        getContentPane().add(layout);

        // -- header
        JPanel header = new JPanel();
        header.setBorder(BorderFactory.createEmptyBorder(0, 0, 5, 0));
        layout.add(header, BorderLayout.NORTH);

        header.add(new JLabel("Add with guts details"));

        // -- form
        JPanel form = new JPanel(new GridBagLayout());
        layout.add(form, BorderLayout.CENTER);

        GridBagConstraints c = new GridBagConstraints();
        c.gridx = 0;
        c.gridy = 0;
        c.anchor = GridBagConstraints.LINE_END;
        form.add(new JLabel("Email: "), c);
        c.gridy++;
        form.add(new JLabel("Password: "), c);

        c.gridy++;
        registerButton = new JButton();
        registerButton.setText("<HTML><FONT color=\"#000099\"><U>Register account</U></FONT></HTML>");
        registerButton.setHorizontalAlignment(SwingConstants.LEFT);
        registerButton.setBorder(null);
        registerButton.setBorderPainted(false);
        registerButton.setOpaque(false);
        registerButton.setBackground(Color.WHITE);
        registerButton.setToolTipText(Configuration.getRegisterUrl());
        registerButton.addActionListener(e -> openRegisterUrl());
        form.add(registerButton, c);

        c.gridx = 1;
        c.gridy = 0;
        c.anchor = GridBagConstraints.LINE_START;
        form.add(emailField = new JTextField(20), c);
        c.gridy++;
        form.add(passwordField = new JPasswordField(20), c);

        c.gridy++;
        c.anchor = GridBagConstraints.LINE_END;
        form.add(loginButton = new JButton("Login"), c);

        // -- footer / error panel
        JPanel errorPanel = new JPanel();
        errorPanel.setBorder(BorderFactory.createEmptyBorder(5, 0, 7, 0));
        layout.add(errorPanel, BorderLayout.SOUTH);
        errorPanel.add(errorLabel = new JLabel(""), c);

        passwordField.addActionListener(e -> loginButtonPressed()); // on enter in password field
        loginButton.addActionListener(e -> loginButtonPressed());
    }

    private void openRegisterUrl() {
        if(Desktop.isDesktopSupported()) {
            String url = Configuration.getRegisterUrl();
            try {
                Desktop.getDesktop().browse(new URI(url));
            } catch (Exception e) {
                GutsJUnit5.getLogger().severe("Unable to open web page: " + url);
            }
        }
    }

    private void loginButtonPressed() {
        loginButton.setEnabled(false);

        Credentials credentials = new Credentials(emailField.getText(), new String(passwordField.getPassword()));

        HttpResponse response = httpClient.post( "api/auth/token", credentials);
        if(response != null) {
            if(response.getStatusLine().getStatusCode() == HttpStatus.SC_OK) {
                try {
                    String json = EntityUtils.toString(response.getEntity());
                    JsonObject obj = new Gson().fromJson(json, JsonObject.class);
                    token = obj.get("token").getAsString();

                    if(token != null && !token.isEmpty()) {
                        dispatchEvent(new WindowEvent(this, WindowEvent.WINDOW_CLOSING));
                        return;
                    }
                } catch (IOException e) {
                    GutsJUnit5.getLogger().severe("Unable to get token from response: " + response);
                    // GutsJUnit5.getLogger().severe(e.getMessage());
                    errorLabel.setText("Failed to get token from http response");
                }
            } else {
                if(response.getStatusLine().getStatusCode() == HttpStatus.SC_UNAUTHORIZED)
                    errorLabel.setText("Wrong username/password combination");
            }
        } else {
            GutsJUnit5.getLogger().severe("Unable to get token from response: " + response);
            errorLabel.setText("Unable to request token");
        }

        // reset login form
        passwordField.setText("");
        loginButton.setEnabled(true);
    }

    public String getToken() {
        return token;
    }

}
