using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Services;
using Guts.Common.Extensions;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Application.Tests.Services;

[TestFixture]
public class SolutionFileServiceTests
{
    private SolutionFileService _service;

    [SetUp]
    public void Setup()
    {
        _service = new SolutionFileService();
    }

    [Test]
    public async Task CreateZipFromFiles_ShouldReturnValidZipByteArrayContainingAllSolutionFiles()
    {
        //Arrange
        List<SolutionDto> solutions =
        [
            new()
            {
                WriterId = Random.Shared.NextPositive(),
                WriterName = $"{Random.Shared.NextString()} {Random.Shared.NextString()}",
                SolutionFiles = (List<SolutionFile>)
                [
                    new SolutionFileBuilder().Build(),
                    new SolutionFileBuilder().Build()
                ]
            },

            new()
            {
                WriterId = Random.Shared.NextPositive(),
                WriterName = $"User{Random.Shared.NextPositive()}",
                SolutionFiles = (List<SolutionFile>)[new SolutionFileBuilder().Build()]
            }
        ];

        //Act
        byte[] zipBytes = await _service.CreateZipFromFiles(solutions);

        //Assert
        Assert.That(zipBytes, Is.Not.Null);
        Assert.That(zipBytes, Is.Not.Empty);

        using var memoryStream = new MemoryStream(zipBytes);
        using var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        int expectedEntryCount = solutions.Sum(s => s.SolutionFiles.Count());
        Assert.That(zipArchive.Entries, Has.Count.EqualTo(expectedEntryCount));

        foreach (SolutionDto solution in solutions)
        {
            foreach (SolutionFile solutionFile in solution.SolutionFiles)
            {
                string expectedEntryName = $@"{solution.WriterName}\{solutionFile.FilePath}";
                ZipArchiveEntry entry = zipArchive.GetEntry(expectedEntryName);
                Assert.That(entry, Is.Not.Null, $"Expected zip entry '{expectedEntryName}' was not found.");

                using var reader = new StreamReader(await entry.OpenAsync());
                string content = await reader.ReadToEndAsync();
                Assert.That(content, Is.EqualTo(solutionFile.Content));
            }
        }
    }

    [Test]
    public async Task CreateZipFromFiles_EmptySolutions_ShouldReturnValidEmptyZipArchive()
    {
        //Arrange
        var solutions = new List<SolutionDto>();

        //Act
        byte[] zipBytes = await _service.CreateZipFromFiles(solutions);

        //Assert
        Assert.That(zipBytes, Is.Not.Null);
        Assert.That(zipBytes, Is.Not.Empty);

        using var memoryStream = new MemoryStream(zipBytes);
        using var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        Assert.That(zipArchive.Entries, Is.Empty);
    }
}