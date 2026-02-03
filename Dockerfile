# Multi-stage build for minimal container size
# Stage 1: Build the self-contained application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore
COPY NugetCacheMcpServer/*.csproj NugetCacheMcpServer/
RUN dotnet restore NugetCacheMcpServer/NugetCacheMcpServer.csproj -r linux-x64

# Copy source and publish
COPY NugetCacheMcpServer/ NugetCacheMcpServer/
RUN dotnet publish NugetCacheMcpServer/NugetCacheMcpServer.csproj \
    -c Release \
    -r linux-x64 \
    -o /app \
    --no-restore

# Stage 2: Create minimal runtime image
# Using runtime-deps for self-contained apps (no .NET runtime needed)
# Alpine variant is the smallest (~10MB base)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS runtime

# Create non-root user for security
RUN adduser -D -u 1000 appuser

WORKDIR /app

# Copy the self-contained executable
COPY --from=build /app/NugetCacheMcpServer .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# The MCP server communicates via stdio, so no port exposure needed
# Environment variable to configure NuGet cache path (mounted at runtime)
ENV NUGET_CACHE_PATH=/nuget-cache

# Health check is not applicable for stdio-based MCP servers

ENTRYPOINT ["./NugetCacheMcpServer"]
