using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Guts.Client.Core
{
    public class MonitoredTestAttribute : TestAttribute, ITestAction
    {
        private readonly string? _displayName;

        public ActionTargets Targets => ActionTargets.Test;

        public MonitoredTestAttribute()
        {
            _displayName = null;
        }

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
            var testName = _displayName ?? new CamelCaseConverter().ToNormalSentence(test.MethodName);

            if (IsTestCase(test))
            {
                testName += $" (Case {GetTestCaseNumber(test)})";
            }

            var resultAdapter = TestContext.CurrentContext.Result;
            var result = new TestResult(
                testName,
                passed:Equals(resultAdapter.Outcome, ResultState.Success),
                message:(resultAdapter.Message ?? string.Empty).Trim()
            );

            ITypeInfo methodTypeInfo = test.Method?.TypeInfo!;
            TestRunResultAccumulator.Instance.AddTestResult(result, methodTypeInfo);
        }

        private int GetTestCaseNumber(ITest test)
        {
            int testCaseNumber = 1;
            bool found = false;
            ITest parentTest = test.Parent!;
            while (!found && testCaseNumber <= parentTest.TestCaseCount)
            {
                if (parentTest.Tests[testCaseNumber - 1].Id == test.Id)
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
