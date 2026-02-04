# NuGet Cache MCP Server

[![Install in VS Code](https://img.shields.io/badge/Install_MCP_with_docker-VS_Code-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://vscode.dev/redirect/mcp/install?name=nuget-cache&config=%7B%22command%22%3A%22docker%22%2C%22args%22%3A%5B%22run%22%2C%22-i%22%2C%22--rm%22%2C%22-v%22%2C%22~%2F.nuget%2Fpackages%3A%2Fnuget-cache%22%2C%22-e%22%2C%22NUGET_CACHE_PATH%22%2C%22ghcr.io%2Fsorenmaagaard%2Fnugetcachemcp%3Alatest%22%5D%2C%22env%22%3A%7B%22NUGET_CACHE_PATH%22%3A%22%2Fnuget-cache%22%7D%7D)
[![Install in VS Code](https://img.shields.io/badge/Install_MCP_with_dnx-VS_Code-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://vscode.dev/redirect/mcp/install?name=nuget-cache&config=%7B%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22Maagaard.NuGetCacheMcp%22%2C%22--yes%22%5D%2C%22env%22%3A%7B%7D%7D)

[![Install in Visual Studio](https://img.shields.io/badge/Install_MCP_with_docker-Visual_Studio-C16FDE?style=flat-square&logo=visualstudio&logoColor=white)](https://vs-open.link/mcp-install?%7B%22command%22%3A%22docker%22%2C%22args%22%3A%5B%22run%22%2C%22-i%22%2C%22--rm%22%2C%22-v%22%2C%22~%2F.nuget%2Fpackages%3A%2Fnuget-cache%22%2C%22-e%22%2C%22NUGET_CACHE_PATH%22%2C%22ghcr.io%2Fsorenmaagaard%2Fnugetcachemcp%3Alatest%22%5D%2C%22env%22%3A%7B%22NUGET_CACHE_PATH%22%3A%22%2Fnuget-cache%22%7D%7D)
[![Install in Visual Studio](https://img.shields.io/badge/Install_MCP_with_dnx-Visual_Studio-C16FDE?style=flat-square&logo=visualstudio&logoColor=white)](https://vs-open.link/mcp-install?%7B%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22Maagaard.NuGetCacheMcp%22%2C%22--yes%22%5D%2C%22env%22%3A%7B%7D%7D)

![NuGet Version](https://img.shields.io/nuget/v/Maagaard.NuGetCacheMcp) 
## Features

- **Package Discovery**: List and search cached NuGet packages
- **Type Exploration**: List types with wildcard filtering, view type definitions with full member details
- **Version Comparison**: Compare API changes between package versions with breaking change detection
- **XML Documentation**: Access XML documentation for methods and types
- **Meta-package Detection**: Identifies packages that contain only dependencies (no code)

## Installation

### .NET Tool Using dnx (no install)
Requires .NET 10

```json
{
  "servers": {
    "nuget-cache": {
      "command": "dnx",
      "args": ["Maagaard.NuGetCacheMcp", "--yes"],
      "env": {
        "NUGET_CACHE_PATH": "C:/custom/nuget/cache" //Optional. Default value: ~/.nuget/packages
      }
    }
  }
}

```
### .NET Tool installed locally
Install as a global .NET tool. Requires .NET 10.

```bash
dotnet tool install -g Maagaard.NuGetCacheMcp
```

mcp configuration (e.g. `mcp.json`):

```json
{
  "servers": {
    "nuget-cache": {
      "command": "nuget-cache-mcp",
      "env": {
        "NUGET_CACHE_PATH": "C:/custom/nuget/cache" //Optional. Default value: ~/.nuget/packages
      }
    }
  }
}
```

### .NET Local executable
Requires .NET 10

Download the latest release from the [Releases](https://github.com/SorenMaagaard/NugetCacheMcp/releases) page:
- `NuGetCacheMcp.exe` - Windows x64
- `NuGetCacheMcp` - Linux x64

mcp configuration (e.g. `mcp.json`):

```json
{
  "servers": {
    "nuget-cache": {
      "command": "C:/path/to/NuGetCacheMcp.exe",
      "env": {
        "NUGET_CACHE_PATH": "C:/custom/nuget/cache" //Optional. Default value: ~/.nuget/packages
      }
    }
  }
}
```

### Docker
If you dont trust this random mcp server (and you probably shouldn't!) you can run it in docker and prevent network access.

> **Note:** When running Docker in WSL with volume mounts to the Windows filesystem, performance may be significantly slower due to cross-filesystem access. The initial traversing of the nuget cache can take minutes. For best performance, consider using the .NET tool or executable directly.

mcp configuration (e.g. `mcp.json`):

```json
{
  "servers": {
    "nuget-cache": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "~/.nuget/packages:/nuget-cache",
        "ghcr.io/sorenmaagaard/nugetcachemcp:latest"
      ],
      "env": {
        "NUGET_CACHE_PATH": "/nuget-cache"
      }
    }
  }
}
```
### Using Claude code?

All the following examples above are for `mcp.json`. For claude code mcp configuration (e.g. `~/.claude.json`) the mcp server should be put into the `mcpServers` section instead of `servers` e.g. 
```json
{
  "mcpServers": {
    "nuget-cache": {
      ...
    }
  }
}
```

### Build from Source

Requires .NET 10 SDK.

```bash
git clone https://github.com/SorenMaagaard/NugetCacheMcp.git
cd NugetCacheMcp
dotnet build NuGetCacheMcp.slnx
dotnet publish NugetCacheMcp/NuGetCacheMcp.csproj -c Release -r win-x64 -o ./publish/win-x64
```

## Environment Variables

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
dotnet test --project NugetCacheMcp.Tests/NuGetCacheMcp.Tests.csproj
```

### Building Docker Image

```bash
docker build -t nugetcachemcp .
```

## Credits

This project was inspired by [DimonSmart/NugetMcpServer](https://github.com/DimonSmart/NugetMcpServer), which provides similar functionality using NuGet feeds. This implementation focuses on the local NuGet cache for offline and faster access.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
