namespace NugetCacheMcpServer.Configuration;

/// <summary>
/// Configuration options for NuGet cache operations.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Path to the local NuGet packages cache.
    /// Defaults to %USERPROFILE%\.nuget\packages on Windows.
    /// </summary>
    public string CachePath { get; set; } = GetDefaultCachePath();

    /// <summary>
    /// Maximum number of assemblies to keep loaded in memory.
    /// </summary>
    public int MaxCachedAssemblies { get; set; } = 50;

    /// <summary>
    /// Default maximum results to return for list operations.
    /// </summary>
    public int DefaultMaxResults { get; set; } = 100;

    private static string GetDefaultCachePath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages");
    }
}
