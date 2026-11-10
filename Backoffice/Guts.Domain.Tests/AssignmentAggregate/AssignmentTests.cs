using System;
using System.Linq;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using NUnit.Framework;
using TestEntity = Guts.Domain.TestAggregate.Test;

namespace Guts.Domain.Tests.AssignmentAggregate;

[TestFixture]
public class AssignmentTests
{
    [Test]
    public void Description_WhenNotSet_ShouldReturnCode()
    {
        string code = Random.Shared.NextString();
        var assignment = new Assignment
        {
            Code = code
        };

        Assert.That(assignment.Description, Is.EqualTo(code));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Description_WhenSetToNullOrEmpty_ShouldReturnCode(string emptyDescription)
    {
        string code = Random.Shared.NextString();
        var assignment = new Assignment
        {
            Code = code,
            Description = emptyDescription
        };

        Assert.That(assignment.Description, Is.EqualTo(code));
    }

    [Test]
    public void Description_WhenCustomDescriptionIsSet_ShouldReturnCustomDescription()
    {
        string code = Random.Shared.NextString();
        string customDescription = Random.Shared.NextString();
        var assignment = new Assignment
        {
            Code = code,
            Description = customDescription
        };

        Assert.That(assignment.Description, Is.EqualTo(customDescription));
    }

    [Test]
    public void ValidateOrRegisterTestCodeHash_WithMatchingHash_ShouldReturnValidAndNotAdded()
    {
        // Arrange
        var assignment = new Assignment();
        string hash = Guid.NewGuid().ToString();
        assignment.AddTestCodeHash(hash);

        // Act
        var result = assignment.ValidateOrRegisterTestCodeHash(hash, isLector: false);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HashWasAdded, Is.False);
        Assert.That(assignment.TestCodeHashes, Has.Count.EqualTo(1));
    }

    [Test]
    public void ValidateOrRegisterTestCodeHash_WithMismatchedHashForStudent_ShouldReturnInvalidAndNotAdded()
    {
        // Arrange
        var assignment = new Assignment();
        assignment.AddTestCodeHash(Guid.NewGuid().ToString());
        string invalidHash = Guid.NewGuid().ToString();

        // Act
        var result = assignment.ValidateOrRegisterTestCodeHash(invalidHash, isLector: false);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.HashWasAdded, Is.False);
        Assert.That(assignment.TestCodeHashes, Has.Count.EqualTo(1));
    }

    [Test]
    public void ValidateOrRegisterTestCodeHash_WithNewHashForTeacher_ShouldRegisterHashAndReturnValidAndAdded()
    {
        // Arrange
        var assignment = new Assignment();
        string newHash = Guid.NewGuid().ToString();

        // Act
        var result = assignment.ValidateOrRegisterTestCodeHash(newHash, isLector: true);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HashWasAdded, Is.True);
        Assert.That(assignment.TestCodeHashes, Has.Count.EqualTo(1));
        Assert.That(assignment.TestCodeHashes.First().Hash, Is.EqualTo(newHash));
    }

    [Test]
    public void ValidateOrRegisterTestCodeHash_WithAlreadyExistingHashForTeacher_ShouldReturnValidAndNotAdded()
    {
        // Arrange
        var assignment = new Assignment();
        string hash = Guid.NewGuid().ToString();
        assignment.AddTestCodeHash(hash);

        // Act
        var result = assignment.ValidateOrRegisterTestCodeHash(hash, isLector: true);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HashWasAdded, Is.False);
        Assert.That(assignment.TestCodeHashes, Has.Count.EqualTo(1));
    }

    [Test]
    public void ValidateOrRegisterTestCodeHash_WhenNoHashesRegisteredAndEmptyHashSubmitted_ShouldReturnValidAndNotAdded()
    {
        // Arrange
        var assignment = new Assignment();

        // Act
        var result = assignment.ValidateOrRegisterTestCodeHash(string.Empty, isLector: false);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HashWasAdded, Is.False);
        Assert.That(assignment.TestCodeHashes, Is.Empty);
    }

    [Test]
    public void ValidateOrRegisterTestCodeHash_WhenHashesRegisteredAndEmptyHashSubmittedByTeacher_ShouldReturnInvalidAndNotAdded()
    {
        // Arrange
        var assignment = new Assignment();
        assignment.AddTestCodeHash(Guid.NewGuid().ToString());

        // Act
        var result = assignment.ValidateOrRegisterTestCodeHash(string.Empty, isLector: true);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.HashWasAdded, Is.False);
        Assert.That(assignment.TestCodeHashes, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddTestCodeHash_WithEmptyHash_ShouldThrowContractException()
    {
        var assignment = new Assignment();

        Assert.That(() => assignment.AddTestCodeHash(string.Empty), Throws.InstanceOf<Common.ContractException>());
    }

    [Test]
    public void AddTest_WithValidName_ShouldAddAndReturnTest()
    {
        // Arrange
        var assignment = new Assignment { Id = 42 };

        // Act
        var test = assignment.AddTest("Test_A", id: 101);

        // Assert
        Assert.That(test, Is.Not.Null);
        Assert.That(test.Id, Is.EqualTo(101));
        Assert.That(test.AssignmentId, Is.EqualTo(42));
        Assert.That(test.TestName, Is.EqualTo("Test_A"));
        Assert.That(assignment.Tests, Is.Not.Null);
        Assert.That(assignment.Tests, Has.Count.EqualTo(1));
        Assert.That(assignment.Tests.First(), Is.SameAs(test));
    }

    [Test]
    public void AddTest_WithExistingName_ShouldReturnExistingWithoutDuplicating()
    {
        // Arrange
        var assignment = new Assignment { Id = 42 };
        var first = assignment.AddTest("Test_A", id: 1);

        // Act
        var second = assignment.AddTest("Test_A", id: 2);

        // Assert
        Assert.That(second, Is.SameAs(first));
        Assert.That(assignment.Tests, Has.Count.EqualTo(1));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AddTest_WithNullOrWhitespaceName_ShouldThrowContractException(string invalidName)
    {
        var assignment = new Assignment();

        Assert.That(() => assignment.AddTest(invalidName), Throws.InstanceOf<Common.ContractException>());
    }

    [Test]
    public void AddTest_WithTestEntity_ShouldAddIfNotPresent()
    {
        // Arrange
        var assignment = new Assignment { Id = 55 };
        var testEntity = new TestEntity { Id = 10, TestName = "UniqueTest" };

        // Act
        assignment.AddTest(testEntity);
        assignment.AddTest(new TestEntity { Id = 99, TestName = "UniqueTest" });

        // Assert
        Assert.That(assignment.Tests, Has.Count.EqualTo(1));
        Assert.That(assignment.Tests.First().AssignmentId, Is.EqualTo(55));
    }

    [Test]
    public void LoadTests_ShouldReplaceExistingTests()
    {
        // Arrange
        var assignment = new Assignment();
        assignment.AddTest("OldTest");

        var newTests = new[]
        {
            new TestEntity { Id = 1, TestName = "NewTest_1" },
            new TestEntity { Id = 2, TestName = "NewTest_2" }
        };

        // Act
        assignment.LoadTests(newTests);

        // Assert
        Assert.That(assignment.Tests, Has.Count.EqualTo(2));
        Assert.That(assignment.Tests.Select(t => t.TestName), Is.EquivalentTo(new[] { "NewTest_1", "NewTest_2" }));
    }

    [Test]
    public void ClearTests_ShouldResetTestsToNull()
    {
        // Arrange
        var assignment = new Assignment();
        assignment.AddTest("Test_1");
        Assert.That(assignment.Tests, Is.Not.Null);

        // Act
        assignment.ClearTests();

        // Assert
        Assert.That(assignment.Tests, Is.Null);
    }

    [Test]
    public void AddMissingTests_ShouldOnlyAddNewTestsAndReturnThem()
    {
        // Arrange
        var assignment = new Assignment { Id = 10 };
        assignment.AddTest("Existing_1");

        // Act
        var added = assignment.AddMissingTests(new[] { "Existing_1", "New_2", "New_3" });

        // Assert
        Assert.That(added, Has.Count.EqualTo(2));
        Assert.That(added.Select(t => t.TestName), Is.EquivalentTo(new[] { "New_2", "New_3" }));
        Assert.That(assignment.Tests, Has.Count.EqualTo(3));
    }
}