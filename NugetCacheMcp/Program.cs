using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NuGetCacheMcp.Cache;
using NuGetCacheMcp.Configuration;
using NuGetCacheMcp.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging - only warnings and errors to avoid MCP client treating info as warnings
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Configure options
builder.Services.Configure<CacheOptions>(options =>
{
    // Can be overridden via environment variable
    var customPath = Environment.GetEnvironmentVariable("NUGET_CACHE_PATH");
    if (!string.IsNullOrEmpty(customPath))
    {
        options.CachePath = customPath;
    }
});

// Register services
builder.Services.AddSingleton<PackageIndex>();
builder.Services.AddSingleton<IPackageIndex>(sp => sp.GetRequiredService<PackageIndex>());
builder.Services.AddSingleton<IAssemblyInspector, AssemblyInspector>();
builder.Services.AddSingleton<IXmlDocumentationParser, XmlDocumentationParser>();
builder.Services.AddSingleton<INuspecParser, NuspecParser>();
builder.Services.AddSingleton<AssemblyCache>();

// Configure MCP server
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "nuget-cache",
        Version = "1.0.0"
    };
})
.WithStdioServerTransport()
.WithToolsFromAssembly(typeof(Program).Assembly);

var host = builder.Build();

// Initialize the package index on startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var packageIndex = host.Services.GetRequiredService<PackageIndex>();

logger.LogInformation("NuGet Cache MCP Server starting...");
await packageIndex.InitializeAsync();
logger.LogInformation("Ready to serve requests. Found {Count} packages.", packageIndex.PackageCount);

await host.RunAsync();
