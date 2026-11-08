using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;

namespace Guts.Domain.GamificationAggregate;

public class CourseClassGroup : AggregateRoot
{
    public const int JoinBonusPoints = 5;

    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            Contracts.Require(!string.IsNullOrWhiteSpace(value), "Class name cannot be empty.");
            _name = value.Trim();
        }
    }

    public int CourseId { get; set; }
    public Course Course { get; set; }

    public int PeriodId { get; set; }
    public Period Period { get; set; }

    public CourseClassGroup() { }

    public CourseClassGroup(string name, int courseId, int periodId)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(name), "Class name cannot be empty.");
        Contracts.Require(courseId > 0, "CourseId must be positive.");
        Contracts.Require(periodId > 0, "PeriodId must be positive.");

        _name = name.Trim();
        CourseId = courseId;
        PeriodId = periodId;
    }

    public static double GetContributionWeightForRank(int withinClassRank)
    {
        Contracts.Require(withinClassRank > 0, "Within-class rank must be positive.");

        return withinClassRank switch
        {
            <= 5 => 1.00,
            <= 10 => 0.60,
            <= 15 => 0.30,
            _ => 0.15
        };
    }

    public static int CalculateStudentContribution(int totalXp, int withinClassRank)
    {
        Contracts.Require(totalXp >= 0, "Total XP cannot be negative.");
        Contracts.Require(withinClassRank > 0, "Within-class rank must be positive.");

        double weight = GetContributionWeightForRank(withinClassRank);
        return JoinBonusPoints + (int)Math.Round(totalXp * weight, MidpointRounding.AwayFromZero);
    }

    public static (int ClassScore, int? CurrentUserContribution) CalculateClassScore(
        IEnumerable<StudentCourseProfile> classProfiles,
        int? currentUserId = null)
    {
        Contracts.Require(classProfiles != null, "Class profiles cannot be null.");

        var orderedProfiles = classProfiles
            .OrderByDescending(p => p.TotalXp)
            .ThenBy(p => p.LastXpEarnedAt)
            .ThenBy(p => p.Id)
            .ToList();

        int classScore = 0;
        int? currentUserContribution = null;

        for (int i = 0; i < orderedProfiles.Count; i++)
        {
            var profile = orderedProfiles[i];
            int contribution = CalculateStudentContribution(profile.TotalXp, i + 1);
            classScore += contribution;

            if (currentUserId.HasValue && profile.UserId == currentUserId.Value)
            {
                currentUserContribution = contribution;
            }
        }

        return (classScore, currentUserContribution);
    }

    public class Factory : ICourseClassGroupFactory
    {
        public CourseClassGroup CreateNew(string name, int courseId, int periodId)
        {
            return new CourseClassGroup(name, courseId, periodId);
        }
    }
}