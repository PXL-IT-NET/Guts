package be.pxl.guts.core.util;

import org.apache.commons.codec.digest.DigestUtils;

import javax.annotation.Nonnull;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.List;
import java.util.stream.Collectors;

// TODO add function single hash and code retrieval from single path and reference in (string, string)

/**
 * Utility class for handling file related things.
 * Example retrieving source code from files or getting the hash from a file.
 */
public class FileUtil {

    private FileUtil() {}

    /**
     * Get source code of a file
     *
     * @param path path to file
     * @return source code of file as string
     */
    public static String getSourceCode(@Nonnull String path) {
        StringBuilder sourceBuilder = new StringBuilder();

        sourceBuilder.append("///  " + path + "  ///");
        sourceBuilder.append("\n");

        try {
            sourceBuilder.append(Files.readString(Paths.get(path)));
            sourceBuilder.append("\n");
        } catch (IOException e) {
            sourceBuilder.append("Failed to read content from " + path + "\n");
        }

        return sourceBuilder.toString();
    }

    /**
     * Get source code of multiple files
     *
     * @param paths list of paths
     * @return source code of files as string
     */
    public static String getSourceCodeFiles(@Nonnull List<String> paths) {
        StringBuilder sourceBuilder = new StringBuilder();

        paths.forEach(p -> sourceBuilder.append(getSourceCode(p)));

        return sourceBuilder.toString();
    }

    public static String getSourceCodeFiles(String basePath, @Nonnull String pathsSeparatedBySemicolon) {
        String[] codeRelativePaths = pathsSeparatedBySemicolon.split(";");
        List<String> codeRelativePathsList = Arrays.asList(codeRelativePaths);
        List<String> codePaths = codeRelativePathsList.stream().map(path -> Paths.get(basePath, path).toString()).collect(Collectors.toList());

        return getSourceCodeFiles(codePaths);
    }

    /**
     * Get md5 hash of a file.
     *
     * @param path path to file
     * @return md5 hash of file
     */
    public static String getFileHash(String path) {
        try (InputStream is = Files.newInputStream(Paths.get(path))) {
            return DigestUtils.md5Hex(is);
        } catch (IOException e) {
            e.printStackTrace();
            return "Failed getting hash";
        }
    }

}
