using System.Collections.Generic;
using System.Linq;
using Guts.Client.Models;

namespace Guts.Client.Utility
{
    internal class TestRunResultAccumulator
    {
        private static TestRunResultAccumulator _instance;

        public IList<TestResult> TestResults { get; }

        private TestRunResultAccumulator()
        {
            TestResults = new List<TestResult>();
        }

        public static TestRunResultAccumulator Instance => _instance ?? (_instance = new TestRunResultAccumulator());

        public void AddTestResult(TestResult result)
        {
            if (TestResults.Any(r => r.TestName == result.TestName)) return; //avoid duplicated (repeated tests)

            TestResults.Add(result);
        }

        public void Clear()
        {
            TestResults.Clear();
        }
    }
}
