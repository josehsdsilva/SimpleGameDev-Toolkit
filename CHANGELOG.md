# Changelog

All notable changes to this package are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.0] - 2026-07-20

### Changed
- **`DebugLogger.Log` and `LogWarning` are now compile-time stripped from player builds** via
  `[Conditional("UNITY_EDITOR")] / [Conditional("SGD_LOGS_IN_BUILD")]`. The call sites — including
  the caller's string interpolation — vanish from the binary, so shipped builds pay zero cost for
  hundreds of log calls (previously every call allocated its message string and a filename string
  before the enabled check). To keep these logs in a build (dev/QA builds), define
  `SGD_LOGS_IN_BUILD` in that build's scripting defines. **Breaking nuance:** the runtime
  `enableAllLogsInBuild` toggle now only matters in builds that define `SGD_LOGS_IN_BUILD` — for
  `Log`/`LogWarning` there is nothing to toggle otherwise. `LogError` and `LogException` are always
  compiled (they are the error-reporting path crash tooling reads).
- Inside every log method the cheap `minimumLogLevel` check now runs before the caller-filename
  string is built, so disabled levels no longer allocate in the editor either.

## [2.2.1] - 2026-07-09

### Added
- **"Prefab Overrides Indicator" toggle in the Hub.** `PrefabOverridesToolbar.SetEnabled` existed and
  the indicator honoured its `EditorPrefs` flag, but nothing ever called it — the only way to turn the
  indicator off was to write the (hashed) `EditorPrefs` key by hand. A scene heavy enough to stall the
  scan was therefore a scene you could not recover from inside the editor. The Hub now exposes the
  toggle next to Auto Refresh Watcher and Git Branch Display.

## [2.2.0] - 2026-07-09

### Changed
- **Relicensed from `Proprietary — All rights reserved` to the `MIT` license.** The repository
  is public, so "all rights reserved" contradicted the intent: it let people read the code but
  granted nobody — not even collaborators on the consuming games — the right to use it. MIT
  aligns the licence with how the package is actually consumed, and unblocks OpenUPM, which
  requires an open-source licence.
- Consumers now track the `main` branch by git URL rather than pinning a tag, so the Package
  Manager's **Update** button re-resolves to the newest commit. `packages-lock.json` still pins
  the resolved commit hash, so other machines and CI stay deterministic. Tags remain as release
  markers for this changelog.

## [2.1.2] - 2026-07-09

### Fixed
- **Prefab Overrides indicator no longer freezes the editor on every inspector edit.**
  Changing a single Transform value on a prefab instance fires `prefabInstanceUpdated`, which
  marked the toolbar indicator dirty and ran a full-scene scan synchronously on the main thread.
  Measured on a scene with 1665 transforms: **21-22 s per scan** — and the 5 s throttle limited how
  *often* that ran, not how much it cost.
  - The scan now rejects clean prefab instances via `IsAnyPrefabInstanceRoot` +
    `HasPrefabInstanceAnyOverrides` before touching `GetObjectOverrides` or building a
    `SerializedObject`, and the toolbar's count path no longer resolves the innermost prefab
    asset, the outermost path, or the property modifications — none of which affect the number.
    Same scene, same result (156 overrides): **~0.5 s, a 30-40x speedup.**
  - `MarkDirty()` now debounces (1.5 s of quiet) instead of throttling, so dragging an inspector
    field no longer queues a scan per frame.
  - Scans are skipped while compiling, importing, or in play mode.
  - A scan that exceeds a 250 ms budget disables auto-refresh for that scene and logs why; the
    indicator marks its count stale and the Prefab Overrides window's **Refresh** re-enables it.

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
