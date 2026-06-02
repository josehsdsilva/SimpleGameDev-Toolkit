# SimpleGameDev

Reusable Unity editor tools and runtime helpers, packaged as a private UPM package.

## Contents

- **Editor/** — editor-only tooling: SimpleGameDev Hub, Favorites tab + context menu, Color Palette, Prefab Overrides scanner/window/toolbar, Play Mode scene manager, PlayerPrefs editor, Raycast target tool, rename utilities, Scene View overlay hider, start-scene override, auto-refresh watcher, Git branch display.
- **Systems/** — runtime helpers used by games: `DebugLogger`, `PersistentObject`, `ScrollToVisible`, `UniformFontSize`, and the `[HideInSceneView]` attribute / `OverlayGroup` enum.

Assemblies: `SimpleGameDev` (runtime) and `SimpleGameDev.Editor` (editor).

## Install (per project)

Add as a git submodule under `Packages/` so it's an editable embedded package:

```sh
git submodule add https://github.com/josehsdsilva/SimpleGameDev.git Packages/com.josesilva.simplegamedev
git config -f .gitmodules submodule.Packages/com.josesilva.simplegamedev.branch main
git submodule update --init
```

The Newtonsoft Json dependency (`com.unity.nuget.newtonsoft-json`) resolves automatically via `package.json`.

## Update from any project

The submodule folder is a full clone of this repo, so edit in place and push back:

```sh
cd Packages/com.josesilva.simplegamedev
git checkout main
# ...edit...
git add -A && git commit -m "..." && git push
cd ../..
git add Packages/com.josesilva.simplegamedev && git commit -m "Bump SimpleGameDev"
```

Pull others' / latest changes elsewhere:

```sh
git submodule update --remote Packages/com.josesilva.simplegamedev
```
