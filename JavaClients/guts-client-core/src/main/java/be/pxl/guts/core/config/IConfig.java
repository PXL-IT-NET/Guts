package be.pxl.guts.core.config;

/**
 * Interface for required minimum config options
 */
public interface IConfig {

    /**
     * Get the url where api request will be made.
     *
     * @return url of the api
     */
    String getApiUrl();

    /**
     * Set the url where apu request will be made.
     *
     * @param url new base url of Guts api
     */
    void setApiUrl(String url);

    /**
     * Get the url where front-end is located.
     *
     * @return url of the front-end
     */
    String getWebUrl();

    /**
     * Set the url where the front-end is located.
     *
     * @param url new base url of Guts website
     */
    void setWebUrl(String url);

    /**
     * Get the relative directory path to the source directory from the execution location.
     * @return relative directory path to source directory from the execution location
     */
    String getSourceDirectory();

    /**
     * Set the relative directory path to the source directory from the execution location.
     *
     * @param path path to source directory relative to execution folder
     */
    void setSourceDirectory(String path);

    /**
     * Get the relative directory path to the testing directory from the execution location.
     * @return relative directory path to testing directory from the execution location
     */
    String getTestDirectory();

    /**
     * Set the relative directory path to the source directory from the execution location.
     *
     * @param path path to test directory relative to execution folder
     */
    void setTestDirectory(String path);

}
