using System;
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
}