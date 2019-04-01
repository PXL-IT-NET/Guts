package be.pxl.guts.core.config;

import com.google.gson.Gson;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Configuration handler for generating config
 */
public class Configuration {

    /**
     * Path to the config file (execution directory)
     */
    private static final Path CONFIGURATION_FILE_PATH = Paths.get(System.getProperty("user.dir") + File.separator + "guts.json");

    private Gson gson;
    private IConfig config;

    public Configuration(IConfig config) {
        this.gson = new Gson();
        this.config = config;

        if(!Files.exists(CONFIGURATION_FILE_PATH)) {
            createNewConfig();
            return;
        }

        loadConfiguration();
    }

    /**
     * Create a new config file.
     * @throws ConfigurationException
     */
    private void createNewConfig() throws ConfigurationException {
        try {
            Files.writeString(CONFIGURATION_FILE_PATH, this.gson.toJson(this.config));
        } catch (Exception e) {
            throw new ConfigurationException("Unable to create instance of config file -> Using default settings.", e);
        }
    }

    /**
     * Load config file.
     * @throws ConfigurationException
     */
    private void loadConfiguration() throws ConfigurationException {
        try {
            this.config = this.gson.fromJson(Files.readString(CONFIGURATION_FILE_PATH), this.config.getClass());
        } catch (IOException e) {
            throw new ConfigurationException("Unable to load config file -> Using default settings.", e);
        }
    }

    /**
     * Get config object
     * @return instance of config object. {@link IConfig}
     */
    public IConfig getConfig() {
        return config;
    }
}
