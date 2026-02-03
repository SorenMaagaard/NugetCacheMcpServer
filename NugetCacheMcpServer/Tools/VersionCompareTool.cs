using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NugetCacheMcpServer.Models;
using NugetCacheMcpServer.Services;
using NugetCacheMcpServer.Utilities;

namespace NugetCacheMcpServer.Tools;

/// <summary>
/// MCP tool for comparing package versions.
/// </summary>
[McpServerToolType]
public class VersionCompareTool
{
    private readonly IPackageIndex _packageIndex;
    private readonly IAssemblyInspector _assemblyInspector;

    public VersionCompareTool(IPackageIndex packageIndex, IAssemblyInspector assemblyInspector)
    {
        _packageIndex = packageIndex;
        _assemblyInspector = assemblyInspector;
    }

    [McpServerTool(Name = "compare_package_versions")]
    [Description("Compare the API between two versions of a NuGet package to see what changed (added/removed/modified types and members). Useful for understanding breaking changes when upgrading.")]
    public string ComparePackageVersions(
        [Description("The NuGet package ID (e.g., 'Newtonsoft.Json')")] string packageId,
        [Description("The older version to compare from")] string fromVersion,
        [Description("The newer version to compare to")] string toVersion,
        [Description("Preferred target framework (e.g., 'net8.0'). Auto-selects best available if not specified.")] string? framework = null,
        [Description("Maximum number of changes per category to return per page (default: 30)")] int pageSize = 30)
    {
        // Validate both versions exist
        var fromPackage = _packageIndex.GetPackage(packageId, fromVersion);
        if (fromPackage == null)
        {
            return GetVersionNotFoundMessage(packageId, fromVersion);
        }

        var toPackage = _packageIndex.GetPackage(packageId, toVersion);
        if (toPackage == null)
        {
            return GetVersionNotFoundMessage(packageId, toVersion);
        }

        // Select frameworks
        var fromFramework = FrameworkSelector.SelectBestFramework(fromPackage.AvailableFrameworks, framework);
        var toFramework = FrameworkSelector.SelectBestFramework(toPackage.AvailableFrameworks, framework);

        if (fromFramework == null || toFramework == null)
        {
            return $"Could not find compatible frameworks for comparison.\n" +
                   $"Version {fromVersion} frameworks: {string.Join(", ", fromPackage.AvailableFrameworks)}\n" +
                   $"Version {toVersion} frameworks: {string.Join(", ", toPackage.AvailableFrameworks)}";
        }

        // Get assembly paths
        var fromAssemblyPath = FrameworkSelector.GetAssemblyPath(fromPackage.PackagePath, fromFramework, packageId);
        var toAssemblyPath = FrameworkSelector.GetAssemblyPath(toPackage.PackagePath, toFramework, packageId);

        if (fromAssemblyPath == null || toAssemblyPath == null)
        {
            return $"Could not find assemblies for comparison.\n" +
                   $"Ensure both versions have DLLs in the lib folder.";
        }

        // Compare assemblies
        List<ApiChange> changes;
        string? comparisonError = null;
        try
        {
            changes = _assemblyInspector.CompareAssemblies(fromAssemblyPath, toAssemblyPath).ToList();
        }
        catch (Exception ex) when (ex.Message.Contains("Could not find assembly") ||
                                   ex.Message.Contains("FileNotFoundException"))
        {
            // Missing dependency assembly - provide helpful message
            comparisonError = $"Could not load all dependency assemblies for comparison. " +
                             $"This typically happens when referenced packages are not in the NuGet cache. " +
                             $"Error: {ex.Message}";
            changes = [];
        }

        // Categorize changes
        var typeChanges = changes.Where(c => c.MemberType is "Class" or "Interface" or "Struct" or "Enum" or "Delegate").ToList();
        var memberChanges = changes.Where(c => c.MemberType is "Method" or "Property" or "Field" or "Event").ToList();

        var comparison = new VersionComparison
        {
            PackageId = packageId,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            TypeChanges = typeChanges,
            MemberChanges = memberChanges,
            AddedTypesCount = typeChanges.Count(c => c.Kind == ApiChangeKind.Added),
            RemovedTypesCount = typeChanges.Count(c => c.Kind == ApiChangeKind.Removed),
            AddedMembersCount = memberChanges.Count(c => c.Kind == ApiChangeKind.Added),
            RemovedMembersCount = memberChanges.Count(c => c.Kind == ApiChangeKind.Removed),
            ModifiedMembersCount = memberChanges.Count(c => c.Kind == ApiChangeKind.Modified),
            HasBreakingChanges = changes.Any(c => c.IsBreakingChange)
        };

        // Compact format - just signatures, grouped by change type
        var breakingAll = changes.Where(c => c.IsBreakingChange).ToList();
        var breaking = breakingAll.Take(pageSize)
            .Select(c => c.Kind == ApiChangeKind.Modified
                ? $"{c.OldSignature} -> {c.NewSignature}"
                : c.OldSignature ?? c.NewSignature)
            .ToList();

        var addedAll = changes.Where(c => c.Kind == ApiChangeKind.Added && !c.IsBreakingChange).ToList();
        var added = addedAll.Take(pageSize)
            .Select(c => c.NewSignature)
            .ToList();

        var removedAll = changes.Where(c => c.Kind == ApiChangeKind.Removed && !c.IsBreakingChange).ToList();
        var removed = removedAll.Take(pageSize)
            .Select(c => c.OldSignature)
            .ToList();

        var modifiedAll = changes.Where(c => c.Kind == ApiChangeKind.Modified && !c.IsBreakingChange).ToList();
        var modified = modifiedAll.Take(pageSize)
            .Select(c => $"{c.OldSignature} -> {c.NewSignature}")
            .ToList();

        // For comparison results, we provide counts and truncation info rather than cursors
        // since the data is computed fresh each time
        var result = new
        {
            packageId = comparison.PackageId,
            fromVersion = $"{comparison.FromVersion} ({fromFramework})",
            toVersion = $"{comparison.ToVersion} ({toFramework})",
            error = comparisonError,
            summary = new
            {
                totalChanges = changes.Count,
                breakingCount = comparison.HasBreakingChanges ? breakingAll.Count : (int?)null,
                addedCount = comparison.AddedTypesCount + comparison.AddedMembersCount,
                removedCount = comparison.RemovedTypesCount + comparison.RemovedMembersCount,
                modifiedCount = comparison.ModifiedMembersCount
            },
            breakingChanges = breaking.Count > 0 ? breaking : null,
            breakingTruncated = breakingAll.Count > pageSize ? breakingAll.Count - pageSize : (int?)null,
            added = added.Count > 0 ? added : null,
            addedTruncated = addedAll.Count > pageSize ? addedAll.Count - pageSize : (int?)null,
            removed = removed.Count > 0 ? removed : null,
            removedTruncated = removedAll.Count > pageSize ? removedAll.Count - pageSize : (int?)null,
            modified = modified.Count > 0 ? modified : null,
            modifiedTruncated = modifiedAll.Count > pageSize ? modifiedAll.Count - pageSize : (int?)null
        };

        return JsonSerializer.Serialize(result, CacheExplorerTool.CompactJson);
    }

    private string GetVersionNotFoundMessage(string packageId, string version)
    {
        var message = $"Package '{packageId}' version '{version}' not found in the local NuGet cache.";

        var versions = _packageIndex.GetPackageVersions(packageId).ToList();
        if (versions.Count > 0)
        {
            message += "\n\nAvailable cached versions:\n" +
                       string.Join("\n", versions.Take(10).Select(v => $"  - {v}"));

            if (versions.Count > 10)
            {
                message += $"\n  ... and {versions.Count - 10} more";
            }
        }
        else
        {
            message += "\n\nThis package is not in the cache. Run 'dotnet restore' in a project that references it.";
        }

        return message;
    }
}
