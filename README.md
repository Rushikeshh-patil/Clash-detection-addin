# Clash Detection Add-in (Revit 2025 MVP)

This repository now contains a working MVP foundation for a Revit clash detection add-in:

- Revit ribbon integration (`Run Clash`, `Open Pane`)
- Dockable WPF pane for config CRUD, run controls, and results
- Manual + auto detection modes with debounce
- Multiple active clash configs with model pair selection (host/link)
- Category pair rules, severity thresholds, and rule-based suggestions
- CSV export and per-user JSON settings persistence
- Unit tests for core logic

## Project Structure

- `Clashdetector/Clashdetector.csproj`: Revit add-in (WPF + Revit API integration)
- `Clashdetector/Clashdetector.Core/`: testable core models/contracts/services
- `Clashdetector/Clashdetector.Core.Tests/`: xUnit tests for core logic

## Build and Test

From `Clashdetector/`:

```powershell
dotnet build Clashdetector.sln
dotnet test Clashdetector.sln
```

## Dev-Local Revit Manifest Setup

1. Build the add-in project.
2. Run:

```powershell
.\Clashdetector\Deployment\Install-RevitAddin.ps1
```

If needed, pass an explicit DLL path:

```powershell
.\Clashdetector\Deployment\Install-RevitAddin.ps1 -AssemblyPath "C:\Git\Clash-detection-addin\Clashdetector\bin\Debug\net8.0-windows\Clashdetector.dll"
```

3. Start Revit 2025 and open the `BirdTools` ribbon tab.

## Runtime Data Paths

- Settings: `%AppData%\ClashDetector\settings.json`
- Logs: `%AppData%\ClashDetector\logs\`

## Manual Validation Checklist

See `docs/MANUAL_VALIDATION_CHECKLIST.md`.
