namespace Verve.Tests.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Verve.Editor;

    internal class ACCComponentInspectorTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Reader_ListsVisibleInstanceFieldsAndCachesMetadata()
        {
            var fields = ACCComponentReader.GetFields(typeof(VisibilityComponent));
            Assert.That(fields.Select(field => field.Name), Is.EquivalentTo(new[] { "visible", "serialized" }));
            Assert.That(ACCComponentReader.GetFields(typeof(VisibilityComponent)), Is.SameAs(fields));
        }

        [Test]
        public void Reader_ObservesOnlyRequestedComponentsOnSelectedActor()
        {
            using var world = new World("Component reader isolation");
            var actor = world.CreateActor();
            var other = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75 });
            world.SetComponent(actor, new ManaComponent { value = 40 });
            world.SetComponent(other, new HealthComponent { value = 999 });
            var values = new Dictionary<Type, object>();
            var observed = new HashSet<Type> { typeof(HealthComponent) };
            Assert.That(ACCComponentReader.Read(world, actor, values, observed), Is.True);
            Assert.That(((HealthComponent)values[typeof(HealthComponent)]).value, Is.EqualTo(75));
            Assert.That(values[typeof(ManaComponent)], Is.Null);
            observed.Clear();
            observed.Add(typeof(ManaComponent));
            Assert.That(ACCComponentReader.Read(world, actor, values, observed), Is.True);
            Assert.That(values[typeof(HealthComponent)], Is.Null);
            Assert.That(((ManaComponent)values[typeof(ManaComponent)]).value, Is.EqualTo(40));
        }

        [Test]
        public void Reader_InvalidSourceClearsPreviousValues()
        {
            using var world = new World("Component reader lifetime");
            var actor = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75 });
            var values = new Dictionary<Type, object>();
            ACCComponentReader.Read(world, actor, values);
            Assert.That(ACCComponentReader.Read(null, actor, values), Is.False);
            Assert.That(values, Is.Empty);
            ACCComponentReader.Read(world, actor, values);
            Assert.That(ACCComponentReader.Read(world, Actor.none, values), Is.False);
            Assert.That(values, Is.Empty);
            ACCComponentReader.Read(world, actor, values);
            world.Dispose();
            Assert.That(ACCComponentReader.Read(world, actor, values), Is.False);
            Assert.That(values, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(4096)]
        public void Reader_WarmedMetadataSamplingDoesNotAllocate(int unrelatedActors)
        {
            using var world = new World("Component sampling allocation");
            var actor = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75 });
            for (var i = 0; i < unrelatedActors; i++)
                world.SetComponent(world.CreateActor(), new ManaComponent { value = i });
            var values = new Dictionary<Type, object>();
            var observed = new HashSet<Type>();
            for (var i = 0; i < 64; i++) ACCComponentReader.Read(world, actor, values, observed);
            // 只测量预热后的元数据读取，不把 NUnit 断言或值装箱计入此范围。
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1024; i++) ACCComponentReader.Read(world, actor, values, observed);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(values.Keys, Is.EquivalentTo(new[] { typeof(HealthComponent) }));
            Assert.That(values[typeof(HealthComponent)], Is.Null);
        }

        [Test]
        public void View_HiddenAndCollapsedComponentsPauseSamplingAndCatchUpWhenOpened()
        {
            using var world = new World("Component view visibility");
            var actor = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75 });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            var row = Row<HealthComponent>(view);
            row.value = true;
            Sample(view, world, actor);
            var field = view.Q<IntegerField>("acc-field-value");
            view.SetVisible(false);
            world.SetComponent(actor, new HealthComponent { value = 50 });
            Sample(view, world, actor);
            Assert.That(field.value, Is.EqualTo(75));
            Assert.That(((HealthComponent)Values(view)[typeof(HealthComponent)]).value, Is.EqualTo(75));
            view.SetVisible(true);
            Sample(view, world, actor);
            Assert.That(field.value, Is.EqualTo(50));
            row.value = false;
            world.SetComponent(actor, new HealthComponent { value = 25 });
            Sample(view, world, actor);
            Assert.That(Values(view)[typeof(HealthComponent)], Is.Null);
            Assert.That(field.value, Is.EqualTo(50));
            row.value = true;
            Sample(view, world, actor);
            Assert.That(view.Q<IntegerField>("acc-field-value"), Is.SameAs(field));
            Assert.That(field.value, Is.EqualTo(25));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void View_SelectionChangeClearsPreviousRowsAndChangeBaseline(bool switchWorld)
        {
            using var firstWorld = new World("First component selection");
            using var secondWorld = new World("Second component selection");
            var first = firstWorld.CreateActor();
            var targetWorld = switchWorld ? secondWorld : firstWorld;
            var second = targetWorld.CreateActor();
            if (switchWorld) Assert.That(second, Is.EqualTo(first), "World identity must distinguish equal Actor handles.");
            firstWorld.SetComponent(first, new HealthComponent { value = 75 });
            targetWorld.SetComponent(second, new ManaComponent { value = 40 });
            var view = new ACCComponentView();
            view.Refresh(firstWorld, first);
            Row<HealthComponent>(view).value = true;
            Sample(view, firstWorld, first);
            firstWorld.SetComponent(first, new HealthComponent { value = 50 });
            Sample(view, firstWorld, first);
            Assert.That(Row<HealthComponent>(view).text, Does.Contain("有变化"));
            Set(view, "m_NextSample", double.MaxValue);
            view.Refresh(targetWorld, second);
            Assert.That(view.Query<Foldout>().ToList().Count, Is.EqualTo(1));
            var row = Row<ManaComponent>(view);
            Assert.That(row.text, Does.EndWith("存在"));
            Assert.That(row.value, Is.False);
            Assert.That(view.Q<IntegerField>(), Is.Null);
            row.value = true;
            Sample(view, targetWorld, second);
            Assert.That(view.Q<IntegerField>().value, Is.EqualTo(40));
            Assert.That(row.text, Does.EndWith("存在"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void View_InvalidSourceClearsFieldsAndPreservesEmptyMessage(bool disposeWorld)
        {
            using var world = new World("Component view lifetime");
            var actor = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75 });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            Row<HealthComponent>(view).value = true;
            Sample(view, world, actor);
            if (disposeWorld) world.Dispose();
            else world.DestroyActor(actor);
            Sample(view, world, actor);
            Assert.That(view.Q<Foldout>(), Is.Null);
            Assert.That(Values(view), Is.Empty);
            var empty = Get<Label>(view, "m_Empty");
            var message = disposeWorld ? "无运行中的 Actor" : "Actor 已销毁";
            Assert.That(empty.text, Is.EqualTo(message));
            Menu(view, "仅显示变化项").Execute();
            Assert.That(empty.text, Is.EqualTo(message));
            Assert.That(empty.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void View_AddedRemovedAndReaddedComponentsResetStaleFields()
        {
            using var world = new World("Component presence changes");
            var actor = world.CreateActor();
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            world.SetComponent(actor, new HealthComponent { value = 75 });
            Sample(view, world, actor);
            var row = Row<HealthComponent>(view);
            Assert.That(row.text, Does.EndWith("已添加"));
            row.value = true;
            Sample(view, world, actor);
            var oldField = view.Q<IntegerField>();
            Assert.That(oldField.value, Is.EqualTo(75));
            world.RemoveComponent<HealthComponent>(actor);
            Sample(view, world, actor);
            Assert.That(row.text, Does.EndWith("已移除"));
            Assert.That(oldField.parent, Is.Null);
            world.SetComponent(actor, new HealthComponent { value = 20 });
            Sample(view, world, actor);
            Assert.That(Row<HealthComponent>(view), Is.SameAs(row));
            Assert.That(row.text, Does.EndWith("已添加"));
            Assert.That(view.Q<IntegerField>().value, Is.EqualTo(20));
            Assert.That(view.Q<IntegerField>(), Is.Not.SameAs(oldField));
        }

        [Test]
        public void View_ChangeFilterContinuesObservingHiddenRowsAndResetEstablishesNewBaseline()
        {
            using var world = new World("Component change filter");
            var actor = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75, active = true });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            var row = Row<HealthComponent>(view);
            row.value = true;
            Sample(view, world, actor);
            var filter = Menu(view, "仅显示变化项");
            filter.Execute();
            filter.UpdateActionStatus(null);
            Assert.That(filter.status, Is.EqualTo(DropdownMenuAction.Status.Checked));
            Assert.That(row.style.display.value, Is.EqualTo(DisplayStyle.None));
            world.SetComponent(actor, new HealthComponent { value = 50, active = true });
            Sample(view, world, actor);
            var changed = view.Q<IntegerField>("acc-field-value");
            Assert.That(row.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(changed.value, Is.EqualTo(50));
            Assert.That(changed.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(view.Q<Toggle>("acc-field-active").style.display.value, Is.EqualTo(DisplayStyle.None));
            Menu(view, "重置变化标记").Execute();
            Assert.That(changed.style.backgroundColor.value, Is.EqualTo(Color.clear));
            Assert.That(row.style.display.value, Is.EqualTo(DisplayStyle.None));
            Sample(view, world, actor);
            Assert.That(row.style.display.value, Is.EqualTo(DisplayStyle.None), "Unchanged samples must not restore reset markers.");
            world.SetComponent(actor, new HealthComponent { value = 25, active = true });
            Sample(view, world, actor);
            Assert.That(row.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(changed.value, Is.EqualTo(25));
        }

        [Test]
        public void View_HeaderSearchIgnoresCaseAndClearingRestoresRows()
        {
            using var world = new World("Component search");
            var actor = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75 });
            world.SetComponent(actor, new ManaComponent { value = 40 });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            Row<HealthComponent>(view).value = true;
            Row<ManaComponent>(view).value = true;
            var search = view.Q<ToolbarSearchField>();
            Assert.That(search.parent, Is.SameAs(view.Q<Toolbar>()));
            Assert.That(search.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Get<ToolbarMenu>(view, "m_Options").menu.MenuItems().OfType<DropdownMenuAction>()
                .Any(item => item.name == "搜索组件"), Is.False);
            search.value = typeof(HealthComponent).FullName.ToUpperInvariant();
            Sample(view, world, actor);
            Assert.That(Row<HealthComponent>(view).style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Row<ManaComponent>(view).style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(Values(view)[typeof(ManaComponent)], Is.Null, "Search-hidden components should not be sampled.");
            search.value = string.Empty;
            Assert.That(search.value, Is.Empty);
            Assert.That(search.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Row<ManaComponent>(view).style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void View_ArrayLimitAndShrinkRemoveStaleControls()
        {
            using var world = new World("Component array bounds");
            var actor = world.CreateActor();
            world.SetComponent(actor, new ArrayComponent { values = Enumerable.Range(0, 40).ToArray() });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            Row<ArrayComponent>(view).value = true;
            Sample(view, world, actor);
            Assert.That(view.Q<IntegerField>("acc-field-values.Length").value, Is.EqualTo(40));
            Assert.That(view.Q<IntegerField>("acc-field-values[31]").value, Is.EqualTo(31));
            Assert.That(view.Q("acc-field-values[32]"), Is.Null);
            Assert.That(view.Q<TextField>("acc-field-values[…]").value, Does.Contain("32"));
            var first = view.Q<IntegerField>("acc-field-values[0]");
            world.SetComponent(actor, new ArrayComponent { values = new[] { 10, 20 } });
            Sample(view, world, actor);
            Assert.That(view.Q<IntegerField>("acc-field-values[0]"), Is.SameAs(first));
            Assert.That(first.value, Is.EqualTo(10));
            Assert.That(view.Q<IntegerField>("acc-field-values.Length").value, Is.EqualTo(2));
            Assert.That(view.Q("acc-field-values[2]"), Is.Null);
            Assert.That(view.Q("acc-field-values[…]"), Is.Null);
            world.SetComponent(actor, new ArrayComponent { values = null });
            Sample(view, world, actor);
            Assert.That(view.Q<TextField>("acc-field-values").value, Is.EqualTo("null"));
            Assert.That(view.Q<IntegerField>(), Is.Null);
            world.SetComponent(actor, new ArrayComponent { values = Array.Empty<int>() });
            Sample(view, world, actor);
            Assert.That(view.Q("acc-field-values"), Is.Null);
            Assert.That(view.Q<IntegerField>("acc-field-values.Length").value, Is.Zero);
        }

        [Test]
        public void View_LimitsTotalFieldsAndNestedExpansionWithoutInvokingManagedCode()
        {
            using var world = new World("Component expansion bounds");
            var actor = world.CreateActor();
            var items = new int[40];
            world.SetComponent(actor, new LargeComponent { a = items, b = items, c = items, d = items, e = items });
            world.SetComponent(actor, new NestedComponent { managed = new ManagedValue() });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            var large = Row<LargeComponent>(view);
            large.value = true;
            Row<NestedComponent>(view).value = true;
            Assert.DoesNotThrow(() => Sample(view, world, actor));
            Assert.That(large.Query<VisualElement>().ToList().Count(element => element.name?.StartsWith("acc-field-") == true), Is.EqualTo(128));
            Assert.That(large.tooltip, Does.Contain("128"));
            Assert.That(large.Q("acc-field-e.Length"), Is.Null);
            Assert.That(view.Q<TextField>("acc-field-nested.next.next.next.next").value, Does.Contain("未展开"));
            Assert.That(view.Q("acc-field-nested.next.next.next.next.value"), Is.Null);
            Assert.That(view.Q<TextField>("acc-field-managed").value, Does.Contain("未展开"));
        }

        [Test]
        public void View_TypedControlsAreReadOnlyAndRefreshWithoutReplacingElements()
        {
            using var world = new World("Component typed controls");
            var actor = world.CreateActor();
            world.SetComponent(actor, new TypedComponent { position = new Vector3(1, 2, 3), mode = SampleMode.Idle, label = "first" });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            Row<TypedComponent>(view).value = true;
            Sample(view, world, actor);
            var vector = view.Q<Vector3Field>("acc-field-position");
            var mode = view.Q<EnumField>("acc-field-mode");
            var label = view.Q<TextField>("acc-field-label");
            Assert.That(vector.value, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(mode.value, Is.EqualTo(SampleMode.Idle));
            Assert.That(vector.enabledSelf, Is.False);
            Assert.That(mode.enabledSelf, Is.False);
            Assert.That(label.isReadOnly, Is.True);
            var changes = 0;
            vector.RegisterValueChangedCallback(_ => changes++);
            mode.RegisterValueChangedCallback(_ => changes++);
            label.RegisterValueChangedCallback(_ => changes++);
            world.SetComponent(actor, new TypedComponent { position = new Vector3(4, 5, 6), mode = SampleMode.Active, label = "next" });
            Sample(view, world, actor);
            Assert.That(view.Q<Vector3Field>("acc-field-position"), Is.SameAs(vector));
            Assert.That(view.Q<EnumField>("acc-field-mode"), Is.SameAs(mode));
            Assert.That(view.Q<TextField>("acc-field-label"), Is.SameAs(label));
            Assert.That(vector.value, Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(mode.value, Is.EqualTo(SampleMode.Active));
            Assert.That(label.value, Is.EqualTo("next"));
            Assert.That(changes, Is.Zero, "Sampling must not emit editable field change events.");
        }

        [Test]
        public void View_SharedHeaderAvoidsDuplicateToolbarAndFollowsTabVisibility()
        {
            var header = new Toolbar();
            var events = new ToolbarButton { text = "事件" };
            header.Add(events);
            var view = new ACCComponentView(header);
            Assert.That(view.Q<Toolbar>(), Is.Null);
            var context = Get<Label>(view, "m_Context");
            var search = Get<ToolbarSearchField>(view, "m_Search");
            var options = Get<ToolbarMenu>(view, "m_Options");
            Assert.That(context.parent, Is.SameAs(header));
            Assert.That(search.parent, Is.SameAs(header));
            Assert.That(options.parent, Is.SameAs(header));
            view.SetVisible(false);
            Assert.That(context.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(search.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(options.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(events.style.display.value, Is.Not.EqualTo(DisplayStyle.None));
            view.SetVisible(true);
            Assert.That(context.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(search.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(options.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void View_DefaultsExpandedAndSettingsCanExpandOrCollapseAllComponents()
        {
            using var world = new World("Component expand menu");
            var actor = world.CreateActor();
            world.SetComponent(actor, new HealthComponent { value = 75 });
            world.SetComponent(actor, new ManaComponent { value = 40 });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            var health = Row<HealthComponent>(view);
            var mana = Row<ManaComponent>(view);
            Assert.That(health.value, Is.True);
            Assert.That(mana.value, Is.True);

            var expand = Menu(view, "展开所有组件");
            var collapse = Menu(view, "折叠所有组件");
            expand.UpdateActionStatus(null);
            collapse.UpdateActionStatus(null);
            Assert.That(expand.status, Is.EqualTo(DropdownMenuAction.Status.Disabled));
            Assert.That(collapse.status, Is.EqualTo(DropdownMenuAction.Status.Normal));

            collapse.Execute();
            Assert.That(health.value, Is.False);
            Assert.That(mana.value, Is.False);
            expand.UpdateActionStatus(null);
            collapse.UpdateActionStatus(null);
            Assert.That(expand.status, Is.EqualTo(DropdownMenuAction.Status.Normal));
            Assert.That(collapse.status, Is.EqualTo(DropdownMenuAction.Status.Disabled));

            expand.Execute();
            Assert.That(health.value, Is.True);
            Assert.That(mana.value, Is.True);
            Assert.That(Get<double>(view, "m_NextSample"), Is.EqualTo(0d));
        }

        private static Foldout Row<T>(ACCComponentView view) where T : struct, IComponent
            => view.Query<Foldout>().ToList().Single(row => row.text.StartsWith(typeof(T).Name + " · ", StringComparison.Ordinal));

        private static Dictionary<Type, object> Values(ACCComponentView view)
            => Get<Dictionary<Type, object>>(view, "m_Values");

        private static DropdownMenuAction Menu(ACCComponentView view, string name)
            => Get<ToolbarMenu>(view, "m_Options").menu.MenuItems().OfType<DropdownMenuAction>().Single(item => item.name == name);

        private static void Sample(ACCComponentView view, World world, Actor actor)
        {
            Set(view, "m_NextSample", 0d);
            view.Refresh(world, actor);
        }

        private static T Get<T>(object target, string name)
            => (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);

        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, PrivateInstance).SetValue(target, value);

        private struct HealthComponent : IComponent { public int value; public bool active; }
        private struct ManaComponent : IComponent { public int value; }
        private struct ArrayComponent : IComponent { public int[] values; }
        private struct LargeComponent : IComponent { public int[] a, b, c, d, e; }
        private enum SampleMode { Idle, Active }
        private struct TypedComponent : IComponent { public Vector3 position; public SampleMode mode; public string label; }
        private struct VisibilityComponent : IComponent
        {
            public int visible;
            [SerializeField] private int serialized;
            [HideInInspector] public int hidden;
            [SerializeField, HideInInspector] private int hiddenSerialized;
            private int unexposed;
            public static int shared;
            public int UnsafeProperty => throw new InvalidOperationException("Component properties must not be invoked.");
        }
        private struct NestedComponent : IComponent { public Nested1 nested; public ManagedValue managed; }
        private struct Nested1 { public Nested2 next; }
        private struct Nested2 { public Nested3 next; }
        private struct Nested3 { public Nested4 next; }
        private struct Nested4 { public Nested5 next; }
        private struct Nested5 { public int value; }
        private sealed class ManagedValue
        {
            public int UnsafeProperty => throw new InvalidOperationException("Component properties must not be invoked.");
            public override string ToString() => throw new InvalidOperationException("Managed values must not be formatted by user code.");
        }
    }
}
