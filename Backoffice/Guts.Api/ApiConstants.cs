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

    /// <summary>
    /// Name for the policy that allows only users with the role 'admin'.
    /// </summary>
    public const string AdminOnlyPolicy = "AdminOnlyPolicy";

    /// <summary>
    /// Name for the policy that allows users with the role 'lector' or 'admin'.
    /// </summary>
    public const string LectorOrAdminPolicy = "LectorOrAdminPolicy";

    /// <summary>
    /// Cookie authentication scheme dedicated to the Admin-only Quartz Dashboard.
    /// </summary>
    public const string QuartzDashboardCookieScheme = "QuartzDashboardCookie";

    /// <summary>
    /// Authorization policy restricting the Quartz Dashboard to authenticated administrators.
    /// </summary>
    public const string QuartzDashboardAdminPolicy = "QuartzDashboardAdminPolicy";
}