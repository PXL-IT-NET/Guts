package guts.client.utility;

import org.apache.commons.codec.digest.DigestUtils;
import guts.client.Configuration;

import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

public final class FileUtil {

    public static String getSourceCode(String pathsString) {
        if(pathsString == null || pathsString.isEmpty())
            return null;

        String[] paths = pathsString.split(";");

        StringBuilder sourceBuilder = new StringBuilder();
        for (String path : paths) {
            String sourcePath = Configuration.getSourceDirectory() + path;

            sourceBuilder.append("///" + path + "///");
            sourceBuilder.append("\n");

            try {
                sourceBuilder.append(Files.readString(Path.of(sourcePath)));
                sourceBuilder.append("\n");
            } catch (IOException e) {
                sourceBuilder.append("Failed to read content from " + path + "\n");
            }
        }

        return sourceBuilder.toString();
    }

    public static String getFileHash(String path) {
        try (InputStream is = Files.newInputStream(Paths.get(path))) {
            return DigestUtils.md5Hex(is);
        } catch (IOException e) {
            e.printStackTrace();
        }

        return "";
    }

}
