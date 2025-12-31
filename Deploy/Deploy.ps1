#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys SudokuSen to a versioned output directory with executable and documentation.

.DESCRIPTION
    This script:
    1. Reads the version from SudokuSen.csproj
    2. Verifies required files exist (Changelog, Presentation)
    3. Builds the C# project in Release mode
    4. Exports the Godot project to create the executable
    5. Creates output directory structure: /Deploy/releases/{version}/
    6. Copies the executable, README, and Changelog to the output folder
    7. Copies screenshots folder if present
    8. Optionally creates a GitHub Release with the ZIP archive

.PARAMETER GodotPath
    Path to Godot executable. If not provided, searches in PATH and common locations.

.PARAMETER SkipChecks
    Skip pre-deployment checks (Changelog, Presentation verification)

.PARAMETER CreateGitHubRelease
    After deployment, create a GitHub release with the ZIP archive.
    Requires GitHub CLI (gh) to be installed and authenticated.

.EXAMPLE
    .\Deploy.ps1

    Deploys the current version (e.g., 0.0.2) to .\releases\0.0.2\

.EXAMPLE
    .\Deploy.ps1 -GodotPath "C:\Godot\Godot_v4.5-stable_mono_win64.exe"

    Uses a specific Godot executable for export

.EXAMPLE
    .\Deploy.ps1 -CreateGitHubRelease

    Deploys and automatically creates a GitHub release
#>

[CmdletBinding()]
param(
    [string]$GodotPath = "",
    [switch]$SkipChecks,
    [switch]$CreateGitHubRelease
)

# Configuration
$DeployDir = $PSScriptRoot
$ProjectDir = Split-Path $DeployDir -Parent
$ProjectFile = Join-Path $ProjectDir "SudokuSen.csproj"
$OutputBaseDir = Join-Path $DeployDir "releases"
$DocsDir = Join-Path $ProjectDir "Docs"
$PresentationDir = Join-Path $DocsDir "Presentation"
$ScreenshotsSource = Join-Path $PresentationDir "screenshots"
$GodotExportDir = Join-Path $env:TEMP "SudokuSen_Export"

# Step 1: Read version from .csproj
Write-Host "📋 Reading version from SudokuSen.csproj..." -ForegroundColor Cyan

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

[xml]$csproj = Get-Content $ProjectFile
$version = $csproj.Project.PropertyGroup.Version

if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Error "Could not find <Version> in SudokuSen.csproj"
    exit 1
}

Write-Host "✓ Version detected: $version" -ForegroundColor Green

# Step 2: Verify required files exist
$versionUnderscore = $version.Replace('.', '_')
$changelogPath = Join-Path $DeployDir "Changelog_$versionUnderscore.md"
$presentationPath = Join-Path $PresentationDir "Presentation_$versionUnderscore.md"

if (-not $SkipChecks) {
    Write-Host "`n🔍 Verifying required files..." -ForegroundColor Cyan

    $missingFiles = @()

    if (-not (Test-Path $changelogPath)) {
        $missingFiles += "Changelog_$versionUnderscore.md (in Deploy folder)"
    }

    if (-not (Test-Path $presentationPath)) {
        $missingFiles += "Presentation_$versionUnderscore.md (in Docs/Presentation folder)"
    }

    if ($missingFiles.Count -gt 0) {
        Write-Error "Missing required files for version $version!"
        Write-Host "`n❌ Missing files:" -ForegroundColor Red
        foreach ($file in $missingFiles) {
            Write-Host "   - $file" -ForegroundColor Yellow
        }
        Write-Host "`n📝 Please create these files before deploying." -ForegroundColor Yellow
        Write-Host "   See Deploy/README.md for the deployment checklist." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✓ All required files found" -ForegroundColor Green
}

# Step 3: Find Godot executable
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
            "$env:USERPROFILE\Downloads\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe",
            "C:\Godot\Godot_v4.5.1-stable_mono_win64.exe",
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

# Step 4: Set up output paths
$VersionedOutputDir = Join-Path $OutputBaseDir $version

Write-Host "`n🏭️  Building SudokuSen v$version..." -ForegroundColor Cyan

# Step 5: Build C# project in Release mode
Write-Host "`n⚙️  Building C# project in Release mode..." -ForegroundColor Cyan
Push-Location $ProjectDir
$buildOutput = dotnet build --configuration Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    Write-Host $buildOutput -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host "✓ C# build successful" -ForegroundColor Green

# Step 6: Export using Godot (if available)
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
    Push-Location $ProjectDir
    $exportArgs = @(
        "--headless",
        "--export-release",
        "Windows Desktop",
        (Join-Path $GodotExportDir "SudokuSen.exe")
    )

    Write-Host "Running: $GodotPath $($exportArgs -join ' ')" -ForegroundColor Gray
    & $GodotPath $exportArgs 2>&1 | Write-Host
    Pop-Location

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Godot export failed or export preset not found"
        Write-Host "Please ensure you have a 'Windows Desktop' export preset configured" -ForegroundColor Yellow
        $SkipGodotExport = $true
    } else {
        Write-Host "✓ Godot export successful" -ForegroundColor Green
    }
}

# Step 7: Create versioned output directory
Write-Host "`n📁 Creating output directory: $VersionedOutputDir" -ForegroundColor Cyan
if (Test-Path $VersionedOutputDir) {
    Write-Host "Removing existing output folder..." -ForegroundColor Yellow
    Remove-Item $VersionedOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $VersionedOutputDir -Force | Out-Null
Write-Host "✓ Directory created" -ForegroundColor Green

# Step 8: Copy exported files
if (-not $SkipGodotExport -and (Test-Path $GodotExportDir)) {
    Write-Host "`n📋 Copying exported application files..." -ForegroundColor Cyan
    $exeName = "SudokuSen.exe"
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

# Step 9: Copy README (Presentation)
Write-Host "`n📄 Copying README (Presentation)..." -ForegroundColor Cyan
$readmeDest = Join-Path $VersionedOutputDir "README.md"

if (Test-Path $presentationPath) {
    Copy-Item $presentationPath $readmeDest -Force
    Write-Host "✓ README copied: Presentation_$versionUnderscore.md → README.md" -ForegroundColor Green
} else {
    Write-Warning "Presentation not found: $presentationPath"
}

# Step 10: Copy Changelog
Write-Host "`n📝 Copying Changelog..." -ForegroundColor Cyan
$changelogDest = Join-Path $VersionedOutputDir "CHANGELOG.md"

if (Test-Path $changelogPath) {
    Copy-Item $changelogPath $changelogDest -Force
    Write-Host "✓ Changelog copied: Changelog_$versionUnderscore.md → CHANGELOG.md" -ForegroundColor Green
} else {
    Write-Warning "Changelog not found: $changelogPath"
}

# Step 11: Copy screenshots folder
Write-Host "`n🖼️  Copying screenshots..." -ForegroundColor Cyan
$screenshotsVersioned = Join-Path $ScreenshotsSource $version
$screenshotsDest = Join-Path $VersionedOutputDir "screenshots"

if (Test-Path $screenshotsVersioned) {
    New-Item -ItemType Directory -Path $screenshotsDest -Force | Out-Null
    Copy-Item "$screenshotsVersioned\*" $screenshotsDest -Recurse -Force
    Write-Host "✓ Screenshots copied" -ForegroundColor Green
} else {
    Write-Warning "Screenshots not found: $screenshotsVersioned"
}

# Step 12: Summary
Write-Host "`n✅ Deployment complete!" -ForegroundColor Green
Write-Host "`n📦 Package location:" -ForegroundColor Cyan
Write-Host "   $VersionedOutputDir" -ForegroundColor White

if (-not $SkipGodotExport) {
    Write-Host "`n📋 Contents:" -ForegroundColor Cyan
    Write-Host "   - SudokuSen.exe" -ForegroundColor White
    Write-Host "   - README.md (User Guide)" -ForegroundColor White
    Write-Host "   - CHANGELOG.md" -ForegroundColor White
    Write-Host "   - screenshots/ (if available)" -ForegroundColor White
    Write-Host "   - All required dependencies" -ForegroundColor White

    $exePath = Join-Path $VersionedOutputDir "SudokuSen.exe"
    if (Test-Path $exePath) {
        $exeSize = (Get-Item $exePath).Length / 1MB
        Write-Host "`n📊 Executable size: $($exeSize.ToString('F2')) MB" -ForegroundColor Cyan
        Write-Host "`n🚀 Ready to distribute!" -ForegroundColor Green
        Write-Host "   Run: $VersionedOutputDir\SudokuSen.exe" -ForegroundColor Yellow
    }
} else {
    Write-Host "`n⚠️  Manual Godot export required:" -ForegroundColor Yellow
    Write-Host "   1. Open project in Godot Editor" -ForegroundColor White
    Write-Host "   2. Go to Project → Export" -ForegroundColor White
    Write-Host "   3. Export to: $VersionedOutputDir\SudokuSen.exe" -ForegroundColor White
    Write-Host "   4. README and CHANGELOG are already in place" -ForegroundColor White
}

# Step 13: Create GitHub Release (optional)
if ($CreateGitHubRelease) {
    Write-Host "`n🚀 Creating GitHub Release..." -ForegroundColor Cyan

    # Check if gh CLI is installed (check PATH and common locations)
    $ghCmd = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $ghCmd) {
        # Check common installation locations
        $commonPaths = @(
            "C:\Program Files\GitHub CLI\gh.exe",
            "C:\Program Files (x86)\GitHub CLI\gh.exe",
            "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe"
        )
        
        foreach ($path in $commonPaths) {
            if (Test-Path $path) {
                $ghCmd = @{ Source = $path }
                break
            }
        }
    }
    
    if (-not $ghCmd) {
        Write-Warning "GitHub CLI (gh) not found!"
        Write-Host "`n📥 Install GitHub CLI:" -ForegroundColor Yellow
        Write-Host "   winget install GitHub.cli" -ForegroundColor White
        Write-Host "   Then run: gh auth login" -ForegroundColor White
    } else {
        # Create ZIP file for release
        $zipName = "SudokuSen-v$version-windows.zip"
        $zipPath = Join-Path $DeployDir $zipName

        Write-Host "📦 Creating release archive: $zipName" -ForegroundColor Cyan
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path "$VersionedOutputDir\*" -DestinationPath $zipPath -Force
        Write-Host "✓ Archive created" -ForegroundColor Green

        # Create git tag if not exists
        $tagName = "v$version"
        $existingTag = git tag -l $tagName 2>$null
        if (-not $existingTag) {
            Write-Host "🏷️  Creating git tag: $tagName" -ForegroundColor Cyan
            Push-Location $ProjectDir
            git tag -a $tagName -m "SudokuSen $tagName"
            git push --tags 2>$null
            Pop-Location
            Write-Host "✓ Tag created and pushed" -ForegroundColor Green
        }

        # Read changelog for release notes
        $releaseNotes = ""
        if (Test-Path $changelogPath) {
            $releaseNotes = Get-Content $changelogPath -Raw
        }

        # Create GitHub release
        Write-Host "📤 Publishing to GitHub..." -ForegroundColor Cyan
        Push-Location $ProjectDir

        $ghArgs = @(
            "release", "create", $tagName,
            $zipPath,
            "--title", "SudokuSen $tagName",
            "--notes-file", $changelogPath
        )

        & $ghCmd.Source @ghArgs 2>&1 | Write-Host

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ GitHub Release created successfully!" -ForegroundColor Green
            Write-Host "`n🌐 View release at: https://github.com/$(& $ghCmd.Source repo view --json nameWithOwner -q .nameWithOwner)/releases/tag/$tagName" -ForegroundColor Cyan
        } else {
            Write-Warning "GitHub release creation failed. You may need to run 'gh auth login' first."
        }

        Pop-Location

        # Cleanup ZIP
        # Remove-Item $zipPath -Force  # Uncomment to auto-delete ZIP after upload
    }
}
