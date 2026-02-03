using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NugetCacheMcpServer.Cache;
using NugetCacheMcpServer.Models;
using NugetCacheMcpServer.Utilities;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Inspects assembly metadata using MetadataLoadContext.
/// </summary>
public class AssemblyInspector : IAssemblyInspector
{
    private readonly AssemblyCache _cache;
    private readonly ILogger<AssemblyInspector> _logger;

    // Well-known type names for MetadataLoadContext comparisons
    private const string SystemObject = "System.Object";
    private const string SystemValueType = "System.ValueType";
    private const string SystemDelegate = "System.Delegate";
    private const string SystemMulticastDelegate = "System.MulticastDelegate";
    private const string SystemEnum = "System.Enum";

    public AssemblyInspector(AssemblyCache cache, ILogger<AssemblyInspector> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public IEnumerable<TypeSummary> ListTypes(
        string assemblyPath,
        string? namespaceFilter = null,
        TypeKind? typeKind = null,
        string? typeNameFilter = null)
    {
        var (_, assembly) = _cache.GetOrCreate(assemblyPath);

        var types = assembly.GetExportedTypes()
            .Where(t => t.IsPublic || t.IsNestedPublic);

        if (!string.IsNullOrEmpty(namespaceFilter))
        {
            types = types.Where(t => t.Namespace?.Contains(namespaceFilter, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (typeKind.HasValue)
        {
            types = types.Where(t => GetTypeKind(t) == typeKind.Value);
        }

        if (!string.IsNullOrEmpty(typeNameFilter))
        {
            var regex = WildcardToRegex(typeNameFilter);
            types = types.Where(t => regex.IsMatch(t.FullName ?? t.Name));
        }

        return types.Select(t => new TypeSummary
        {
            FullName = t.FullName ?? t.Name,
            Name = t.Name,
            Namespace = t.Namespace ?? string.Empty,
            Kind = GetTypeKind(t),
            IsGeneric = t.IsGenericType,
            GenericParameterCount = t.IsGenericType ? t.GetGenericArguments().Length : 0,
            IsStatic = t.IsAbstract && t.IsSealed,
            IsAbstract = t.IsAbstract && !t.IsInterface && !(t.IsAbstract && t.IsSealed)
        }).OrderBy(t => t.Namespace).ThenBy(t => t.Name);
    }

    /// <summary>
    /// Converts a wildcard pattern to a regex for type name matching.
    /// Supports * (matches any characters) and ? (matches single character).
    /// </summary>
    private static Regex WildcardToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public TypeDefinition? GetTypeDefinition(string assemblyPath, string typeName)
    {
        var (_, assembly) = _cache.GetOrCreate(assemblyPath);

        var type = FindType(assembly, typeName);
        if (type == null)
        {
            _logger.LogWarning("Type not found: {TypeName} in {Assembly}", typeName, assemblyPath);
            return null;
        }

        var definition = new TypeDefinition
        {
            FullName = type.FullName ?? type.Name,
            Name = GetSimpleTypeName(type),
            Namespace = type.Namespace ?? string.Empty,
            Kind = GetTypeKind(type),
            IsAbstract = type.IsAbstract && !type.IsInterface,
            IsSealed = type.IsSealed,
            IsStatic = type.IsAbstract && type.IsSealed,
            IsGeneric = type.IsGenericType,
            BaseType = GetBaseTypeName(type),
            Interfaces = GetDirectInterfaces(type)
                .Select(i => TypeNameFormatter.FormatTypeName(i))
                .ToList(),
            GenericParameters = type.IsGenericType
                ? type.GetGenericArguments().Select(a => a.Name).ToList()
                : [],
            GenericConstraints = GetGenericConstraints(type),
            Signature = TypeNameFormatter.FormatTypeSignature(type),
            Constructors = GetConstructors(type),
            Methods = GetMethods(type),
            Properties = GetProperties(type),
            Fields = GetFields(type),
            Events = GetEvents(type),
            EnumMembers = type.IsEnum ? GetEnumMembers(type) : []
        };

        return definition;
    }

    public IEnumerable<MethodDefinition> GetMethods(string assemblyPath, string typeName, string methodName)
    {
        var (_, assembly) = _cache.GetOrCreate(assemblyPath);

        var type = FindType(assembly, typeName);
        if (type == null)
            return [];

        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .Select(CreateMethodDefinition);
    }

    public IEnumerable<ApiChange> CompareAssemblies(string oldAssemblyPath, string newAssemblyPath)
    {
        var changes = new List<ApiChange>();

        var (_, oldAssembly) = _cache.GetOrCreate(oldAssemblyPath);
        var (_, newAssembly) = _cache.GetOrCreate(newAssemblyPath);

        var oldTypes = oldAssembly.GetExportedTypes()
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .ToDictionary(t => t.FullName ?? t.Name, StringComparer.OrdinalIgnoreCase);

        var newTypes = newAssembly.GetExportedTypes()
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .ToDictionary(t => t.FullName ?? t.Name, StringComparer.OrdinalIgnoreCase);

        // Find removed types
        foreach (var (name, oldType) in oldTypes)
        {
            if (!newTypes.ContainsKey(name))
            {
                changes.Add(new ApiChange
                {
                    Kind = ApiChangeKind.Removed,
                    MemberType = GetTypeKind(oldType).ToString(),
                    MemberName = name,
                    OldSignature = TypeNameFormatter.FormatTypeSignature(oldType),
                    IsBreakingChange = true,
                    Description = $"Type '{name}' was removed"
                });
            }
        }

        // Find added types
        foreach (var (name, newType) in newTypes)
        {
            if (!oldTypes.ContainsKey(name))
            {
                changes.Add(new ApiChange
                {
                    Kind = ApiChangeKind.Added,
                    MemberType = GetTypeKind(newType).ToString(),
                    MemberName = name,
                    NewSignature = TypeNameFormatter.FormatTypeSignature(newType),
                    IsBreakingChange = false,
                    Description = $"Type '{name}' was added"
                });
            }
        }

        // Compare existing types for member changes
        foreach (var (name, oldType) in oldTypes)
        {
            if (newTypes.TryGetValue(name, out var newType))
            {
                changes.AddRange(CompareTypeMembers(oldType, newType));
            }
        }

        return changes;
    }

    private IEnumerable<ApiChange> CompareTypeMembers(Type oldType, Type newType)
    {
        var changes = new List<ApiChange>();
        var typeName = oldType.FullName ?? oldType.Name;

        // Compare methods
        var oldMethods = GetPublicMethods(oldType);
        var newMethods = GetPublicMethods(newType);

        foreach (var (sig, _) in oldMethods)
        {
            if (!newMethods.ContainsKey(sig))
            {
                changes.Add(new ApiChange
                {
                    Kind = ApiChangeKind.Removed,
                    MemberType = "Method",
                    MemberName = $"{typeName}.{sig}",
                    OldSignature = sig,
                    IsBreakingChange = true,
                    Description = $"Method was removed"
                });
            }
        }

        foreach (var (sig, _) in newMethods)
        {
            if (!oldMethods.ContainsKey(sig))
            {
                changes.Add(new ApiChange
                {
                    Kind = ApiChangeKind.Added,
                    MemberType = "Method",
                    MemberName = $"{typeName}.{sig}",
                    NewSignature = sig,
                    IsBreakingChange = false,
                    Description = $"Method was added"
                });
            }
        }

        // Compare properties
        var oldProps = oldType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var newProps = newType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, oldProp) in oldProps)
        {
            if (!newProps.ContainsKey(name))
            {
                changes.Add(new ApiChange
                {
                    Kind = ApiChangeKind.Removed,
                    MemberType = "Property",
                    MemberName = $"{typeName}.{name}",
                    OldSignature = TypeNameFormatter.FormatPropertySignature(oldProp),
                    IsBreakingChange = true,
                    Description = "Property was removed"
                });
            }
            else if (TypeNameFormatter.FormatPropertySignature(oldProp) != TypeNameFormatter.FormatPropertySignature(newProps[name]))
            {
                changes.Add(new ApiChange
                {
                    Kind = ApiChangeKind.Modified,
                    MemberType = "Property",
                    MemberName = $"{typeName}.{name}",
                    OldSignature = TypeNameFormatter.FormatPropertySignature(oldProp),
                    NewSignature = TypeNameFormatter.FormatPropertySignature(newProps[name]),
                    IsBreakingChange = true,
                    Description = "Property signature changed"
                });
            }
        }

        foreach (var (name, _) in newProps)
        {
            if (!oldProps.ContainsKey(name))
            {
                changes.Add(new ApiChange
                {
                    Kind = ApiChangeKind.Added,
                    MemberType = "Property",
                    MemberName = $"{typeName}.{name}",
                    NewSignature = TypeNameFormatter.FormatPropertySignature(newProps[name]),
                    IsBreakingChange = false,
                    Description = "Property was added"
                });
            }
        }

        return changes;
    }

    private static Dictionary<string, MethodInfo> GetPublicMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .ToDictionary(m => TypeNameFormatter.FormatMethodSignature(m), StringComparer.OrdinalIgnoreCase);
    }

    private static Type? FindType(Assembly assembly, string typeName)
    {
        // Try exact match first
        var type = assembly.GetType(typeName);
        if (type != null)
            return type;

        // Try case-insensitive search
        var exportedTypes = assembly.GetExportedTypes();

        // Match by full name
        type = exportedTypes.FirstOrDefault(t =>
            (t.FullName?.Equals(typeName, StringComparison.OrdinalIgnoreCase) ?? false));
        if (type != null)
            return type;

        // Match by simple name
        type = exportedTypes.FirstOrDefault(t =>
            t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        if (type != null)
            return type;

        // Match by name without generic arity (e.g., "List" matches "List`1")
        type = exportedTypes.FirstOrDefault(t =>
        {
            var simpleName = t.Name;
            var backtick = simpleName.IndexOf('`');
            if (backtick > 0)
                simpleName = simpleName[..backtick];
            return simpleName.Equals(typeName, StringComparison.OrdinalIgnoreCase);
        });

        return type;
    }

    private static string GetSimpleTypeName(Type type)
    {
        var name = type.Name;
        if (type.IsGenericType)
        {
            var backtick = name.IndexOf('`');
            if (backtick > 0)
            {
                name = name[..backtick];
                var args = type.GetGenericArguments();
                name += "<" + string.Join(", ", args.Select(a => a.Name)) + ">";
            }
        }
        return name;
    }

    private static string? GetBaseTypeName(Type type)
    {
        var baseType = type.BaseType;
        if (baseType == null)
            return null;

        var baseFullName = baseType.FullName;
        if (baseFullName == SystemObject || baseFullName == SystemValueType || baseFullName == SystemEnum)
            return null;

        return TypeNameFormatter.FormatTypeName(baseType);
    }

    private static IEnumerable<Type> GetDirectInterfaces(Type type)
    {
        var allInterfaces = type.GetInterfaces();
        var directInterfaces = new HashSet<Type>(allInterfaces);

        // Remove interfaces that are inherited through other interfaces
        foreach (var iface in allInterfaces)
        {
            foreach (var parentInterface in iface.GetInterfaces())
            {
                directInterfaces.Remove(parentInterface);
            }
        }

        // Remove interfaces inherited from base type
        if (type.BaseType != null)
        {
            foreach (var baseInterface in type.BaseType.GetInterfaces())
            {
                directInterfaces.Remove(baseInterface);
            }
        }

        return directInterfaces;
    }

    private static TypeKind GetTypeKind(Type type)
    {
        if (type.IsInterface) return TypeKind.Interface;
        if (type.IsEnum) return TypeKind.Enum;
        if (type.IsValueType) return TypeKind.Struct;

        // Check for delegate by looking at base type name (works with MetadataLoadContext)
        var baseType = type.BaseType;
        while (baseType != null)
        {
            var baseFullName = baseType.FullName;
            if (baseFullName == SystemDelegate || baseFullName == SystemMulticastDelegate)
                return TypeKind.Delegate;
            baseType = baseType.BaseType;
        }

        return TypeKind.Class;
    }

    private static List<GenericConstraint> GetGenericConstraints(Type type)
    {
        if (!type.IsGenericType)
            return [];

        return type.GetGenericArguments()
            .Where(arg => arg.IsGenericParameter)
            .Where(arg => arg.GetGenericParameterConstraints().Length > 0 ||
                          arg.GenericParameterAttributes != GenericParameterAttributes.None)
            .Select(arg =>
            {
                var constraints = new List<string>();

                var attrs = arg.GenericParameterAttributes;
                if ((attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                    constraints.Add("class");
                if ((attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                    constraints.Add("struct");
                if ((attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                    (attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                    constraints.Add("new()");

                constraints.AddRange(arg.GetGenericParameterConstraints()
                    .Where(c => c.FullName != SystemValueType)
                    .Select(c => TypeNameFormatter.FormatTypeName(c)));

                return new GenericConstraint
                {
                    ParameterName = arg.Name,
                    Constraints = constraints
                };
            })
            .Where(c => c.Constraints.Count > 0)
            .ToList();
    }

    private static List<ConstructorDefinition> GetConstructors(Type type)
    {
        return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Select(ctor =>
            {
                var simpleName = GetSimpleTypeName(type);
                var backtick = simpleName.IndexOf('<');
                if (backtick > 0)
                    simpleName = simpleName[..backtick];

                return new ConstructorDefinition
                {
                    Signature = TypeNameFormatter.FormatConstructorSignature(ctor, simpleName),
                    Parameters = ctor.GetParameters().Select(CreateParameterDefinition).ToList(),
                    IsStatic = ctor.IsStatic
                };
            })
            .ToList();
    }

    private static List<MethodDefinition> GetMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(CreateMethodDefinition)
            .OrderBy(m => m.Name)
            .ToList();
    }

    private static MethodDefinition CreateMethodDefinition(MethodInfo method)
    {
        return new MethodDefinition
        {
            Name = method.Name,
            ReturnType = TypeNameFormatter.FormatTypeName(method.ReturnType),
            Signature = TypeNameFormatter.FormatMethodSignature(method),
            Parameters = method.GetParameters().Select(CreateParameterDefinition).ToList(),
            GenericParameters = method.IsGenericMethod
                ? method.GetGenericArguments().Select(a => a.Name).ToList()
                : [],
            GenericConstraints = method.IsGenericMethod ? GetMethodGenericConstraints(method) : [],
            IsStatic = method.IsStatic,
            IsVirtual = method.IsVirtual && !method.IsFinal,
            IsAbstract = method.IsAbstract,
            IsOverride = IsMethodOverride(method),
            IsSealed = method.IsFinal && method.IsVirtual
        };
    }

    private static List<GenericConstraint> GetMethodGenericConstraints(MethodInfo method)
    {
        return method.GetGenericArguments()
            .Where(arg => arg.IsGenericParameter)
            .Where(arg => arg.GetGenericParameterConstraints().Length > 0 ||
                          arg.GenericParameterAttributes != GenericParameterAttributes.None)
            .Select(arg =>
            {
                var constraints = new List<string>();

                var attrs = arg.GenericParameterAttributes;
                if ((attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                    constraints.Add("class");
                if ((attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                    constraints.Add("struct");
                if ((attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                    (attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                    constraints.Add("new()");

                constraints.AddRange(arg.GetGenericParameterConstraints()
                    .Where(c => c.FullName != SystemValueType)
                    .Select(c => TypeNameFormatter.FormatTypeName(c)));

                return new GenericConstraint
                {
                    ParameterName = arg.Name,
                    Constraints = constraints
                };
            })
            .Where(c => c.Constraints.Count > 0)
            .ToList();
    }

    private static List<PropertyDefinition> GetProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(prop =>
            {
                var getter = prop.GetGetMethod(true);
                var setter = prop.GetSetMethod(true);
                var accessor = getter ?? setter;

                return new PropertyDefinition
                {
                    Name = prop.Name,
                    Type = TypeNameFormatter.FormatTypeName(prop.PropertyType),
                    Signature = TypeNameFormatter.FormatPropertySignature(prop),
                    HasGetter = getter != null,
                    HasSetter = setter != null,
                    IsStatic = accessor?.IsStatic ?? false,
                    IsVirtual = accessor?.IsVirtual ?? false,
                    IsAbstract = accessor?.IsAbstract ?? false,
                    IsOverride = accessor != null && IsMethodOverride(accessor)
                };
            })
            .OrderBy(p => p.Name)
            .ToList();
    }

    private static List<FieldDefinition> GetFields(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => !f.IsSpecialName)
            .Select(field =>
            {
                string? constantValue = null;
                if (field.IsLiteral)
                {
                    try
                    {
                        var value = field.GetRawConstantValue();
                        constantValue = value?.ToString();
                    }
                    catch
                    {
                        // Ignore - GetRawConstantValue may not work with MetadataLoadContext for some types
                    }
                }

                return new FieldDefinition
                {
                    Name = field.Name,
                    Type = TypeNameFormatter.FormatTypeName(field.FieldType),
                    Signature = TypeNameFormatter.FormatFieldSignature(field),
                    IsStatic = field.IsStatic,
                    IsReadOnly = field.IsInitOnly,
                    IsConst = field.IsLiteral,
                    ConstantValue = constantValue
                };
            })
            .OrderBy(f => f.Name)
            .ToList();
    }

    private static List<EventDefinition> GetEvents(Type type)
    {
        return type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(evt => new EventDefinition
            {
                Name = evt.Name,
                Type = evt.EventHandlerType != null ? TypeNameFormatter.FormatTypeName(evt.EventHandlerType) : "unknown",
                Signature = TypeNameFormatter.FormatEventSignature(evt),
                IsStatic = evt.GetAddMethod(true)?.IsStatic ?? false
            })
            .OrderBy(e => e.Name)
            .ToList();
    }

    private static List<EnumMember> GetEnumMembers(Type type)
    {
        // For MetadataLoadContext, we need to get enum values from the fields
        var members = new List<EnumMember>();

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var field in fields)
        {
            if (!field.IsLiteral) continue;

            try
            {
                var value = field.GetRawConstantValue();
                members.Add(new EnumMember
                {
                    Name = field.Name,
                    Value = Convert.ToInt64(value)
                });
            }
            catch
            {
                // If we can't get the value, just add with 0
                members.Add(new EnumMember
                {
                    Name = field.Name,
                    Value = 0
                });
            }
        }

        return members;
    }

    private static ParameterDefinition CreateParameterDefinition(ParameterInfo param)
    {
        string? defaultValue = null;
        if (param.HasDefaultValue)
        {
            try
            {
                var rawDefault = param.RawDefaultValue;
                if (rawDefault == null || rawDefault == DBNull.Value)
                    defaultValue = "null";
                else if (rawDefault is string s)
                    defaultValue = $"\"{s}\"";
                else if (rawDefault is bool b)
                    defaultValue = b ? "true" : "false";
                else if (rawDefault is char c)
                    defaultValue = $"'{c}'";
                else
                    defaultValue = rawDefault.ToString();
            }
            catch
            {
                defaultValue = "default";
            }
        }

        // Check for params array - using CustomAttributeData instead of GetCustomAttributes
        var isParams = false;
        try
        {
            isParams = param.CustomAttributes.Any(a =>
                a.AttributeType.FullName == "System.ParamArrayAttribute");
        }
        catch
        {
            // Ignore
        }

        return new ParameterDefinition
        {
            Name = param.Name ?? $"arg{param.Position}",
            Type = TypeNameFormatter.FormatTypeName(param.ParameterType),
            IsOptional = param.IsOptional,
            DefaultValue = defaultValue,
            IsParams = isParams,
            IsRef = param.ParameterType.IsByRef && !param.IsOut && !param.IsIn,
            IsOut = param.IsOut,
            IsIn = param.IsIn
        };
    }

    private static bool IsMethodOverride(MethodInfo method)
    {
        // For MetadataLoadContext, we can't use GetBaseDefinition()
        // Instead, check if the method is virtual and has 'override' semantics
        // by checking if the declaring type has a base type with a method of the same signature
        if (!method.IsVirtual || method.IsFinal)
            return false;

        try
        {
            var declaringType = method.DeclaringType;
            if (declaringType?.BaseType == null)
                return false;

            // Look for a method with the same name and parameter types in the base type
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            var baseMethod = declaringType.BaseType.GetMethod(
                method.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                parameterTypes,
                null);

            return baseMethod != null && baseMethod.IsVirtual;
        }
        catch
        {
            // If we can't determine, assume it's not an override
            return false;
        }
    }
}
