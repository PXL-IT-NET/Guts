package guts.client;

import com.google.gson.Gson;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Configuration options for Guts
 */
public final class Configuration {

    private static final Path CONFIGURATION_FILE_PATH = Paths.get(System.getProperty("user.dir") + File.separator + "guts.json");
    private static Options options;

    public static void setup() {
        Gson gson = new Gson();

        if(!Files.exists(CONFIGURATION_FILE_PATH)) {
            GutsJUnit5.getLogger().fine("No configuration file found! Creating new file... ");
            try {
                Files.writeString(CONFIGURATION_FILE_PATH, gson.toJson(new Options()));
                GutsJUnit5.getLogger().info("Created default configuration file, meant for maven projects.");
            } catch (IOException e) {
                GutsJUnit5.getLogger().warning("Unable to create configuration file... Using default configuration options, meant for maven projects.");
            }

            options = new Options();
            return;
        }

        try {
            options = gson.fromJson(Files.readString(CONFIGURATION_FILE_PATH), Options.class);
        } catch (IOException e) {
            GutsJUnit5.getLogger().severe("Failed to load config file... Using default configuration options, meant for maven projects.");
            options = new Options();
        }
    }

    public static String getBaseUrl() {
        checkIfOptionsLoaded();
        return options.baseUrl;
    }

    public static String getWebUrl() {
        checkIfOptionsLoaded();
        return options.webUrl;
    }

    public static String getSourceDirectory() {
        checkIfOptionsLoaded();
        return options.sourceDirectory;
    }

    public static String getTestDirectory() {
        checkIfOptionsLoaded();
        return options.testDirectory;
    }

    private static void checkIfOptionsLoaded() {
        if(options != null)
            return;

        options = new Options();
        GutsJUnit5.getLogger().warning("No configuration file was loaded... Using default configuration options, meant for maven projects.");
    }

    private static class Options {
        private String baseUrl = "http://localhost:54830/";
        private String webUrl = "http://localhost:54831/";
        private String sourceDirectory = "src/main/java/";
        private String testDirectory = "src/test/java/";
    }

}
