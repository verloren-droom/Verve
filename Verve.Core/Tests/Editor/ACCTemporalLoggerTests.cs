namespace Verve.Tests.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;
    using Verve.Editor;

    internal class ACCTemporalLoggerTests
    {
        private static readonly Actor s_First = new(1, 1);
        private static readonly Actor s_Second = new(2, 1);
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Components_ReadOnlyObservedValuesAndRejectDestroyedActors()
        {
            using var world = new World("Component inspection test");
            var actor = world.CreateActor();
            world.SetComponent(actor, new InspectComponent { health = 75, active = true });
            var values = new Dictionary<Type, object>();
            var observed = new HashSet<Type>();
            Assert.That(ACCComponentReader.Read(world, actor, values, observed), Is.True);
            Assert.That(values.ContainsKey(typeof(InspectComponent)), Is.True);
            Assert.That(values[typeof(InspectComponent)], Is.Null);
            observed.Add(typeof(InspectComponent));
            ACCComponentReader.Read(world, actor, values, observed);
            Assert.That(((InspectComponent)values[typeof(InspectComponent)]).health, Is.EqualTo(75));
            typeof(InspectComponent).GetField(nameof(InspectComponent.health)).SetValue(values[typeof(InspectComponent)], 1);
            Assert.That(world.GetComponent<InspectComponent>(actor).health, Is.EqualTo(75));
            Assert.That(ACCComponentReader.GetFields(typeof(InspectComponent)).Length, Is.EqualTo(2));
            world.DestroyActor(actor);
            var replacement = world.CreateActor();
            Assert.That(replacement, Is.Not.EqualTo(actor));
            Assert.That(ACCComponentReader.Read(world, actor, values, observed), Is.False);
            Assert.That(values, Is.Empty);
        }

        [Test]
        public void Components_ViewDefersCollapsedFieldsAndReusesReadonlyControls()
        {
            using var world = new World("Component view test");
            var actor = world.CreateActor();
            world.SetComponent(actor, new InspectComponent { health = 75, active = true });
            var view = new ACCComponentView();
            view.Refresh(world, actor);
            Assert.That(view.Q<IntegerField>(), Is.Null);
            view.Q<Foldout>().value = true;
            view.Refresh(world, actor);
            var field = view.Q<IntegerField>();
            Assert.That(field.value, Is.EqualTo(75));
            Assert.That(field.isReadOnly, Is.True);
            Assert.That(view.Q<Toggle>("acc-field-active").enabledSelf, Is.False);
            world.SetComponent(actor, new InspectComponent { health = 50, active = true });
            Set(view, "m_NextSample", double.MaxValue);
            view.Refresh(world, actor);
            Assert.That(field.value, Is.EqualTo(75), "Samples are throttled within the current interval.");
            Set(view, "m_NextSample", 0d);
            view.Refresh(world, actor);
            Assert.That(view.Q<IntegerField>(), Is.SameAs(field));
            Assert.That(field.value, Is.EqualTo(50));
            Assert.That(view.Q<Foldout>().text, Does.Contain("有变化"));
            world.RemoveComponent<InspectComponent>(actor);
            Set(view, "m_NextSample", 0d);
            view.Refresh(world, actor);
            Assert.That(view.Q<IntegerField>(), Is.Null);
            Assert.That(view.Q<Foldout>().text, Does.Contain("已移除"));
            view.Refresh(null, Actor.none);
            Assert.That(view.Q<Foldout>(), Is.Null);
        }

        [Test]
        public void Reload_InvalidSerializedWorldCanBeReleasedWithoutThrowing()
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            try
            {
                var field = typeof(ACCTemporalLoggerWindow).GetField("m_SelectedWorld", PrivateInstance);
                Assert.That(Attribute.IsDefined(field, typeof(NonSerializedAttribute)), Is.True);
                var restored = (World)FormatterServices.GetUninitializedObject(typeof(World));
                Assert.That(restored.DebugTrace, Is.Null);
                field.SetValue(window, restored);
                Assert.DoesNotThrow(() => Invoke(window, "ReleaseRecording"));
                Assert.That(typeof(ACCTemporalLoggerWindow).GetProperty("HasWorld", PrivateInstance).GetValue(window), Is.False);
                Assert.DoesNotThrow(() => Invoke(window, "OnDisable"));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test]
        public void Tracks_SeparateActorVersionsAndIdenticalShortTypeNames()
        {
            var timeline = CreateTimeline(
                Event(1, 1, CapabilityDebugEventKind.Added),
                Event(2, 1, CapabilityDebugEventKind.Added, s_Second),
                Event(3, 1, CapabilityDebugEventKind.Added, new Actor(1, 2)),
                Event(4, 1, CapabilityDebugEventKind.Added, s_First, typeof(Other.SameName)));
            Assert.That(timeline.groups.Count, Is.EqualTo(3));
            Assert.That(timeline.groups[0].tracks.Count, Is.EqualTo(2));
            Assert.That(timeline.rows.Count, Is.EqualTo(7));
        }

        [Test]
        public void Spans_KeepHiddenDeactivationAndDoNotInferMissingActivation()
        {
            var timeline = CreateTimeline(
                Event(1, 0, CapabilityDebugEventKind.Deactivated),
                Event(2, 1, CapabilityDebugEventKind.Activated),
                Event(3, 2, CapabilityDebugEventKind.Failed),
                Event(4, 3, CapabilityDebugEventKind.Deactivated),
                Event(5, 4, CapabilityDebugEventKind.Activated));
            timeline.Filter("", 1 << (int)CapabilityDebugEventKind.Activated, new HashSet<Actor>());
            var track = timeline.groups[0].tracks[0];
            Assert.That(track.spans.Count, Is.EqualTo(2));
            Assert.That(track.spans[0].start, Is.EqualTo(1));
            Assert.That(track.spans[0].end, Is.EqualTo(3));
            Assert.That(track.spans[1].end, Is.EqualTo(-1));
            Assert.That(track.visibleEvents, Is.EqualTo(new[] { 1, 4 }));
        }

        [Test]
        public void Search_MatchesFullTypeActorAndReasonAndPreservesNavigationWhenCollapsed()
        {
            var timeline = CreateTimeline(Event(1, 1, CapabilityDebugEventKind.Added),
                Event(2, 2, CapabilityDebugEventKind.Blocked, s_Second, error: "Tag movement blocked"));
            timeline.Filter("MOVEMENT", ACCTemporalTimeline.AllKinds, new HashSet<Actor> { s_Second });
            Assert.That(timeline.rows.Count, Is.EqualTo(1));
            Assert.That(timeline.rows[0].track, Is.Null);
            Assert.That(timeline.visibleEvents, Is.EqualTo(new[] { 1 }));
            timeline.Filter("Actor(1:1)", ACCTemporalTimeline.AllKinds, new HashSet<Actor>());
            Assert.That(timeline.visibleEvents, Is.EqualTo(new[] { 0 }));
            timeline.Filter("ACCTemporalLoggerTests+SameName", ACCTemporalTimeline.AllKinds, new HashSet<Actor>());
            Assert.That(timeline.visibleEvents.Count, Is.EqualTo(2));
            timeline.Filter("", 0, new HashSet<Actor>());
            Assert.That(timeline.rows, Is.Empty);
        }

        [Test]
        public void HitTest_UsesRowAndToleranceAndCyclesOverlappingEvents()
        {
            var timeline = CreateTimeline(Event(1, 1, CapabilityDebugEventKind.Added),
                Event(2, 1, CapabilityDebugEventKind.Activated),
                Event(3, 1, CapabilityDebugEventKind.Added, s_Second));
            Assert.That(timeline.HitTest(1, 1, 0.01), Is.EqualTo(0));
            Assert.That(timeline.HitTest(1, 1, 0.01, 1), Is.EqualTo(1));
            Assert.That(timeline.HitTest(1, 1, 0.01, 2), Is.EqualTo(0));
            Assert.That(timeline.HitTest(3, 1, 0.01), Is.EqualTo(2));
            Assert.That(timeline.HitTest(1, 2, 0.01), Is.EqualTo(-1));
            Assert.That(timeline.HitTest(9, 1, 0.01), Is.EqualTo(-1));
        }

        [Test]
        public void Selection_ResolvesSequenceAfterRingBufferEviction()
        {
            var timeline = CreateTimeline(Event(100, 1, CapabilityDebugEventKind.Added),
                Event(101, 2, CapabilityDebugEventKind.Activated));
            timeline.SetEvents(new List<CapabilityDebugEvent> { Event(101, 2, CapabilityDebugEventKind.Activated),
                Event(102, 3, CapabilityDebugEventKind.Deactivated) });
            Assert.That(timeline.FindSequence(100), Is.EqualTo(-1));
            Assert.That(timeline.FindSequence(101), Is.EqualTo(0));
        }

        [TestCase(0.01, 320)]
        [TestCase(10, 720)]
        [TestCase(86400, 2000)]
        public void Ticks_RemainFiniteAndBounded(double duration, double width)
        {
            var tick = ACCTemporalTimeline.TickInterval(duration, width);
            Assert.That(tick, Is.GreaterThan(0));
            Assert.That(Game.NumberUtility.IsFinite(tick), Is.True);
            Assert.That(duration / tick, Is.LessThan(100));
        }

        [TestCase(0d, 4d, 0d, 10d, 0d)]
        [TestCase(0d, 9d, 0d, 10d, 0d)]
        [TestCase(0d, 12d, 0d, 10d, 3d)]
        [TestCase(0d, 13d, 3d, 10d, 4d)]
        [TestCase(0d, 12d, 20d, 10d, 3d)]
        [TestCase(10d, 12d, 0d, 10d, 10d)]
        [TestCase(0d, 12d, 0d, 2d, 10.2d)]
        [TestCase(0d, 12d, 0d, 0.01d, 11.991d)]
        public void Follow_ViewportKeepsLiveEdgeVisibleWithoutChangingZoom(double start, double end,
            double viewStart, double duration, double expected)
        {
            var result = ACCTemporalTimeline.FollowViewStart(start, end, viewStart, duration);
            Assert.That(result, Is.EqualTo(expected).Within(1e-9));
            Assert.That(result, Is.GreaterThanOrEqualTo(start));
            Assert.That(end, Is.InRange(result, result + duration));
        }

        [Test]
        public void Follow_ManualSeekHoldsPositionAndEndResumesWithNoMatchingEvents()
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            try
            {
                Invoke(window, "CreateGUI");
                Invoke(window, "OnDisable");
                var timeline = Get<ACCTemporalTimeline>(window, "m_Timeline");
                timeline.SetEvents(new List<CapabilityDebugEvent> { Event(1, 1, CapabilityDebugEventKind.Added) });
                timeline.Filter("", 0, new HashSet<Actor>());
                Set(window, "m_DataStart", 1d);
                Set(window, "m_DataEnd", 20d);
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.True);
                Invoke(window, "ResumeFollowing");
                var preview = window.rootVisualElement.Q<ToolbarToggle>("acc-preview");
                Assert.That(preview.value, Is.True);
                Assert.That(preview.Q<Image>().image, Is.SameAs(EditorGUIUtility.IconContent("PauseButton").image));
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(20d));
                preview.value = false;
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.False);
                Assert.That(Get<bool>(window, "m_Playing"), Is.False);
                Assert.That(preview.Q<Image>().image, Is.SameAs(EditorGUIUtility.IconContent("Animation.Play").image));
                preview.value = true;
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.True);
                Assert.That(Get<bool>(window, "m_Playing"), Is.False, "Playing at the live edge resumes following without rewinding.");
                Invoke(window, "SetPlayhead", 5d, false);
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.False);
                Assert.That(preview.value, Is.False);
                Set(window, "m_DataEnd", 25d);
                Invoke(window, "FollowLatest");
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(5d));
                Assert.That(timeline.visibleEvents, Is.Empty);
                Invoke(window, "NavigateEvent", 2);
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.True);
                Assert.That(preview.value, Is.True);
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(25d));
                Assert.That(Get<double>(window, "m_ViewDuration"), Is.EqualTo(10d));
                Invoke(window, "SetPlayhead", 4d, false);
                Invoke(window, "ClearEvents");
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.True);
                Assert.That(Get<double>(window, "m_Playhead"), Is.Zero);
                Assert.That(preview.value, Is.False, "An empty timeline has nothing to play even when following is armed.");
                Assert.That(timeline.events, Is.Empty);
                Invoke(window, "FollowLatest");
                Assert.That(Get<double>(window, "m_Playhead"), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test]
        public void Follow_PreviewAndPausedInspectionDoNotJumpToTheLiveEdge()
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            try
            {
                var timeline = Get<ACCTemporalTimeline>(window, "m_Timeline");
                timeline.SetEvents(new List<CapabilityDebugEvent> { Event(1, 0, CapabilityDebugEventKind.Added) });
                Set(window, "m_DataEnd", 20d);
                Invoke(window, "FollowLatest");
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(20d));
                Invoke(window, "SetPlayhead", 5d, false);
                Invoke(window, "SetPlaying", true);
                Assert.That(Get<bool>(window, "m_Playing"), Is.True);
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.False);
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(5d));
                Set(window, "m_DataEnd", 25d);
                Invoke(window, "FollowLatest");
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(5d));
                Invoke(window, "SetPlaying", false);
                Invoke(window, "FollowLatest");
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(5d));
                Invoke(window, "ResumeFollowing");
                Assert.That(Get<bool>(window, "m_Playing"), Is.False);
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.True);
                Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(25d));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test]
        public void Follow_SpacePausesAndResumesTheSameStateAsTheToolbar()
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            try
            {
                Invoke(window, "CreateGUI");
                Invoke(window, "OnDisable");
                Get<VisualElement>(window, "m_TimelineView").SetEnabled(true);
                Get<ACCTemporalTimeline>(window, "m_Timeline").SetEvents(new List<CapabilityDebugEvent>
                    { Event(1, 0, CapabilityDebugEventKind.Added) });
                Set(window, "m_DataEnd", 20d);
                Invoke(window, "ResumeFollowing");
                var preview = window.rootVisualElement.Q<ToolbarToggle>("acc-preview");
                foreach (var active in new[] { false, true })
                {
                    using var evt = KeyDownEvent.GetPooled('\0', KeyCode.Space, EventModifiers.None);
                    evt.target = Get<VisualElement>(window, "m_Canvas");
                    Invoke(window, "OnKeyDown", evt);
                    Assert.That(preview.value, Is.EqualTo(active));
                    Assert.That(Get<bool>(window, "m_FollowLatest"), Is.EqualTo(active));
                    Assert.That(Get<bool>(window, "m_Playing"), Is.False);
                    Assert.That(Get<double>(window, "m_Playhead"), Is.EqualTo(20d));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test]
        public void Logging_StreamsBeyondBufferCapacityAndUnsubscribesOnRelease()
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            var path = Path.GetTempFileName();
            var trace = new CapabilityDebugTrace();
            var capability = new LogCapability();
            try
            {
                Assert.That(Get<bool>(window, "m_WriteLog"), Is.False);
                var writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read));
                Set(window, "m_LogWriter", writer);
                Set(window, "m_LogTrace", trace);
                var callback = (Action<CapabilityDebugEvent>)Delegate.CreateDelegate(typeof(Action<CapabilityDebugEvent>),
                    window, typeof(ACCTemporalLoggerWindow).GetMethod("WriteLogEvent", PrivateInstance));
                trace.Recorded += callback;
                trace.SetOwnerEnabled(this, true);
                for (var i = 0; i < 300; i++) trace.Record(s_First, capability, CapabilityDebugEventKind.Added);
                Invoke(window, "FlushLog");
                Assert.That(trace.Count, Is.EqualTo(256));
                Assert.That(File.ReadAllLines(path).Length, Is.EqualTo(300));
                var reason = "tab\tquote\"\nnext line";
                trace.Record(s_First, capability, CapabilityDebugEventKind.Failed, reason);
                Invoke(window, "ReleaseRecording");
                var contents = File.ReadAllText(path);
                Assert.That(contents, Does.Contain("301\tActor(1:1)\t"));
                Assert.That(contents, Does.Contain("\"tab\tquote\"\"\nnext line\""));
                Assert.That(Get<StreamWriter>(window, "m_LogWriter"), Is.Null);
                Assert.That(Get<CapabilityDebugTrace>(window, "m_LogTrace"), Is.Null);
                Assert.DoesNotThrow(() => trace.Record(s_First, capability, CapabilityDebugEventKind.Removed));
                Assert.That(File.ReadAllText(path), Is.EqualTo(contents));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                File.Delete(path);
            }
        }

        [TestCase("Write")]
        [TestCase("Flush")]
        [TestCase("Dispose")]
        public void Logging_FailureDisablesWriterUnsubscribesAndWarnsOnce(string operation)
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            var trace = new CapabilityDebugTrace();
            var capability = new LogCapability();
            try
            {
                Set(window, "m_WriteLog", true);
                Set(window, "m_LogWriter", new FailingLogWriter(operation));
                Set(window, "m_LogTrace", trace);
                var callback = (Action<CapabilityDebugEvent>)Delegate.CreateDelegate(typeof(Action<CapabilityDebugEvent>),
                    window, typeof(ACCTemporalLoggerWindow).GetMethod("WriteLogEvent", PrivateInstance));
                trace.Recorded += callback;
                trace.SetOwnerEnabled(this, true);
                LogAssert.Expect(LogType.Warning, "ACC 日志写入已关闭：Expected log failure");
                if (operation == "Write") trace.Record(s_First, capability, CapabilityDebugEventKind.Added);
                else if (operation == "Flush") Invoke(window, "FlushLog");
                else Invoke(window, "StopFileLogging", true);
                Assert.That(Get<bool>(window, "m_WriteLog"), Is.False);
                Assert.That(Get<StreamWriter>(window, "m_LogWriter"), Is.Null);
                Assert.That(Get<CapabilityDebugTrace>(window, "m_LogTrace"), Is.Null);
                // 清理时再次发生 I/O 错误，也只报告最初的一次失败。
                Assert.DoesNotThrow(() => trace.Record(s_First, capability, CapabilityDebugEventKind.Removed));
                Assert.DoesNotThrow(() => Invoke(window, "FlushLog"));
                Assert.DoesNotThrow(() => Invoke(window, "ReleaseRecording"));
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator Layout_FillsViewportAndAlignsRulerAtSmallAndLargeSizes()
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            try
            {
                window.titleContent = new GUIContent("ACC Layout Test");
                window.ShowUtility();
                foreach (var size in new[] { new Vector2(760, 420), new Vector2(1200, 700) })
                {
                    window.position = new Rect(60, 60, size.x, size.y);
                    for (var i = 0; i < 8; i++) yield return null;
                    var root = window.rootVisualElement;
                    var canvas = root.Q("acc-track-canvas");
                    var ruler = root.Q("acc-ruler");
                    var headers = root.Q("acc-track-headers");
                    var scroll = root.Q<ScrollView>("acc-tracks");
                    Assert.That(canvas.worldBound.x, Is.EqualTo(ruler.worldBound.x).Within(1f));
                    Assert.That(canvas.worldBound.width, Is.EqualTo(ruler.worldBound.width).Within(1f));
                    Assert.That(headers.worldBound.y, Is.EqualTo(canvas.worldBound.y).Within(1f));
                    Assert.That(canvas.worldBound.height, Is.GreaterThanOrEqualTo(scroll.contentViewport.worldBound.height - 1f));
                    Assert.That(canvas.worldBound.width, Is.GreaterThan(300f));
                    Assert.That(root.Q("acc-status").worldBound.yMax, Is.LessThanOrEqualTo(root.worldBound.yMax + 1f));
                    var toolbar = root.Q("acc-transport");
                    Assert.That(toolbar.worldBound.height, Is.EqualTo(22f).Within(1f));
                    Assert.That(toolbar[0].name, Is.EqualTo("acc-world"));
                    Assert.That(toolbar[1].name, Is.EqualTo("acc-record"));
                    Assert.That(Get<bool>(window, "m_RecordingEnabled"), Is.True);
                    Assert.That(root.Q("acc-record").enabledInHierarchy, Is.True);
                    Assert.That(root.Q("acc-world").enabledInHierarchy, Is.False);
                    Assert.That(root.Q("acc-follow"), Is.Null);
                    Assert.That(root.Q("acc-zoom"), Is.Null);
                    foreach (var name in new[] { "acc-record", "acc-preview", "acc-time", "acc-world", "acc-options" })
                    {
                        var control = root.Q(name);
                        Assert.That(control.worldBound.height, Is.EqualTo(toolbar.worldBound.height).Within(1f), name);
                        Assert.That(control.worldBound.center.y, Is.EqualTo(toolbar.worldBound.center.y).Within(1f), name);
                    }
                    var filters = root.Q("acc-filters");
                    Assert.That(filters.childCount, Is.EqualTo(2));
                    Assert.That(toolbar.resolvedStyle.borderBottomWidth, Is.EqualTo(1f));
                    Assert.That(filters.resolvedStyle.borderBottomWidth, Is.EqualTo(1f));
                    var empty = root.Q("acc-empty");
                    Assert.That(empty.worldBound.center.x, Is.EqualTo(ruler.worldBound.center.x).Within(1f));
                    Assert.That(empty.worldBound.center.y, Is.EqualTo(scroll.contentViewport.worldBound.center.y).Within(1f));
                    Assert.That(empty.resolvedStyle.unityTextAlign, Is.EqualTo(TextAnchor.MiddleCenter));
                    Assert.That(root.Q<TwoPaneSplitView>("acc-content-split"), Is.Not.Null);
                    Assert.That(root.Q("acc-header-resizer").enabledInHierarchy, Is.True);
                    Assert.That(root.Q("acc-components-pane").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                    Assert.That(root.Q("acc-status").parent, Is.SameAs(root.Q("acc-time-scroll").parent));
                    foreach (var control in filters.Children())
                    {
                        Assert.That(control.worldBound.height, Is.EqualTo(filters.worldBound.height).Within(1f));
                        Assert.That(control.worldBound.center.y, Is.EqualTo(filters.worldBound.center.y).Within(1f));
                    }
                }
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator Layout_EventFilterKeepsTextAndArrowAlignedWhenContentChanges()
        {
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            try
            {
                window.ShowUtility();
                for (var i = 0; i < 5; i++) yield return null;
                Invoke(window, "OnDisable");
                Set(window, "m_Initialized", true);
                var filter = window.rootVisualElement.Q<ToolbarButton>("acc-event-filter");
                var label = filter.Q<Label>();
                foreach (var size in new[] { new Vector2(760, 420), new Vector2(1200, 700) })
                {
                    window.position = new Rect(60, 60, size.x, size.y);
                    var glyphY = float.NaN;
                    foreach (var mask in new[] { ACCTemporalTimeline.AllKinds, 0 })
                    {
                        Invoke(window, "SetKindMask", mask);
                        filter.Focus();
                        for (var i = 0; i < 5; i++) yield return null;
                        Assert.That(label.text, Is.EqualTo(mask == 0 ? "事件筛选" : "全部事件"));
                        Assert.That(label.worldBound.center.y, Is.EqualTo(filter.worldBound.center.y).Within(0.5f));
                        Assert.That(label.resolvedStyle.fontSize, Is.EqualTo(11f));
                        Assert.That(label.resolvedStyle.unityFontStyleAndWeight, Is.EqualTo(FontStyle.Normal));
                        Assert.That(label.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.NoWrap));
                        var currentY = GlyphY(label) - label.worldBound.y;
                        if (Game.NumberUtility.IsFinite(glyphY)) Assert.That(currentY, Is.EqualTo(glyphY).Within(0.5f));
                        glyphY = currentY;
                        var arrow = filter.Q("acc-event-filter-arrow");
                        Assert.That(arrow.worldBound.center.y, Is.EqualTo(label.worldBound.center.y).Within(0.5f));
                        Assert.That(arrow.worldBound.xMin, Is.GreaterThanOrEqualTo(label.worldBound.xMax + 3f));
                    }
                }
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator Layout_SharedScrollKeepsRowsAlignedAndZoomKeepsPointerTime()
        {
            using var world = new World("Component pane layout");
            var window = ScriptableObject.CreateInstance<ACCTemporalLoggerWindow>();
            try
            {
                window.ShowUtility();
                window.position = new Rect(60, 60, 960, 500);
                for (var i = 0; i < 5; i++) yield return null;
                // 暂停自动发现真实世界；样本仅写入测试窗口的数据视图。
                Invoke(window, "OnDisable");
                Set(window, "m_Initialized", true);
                var timeline = Get<ACCTemporalTimeline>(window, "m_Timeline");
                Set(window, "m_SelectedWorld", world);
                var events = new List<CapabilityDebugEvent>();
                for (var i = 0; i < 20; i++)
                    events.Add(Event(i + 1, i, CapabilityDebugEventKind.Activated, world.CreateActor()));
                var actor = events[0].actor;
                world.SetComponent(actor, new InspectComponent { health = 75, active = true });
                timeline.SetEvents(events);
                Set(window, "m_DataEnd", 20d);
                Invoke(window, "RebuildRows");
                for (var i = 0; i < 5; i++) yield return null;
                var root = window.rootVisualElement;
                Invoke(window, "SelectActor", actor);
                Assert.That(Get<Actor>(window, "m_SelectedActor"), Is.EqualTo(actor));
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.True);
                Assert.That(Get<bool>(window, "m_ComponentsVisible"), Is.True);
                Assert.That(root.Q("acc-components").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                var scroll = root.Q<ScrollView>("acc-tracks");
                scroll.scrollOffset = new Vector2(0, 150);
                Invoke(window, "SetTrackWidth", 310f);
                for (var i = 0; i < 5; i++) yield return null;
                var canvas = root.Q("acc-track-canvas");
                var headers = root.Q("acc-track-headers");
                Assert.That(headers.worldBound.y, Is.EqualTo(canvas.worldBound.y).Within(1f));
                Assert.That(canvas.worldBound.x, Is.EqualTo(root.Q("acc-ruler").worldBound.x).Within(1f));
                Assert.That(scroll.scrollOffset.y, Is.GreaterThan(0));
                var pixel = canvas.resolvedStyle.width * 0.5f;
                var before = (double)Invoke(window, "PixelToTime", pixel);
                Invoke(window, "ZoomAt", 0.5d, pixel);
                var after = (double)Invoke(window, "PixelToTime", pixel);
                Assert.That(after, Is.EqualTo(before).Within(0.001d));
                Assert.That(Get<double>(window, "m_ViewDuration"), Is.EqualTo(5d));
                Invoke(window, "SelectEvent", 0, false);
                for (var i = 0; i < 5; i++) yield return null;
                var details = root.Q("acc-components-pane");
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.False);
                Assert.That(Get<bool>(window, "m_ComponentsVisible"), Is.True);
                Assert.That(root.Q("acc-detail-events"), Is.Null);
                Assert.That(root.Q("acc-detail-components"), Is.Null);
                Assert.That(root.Q("acc-event-details"), Is.Null);
                Assert.That(details.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(details.worldBound.yMin,
                    Is.GreaterThanOrEqualTo(root.Q("acc-timeline-pane").worldBound.yMax - 1f));
                var componentView = root.Q<ACCComponentView>();
                Assert.That(componentView.Q<Toolbar>(), Is.Null, "The component view shares one compact header.");
                var header = root.Q<Toolbar>("acc-component-toolbar");
                Assert.That(header.childCount, Is.EqualTo(3));
                Assert.That(header.Q<Label>().text, Does.Contain("实时组件"));
                var search = header.Q<ToolbarSearchField>("acc-component-search");
                Assert.That(search.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(search.worldBound.center.y, Is.EqualTo(header.worldBound.center.y).Within(1f));
                componentView.Q<Foldout>().value = true;
                Set(componentView, "m_NextSample", 0d);
                Invoke(window, "UpdateComponents");
                Assert.That(componentView.Q<IntegerField>().value, Is.EqualTo(75));
                var height = root.Q("acc-timeline-pane").worldBound.height;
                world.DestroyActor(actor);
                Invoke(window, "UpdateComponents");
                for (var i = 0; i < 5; i++) yield return null;
                Assert.That(details.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Assert.That(componentView.Q<Foldout>(), Is.Null);
                Assert.That(root.Q("acc-timeline-pane").worldBound.height, Is.GreaterThan(height));
                Invoke(window, "SelectEvent", 0, false);
                Assert.That(Get<bool>(window, "m_ComponentsVisible"), Is.False, "A historical event must not open an empty component pane.");
                Invoke(window, "OnPlayModeChanged", PlayModeStateChange.EnteredEditMode);
                for (var i = 0; i < 5; i++) yield return null;
                Assert.That(details.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Assert.That(timeline.events, Is.Empty);
                Assert.That(timeline.rows, Is.Empty);
                Assert.That(headers.childCount, Is.Zero);
                Assert.That(Get<double>(window, "m_Playhead"), Is.Zero);
                Assert.That(Get<long>(window, "m_SelectedSequence"), Is.EqualTo(-1));
                Assert.That(Get<bool>(window, "m_FollowLatest"), Is.True);
                Assert.That(root.Q("acc-timeline").enabledSelf, Is.False);
            }
            finally { window.Close(); }
        }

        private static ACCTemporalTimeline CreateTimeline(params CapabilityDebugEvent[] events)
        {
            var timeline = new ACCTemporalTimeline();
            timeline.SetEvents(new List<CapabilityDebugEvent>(events));
            timeline.Filter("", ACCTemporalTimeline.AllKinds, new HashSet<Actor>());
            return timeline;
        }

        private static CapabilityDebugEvent Event(long sequence, float time, CapabilityDebugEventKind kind,
            Actor? actor = null, Type type = null, string error = null)
            => new(sequence, actor ?? s_First, type ?? typeof(SameName), kind, (int)(time * 60), time, error);

        private static object Invoke(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, PrivateInstance).Invoke(target, args);
        private static T Get<T>(object target, string name)
            => (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);
        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, PrivateInstance).SetValue(target, value);

        private static float GlyphY(TextElement text)
        {
            var handle = typeof(TextElement).GetProperty("uitkTextHandle", PrivateInstance).GetValue(text);
            handle.GetType().GetMethod("ComputeSettingsAndUpdate").Invoke(handle, null);
            var position = (Vector2)handle.GetType().GetMethod("GetCursorPositionFromStringIndexUsingCharacterHeight")
                .Invoke(handle, new object[] { text.text.IndexOf('件'), true });
            return text.LocalToWorld(text.contentRect.position + position).y;
        }

        private sealed class SameName { }
        private struct InspectComponent : IComponent
        {
            public int health;
            public bool active;
            public int UnsafeProperty => throw new InvalidOperationException("Properties must not be evaluated by inspection.");
        }
        private sealed class LogCapability : Capability { }
        private sealed class FailingLogWriter : StreamWriter
        {
            private readonly string m_Operation;
            internal FailingLogWriter(string operation) : base(new MemoryStream()) => m_Operation = operation;
            public override void WriteLine(string value)
            {
                if (m_Operation == "Write") throw new IOException("Expected log failure");
                base.WriteLine(value);
            }
            public override void Flush()
            {
                if (m_Operation == "Flush") throw new IOException("Expected log failure");
                base.Flush();
            }
            protected override void Dispose(bool disposing)
            {
                try { base.Dispose(disposing); }
                finally { if (disposing) throw new IOException("Expected log failure"); }
            }
        }
        private static class Other { internal sealed class SameName { } }
    }
}
