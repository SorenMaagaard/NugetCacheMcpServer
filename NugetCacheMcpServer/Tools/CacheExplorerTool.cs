using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using NugetCacheMcpServer.Services;

namespace NugetCacheMcpServer.Tools;

/// <summary>
/// MCP tool for exploring the local NuGet cache.
/// </summary>
[McpServerToolType]
public class CacheExplorerTool
{
    private readonly IPackageIndex _packageIndex;

    // Compact JSON options - no indentation, skip nulls/defaults
    internal static readonly JsonSerializerOptions CompactJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CacheExplorerTool(IPackageIndex packageIndex)
    {
        _packageIndex = packageIndex;
    }

    /// <summary>
    /// Encodes pagination state into an opaque cursor token.
    /// </summary>
    internal static string EncodeCursor(int skip, string? filter = null)
    {
        var state = new { s = skip, f = filter };
        var json = JsonSerializer.Serialize(state);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Decodes an opaque cursor token into pagination state.
    /// </summary>
    internal static (int skip, string? filter) DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return (0, null);

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            using var doc = JsonDocument.Parse(json);
            var skip = doc.RootElement.GetProperty("s").GetInt32();
            var filter = doc.RootElement.TryGetProperty("f", out var f) && f.ValueKind != JsonValueKind.Null
                ? f.GetString()
                : null;
            return (skip, filter);
        }
        catch
        {
            throw new ArgumentException("Invalid cursor token");
        }
    }

    [McpServerTool(Name = "list_cached_packages")]
    [Description("List all NuGet packages available in the local cache with their versions. Use this to discover what packages are available for inspection.")]
    public string ListCachedPackages(
        [Description("Optional filter to search for packages by name (case-insensitive partial match)")] string? filter = null,
        [Description("Opaque cursor for pagination. Pass the nextCursor from a previous response to get the next page.")] string? cursor = null,
        [Description("Maximum number of packages to return per page (default: 50)")] int pageSize = 50)
    {
        // Decode cursor if provided (cursor overrides filter parameter)
        var (skip, cursorFilter) = DecodeCursor(cursor);
        var effectiveFilter = cursor != null ? cursorFilter : filter;

        var allPackages = _packageIndex.GetPackages(effectiveFilter).ToList();
        var packages = allPackages.Skip(skip).Take(pageSize).ToList();

        if (packages.Count == 0 && skip == 0)
        {
            if (!string.IsNullOrEmpty(effectiveFilter))
            {
                return $"No packages matching '{effectiveFilter}'. Run 'dotnet restore' to cache packages.";
            }
            return "No packages in cache. Run 'dotnet restore' in a .NET project.";
        }

        var nextSkip = skip + packages.Count;
        var hasMore = nextSkip < allPackages.Count;

        var result = new
        {
            totalCount = allPackages.Count,
            returnedCount = packages.Count,
            nextCursor = hasMore ? EncodeCursor(nextSkip, effectiveFilter) : null,
            packages = packages.Select(p =>
            {
                var versions = p.Versions.Take(5).ToList();
                return new
                {
                    packageId = p.PackageId,
                    latestVersion = p.LatestVersion,
                    versions,
                    moreVersions = p.Versions.Count > 5 ? p.Versions.Count - 5 : (int?)null
                };
            })
        };

        return JsonSerializer.Serialize(result, CompactJson);
    }
}
