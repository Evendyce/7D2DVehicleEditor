# Changelog

All notable changes to `7D2DVehicleEditor` will be documented in this file.

The format is intentionally simple and project-friendly rather than strict-semver ceremony.

## [Unreleased]

### Added
- Top-level editor groundwork for the `overhaul/editor-rework` branch.
- Reusable editor field/view-model infrastructure for:
  - scalar fields
  - boolean fields
  - read-only info rows
  - numeric fields
  - multi-value numeric fields
- Section popup editor prototype flow for vehicle sections.
- `Performance` popup editor with split tuple controls for:
  - `Motor Torque / Turbo`
  - `Max Velocity / Turbo`
- `Handling` popup editor with per-field numeric step rules.
- `Fuel And Utility` popup editor with adaptive per-part precision for values like `Food Drain`.
- `Extras` popup editor with mixed safe-edit/read-only treatment.

### Changed
- Vehicle editing is moving toward focused section editors instead of relying only on inline table editing.
- Numeric-heavy editing now prefers stepper-style controls over plain text entry in popup editors.
- Sensitive fields can now use finer-grained steps, such as `Tilt Dampening` using `0.01`.
- Multi-value fields can now preserve different decimal precision per part instead of forcing one shared step size.
- `Horn Sound` is treated as inspection-only in the safe `Extras` popup because it is a sound-event reference rather than a straightforward tuning value.

### Notes
- `Advanced Raw Properties` remains intentionally untouched by the curated popup editor system.
- This branch is currently focused on UI, editor flow, and reusable control infrastructure rather than broad new gameplay/config coverage.

## [v1.1.0]

### Added
- New top-level `Progression` module alongside `Vehicles`.
- View-based shell split with dedicated `Vehicles` and `Progression` screens.
- `progression.xml` loading, saving, backup, and restore support.
- Editable `Progression > Level` section for:
  - `max_level`
  - `exp_to_level`
  - `experience_multiplier`
  - `skill_points_per_level`
  - `clamp_exp_cost_at_level`
- Editable `Progression > Attributes` support for:
  - shared top-level defaults
  - safe per-attribute overrides
- Read-only browser support for:
  - `Skills`
  - `Crafting Skills`
  - `Perks`

### Changed
- The app evolved from a vehicle-only tool into a broader `7 Days To Die` config editor.
- Shared theme resources were moved into application-level merged dictionaries so multiple views could safely use the same styling.

## [v1.0.0]

### Added
- Initial Windows desktop release focused on `vehicles.xml`.
- Vanilla vehicle editor for the main game config.
- Modded vehicle support by scanning the game `Mods` folder for compatible `Config\\vehicles.xml` files.
- Safe grouped vehicle sections:
  - `Performance`
  - `Handling`
  - `Fuel And Utility`
  - `Extras`
- `Advanced Raw Properties` browser for direct inspection/editing of all discovered vehicle properties.
- Performance multiplier tooling with scope for:
  - selected vehicle
  - all vehicles in the active context
- Per-setting multiplier targeting for:
  - `Motor Torque / Turbo`
  - `Max Velocity / Turbo`
  - `Brake Torque`
- Automatic backup and restore support for:
  - vanilla vehicle config
  - per-mod vehicle configs
- Default game directory detection based on the Steam install pattern:
  - `steamapps\\common\\7 Days To Die`
- 7 Days To Die-inspired UI styling and Windows executable icon.
- Release/publish helper script and GitHub-ready project cleanup.

### Changed
- The original vehicle editor was refined into separate `Vanilla` and `Modded` workflows within the vehicle module.
- Mod display names began using `ModInfo.xml` `DisplayName` values when available.

