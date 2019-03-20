package guts.client.auth.swing;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class SwingLoginWindowTest {

    @Test
    void checkIfWindowOpens() {
        SwingLoginWindow window = new SwingLoginWindow(null);
        assertTrue(window.isVisible(), "Login window is not visible");
    }

}