package be.pxl.guts.core.config;

/**
 * Exception thrown when an error occurs during configuration creating/loading.
 */
public class ConfigurationException extends RuntimeException {

    /** Construct a new configuration exception with the specified detail message.
     * @param message the detail message.
     */
    ConfigurationException(String message) {
        super(message);
    }

    /** Construct a new configuration exception with the specified detail message and cause.
     * @param message the detail message.
     * @param cause the cause
     */
    ConfigurationException(String message, Throwable cause) {
        super(message, cause);
    }

}
