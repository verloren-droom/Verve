---
name: verve-ui
description: Build Verve UI with managed View instances, owned ViewParts and Widgets, generated prefab bindings, and explicit open/close/release lifecycles.
---

# Scope

Verve.UI manages UI prefab instances and their view lifecycle.

| Type | Responsibility |
| --- | --- |
| UIModule / IUIManager | Open, close, release, query, cache, and layer View instances |
| ViewBase / ViewBase<TArgs> | Parameterless / typed page state and lifecycle |
| ViewPartBase<TComponent> | A typed sub-tree owned by one View |
| WidgetBase<TData> | A reusable data-bound UI unit |
| ViewStack | Back navigation order, exposed by UIModule |
| UIModalLayer | Full-screen input blocking overlay |

Prefab roots use UIViewComponent for a View and UIWidgetComponent for a Widget. The editor generates .Variables.cs and optional .Events.cs files from those components.

## Minimal View

~~~csharp
using Verve;

[ViewConfig("Assets/UI/LoginView.prefab", UILayer.Main)]
public partial class LoginView : ViewBase
{
    partial void OnConfirmClick()
    {
        // Implement the event selected in the prefab editor.
    }
}

var ui = modules.Modules.GetModule<IUIManager>();
var login = ui.Open<LoginView>();
ui.Close(login);    // Hide and keep the instance when KeepAlive is selected.
ui.Release(login);  // Destroy the instance and release its resources.
~~~

When a type has no ViewConfigAttribute, use the explicit overload:

~~~csharp
var login = ui.Open<LoginView>(
    "Assets/UI/LoginView.prefab",
    UILayer.Main,
    UIViewCacheMode.DestroyOnClose);
~~~

Use `openMode: UIViewOpenMode.New` only when multiple independent instances are required. The default `Open<TView>` reuses the existing type instance.

## Typed opening parameters

Use `ViewBase` and `OnOpened()` when a page takes no arguments. For pages with arguments, derive the parameter from `ViewArgs` and the page from `ViewBase<TArgs>`:

~~~csharp
public sealed class ItemArgs : ViewArgs
{
    public int ItemId { get; }
    public ItemArgs(int itemId) => ItemId = itemId;
}

[ViewConfig("Assets/UI/ItemView.prefab")]
public sealed class ItemView : ViewBase<ItemArgs>
{
    protected override void OnOpened(ItemArgs args) { /* Render args.ItemId. */ }
}

var args = new ItemArgs(42);
var item = ui.Open<ItemView>(args);
ui.Close(item);
item = await ui.OpenAsync<ItemView>(args); // Reuses the arguments and closed instance.
~~~

Calls specify only the View type. `IUIManager` shares four opening methods across parameterless and parameterized pages; there is no separate generic-data API. Argument relationships are checked before loading, with type metadata cached. Wrong types, missing required arguments and unexpected arguments on parameterless pages throw.

Keep arguments immutable and reuse the same instance when appropriate. Creating an argument object allocates once; passing it adds no boxing or parameter wrapper. Models and collections can be borrowed through the argument object's properties.

`OpenArgs` is cleared on close, release and failed opening. Pending operations also clear their argument references on completion. Borrowed models are never disposed by the View. Shared pending and already-open requests require the same argument instance; different arguments require closing first or selecting `New`.

Controllers override `OnOpened()` and read arguments from `View.OpenArgs`. After-open asynchronous callbacks take `(view, ct)` and can read the same property.

## Lifecycle contract

For a ViewBase, the order is:

~~~text
OnCreated -> OnBindEvents -> OnOpened -> OnClosed -> OnUnbindEvents -> OnReleased
~~~

Creation, event binding, event unbinding, and release run once per instance. Open and close run for each transition. ViewPartBase owns its created Widgets and dynamic objects and releases them before OnReleased; WidgetBase<TData> exposes SetData and ClearData for list/grid refreshes.

Do not new View, ViewPart, or Widget classes and do not release a child Widget independently of its owner.

## Common workflows

### Dynamic list

~~~csharp
public sealed class ItemListPart : ViewPartBase<UnityEngine.UI.ScrollRect>
{
    protected override void OnCreated()
    {
        // CreateWidget<TWidget>(template, Component.content) for visible cells.
        // The part owns and releases the cells and instantiated nodes.
    }
}
~~~

Keep list virtualization and data-to-cell mapping in the part/list owner. Do not make UIModule track every cell.

### Multiple instances and cache

~~~csharp
var first = ui.Open<NoticeView>(firstArgs, openMode: UIViewOpenMode.New);
var second = ui.Open<NoticeView>(secondArgs, openMode: UIViewOpenMode.New);

var notices = new List<NoticeView>(2);
ui.GetViews(notices); // Reuses the caller's list.
~~~

KeepAliveLimit evicts the least recently used closed pages after UI operations. It is an operation-time limit, not a per-frame scan.

### Back stack and modal input

~~~csharp
var module = modules.Modules.GetModule<UIModule>();
module.ViewStack.Back();

module.ModalLayer.Show();
try
{
    await ui.OpenAsync<ConfirmView>();
}
finally
{
    module.ModalLayer.Hide();
}
~~~

UIModalLayer blocks input below it; it is not a tutorial cutout system. Use HideAll when a whole interaction scope is cancelled.

## Root and editor checks

UIModule creates a default overlay Canvas. If the project supplies its own root, call SetViewRoot(existingRoot) before the first View is created; the root is then fixed for that module lifetime.

Run the UI build validator before a player build. It checks View/Widget prefab components, selected types, generated bindings/events, and referenced scripts. Newer Unity versions expose the command from the Verve toolbar; older versions use the editor menu fallback.

The Player build callback runs UI validation automatically. For content-only builds, call `Verve.Editor.UIBuildValidator.Validate()` from the project's build entry before invoking the Loader content build. Reference `Verve.UI.Editor` from that editor assembly; the Loader editor does not depend on UI.

## Component lifetime

UIModule manages pages and their owned GameObjects. Feature-specific components use their normal Unity lifecycle; the UI module neither stores their service modules nor performs their binding or cleanup.

Releasing a page deactivates it before Unity's deferred destruction, including creation failures, so active components stop immediately. KeepAlive hiding uses the existing Canvas optimization: disabling only a Canvas keeps child components active; deactivating the page triggers OnDisable/OnEnable. Component subscriptions should follow the component's actual lifetime.

## Rules

- Open, close, release, and query on the Unity main thread.
- Use Close to hide a reusable instance and Release to destroy it.
- Treat generated binding files as output; change bindings in the prefab editor.
- Keep View ownership at UIModule, and child ownership at View/ViewPart.
- Do not bypass the loader or instantiate a View prefab manually when the View is managed by IUIManager.
