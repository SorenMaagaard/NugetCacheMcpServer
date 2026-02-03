namespace NugetCacheMcpServer.Models;

/// <summary>
/// Package metadata extracted from .nuspec file.
/// </summary>
public class PackageMetadata
{
    public required string PackageId { get; init; }
    public required string Version { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Authors { get; init; }
    public string? Owners { get; init; }
    public string? ProjectUrl { get; init; }
    public string? LicenseUrl { get; init; }
    public string? License { get; init; }
    public string? Tags { get; init; }
    public List<PackageDependencyGroup> DependencyGroups { get; init; } = [];
    public List<string> AvailableFrameworks { get; init; } = [];
}

/// <summary>
/// A group of dependencies for a specific target framework.
/// </summary>
public class PackageDependencyGroup
{
    public string? TargetFramework { get; init; }
    public List<PackageDependency> Dependencies { get; init; } = [];
}

/// <summary>
/// A single package dependency.
/// </summary>
public class PackageDependency
{
    public required string PackageId { get; init; }
    public string? VersionRange { get; init; }
}
