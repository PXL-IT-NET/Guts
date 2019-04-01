package be.pxl.guts.core.models;

/**
 * Model used to send credentials to api.
 */
public class Credentials {

    private String email;
    private String password;

    /**
     * Constructor for creating a new Credentials object
     *
     * @param email email to be authorized
     * @param password password to be authorized
     */
    public Credentials(String email, String password) {
        this.email = email;
        this.password = password;
    }

}
