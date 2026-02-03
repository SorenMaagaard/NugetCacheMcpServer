using NugetCacheMcpServer.Models;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Interface for inspecting assembly metadata.
/// </summary>
public interface IAssemblyInspector
{
    /// <summary>
    /// Lists all public types in an assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <param name="namespaceFilter">Filter types by namespace (case-insensitive partial match).</param>
    /// <param name="typeKind">Filter by type kind.</param>
    /// <param name="typeNameFilter">Filter by type name pattern with wildcards (* matches any characters).</param>
    IEnumerable<TypeSummary> ListTypes(
        string assemblyPath,
        string? namespaceFilter = null,
        TypeKind? typeKind = null,
        string? typeNameFilter = null);

    /// <summary>
    /// Gets the complete definition of a type.
    /// </summary>
    TypeDefinition? GetTypeDefinition(string assemblyPath, string typeName);

    /// <summary>
    /// Gets method definitions for a specific method name (all overloads).
    /// </summary>
    IEnumerable<MethodDefinition> GetMethods(string assemblyPath, string typeName, string methodName);

    /// <summary>
    /// Compares types between two assemblies.
    /// </summary>
    IEnumerable<ApiChange> CompareAssemblies(string oldAssemblyPath, string newAssemblyPath);
}
