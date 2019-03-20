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

import javax.imageio.ImageIO;
import javax.swing.*;
import java.awt.*;
import java.awt.event.WindowEvent;
import java.awt.image.BufferedImage;
import java.io.IOException;
import java.net.URI;

public class SwingLoginWindow extends JFrame {

    private JTextField emailField;
    private JPasswordField passwordField;
    private Button loginButton;
    private Button forgotPasswordButton;
    private JLabel errorLabel;

    private IHttpClient httpClient;
    private String token = "";

    public SwingLoginWindow(IHttpClient httpClient) {
        super("GUTS Login");
        this.httpClient = httpClient;

        setAlwaysOnTop(true);
        setResizable(false);
        setLocationRelativeTo(null);

        createView();

        pack();
        setVisible(true);
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);

        emailField.requestFocus();
    }

    public void createView() {
        JPanel windowPanel = new JPanel(new BorderLayout());
        getContentPane().add(windowPanel);

        JPanel layout = new JPanel();
        windowPanel.add(layout, BorderLayout.NORTH);

        // 10 pixel padding
        layout.setBorder(BorderFactory.createEmptyBorder(15, 25, 5, 25));

        // set layout to box layout
        layout.setLayout(new GridBagLayout());

        GridBagConstraints constraints = new GridBagConstraints();

        constraints.gridx = 0;
        constraints.gridy = 0;
        constraints.fill = GridBagConstraints.HORIZONTAL;

        // -- header - image
        LogoPanel logoPanel = new LogoPanel();
        layout.add(logoPanel, constraints);

        constraints.gridx = 1;
        constraints.insets = new Insets(0, 15, 0, 0);
        // -- header - text
        TextPanel textPanel = new TextPanel();
        layout.add(textPanel, constraints);

        // -- form
        constraints.insets = new Insets(15, 0, 0, 0);
        constraints.gridx = 0;
        constraints.gridy = 1;
        constraints.ipadx = 0;
        constraints.ipady = 0;
        layout.add(new JLabel("Email"), constraints);

        constraints.insets = new Insets(15, 15, 0, 0);
        constraints.gridx = 1;
        constraints.gridy = 1;
        layout.add(emailField = new JTextField(), constraints);

        constraints.insets = new Insets(0, 0, 0, 0);
        constraints.gridx = 0;
        constraints.gridy = 2;
        layout.add(new JLabel("Password"), constraints);

        constraints.insets = new Insets(0, 15, 0, 0);
        constraints.gridx = 1;
        layout.add(passwordField = new JPasswordField(), constraints);

        constraints.insets = new Insets(15, 0, 0, 0);
        constraints.gridy = 3;

        // -- login buttons
        JPanel buttonPanel = new JPanel();
        layout.add(buttonPanel, constraints);

        buttonPanel.setLayout(new BoxLayout(buttonPanel, BoxLayout.LINE_AXIS));
        buttonPanel.add(Box.createHorizontalGlue());
        buttonPanel.add(forgotPasswordButton = new Button("Forgot password"));
        buttonPanel.add(Box.createRigidArea(new Dimension(10, 0)));
        buttonPanel.add(loginButton = new Button("Login"));

        // -- error label
        JPanel errorPanel = new JPanel();
        windowPanel.add(errorPanel, BorderLayout.SOUTH);
        errorPanel.add(errorLabel = new JLabel());

        passwordField.addActionListener(e -> loginButtonPressed()); // on enter in password field
        loginButton.addActionListener(e -> loginButtonPressed());
        forgotPasswordButton.addActionListener(e -> open(Configuration.getWebUrl() + "forgotpassword"));
    }

    private void open(String url) {
        if (Desktop.isDesktopSupported()) {
            try {
                Desktop.getDesktop().browse(new URI(url));
            } catch (Exception e) {
                GutsJUnit5.getLogger().severe("Failed to open web browser! Please go to '" + url + "' manually");
            }
        } else {
            GutsJUnit5.getLogger().severe("Opening web browser is not supported on your system. Please go to '" + url + "' manually");
        }
    }

    private void loginButtonPressed() {
        loginButton.setEnabled(false);

        if(emailField.getText().isEmpty() || new String(passwordField.getPassword()).isEmpty()) {
            showError("Please enter your credentials!");
            loginButton.setEnabled(true);
            return;
        }

        Credentials credentials = new Credentials(emailField.getText(), new String(passwordField.getPassword()));

        HttpResponse response = httpClient.post( Configuration.getBaseUrl() + "api/auth/token", credentials);
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
                    showError("Failed to get token from response!");
                }
            } else {
                if(response.getStatusLine().getStatusCode() == HttpStatus.SC_UNAUTHORIZED)
                    showError("Wrong username/password combination...");
            }
        } else {
            GutsJUnit5.getLogger().severe("Unable to get token from response: " + response);
            showError("Unable to request token");

        }

        // reset login form
        passwordField.setText("");
        loginButton.setEnabled(true);
    }

    public String getToken() {
        return token;
    }

    public void showError(String error) {
        errorLabel.setText(error);
        pack();
    }

    private class LogoPanel extends JPanel {
        private BufferedImage image;

        public LogoPanel() {
            try {
                image = ImageIO.read(getClass().getClassLoader().getResource("images/logo.png"));
            } catch (IOException e) {
                e.printStackTrace();
            }
        }

        @Override
        public Dimension getPreferredSize() {
            return new Dimension(75, 75);
        }

        @Override
        protected void paintComponent(Graphics g) {
            super.paintComponent(g);

            if(image == null) {
                g.setColor(Color.RED);
                g.drawRect(0, 0, 75, 75);
                return;
            }

            g.drawImage(image, 0, 0, 75, 75, this);
        }
    }

    private class TextPanel extends JPanel {

        public TextPanel() {
            JLabel line1 = new JLabel("To be able to send your test results you need to", SwingConstants.CENTER);
            JLabel line2 = new JLabel("supply your credentials from guts-web.pxl.be", SwingConstants.CENTER);
            JLabel line3 = new JLabel("No credentials yet?");
            Color green = new Color(88, 165, 24);
            line1.setForeground(green);
            line2.setForeground(green);
            line3.setForeground(green);
            add(line1);
            add(line2);
            add(line3);
            JButton registerButton = new JButton("Register");
            registerButton.setBorder(null);
            registerButton.setBorderPainted(false);
            registerButton.setOpaque(false);
            registerButton.setBackground(Color.WHITE);
            registerButton.setForeground(new Color(63, 119, 207));
            registerButton.setToolTipText(Configuration.getWebUrl() + "register");
            registerButton.addActionListener(e -> open(Configuration.getWebUrl() + "register"));
            add(registerButton);
        }

        @Override
        public Dimension getPreferredSize() {
            return new Dimension(325, 75);
        }
    }

}
