package be.pxl.guts.core.logger;

import java.io.IOException;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.logging.*;

/**
 * Logger for Guts java clients
 */
public class GutsLogger extends Logger {

    private final Formatter formatter = new ConciseFormatter();

    /**
     * Constructor to create new Guts Logger without file logging
     */
    public GutsLogger() {
        this("");
    }

    /**
     * Constructor to create new Guts logger
     *
     * @param filePath file to log to (null or empty to disable file logging)
     */
    public GutsLogger(String filePath) {
        super("Guts Logger", null);
        this.setLevel(Level.ALL);

        try {
            if(filePath != null && !filePath.isEmpty()) {
                FileHandler fileHandler = new FileHandler(filePath, 16777216, 8, true);
                fileHandler.setFormatter(formatter);
                this.addHandler(fileHandler);
            }

            ConsoleHandler consoleHandler = new ConsoleHandler();
            consoleHandler.setLevel(Level.INFO);
            consoleHandler.setFormatter(formatter);
            this.addHandler(consoleHandler);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    class ConciseFormatter extends Formatter {

        private final DateFormat date = new SimpleDateFormat("dd-MM-yyyy HH:mm:ss");

        @Override
        public String format(LogRecord record) {
            StringBuilder formatted = new StringBuilder();

            formatted.append(this.date.format(record.getMillis()));
            formatted.append(" [");
            formatted.append(record.getLevel().getLocalizedName());
            formatted.append("] ");
            formatted.append(this.formatMessage(record));
            formatted.append('\n');

            if (record.getThrown() != null) {
                StringWriter writer = new StringWriter();
                record.getThrown().printStackTrace(new PrintWriter(writer));
                formatted.append(writer);
            }

            return formatted.toString();
        }
    }

}
