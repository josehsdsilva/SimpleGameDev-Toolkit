# Changelog

All notable changes to this package are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.1] - 2026-06-08

### Changed
- Documented the full workflow for **adding a tool** (incubate inside a game → promote to
  the kit) and the global `toolkit update` helper command in the README.

## [2.1.0] - 2026-06-08

### Added
- `ScrollToVisible.ScrollToBottom()` and a `Scroll(int siblingOffset)` overload.

### Changed
- Aligned a `.meta` GUID for Elifoot consumers.
- Install docs now recommend embedding via **git subtree** (the files are committed
  into the consuming game's repo) instead of a git submodule, so consumers need no
  access to this private repo and runtime dependencies are always present.

## [2.0.0]

### Changed
- **BREAKING:** all runtime types are now under the `SimpleGameDev` namespace and
  all editor types under `SimpleGameDev.Editor`. Consumers must add
  `using SimpleGameDev;` (and `using SimpleGameDev.Editor;` where relevant).
- `SimpleGameDev` / `SimpleGameDev.Editor` assemblies now declare `rootNamespace`.
- `SimpleGameDev.Editor` references `Unity.Newtonsoft.Json` explicitly (was relying on auto-reference).

### Added
- Package metadata: `keywords`, author URL, documentation/changelog/license URLs, license field.
- `CHANGELOG.md` and `LICENSE.md`.

## [1.0.0]

### Added
- Initial extraction of the SimpleGameDev toolkit into a standalone UPM package.
- Editor tooling: SimpleGameDev Hub, Favorites tab + context menu, Color Palette,
  Prefab Overrides scanner/window/toolbar, Play Mode scene manager, PlayerPrefs editor,
  Raycast target tool, rename utilities, Scene View overlay hider, start-scene override,
  auto-refresh watcher, Git branch display.
- Runtime helpers: `DebugLogger`, `PersistentObject`, `ScrollToVisible`, `UniformFontSize`,
  `[HideInSceneView]` attribute and `OverlayGroups` enum.
- `FavoritesTab` uses Newtonsoft Json (avoids `JsonUtility`'s recursive-depth warning);
  declared `com.unity.nuget.newtonsoft-json` dependency.
