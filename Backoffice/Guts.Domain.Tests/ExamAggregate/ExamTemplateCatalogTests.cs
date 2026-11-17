using System.Linq;
using Guts.Common;
using Guts.Domain.ExamAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate;

[TestFixture]
public class ExamTemplateCatalogTests
{
    [Test]
    public void All_ContainsCuratedDotNet10Templates()
    {
        Assert.That(ExamTemplateCatalog.All, Is.Not.Empty);
        Assert.That(ExamTemplateCatalog.All.Select(t => t.Key), Contains.Item("dotnet10-console"));
        Assert.That(ExamTemplateCatalog.All.Select(t => t.Key), Contains.Item("dotnet10-classlib-nunit"));
        Assert.That(ExamTemplateCatalog.All.Select(t => t.Key), Contains.Item("dotnet10-classlib-xunit"));
        Assert.That(ExamTemplateCatalog.All.Select(t => t.Key), Contains.Item("dotnet10-aspnetcore-webapi"));
    }

    [Test]
    public void IsSupported_ReturnsTrueForKnownKeysAndFalseOtherwise()
    {
        Assert.That(ExamTemplateCatalog.IsSupported("dotnet10-console"), Is.True);
        Assert.That(ExamTemplateCatalog.IsSupported("DOTNET10-CONSOLE"), Is.True);
        Assert.That(ExamTemplateCatalog.IsSupported("unknown-template"), Is.False);
        Assert.That(ExamTemplateCatalog.IsSupported(null), Is.False);
        Assert.That(ExamTemplateCatalog.IsSupported(" "), Is.False);
    }

    [Test]
    public void GetByKey_ReturnsExpectedWasmExecutionCapability()
    {
        var console = ExamTemplateCatalog.GetByKey("dotnet10-console");
        var webApi = ExamTemplateCatalog.GetByKey("dotnet10-aspnetcore-webapi");

        Assert.That(console.SupportsWasmExecution, Is.True);
        Assert.That(webApi.SupportsWasmExecution, Is.False);
        Assert.That(console.DisplayName, Is.Not.Empty);
    }

    [Test]
    public void GetByKey_UnsupportedKey_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() => ExamTemplateCatalog.GetByKey("java-17"));
        Assert.Throws<ContractException>(() => ExamTemplateCatalog.GetByKey(""));
    }
}
