# Build script to pack test libraries and extract them to NuGet cache-like structure
# Usage: .\build-test-packages.ps1

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ScriptDir "test-packages"
$PackageOutputDir = Join-Path $ScriptDir "nupkgs"

Write-Host "Building and packing test libraries..." -ForegroundColor Cyan

# Clean output directories
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
if (Test-Path $PackageOutputDir) {
    Remove-Item -Path $PackageOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $PackageOutputDir -Force | Out-Null

# Pack V1
Write-Host "Packing TestLibrary.V1 (version 1.0.0)..." -ForegroundColor Yellow
$v1Project = Join-Path $ScriptDir "TestLibrary.V1\TestLibrary.V1.csproj"
dotnet pack $v1Project -c Release -o $PackageOutputDir --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack TestLibrary.V1"
    exit 1
}

# Pack V2
Write-Host "Packing TestLibrary.V2 (version 2.0.0)..." -ForegroundColor Yellow
$v2Project = Join-Path $ScriptDir "TestLibrary.V2\TestLibrary.V2.csproj"
dotnet pack $v2Project -c Release -o $PackageOutputDir --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack TestLibrary.V2"
    exit 1
}

# Pack MetaPackage
Write-Host "Packing TestLibrary.MetaPackage (version 1.0.0)..." -ForegroundColor Yellow
$metaProject = Join-Path $ScriptDir "TestLibrary.MetaPackage\TestLibrary.MetaPackage.csproj"
dotnet pack $metaProject -c Release -o $PackageOutputDir --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack TestLibrary.MetaPackage"
    exit 1
}

Write-Host ""
Write-Host "Extracting packages to NuGet cache structure..." -ForegroundColor Cyan

# Function to extract a nupkg to cache-like structure
function Extract-NuGetPackage {
    param (
        [string]$NupkgPath,
        [string]$OutputRoot
    )

    # Get package info from filename (format: PackageId.Version.nupkg)
    $filename = [System.IO.Path]::GetFileNameWithoutExtension($NupkgPath)

    # Read the nuspec from inside the package to get accurate ID and version
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)

    try {
        $nuspecEntry = $zip.Entries | Where-Object { $_.Name -like "*.nuspec" } | Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            Write-Error "No .nuspec found in $NupkgPath"
            return
        }

        $stream = $nuspecEntry.Open()
        $reader = New-Object System.IO.StreamReader($stream)
        $nuspecContent = $reader.ReadToEnd()
        $reader.Close()
        $stream.Close()

        # Parse nuspec XML
        $xml = [xml]$nuspecContent
        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace("nu", $xml.DocumentElement.NamespaceURI)

        $packageId = $xml.SelectSingleNode("//nu:id", $ns).InnerText
        $version = $xml.SelectSingleNode("//nu:version", $ns).InnerText

        Write-Host "  Extracting $packageId v$version..." -ForegroundColor Gray

        # Create target directory: outputRoot/packageId/version/
        $targetDir = Join-Path $OutputRoot ($packageId.ToLower()) | Join-Path -ChildPath $version
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

        # Extract all entries
        foreach ($entry in $zip.Entries) {
            # Skip directories and metadata files we don't need
            if ($entry.FullName.EndsWith('/') -or
                $entry.FullName -eq '[Content_Types].xml' -or
                $entry.FullName -eq '_rels/.rels' -or
                $entry.FullName.StartsWith('package/')) {
                continue
            }

            $targetPath = Join-Path $targetDir $entry.FullName
            $targetFileDir = [System.IO.Path]::GetDirectoryName($targetPath)

            if (-not (Test-Path $targetFileDir)) {
                New-Item -ItemType Directory -Path $targetFileDir -Force | Out-Null
            }

            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $targetPath, $true)
        }
    }
    finally {
        $zip.Dispose()
    }
}

# Extract all packages
Get-ChildItem -Path $PackageOutputDir -Filter "*.nupkg" | ForEach-Object {
    Extract-NuGetPackage -NupkgPath $_.FullName -OutputRoot $OutputDir
}

# Clean up nupkgs folder (optional - keep for debugging)
# Remove-Item -Path $PackageOutputDir -Recurse -Force

Write-Host ""
Write-Host "Test packages created successfully!" -ForegroundColor Green
Write-Host "Location: $OutputDir" -ForegroundColor Gray
Write-Host "NuGet packages: $PackageOutputDir" -ForegroundColor Gray
Write-Host ""
Write-Host "Package structure:" -ForegroundColor Cyan

# Display structure
function Show-Tree {
    param([string]$Path, [int]$Indent = 0)

    $items = Get-ChildItem -Path $Path | Sort-Object { -not $_.PSIsContainer }, Name
    foreach ($item in $items) {
        $prefix = "  " * $Indent
        if ($item.PSIsContainer) {
            Write-Host "$prefix$($item.Name)/" -ForegroundColor Blue
            Show-Tree -Path $item.FullName -Indent ($Indent + 1)
        } else {
            Write-Host "$prefix$($item.Name)" -ForegroundColor Gray
        }
    }
}

Show-Tree -Path $OutputDir
