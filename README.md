# 7 Days Config Editor

`7 Days Config Editor` is a Windows desktop tool for editing selected `7 Days To Die` configuration files through safer, more structured screens instead of raw XML editing.

It started as a vehicle editor and now also includes a dedicated progression module, with some sections intentionally kept read-only when the XML is too interdependent to present as a safe editor.

## What It Does

- Detects the `7 Days To Die` install folder from Steam library paths
- Loads the vanilla vehicle config from `Data\Config\vehicles.xml`
- Scans the game's `Mods` folder for compatible vehicle mods
- Uses a mod's `ModInfo.xml` `DisplayName` when available
- Loads the progression config from `Data\Config\progression.xml`
- Separates the app into top-level modules:
  - `Vehicles`
  - `Progression`
- Creates backup files before saving
- Restores backups on demand

## Module Overview

### Vehicles

The `Vehicles` module is the original editor and includes:

- `Vanilla` mode
- `Modded` mode
- grouped safe editing sections
- `Advanced Raw Properties`
- a performance multiplier

### Progression

The `Progression` module has its own dedicated screen and is split into:

- `Level`
- `Attributes`
- `Skills`
- `Crafting Skills`
- `Perks`

Editable vs read-only behavior:

- `Level` is editable
- `Attributes` is editable in a limited, safe way
- `Skills` is read-only
- `Crafting Skills` is read-only
- `Perks` is read-only

This is intentional. The app only promotes settings into editable screens when the effect of changing them is understandable and predictable.

## Supported Files

### Vehicles

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

### Progression

The progression module reads:

```text
Data\Config\progression.xml
```

## Vehicles Module

### Vanilla Tab

- Reads directly from:

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

These are intended to cover the values most people are most likely to tweak.

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

## Progression Module

### Level

The `Level` tab exposes the top-level `<level>` block from `progression.xml`.

Safe editable fields:

- `Max Level`
- `Base XP To Level`
- `Experience Multiplier`
- `Skill Points Per Level`
- `Clamp XP Cost At Level`

### Attributes

The `Attributes` tab exposes:

- shared defaults from the top-level `<attributes>` node
- a list of individual attributes
- safe top-level per-attribute overrides

Shared defaults:

- `Min Level`
- `Max Level`
- `Base Skill Point Cost`
- `Cost Multiplier Per Level`

Per-attribute safe overrides when present:

- `Min Level`
- `Max Level`
- `Base Skill Point Cost`
- `Hidden`

The attribute details panel also shows read-only context such as:

- internal name
- icon
- name key
- description key
- requirement/effect counts

### Skills

The `Skills` tab is a read-only browser.

It shows:

- top-level `skills` defaults
- `skill` entries
- `book_group` entries
- structured metadata for the selected entry

This section is read-only because the current goal is inspection, not encouraging direct skill metadata edits.

### Crafting Skills

The `Crafting Skills` tab is a read-only browser.

It shows:

- top-level `crafting_skills` defaults
- all `crafting_skill` entries
- structured counts for:
  - display entries
  - unlock entries
  - effect groups
  - passive effects

This section is intentionally read-only because unlock displays, recipe unlocks, and passive effect curves are tightly coupled.

### Perks

The `Perks` tab is a read-only browser.

It shows:

- top-level `perks` defaults
- all `perk` entries
- structured counts for:
  - level requirements
  - effect groups
  - passive effects
  - triggered effects
  - effect descriptions

This section is intentionally read-only because perk requirements, balance effects, and description layers are heavily interdependent.

## Backups

Backups are created beside the active XML file with `.bak` appended.

Examples:

- vehicles:

```text
...\7 Days To Die\Data\Config\vehicles.xml.bak
```

- modded vehicles:

```text
...\7 Days To Die\Mods\<ModName>\Config\vehicles.xml.bak
```

- progression:

```text
...\7 Days To Die\Data\Config\progression.xml.bak
```

Restore behavior is context-aware:

- `Vehicles > Vanilla` restores the vanilla vehicle backup
- `Vehicles > Modded` restores the selected mod vehicle backup
- `Progression` restores the progression backup

## Button Availability

The app disables actions when they are not valid:

- `Save` is disabled when no editable file is active
- `Create Backup` is disabled when no editable file is active
- `Restore Backup` is disabled when the active file has no `.bak`

This applies to both the `Vehicles` and `Progression` modules.

## How To Use

### Quick Start

1. Launch the app.
2. Let it auto-detect your `7 Days To Die` install folder.
3. Choose either `Vehicles` or `Progression`.
4. Select the item you want to inspect or edit.
5. Make changes where editing is supported.
6. Click `Save`.

### Editing Vanilla Vehicles

1. Open `Vehicles`.
2. Open the `Vanilla` tab.
3. Select a vehicle from the list on the left.
4. Change values in the grouped sections or advanced properties.
5. Click `Save`.

### Editing Modded Vehicles

1. Open `Vehicles`.
2. Open the `Modded` tab.
3. Click `Scan Mods` or `Rescan` if needed.
4. Expand a compatible mod in the tree.
5. Select one of its vehicles.
6. Change values.
7. Click `Save`.

### Using The Vehicle Multiplier

1. Enter a multiplier value such as `1.2` or `0.8`.
2. Choose whether it applies to:
   - `Selected Vehicle`
   - `All Vehicles`
3. Tick the performance fields it should affect.
4. Click `Apply`.
5. Review the updated values.
6. Click `Save`.

### Editing Progression

1. Open `Progression`.
2. Use `Level` for global progression settings.
3. Use `Attributes` for shared defaults and safe per-attribute overrides.
4. Use `Skills`, `Crafting Skills`, and `Perks` as read-only reference browsers.
5. Click `Save` when you change editable progression values.

### Restoring A Backup

1. Select the correct module and file context.
2. Click `Restore Backup`.
3. Reload or review the file if needed.

## Technical Details

### Framework

- .NET `10.0-windows`
- WPF desktop application
- Windows-focused release target: `win-x64`

### UI Structure

The app is split into a shell plus feature-specific views:

- top-level shell in `MainWindow`
- `VehiclesView`
- `ProgressionView`

Shared theme resources are provided through a merged resource dictionary so both modules use the same styling.

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

```text
release/publish
```

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

- The app only makes direct edits in areas that have been deliberately judged safe enough to explain and predict.
- Some XML sections are intentionally read-only, even when they are fully browsable.
- The vehicle editor works well for both vanilla and compatible mod vehicle configs.
- The progression editor is designed to expand over time without immediately exposing every nested progression rule as an editable surface.
