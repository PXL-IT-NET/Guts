using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Guts.Client.TestTools
{
    public class Solution
    {
        public string Path { get; set; }

        private Solution(string path)
        {
            Path = path;
        }

        private static Solution _current;
        public static Solution Current
        {
            get
            {
                if (_current != null) return _current;

                var executingFolder = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
                var solutionFolder = executingFolder.Parent?.Parent?.Parent?.Parent;
                Assert.That(solutionFolder, Is.Not.Null, () => "Technical error: could not find the path of the solution.");
                _current = new Solution(solutionFolder.FullName);
                return _current;
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
    }
}
