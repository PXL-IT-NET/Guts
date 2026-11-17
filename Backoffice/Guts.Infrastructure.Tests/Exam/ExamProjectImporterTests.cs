using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Guts.Common;
using Guts.Domain.ExamAggregate;
using Guts.Infrastructure.Exams;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Exams;

[TestFixture]
public class ExamProjectImporterTests
{
    private ExamProjectImporter _importer;

    [SetUp]
    public void Setup()
    {
        _importer = new ExamProjectImporter();
    }

    [Test]
    public async Task ExtractFromZipAsync_FiltersBuildArtifactsAndSuggestsRoles()
    {
        using var zipStream = CreateZip(
            ("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            ("App/Calculator.cs", "public class Calculator {}"),
            ("App.Tests/CalculatorTests.cs", "public class CalculatorTests {}"),
            ("App/bin/Debug/App.dll", "binary-bytes"),
            ("App/obj/project.assets.json", "{}"),
            (".git/HEAD", "ref: refs/heads/main"));

        var files = await _importer.ExtractFromZipAsync(zipStream);

        Assert.That(files, Has.Count.EqualTo(3));
        Assert.That(files.Single(f => f.RelativePath == "App/App.csproj").SuggestedRole, Is.EqualTo(ExamFileRole.ReadOnly));
        Assert.That(files.Single(f => f.RelativePath == "App/Calculator.cs").SuggestedRole, Is.EqualTo(ExamFileRole.Editable));
        Assert.That(files.Single(f => f.RelativePath == "App.Tests/CalculatorTests.cs").SuggestedRole, Is.EqualTo(ExamFileRole.PublicTest));
    }

    [Test]
    public void ExtractFromZipAsync_PathTraversalOrNullStream_ThrowsContractException()
    {
        Assert.ThrowsAsync<ContractException>(() => _importer.ExtractFromZipAsync(null));

        using var zipStream = CreateZip(("../evil.cs", "class Evil {}"));
        Assert.ThrowsAsync<ContractException>(() => _importer.ExtractFromZipAsync(zipStream));
    }

    [Test]
    public void CloneFromGitAsync_NonHttpsOrPrivateHost_ThrowsContractException()
    {
        Assert.ThrowsAsync<ContractException>(() => _importer.CloneFromGitAsync(""));
        Assert.ThrowsAsync<ContractException>(() => _importer.CloneFromGitAsync("file:///C:/repo"));
        Assert.ThrowsAsync<ContractException>(() => _importer.CloneFromGitAsync("http://github.com/org/repo.git"));
        Assert.ThrowsAsync<ContractException>(() => _importer.CloneFromGitAsync("https://localhost/org/repo.git"));
        Assert.ThrowsAsync<ContractException>(() => _importer.CloneFromGitAsync("https://127.0.0.1/org/repo.git"));
        Assert.ThrowsAsync<ContractException>(() => _importer.CloneFromGitAsync("https://192.168.1.5/org/repo.git"));
    }

    private static MemoryStream CreateZip(params (string EntryName, string Content)[] entries)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entryName, content) in entries)
            {
                var entry = archive.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content);
            }
        }

        ms.Position = 0;
        return ms;
    }
}
