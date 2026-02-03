using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NugetCacheMcpServer.Models;
using NugetCacheMcpServer.Utilities;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Parses .nuspec files to extract package metadata.
/// </summary>
public class NuspecParser : INuspecParser
{
    private readonly ILogger<NuspecParser> _logger;

    public NuspecParser(ILogger<NuspecParser> logger)
    {
        _logger = logger;
    }

    public PackageMetadata? Parse(string packagePath, string packageId)
    {
        // Find the .nuspec file
        var nuspecPath = Path.Combine(packagePath, $"{packageId}.nuspec");
        if (!File.Exists(nuspecPath))
        {
            // Try to find any .nuspec file in the directory
            var nuspecFiles = Directory.GetFiles(packagePath, "*.nuspec");
            if (nuspecFiles.Length > 0)
            {
                nuspecPath = nuspecFiles[0];
            }
            else
            {
                _logger.LogDebug("No .nuspec file found in {Path}", packagePath);
                return null;
            }
        }

        try
        {
            var doc = XDocument.Load(nuspecPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            var metadata = doc.Root?.Element(ns + "metadata");

            if (metadata == null)
            {
                _logger.LogWarning("No metadata element found in {Path}", nuspecPath);
                return null;
            }

            var frameworks = FrameworkSelector.GetAvailableFrameworks(packagePath);

            return new PackageMetadata
            {
                PackageId = GetElementValue(metadata, ns, "id") ?? packageId,
                Version = GetElementValue(metadata, ns, "version") ?? "unknown",
                Title = GetElementValue(metadata, ns, "title"),
                Description = GetElementValue(metadata, ns, "description"),
                Authors = GetElementValue(metadata, ns, "authors"),
                Owners = GetElementValue(metadata, ns, "owners"),
                ProjectUrl = GetElementValue(metadata, ns, "projectUrl"),
                LicenseUrl = GetElementValue(metadata, ns, "licenseUrl"),
                License = GetLicense(metadata, ns),
                Tags = GetElementValue(metadata, ns, "tags"),
                DependencyGroups = GetDependencyGroups(metadata, ns),
                AvailableFrameworks = frameworks
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse .nuspec file: {Path}", nuspecPath);
            return null;
        }
    }

    private static string? GetElementValue(XElement parent, XNamespace ns, string name)
    {
        var element = parent.Element(ns + name);
        var value = element?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? GetLicense(XElement metadata, XNamespace ns)
    {
        var licenseElement = metadata.Element(ns + "license");
        if (licenseElement != null)
        {
            var type = licenseElement.Attribute("type")?.Value;
            var value = licenseElement.Value;
            if (type == "expression")
            {
                return value;
            }
            return value;
        }
        return null;
    }

    private static List<PackageDependencyGroup> GetDependencyGroups(XElement metadata, XNamespace ns)
    {
        var groups = new List<PackageDependencyGroup>();
        var dependencies = metadata.Element(ns + "dependencies");

        if (dependencies == null)
            return groups;

        // Check for dependency groups (target framework specific)
        var dependencyGroups = dependencies.Elements(ns + "group");
        if (dependencyGroups.Any())
        {
            foreach (var group in dependencyGroups)
            {
                groups.Add(new PackageDependencyGroup
                {
                    TargetFramework = group.Attribute("targetFramework")?.Value,
                    Dependencies = group.Elements(ns + "dependency")
                        .Select(d => new PackageDependency
                        {
                            PackageId = d.Attribute("id")?.Value ?? "",
                            VersionRange = d.Attribute("version")?.Value
                        })
                        .Where(d => !string.IsNullOrEmpty(d.PackageId))
                        .ToList()
                });
            }
        }
        else
        {
            // No groups, just direct dependencies
            var deps = dependencies.Elements(ns + "dependency")
                .Select(d => new PackageDependency
                {
                    PackageId = d.Attribute("id")?.Value ?? "",
                    VersionRange = d.Attribute("version")?.Value
                })
                .Where(d => !string.IsNullOrEmpty(d.PackageId))
                .ToList();

            if (deps.Count > 0)
            {
                groups.Add(new PackageDependencyGroup
                {
                    Dependencies = deps
                });
            }
        }

        return groups;
    }
}
