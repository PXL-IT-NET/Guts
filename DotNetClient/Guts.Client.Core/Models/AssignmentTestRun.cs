using System.Collections.Generic;

namespace Guts.Client.Core.Models
{
    public class AssignmentTestRun
    {
        public Assignment Assignment { get;}

        public IEnumerable<TestResult> Results { get; }

        public IEnumerable<SolutionFile> SolutionFiles { get; }

        public string TestCodeHash { get; }

        public AssignmentTestRun(Assignment assignment, IEnumerable<TestResult> results, IEnumerable<SolutionFile> solutionFiles, string testCodeHash)
        {
            Assignment = assignment;
            Results = results;
            SolutionFiles = solutionFiles;
            TestCodeHash = testCodeHash;
        }
    }
}