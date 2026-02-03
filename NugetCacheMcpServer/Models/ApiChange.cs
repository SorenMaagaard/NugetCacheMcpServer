namespace NugetCacheMcpServer.Models;

/// <summary>
/// Type of API change between versions.
/// </summary>
public enum ApiChangeKind
{
    Added,
    Removed,
    Modified
}

/// <summary>
/// Represents a change in the API between two versions.
/// </summary>
public class ApiChange
{
    public required ApiChangeKind Kind { get; init; }
    public required string MemberType { get; init; }
    public required string MemberName { get; init; }
    public string? OldSignature { get; init; }
    public string? NewSignature { get; init; }
    public bool IsBreakingChange { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Summary of API changes between two package versions.
/// </summary>
public class VersionComparison
{
    public required string PackageId { get; init; }
    public required string FromVersion { get; init; }
    public required string ToVersion { get; init; }
    public List<ApiChange> TypeChanges { get; init; } = [];
    public List<ApiChange> MemberChanges { get; init; } = [];
    public int AddedTypesCount { get; init; }
    public int RemovedTypesCount { get; init; }
    public int AddedMembersCount { get; init; }
    public int RemovedMembersCount { get; init; }
    public int ModifiedMembersCount { get; init; }
    public bool HasBreakingChanges { get; init; }
}
