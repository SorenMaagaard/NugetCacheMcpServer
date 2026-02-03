using NugetCacheMcpServer.Models;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Interface for parsing .nuspec files.
/// </summary>
public interface INuspecParser
{
    /// <summary>
    /// Parses package metadata from a .nuspec file.
    /// </summary>
    PackageMetadata? Parse(string packagePath, string packageId);
}
