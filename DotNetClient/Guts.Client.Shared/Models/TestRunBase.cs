using System.Collections.Generic;

namespace Guts.Client.Shared.Models
{
    public abstract class TestRunBase
    {
        public IEnumerable<TestResult> Results { get; set; }

        public string SourceCode { get; set; }

        public string TestCodeHash { get; set; }
    }
}