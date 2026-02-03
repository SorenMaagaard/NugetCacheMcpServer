namespace NugetCacheMcpServer.Models;

/// <summary>
/// Kind of type (class, interface, struct, enum, delegate).
/// </summary>
public enum TypeKind
{
    Class,
    Interface,
    Struct,
    Enum,
    Delegate
}

/// <summary>
/// Summary information about a type for listing purposes.
/// </summary>
public class TypeSummary
{
    public required string FullName { get; init; }
    public required string Name { get; init; }
    public required string Namespace { get; init; }
    public required TypeKind Kind { get; init; }
    public string? Summary { get; init; }
    public bool IsGeneric { get; init; }
    public int GenericParameterCount { get; init; }
    /// <summary>
    /// True if the type is static (abstract + sealed). Static types cannot be instantiated.
    /// </summary>
    public bool IsStatic { get; init; }
    /// <summary>
    /// True if the type is abstract and must be inherited (not applicable to static types or interfaces).
    /// </summary>
    public bool IsAbstract { get; init; }
}

/// <summary>
/// Complete definition of a type with all members.
/// </summary>
public class TypeDefinition
{
    public required string FullName { get; init; }
    public required string Name { get; init; }
    public required string Namespace { get; init; }
    public required TypeKind Kind { get; init; }
    public string? Summary { get; init; }
    public string? Remarks { get; init; }

    public bool IsAbstract { get; init; }
    public bool IsSealed { get; init; }
    public bool IsStatic { get; init; }
    public bool IsGeneric { get; init; }

    public string? BaseType { get; init; }
    public List<string> Interfaces { get; init; } = [];
    public List<string> GenericParameters { get; init; } = [];
    public List<GenericConstraint> GenericConstraints { get; init; } = [];

    public List<ConstructorDefinition> Constructors { get; init; } = [];
    public List<MethodDefinition> Methods { get; init; } = [];
    public List<PropertyDefinition> Properties { get; init; } = [];
    public List<FieldDefinition> Fields { get; init; } = [];
    public List<EventDefinition> Events { get; init; } = [];
    public List<EnumMember> EnumMembers { get; init; } = [];

    public string? Signature { get; init; }
}

/// <summary>
/// Generic type parameter constraint.
/// </summary>
public class GenericConstraint
{
    public required string ParameterName { get; init; }
    public List<string> Constraints { get; init; } = [];
}
