using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NugetCacheMcpServer.Models;
using NugetCacheMcpServer.Services;
using NugetCacheMcpServer.Utilities;

namespace NugetCacheMcpServer.Tools;

/// <summary>
/// MCP tools for exploring types in NuGet packages.
/// </summary>
[McpServerToolType]
public class TypeExplorerTool
{
    private readonly IPackageIndex _packageIndex;
    private readonly IAssemblyInspector _assemblyInspector;
    private readonly IXmlDocumentationParser _xmlParser;

    public TypeExplorerTool(
        IPackageIndex packageIndex,
        IAssemblyInspector assemblyInspector,
        IXmlDocumentationParser xmlParser)
    {
        _packageIndex = packageIndex;
        _assemblyInspector = assemblyInspector;
        _xmlParser = xmlParser;
    }

    [McpServerTool(Name = "list_types")]
    [Description("List all public types (classes, interfaces, structs, enums) in a NuGet package. Use this to discover what types are available.")]
    public string ListTypes(
        [Description("The NuGet package ID (e.g., 'Newtonsoft.Json')")] string packageId,
        [Description("Optional specific version. If not provided, uses the latest cached version.")] string? version = null,
        [Description("Filter types by namespace (case-insensitive partial match)")] string? namespaceFilter = null,
        [Description("Filter by type kind: 'class', 'interface', 'struct', 'enum', or 'delegate'")] string? typeKind = null,
        [Description("Filter by type name pattern with wildcards. Examples: '*Converter' (ends with), '*Message*' (contains), 'Api.Models.*' (namespace prefix).")] string? filter = null,
        [Description("Preferred target framework (e.g., 'net8.0'). Auto-selects best available if not specified.")] string? framework = null,
        [Description("Opaque cursor for pagination. Pass the nextCursor from a previous response to get the next page.")] string? cursor = null,
        [Description("Maximum number of types to return per page (default: 100)")] int pageSize = 100)
    {
        var package = _packageIndex.GetPackage(packageId, version);
        if (package == null)
        {
            return GetPackageNotFoundMessage(packageId, version);
        }

        var selectedFramework = FrameworkSelector.SelectBestFramework(package.AvailableFrameworks, framework);
        if (selectedFramework == null)
        {
            return $"No compatible framework found for package '{packageId}' version '{package.Version}'.";
        }

        var assemblyPath = FrameworkSelector.GetAssemblyPath(package.PackagePath, selectedFramework, packageId);
        if (assemblyPath == null)
        {
            return $"No assembly found for package '{packageId}' in framework '{selectedFramework}'.";
        }

        TypeKind? kind = null;
        if (!string.IsNullOrEmpty(typeKind))
        {
            if (Enum.TryParse<TypeKind>(typeKind, true, out var parsedKind))
            {
                kind = parsedKind;
            }
            else
            {
                return $"Invalid type kind: '{typeKind}'. Valid values: class, interface, struct, enum, delegate.";
            }
        }

        // Load XML documentation if available
        var xmlPath = FrameworkSelector.GetXmlDocumentationPath(assemblyPath);
        if (xmlPath != null)
        {
            _xmlParser.LoadDocumentation(xmlPath);
        }

        // Decode cursor if provided
        var skip = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                var (cursorSkip, _) = CacheExplorerTool.DecodeCursor(cursor);
                skip = cursorSkip;
            }
            catch
            {
                return "Invalid cursor token";
            }
        }

        var allTypes = _assemblyInspector.ListTypes(assemblyPath, namespaceFilter, kind, filter).ToList();
        var types = allTypes.Skip(skip).Take(pageSize).ToList();

        var nextSkip = skip + types.Count;
        var hasMore = nextSkip < allTypes.Count;

        var result = new
        {
            packageId = package.PackageId,
            version = package.Version,
            framework = selectedFramework,
            totalCount = allTypes.Count,
            returnedCount = types.Count,
            nextCursor = hasMore ? CacheExplorerTool.EncodeCursor(nextSkip) : null,
            types = types.Select(t =>
            {
                var doc = _xmlParser.GetTypeDocumentation(t.FullName);
                return new
                {
                    fullName = t.FullName,
                    kind = t.Kind.ToString().ToLowerInvariant(),
                    genericParameters = t.GenericParameterCount > 0 ? t.GenericParameterCount : (int?)null,
                    isStatic = t.IsStatic ? true : (bool?)null,
                    isAbstract = t.IsAbstract ? true : (bool?)null,
                    summary = doc?.Summary
                };
            })
        };

        _xmlParser.Clear();
        return JsonSerializer.Serialize(result, CacheExplorerTool.CompactJson);
    }

    [McpServerTool(Name = "get_type_definition")]
    [Description("Get the complete definition of a type including all methods, properties, constructors, and documentation. Use this to understand how to use a specific class or interface.")]
    public string GetTypeDefinition(
        [Description("The NuGet package ID (e.g., 'Newtonsoft.Json')")] string packageId,
        [Description("The type name to look up. Can be simple name (e.g., 'JsonConvert') or full name (e.g., 'Newtonsoft.Json.JsonConvert')")] string typeName,
        [Description("Optional specific version. If not provided, uses the latest cached version.")] string? version = null,
        [Description("Preferred target framework (e.g., 'net8.0'). Auto-selects best available if not specified.")] string? framework = null,
        [Description("Maximum number of methods to return (default: 50)")] int maxMethods = 50,
        [Description("Maximum number of properties to return (default: 50)")] int maxProperties = 50)
    {
        var package = _packageIndex.GetPackage(packageId, version);
        if (package == null)
        {
            return GetPackageNotFoundMessage(packageId, version);
        }

        var selectedFramework = FrameworkSelector.SelectBestFramework(package.AvailableFrameworks, framework);
        if (selectedFramework == null)
        {
            return $"No compatible framework found for package '{packageId}' version '{package.Version}'.";
        }

        var assemblyPath = FrameworkSelector.GetAssemblyPath(package.PackagePath, selectedFramework, packageId);
        if (assemblyPath == null)
        {
            return $"No assembly found for package '{packageId}' in framework '{selectedFramework}'.";
        }

        // Load XML documentation if available
        var xmlPath = FrameworkSelector.GetXmlDocumentationPath(assemblyPath);
        if (xmlPath != null)
        {
            _xmlParser.LoadDocumentation(xmlPath);
        }

        var definition = _assemblyInspector.GetTypeDefinition(assemblyPath, typeName);
        if (definition == null)
        {
            _xmlParser.Clear();
            return $"Type '{typeName}' not found in package '{packageId}' version '{package.Version}'.\n" +
                   $"Use list_types to see available types.";
        }

        // Enrich with XML documentation
        var typeDoc = _xmlParser.GetTypeDocumentation(definition.FullName);

        // Apply pagination to methods and properties
        var methods = definition.Methods.Take(maxMethods).ToList();
        var properties = definition.Properties.Take(maxProperties).ToList();

        var result = new
        {
            packageId = package.PackageId,
            version = package.Version,
            framework = selectedFramework,
            signature = definition.Signature,
            fullName = definition.FullName,
            kind = definition.Kind.ToString().ToLowerInvariant(),
            baseType = definition.BaseType,
            interfaces = definition.Interfaces.Count > 0 ? definition.Interfaces : null,
            genericParameters = definition.GenericParameters.Count > 0 ? definition.GenericParameters : null,
            genericConstraints = definition.GenericConstraints.Count > 0
                ? definition.GenericConstraints.Select(c => $"{c.ParameterName}: {string.Join(", ", c.Constraints)}")
                : null,
            summary = typeDoc?.Summary,
            remarks = typeDoc?.Remarks,
            constructors = definition.Constructors.Count > 0 ? definition.Constructors.Select(c => new
            {
                signature = c.Signature,
                parameters = c.Parameters.Count > 0 ? c.Parameters.Select(p => FormatParameterCompact(p, definition.FullName)).ToList() : null
            }).ToList() : null,
            methodCount = definition.Methods.Count,
            methods = methods.Count > 0 ? methods.Select(m =>
            {
                // Pass parameter types for accurate XML doc lookup
                var methodDoc = _xmlParser.GetMethodDocumentation(
                    definition.FullName,
                    m.Name,
                    m.Parameters.Select(p => p.Type).ToArray());
                return new
                {
                    signature = m.Signature,
                    summary = methodDoc?.Summary,
                    returns = methodDoc?.Returns
                };
            }).ToList() : null,
            hasMoreMethods = definition.Methods.Count > maxMethods,
            propertyCount = definition.Properties.Count,
            properties = properties.Count > 0 ? properties.Select(p =>
            {
                var propDoc = _xmlParser.GetPropertyDocumentation(definition.FullName, p.Name);
                return new
                {
                    signature = p.Signature,
                    summary = propDoc?.Summary
                };
            }).ToList() : null,
            hasMoreProperties = definition.Properties.Count > maxProperties,
            fields = definition.Fields.Count > 0 ? definition.Fields.Select(f =>
            {
                var fieldDoc = _xmlParser.GetFieldDocumentation(definition.FullName, f.Name);
                return new
                {
                    signature = f.Signature,
                    constantValue = f.ConstantValue,
                    summary = fieldDoc?.Summary
                };
            }).ToList() : null,
            events = definition.Events.Count > 0 ? definition.Events.Select(e => new
            {
                signature = e.Signature
            }) : null,
            enumValues = definition.EnumMembers.Count > 0 ? definition.EnumMembers.Select(m => new
            {
                name = m.Name,
                value = m.Value
            }) : null
        };

        _xmlParser.Clear();
        return JsonSerializer.Serialize(result, CacheExplorerTool.CompactJson);
    }

    private object FormatParameterCompact(ParameterDefinition param, string typeName, string? methodName = null)
    {
        // Build compact parameter string: "type name" or "type name = default" or "ref type name"
        var modifiers = new List<string>();
        if (param.IsParams) modifiers.Add("params");
        if (param.IsRef) modifiers.Add("ref");
        if (param.IsOut) modifiers.Add("out");
        if (param.IsIn) modifiers.Add("in");

        var prefix = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";
        var defaultPart = param.IsOptional && param.DefaultValue != null ? $" = {param.DefaultValue}" : "";

        return $"{prefix}{param.Type} {param.Name}{defaultPart}";
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
