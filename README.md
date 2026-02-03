# NuGet Cache MCP Server

A Model Context Protocol (MCP) server that provides AI assistants with tools to explore and analyze NuGet packages from your local cache.

## Features

- **Package Discovery**: List and search cached NuGet packages
- **Type Exploration**: List types with wildcard filtering, view type definitions with full member details
- **Version Comparison**: Compare API changes between package versions with breaking change detection
- **XML Documentation**: Access XML documentation for methods and types
- **Meta-package Detection**: Identifies packages that contain only dependencies (no code)

## Installation

### Using Docker

```bash
docker pull ghcr.io/YOUR_USERNAME/testnugetmcp:latest
```

### Download Binary

Download the latest release from the [Releases](../../releases) page:
- `nuget-cache-mcp-server-win-x64.zip` - Windows x64
- `nuget-cache-mcp-server-linux-x64.tar.gz` - Linux x64

### Build from Source

Requires .NET 10 SDK.

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/TestNugetMcp.git
cd TestNugetMcp

# Build
dotnet build NugetCacheMcpServer.slnx

# Publish self-contained executable
dotnet publish NugetCacheMcpServer -c Release -r win-x64 -o ./publish/win-x64
dotnet publish NugetCacheMcpServer -c Release -r linux-x64 -o ./publish/linux-x64
```

## Configuration

### Claude Desktop

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "nuget-cache": {
      "command": "path/to/NugetCacheMcpServer.exe",
      "env": {
        "NUGET_CACHE_PATH": "C:/Users/YourName/.nuget/packages"
      }
    }
  }
}
```

### Docker

```json
{
  "mcpServers": {
    "nuget-cache": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-v", "/path/to/.nuget/packages:/nuget-cache:ro",
        "ghcr.io/YOUR_USERNAME/testnugetmcp:latest"
      ]
    }
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `NUGET_CACHE_PATH` | Path to NuGet packages cache | `~/.nuget/packages` |

## Available Tools

### `list_cached_packages`
List packages in the NuGet cache with optional filtering and pagination.

**Parameters:**
- `filter` (optional): Filter packages by name pattern
- `cursor` (optional): Pagination cursor
- `pageSize` (optional): Number of results per page (default: 50)

### `get_package_info`
Get detailed information about a cached package.

**Parameters:**
- `packageId`: The NuGet package ID
- `version` (optional): Specific version (defaults to latest)

### `list_types`
List public types in a package with filtering options.

**Parameters:**
- `packageId`: The NuGet package ID
- `version` (optional): Package version
- `namespaceFilter` (optional): Filter by namespace prefix
- `typeKind` (optional): Filter by type kind (class, interface, enum, struct, delegate)
- `filter` (optional): Wildcard pattern for type names (`*Converter`, `*Message*`, `Api.Models.*`)

### `get_type_definition`
Get full definition of a type including members, inheritance, and documentation.

**Parameters:**
- `packageId`: The NuGet package ID
- `typeName`: The type name (simple or full name)
- `version` (optional): Package version

### `get_method_documentation`
Get detailed documentation for a specific method.

**Parameters:**
- `packageId`: The NuGet package ID
- `typeName`: The containing type name
- `methodName`: The method name
- `version` (optional): Package version

### `compare_package_versions`
Compare API changes between two versions of a package.

**Parameters:**
- `packageId`: The NuGet package ID
- `fromVersion`: The older version
- `toVersion`: The newer version

## Development

### Prerequisites

- .NET 10 SDK
- Docker (optional, for container builds)

### Running Tests

```bash
dotnet test --project NugetCacheMcpServer.Tests
```

### Building Docker Image

```bash
docker build -t nuget-cache-mcp-server .
```

## Credits

This project was inspired by [DimonSmart/NugetMcpServer](https://github.com/DimonSmart/NugetMcpServer), which provides similar functionality using NuGet feeds. This implementation focuses on the local NuGet cache for offline and faster access.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
