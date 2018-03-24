using Guts.Client.Models;
using Guts.Client.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client
{
    public class MonitoredTestAttribute : TestAttribute, ITestAction
    {
        private readonly string _displayName;

        public ActionTargets Targets => ActionTargets.Test;

        public MonitoredTestAttribute(string displayName)
        {
            _displayName = displayName;
        }

        public void BeforeTest(ITest test)
        {
            //do nothing before
        }

        public void AfterTest(ITest test)
        {
            var resultAdapter = TestContext.CurrentContext.Result;
            var result = new TestResult
            {
                TestName = _displayName ?? test.MethodName,
                Passed = Equals(resultAdapter.Outcome, ResultState.Success),
                Message = (resultAdapter.Message ?? string.Empty).Trim()
            };

            TestRunResultAccumulator.Instance.AddTestResult(result);
        }
    }
}
