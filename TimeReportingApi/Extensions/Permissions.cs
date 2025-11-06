namespace TimeReportingApi.Extensions;

/// <summary>
/// Standard permission abbreviations used in ACL entries.
/// </summary>
public static class Permissions
{
    /// <summary>View permission - read access to resources</summary>
    public const string View = "V";

    /// <summary>Edit permission - modify time entries and resources</summary>
    public const string Edit = "E";

    /// <summary>Approve permission - approve or decline time entries</summary>
    public const string Approve = "A";

    /// <summary>Manage permission - administrative operations</summary>
    public const string Manage = "M";

    /// <summary>Track permission - log new time entries</summary>
    public const string Track = "T";
}
