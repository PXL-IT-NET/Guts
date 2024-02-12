namespace Guts.Api;

/// <summary>
/// Contains constants that are used in the API project.
/// </summary>
public static class ApiConstants
{
    /// <summary>
    /// Name for the policy that allows only authenticated users.
    /// </summary>
    public const string AuthenticatedUsersOnlyPolicy = "AuthenticatedUsersOnlyPolicy";

    /// <summary>
    /// Name for the policy that allows only users with the role 'lector'.
    /// </summary>
    public const string LectorsOnlyPolicy = "LectorsOnlyPolicy";
}