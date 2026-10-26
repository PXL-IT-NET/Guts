using Guts.Common;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.ValueObjects;

[TestFixture]
public class AssessmentScoreTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Constructor_ValidNonNegativeScore_ShouldSetValue(int validScore)
    {
        var score = new AssessmentScore(validScore);

        Assert.That(score.Value, Is.EqualTo(validScore));
    }

    [TestCase(-1)]
    [TestCase(-2)]
    [TestCase(-100)]
    public void Constructor_NegativeValue_ShouldClampToMinusOne(int negativeValue)
    {
        var score = new AssessmentScore(negativeValue);

        Assert.That(score.Value, Is.EqualTo(-1));
        Assert.That(score, Is.EqualTo(AssessmentScore.NullScore));
    }

    [TestCase(6)]
    [TestCase(10)]
    [TestCase(100)]
    public void Constructor_ValueGreaterThanFive_ShouldThrowContractException(int invalidValue)
    {
        Assert.That(() => new AssessmentScore(invalidValue), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void StaticPredefinedInstances_ShouldHaveExpectedValues()
    {
        Assert.That(AssessmentScore.NullScore.Value, Is.EqualTo(-1));
        Assert.That(AssessmentScore.NoAddedValue.Value, Is.EqualTo(0));
        Assert.That(AssessmentScore.WayBelowAverage.Value, Is.EqualTo(1));
        Assert.That(AssessmentScore.BelowAverage.Value, Is.EqualTo(2));
        Assert.That(AssessmentScore.Average.Value, Is.EqualTo(3));
        Assert.That(AssessmentScore.AboveAverage.Value, Is.EqualTo(4));
        Assert.That(AssessmentScore.WayAboveAverage.Value, Is.EqualTo(5));
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(5)]
    public void ImplicitConversion_IntToAndFromAssessmentScore_ShouldPreserveValue(int intValue)
    {
        AssessmentScore score = intValue;
        int convertedBack = score;

        Assert.That(score.Value, Is.EqualTo(intValue));
        Assert.That(convertedBack, Is.EqualTo(intValue));
    }

    [TestCase(2.4, 2)]
    [TestCase(2.5, 3)]
    [TestCase(3.5, 4)]
    [TestCase(4.5, 5)]
    [TestCase(0.4, 0)]
    [TestCase(-0.5, -1)]
    public void ImplicitConversion_DoubleToAssessmentScore_ShouldRoundAwayFromZero(double doubleValue, int expectedIntValue)
    {
        AssessmentScore score = doubleValue;

        Assert.That(score.Value, Is.EqualTo(expectedIntValue));
    }

    [Test]
    public void Equality_SameScoreValue_ShouldBeEqual()
    {
        var scoreA = new AssessmentScore(3);
        var scoreB = new AssessmentScore(3);

        Assert.That(scoreA.Equals(scoreB), Is.True);
        Assert.That(scoreA == scoreB, Is.True);
        Assert.That(scoreA != scoreB, Is.False);
        Assert.That(scoreA.GetHashCode(), Is.EqualTo(scoreB.GetHashCode()));
    }

    [Test]
    public void Equality_DifferentScoreValue_ShouldNotBeEqual()
    {
        var scoreA = new AssessmentScore(2);
        var scoreB = new AssessmentScore(4);

        Assert.That(scoreA.Equals(scoreB), Is.False);
        Assert.That(scoreA == scoreB, Is.False);
        Assert.That(scoreA != scoreB, Is.True);
        Assert.That(scoreA.GetHashCode(), Is.Not.EqualTo(scoreB.GetHashCode()));
    }

    [Test]
    public void Equality_NullComparisons_ShouldBehaveCorrectly()
    {
        var score = new AssessmentScore(3);
        AssessmentScore nullScoreA = null;
        AssessmentScore nullScoreB = null;

        Assert.That(score.Equals(null), Is.False);
        Assert.That(score == null, Is.False);
        Assert.That(null == score, Is.False);
        Assert.That(score != null, Is.True);
        Assert.That(nullScoreA == nullScoreB, Is.True);
        Assert.That(nullScoreA != nullScoreB, Is.False);
    }

    [TestCase(-1, "-1")]
    [TestCase(0, "0")]
    [TestCase(3, "3")]
    [TestCase(5, "5")]
    public void ToString_ShouldReturnStringRepresentationOfValue(int value, string expectedString)
    {
        var score = new AssessmentScore(value);

        Assert.That(score.ToString(), Is.EqualTo(expectedString));
    }
}