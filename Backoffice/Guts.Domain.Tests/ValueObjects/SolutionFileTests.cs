using System;
using System.Reflection;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ValueObjects;

[TestFixture]
public class SolutionFileTests
{
    [Test]
    public void CreateNew_ValidInput_ShouldCreateSolutionFileWithUtcNow()
    {
        int assignmentId = Random.Shared.NextPositive();
        int userId = Random.Shared.NextPositive();
        var filePath = new FilePath("Folder/Solution.cs");
        string content = "public class Solution {}";
        DateTime before = DateTime.UtcNow;

        var solutionFile = SolutionFile.CreateNew(assignmentId, userId, filePath, content);

        DateTime after = DateTime.UtcNow;
        Assert.That(solutionFile, Is.Not.Null);
        Assert.That(solutionFile.AssignmentId, Is.EqualTo(assignmentId));
        Assert.That(solutionFile.UserId, Is.EqualTo(userId));
        Assert.That(solutionFile.FilePath, Is.EqualTo(filePath));
        Assert.That(solutionFile.Content, Is.EqualTo(content));
        Assert.That(solutionFile.ModifyDateTime, Is.InRange(before, after));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateNew_InvalidAssignmentId_ShouldThrowContractException(int invalidAssignmentId)
    {
        int userId = Random.Shared.NextPositive();
        var filePath = new FilePath("Solution.cs");

        Assert.That(() => SolutionFile.CreateNew(invalidAssignmentId, userId, filePath, "content"),
            Throws.InstanceOf<ContractException>());
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateNew_InvalidUserId_ShouldThrowContractException(int invalidUserId)
    {
        int assignmentId = Random.Shared.NextPositive();
        var filePath = new FilePath("Solution.cs");

        Assert.That(() => SolutionFile.CreateNew(assignmentId, invalidUserId, filePath, "content"),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_NullFilePath_ShouldThrowContractException()
    {
        int assignmentId = Random.Shared.NextPositive();
        int userId = Random.Shared.NextPositive();

        Assert.That(() => SolutionFile.CreateNew(assignmentId, userId, null, "content"),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_FilePathExceeds255Characters_ShouldThrowContractException()
    {
        int assignmentId = Random.Shared.NextPositive();
        int userId = Random.Shared.NextPositive();
        var maxLengthPath = new FilePath(new string('a', 252) + ".cs"); // 255 chars
        var tooLongPath = new FilePath(new string('a', 253) + ".cs"); // 256 chars

        Assert.That(() => SolutionFile.CreateNew(assignmentId, userId, maxLengthPath, "content"), Throws.Nothing);
        Assert.That(() => SolutionFile.CreateNew(assignmentId, userId, tooLongPath, "content"),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_NullContent_ShouldThrowContractException()
    {
        int assignmentId = Random.Shared.NextPositive();
        int userId = Random.Shared.NextPositive();
        var filePath = new FilePath("Solution.cs");

        Assert.That(() => SolutionFile.CreateNew(assignmentId, userId, filePath, null),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void IsNewVersionOf_NullPreviousFile_ShouldReturnTrue()
    {
        var currentFile = SolutionFile.CreateNew(1, 1, new FilePath("Solution.cs"), "content");

        Assert.That(currentFile.IsNewVersionOf(null), Is.True);
    }

    [Test]
    public void IsNewVersionOf_DifferentAssignmentId_ShouldReturnFalse()
    {
        DateTime t1 = DateTime.UtcNow.AddMinutes(-5);
        DateTime t2 = DateTime.UtcNow;
        var previousFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "v1", t1);
        var currentFile = CreateSolutionFileWithTimestamp(2, 1, "Solution.cs", "v2", t2);

        Assert.That(currentFile.IsNewVersionOf(previousFile), Is.False);
    }

    [Test]
    public void IsNewVersionOf_DifferentUserId_ShouldReturnFalse()
    {
        DateTime t1 = DateTime.UtcNow.AddMinutes(-5);
        DateTime t2 = DateTime.UtcNow;
        var previousFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "v1", t1);
        var currentFile = CreateSolutionFileWithTimestamp(1, 2, "Solution.cs", "v2", t2);

        Assert.That(currentFile.IsNewVersionOf(previousFile), Is.False);
    }

    [Test]
    public void IsNewVersionOf_DifferentFilePath_ShouldReturnFalse()
    {
        DateTime t1 = DateTime.UtcNow.AddMinutes(-5);
        DateTime t2 = DateTime.UtcNow;
        var previousFile = CreateSolutionFileWithTimestamp(1, 1, "SolutionA.cs", "v1", t1);
        var currentFile = CreateSolutionFileWithTimestamp(1, 1, "SolutionB.cs", "v2", t2);

        Assert.That(currentFile.IsNewVersionOf(previousFile), Is.False);
    }

    [TestCase(0)]
    [TestCase(5)]
    public void IsNewVersionOf_PreviousFileModifyDateTimeGreaterOrEqual_ShouldReturnFalse(int minutesOffset)
    {
        DateTime currentTimestamp = DateTime.UtcNow;
        DateTime previousTimestamp = currentTimestamp.AddMinutes(minutesOffset);
        var previousFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "v1", previousTimestamp);
        var currentFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "v2", currentTimestamp);

        Assert.That(currentFile.IsNewVersionOf(previousFile), Is.False);
    }

    [Test]
    public void IsNewVersionOf_SameContent_ShouldReturnFalse()
    {
        DateTime t1 = DateTime.UtcNow.AddMinutes(-5);
        DateTime t2 = DateTime.UtcNow;
        var previousFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "same content", t1);
        var currentFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "same content", t2);

        Assert.That(currentFile.IsNewVersionOf(previousFile), Is.False);
    }

    [Test]
    public void IsNewVersionOf_NewerTimestampAndDifferentContent_ShouldReturnTrue()
    {
        DateTime t1 = DateTime.UtcNow.AddMinutes(-5);
        DateTime t2 = DateTime.UtcNow;
        var previousFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "old content", t1);
        var currentFile = CreateSolutionFileWithTimestamp(1, 1, "Solution.cs", "new content", t2);

        Assert.That(currentFile.IsNewVersionOf(previousFile), Is.True);
    }

    [Test]
    public void Equality_SameComponents_ShouldBeEqual()
    {
        DateTime timestamp = DateTime.UtcNow;
        var fileA = CreateSolutionFileWithTimestamp(1, 2, "Solution.cs", "content A", timestamp);
        var fileB = CreateSolutionFileWithTimestamp(1, 2, "Solution.cs", "content B", timestamp);

        Assert.That(fileA.Equals(fileB), Is.True);
        Assert.That(fileA == fileB, Is.True);
        Assert.That(fileA != fileB, Is.False);
        Assert.That(fileA.GetHashCode(), Is.EqualTo(fileB.GetHashCode()));
    }

    [Test]
    public void Equality_DifferentComponents_ShouldNotBeEqual()
    {
        DateTime timestamp = DateTime.UtcNow;
        var baseFile = CreateSolutionFileWithTimestamp(1, 2, "Solution.cs", "content", timestamp);
        var diffAssignment = CreateSolutionFileWithTimestamp(99, 2, "Solution.cs", "content", timestamp);
        var diffUser = CreateSolutionFileWithTimestamp(1, 99, "Solution.cs", "content", timestamp);
        var diffPath = CreateSolutionFileWithTimestamp(1, 2, "Other.cs", "content", timestamp);
        var diffTime = CreateSolutionFileWithTimestamp(1, 2, "Solution.cs", "content", timestamp.AddSeconds(1));

        Assert.That(baseFile, Is.Not.EqualTo(diffAssignment));
        Assert.That(baseFile, Is.Not.EqualTo(diffUser));
        Assert.That(baseFile, Is.Not.EqualTo(diffPath));
        Assert.That(baseFile, Is.Not.EqualTo(diffTime));
    }

    private static SolutionFile CreateSolutionFileWithTimestamp(int assignmentId, int userId, string path, string content, DateTime modifyDateTime)
    {
        var solutionFile = SolutionFile.CreateNew(assignmentId, userId, new FilePath(path), content);
        typeof(SolutionFile)
            .GetProperty(nameof(SolutionFile.ModifyDateTime), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(solutionFile, modifyDateTime);
        return solutionFile;
    }
}