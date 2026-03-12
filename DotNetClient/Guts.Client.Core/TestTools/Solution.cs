namespace Guts.Client.Core.TestTools;

public class Solution
{
    public string Path { get; set; }

    private Solution(string path)
    {
        Path = path;
    }

    public static Solution Current
    {
        get
        {
            if (field is not null) return field;

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
                
            bool isSolutionDirectory = IsSolutionDirectory(directory);
            while (!isSolutionDirectory && directory.Parent != null)
            {
                directory = directory.Parent;
                isSolutionDirectory = IsSolutionDirectory(directory);
            }

            if (!isSolutionDirectory)
            {
                throw new InvalidOperationException("Technical error: could not find the path of the solution.");
            }
            field = new Solution(directory.FullName);
            return field;
        }
    }

    /// <summary>
    /// Gets the content of a file in the solution.
    /// </summary>
    /// <param name="relativeFilePath">
    /// The path to the file relative to the root folder of the solution.
    /// <example>@"ProjectName\MainWindow.xaml.cs"</example>
    /// </param>
    /// <returns>The file content as text.</returns>
    public string GetFileContent(string relativeFilePath)
    {
        var sourceCodePath = System.IO.Path.Combine(Path, relativeFilePath);
        return File.ReadAllText(sourceCodePath);
    }

    /// <summary>
    /// Calculates a MD5 hash from the (trimmed) content of a file in the solution
    /// </summary>
    /// <param name="relativeFilePath">
    /// The path to the file relative to the root folder of the solution.
    /// <example>@"ProjectName\MainWindow.xaml.cs"</example>
    /// </param>
    /// <returns>The hash of the file content</returns>
    public string GetFileHash(string relativeFilePath)
    {
        var sourceCodePath = System.IO.Path.Combine(Path, relativeFilePath);
        return FileUtil.GetFileHash(sourceCodePath);
    }

    private static bool IsSolutionDirectory(DirectoryInfo directoryInfo)
    {
        var isSolutionDirectory = directoryInfo.GetFiles("*.sln").Length > 0;
        if (!isSolutionDirectory)
        {
            isSolutionDirectory = directoryInfo.GetFiles("*.slnx").Length > 0;
        }
        return isSolutionDirectory;
    }
}