namespace Guts.Client.Core.Models
{
    public class SolutionFile
    {
        public string FilePath { get; }
        public string Content { get; }

        public SolutionFile(string filePath, string content)        
        {
            FilePath = filePath;
            Content = content;
        }
    }
}