package be.pxl.guts.core.util;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

import static org.junit.jupiter.api.Assertions.*;

class FileUtilTest {

    private static final Path TEST_FILE_PATH = Paths.get(System.getProperty("user.home"),".guts", "testfile.java");

    private static final String TEST_STRING = "package be.pxl.guts.core.models;\n" +
            "\n" +
            "public class Credentials {\n" +
            "\n" +
            "    private String email;\n" +
            "    private String password;\n" +
            "\n" +
            "    // comment on constructor\n" +
            "    public Credentials(String email, String password) {\n" +
            "        this.email = email;\n" +
            "        this.password = password;\n" +
            "    }\n" +
            "\n" +
            "    public String getEmail() {\n" +
            "        /**\n" +
            "         * Some multiline comment\n" +
            "         */\n" +
            "        return email;\n" +
            "    }\n" +
            "\n" +
            "    public String getPassword() {\n" +
            "        return password;\n" +
            "    }\n" +
            "\n" +
            "}\n";

    private static final String TEST_STRING_EXPECTED = "///  C:\\Users\\11700591\\.guts\\testfile.java  ///\n" +
            "package be.pxl.guts.core.models;\n" +
            "\n" +
            "public class Credentials {\n" +
            "\n" +
            "    private String email;\n" +
            "    private String password;\n" +
            "\n" +
            "    // comment on constructor\n" +
            "    public Credentials(String email, String password) {\n" +
            "        this.email = email;\n" +
            "        this.password = password;\n" +
            "    }\n" +
            "\n" +
            "    public String getEmail() {\n" +
            "        /**\n" +
            "         * Some multiline comment\n" +
            "         */\n" +
            "        return email;\n" +
            "    }\n" +
            "\n" +
            "    public String getPassword() {\n" +
            "        return password;\n" +
            "    }\n" +
            "\n" +
            "}\n" +
            "\n";

    @BeforeEach
    void setUp() {
        try {
            Files.deleteIfExists(TEST_FILE_PATH);
            Files.createFile(TEST_FILE_PATH);
            Files.writeString(TEST_FILE_PATH, TEST_STRING);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    @AfterEach
    void tearDown() {
        try {
            Files.deleteIfExists(TEST_FILE_PATH);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    @Test
    @DisplayName("Should get file hash")
    void shouldGetCorrectHash() {
        String hash = FileUtil.getFileHash(TEST_FILE_PATH.toString());
        assertEquals("86f9934d44ebfe0ac1345578a2350816", hash);
    }

    @Test
    @DisplayName("Should get source code")
    void shouldGetSourceFileContent() {
        String result = FileUtil.getSourceCode(TEST_FILE_PATH.toString());
        assertEquals(TEST_STRING_EXPECTED, result);
    }
}