using Guts.Domain.GamificationAggregate.Profiles;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate.Profiles;

[TestFixture]
public class BadgeTests
{
    [Test]
    public void All_ShouldContainAll22Badges()
    {
        Assert.That(Badge.All.Count, Is.EqualTo(22));

        foreach (var badge in Badge.All)
        {
            Assert.That(badge.Value, Is.GreaterThan(0));
            Assert.That(badge.Name, Is.Not.Null.And.Not.Empty);
            Assert.That(badge.Description, Is.Not.Null.And.Not.Empty);
            Assert.That(badge.Icon, Is.Not.Null.And.Not.Empty);
        }
    }

    [TestCase(1, "First Spark", "🌱")]
    [TestCase(2, "Speed Demon", "⚡")]
    [TestCase(4, "Clean Sweep", "🧹")]
    [TestCase(5, "Night Owl", "🦉")]
    [TestCase(6, "Early Bird", "🌅")]
    [TestCase(7, "Weekend Warrior", "⚔️")]
    [TestCase(8, "Century Club", "💯")]
    [TestCase(9, "On Fire", "🔥")]
    [TestCase(10, "Iron Will", "🛡️")]
    [TestCase(11, "Sprint Gold", "🥇")]
    [TestCase(12, "Team Powerhouse", "🤝")]
    [TestCase(13, "Sprint Silver", "🥈")]
    [TestCase(14, "Sprint Bronze", "🥉")]
    [TestCase(15, "Sprint Top 4", "🎖️")]
    [TestCase(16, "Sprint Top 5", "⭐")]
    [TestCase(17, "Warming Up", "🕯️")]
    [TestCase(18, "Unstoppable", "☄️")]
    [TestCase(19, "Quarter Century", "🎯")]
    [TestCase(20, "Half Century", "🏅")]
    [TestCase(21, "First Victory", "🏁")]
    [TestCase(22, "High Five", "🖐️")]
    [TestCase(23, "Rank Defender", "🏰")]
    public void FromValue_KnownValue_ShouldReturnExpectedBadge(int value, string expectedName, string expectedIcon)
    {
        var badge = Badge.FromValue(value);

        Assert.That(badge.Value, Is.EqualTo(value));
        Assert.That(badge.Name, Is.EqualTo(expectedName));
        Assert.That(badge.Icon, Is.EqualTo(expectedIcon));
    }

    [Test]
    public void FromValue_UnknownValue_ShouldReturnFallbackBadge()
    {
        var badge = Badge.FromValue(9999);

        Assert.That(badge.Value, Is.EqualTo(9999));
        Assert.That(badge.Name, Is.EqualTo("9999"));
        Assert.That(badge.Description, Is.EqualTo(string.Empty));
        Assert.That(badge.Icon, Is.EqualTo("🏅"));
    }

    [Test]
    public void Equals_SameValue_ShouldBeEqual()
    {
        var b1 = Badge.FromValue(1);
        var b2 = Badge.FirstSpark;

        Assert.That(b1, Is.EqualTo(b2));
        Assert.That(b1 == b2, Is.True);
        Assert.That(b1 != Badge.SpeedDemon, Is.True);
    }

    [Test]
    public void ImplicitConversionToInt_ShouldReturnBadgeValue()
    {
        int val = Badge.SprintGold;
        Assert.That(val, Is.EqualTo(11));

        Badge nullBadge = null;
        int nullVal = nullBadge;
        Assert.That(nullVal, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_InvalidParameters_ShouldThrow()
    {
        Assert.Throws<Guts.Common.ContractException>(() => new Badge(0, "Name", "Desc", "🏅"));
        Assert.Throws<Guts.Common.ContractException>(() => new Badge(1, "", "Desc", "🏅"));
        Assert.Throws<Guts.Common.ContractException>(() => new Badge(1, "Name", "Desc", ""));
    }
}

