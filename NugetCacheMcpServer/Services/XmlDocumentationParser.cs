using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NugetCacheMcpServer.Models;

namespace NugetCacheMcpServer.Services;

/// <summary>
/// Parses XML documentation files to extract member documentation.
/// </summary>
public partial class XmlDocumentationParser : IXmlDocumentationParser
{
    private readonly ILogger<XmlDocumentationParser> _logger;
    private readonly Dictionary<string, XElement> _members = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public XmlDocumentationParser(ILogger<XmlDocumentationParser> logger)
    {
        _logger = logger;
    }

    public bool IsLoaded
    {
        get
        {
            lock (_lock)
            {
                return _members.Count > 0;
            }
        }
    }

    public void LoadDocumentation(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            _logger.LogDebug("XML documentation file not found: {Path}", xmlPath);
            return;
        }

        try
        {
            var doc = XDocument.Load(xmlPath);
            var members = doc.Descendants("member");

            lock (_lock)
            {
                _members.Clear();
                foreach (var member in members)
                {
                    var name = member.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(name))
                    {
                        _members[name] = member;
                    }
                }
            }

            _logger.LogDebug("Loaded {Count} documentation entries from {Path}", _members.Count, xmlPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse XML documentation: {Path}", xmlPath);
        }
    }

    public MemberDocumentation? GetTypeDocumentation(string fullTypeName)
    {
        var key = $"T:{fullTypeName}";
        return GetDocumentation(key);
    }

    public MemberDocumentation? GetMethodDocumentation(string fullTypeName, string methodName, string[]? parameterTypes = null)
    {
        var paramCount = parameterTypes?.Length ?? 0;

        // Try exact match with parameters first
        if (parameterTypes != null && parameterTypes.Length > 0)
        {
            var key = $"M:{fullTypeName}.{methodName}({string.Join(",", parameterTypes)})";
            var doc = GetDocumentation(key);
            if (doc != null) return doc;
        }

        // Try without parameters (for parameterless methods)
        if (paramCount == 0)
        {
            var simpleKey = $"M:{fullTypeName}.{methodName}";
            var simpleDoc = GetDocumentation(simpleKey);
            if (simpleDoc != null) return simpleDoc;
        }

        // Find all matching methods (both generic and non-generic)
        // Pattern: M:{typeName}.{methodName} followed by optional `N and optional (params)
        var methodPrefix = $"M:{fullTypeName}.{methodName}";
        List<string> candidates;
        lock (_lock)
        {
            candidates = _members.Keys
                .Where(k => k.StartsWith(methodPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (candidates.Count == 0)
            return null;

        // If only one candidate, return it
        if (candidates.Count == 1)
            return GetDocumentation(candidates[0]);

        // Try to find best match by parameter count
        // XML format: M:Type.Method`N(params) where N is generic arity
        // Parameter count in XML is number of commas + 1 (or 0 if no parens)
        var bestMatch = candidates
            .Select(k => new { Key = k, ParamCount = CountXmlParameters(k) })
            .OrderBy(x => Math.Abs(x.ParamCount - paramCount)) // Closest parameter count first
            .ThenBy(x => x.Key.Length) // Shorter keys (less generic complexity) preferred
            .FirstOrDefault();

        return bestMatch != null ? GetDocumentation(bestMatch.Key) : null;
    }

    private static int CountXmlParameters(string xmlKey)
    {
        // Extract parameter section from XML key
        var parenStart = xmlKey.IndexOf('(');
        if (parenStart < 0)
            return 0;

        var parenEnd = xmlKey.LastIndexOf(')');
        if (parenEnd <= parenStart)
            return 0;

        var paramSection = xmlKey[(parenStart + 1)..parenEnd];
        if (string.IsNullOrEmpty(paramSection))
            return 0;

        // Count parameters by counting commas at nesting level 0
        // Need to handle nested generics like Foo{A,B},Bar
        int count = 1;
        int depth = 0;
        foreach (char c in paramSection)
        {
            if (c == '{' || c == '<' || c == '(')
                depth++;
            else if (c == '}' || c == '>' || c == ')')
                depth--;
            else if (c == ',' && depth == 0)
                count++;
        }
        return count;
    }

    public MemberDocumentation? GetPropertyDocumentation(string fullTypeName, string propertyName)
    {
        var key = $"P:{fullTypeName}.{propertyName}";
        return GetDocumentation(key);
    }

    public MemberDocumentation? GetFieldDocumentation(string fullTypeName, string fieldName)
    {
        var key = $"F:{fullTypeName}.{fieldName}";
        return GetDocumentation(key);
    }

    public MemberDocumentation? GetEventDocumentation(string fullTypeName, string eventName)
    {
        var key = $"E:{fullTypeName}.{eventName}";
        return GetDocumentation(key);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _members.Clear();
        }
    }

    private MemberDocumentation? GetDocumentation(string key)
    {
        XElement? element;
        lock (_lock)
        {
            if (!_members.TryGetValue(key, out element))
                return null;
        }

        var doc = new MemberDocumentation
        {
            MemberName = key,
            Summary = GetElementText(element.Element("summary")),
            Remarks = GetElementText(element.Element("remarks")),
            Returns = GetElementText(element.Element("returns")),
            Example = GetElementText(element.Element("example")),
            Value = GetElementText(element.Element("value")),
            Parameters = element.Elements("param")
                .Where(p => p.Attribute("name") != null)
                .ToDictionary(
                    p => p.Attribute("name")!.Value,
                    p => GetElementText(p) ?? string.Empty),
            TypeParameters = element.Elements("typeparam")
                .Where(p => p.Attribute("name") != null)
                .ToDictionary(
                    p => p.Attribute("name")!.Value,
                    p => GetElementText(p) ?? string.Empty),
            Exceptions = element.Elements("exception")
                .Select(e => new ExceptionDoc
                {
                    Type = ExtractTypeFromCref(e.Attribute("cref")?.Value ?? ""),
                    Description = GetElementText(e)
                })
                .ToList(),
            SeeAlso = element.Elements("seealso")
                .Select(s => ExtractTypeFromCref(s.Attribute("cref")?.Value ?? ""))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList()
        };

        return doc;
    }

    private static string? GetElementText(XElement? element)
    {
        if (element == null)
            return null;

        // Process the element to handle nested tags and whitespace
        var text = ProcessXmlContent(element);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string ProcessXmlContent(XElement element)
    {
        var result = new System.Text.StringBuilder();

        foreach (var node in element.Nodes())
        {
            if (node is XText textNode)
            {
                result.Append(NormalizeWhitespace(textNode.Value));
            }
            else if (node is XElement childElement)
            {
                switch (childElement.Name.LocalName.ToLowerInvariant())
                {
                    case "see":
                    case "seealso":
                        var cref = childElement.Attribute("cref")?.Value;
                        if (!string.IsNullOrEmpty(cref))
                        {
                            result.Append(ExtractTypeFromCref(cref));
                        }
                        else
                        {
                            result.Append(childElement.Value);
                        }
                        break;
                    case "paramref":
                        var paramName = childElement.Attribute("name")?.Value;
                        result.Append(paramName ?? childElement.Value);
                        break;
                    case "typeparamref":
                        var typeParamName = childElement.Attribute("name")?.Value;
                        result.Append(typeParamName ?? childElement.Value);
                        break;
                    case "c":
                    case "code":
                        result.Append('`');
                        result.Append(childElement.Value);
                        result.Append('`');
                        break;
                    case "para":
                        result.AppendLine();
                        result.Append(ProcessXmlContent(childElement));
                        result.AppendLine();
                        break;
                    case "list":
                        result.AppendLine();
                        result.Append(ProcessXmlContent(childElement));
                        break;
                    case "item":
                        result.Append("- ");
                        result.Append(ProcessXmlContent(childElement));
                        result.AppendLine();
                        break;
                    default:
                        result.Append(ProcessXmlContent(childElement));
                        break;
                }
            }
        }

        return result.ToString();
    }

    private static string NormalizeWhitespace(string text)
    {
        return WhitespaceRegex().Replace(text, " ");
    }

    private static string ExtractTypeFromCref(string cref)
    {
        if (string.IsNullOrEmpty(cref))
            return string.Empty;

        // Remove prefix like T:, M:, P:, F:, E:
        if (cref.Length > 2 && cref[1] == ':')
        {
            cref = cref[2..];
        }

        // Remove method parameters for display
        var parenIndex = cref.IndexOf('(');
        if (parenIndex > 0)
        {
            cref = cref[..parenIndex];
        }

        // Get just the simple name (last segment)
        var lastDot = cref.LastIndexOf('.');
        if (lastDot > 0)
        {
            return cref[(lastDot + 1)..];
        }

        return cref;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
