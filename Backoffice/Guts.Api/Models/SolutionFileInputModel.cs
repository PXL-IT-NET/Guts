namespace Guts.Api.Models;

public class SolutionFileInputModel
{
    public string FilePath { get; set; }

    /// <summary>
    /// Base64 encoded content of the file
    /// </summary>
    public string Content { get; set; }
}