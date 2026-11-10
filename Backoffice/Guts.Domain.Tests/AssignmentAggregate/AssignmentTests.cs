using System;
using System.Linq;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using NUnit.Framework;

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
}