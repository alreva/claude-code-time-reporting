namespace TimeReportingApi.Extensions;

/// <summary>
/// Represents a single ACL (Access Control List) entry from the JWT token.
/// Format: "Path=Perm1,Perm2" (e.g., "Project/INTERNAL=V,A,M")
/// </summary>
/// <param name="Path">Hierarchical resource path (e.g., "Project/INTERNAL/Task/17")</param>
/// <param name="Permissions">Array of permission abbreviations (e.g., ["V", "A", "M"])</param>
public record AclEntry(string Path, string[] Permissions);
