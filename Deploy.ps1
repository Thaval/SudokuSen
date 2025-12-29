#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys MySudoku to a versioned output directory with executable and documentation.

.DESCRIPTION
    This script:
    1. Reads the version from MySudoku.csproj
    2. Builds the C# project in Release mode
    3. Exports the Godot project to create the executable
    4. Creates output directory structure: /output/{version}/
    5. Copies the executable and README to the output folder
    6. Copies screenshots folder if present

.PARAMETER GodotPath
    Path to Godot executable. If not provided, searches in PATH and common locations.

.EXAMPLE
    .\Deploy.ps1

    Deploys the current version (e.g., 0.0.1) to .\output\0.0.1\

.EXAMPLE
    .\Deploy.ps1 -GodotPath "C:\Godot\Godot_v4.5-stable_mono_win64.exe"

    Uses a specific Godot executable for export
#>

[CmdletBinding()]
param(
    [string]$GodotPath = ""
)

# Configuration
$ProjectFile = "MySudoku.csproj"
$ProjectDir = $PSScriptRoot
# Output folder is at project root/release
$OutputBaseDir = Join-Path $ProjectDir "release"
$PresentationSource = Join-Path $ProjectDir "Docs\Presentation"
$ScreenshotsSource = Join-Path $ProjectDir "Docs\Presentation\screenshots"
$GodotExportDir = Join-Path $env:TEMP "MySudoku_Export"

# Step 1: Read version from .csproj
Write-Host "📋 Reading version from $ProjectFile..." -ForegroundColor Cyan

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

[xml]$csproj = Get-Content $ProjectFile
$version = $csproj.Project.PropertyGroup.Version

if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Error "Could not find <Version> in $ProjectFile"
    exit 1
}

Write-Host "✓ Version detected: $version" -ForegroundColor Green

# Step 2: Find Godot executable
Write-Host "`n🔍 Locating Godot executable..." -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($GodotPath)) {
    # Try to find in PATH
    $godotCmd = Get-Command godot -ErrorAction SilentlyContinue
    if ($godotCmd) {
        $GodotPath = $godotCmd.Source
    }

    # Try common locations
    if ([string]::IsNullOrWhiteSpace($GodotPath)) {
        $commonPaths = @(
            "C:\Godot\Godot_v4.5-stable_mono_win64.exe",
            "C:\Program Files\Godot\Godot.exe",
            "$env:LOCALAPPDATA\Godot\Godot.exe"
        )

        foreach ($path in $commonPaths) {
            if (Test-Path $path) {
                $GodotPath = $path
                break
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($GodotPath) -or (-not (Test-Path $GodotPath))) {
    Write-Warning "Godot executable not found!"
    Write-Host "`n⚠️  Please specify Godot path using -GodotPath parameter" -ForegroundColor Yellow
    Write-Host "   Example: .\Deploy.ps1 -GodotPath 'C:\Path\To\Godot.exe'" -ForegroundColor Yellow
    Write-Host "`n📝 Skipping Godot export. Manual export required." -ForegroundColor Yellow
    $SkipGodotExport = $true
} else {
    Write-Host "✓ Found Godot at: $GodotPath" -ForegroundColor Green
    $SkipGodotExport = $false
}

# Step 3: Set up output paths
$VersionedOutputDir = Join-Path $OutputBaseDir $version

Write-Host "`n🏗️  Building MySudoku v$version..." -ForegroundColor Cyan

# Step 4: Build C# project in Release mode
Write-Host "`n⚙️  Building C# project in Release mode..." -ForegroundColor Cyan
$buildOutput = dotnet build --configuration Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    Write-Host $buildOutput -ForegroundColor Red
    exit 1
}
Write-Host "✓ C# build successful" -ForegroundColor Green

# Step 5: Export using Godot (if available)
if (-not $SkipGodotExport) {
    Write-Host "`n📦 Exporting Godot project..." -ForegroundColor Cyan

    # Check if export templates are installed
    $godotVersion = "4.5.1.stable.mono"
    $templatesPath = Join-Path $env:APPDATA "Godot\export_templates\$godotVersion"
    if (-not (Test-Path $templatesPath)) {
        Write-Warning "Export templates not installed!"
        Write-Host "`n📥 To install export templates:" -ForegroundColor Yellow
        Write-Host "   1. Open Godot Editor" -ForegroundColor White
        Write-Host "   2. Go to Editor → Manage Export Templates" -ForegroundColor White
        Write-Host "   3. Click 'Download and Install'" -ForegroundColor White
        Write-Host "   4. Wait for download to complete, then run this script again" -ForegroundColor White
        $SkipGodotExport = $true
    }
}

if (-not $SkipGodotExport) {
    # Create temporary export directory
    if (Test-Path $GodotExportDir) {
        Remove-Item $GodotExportDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $GodotExportDir -Force | Out-Null

    # Export the project (Windows Desktop)
    $exportArgs = @(
        "--headless",
        "--export-release",
        "Windows Desktop",
        (Join-Path $GodotExportDir "MySudoku.exe")
    )

    Write-Host "Running: $GodotPath $($exportArgs -join ' ')" -ForegroundColor Gray
    & $GodotPath $exportArgs 2>&1 | Write-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Godot export failed or export preset not found"
        Write-Host "Please ensure you have a 'Windows Desktop' export preset configured" -ForegroundColor Yellow
        $SkipGodotExport = $true
    } else {
        Write-Host "✓ Godot export successful" -ForegroundColor Green
    }
}

# Step 6: Create versioned output directory
Write-Host "`n📁 Creating output directory: $VersionedOutputDir" -ForegroundColor Cyan
if (Test-Path $VersionedOutputDir) {
    Write-Host "Removing existing output folder..." -ForegroundColor Yellow
    Remove-Item $VersionedOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $VersionedOutputDir -Force | Out-Null
Write-Host "✓ Directory created" -ForegroundColor Green

# Step 7: Copy exported files
if (-not $SkipGodotExport -and (Test-Path $GodotExportDir)) {
    Write-Host "`n📋 Copying exported application files..." -ForegroundColor Cyan
    $exeName = "MySudoku.exe"
    $exePath = Join-Path $GodotExportDir $exeName

    if (Test-Path $exePath) {
        # Copy all files from export directory
        Copy-Item -Path "$GodotExportDir\*" -Destination $VersionedOutputDir -Recurse -Force
        Write-Host "✓ Application files copied" -ForegroundColor Green
    } else {
        Write-Warning "Exported executable not found at $exePath"
        $SkipGodotExport = $true
    }
}

# Step 8: Copy README (Presentation)
Write-Host "`n📄 Copying README..." -ForegroundColor Cyan
$readmeSource = Join-Path $PresentationSource "Presentation_$($version.Replace('.', '_')).md"
$readmeDest = Join-Path $VersionedOutputDir "README.md"

if (Test-Path $readmeSource) {
    Copy-Item $readmeSource $readmeDest -Force
    Write-Host "✓ README copied: $(Split-Path $readmeSource -Leaf) → README.md" -ForegroundColor Green
} else {
    Write-Warning "README not found: $readmeSource"
}

# Step 9: Copy screenshots folder
Write-Host "`n🖼️  Copying screenshots..." -ForegroundColor Cyan
$screenshotsVersioned = Join-Path $ScreenshotsSource $version
$screenshotsDest = Join-Path $VersionedOutputDir "screenshots\$version"

if (Test-Path $screenshotsVersioned) {
    New-Item -ItemType Directory -Path (Split-Path $screenshotsDest -Parent) -Force | Out-Null
    Copy-Item $screenshotsVersioned $screenshotsDest -Recurse -Force
    Write-Host "✓ Screenshots copied: $version folder" -ForegroundColor Green
} else {
    Write-Warning "Screenshots not found: $screenshotsVersioned"
}

# Step 10: Summary
Write-Host "`n✅ Deployment complete!" -ForegroundColor Green
Write-Host "`n📦 Package location:" -ForegroundColor Cyan
Write-Host "   $VersionedOutputDir" -ForegroundColor White

if (-not $SkipGodotExport) {
    Write-Host "`n📋 Contents:" -ForegroundColor Cyan
    Write-Host "   - MySudoku.exe" -ForegroundColor White
    Write-Host "   - README.md (User Guide)" -ForegroundColor White
    Write-Host "   - screenshots/ (if available)" -ForegroundColor White
    Write-Host "   - All required dependencies" -ForegroundColor White

    $exePath = Join-Path $VersionedOutputDir "MySudoku.exe"
    if (Test-Path $exePath) {
        $exeSize = (Get-Item $exePath).Length / 1MB
        Write-Host "`n📊 Executable size: $($exeSize.ToString('F2')) MB" -ForegroundColor Cyan
        Write-Host "`n🚀 Ready to distribute!" -ForegroundColor Green
        Write-Host "   Run: $VersionedOutputDir\MySudoku.exe" -ForegroundColor Yellow
    }
} else {
    Write-Host "`n⚠️  Manual Godot export required:" -ForegroundColor Yellow
    Write-Host "   1. Open project in Godot Editor" -ForegroundColor White
    Write-Host "   2. Go to Project → Export" -ForegroundColor White
    Write-Host "   3. Export to: $VersionedOutputDir\MySudoku.exe" -ForegroundColor White
    Write-Host "   4. README and screenshots are already in place" -ForegroundColor White
}
