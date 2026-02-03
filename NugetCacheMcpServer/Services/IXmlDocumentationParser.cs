using NugetCacheMcpServer.Models;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Interface for parsing XML documentation files.
/// </summary>
public interface IXmlDocumentationParser
{
    /// <summary>
    /// Loads documentation from an XML file.
    /// </summary>
    void LoadDocumentation(string xmlPath);

    /// <summary>
    /// Gets documentation for a type.
    /// </summary>
    MemberDocumentation? GetTypeDocumentation(string fullTypeName);

    /// <summary>
    /// Gets documentation for a method.
    /// </summary>
    MemberDocumentation? GetMethodDocumentation(string fullTypeName, string methodName, string[]? parameterTypes = null);

    /// <summary>
    /// Gets documentation for a property.
    /// </summary>
    MemberDocumentation? GetPropertyDocumentation(string fullTypeName, string propertyName);

    /// <summary>
    /// Gets documentation for a field.
    /// </summary>
    MemberDocumentation? GetFieldDocumentation(string fullTypeName, string fieldName);

    /// <summary>
    /// Gets documentation for an event.
    /// </summary>
    MemberDocumentation? GetEventDocumentation(string fullTypeName, string eventName);

    /// <summary>
    /// Checks if documentation is loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Clears the loaded documentation.
    /// </summary>
    void Clear();
}
