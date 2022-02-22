using System;
using System.Collections.Generic;
using Guts.Client.Core.Models;
using Guts.Client.Core.TestTools;

namespace Guts.Client.Core.Utility
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