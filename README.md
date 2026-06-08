# SimpleGameDev

Reusable Unity editor tools and runtime helpers, packaged as a private UPM package.

## Contents

- **Editor/** — editor-only tooling: SimpleGameDev Hub, Favorites tab + context menu, Color Palette, Prefab Overrides scanner/window/toolbar, Play Mode scene manager, PlayerPrefs editor, Raycast target tool, rename utilities, Scene View overlay hider, start-scene override, auto-refresh watcher, Git branch display.
- **Systems/** — runtime helpers used by games: `DebugLogger`, `PersistentObject`, `ScrollToVisible`, `UniformFontSize`, and the `[HideInSceneView]` attribute / `OverlayGroup` enum.

Assemblies: `SimpleGameDev` (runtime) and `SimpleGameDev.Editor` (editor).

## Install (per project)

Embed it with **git subtree** so the real files are committed into the game repo. Anyone
who clones the game then gets the toolkit directly — no access to this private repo is
needed, and runtime tools the game depends on are always present (Asset-Store-style).

> Why not a submodule? A submodule would force everyone who clones the game to have read
> access to this private repo to fetch it. Subtree copies the files in, so it stays private
> (only you) while the games that embed it need no access at all.

Run once, from the game's git root (the package must live under `Packages/` with a folder
name matching its `package.json` name, so Unity picks it up as an embedded package):

```sh
git remote add sgdt https://github.com/josehsdsilva/SimpleGameDev-Toolkit.git
git subtree add --prefix Packages/com.josesilva.simplegamedev sgdt v2.1.0 --squash
```

If the Unity project sits in a subfolder of the repo, prefix the path accordingly
(e.g. `--prefix MyGame/Packages/com.josesilva.simplegamedev`).

The Newtonsoft Json dependency (`com.unity.nuget.newtonsoft-json`, Unity registry) resolves
automatically via `package.json` — no private access required.

## Update a game to a newer version

Bump to a newer tag with one command (run by whoever owns this repo; other devs just
`git pull` the game afterwards and get the new files):

```sh
git subtree pull --prefix Packages/com.josesilva.simplegamedev sgdt v2.2.0 --squash
```

## Maintain the toolkit (centrally)

Keep a standalone clone of this repo, independent of any game:

```sh
git clone https://github.com/josehsdsilva/SimpleGameDev-Toolkit.git
```

Add/edit tools there, update `CHANGELOG.md` and the `version` in `package.json` to match,
then tag and push a new release:

```sh
git tag v2.2.0 && git push --tags
```

> Note: embedded packages don't get an update button in the Unity Package Manager (that
> needs a reachable, authenticated source). The `git subtree pull` above is the update
> mechanism.
