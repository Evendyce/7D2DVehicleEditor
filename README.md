# 7 Days Vehicle Editor

`7 Days Vehicle Editor` is a Windows desktop tool for editing vehicle configuration values in `7 Days To Die`.

It is designed to be safer and easier than editing `vehicles.xml` by hand, while still exposing advanced values for people who want deeper control.

## What It Does

- Detects the `7 Days To Die` install folder from Steam library paths
- Loads the vanilla vehicle config from `Data\Config\vehicles.xml`
- Scans the game's `Mods` folder for compatible vehicle mods
- Uses a mod's `ModInfo.xml` `DisplayName` when available
- Shows discovered vehicles in:
  - `Vanilla` mode
  - `Modded` mode
- Groups common fields into simple editing sections
- Exposes all discovered values in an `Advanced Raw Properties` section
- Creates backup files before saving
- Restores backups on demand
- Includes a performance multiplier for quick bulk tuning

## Supported Files

The app supports:

- vanilla vehicle config files
- mod vehicle config files that use a direct `vehicles.xml`
- mod patch XML such as:

```xml
<configs>
  <append xpath="/vehicles">
    ...
  </append>
</configs>
```

A mod is considered compatible when this file exists:

```text
Mods\<ModName>\Config\vehicles.xml
```

## Main Features

### Vanilla Tab

- Reads directly from the game's main vehicle config:

```text
Data\Config\vehicles.xml
```

- Shows all vanilla vehicles found in that file

### Modded Tab

- Scans:

```text
<7 Days To Die Root>\Mods
```

- Checks each mod folder for:

```text
Config\vehicles.xml
```

- Shows compatible mods in a tree:
  - mod name
  - vehicle entries under that mod

### Safe Editing Sections

The editor groups common tuning values into:

- `Performance`
- `Handling`
- `Fuel And Utility`
- `Extras`

These are intended to cover the values most people are likely to tweak.

### Advanced Raw Properties

- Shows every discovered vehicle property loaded from the XML
- Useful for unusual modded vehicles or niche tuning values not promoted into the safe sections

### Performance Multiplier

The multiplier can target:

- the selected vehicle
- all vehicles in the current context

It can affect any combination of:

- `Motor Torque / Turbo`
- `Max Velocity / Turbo`
- `Brake Torque`

Rounding behavior:

- `Max Velocity / Turbo` rounds to the nearest `0.5`
- `Motor Torque / Turbo` rounds to whole numbers
- `Brake Torque` rounds to whole numbers

## Backups

Backups are created beside the active XML file as:

```text
vehicles.xml.bak
```

Examples:

- vanilla:

```text
...\7 Days To Die\Data\Config\vehicles.xml.bak
```

- modded:

```text
...\7 Days To Die\Mods\<ModName>\Config\vehicles.xml.bak
```

Restore behavior is context-aware:

- in `Vanilla` mode, restore targets the vanilla file backup
- in `Modded` mode, restore targets the selected mod file backup

## Button Availability

The app disables actions when they are not valid:

- `Save` is disabled when no editable file is active
- `Create Backup` is disabled when no editable file is active
- `Restore Backup` is disabled when the active file has no `.bak`

## How To Use

### Quick Start

1. Launch the app.
2. Let it auto-detect your `7 Days To Die` install folder.
3. Choose either `Vanilla` or `Modded`.
4. Select a vehicle.
5. Edit the values you want.
6. Click `Save`.

### Editing Vanilla Vehicles

1. Open the `Vanilla` tab.
2. Select a vehicle from the list on the left.
3. Change values in the grouped sections or advanced properties.
4. Click `Save`.

### Editing Modded Vehicles

1. Open the `Modded` tab.
2. Click `Scan Mods` or `Rescan` if needed.
3. Expand a compatible mod in the tree.
4. Select one of its vehicles.
5. Change values.
6. Click `Save`.

### Using The Multiplier

1. Enter a multiplier value such as `1.2` or `0.8`.
2. Choose whether it applies to:
   - `Selected Vehicle`
   - `All Vehicles`
3. Tick the performance fields it should affect.
4. Click `Apply`.
5. Review the updated values.
6. Click `Save`.

### Restoring A Backup

1. Select the correct tab and vehicle context.
2. Click `Restore Backup`.
3. Reload or review the file if needed.

## Technical Details

### Framework

- .NET `10.0-windows`
- WPF desktop application
- Windows-focused release target: `win-x64`

### Release Configuration

The project is configured for:

- self-contained publish
- single-file publish
- native library self-extract for WPF/runtime dependencies
- compressed single-file output
- no `.pdb` in release output

That means release builds are intended to ship as a single `.exe`, even though the app may extract internal native files to a temp location at runtime.

### Admin Behavior

The app includes an application manifest that requests elevation.

This helps when editing files under:

```text
Program Files (x86)
```

Users should expect a Windows UAC prompt when launching the app.

## Build

From the project folder:

```powershell
dotnet build .\SevenDaysVehicleEditor.csproj
```

## Publish

### Recommended

Use the helper script:

```powershell
.\publish-release.ps1
```

This publishes the app into:

[release/publish](/C:/Users/Evendyce/Documents/Codex/2026-05-01/hey-mr-codex-i-have-a/SevenDaysVehicleEditor/release/publish)

### Manual

You can also publish directly:

```powershell
dotnet publish .\SevenDaysVehicleEditor.csproj -c Release
```

## If Build Or Publish Says The EXE Is In Use

If `dotnet build` or `dotnet publish` complains that the app or output files are in use:

- make sure the app is fully closed
- stop any active Visual Studio debug session
- close Visual Studio if the XAML designer is holding files open
- close Explorer windows that are previewing the release folder
- check for leftover `MSBuild`, `dotnet`, or `devenv` processes
- retry the command

## Notes

- The app edits whatever vehicle entries exist in the selected XML file.
- That means vanilla and modded vehicle configs behave similarly as long as they use compatible vehicle XML structures.
- Some unusual modded vehicles may expose their best tuning options through `Advanced Raw Properties` rather than the simplified grouped sections.
