using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NugetCacheMcpServer.Services;
using NugetCacheMcpServer.Utilities;

namespace NugetCacheMcpServer.Tools;

/// <summary>
/// MCP tool for getting package metadata.
/// </summary>
[McpServerToolType]
public class PackageInfoTool
{
    private readonly IPackageIndex _packageIndex;
    private readonly INuspecParser _nuspecParser;

    public PackageInfoTool(IPackageIndex packageIndex, INuspecParser nuspecParser)
    {
        _packageIndex = packageIndex;
        _nuspecParser = nuspecParser;
    }

    /// <summary>
    /// Checks if a package is a meta-package (contains no DLLs, only dependencies).
    /// </summary>
    private static bool IsMetaPackage(string packagePath, IReadOnlyList<string> frameworks)
    {
        // Check if any framework has actual DLL files
        foreach (var framework in frameworks)
        {
            var assemblyPath = FrameworkSelector.GetAssemblyPath(packagePath, framework, "");
            if (assemblyPath != null)
                return false;
        }
        return true;
    }

    [McpServerTool(Name = "get_package_info")]
    [Description("Get package metadata including description, authors, license, dependencies, and available frameworks. Use this to understand what a package does and its requirements.")]
    public string GetPackageInfo(
        [Description("The NuGet package ID (e.g., 'Newtonsoft.Json')")] string packageId,
        [Description("Optional specific version. If not provided, uses the latest cached version.")] string? version = null)
    {
        var package = _packageIndex.GetPackage(packageId, version);
        if (package == null)
        {
            return GetPackageNotFoundMessage(packageId, version);
        }

        var metadata = _nuspecParser.Parse(package.PackagePath, package.PackageId);
        if (metadata == null)
        {
            // Return basic info from the index if nuspec parsing fails
            var basicResult = new
            {
                packageId = package.PackageId,
                version = package.Version,
                frameworks = package.AvailableFrameworks,
                note = "Could not parse .nuspec file."
            };
            return JsonSerializer.Serialize(basicResult, CacheExplorerTool.CompactJson);
        }

        // Get all cached versions
        var versions = _packageIndex.GetPackageVersions(packageId).ToList();

        // Flatten dependencies to simple list
        var deps = metadata.DependencyGroups
            .SelectMany(g => g.Dependencies.Select(d => g.TargetFramework != null
                ? $"{d.PackageId} {d.VersionRange} ({g.TargetFramework})"
                : $"{d.PackageId} {d.VersionRange}"))
            .Distinct()
            .ToList();

        // Check if this is a meta-package (no code, only dependencies)
        var isMetaPkg = IsMetaPackage(package.PackagePath, metadata.AvailableFrameworks);

        var result = new
        {
            packageId = metadata.PackageId,
            version = metadata.Version,
            description = metadata.Description,
            authors = metadata.Authors,
            projectUrl = metadata.ProjectUrl,
            license = metadata.License ?? metadata.LicenseUrl,
            frameworks = metadata.AvailableFrameworks,
            cachedVersions = versions,
            dependencies = deps.Count > 0 ? deps : null,
            isMetaPackage = isMetaPkg ? true : (bool?)null,
            warning = isMetaPkg
                ? "This is a META-PACKAGE that contains NO code, only dependencies. Do NOT use list_types on this package - it will return empty results. Instead, explore the dependencies listed above to find the actual packages with code."
                : null
        };

        return JsonSerializer.Serialize(result, CacheExplorerTool.CompactJson);
    }

    private string GetPackageNotFoundMessage(string packageId, string? version)
    {
        var message = version != null
            ? $"Package '{packageId}' version '{version}' not found in the local NuGet cache."
            : $"Package '{packageId}' not found in the local NuGet cache.";

        message += "\n\nTo cache this package, run 'dotnet restore' in a project that references it.";

        // Suggest similar packages
        var similar = _packageIndex.GetPackages(packageId.Split('.').First(), 5)
            .Where(p => !p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        if (similar.Count > 0)
        {
            message += "\n\nSimilar packages in cache:\n" +
                       string.Join("\n", similar.Select(p => $"  - {p.PackageId} ({p.LatestVersion})"));
        }

        return message;
    }
}
