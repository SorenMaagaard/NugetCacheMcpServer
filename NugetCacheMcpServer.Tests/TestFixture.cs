using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NugetCacheMcpServer.Cache;
using NugetCacheMcpServer.Configuration;
using NugetCacheMcpServer.Services;
using NugetCacheMcpServer.Tools;

namespace NugetCacheMcpServer.Tests;

/// <summary>
/// Shared test fixture providing access to MCP server components.
/// Uses the test-packages directory which contains TestLibrary 1.0.0 and 2.0.0.
/// </summary>
public class TestFixture : IAsyncDisposable
{
    private static TestFixture? _instance;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public IPackageIndex PackageIndex { get; }
    public IAssemblyInspector AssemblyInspector { get; }
    public IXmlDocumentationParser XmlParser { get; }
    public INuspecParser NuspecParser { get; }
    public AssemblyCache AssemblyCache { get; }
    public string TestCachePath { get; }

    // Tools
    public CacheExplorerTool CacheExplorer { get; }
    public PackageInfoTool PackageInfo { get; }
    public TypeExplorerTool TypeExplorer { get; }
    public DocumentationTool Documentation { get; }
    public VersionCompareTool VersionCompare { get; }

    private TestFixture(string testCachePath)
    {
        TestCachePath = testCachePath;

        var options = Options.Create(new CacheOptions { CachePath = testCachePath });
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));

        PackageIndex = new PackageIndex(options, loggerFactory.CreateLogger<PackageIndex>());
        AssemblyCache = new AssemblyCache(options, loggerFactory.CreateLogger<AssemblyCache>());
        AssemblyInspector = new AssemblyInspector(AssemblyCache, loggerFactory.CreateLogger<AssemblyInspector>());
        XmlParser = new XmlDocumentationParser(loggerFactory.CreateLogger<XmlDocumentationParser>());
        NuspecParser = new NuspecParser(loggerFactory.CreateLogger<NuspecParser>());

        // Create tools
        CacheExplorer = new CacheExplorerTool(PackageIndex);
        PackageInfo = new PackageInfoTool(PackageIndex, NuspecParser);
        TypeExplorer = new TypeExplorerTool(PackageIndex, AssemblyInspector, XmlParser);
        Documentation = new DocumentationTool(PackageIndex, AssemblyInspector, XmlParser);
        VersionCompare = new VersionCompareTool(PackageIndex, AssemblyInspector);
    }

    public static async Task<TestFixture> GetInstanceAsync()
    {
        if (_instance != null)
            return _instance;

        await _lock.WaitAsync();
        try
        {
            if (_instance != null)
                return _instance;

            // Find the test-packages directory relative to the test assembly
            var testCachePath = FindTestPackagesPath();
            _instance = new TestFixture(testCachePath);
            await ((PackageIndex)_instance.PackageIndex).InitializeAsync();
            return _instance;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string FindTestPackagesPath()
    {
        // Start from the current directory and search upward for TestLibraries/test-packages
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var testPackagesPath = Path.Combine(dir, "TestLibraries", "test-packages");
            if (Directory.Exists(testPackagesPath))
                return testPackagesPath;

            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: try relative to solution root
        var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var fallbackPath = Path.Combine(solutionDir, "TestLibraries", "test-packages");

        if (Directory.Exists(fallbackPath))
            return fallbackPath;

        throw new DirectoryNotFoundException(
            "Could not find TestLibraries/test-packages directory. " +
            "Run build-test-packages.ps1 first to create the test packages.");
    }

    public async ValueTask DisposeAsync()
    {
        AssemblyCache.Dispose();
        await Task.CompletedTask;
    }
}
