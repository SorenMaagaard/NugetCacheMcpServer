using System.Reflection;
using System.Text;

namespace NugetCacheMcpServer.Utilities;

/// <summary>
/// Formats type names and signatures in C# style.
/// </summary>
public static class TypeNameFormatter
{
    private static readonly Dictionary<string, string> BuiltInTypes = new()
    {
        ["System.Void"] = "void",
        ["System.Boolean"] = "bool",
        ["System.Byte"] = "byte",
        ["System.SByte"] = "sbyte",
        ["System.Char"] = "char",
        ["System.Int16"] = "short",
        ["System.UInt16"] = "ushort",
        ["System.Int32"] = "int",
        ["System.UInt32"] = "uint",
        ["System.Int64"] = "long",
        ["System.UInt64"] = "ulong",
        ["System.Single"] = "float",
        ["System.Double"] = "double",
        ["System.Decimal"] = "decimal",
        ["System.String"] = "string",
        ["System.Object"] = "object"
    };

    /// <summary>
    /// Formats a type name in C# style (e.g., "int" instead of "System.Int32").
    /// </summary>
    public static string FormatTypeName(Type type, bool includeNamespace = false)
    {
        if (type.IsByRef)
        {
            return FormatTypeName(type.GetElementType()!, includeNamespace);
        }

        if (type.IsArray)
        {
            var elementType = FormatTypeName(type.GetElementType()!, includeNamespace);
            var rank = type.GetArrayRank();
            var brackets = rank == 1 ? "[]" : "[" + new string(',', rank - 1) + "]";
            return elementType + brackets;
        }

        if (type.IsPointer)
        {
            return FormatTypeName(type.GetElementType()!, includeNamespace) + "*";
        }

        // Handle nullable value types
        if (type.IsGenericType && type.GetGenericTypeDefinition().FullName == "System.Nullable`1")
        {
            var underlyingType = type.GetGenericArguments()[0];
            return FormatTypeName(underlyingType, includeNamespace) + "?";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var baseName = genericDef.Name;
            var backtickIndex = baseName.IndexOf('`');
            if (backtickIndex > 0)
                baseName = baseName[..backtickIndex];

            if (includeNamespace && !string.IsNullOrEmpty(type.Namespace))
                baseName = type.Namespace + "." + baseName;

            var args = type.GetGenericArguments()
                .Select(t => FormatTypeName(t, includeNamespace));
            return $"{baseName}<{string.Join(", ", args)}>";
        }

        // Handle generic type parameters
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        // Check for built-in type names
        if (type.FullName != null && BuiltInTypes.TryGetValue(type.FullName, out var builtIn))
        {
            return builtIn;
        }

        // Return simple name or full name based on preference
        if (includeNamespace && !string.IsNullOrEmpty(type.Namespace))
        {
            return type.FullName ?? type.Name;
        }

        return type.Name;
    }

    /// <summary>
    /// Formats a type name from a string (handling generic arity notation).
    /// </summary>
    public static string FormatTypeNameFromString(string? typeName, bool includeNamespace = false)
    {
        if (string.IsNullOrEmpty(typeName))
            return "unknown";

        // Check for built-in types
        if (BuiltInTypes.TryGetValue(typeName, out var builtIn))
            return builtIn;

        // Handle generic types with backtick notation
        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex > 0)
        {
            var baseName = typeName[..backtickIndex];
            if (!includeNamespace)
            {
                var lastDot = baseName.LastIndexOf('.');
                if (lastDot >= 0)
                    baseName = baseName[(lastDot + 1)..];
            }

            // Extract generic arity
            var arityStr = typeName[(backtickIndex + 1)..];
            var bracketIndex = arityStr.IndexOf('[');
            if (bracketIndex > 0)
                arityStr = arityStr[..bracketIndex];

            if (int.TryParse(arityStr, out var arity))
            {
                var typeParams = string.Join(", ", Enumerable.Range(0, arity).Select(i => $"T{(arity > 1 ? (i + 1).ToString() : "")}"));
                return $"{baseName}<{typeParams}>";
            }
        }

        // Remove namespace if not wanted
        if (!includeNamespace)
        {
            var lastDot = typeName.LastIndexOf('.');
            if (lastDot >= 0)
                return typeName[(lastDot + 1)..];
        }

        return typeName;
    }

    /// <summary>
    /// Formats a method signature in C# style.
    /// </summary>
    public static string FormatMethodSignature(MethodInfo method)
    {
        var sb = new StringBuilder();

        // Modifiers
        if (method.IsPublic) sb.Append("public ");
        else if (method.IsFamily) sb.Append("protected ");
        else if (method.IsAssembly) sb.Append("internal ");

        if (method.IsStatic) sb.Append("static ");
        if (method.IsAbstract) sb.Append("abstract ");
        else if (method.IsVirtual && !method.IsFinal) sb.Append("virtual ");
        if (method.IsFinal && method.IsVirtual) sb.Append("sealed override ");

        // Return type
        sb.Append(FormatTypeName(method.ReturnType));
        sb.Append(' ');

        // Method name
        sb.Append(method.Name);

        // Generic parameters
        if (method.IsGenericMethod)
        {
            var genericArgs = method.GetGenericArguments();
            sb.Append('<');
            sb.Append(string.Join(", ", genericArgs.Select(a => a.Name)));
            sb.Append('>');
        }

        // Parameters
        sb.Append('(');
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatParameter(parameters[i]));
        }
        sb.Append(')');

        return sb.ToString();
    }

    /// <summary>
    /// Formats a constructor signature.
    /// </summary>
    public static string FormatConstructorSignature(ConstructorInfo ctor, string typeName)
    {
        var sb = new StringBuilder();

        if (ctor.IsPublic) sb.Append("public ");
        else if (ctor.IsFamily) sb.Append("protected ");
        else if (ctor.IsAssembly) sb.Append("internal ");

        if (ctor.IsStatic) sb.Append("static ");

        sb.Append(typeName);

        sb.Append('(');
        var parameters = ctor.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatParameter(parameters[i]));
        }
        sb.Append(')');

        return sb.ToString();
    }

    /// <summary>
    /// Formats a property signature.
    /// </summary>
    public static string FormatPropertySignature(PropertyInfo property)
    {
        var sb = new StringBuilder();

        var getter = property.GetGetMethod(true);
        var setter = property.GetSetMethod(true);
        var accessor = getter ?? setter;

        if (accessor != null)
        {
            if (accessor.IsPublic) sb.Append("public ");
            else if (accessor.IsFamily) sb.Append("protected ");
            else if (accessor.IsAssembly) sb.Append("internal ");

            if (accessor.IsStatic) sb.Append("static ");
            if (accessor.IsAbstract) sb.Append("abstract ");
            else if (accessor.IsVirtual && !accessor.IsFinal) sb.Append("virtual ");
        }

        sb.Append(FormatTypeName(property.PropertyType));
        sb.Append(' ');
        sb.Append(property.Name);
        sb.Append(" { ");

        if (getter != null)
        {
            if (getter.IsPublic) sb.Append("get; ");
            else sb.Append("private get; ");
        }

        if (setter != null)
        {
            if (setter.IsPublic) sb.Append("set; ");
            else sb.Append("private set; ");
        }

        sb.Append('}');

        return sb.ToString();
    }

    /// <summary>
    /// Formats a field signature.
    /// </summary>
    public static string FormatFieldSignature(FieldInfo field)
    {
        var sb = new StringBuilder();

        if (field.IsPublic) sb.Append("public ");
        else if (field.IsFamily) sb.Append("protected ");
        else if (field.IsAssembly) sb.Append("internal ");

        if (field.IsStatic) sb.Append("static ");
        if (field.IsLiteral) sb.Append("const ");
        else if (field.IsInitOnly) sb.Append("readonly ");

        sb.Append(FormatTypeName(field.FieldType));
        sb.Append(' ');
        sb.Append(field.Name);

        if (field.IsLiteral)
        {
            try
            {
                var value = field.GetRawConstantValue();
                sb.Append(" = ");
                sb.Append(FormatConstantValue(value, field.FieldType));
            }
            catch
            {
                // Ignore if we can't get constant value
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats an event signature.
    /// </summary>
    public static string FormatEventSignature(EventInfo evt)
    {
        var sb = new StringBuilder();

        var addMethod = evt.GetAddMethod(true);
        if (addMethod != null)
        {
            if (addMethod.IsPublic) sb.Append("public ");
            else if (addMethod.IsFamily) sb.Append("protected ");
            else if (addMethod.IsAssembly) sb.Append("internal ");

            if (addMethod.IsStatic) sb.Append("static ");
        }

        sb.Append("event ");
        sb.Append(FormatTypeName(evt.EventHandlerType!));
        sb.Append(' ');
        sb.Append(evt.Name);

        return sb.ToString();
    }

    /// <summary>
    /// Formats a type declaration signature.
    /// </summary>
    public static string FormatTypeSignature(Type type)
    {
        var sb = new StringBuilder();

        if (type.IsPublic || type.IsNestedPublic) sb.Append("public ");
        else if (type.IsNestedFamily) sb.Append("protected ");
        else if (type.IsNestedAssembly) sb.Append("internal ");

        if (type.IsAbstract && type.IsSealed) sb.Append("static ");
        else if (type.IsAbstract && !type.IsInterface) sb.Append("abstract ");
        else if (type.IsSealed && !type.IsValueType) sb.Append("sealed ");

        if (type.IsInterface) sb.Append("interface ");
        else if (type.IsEnum) sb.Append("enum ");
        else if (type.IsValueType) sb.Append("struct ");
        else sb.Append("class ");

        sb.Append(type.Name);

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            sb.Append('<');
            sb.Append(string.Join(", ", genericArgs.Select(a => a.Name)));
            sb.Append('>');
        }

        // Base type and interfaces
        var baseType = type.BaseType;
        var interfaces = type.GetInterfaces();
        var inherited = new List<string>();

        // Use string comparison for MetadataLoadContext compatibility
        if (baseType != null &&
            baseType.FullName != "System.Object" &&
            baseType.FullName != "System.ValueType" &&
            baseType.FullName != "System.Enum" &&
            !type.IsEnum)
        {
            inherited.Add(FormatTypeName(baseType));
        }

        inherited.AddRange(interfaces
            .Where(i => !interfaces.Any(other => other != i && other.GetInterfaces().Contains(i)))
            .Select(i => FormatTypeName(i)));

        if (inherited.Count > 0)
        {
            sb.Append(" : ");
            sb.Append(string.Join(", ", inherited));
        }

        return sb.ToString();
    }

    private static string FormatParameter(ParameterInfo param)
    {
        var sb = new StringBuilder();

        // Use CustomAttributes property for MetadataLoadContext compatibility
        var isParams = false;
        try
        {
            isParams = param.CustomAttributes.Any(a =>
                a.AttributeType.FullName == "System.ParamArrayAttribute");
        }
        catch
        {
            // Ignore if custom attributes can't be read
        }

        if (isParams)
            sb.Append("params ");
        else if (param.IsIn)
            sb.Append("in ");
        else if (param.IsOut)
            sb.Append("out ");
        else if (param.ParameterType.IsByRef)
            sb.Append("ref ");

        sb.Append(FormatTypeName(param.ParameterType));
        sb.Append(' ');
        sb.Append(param.Name);

        if (param.HasDefaultValue)
        {
            sb.Append(" = ");
            try
            {
                // Use RawDefaultValue for MetadataLoadContext compatibility
                var rawDefault = param.RawDefaultValue;
                sb.Append(FormatConstantValue(rawDefault, param.ParameterType));
            }
            catch
            {
                sb.Append("default");
            }
        }

        return sb.ToString();
    }

    private static string FormatConstantValue(object? value, Type type)
    {
        if (value == null)
            return "null";

        if (value is string s)
            return $"\"{s}\"";

        if (value is char c)
            return $"'{c}'";

        if (value is bool b)
            return b ? "true" : "false";

        if (type.IsEnum)
            return $"{type.Name}.{value}";

        return value.ToString() ?? "default";
    }
}
