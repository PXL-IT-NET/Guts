using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Core
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
            var testName = _displayName ?? test.MethodName;

            if (IsTestCase(test))
            {
                testName += $" (Case {GetTestCaseNumber(test)})";
            }

            var resultAdapter = TestContext.CurrentContext.Result;
            var result = new TestResult
            {
                TestName = testName,
                Passed = Equals(resultAdapter.Outcome, ResultState.Success),
                Message = (resultAdapter.Message ?? string.Empty).Trim()
            };

            TestRunResultAccumulator.Instance.AddTestResult(result, test.Method?.TypeInfo);
        }

        private int GetTestCaseNumber(ITest test)
        {
            int testCaseNumber = 1;
            bool found = false;
            while (!found && testCaseNumber <= test.Parent.TestCaseCount)
            {
                if (test.Parent.Tests[testCaseNumber - 1].Id == test.Id)
                {
                    found = true;
                }
                else
                {
                    testCaseNumber++;
                }
            }

            return testCaseNumber;
        }

        private bool IsTestCase(ITest test)
        {
            return test.Arguments != null && test.Arguments.Length > 0;
        }
    }
}
