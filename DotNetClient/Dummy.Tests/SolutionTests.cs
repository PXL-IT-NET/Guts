using Guts.Client.Core.TestTools;
using NUnit.Framework;

namespace Dummy.Tests;

public class SolutionTests
{
    [Test]
    public void GetFileContent_ExistingFile_ReadFile()
    {
        string sourceCode = Solution.Current.GetFileContent(@"Guts.Client.Core\TestTools\Solution.cs");
        Assert.That(sourceCode, Is.Not.Empty);
    }
}