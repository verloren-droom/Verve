---
name: verve-loader
description: Load Unity assets and scenes through Verve's single ILoader contract while selecting Addressables or AssetBundles only at module configuration time.
---

# Scope

LoaderModule is the one public loading module. Business code calls ILoader with normalized project paths; it does not call Addressables or AssetBundle APIs directly.

## Install

Choose the implementation before installation:

~~~csharp
var manifest = new GameModuleManifest();
manifest.Add<LoaderModule>(() => new LoaderModule
{
    LoaderMode = AssetLoaderMode.Addressables
});

var modules = Game.CreateModules(this);
await modules.Modules.InstallFromManifestAsync(manifest);
var loader = modules.Modules.GetModule<ILoader>();
~~~

LoaderMode cannot be changed after installation. Use AssetLoaderMode.AssetBundles when the project builds and distributes bundles instead.

## Asset lifetime

All asset paths must begin with Assets/ or Packages/. Prefer binding loads to the Component/GameObject that uses them. The framework releases them when that GameObject is destroyed, including loads that finish after owner destruction or cancellation:

~~~csharp
var icon = await loader.LoadAssetAsync<Sprite>("Assets/UI/Icons/Close.png", this);
~~~

First binding requires an active GameObject, since Unity does not reliably call OnDestroy for never-activated objects. No handle list or custom OnDestroy is needed.

Use AssetLoadScope for a lifetime that has no Unity owner. The scope extensions transfer release ownership automatically:

~~~csharp
using var scope = new AssetLoadScope();
var icon = loader.LoadAsset<Sprite>("Assets/UI/Icons/Close.png", scope);
~~~

For low-level control, retain and dispose AssetLoadHandle<T> directly. scope.Track(handle) consumes that handle and returns its resource; the old handle becomes invalid and can no longer release or transfer the resource. Resources must not outlive their owner. Instantiated GameObjects can still share materials, textures and other prefab dependencies, so keep the loading owner alive for those instances.

## Scene lifetime

~~~csharp
var scene = await loader.LoadSceneAsync(
    "Assets/Scenes/Battle.unity",
    allowSceneActivation: false);

scene.Activate();
await loader.UnloadSceneAsync("Assets/Scenes/Battle.unity");
~~~

Unload scenes through ILoader; do not mix the module's tracked operation with a separate loading implementation.

## Build boundary

- Addressables uses its official content/player build pipeline and profile paths.
- AssetBundles are built by the Verve editor/CI pipeline for the selected target and placed in the configured StreamingAssets output.
- `Verve.Loader.Editor` owns content building and Addressables editor integration. It does not reference UI or Table editors; the project's content-build entry runs any required business-module validation first.
- Build hot-update content before publishing the player, store player and content under one immutable content version, and verify the content on a clean device.
- Older Unity versions use the module's menu fallback; newer versions can use the supported build callback/toolbar entry. Do not add a second business-facing loader API for version differences.

## Rules

- Call loader APIs on the Unity main thread.
- Pass project paths, not implementation-specific keys.
- Prefer owner-bound loading; framework code handles successful, cancelled and failed transfers.
- Disposing the module container cancels pending loads and releases module-owned references. Explicit low-level handles remain the responsibility of their caller until transferred to a scope.
