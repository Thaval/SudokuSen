# MySudoku Deployment Guide

This folder contains everything needed to build and deploy MySudoku releases.

## 📁 Folder Structure

```
Deploy/
├── Deploy.ps1              # Main deployment script
├── README.md               # This file
├── Changelog_X_X_X.md      # Changelog for each version
└── releases/
    └── {version}/          # Output folders (e.g., 0.0.1, 0.0.2)
        ├── MySudoku.exe
        ├── README.md       # User guide (from Presentation)
        ├── CHANGELOG.md    # What's new
        └── screenshots/
```

---

## 🚀 Deployment Checklist

Before running the deploy script, complete these steps **in order**:

### 1. Update Version Number

Edit `MySudoku.csproj` and update the `<Version>` tag:

```xml
<PropertyGroup>
    <Version>0.0.2</Version>  <!-- Update this -->
</PropertyGroup>
```

### 2. Create Changelog

Create `Deploy/Changelog_X_X_X.md` (replace X_X_X with version, e.g., `0_0_2`):

```markdown
# MySudoku v0.0.2 Changelog

## 🎉 New Features
- Feature description

## 🐛 Bug Fixes
- Fix description

## 🔧 Improvements
- Improvement description
```

### 3. Create/Update Presentation

Create or update `Docs/Presentation/Presentation_X_X_X.md`:

This is the user-facing README that ships with the release. Include:
- Quick start guide
- Feature overview
- Screenshots references
- Controls/shortcuts

### 4. Add Screenshots (Optional)

If you have new screenshots, add them to:
```
Docs/Presentation/screenshots/{version}/
```

### 5. Run Deploy Script

```powershell
cd Deploy
.\Deploy.ps1
```

Or with a specific Godot path:
```powershell
.\Deploy.ps1 -GodotPath "C:\Godot\Godot_v4.5-stable_mono_win64.exe"
```

---

## 📋 Quick Reference

| Step | Action | File Location |
|------|--------|---------------|
| 1 | Update version | `MySudoku.csproj` |
| 2 | Write changelog | `Deploy/Changelog_X_X_X.md` |
| 3 | Write presentation | `Docs/Presentation/Presentation_X_X_X.md` |
| 4 | Add screenshots | `Docs/Presentation/screenshots/{version}/` |
| 5 | Run deploy | `Deploy/Deploy.ps1` |

---

## 🔧 Deploy Script Options

```powershell
# Standard deployment
.\Deploy.ps1

# Specify Godot path
.\Deploy.ps1 -GodotPath "C:\Path\To\Godot.exe"

# Skip file checks (for testing)
.\Deploy.ps1 -SkipChecks
```

---

## ⚠️ Prerequisites

1. **Godot 4.5.1 Mono** installed with export templates
2. **.NET 8 SDK** installed
3. **Windows Desktop** export preset configured in Godot

### Installing Export Templates

1. Open Godot Editor
2. Go to **Editor → Manage Export Templates**
3. Click **Download and Install**
4. Wait for completion

### Configuring Export Preset

1. Open project in Godot
2. Go to **Project → Export**
3. Add **Windows Desktop** preset
4. Configure settings as needed
5. Save

---

## 📦 Output

After successful deployment, find the release at:
```
Deploy/releases/{version}/
├── MySudoku.exe           # Main executable
├── MySudoku.pck           # Game data (if separate)
├── data_MySudoku_*/       # .NET runtime files
├── README.md              # User guide
├── CHANGELOG.md           # Version changes
└── screenshots/           # Screenshots for documentation
```

---

## 🔄 Version History

| Version | Date | Notes |
|---------|------|-------|
| 0.0.1 | 2024-XX-XX | Initial release |

---

## ❓ Troubleshooting

### "Missing required files" Error
Create the Changelog and Presentation files as described above.

### "Godot executable not found"
Use `-GodotPath` parameter or add Godot to your PATH.

### "Export templates not installed"
Follow the export templates installation guide above.

### Build Failed
Run `dotnet build` from the project root to see detailed errors.
