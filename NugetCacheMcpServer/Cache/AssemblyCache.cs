using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NugetCacheMcpServer.Configuration;

namespace NugetCacheMcpServer.Cache;

/// <summary>
/// LRU cache for loaded MetadataLoadContext instances.
/// </summary>
public class AssemblyCache : IDisposable
{
    private readonly int _maxEntries;
    private readonly string _nugetCachePath;
    private readonly ILogger<AssemblyCache> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly LinkedList<string> _lruList = new();
    private readonly object _lruLock = new();

    // Lazy-loaded assembly paths from NuGet cache (thread-safe initialization)
    private Dictionary<string, string>? _nugetAssemblyPaths;
    private readonly object _nugetPathsLock = new();

    public AssemblyCache(IOptions<CacheOptions> options, ILogger<AssemblyCache> logger)
    {
        _maxEntries = options.Value.MaxCachedAssemblies;
        _nugetCachePath = options.Value.CachePath;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a MetadataLoadContext for the specified assembly.
    /// </summary>
    public (MetadataLoadContext Context, Assembly Assembly) GetOrCreate(string assemblyPath)
    {
        var key = assemblyPath.ToLowerInvariant();

        if (_cache.TryGetValue(key, out var entry))
        {
            TouchEntry(key);
            return (entry.Context, entry.Assembly);
        }

        // Create new context with comprehensive assembly resolution
        var paths = GetAssemblyPaths(assemblyPath);
        var resolver = new PathAssemblyResolver(paths);
        var context = new MetadataLoadContext(resolver);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);

        var newEntry = new CacheEntry(context, assembly);
        _cache[key] = newEntry;
        AddToLru(key);

        // Evict if necessary
        EvictIfNeeded();

        _logger.LogDebug("Loaded assembly: {AssemblyPath}", assemblyPath);

        return (context, assembly);
    }

    private IEnumerable<string> GetAssemblyPaths(string assemblyPath)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. Add core runtime assemblies first (highest priority)
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

        // 2. Add assemblies from NuGet cache (take a snapshot for thread-safety)
        var nugetPaths = GetNuGetAssemblyPaths();
        foreach (var (name, path) in nugetPaths)
        {
            // Don't override runtime assemblies
            if (!paths.ContainsKey(name))
            {
                paths[name] = path;
            }
        }

        // 3. Add the target assembly's directory (may override NuGet cache)
        var dir = Path.GetDirectoryName(assemblyPath);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            foreach (var dll in Directory.GetFiles(dir, "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(dll);
                if (!string.IsNullOrEmpty(name))
                {
                    paths[name] = dll;
                }
            }
        }

        // 4. Add the target assembly itself
        var targetName = Path.GetFileNameWithoutExtension(assemblyPath);
        if (!string.IsNullOrEmpty(targetName))
        {
            paths[targetName] = assemblyPath;
        }

        return paths.Values;
    }

    /// <summary>
    /// Gets a thread-safe snapshot of NuGet assembly paths.
    /// </summary>
    private KeyValuePair<string, string>[] GetNuGetAssemblyPaths()
    {
        EnsureNuGetAssemblyPaths();
        lock (_nugetPathsLock)
        {
            return _nugetAssemblyPaths?.ToArray() ?? [];
        }
    }

    private void EnsureNuGetAssemblyPaths()
    {
        // Double-checked locking for thread-safe lazy initialization
        if (_nugetAssemblyPaths != null)
            return;

        lock (_nugetPathsLock)
        {
            if (_nugetAssemblyPaths != null)
                return;

            _nugetAssemblyPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Scan configured NuGet cache
            ScanNuGetCache(_nugetCachePath);

            // Also scan default NuGet cache if different
            var defaultCache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages");

            if (!defaultCache.Equals(_nugetCachePath, StringComparison.OrdinalIgnoreCase))
            {
                ScanNuGetCache(defaultCache);
            }

            _logger.LogDebug("Indexed {Count} assemblies from NuGet cache", _nugetAssemblyPaths.Count);
        }
    }

    private void ScanNuGetCache(string cachePath)
    {
        if (!Directory.Exists(cachePath))
            return;

        try
        {
            foreach (var packageDir in Directory.GetDirectories(cachePath))
            {
                var packageName = Path.GetFileName(packageDir);
                if (string.IsNullOrEmpty(packageName) || packageName.StartsWith("."))
                    continue;

                // Get the latest version
                var versionDirs = Directory.GetDirectories(packageDir)
                    .OrderByDescending(v => v)
                    .Take(1);

                foreach (var versionDir in versionDirs)
                {
                    AddAssembliesFromPackageVersion(versionDir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning NuGet cache at {Path}", cachePath);
        }
    }

    private void AddAssembliesFromPackageVersion(string versionDir)
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
                    if (!string.IsNullOrEmpty(name) && !_nugetAssemblyPaths!.ContainsKey(name))
                    {
                        _nugetAssemblyPaths[name] = dll;
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
                    if (!string.IsNullOrEmpty(name) && !_nugetAssemblyPaths!.ContainsKey(name))
                    {
                        _nugetAssemblyPaths[name] = dll;
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
            "netstandard1.6" => 60,
            _ when name.StartsWith("net") => 40,
            _ => 0
        };
    }

    private void TouchEntry(string key)
    {
        lock (_lruLock)
        {
            _lruList.Remove(key);
            _lruList.AddFirst(key);
        }
    }

    private void AddToLru(string key)
    {
        lock (_lruLock)
        {
            _lruList.AddFirst(key);
        }
    }

    private void EvictIfNeeded()
    {
        while (_cache.Count > _maxEntries)
        {
            string? keyToRemove;
            lock (_lruLock)
            {
                if (_lruList.Last == null)
                    break;

                keyToRemove = _lruList.Last.Value;
                _lruList.RemoveLast();
            }

            if (_cache.TryRemove(keyToRemove, out var entry))
            {
                entry.Context.Dispose();
                _logger.LogDebug("Evicted assembly from cache: {Key}", keyToRemove);
            }
        }
    }

    public void Dispose()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Context.Dispose();
        }
        _cache.Clear();
        GC.SuppressFinalize(this);
    }

    private record CacheEntry(MetadataLoadContext Context, Assembly Assembly);
}
