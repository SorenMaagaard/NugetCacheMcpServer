namespace NugetCacheMcpServer.Models;

/// <summary>
/// Information about a package in the local NuGet cache.
/// </summary>
public class CachedPackageInfo
{
    public required string PackageId { get; init; }
    public required string Version { get; init; }
    public required string PackagePath { get; init; }
    public List<string> AvailableFrameworks { get; init; } = [];
    public bool HasXmlDocumentation { get; init; }
}

/// <summary>
/// Summary of a cached package with all its versions.
/// </summary>
public class CachedPackageSummary
{
    public required string PackageId { get; init; }
    public required List<string> Versions { get; init; }
    public required string LatestVersion { get; init; }
}
