using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace NugetCacheMcpServer.Cache;

/// <summary>
/// Custom AssemblyLoadContext that resolves assemblies from the NuGet cache.
/// </summary>
internal sealed class NuGetCacheAssemblyLoadContext : AssemblyLoadContext
{
    private readonly Dictionary<string, string> _assemblyPaths;
    private readonly ILogger? _logger;

    public NuGetCacheAssemblyLoadContext(Dictionary<string, string> assemblyPaths, ILogger? logger = null)
        : base(isCollectible: true)
    {
        _assemblyPaths = assemblyPaths;
        _logger = logger;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (name != null && _assemblyPaths.TryGetValue(name, out var path))
        {
            try
            {
                return LoadFromAssemblyPath(path);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Failed to load assembly {Name} from {Path}", name, path);
            }
        }

        return null;
    }

    /// <summary>
    /// Builds an assembly path dictionary from a NuGet cache directory.
    /// </summary>
    public static Dictionary<string, string> BuildAssemblyPathsFromCache(string cachePath)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(cachePath))
            return paths;

        // Add runtime assemblies first
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (!string.IsNullOrEmpty(runtimeDir) && Directory.Exists(runtimeDir))
        {
            foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(dll);
                if (!string.IsNullOrEmpty(name))
                {
                    paths[name] = dll;
                }
            }
        }

        // Scan NuGet cache for package assemblies
        foreach (var packageDir in Directory.GetDirectories(cachePath))
        {
            var packageName = Path.GetFileName(packageDir);
            if (string.IsNullOrEmpty(packageName) || packageName.StartsWith("."))
                continue;

            // Get the latest version directory
            var versionDirs = Directory.GetDirectories(packageDir)
                .OrderByDescending(v => v)
                .ToList();

            foreach (var versionDir in versionDirs.Take(1))
            {
                AddAssembliesFromPackageVersion(paths, versionDir);
            }
        }

        return paths;
    }

    private static void AddAssembliesFromPackageVersion(Dictionary<string, string> paths, string versionDir)
    {
        // Check lib folder
        var libPath = Path.Combine(versionDir, "lib");
        if (Directory.Exists(libPath))
        {
            var frameworkDir = Directory.GetDirectories(libPath)
                .OrderByDescending(GetFrameworkPriority)
                .FirstOrDefault();

            if (frameworkDir != null)
            {
                foreach (var dll in Directory.GetFiles(frameworkDir, "*.dll"))
                {
                    var name = Path.GetFileNameWithoutExtension(dll);
                    if (!string.IsNullOrEmpty(name) && !paths.ContainsKey(name))
                    {
                        paths[name] = dll;
                    }
                }
            }
        }

        // Check ref folder
        var refPath = Path.Combine(versionDir, "ref");
        if (Directory.Exists(refPath))
        {
            var frameworkDir = Directory.GetDirectories(refPath)
                .OrderByDescending(GetFrameworkPriority)
                .FirstOrDefault();

            if (frameworkDir != null)
            {
                foreach (var dll in Directory.GetFiles(frameworkDir, "*.dll"))
                {
                    var name = Path.GetFileNameWithoutExtension(dll);
                    if (!string.IsNullOrEmpty(name) && !paths.ContainsKey(name))
                    {
                        paths[name] = dll;
                    }
                }
            }
        }
    }

    private static int GetFrameworkPriority(string frameworkPath)
    {
        var name = Path.GetFileName(frameworkPath)?.ToLowerInvariant() ?? "";

        return name switch
        {
            "net10.0" => 100,
            "net9.0" => 99,
            "net8.0" => 98,
            "net7.0" => 97,
            "net6.0" => 96,
            "net5.0" => 95,
            "netcoreapp3.1" => 80,
            "netcoreapp3.0" => 79,
            "netstandard2.1" => 70,
            "netstandard2.0" => 69,
            _ when name.StartsWith("net") => 40,
            _ => 0
        };
    }
}
