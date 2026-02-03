using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using NugetCacheMcpServer.Configuration;
using NugetCacheMcpServer.Models;
using NugetCacheMcpServer.Utilities;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Indexes and queries the local NuGet package cache.
/// </summary>
public class PackageIndex : IPackageIndex
{
    private readonly CacheOptions _options;
    private readonly ILogger<PackageIndex> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CachedPackageInfo>> _packages = new(StringComparer.OrdinalIgnoreCase);

    public PackageIndex(IOptions<CacheOptions> options, ILogger<PackageIndex> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int PackageCount => _packages.Count;

    /// <summary>
    /// Indexes the NuGet cache directory on startup.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        _packages.Clear();

        if (!Directory.Exists(_options.CachePath))
        {
            _logger.LogWarning("NuGet cache directory not found: {CachePath}", _options.CachePath);
            return;
        }

        _logger.LogInformation("Indexing NuGet cache at: {CachePath}", _options.CachePath);

        await Task.Run(() =>
        {
            var packageDirs = Directory.GetDirectories(_options.CachePath);
            var packageCount = 0;
            var versionCount = 0;

            foreach (var packageDir in packageDirs)
            {
                var packageId = Path.GetFileName(packageDir);
                if (string.IsNullOrEmpty(packageId) || packageId.StartsWith("."))
                    continue;

                var versionDirs = Directory.GetDirectories(packageDir);
                var packageVersions = new ConcurrentDictionary<string, CachedPackageInfo>(StringComparer.OrdinalIgnoreCase);

                foreach (var versionDir in versionDirs)
                {
                    var version = Path.GetFileName(versionDir);
                    if (string.IsNullOrEmpty(version))
                        continue;

                    var frameworks = FrameworkSelector.GetAvailableFrameworks(versionDir);
                    var hasXml = frameworks.Any(fw =>
                    {
                        var asmPath = FrameworkSelector.GetAssemblyPath(versionDir, fw, packageId);
                        return asmPath != null && FrameworkSelector.GetXmlDocumentationPath(asmPath) != null;
                    });

                    var info = new CachedPackageInfo
                    {
                        PackageId = packageId,
                        Version = version,
                        PackagePath = versionDir,
                        AvailableFrameworks = frameworks,
                        HasXmlDocumentation = hasXml
                    };

                    packageVersions[version] = info;
                    versionCount++;
                }

                if (packageVersions.Count > 0)
                {
                    _packages[packageId] = packageVersions;
                    packageCount++;
                }
            }

            _logger.LogInformation("Indexed {PackageCount} packages with {VersionCount} total versions", packageCount, versionCount);
        });
    }

    public IEnumerable<CachedPackageSummary> GetPackages(string? filter = null, int? maxResults = null)
    {
        var query = _packages.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(p => p.Key.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        if (maxResults.HasValue)
        {
            query = query.Take(maxResults.Value);
        }

        return query
            .Select(p =>
            {
                var versions = p.Value.Keys
                    .Select(v => NuGetVersion.TryParse(v, out var nv) ? nv : null)
                    .Where(v => v != null)
                    .OrderByDescending(v => v)
                    .Select(v => v!.ToString())
                    .ToList();

                return new CachedPackageSummary
                {
                    PackageId = p.Key,
                    Versions = versions,
                    LatestVersion = versions.FirstOrDefault() ?? p.Value.Keys.First()
                };
            })
            .OrderBy(p => p.PackageId);
    }

    public CachedPackageInfo? GetPackage(string packageId, string? version = null)
    {
        if (!_packages.TryGetValue(packageId, out var versions))
            return null;

        if (!string.IsNullOrEmpty(version))
        {
            return versions.TryGetValue(version, out var info) ? info : null;
        }

        // Return latest version
        var latestVersion = versions.Keys
            .Select(v => NuGetVersion.TryParse(v, out var nv) ? nv : null)
            .Where(v => v != null)
            .OrderByDescending(v => v)
            .FirstOrDefault();

        if (latestVersion != null && versions.TryGetValue(latestVersion.ToString(), out var latestInfo))
            return latestInfo;

        return versions.Values.FirstOrDefault();
    }

    public IEnumerable<string> GetPackageVersions(string packageId)
    {
        if (!_packages.TryGetValue(packageId, out var versions))
            return [];

        return versions.Keys
            .Select(v => NuGetVersion.TryParse(v, out var nv) ? nv : null)
            .Where(v => v != null)
            .OrderByDescending(v => v)
            .Select(v => v!.ToString());
    }

    public bool PackageExists(string packageId, string? version = null)
    {
        if (!_packages.TryGetValue(packageId, out var versions))
            return false;

        if (string.IsNullOrEmpty(version))
            return true;

        return versions.ContainsKey(version);
    }

    public string? GetPackagePath(string packageId, string version)
    {
        if (_packages.TryGetValue(packageId, out var versions) &&
            versions.TryGetValue(version, out var info))
        {
            return info.PackagePath;
        }
        return null;
    }
}
