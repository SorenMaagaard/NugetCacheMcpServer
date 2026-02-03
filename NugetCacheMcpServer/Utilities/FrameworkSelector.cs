namespace NugetCacheMcpServer.Utilities;

/// <summary>
/// Selects the best target framework from available options.
/// </summary>
public static class FrameworkSelector
{
    // Priority order: newer .NET first, then .NET Standard, then .NET Framework
    private static readonly string[] FrameworkPriority =
    [
        "net10.0", "net9.0", "net8.0", "net7.0", "net6.0", "net5.0",
        "netcoreapp3.1", "netcoreapp3.0", "netcoreapp2.2", "netcoreapp2.1", "netcoreapp2.0",
        "netstandard2.1", "netstandard2.0", "netstandard1.6", "netstandard1.5",
        "netstandard1.4", "netstandard1.3", "netstandard1.2", "netstandard1.1", "netstandard1.0",
        "net48", "net472", "net471", "net47", "net462", "net461", "net46", "net452", "net451", "net45", "net40", "net35", "net20"
    ];

    /// <summary>
    /// Selects the best framework from available options.
    /// </summary>
    public static string? SelectBestFramework(IEnumerable<string> availableFrameworks, string? preferredFramework = null)
    {
        var frameworks = availableFrameworks.ToList();
        if (frameworks.Count == 0)
            return null;

        // If a preferred framework is specified and available, use it
        if (!string.IsNullOrEmpty(preferredFramework))
        {
            var preferred = frameworks.FirstOrDefault(f =>
                f.Equals(preferredFramework, StringComparison.OrdinalIgnoreCase));
            if (preferred != null)
                return preferred;
        }

        // Find the first matching framework from our priority list
        foreach (var priority in FrameworkPriority)
        {
            var match = frameworks.FirstOrDefault(f =>
                f.Equals(priority, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }

        // Fallback: return the first available
        return frameworks.First();
    }

    /// <summary>
    /// Gets all available frameworks from a package's lib directory.
    /// </summary>
    public static List<string> GetAvailableFrameworks(string packagePath)
    {
        var frameworks = new List<string>();

        // Check lib folder
        var libPath = Path.Combine(packagePath, "lib");
        if (Directory.Exists(libPath))
        {
            frameworks.AddRange(Directory.GetDirectories(libPath)
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrEmpty(f))!);
        }

        // Also check ref folder (reference assemblies)
        var refPath = Path.Combine(packagePath, "ref");
        if (Directory.Exists(refPath))
        {
            var refFrameworks = Directory.GetDirectories(refPath)
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrEmpty(f) && !frameworks.Contains(f, StringComparer.OrdinalIgnoreCase));
            frameworks.AddRange(refFrameworks!);
        }

        return frameworks;
    }

    /// <summary>
    /// Gets the assembly path for a specific framework in a package.
    /// </summary>
    public static string? GetAssemblyPath(string packagePath, string framework, string packageId)
    {
        // Try lib folder first
        var libPath = Path.Combine(packagePath, "lib", framework);
        if (Directory.Exists(libPath))
        {
            var dll = Directory.GetFiles(libPath, "*.dll")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .Equals(packageId, StringComparison.OrdinalIgnoreCase))
                ?? Directory.GetFiles(libPath, "*.dll").FirstOrDefault();

            if (dll != null)
                return dll;
        }

        // Try ref folder (reference assemblies)
        var refPath = Path.Combine(packagePath, "ref", framework);
        if (Directory.Exists(refPath))
        {
            var dll = Directory.GetFiles(refPath, "*.dll")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .Equals(packageId, StringComparison.OrdinalIgnoreCase))
                ?? Directory.GetFiles(refPath, "*.dll").FirstOrDefault();

            if (dll != null)
                return dll;
        }

        return null;
    }

    /// <summary>
    /// Gets the XML documentation path for an assembly if it exists.
    /// </summary>
    public static string? GetXmlDocumentationPath(string assemblyPath)
    {
        var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        return File.Exists(xmlPath) ? xmlPath : null;
    }
}
