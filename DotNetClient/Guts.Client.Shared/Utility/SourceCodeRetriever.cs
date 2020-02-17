using System;
using System.Collections.Generic;
using Guts.Client.Shared.Models;
using Guts.Client.Shared.TestTools;

namespace Guts.Client.Shared.Utility
{
    public static class SourceCodeRetriever
    {
        public static IEnumerable<SolutionFile> ReadSourceCodeFiles(string sourceCodeRelativeFilePaths)
        {
            var sourceFiles = new List<SolutionFile>();
            if (string.IsNullOrEmpty(sourceCodeRelativeFilePaths)) return sourceFiles;

            var paths = sourceCodeRelativeFilePaths.Split(" ,;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach (var path in paths)
            {
                var trimmedPath = path.Trim('\n', '\r');

                sourceFiles.Add(new SolutionFile
                {
                    FilePath = trimmedPath,
                    Content = Solution.Current.GetFileContent(trimmedPath)
                });
            }

            return sourceFiles;
        }
    }
}