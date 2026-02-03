using NugetCacheMcpServer.Models;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Interface for indexing and querying the local NuGet package cache.
/// </summary>
public interface IPackageIndex
{
    /// <summary>
    /// Gets the total number of indexed packages.
    /// </summary>
    int PackageCount { get; }

    /// <summary>
    /// Gets all cached packages, optionally filtered.
    /// </summary>
    IEnumerable<CachedPackageSummary> GetPackages(string? filter = null, int? maxResults = null);

    /// <summary>
    /// Gets information about a specific package version.
    /// </summary>
    CachedPackageInfo? GetPackage(string packageId, string? version = null);

    /// <summary>
    /// Gets all cached versions of a package.
    /// </summary>
    IEnumerable<string> GetPackageVersions(string packageId);

    /// <summary>
    /// Checks if a package exists in the cache.
    /// </summary>
    bool PackageExists(string packageId, string? version = null);

    /// <summary>
    /// Gets the path to a package in the cache.
    /// </summary>
    string? GetPackagePath(string packageId, string version);

    /// <summary>
    /// Refreshes the index by rescanning the cache directory.
    /// </summary>
    Task RefreshAsync();
}
