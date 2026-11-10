using Guts.Common;

namespace Guts.Domain.GamificationAggregate;

public class HallOfFameStanding
{
    public StudentCourseProfile Profile { get; }
    public int GlobalRank { get; }
    public HallOfFameTier Tier { get; }
    public int TierRank { get; }

    public HallOfFameStanding(StudentCourseProfile profile, int globalRank, HallOfFameTier tier, int tierRank)
    {
        Contracts.Require(profile != null, "Profile cannot be null.");
        Contracts.Require(globalRank > 0, "Global rank must be positive.");
        Contracts.Require(tierRank > 0, "Tier rank must be positive.");

        Profile = profile;
        GlobalRank = globalRank;
        Tier = tier;
        TierRank = tierRank;
    }
}

