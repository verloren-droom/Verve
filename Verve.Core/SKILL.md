---
name: verve-core
description: Use Verve.Core for module ownership, lifecycle orchestration, events, game-loop integration, and shared runtime utilities in Unity.
---

# Scope

Verve.Core owns the module container and common runtime services. It does not load UI prefabs, parse configuration tables, or provide feature-specific business logic.

The package targets Unity 2021.3+. Runtime module assemblies are independently referenced through asmdefs.

## Bootstrap

GameModulesHandle owns the container, which owns modules and extension instances. Unity lifetime binding is optional and only triggers framework cleanup:

~~~csharp
using Verve;

var manifest = new GameModuleManifest();
manifest.Add<MyGameModule>();

GameModulesHandle modules = Game.CreateModules(this);
await modules.Modules.InstallFromManifestAsync(manifest);
var gameModule = modules.Modules.GetModule<MyGameModule>();

// The framework awaits teardown when this GameObject is destroyed.
~~~

First lifetime binding requires an active GameObject. Without Unity lifetime binding, use Game.CreateModules(), retain the handle, and await DisposeAsync at the application boundary. Synchronous lifecycle APIs require callbacks to finish synchronously.

GameModulesHandle owns the installed modules. Do not call module lifecycle methods, construct framework modules for normal use, or dispose an individual module owned by the container.

## Module rules

- Declare only required modules with GameModuleDependency. Compose optional features at the project boundary; do not add dependency attributes for optional integrations.
- Install, query, and uninstall modules through GameModules.
- Use GameModuleContext.GetDependency<T>() only for declared dependencies.
- Register tick objects with GameModuleContext.AddTickSystem and remove them when the module no longer needs them.
- A GameModule is not a tick implementation. Register an object implementing the relevant I...Tick interface instead.
- Keep all module operations on the Unity main thread unless the API explicitly permits otherwise.

## Customization

Pass GameModulesOptions to Game.CreateModules(lifetime, options), Game.CreateModules(options: options), or new GameModules(options). Defaults are ready to use. Supply factories for IGameModuleFactory and IGameLoopTickSystemScheduler to customize construction or tick dispatch; each container owns and releases the resulting instances. Extension selection is fixed at creation; default implementation classes stay internal. Full contracts and examples are in Runtime/Core/Modules/SKILL.md. Do not replace lifecycle/ownership internals to customize business behavior.

IGameModuleFactory.Configure runs after ownership transfer, before per-entry configuration and installation. OnModulesChanged is removed; optional IGameModuleObserver receives GameModuleOperationResult through OnCompleted. The container owns the observer and releases it last. Observer errors are reported after the current operation finishes required work; they do not roll back successful modules.

## Optional modules

Add only the assembly and module needed by the project:

- UI: Runtime/UI/SKILL.md
- Loader: Runtime/Loader/SKILL.md
- Tables: Runtime/Table/SKILL.md
- Events: Runtime/Core/Event/SKILL.md

The ACC module has a separate boundary and is not part of this core contract.

Shared editor functionality is in `Verve.Editor`, which references only `Verve.Core`. UI, Table and Loader editors use `Verve.UI.Editor`, `Verve.Table.Editor` and `Verve.Loader.Editor` respectively. Keep module-specific build checks in their owning editor assembly; Loader content builds do not invoke UI validation.

## Verification

Before handing off a module change, verify that the module is installed exactly once, all declared dependencies are present, the container handle is disposed by its owner, and no public code calls lifecycle methods directly.
