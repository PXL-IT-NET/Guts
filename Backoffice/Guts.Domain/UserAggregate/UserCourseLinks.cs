using System.Collections.Generic;

namespace Guts.Domain.UserAggregate;

public sealed class UserCourseLinks : ValueObject<UserCourseLinks>
{
    public bool HasTestRuns { get; }
    public bool HasTestResults { get; }
    public bool HasSolutionFiles { get; }
    public bool HasProjectTeamMemberships { get; }
    public bool HasStudentCourseProfiles { get; }
    public bool HasPeerAssessments { get; }

    public bool HasAnyLinks =>
        HasTestRuns ||
        HasTestResults ||
        HasSolutionFiles ||
        HasProjectTeamMemberships ||
        HasStudentCourseProfiles ||
        HasPeerAssessments;

    public UserCourseLinks(
        bool hasTestRuns,
        bool hasTestResults,
        bool hasSolutionFiles,
        bool hasProjectTeamMemberships,
        bool hasStudentCourseProfiles,
        bool hasPeerAssessments)
    {
        HasTestRuns = hasTestRuns;
        HasTestResults = hasTestResults;
        HasSolutionFiles = hasSolutionFiles;
        HasProjectTeamMemberships = hasProjectTeamMemberships;
        HasStudentCourseProfiles = hasStudentCourseProfiles;
        HasPeerAssessments = hasPeerAssessments;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return HasTestRuns;
        yield return HasTestResults;
        yield return HasSolutionFiles;
        yield return HasProjectTeamMemberships;
        yield return HasStudentCourseProfiles;
        yield return HasPeerAssessments;
    }
}
