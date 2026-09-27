---
name: verve-game-modules
description: Install and own Verve GameModule instances through GameModulesHandle, dependencies, and the module tick scheduler.
---

# Scope

GameModules is the runtime container for module installation, dependency ordering, lookup, and teardown. GameModulesHandle is the ownership boundary.

## Minimal module

~~~csharp
using System.Threading;
using System.Threading.Tasks;
using Verve;

[GameModuleDependency(typeof(ConfigModule))]
public sealed class GameplayModule : GameModule
{
    protected override ValueTask OnInstall(
        GameModuleContext context,
        CancellationToken cancellationToken)
    {
        var config = context.GetDependency<ConfigModule>();
        // Register systems and retain only dependencies declared by the attribute.
        return default;
    }
}
~~~

Install through a manifest or the container:

~~~csharp
var manifest = new GameModuleManifest();
manifest.Add<ConfigModule>();
manifest.Add<GameplayModule>();

var handle = Game.CreateModules(manifest);
var gameplay = handle.Modules.GetModule<GameplayModule>();

// The owner releases the complete container.
handle.Dispose();
~~~

## Lifecycle and ownership

- The container creates and installs modules in dependency order.
- Use Install<T>(), InstallAsync<T>(), InstallFromManifest(...), and the matching uninstall methods on GameModules.
- Read a dependency with GameModuleContext.GetDependency<T>() or TryGetDependency<T>() only after declaring it.
- Do not call OnInstall/OnUninstall yourself. Create container modules inside the factory invoked by the framework; do not preallocate them.
- Ownership stays inside GameModulesHandle → GameModules. Game.CreateModules() has no GameObject dependency. In Unity, Game.CreateModules(componentOrGameObject, options) optionally binds a destruction trigger; the internal adapter requests handle disposal and never owns modules or extension instances. First binding requires an active GameObject.
- Without a Unity lifetime binding, release the GameModulesHandle at the application boundary. Await DisposeAsync when callbacks can suspend.
- A module belongs to exactly one container. Duplicate exact types fail; no live replacement or reinstall recovery occurs. Do not dispose a container-owned instance directly.
- Uninstall always releases the instance. There is no dispose=false option. Once teardown starts, errors do not stop final cleanup or restore ticks; errors reach the caller afterward.
- Manifest factories execute one at a time in dependency order. Do not preallocate resources for factories that might never run.
- OnDispose must release resources even after partial installation or failed OnUninstall. Dependencies remain borrowed; never dispose another module obtained from the context.
- Keep constructors limited to arguments and ordinary managed state. Acquire resources in OnInstall after ownership transfer and release them in OnDispose. Direct construction remains possible for custom factories; an instance not yet handed to the container belongs to its creator. Constructor guards cannot make the container clean up allocations that a throwing constructor or factory never returns.

## Container customization

Use GameModulesOptions at construction. Each configured delegate creates one exclusive instance per container; reusing an options object is supported, reusing the returned instances is rejected. The observer is optional. Defaults require no configuration:

~~~csharp
var options = new GameModulesOptions
{
    CreateScheduler = () => new ProjectTickScheduler(),
    CreateModuleFactory = () => new ProjectModuleFactory()
};
var handle = Game.CreateModules(this, options);
var manifest = new GameModuleManifest();
manifest.Add<GameplayModule>();
await handle.Modules.InstallFromManifestAsync(manifest);
~~~

- IGameLoopTickSystemScheduler selects tick dispatch. The default implementation stays internal and supports ITickOrder and the existing tick phases. To compose defaults, capture new GameModulesOptions().CreateScheduler and wrap each new result through the interface. AddSystem must either register successfully or fail without retaining the system; RemoveSystem must remove a registered system before returning true. The scheduler owns its internal resources, while the container owns module/tick registration.
- IGameModuleFactory selects construction and common configuration. The internal GameModuleFactory uses a public parameterless constructor. A custom factory may perform constructor injection; Install<T>(), Install(Type), code manifests, resource manifests and assembly installation all use it. Generic code manifests and Install<T>() no longer require new().
- Implement Configure(GameModule) in every custom factory. The framework calls it after ownership transfer and dependency reservation, then invokes the descriptor's optional configure callback, then OnInstall. manifest.Add<T>(() => new T(...)) overrides creation only; the container factory still configures that instance. Configuration failure releases the new instance.
- Factories must return a new, unowned instance of the exact requested type, never a cached or externally owned module. The framework releases returned unowned instances even when type validation, configuration or installation fails. Already-owned instances are rejected without configuration or disposal. A factory must clean up allocations it fails to return.
- The container releases modules first, then its scheduler, then its factory, then its observer. Custom factories dispose their own services only; they must not re-dispose modules already handed to the container. Do not share an externally owned DI scope or scheduler; create a dedicated scope/adapter for each container.
- For a container without a Unity lifetime binding use new GameModules(options) or Game.CreateModules(options: options), and dispose its owner explicitly.
- Unity manifest fields are applied after the selected factory creates the instance. Asset manifests keep their public-parameterless-constructor requirement, since editor preview and link.xml generation use the default factory. For constructor-only types use a code manifest and preserve any reflection-created types required by the target's stripping settings.

Extensions are selected at container construction and are never hot-swapped. Options changes affect only future containers. Modules or custom implementations may expose explicit data/behavior updates; switching extension implementations requires a new container.

Lifecycle state, dependency validation, duplicate policy, rollback, ownership and teardown remain framework responsibilities. Customize behavior through module callbacks, factories and tick schedulers; do not replace the lifecycle executor or registry.

## Operation observation

OnModulesChanged has been removed. Supply GameModulesOptions.CreateObserver to create an exclusive IGameModuleObserver, implementing OnCompleted(GameModuleOperationResult result) and Dispose(). There is no hot replacement or empty default implementation.

- Results contain ModuleType, Operation (Install/Uninstall), Duration and Failure; they do not expose module instances. An untyped factory failure before returning an instance has a null ModuleType.
- Each started installation attempt or teardown reports its completed outcome after cleanup. Precondition rejection or a missing uninstall target does not report an operation. A manifest can emit successful installation results followed by rollback uninstall results; await the manifest operation to determine its overall outcome.
- Observer callbacks cannot reenter container changes. Callback errors are collected until the current container operation completes, then thrown together with lifecycle errors. They never trigger rollback of successful modules or interrupt mandatory cleanup. Failure in the result describes the module operation, not the observer callback.
- The observer is disposed after other owned extensions, including when earlier cleanup fails. No observer means no timing or callback allocation. A configured factory returning null or a previously transferred instance is an error.
- Game.CreateModules(manifest, options) cleans up the unreturned container if creation fails, including observer errors during initial installation. To handle observation errors while retaining the container, create the handle before calling installation.

## Tick registration

GameModuleContext.AddTickSystem accepts an object implementing the appropriate I...Tick contract. Keep GameModule itself out of the tick registry. Public code depends on tick interfaces; GameLoop implementation details remain internal.

## Custom module inspectors

Place editor code in an Editor assembly and register it with `CustomGameModuleEditorAttribute`:

~~~csharp
using UnityEditor;
using Verve.Editor;

[CustomGameModuleEditor(typeof(GameplayModule))]
sealed class GameplayModuleEditor : GameModuleEditor
{
    public override void OnInspectorGUI(SerializedProperty module)
    {
        using var speed = module.FindPropertyRelative("m_Speed");
        EditorGUILayout.PropertyField(speed);
        // Or call base.OnInspectorGUI(module) for the default fields.
    }
}
~~~

- `GameModuleEditor` customizes only the field UI. The manifest host owns serialization, Undo/Redo, Apply/Revert and object-reference persistence. Do not call Update or ApplyModifiedProperties inside the custom editor.
- The `SerializedProperty` argument is borrowed for this draw only. Do not retain or dispose it, its SerializedObject, the module, or referenced assets. Dispose property copies you create.
- The host lazily creates one editor per expanded module entry and reuses it for the same type. Undo, reset, removal and host destruction release editors. Override `OnDispose` only to release your own temporary resources; constructors initialize ordinary managed state.
- Exact registrations take priority. Use `editorForChildClasses: true` to opt into inherited selection; the nearest registered base type wins. Duplicate registrations and invalid implementations are errors, not requests to draw a default UI.
- Editor folders follow runtime responsibilities: Modules and module-specific helpers under `Editor/Core/Modules`, replaceable-tool configuration under `Editor/Core/Tools`, and general editor functions under `Editor/Core/Utilities`. CSV parsing is runtime-neutral and belongs to `Game.CsvUtility` in `Runtime/Core/Utilities`.

## Verification

Check dependency attributes, installation order, duplicate installation policy, cancellation during uninstall, and handle disposal. A module change is incomplete if a system or resource survives container teardown.
