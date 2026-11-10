using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;

namespace Guts.Domain.GamificationAggregate.Competitions;

public static class ClassGroupCompetition
{
    public static IReadOnlyList<ClassGroupStanding> DetermineStandings(
        IEnumerable<CourseClassGroup> classGroups,
        IEnumerable<StudentCourseProfile> allProfiles,
        int? currentUserId = null)
    {
        Contracts.Require(classGroups != null, "Class groups cannot be null.");
        Contracts.Require(allProfiles != null, "Profiles cannot be null.");

        var profileList = allProfiles as IReadOnlyList<StudentCourseProfile> ?? allProfiles.ToList();
        var profilesByGroup = profileList
            .Where(p => p.ClassGroupId.HasValue)
            .GroupBy(p => p.ClassGroupId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var computed = new List<(CourseClassGroup Group, int StudentCount, int TotalGreen, int ClassScore, int TotalXp, int? UserContribution)>();

        foreach (var group in classGroups)
        {
            var groupStudents = profilesByGroup.GetValueOrDefault(group.Id, []);
            int studentCount = groupStudents.Count;
            int totalXp = groupStudents.Sum(p => p.TotalXp);
            int totalGreen = groupStudents.Sum(p => p.TotalGreenTests);
            var (classScore, currentUserContribution) = CourseClassGroup.CalculateClassScore(groupStudents, currentUserId);

            computed.Add((group, studentCount, totalGreen, classScore, totalXp, currentUserContribution));
        }

        var ordered = computed
            .OrderByDescending(c => c.ClassScore)
            .ThenByDescending(c => c.TotalGreen)
            .ThenByDescending(c => c.TotalXp)
            .ThenBy(c => c.Group.Name)
            .ToList();

        var result = new List<ClassGroupStanding>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            result.Add(new ClassGroupStanding(
                rank: i + 1,
                classGroup: item.Group,
                studentCount: item.StudentCount,
                totalGreenTests: item.TotalGreen,
                classScore: item.ClassScore,
                totalXp: item.TotalXp,
                currentUserContribution: item.UserContribution));
        }

        return result;
    }
}

