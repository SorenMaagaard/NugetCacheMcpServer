namespace NugetCacheMcpServer.Models;

/// <summary>
/// Definition of a method member.
/// </summary>
public class MethodDefinition
{
    public required string Name { get; init; }
    public required string ReturnType { get; init; }
    public required string Signature { get; init; }
    public List<ParameterDefinition> Parameters { get; init; } = [];
    public List<string> GenericParameters { get; init; } = [];
    public List<GenericConstraint> GenericConstraints { get; init; } = [];

    public bool IsStatic { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsOverride { get; init; }
    public bool IsSealed { get; init; }
    public bool IsAsync { get; init; }

    public string? Summary { get; init; }
    public string? Returns { get; init; }
    public string? Remarks { get; init; }
    public string? Example { get; init; }
    public List<ExceptionDoc> Exceptions { get; init; } = [];
}

/// <summary>
/// Definition of a constructor.
/// </summary>
public class ConstructorDefinition
{
    public required string Signature { get; init; }
    public List<ParameterDefinition> Parameters { get; init; } = [];
    public bool IsStatic { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Definition of a method or constructor parameter.
/// </summary>
public class ParameterDefinition
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool IsOptional { get; init; }
    public string? DefaultValue { get; init; }
    public bool IsParams { get; init; }
    public bool IsRef { get; init; }
    public bool IsOut { get; init; }
    public bool IsIn { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Definition of a property.
/// </summary>
public class PropertyDefinition
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Signature { get; init; }
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
    public bool IsStatic { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsOverride { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Definition of a field.
/// </summary>
public class FieldDefinition
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Signature { get; init; }
    public bool IsStatic { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsConst { get; init; }
    public string? ConstantValue { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Definition of an event.
/// </summary>
public class EventDefinition
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Signature { get; init; }
    public bool IsStatic { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Definition of an enum member.
/// </summary>
public class EnumMember
{
    public required string Name { get; init; }
    public required object Value { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Exception documentation.
/// </summary>
public class ExceptionDoc
{
    public required string Type { get; init; }
    public string? Description { get; init; }
}
