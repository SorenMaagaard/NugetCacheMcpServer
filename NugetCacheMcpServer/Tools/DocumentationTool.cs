using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NugetCacheMcpServer.Services;
using NugetCacheMcpServer.Utilities;

namespace NugetCacheMcpServer.Tools;

/// <summary>
/// MCP tool for getting detailed method documentation.
/// </summary>
[McpServerToolType]
public class DocumentationTool
{
    private readonly IPackageIndex _packageIndex;
    private readonly IAssemblyInspector _assemblyInspector;
    private readonly IXmlDocumentationParser _xmlParser;

    public DocumentationTool(
        IPackageIndex packageIndex,
        IAssemblyInspector assemblyInspector,
        IXmlDocumentationParser xmlParser)
    {
        _packageIndex = packageIndex;
        _assemblyInspector = assemblyInspector;
        _xmlParser = xmlParser;
    }

    [McpServerTool(Name = "get_method_documentation")]
    [Description("Get detailed documentation for a specific method, including all overloads, parameter descriptions, return value info, and examples. Use this when you need to understand how to use a specific method.")]
    public string GetMethodDocumentation(
        [Description("The NuGet package ID (e.g., 'Newtonsoft.Json')")] string packageId,
        [Description("The type name containing the method (e.g., 'JsonConvert')")] string typeName,
        [Description("The method name to look up (e.g., 'SerializeObject')")] string methodName,
        [Description("Optional specific version. If not provided, uses the latest cached version.")] string? version = null,
        [Description("Preferred target framework (e.g., 'net8.0'). Auto-selects best available if not specified.")] string? framework = null)
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

        // Get type definition to find the full type name
        var typeDef = _assemblyInspector.GetTypeDefinition(assemblyPath, typeName);
        if (typeDef == null)
        {
            return $"Type '{typeName}' not found in package '{packageId}'.\n" +
                   "Use list_types to see available types.";
        }

        // Get all overloads of the method
        var methods = _assemblyInspector.GetMethods(assemblyPath, typeName, methodName).ToList();
        if (methods.Count == 0)
        {
            return $"Method '{methodName}' not found in type '{typeName}'.\n" +
                   $"Use get_type_definition to see available methods on this type.";
        }

        // Load XML documentation if available
        var xmlPath = FrameworkSelector.GetXmlDocumentationPath(assemblyPath);
        if (xmlPath != null)
        {
            _xmlParser.LoadDocumentation(xmlPath);
        }

        var overloads = methods.Select(m =>
        {
            var methodDoc = _xmlParser.GetMethodDocumentation(
                typeDef.FullName,
                m.Name,
                m.Parameters.Select(p => p.Type).ToArray());

            // Build compact parameter list with descriptions
            var parameters = m.Parameters.Select(p =>
            {
                string? desc = null;
                methodDoc?.Parameters.TryGetValue(p.Name, out desc);

                var modifiers = new List<string>();
                if (p.IsParams) modifiers.Add("params");
                if (p.IsRef) modifiers.Add("ref");
                if (p.IsOut) modifiers.Add("out");
                if (p.IsIn) modifiers.Add("in");

                var prefix = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";
                var defaultPart = p.IsOptional && p.DefaultValue != null ? $" = {p.DefaultValue}" : "";
                var paramStr = $"{prefix}{p.Type} {p.Name}{defaultPart}";

                return desc != null ? new { parameter = paramStr, description = desc } : (object)paramStr;
            }).ToList();

            var exceptions = methodDoc?.Exceptions.Count > 0
                ? methodDoc.Exceptions.Select(e => $"{e.Type}: {e.Description}")
                : null;

            return new
            {
                signature = m.Signature,
                summary = methodDoc?.Summary,
                parameters = parameters.Count > 0 ? parameters : null,
                returns = methodDoc?.Returns,
                remarks = methodDoc?.Remarks,
                example = methodDoc?.Example,
                exceptions
            };
        }).ToList();

        var result = new
        {
            packageId = package.PackageId,
            version = package.Version,
            typeName = typeDef.FullName,
            methodName,
            overloads
        };

        _xmlParser.Clear();
        return JsonSerializer.Serialize(result, CacheExplorerTool.CompactJson);
    }

    private string GetPackageNotFoundMessage(string packageId, string? version)
    {
        var message = version != null
            ? $"Package '{packageId}' version '{version}' not found in the local NuGet cache."
            : $"Package '{packageId}' not found in the local NuGet cache.";

        message += "\n\nTo cache this package, run 'dotnet restore' in a project that references it.";

        return message;
    }
}
