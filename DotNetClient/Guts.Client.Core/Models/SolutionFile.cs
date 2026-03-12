namespace Guts.Client.Core.Models;

public class SolutionFile(string filePath, string content)
{
    public string FilePath { get; } = filePath;
    public string Content { get; } = content;
}