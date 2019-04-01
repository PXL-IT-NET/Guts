package be.pxl.guts.core.config;

/**
 * Standard configuration required for Guts Java client core
 */
public class BasicConfig implements IConfig {

    private String apiUrl = "http://localhost:54830/";
    private String webUrl = "http://localhost:54831/";
    private String sourceDirectory = "src/main/java/";
    private String testDirectory = "src/test/java/";

    @Override
    public String getApiUrl() {
        return apiUrl;
    }

    @Override
    public void setApiUrl(String url) {
        apiUrl = url;
    }

    @Override
    public String getWebUrl() {
        return webUrl;
    }

    @Override
    public void setWebUrl(String url) {
        webUrl = url;
    }

    @Override
    public String getSourceDirectory() {
        return sourceDirectory;
    }

    @Override
    public void setSourceDirectory(String path) {
        sourceDirectory = path;
    }

    @Override
    public String getTestDirectory() {
        return testDirectory;
    }

    @Override
    public void setTestDirectory(String path) {
        testDirectory = path;
    }
}
