namespace NugetCacheMcpServer.Models;

/// <summary>
/// Detailed documentation for a type member extracted from XML.
/// </summary>
public class MemberDocumentation
{
    public required string MemberName { get; init; }
    public string? Summary { get; init; }
    public string? Remarks { get; init; }
    public string? Returns { get; init; }
    public string? Example { get; init; }
    public string? Value { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = [];
    public Dictionary<string, string> TypeParameters { get; init; } = [];
    public List<ExceptionDoc> Exceptions { get; init; } = [];
    public List<string> SeeAlso { get; init; } = [];
}

/// <summary>
/// Complete documentation for a method including signature and XML docs.
/// </summary>
public class MethodDocumentation
{
    public required string TypeName { get; init; }
    public required string MethodName { get; init; }
    public required string Signature { get; init; }
    public required string ReturnType { get; init; }
    public List<ParameterDefinition> Parameters { get; init; } = [];

    public string? Summary { get; init; }
    public string? Remarks { get; init; }
    public string? Returns { get; init; }
    public string? Example { get; init; }
    public List<ExceptionDoc> Exceptions { get; init; } = [];

    public List<MethodDefinition>? Overloads { get; init; }
}
