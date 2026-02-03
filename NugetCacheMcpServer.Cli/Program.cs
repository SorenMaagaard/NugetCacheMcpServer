using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NugetCacheMcpServer.Cache;
using NugetCacheMcpServer.Configuration;
using NugetCacheMcpServer.Services;
using NugetCacheMcpServer.Tools;

// Parse command line args for cache path
var cachePath = args.Length > 0 ? args[0] : null;

if (string.IsNullOrEmpty(cachePath))
{
    cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
}

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         NuGet Cache MCP Server - Interactive CLI              ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"Using cache: {cachePath}");
Console.WriteLine();

// Setup
var options = Options.Create(new CacheOptions { CachePath = cachePath });
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

var packageIndex = new PackageIndex(Options.Create(new CacheOptions { CachePath = cachePath }), loggerFactory.CreateLogger<PackageIndex>());
var assemblyCache = new AssemblyCache(options, loggerFactory.CreateLogger<AssemblyCache>());
var assemblyInspector = new AssemblyInspector(assemblyCache, loggerFactory.CreateLogger<AssemblyInspector>());
var xmlParser = new XmlDocumentationParser(loggerFactory.CreateLogger<XmlDocumentationParser>());
var nuspecParser = new NuspecParser(loggerFactory.CreateLogger<NuspecParser>());

// Initialize
Console.WriteLine("Indexing packages...");
await packageIndex.InitializeAsync();
Console.WriteLine($"Found {packageIndex.PackageCount} packages.");
Console.WriteLine();

// Create tools
var cacheExplorer = new CacheExplorerTool(packageIndex);
var packageInfo = new PackageInfoTool(packageIndex, nuspecParser);
var typeExplorer = new TypeExplorerTool(packageIndex, assemblyInspector, xmlParser);
var docTool = new DocumentationTool(packageIndex, assemblyInspector, xmlParser);
var compareTool = new VersionCompareTool(packageIndex, assemblyInspector);

PrintHelp();

while (true)
{
    Console.Write("\n> ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(input))
        continue;

    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var command = parts[0].ToLowerInvariant();

    try
    {
        switch (command)
        {
            case "help" or "?":
                PrintHelp();
                break;

            case "quit" or "exit" or "q":
                Console.WriteLine("Goodbye!");
                assemblyCache.Dispose();
                return;

            case "list" or "ls":
                var filter = parts.Length > 1 ? parts[1] : null;
                var pageSize = parts.Length > 2 && int.TryParse(parts[2], out var max) ? max : 20;
                Console.WriteLine(cacheExplorer.ListCachedPackages(filter, null, pageSize));
                break;

            case "info" or "i":
                if (parts.Length < 2)
                {
                    Console.WriteLine("Usage: info <packageId> [version]");
                    break;
                }
                var infoVersion = parts.Length > 2 ? parts[2] : null;
                Console.WriteLine(packageInfo.GetPackageInfo(parts[1], infoVersion));
                break;

            case "types" or "t":
                if (parts.Length < 2)
                {
                    Console.WriteLine("Usage: types <packageId> [version] [namespaceFilter]");
                    break;
                }
                var typesVersion = parts.Length > 2 ? parts[2] : null;
                var nsFilter = parts.Length > 3 ? parts[3] : null;
                Console.WriteLine(typeExplorer.ListTypes(parts[1], typesVersion, nsFilter));
                break;

            case "type" or "typedef" or "td":
                if (parts.Length < 3)
                {
                    Console.WriteLine("Usage: type <packageId> <typeName> [version]");
                    break;
                }
                var typeDefVersion = parts.Length > 3 ? parts[3] : null;
                Console.WriteLine(typeExplorer.GetTypeDefinition(parts[1], parts[2], typeDefVersion));
                break;

            case "method" or "m":
                if (parts.Length < 4)
                {
                    Console.WriteLine("Usage: method <packageId> <typeName> <methodName> [version]");
                    break;
                }
                var methodVersion = parts.Length > 4 ? parts[4] : null;
                Console.WriteLine(docTool.GetMethodDocumentation(parts[1], parts[2], parts[3], methodVersion));
                break;

            case "compare" or "diff":
                if (parts.Length < 4)
                {
                    Console.WriteLine("Usage: compare <packageId> <fromVersion> <toVersion>");
                    break;
                }
                Console.WriteLine(compareTool.ComparePackageVersions(parts[1], parts[2], parts[3]));
                break;

            case "versions" or "v":
                if (parts.Length < 2)
                {
                    Console.WriteLine("Usage: versions <packageId>");
                    break;
                }
                var versions = packageIndex.GetPackageVersions(parts[1]).ToList();
                if (versions.Count == 0)
                {
                    Console.WriteLine($"Package '{parts[1]}' not found in cache.");
                }
                else
                {
                    Console.WriteLine($"Versions of {parts[1]}:");
                    foreach (var v in versions)
                    {
                        Console.WriteLine($"  - {v}");
                    }
                }
                break;

            default:
                Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

void PrintHelp()
{
    Console.WriteLine("Commands:");
    Console.WriteLine("  list [filter] [max]                    - List cached packages");
    Console.WriteLine("  versions <packageId>                   - List versions of a package");
    Console.WriteLine("  info <packageId> [version]             - Show package metadata");
    Console.WriteLine("  types <packageId> [version] [nsFilter] - List types in a package");
    Console.WriteLine("  type <packageId> <typeName> [version]  - Show type definition");
    Console.WriteLine("  method <pkg> <type> <method> [version] - Show method documentation");
    Console.WriteLine("  compare <packageId> <from> <to>        - Compare two versions");
    Console.WriteLine("  help                                   - Show this help");
    Console.WriteLine("  quit                                   - Exit");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  list auto                              - Find packages matching 'auto'");
    Console.WriteLine("  info automapper                        - Show AutoMapper info");
    Console.WriteLine("  types automapper 15.0.1                - List types in AutoMapper");
    Console.WriteLine("  type automapper IMapper                - Show IMapper definition");
    Console.WriteLine("  method automapper IMapper Map          - Show Map method docs");
    Console.WriteLine("  compare automapper 12.0.0 15.0.1       - Compare versions");
}
