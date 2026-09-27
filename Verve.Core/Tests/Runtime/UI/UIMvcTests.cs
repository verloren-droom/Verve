
namespace Verve.Tests.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UnityEngine.TestTools;
    using Verve;

    [Category("UI")]
    internal sealed class UIMvcTests
    {
        [Test]
        public void ModelBase_SetProperty_NotifiesOnlyWhenValueChanges()
        {
            var model = new ObservableModel();
            var changed = new List<string>();
            model.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

            model.UserName = "player";
            model.UserName = "player";
            model.UserName = "guest";

            Assert.That(changed, Is.EqualTo(new[] { nameof(ObservableModel.UserName), nameof(ObservableModel.UserName) }));
        }

        private sealed class ObservableModel : ModelBase
        {
            private string m_UserName;

            public string UserName
            {
                get => m_UserName;
                set => SetProperty(ref m_UserName, value);
            }
        }

    }

    [Category("UI")]
    internal sealed class UIManagerTests
    {
        private GameModules m_Modules;
        private TestLoaderModule m_Loader;
        private UIModule m_UI;
        private GameObject m_ViewPrefab;
        private GameObject m_ViewRoot;

        [SetUp]
        public void SetUp()
        {
            m_ViewPrefab = new GameObject("UI Test Prefab");
            m_ViewPrefab.AddComponent<UIViewComponent>();
            m_ViewPrefab.SetActive(false);

            m_ViewRoot = new GameObject(
                "UI Test Root",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            m_ViewRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            m_Loader = new TestLoaderModule(m_ViewPrefab);
            m_UI = new UIModule();
            m_UI.SetViewRoot(m_ViewRoot.transform);

            m_Modules = new GameModules();
            m_Modules.Install(() => m_Loader);
            m_Modules.Install(() => m_UI);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                m_Modules?.Dispose();
            }
            finally
            {
                ClosingDuringOpenView.CloseRequest = null;
                if (m_ViewRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_ViewRoot);
                }

                if (m_ViewPrefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_ViewPrefab);
                }
            }
        }

        [Test]
        public void StressManyViews_CloseTrimAndReleaseDeactivateComponentsAndReleaseHandles()
        {
            m_ViewPrefab.AddComponent<ActivationProbe>();
            m_UI.KeepAliveLimit = 16;
            for (var round = 0; round < 4; round++)
            {
                var probes = new ActivationProbe[128];
                for (var index = 0; index < probes.Length; index++)
                {
                    var view = m_UI.Open<KeepAliveTestView>(openMode: UIViewOpenMode.New);
                    probes[index] = view.Component.GetComponent<ActivationProbe>();
                    Assert.That(probes[index].EnableCount, Is.EqualTo(1));
                }
                m_UI.CloseAll();
                Assert.That(m_UI.ViewCount, Is.EqualTo(16));
                foreach (var probe in probes)
                {
                    Assert.That(probe.gameObject.activeSelf, Is.False);
                    Assert.That(probe.DisableCount, Is.EqualTo(1));
                }
                m_UI.ReleaseAll();
                Assert.That(m_UI.ViewCount, Is.Zero);
                Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo((round + 1) * 128));
            }
        }

        [Test]
        public void CanvasOnlyHiding_KeepsComponentsActiveUntilRelease()
        {
            m_ViewPrefab.AddComponent<Canvas>();
            m_ViewPrefab.AddComponent<ActivationProbe>();
            var view = m_UI.Open<KeepAliveTestView>();
            var node = view.Component.gameObject;
            var probe = node.GetComponent<ActivationProbe>();
            m_UI.Close(view);
            Assert.That(node.activeInHierarchy, Is.True);
            Assert.That(node.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(probe.DisableCount, Is.Zero);
            Assert.That(m_UI.Open<KeepAliveTestView>(), Is.SameAs(view));
            Assert.That(probe.EnableCount, Is.EqualTo(1));
            m_UI.Release(view);
            Assert.That(node.activeSelf, Is.False, "Deactivate components before Unity's deferred destruction.");
            Assert.That(probe.DisableCount, Is.EqualTo(1));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void CreatingActivePageFails_DeactivatesComponentsAndReleasesHandle()
        {
            m_ViewPrefab.AddComponent<ActivationProbe>();
            m_ViewPrefab.SetActive(true);
            ActivationProbe probe = null;
            CreationCallbackView.Callback = () =>
            {
                probe = m_ViewRoot.GetComponentInChildren<ActivationProbe>();
                throw new InvalidOperationException("Page creation failed.");
            };
            try
            {
                Assert.Throws<InvalidOperationException>(() => m_UI.Open<CreationCallbackView>());
                Assert.That(probe, Is.Not.Null);
                Assert.That(probe.gameObject.activeSelf, Is.False);
                Assert.That(probe.DisableCount, Is.EqualTo(probe.EnableCount));
                Assert.That(probe.DisableCount, Is.GreaterThan(0));
                Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
                Assert.That(m_UI.ViewCount, Is.Zero);
            }
            finally { CreationCallbackView.Callback = null; }
        }

        private sealed class ActivationProbe : MonoBehaviour
        {
            internal int EnableCount { get; private set; }
            internal int DisableCount { get; private set; }
            private void OnEnable() => EnableCount++;
            private void OnDisable() => DisableCount++;
        }

        [Test]
        public void SetViewRoot_AfterInstallBeforeFirstOpen_IsAllowed()
        {
            var customRoot = new GameObject(
                "Custom UI Root",
                typeof(RectTransform),
                typeof(Canvas));
            customRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                m_UI.SetViewRoot(customRoot.transform);

                Assert.That(m_UI.ViewRoot, Is.SameAs(customRoot.transform));
                var view = m_UI.Open<KeepAliveTestView>();
                Assert.That(view.Component.transform.parent.parent, Is.SameAs(customRoot.transform));
                Assert.That(m_UI.Release<KeepAliveTestView>(), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(customRoot);
            }
        }

        [Test]
        public void SetViewRoot_AfterFirstOpen_IsRejected()
        {
            var customRoot = new GameObject(
                "Custom UI Root",
                typeof(RectTransform),
                typeof(Canvas));
            customRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                m_UI.Open<KeepAliveTestView>();

                Assert.That(
                    () => m_UI.SetViewRoot(customRoot.transform),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                m_UI.Release<KeepAliveTestView>();
                UnityEngine.Object.DestroyImmediate(customRoot);
            }
        }

        [Test]
        public void SetViewRoot_AfterExplicitParentOpen_IsRejected()
        {
            var customRoot = new GameObject(
                "Custom UI Root",
                typeof(RectTransform),
                typeof(Canvas));
            customRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                m_UI.Open<KeepAliveTestView>(parent: m_ViewRoot.transform);

                Assert.That(
                    () => m_UI.SetViewRoot(customRoot.transform),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                m_UI.Release<KeepAliveTestView>();
                UnityEngine.Object.DestroyImmediate(customRoot);
            }
        }

        [Test]
        public void TypedView_CacheClearsBorrowedDataAndControllerReadsTheTypedView()
        {
            var firstData = new OpenPayload();
            var secondData = new OpenPayload();
            IUIManager manager = m_UI;
            var view = manager.Open<ReferenceDataView>(firstData);
            Assert.That(view.OpenArgs, Is.SameAs(firstData));
            Assert.That(view.OpenedWithCommittedData, Is.True);
            using var controller = new TypedController();
            controller.Bind(view, new object());
            Assert.That(controller.OpenArgsSeen, Is.SameAs(firstData));

            Assert.That(manager.Close(view), Is.True);
            Assert.That(view.OpenArgs, Is.Null);
            Assert.That(view.ClosedWithClearedData, Is.True);
            Assert.That(controller.OpenArgsSeen, Is.Null);
            Assert.That(manager.Open<ReferenceDataView>(secondData), Is.SameAs(view));
            Assert.That(controller.OpenArgsSeen, Is.SameAs(secondData));
            Assert.That(manager.Release(view), Is.True);
            Assert.That(view.OpenArgs, Is.Null);
            Assert.That(controller.IsBound, Is.False);
            Assert.That(firstData.Disposed || secondData.Disposed, Is.False);
        }

        [Test]
        public void TypedView_RejectsMissingOrConflictingDataWithoutChangingTheOpenView()
        {
            Assert.Throws<ArgumentNullException>(() => m_UI.Open<ReferenceDataView>());
            Assert.Throws<ArgumentNullException>(() => m_UI.OpenAsync(typeof(ReferenceDataView)));
            Assert.Throws<ArgumentException>(() => m_UI.Open<ReferenceDataView>(new IdArgs(7)));
            Assert.Throws<ArgumentException>(() => m_UI.Open<KeepAliveTestView>(new OpenPayload()));
            Assert.That(m_Loader.LoadCount, Is.Zero);
            Assert.That(m_UI.ViewCount, Is.Zero);
            Assert.That(m_UI.OpeningViewCount, Is.Zero);

            var data = new OpenPayload();
            var view = m_UI.Open<ReferenceDataView>(data);
            var parent = view.Component.transform.parent;
            Assert.Throws<InvalidOperationException>(() => m_UI.Open<ReferenceDataView>(new OpenPayload()));
            Assert.That(view.OpenArgs, Is.SameAs(data));
            Assert.That(view.IsOpen, Is.True);
            Assert.That(view.Component.transform.parent, Is.SameAs(parent));
            Assert.That(view.OpenCount, Is.EqualTo(1));
            Assert.That(m_UI.Open<ReferenceDataView>(data), Is.SameAs(view));
            Assert.That(view.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void TypedView_FailingOpenAndCloseClearDataAndPreserveOwnership()
        {
            var data = new OpenPayload();
            var view = m_UI.Open<ReferenceDataView>(data);
            m_UI.Close(view);
            view.ThrowOnOpen = true;
            Assert.Throws<InvalidOperationException>(() => m_UI.Open<ReferenceDataView>(data));
            Assert.That(view.OpenArgs, Is.Null);
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.Component.gameObject.activeSelf, Is.False);

            view.ThrowOnOpen = false;
            m_UI.Open<ReferenceDataView>(data);
            view.ThrowOnClose = true;
            Assert.Throws<InvalidOperationException>(() => m_UI.Close(view));
            Assert.That(view.OpenArgs, Is.Null);
            Assert.That(view.IsOpen, Is.False);
            Assert.That(data.Disposed, Is.False);
        }

        [Test]
        public async Task TypedAsync_SameArgsShareLoadAndDifferentInstancesConflict()
        {
            var gate = new TaskCompletionSource<bool>();
            var loadCount = 0;
            m_Loader.BeforeLoadAsync = _ => { loadCount++; return gate.Task; };
            var args = new IdArgs(7);
            var first = m_UI.OpenAsync<IdView>(args);
            var second = m_UI.OpenAsync<IdView>(args);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await m_UI.OpenAsync<IdView>(new IdArgs(7)));
            Assert.That(m_UI.OpeningViewCount, Is.EqualTo(1));
            gate.SetResult(true);
            var view = await first;
            Assert.That(await second, Is.SameAs(view));
            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(view.OpenArgs.Id, Is.EqualTo(7));
            Assert.That(m_UI.OpeningViewCount, Is.Zero);
            var independent = await m_UI.OpenAsync<IdView>(new IdArgs(8), openMode: UIViewOpenMode.New);
            Assert.That(independent, Is.Not.SameAs(view));
            Assert.That(independent.OpenArgs.Id, Is.EqualTo(8));
            m_UI.Close(view);
            Assert.That(view.OpenArgs, Is.Null);
        }

        [Test]
        public async Task TypedAsync_CanceledIndependentOpenReleasesLateHandleAndBorrowsData()
        {
            var gate = new TaskCompletionSource<bool>();
            m_Loader.BeforeLoadAsync = _ => gate.Task;
            using var cancellation = new CancellationTokenSource();
            var data = new OpenPayload();
            var opening = m_UI.OpenAsync<ReferenceDataView>(data,
                ct: cancellation.Token, openMode: UIViewOpenMode.New);
            cancellation.Cancel();
            gate.SetResult(true);
            try { await opening; Assert.Fail("Expected cancellation."); }
            catch (OperationCanceledException) { }
            await m_Modules.UninstallAsync<UIModule>();
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
            Assert.That(m_UI.ViewCount, Is.Zero);
            Assert.That(m_UI.OpeningViewCount, Is.Zero);
            Assert.That(data.Disposed, Is.False);
        }

        [Test]
        public async Task TypedAsync_ExplicitPathAndAfterOpenReceiveTypedData()
        {
            var data = new OpenPayload();
            var callbacks = 0;
            var view = await m_UI.OpenAsync<ReferenceDataView>(
                "Assets/UI/ReferenceDataView.prefab", UILayer.Main, UIViewCacheMode.KeepAlive,
                (opened, ct) =>
                {
                    Assert.That(opened.OpenArgs, Is.SameAs(data));
                    callbacks++;
                    return Task.CompletedTask;
                }, args: data);
            Assert.That(callbacks, Is.EqualTo(1));
            m_UI.Close(view);
            Assert.That(m_UI.Open<ReferenceDataView>(
                "Assets/UI/ReferenceDataView.prefab", UILayer.Main, UIViewCacheMode.KeepAlive, data), Is.SameAs(view));
            Assert.That(view.OpenArgs, Is.SameAs(data));
        }

        [Test]
        public void TypedView_ReusingArgsDoesNotAllocateAParameterWrapper()
        {
            var args = new IdArgs(7);
            var view = m_UI.Open<IdView>(args);
            view.OpenInternal(args);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 256; i++) view.OpenInternal(args);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [Test]
        public void KeepAliveView_ReopensSameInstanceAndReleasesHandleOnlyOnRelease()
        {
            var first = m_UI.Open<KeepAliveTestView>();

            Assert.That(first.CreateCount, Is.EqualTo(1));
            Assert.That(first.BindCount, Is.EqualTo(1));
            Assert.That(first.OpenCount, Is.EqualTo(1));
            Assert.That(first.OpenedWithCommittedState, Is.True);
            Assert.That(m_UI.ViewCount, Is.EqualTo(1));
            Assert.That(m_UI.OpenViewCount, Is.EqualTo(1));
            Assert.That(m_UI.OpeningViewCount, Is.EqualTo(0));
            Assert.That(first.Component.gameObject.activeSelf, Is.True);

            var debugInfo = new List<UIViewDebugInfo>();
            m_UI.CopyViewDebugInfoTo(debugInfo);
            Assert.That(debugInfo, Has.Count.EqualTo(1));
            Assert.That(debugInfo[0].ViewType, Is.EqualTo(typeof(KeepAliveTestView)));
            Assert.That(debugInfo[0].Layer, Is.EqualTo(UILayer.Main));
            Assert.That(debugInfo[0].CacheMode, Is.EqualTo(UIViewCacheMode.KeepAlive));
            Assert.That(debugInfo[0].IsOpen, Is.True);

            Assert.That(m_UI.Close<KeepAliveTestView>(), Is.True);
            Assert.That(first.CloseCount, Is.EqualTo(1));
            Assert.That(first.ClosedWithCommittedState, Is.True);
            Assert.That(first.IsOpen, Is.False);
            Assert.That(m_UI.ViewCount, Is.EqualTo(1));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(0));
            Assert.That(first.Component.gameObject.activeSelf, Is.False);

            var reopened = m_UI.Open<KeepAliveTestView>();
            Assert.That(reopened, Is.SameAs(first));
            Assert.That(first.CreateCount, Is.EqualTo(1));
            Assert.That(first.BindCount, Is.EqualTo(1));
            Assert.That(first.OpenCount, Is.EqualTo(2));
            Assert.That(m_UI.OpenViewCount, Is.EqualTo(1));
            Assert.That(first.Component.gameObject.activeSelf, Is.True);

            Assert.That(m_UI.Release<KeepAliveTestView>(), Is.True);
            Assert.That(first.ReleaseCount, Is.EqualTo(1));
            Assert.That(first.UnbindCount, Is.EqualTo(1));
            Assert.That(first.CloseCount, Is.EqualTo(2));
            Assert.That(first.ReleasedWithCommittedState, Is.True);
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void KeepAliveLimit_EvictsLeastRecentlyUsedClosedView()
        {
            var first = m_UI.Open<KeepAliveTestView>();
            m_UI.Close<KeepAliveTestView>();
            var second = m_UI.Open<SecondKeepAliveTestView>();
            m_UI.Close<SecondKeepAliveTestView>();

            m_UI.KeepAliveLimit = 1;

            Assert.That(first.IsReleased, Is.True);
            Assert.That(m_UI.TryGet<KeepAliveTestView>(out _), Is.False);
            Assert.That(second.IsReleased, Is.False);
            Assert.That(m_UI.TryGet<SecondKeepAliveTestView>(out var cached), Is.True);
            Assert.That(cached, Is.SameAs(second));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void KeepAliveLimit_DoesNotEvictOpenView()
        {
            var first = m_UI.Open<KeepAliveTestView>();
            m_UI.Close<KeepAliveTestView>();
            var second = m_UI.Open<SecondKeepAliveTestView>();

            m_UI.KeepAliveLimit = 1;

            Assert.That(first.IsReleased, Is.False);
            Assert.That(second.IsOpen, Is.True);
            Assert.That(m_UI.TryGet<SecondKeepAliveTestView>(out var opened), Is.True);
            Assert.That(opened, Is.SameAs(second));
            Assert.That(m_Loader.ReleasedHandleCount, Is.Zero);
            m_UI.Close(second);
            Assert.That(first.IsReleased, Is.True);
            Assert.That(second.IsReleased, Is.False);
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseOpenView_ClosesBeforeRelease()
        {
            var view = m_UI.Open<KeepAliveTestView>();

            Assert.That(m_UI.Release<KeepAliveTestView>(), Is.True);
            Assert.That(view.CloseCount, Is.EqualTo(1));
            Assert.That(view.UnbindCount, Is.EqualTo(1));
            Assert.That(view.ReleaseCount, Is.EqualTo(1));
            Assert.That(view.IsReleased, Is.True);
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task OperationExtensions_WaitAtTheRequestedLifecycleBoundary()
        {
            var calls = new List<string>();
            var openGate = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var openTask = m_UI.OpenAsync<KeepAliveTestView>(
                (opened, ct) =>
                {
                    Assert.That(opened.IsOpen, Is.True);
                    calls.Add("open");
                    return openGate.Task;
                });

            Assert.That(openTask.IsCompleted, Is.False);
            openGate.SetResult(null);
            var view = await openTask;
            Assert.That(view.IsOpen, Is.True);

            var closeGate = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var closeTask = m_UI.CloseAsync<KeepAliveTestView>(
                (opened, ct) =>
                {
                    Assert.That(opened.IsOpen, Is.True);
                    calls.Add("close");
                    return closeGate.Task;
                });

            Assert.That(view.IsOpen, Is.True);
            Assert.That(closeTask.IsCompleted, Is.False);
            closeGate.SetResult(null);
            await closeTask;

            Assert.That(view.IsOpen, Is.False);
            Assert.That(calls, Is.EqualTo(new[] { "open", "close" }));
        }

        [Test]
        public async Task AsyncCloseAndRelease_AreSerializedPerView()
        {
            var view = m_UI.Open<KeepAliveTestView>();
            var started = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var gate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = new List<string>();

            var closeTask = m_UI.CloseAsync(
                view,
                async (closed, ct) =>
                {
                    Assert.That(closed, Is.SameAs(view));
                    calls.Add("close-start");
                    started.TrySetResult(true);
                    await gate.Task;
                    calls.Add("close-end");
                });

            await started.Task;
            var releaseTask = m_UI.ReleaseAsync(
                view,
                (released, ct) =>
                {
                    Assert.That(released, Is.SameAs(view));
                    calls.Add("release");
                    return Task.CompletedTask;
                });

            Assert.That(releaseTask.IsCompleted, Is.False);
            gate.SetResult(true);

            Assert.That(await closeTask, Is.True);
            Assert.That(await releaseTask, Is.True);
            Assert.That(calls, Is.EqualTo(new[] { "close-start", "close-end", "release" }));
            Assert.That(view.IsReleased, Is.True);
        }

        [Test]
        public async Task SynchronousRelease_IsRejectedWhileAsyncCloseIsWaiting()
        {
            var view = m_UI.Open<KeepAliveTestView>();
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var closeTask = m_UI.CloseAsync(
                view,
                async (opened, ct) =>
                {
                    started.SetResult(true);
                    await gate.Task;
                });

            await started.Task;
            Assert.That(
                () => m_UI.Release(view),
                Throws.TypeOf<InvalidOperationException>());

            gate.SetResult(true);
            Assert.That(await closeTask, Is.True);
            Assert.That(m_UI.Release(view), Is.True);
        }

        [Test]
        public async Task KeepAliveTrim_DoesNotReleaseViewWhileItsAsyncCloseCallbackIsRunning()
        {
            var first = m_UI.Open<KeepAliveTestView>();
            m_UI.Close(first);
            var second = m_UI.Open<SecondKeepAliveTestView>();
            m_UI.KeepAliveLimit = 1;

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var closeTask = m_UI.CloseAsync(
                second,
                async (view, ct) =>
                {
                    await gate.Task;
                    Assert.That(view.IsReleased, Is.False);
                });

            await Task.Yield();
            Assert.That(second.IsReleased, Is.False);
            gate.SetResult(true);
            Assert.That(await closeTask, Is.True);
            Assert.That(second.IsReleased, Is.False);
        }

        [Test]
        public void ModalLayer_UsesReferenceCountAndBlocksInput()
        {
            var modal = m_UI.ModalLayer;
            Assert.That(modal, Is.Not.Null);

            modal.Show();
            modal.Show();
            Assert.That(modal.IsVisible, Is.True);

            var overlayRoot = m_UI.ViewRoot.Find("UILayer-Overlay");
            Assert.That(overlayRoot, Is.Not.Null);
            var image = overlayRoot.Find(nameof(UIModalLayer)).GetComponent<Image>();
            Assert.That(image.raycastTarget, Is.True);

            modal.Hide();
            Assert.That(modal.IsVisible, Is.True);
            modal.Hide();
            Assert.That(modal.IsVisible, Is.False);
            Assert.That(image.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ModalLayer_RecreatesUnderNewViewRootAfterBeingHidden()
        {
            var modal = m_UI.ModalLayer;
            modal.Show();
            modal.Hide();

            var customRoot = new GameObject(
                "Replacement UI Root",
                typeof(RectTransform),
                typeof(Canvas));
            customRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                m_UI.SetViewRoot(customRoot.transform);
                modal.Show();

                var overlayRoot = customRoot.transform.Find("UILayer-Overlay");
                Assert.That(overlayRoot, Is.Not.Null);
                Assert.That(overlayRoot.Find(nameof(UIModalLayer)), Is.Not.Null);
                Assert.That(modal.IsVisible, Is.True);
            }
            finally
            {
                modal.HideAll();
                UnityEngine.Object.DestroyImmediate(customRoot);
            }
        }

        [Test]
        public void TypeInterface_ProvidesCoreLifecycle_AndGenericExtensionsNeedOnlyIUIManager()
        {
            IUIManager manager = m_UI;
            var view = manager.Open(typeof(KeepAliveTestView));

            Assert.That(view, Is.TypeOf<KeepAliveTestView>());
            Assert.That(manager.TryGetView(typeof(KeepAliveTestView), out var current), Is.True);
            Assert.That(current, Is.SameAs(view));
            Assert.That(manager.Close(typeof(KeepAliveTestView)), Is.True);

            // 泛型方法来自扩展层，接收的仍然是 IUIManager，而不是 UIModule。
            Assert.That(manager.Open<KeepAliveTestView>(), Is.SameAs(view));
            manager.ReleaseAll();
            Assert.That(manager.TryGetView(typeof(KeepAliveTestView), out _), Is.False);
        }

        [Test]
        public void TypeInterface_ProvidesExplicitPathWithLayerAndCachePolicy()
        {
            IUIManager manager = m_UI;
            var view = manager.Open(
                typeof(KeepAliveTestView),
                "Assets/UI/KeepAliveTestView.prefab",
                UILayer.Popup,
                UIViewCacheMode.DestroyOnClose);

            Assert.That(view, Is.TypeOf<KeepAliveTestView>());
            Assert.That(manager.Release(typeof(KeepAliveTestView)), Is.True);
        }

        [Test]
        public void ExplicitPath_IsNormalizedForCacheAndDiagnostics()
        {
            var first = m_UI.Open(
                typeof(KeepAliveTestView),
                " Assets\\UI\\KeepAliveTestView.prefab ",
                UILayer.Main,
                UIViewCacheMode.KeepAlive);
            var second = m_UI.Open(
                typeof(KeepAliveTestView),
                "Assets/UI/KeepAliveTestView.prefab",
                UILayer.Main,
                UIViewCacheMode.KeepAlive);

            Assert.That(second, Is.SameAs(first));
            var debugInfo = new List<UIViewDebugInfo>();
            m_UI.CopyViewDebugInfoTo(debugInfo);
            Assert.That(debugInfo[0].AssetPath, Is.EqualTo("Assets/UI/KeepAliveTestView.prefab"));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(0));
        }

        [Test]
        public void DifferentConfiguration_RequiresExplicitReleaseBeforeReopening()
        {
            var first = m_UI.Open<KeepAliveTestView>();

            Assert.Throws<InvalidOperationException>(() => m_UI.Open(
                typeof(KeepAliveTestView), "Assets/UI/KeepAliveTestView.prefab",
                UILayer.Popup, UIViewCacheMode.DestroyOnClose));
            Assert.That(first.IsReleased, Is.False);
            Assert.That(m_Loader.ReleasedHandleCount, Is.Zero);
            m_UI.Release(first);

            var second = m_UI.Open(
                typeof(KeepAliveTestView),
                "Assets/UI/KeepAliveTestView.prefab",
                UILayer.Popup,
                UIViewCacheMode.DestroyOnClose);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first.IsReleased, Is.True);
            Assert.That(second.Component.transform.parent.name, Is.EqualTo("UILayer-Popup"));

            m_UI.Release<KeepAliveTestView>();
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(2));
        }

        [Test]
        public void NewOpenMode_CreatesIndependentInstances_AndTypeOperationsRejectAmbiguity()
        {
            var first = m_UI.Open<KeepAliveTestView>(openMode: UIViewOpenMode.New);
            var second = m_UI.Open<KeepAliveTestView>(openMode: UIViewOpenMode.New);

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(m_UI.ViewCount, Is.EqualTo(2));
            Assert.That(m_UI.OpenViewCount, Is.EqualTo(2));
            Assert.That(() => m_UI.TryGet<KeepAliveTestView>(out _),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => m_UI.Close<KeepAliveTestView>(),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(m_UI.Close(first), Is.True);
            Assert.That(first.IsOpen, Is.False);
            Assert.That(second.IsOpen, Is.True);
            Assert.That(m_UI.Release(second), Is.True);
            Assert.That(m_UI.Release(first), Is.True);
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(2));
        }

        [Test]
        public async Task SharedOpen_CancelingOneWaiterKeepsSingleOwnedLoad()
        {
            var completion = new TaskCompletionSource<bool>();
            var loadCount = 0;
            m_Loader.BeforeLoadAsync = _ => { loadCount++; return completion.Task; };
            using var cancellation = new CancellationTokenSource();
            var first = m_UI.OpenAsync<KeepAliveTestView>(ct: cancellation.Token);
            var second = m_UI.OpenAsync<KeepAliveTestView>();
            var pendingCount = m_UI.OpeningViewCount;
            cancellation.Cancel();
            completion.SetResult(true);
            try { await first; Assert.Fail("Expected waiter cancellation."); }
            catch (OperationCanceledException) { }
            var view = await second;

            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(pendingCount, Is.EqualTo(1));
            Assert.That(m_UI.OpeningViewCount, Is.Zero);
            Assert.That(m_UI.TryGet<KeepAliveTestView>(out var cached), Is.True);
            Assert.That(cached, Is.SameAs(view));
            Assert.That(m_Loader.ReleasedHandleCount, Is.Zero);
            m_UI.Release(view);
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task IndependentOpens_CancellationReleasesOnlyItsOwnLateHandle()
        {
            var completion = new TaskCompletionSource<bool>();
            m_Loader.BeforeLoadAsync = _ => completion.Task;
            using var cancellation = new CancellationTokenSource();
            var first = m_UI.OpenAsync<KeepAliveTestView>(ct: cancellation.Token, openMode: UIViewOpenMode.New);
            var second = m_UI.OpenAsync<KeepAliveTestView>(openMode: UIViewOpenMode.New);
            var pendingCount = m_UI.OpeningViewCount;
            Exception conflict = null;
            try { _ = m_UI.OpenAsync(typeof(KeepAliveTestView)); }
            catch (Exception error) { conflict = error; }
            cancellation.Cancel();
            completion.SetResult(true);
            try { await first; Assert.Fail("Expected independent request cancellation."); }
            catch (OperationCanceledException) { }
            var view = await second;
            Assert.That(view.IsOpen, Is.True);
            await m_Modules.UninstallAsync<UIModule>();

            Assert.That(pendingCount, Is.EqualTo(2));
            Assert.That(conflict, Is.TypeOf<InvalidOperationException>());
            Assert.That(m_UI.OpeningViewCount, Is.Zero);
            Assert.That(view.IsReleased, Is.True);
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(2));
        }

        [Test]
        public async Task LoaderReentry_SeesRegisteredRequestAndCannotChangeRoot()
        {
            var completion = new TaskCompletionSource<bool>();
            Task<KeepAliveTestView> reentered = null;
            Exception rootFailure = null;
            var loadCount = 0;
            var pendingCount = 0;
            m_Loader.BeforeLoadAsync = _ =>
            {
                if (++loadCount == 1)
                {
                    pendingCount = m_UI.OpeningViewCount;
                    try { m_UI.SetViewRoot(null); }
                    catch (Exception error) { rootFailure = error; }
                    reentered = m_UI.OpenAsync<KeepAliveTestView>();
                }
                return completion.Task;
            };
            var opening = m_UI.OpenAsync<KeepAliveTestView>();
            completion.SetResult(true);
            var view = await opening;
            Assert.That(await reentered, Is.SameAs(view));
            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(pendingCount, Is.EqualTo(1));
            Assert.That(rootFailure, Is.TypeOf<InvalidOperationException>());
            Assert.That(m_UI.OpeningViewCount, Is.Zero);
        }

        [Test]
        public void NestedCreationFailure_DoesNotUnlockOuterCreationRoot()
        {
            CreationCallbackView.Callback = () =>
            {
                UnityEngine.Object.DestroyImmediate(m_ViewPrefab.GetComponent<UIViewComponent>());
                try
                {
                    Assert.Throws<InvalidOperationException>(() => m_UI.Open<SecondKeepAliveTestView>());
                    Assert.Throws<InvalidOperationException>(() => m_UI.SetViewRoot(null));
                }
                finally { m_ViewPrefab.AddComponent<UIViewComponent>(); }
            };
            try
            {
                var view = m_UI.Open<CreationCallbackView>();
                Assert.That(view.IsOpen, Is.True);
                Assert.That(m_UI.ViewRoot, Is.SameAs(m_ViewRoot.transform));
                Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
            }
            finally { CreationCallbackView.Callback = null; }
        }

        [Test]
        public void InvalidEntryCleanup_ReentrantCreationKeepsItsTypeRegistration()
        {
            var first = m_UI.Open<ReleaseCallbackView>();
            ReleaseCallbackView replacement = null;
            ReleaseCallbackView.Callback = () =>
            {
                ReleaseCallbackView.Callback = null;
                replacement = m_UI.Open<ReleaseCallbackView>(openMode: UIViewOpenMode.New);
            };
            try
            {
                // 模拟 Unity 节点销毁时未送达通知；查询必须仍回收资源。
                Game.ReflectionUtility.SetFieldValue(first.Component, "Destroyed", null);
                UnityEngine.Object.DestroyImmediate(first.Component);
                Assert.That(m_UI.TryGet<ReleaseCallbackView>(out var current), Is.True);
                Assert.That(current, Is.SameAs(replacement));
                Assert.That(first.IsReleased, Is.True);
                Assert.That(m_UI.ViewCount, Is.EqualTo(1));
                Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
            }
            finally { ReleaseCallbackView.Callback = null; }
        }

        [Test]
        public void GetViews_ReusesStronglyTypedListForMultipleAndCachedInstances()
        {
            var first = m_UI.Open<KeepAliveTestView>(openMode: UIViewOpenMode.New);
            var second = m_UI.Open<KeepAliveTestView>(openMode: UIViewOpenMode.New);
            var views = new List<KeepAliveTestView>(2);
            IUIManager manager = m_UI;

            manager.GetViews(views);
            Assert.That(views, Has.Count.EqualTo(2));
            Assert.That(views[0], Is.SameAs(first));
            Assert.That(views[1], Is.SameAs(second));

            m_UI.Close(first);
            manager.GetViews(views);
            Assert.That(views, Has.Count.EqualTo(2));
            Assert.That(views[0], Is.SameAs(first));
            Assert.That(first.IsOpen, Is.False);

            m_UI.Release(first);
            manager.GetViews(views);
            Assert.That(views, Has.Count.EqualTo(1));
            Assert.That(views[0], Is.SameAs(second));

            var untyped = new List<ViewBase>(2);
            manager.GetViews(typeof(KeepAliveTestView), untyped);
            Assert.That(untyped, Has.Count.EqualTo(1));
            Assert.That(untyped[0], Is.SameAs(second));
        }

        [Test]
        public void CoreOpenModeNew_CreatesIndependentInstance()
        {
            IUIManager manager = m_UI;
            var first = manager.Open(typeof(KeepAliveTestView), openMode: UIViewOpenMode.New);
            var second = manager.Open(typeof(KeepAliveTestView), openMode: UIViewOpenMode.New);

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(manager.Close(first), Is.True);
            Assert.That(manager.Release(second), Is.True);
            Assert.That(manager.Release(first), Is.True);
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(2));
        }

        [Test]
        public void ViewStack_TracksMultipleInstancesByReference()
        {
            var first = m_UI.Open<KeepAliveTestView>(openMode: UIViewOpenMode.New);
            var second = m_UI.Open<KeepAliveTestView>(openMode: UIViewOpenMode.New);

            Assert.That(m_UI.ViewStack.Count, Is.EqualTo(2));
            Assert.That(m_UI.ViewStack.TryPeek(out var top), Is.True);
            Assert.That(top, Is.SameAs(second));
            Assert.That(m_UI.ViewStack.Back(), Is.True);
            Assert.That(second.IsOpen, Is.False);
            Assert.That(m_UI.ViewStack.TryPeek(out top), Is.True);
            Assert.That(top, Is.SameAs(first));
            Assert.That(m_UI.ViewStack.Back(), Is.True);
            Assert.That(first.IsOpen, Is.False);
        }

        [Test]
        public void DestroyOnCloseView_ReleasesInstanceAndHandleOnClose()
        {
            var view = m_UI.Open<PopupTestView>();

            Assert.That(view.Component.transform.parent.name, Is.EqualTo("UILayer-Popup"));
            Assert.That(m_UI.Close<PopupTestView>(), Is.True);
            Assert.That(view.CloseCount, Is.EqualTo(1));
            Assert.That(view.ReleaseCount, Is.EqualTo(1));
            Assert.That(m_UI.TryGet<PopupTestView>(out _), Is.False);
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void DestroyingComponentExternally_RemovesViewAndReleasesHandle()
        {
            var view = m_UI.Open<KeepAliveTestView>();
            var instance = view.Component.gameObject;

            UnityEngine.Object.DestroyImmediate(instance);

            Assert.That(view.IsReleased, Is.True);
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyingOnlyViewComponent_ReleasesOwnedGameObject()
        {
            var view = m_UI.Open<KeepAliveTestView>();
            var instance = view.Component.gameObject;
            UnityEngine.Object.DestroyImmediate(view.Component);
            Assert.That(view.IsReleased, Is.True);
            Assert.That(m_UI.ViewCount, Is.Zero);
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
            yield return null;
            Assert.That(instance == null, Is.True);
        }

        [Test]
        public void DestroyedView_RejectsGeneratedVariableAccess()
        {
            var component = m_ViewPrefab.GetComponent<UIViewComponent>();
            SetVariable(component, "Root", m_ViewPrefab.transform);
            var view = m_UI.Open<GeneratedVariableTestView>();

            UnityEngine.Object.DestroyImmediate(view.Component.gameObject);

            Assert.That(view.IsReleased, Is.True);
            Assert.That(() => view.Root, Throws.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void DestroyingComponentFromOnClosed_DoesNotInvokeCloseTwice()
        {
            var view = m_UI.Open<DestroyOnCloseCallbackView>();

            Assert.That(m_UI.Close<DestroyOnCloseCallbackView>(), Is.True);
            Assert.That(view.CloseCount, Is.EqualTo(1));
            Assert.That(view.ReleaseCount, Is.EqualTo(1));
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void DestroyingComponentFromOnOpened_DoesNotPublishOpened()
        {
            Assert.That(
                () => m_UI.Open<DestroyOnOpenCallbackView>(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void DestroyOnClose_ReleasesResourcesWhenCloseCallbackFails()
        {
            var view = m_UI.Open<ThrowingCloseView>();

            Assert.That(
                () => m_UI.Close<ThrowingCloseView>(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(view.CloseCount, Is.EqualTo(1));
            Assert.That(view.ReleaseCount, Is.EqualTo(1));
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void ViewBaseIsPureCSharpAndViewComponentIsSealed()
        {
            Assert.That(typeof(ViewBase).IsSubclassOf(typeof(MonoBehaviour)), Is.False);
            Assert.That(typeof(UIViewComponent).IsSealed, Is.True);
        }

        [Test]
        public void ViewBaseRejectsDirectConstruction()
        {
            Assert.That(
                () => new DirectConstructionView(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains($"只能由 {nameof(UIModule)} 创建"));
        }

        [Test]
        public void ViewPartRejectsDirectConstruction()
        {
            Assert.That(
                () => new DirectConstructionViewPart(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("只能由 View 创建"));
        }

        [Test]
        public void WidgetBindsRepeatedlyAndReleasesOnce()
        {
            var gameObject = new GameObject("UI Widget");
            var component = gameObject.AddComponent<UIWidgetComponent>();
            try
            {
                var widget = WidgetBase.Create<TestWidget>(component);
                var first = new object();
                var second = new object();

                widget.SetData(first);
                widget.SetData(second);
                widget.ClearData();
                widget.ClearData();
                widget.Release();
                widget.Release();

                Assert.That(widget.CreateCount, Is.EqualTo(1));
                Assert.That(widget.SetDataCount, Is.EqualTo(2));
                Assert.That(widget.ClearDataCount, Is.EqualTo(1));
                Assert.That(widget.EventBindCount, Is.EqualTo(1));
                Assert.That(widget.EventUnbindCount, Is.EqualTo(1));
                Assert.That(widget.ReleaseCount, Is.EqualTo(1));
                Assert.That(widget.IsReleased, Is.True);
                Assert.That(widget.Component, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void WidgetRejectsDirectConstruction()
        {
            Assert.That(
                () => new DirectConstructionWidget(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("只能由 ViewBase 或 ViewPartBase 创建"));
        }

        [Test]
        public void DestroyingWidgetComponentReleasesWidget()
        {
            var gameObject = new GameObject("UI Widget");
            var component = gameObject.AddComponent<UIWidgetComponent>();
            var widget = WidgetBase.Create<TestWidget>(component);

            UnityEngine.Object.DestroyImmediate(gameObject);

            Assert.That(widget.IsReleased, Is.True);
            Assert.That(widget.ReleaseCount, Is.EqualTo(1));
            Assert.That(widget.Component, Is.Null);
        }

        [Test]
        public void GeneratedVariable_GetsInstantiatedPrefabObject()
        {
            var component = m_ViewPrefab.GetComponent<UIViewComponent>();
            SetVariable(component, "Root", m_ViewPrefab.transform);

            var view = m_UI.Open<GeneratedVariableTestView>();

            Assert.That(view.Root, Is.SameAs(view.Component.transform));
            Assert.That(view.Root, Is.Not.SameAs(m_ViewPrefab.transform));
        }

        [Test]
        public void OutdatedVariableSignature_RejectsOpenAndReleasesHandle()
        {
            var component = m_ViewPrefab.GetComponent<UIViewComponent>();
            SetVariable(component, "Root", m_ViewPrefab.transform);
            Game.ReflectionUtility.SetFieldValue(component, "m_VariableSignature", 123);

            Assert.That(
                () => m_UI.Open<GeneratedVariableTestView>(),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("不一致"));
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void MissingSerializedVariables_ReportsRegenerationErrorAndReleasesResources()
        {
            var component = m_ViewPrefab.GetComponent<UIViewComponent>();
            Game.ReflectionUtility.SetFieldValue(component, "m_Variables", null);

            Assert.That(
                () => m_UI.Open<GeneratedVariableTestView>(),
                Throws.TypeOf<IndexOutOfRangeException>().With.Message.Contains("重新生成变量代码"));
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void ThrowingOnCreated_ReleasesViewAndHandle()
        {
            Assert.That(
                () => m_UI.Open<ThrowingCreateView>(),
                Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("create failure"));

            Assert.That(ThrowingCreateView.LastInstance, Is.Not.Null);
            Assert.That(ThrowingCreateView.LastInstance.ReleaseCount, Is.EqualTo(1));
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void ThrowingOnBind_UnbindsBeforeRelease()
        {
            Assert.That(
                () => m_UI.Open<ThrowingBindView>(),
                Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("bind failure"));

            Assert.That(ThrowingBindView.LastInstance, Is.Not.Null);
            Assert.That(ThrowingBindView.LastInstance.UnbindCount, Is.EqualTo(1));
            Assert.That(ThrowingBindView.LastInstance.ReleaseCount, Is.EqualTo(1));
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
        }

        [Test]
        public void Layers_StayOrderedWhenCreatedOutOfOrder_AndBulkOperationsRespectLayer()
        {
            var popup = m_UI.Open<PopupTestView>();
            var main = m_UI.Open<KeepAliveTestView>();

            Assert.That(main.Component.transform.parent.parent, Is.SameAs(m_ViewRoot.transform));
            Assert.That(popup.Component.transform.parent.parent, Is.SameAs(m_ViewRoot.transform));
            Assert.That(main.Component.transform.parent.GetSiblingIndex(), Is.LessThan(popup.Component.transform.parent.GetSiblingIndex()));

            m_UI.CloseAll(UILayer.Popup);
            Assert.That(main.IsOpen, Is.True);
            Assert.That(m_UI.TryGet<PopupTestView>(out _), Is.False);

            m_UI.ReleaseAll(UILayer.Main);
            Assert.That(m_UI.ViewCount, Is.EqualTo(0));
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(2));
        }

        [Test]
        public void ControllerCallbacks_ObserveCommittedBindingState()
        {
            var view = m_UI.Open<KeepAliveTestView>();
            var model = new object();
            var controller = new TrackingController();

            controller.Bind(view, model);
            Assert.That(controller.BoundWithCommittedState, Is.True);

            controller.Unbind();
            Assert.That(controller.UnboundWithCommittedState, Is.True);
        }

        [Test]
        public void ViewStack_BackClosesTheMostRecentlyOpenedView()
        {
            var first = m_UI.Open<KeepAliveTestView>();
            var second = m_UI.Open<PopupTestView>();

            Assert.That(m_UI.ViewStack.Count, Is.EqualTo(2));
            Assert.That(m_UI.ViewStack.TryPeek(out var top), Is.True);
            Assert.That(top, Is.SameAs(second));

            Assert.That(m_UI.ViewStack.Back(), Is.True);
            Assert.That(second.IsReleased, Is.True);
            Assert.That(first.IsOpen, Is.True);
            Assert.That(m_UI.ViewStack.TryPeek(out top), Is.True);
            Assert.That(top, Is.SameAs(first));

            Assert.That(m_UI.ViewStack.Back(), Is.True);
            Assert.That(first.IsOpen, Is.False);
            Assert.That(m_UI.ViewStack.Count, Is.EqualTo(0));
        }

        [Test]
        public void ViewStack_BecomesInactiveAfterModuleUninstall()
        {
            var stack = m_UI.ViewStack;
            m_UI.Open<KeepAliveTestView>();

            Assert.That(m_Modules.Uninstall<UIModule>(), Is.True);
            Assert.That(stack.Count, Is.EqualTo(0));
            Assert.That(stack.TryPeek(out _), Is.False);
            Assert.That(stack.Back(), Is.False);
            stack.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Uninstall_CancellationFailureStillWaitsForLoadsAndReleasesViews(bool completeDuringCancellation)
        {
            var view = m_UI.Open<KeepAliveTestView>();
            var completion = new TaskCompletionSource<bool>();
            var cancellationFailure = new InvalidOperationException("cancel failure");
            var loadFailure = new InvalidOperationException("load failure");
            m_Loader.BeforeLoadAsync = async ct =>
            {
                using var registration = ct.Register(() =>
                {
                    if (completeDuringCancellation) completion.SetException(loadFailure);
                    throw cancellationFailure;
                });
                await completion.Task;
            };

            var opening = m_UI.OpenAsync<SecondKeepAliveTestView>();
            var uninstall = m_Modules.UninstallAsync<UIModule>().AsTask();
            var waitedForLoad = !uninstall.IsCompleted;
            if (!completeDuringCancellation) completion.SetException(loadFailure);
            Exception uninstallFailure = null;
            try { await uninstall; }
            catch (Exception error) { uninstallFailure = error; }
            try { await opening; Assert.Fail("Expected load failure."); }
            catch (InvalidOperationException error) { Assert.That(error, Is.SameAs(loadFailure)); }

            Assert.That(uninstallFailure, Is.Not.Null);
            Assert.That(uninstallFailure.ToString(), Does.Contain("cancel failure").And.Contain("load failure"));
            if (!completeDuringCancellation) Assert.That(waitedForLoad, Is.True);
            Assert.That(view.IsReleased, Is.True);
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
            Assert.That(m_UI.IsDisposed, Is.True);
        }

        [Test]
        public async Task Uninstall_LateHandleReleaseFailurePreservesCancellation()
        {
            var completion = new TaskCompletionSource<bool>();
            var releaseFailure = new InvalidOperationException("handle release failure");
            m_Loader.BeforeLoadAsync = _ => completion.Task;
            m_Loader.OnHandleReleased = () => throw releaseFailure;
            var opening = m_UI.OpenAsync<KeepAliveTestView>();
            var uninstall = m_Modules.UninstallAsync<UIModule>().AsTask();
            completion.SetResult(true);

            Exception openingFailure = null;
            try { await opening; }
            catch (Exception error) { openingFailure = error; }
            Exception uninstallFailure = null;
            try { await uninstall; }
            catch (Exception error) { uninstallFailure = error; }

            Assert.That(openingFailure, Is.TypeOf<AggregateException>());
            var errors = ((AggregateException)openingFailure).Flatten().InnerExceptions;
            Assert.That(errors.Count, Is.EqualTo(2));
            Assert.That(errors[0], Is.InstanceOf<OperationCanceledException>());
            Assert.That(errors[1], Is.SameAs(releaseFailure));
            Assert.That(uninstallFailure, Is.Not.Null);
            Assert.That(m_Loader.ReleasedHandleCount, Is.EqualTo(1));
            Assert.That(m_UI.IsDisposed, Is.True);
        }

        [Test]
        public void ViewPart_IsReleasedWithItsView()
        {
            var view = m_UI.Open<ViewPartTestView>();

            Assert.That(view.ViewPart, Is.Not.Null);
            Assert.That(view.ViewPart.IsCreated, Is.True);
            Assert.That(view.ViewPart.CreatedWithCommittedState, Is.True);
            Assert.That(view.ViewPart.Owner, Is.SameAs(view));
            Assert.That(view.ViewPartAgain, Is.SameAs(view.ViewPart));

            Assert.That(m_UI.Release<ViewPartTestView>(), Is.True);
            Assert.That(view.ViewPart.ReleaseCount, Is.EqualTo(1));
            Assert.That(view.ViewPart.IsReleased, Is.True);
            Assert.That(view.ViewPart.ReleasedWithCommittedState, Is.True);
        }

        [Test]
        public void ViewOwnedWidget_IsReleasedWithItsView()
        {
            var view = m_UI.Open<WidgetOwnedView>();

            Assert.That(view.Widget, Is.Not.Null);
            Assert.That(view.WidgetAgain, Is.SameAs(view.Widget));
            Assert.That(view.Widget.IsCreated, Is.True);
            Assert.That(view.Widget.Component, Is.Not.Null);

            Assert.That(m_UI.Release<WidgetOwnedView>(), Is.True);
            Assert.That(view.Widget.ReleaseCount, Is.EqualTo(1));
            Assert.That(view.Widget.IsReleased, Is.True);
            Assert.That(view.Widget.Component, Is.Null);
        }

        [Test]
        public void ClosingKeepAliveView_KeepsItsWidgetsUntilRelease()
        {
            var view = m_UI.Open<WidgetOwnedView>();
            var widget = view.Widget;

            Assert.That(m_UI.Close<WidgetOwnedView>(), Is.True);
            Assert.That(widget.IsReleased, Is.False);
            Assert.That(widget.Component, Is.Not.Null);

            Assert.That(m_UI.Release<WidgetOwnedView>(), Is.True);
            Assert.That(widget.IsReleased, Is.True);
            Assert.That(widget.Component, Is.Null);
        }

        [Test]
        public void ViewPartOwnedWidget_IsReleasedWithItsView()
        {
            var view = m_UI.Open<WidgetViewPartTestView>();

            Assert.That(view.Part, Is.Not.Null);
            Assert.That(view.Part.Widget, Is.Not.Null);
            Assert.That(view.Part.WidgetAgain, Is.SameAs(view.Part.Widget));
            Assert.That(view.Part.Widget.IsCreated, Is.True);

            Assert.That(m_UI.Release<WidgetViewPartTestView>(), Is.True);
            Assert.That(view.Part.Widget.ReleaseCount, Is.EqualTo(1));
            Assert.That(view.Part.Widget.IsReleased, Is.True);
            Assert.That(view.Part.Widget.Component, Is.Null);
        }

        [Test]
        public void CloseRequestedDuringOnOpened_IsAppliedAfterOpenNotification()
        {
            ClosingDuringOpenView.CloseRequest = () => m_UI.Close<ClosingDuringOpenView>();
            var view = m_UI.Open<ClosingDuringOpenView>();

            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.Component, Is.Not.Null);
            Assert.That(view.Component.gameObject.activeSelf, Is.False);
            Assert.That(view.CloseCount, Is.EqualTo(1));
            Assert.That(m_UI.ViewStack.Count, Is.EqualTo(0));
        }

        [ViewConfig("Assets/UI/KeepAliveTestView.prefab")]
        private sealed class IdView : ViewBase<IdArgs> { }

        private sealed class IdArgs : ViewArgs
        {
            public int Id { get; }
            public IdArgs(int id) => Id = id;
        }

        [ViewConfig("Assets/UI/ReferenceDataView.prefab")]
        private sealed class ReferenceDataView : ViewBase<OpenPayload>
        {
            public int OpenCount;
            public bool OpenedWithCommittedData;
            public bool ClosedWithClearedData;
            public bool ThrowOnOpen;
            public bool ThrowOnClose;

            protected override void OnOpened(OpenPayload data)
            {
                OpenCount++;
                OpenedWithCommittedData = IsOpen && ReferenceEquals(OpenArgs, data);
                if (ThrowOnOpen) throw new InvalidOperationException("open failed");
            }

            protected override void OnClosed()
            {
                ClosedWithClearedData = !IsOpen && OpenArgs == null;
                if (ThrowOnClose) throw new InvalidOperationException("close failed");
            }
        }

        private sealed class OpenPayload : ViewArgs, IDisposable
        {
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        private sealed class TypedController : ControllerBase<ReferenceDataView, object>
        {
            public OpenPayload OpenArgsSeen;
            protected override void OnOpened() => OpenArgsSeen = View.OpenArgs;
            protected override void OnClosed() => OpenArgsSeen = null;
        }

        [ViewConfig("Assets/UI/KeepAliveTestView.prefab")]
        private sealed class KeepAliveTestView : ViewBase
        {
            public int CreateCount { get; private set; }
            public int OpenCount { get; private set; }
            public int CloseCount { get; private set; }
            public int BindCount { get; private set; }
            public int UnbindCount { get; private set; }
            public int ReleaseCount { get; private set; }
            public bool OpenedWithCommittedState { get; private set; }
            public bool ClosedWithCommittedState { get; private set; }
            public bool ReleasedWithCommittedState { get; private set; }

            protected override void OnCreated() => CreateCount++;
            protected override void OnBindEvents() => BindCount++;
            protected override void OnOpened()
            {
                OpenCount++;
                OpenedWithCommittedState = IsOpen;
            }

            protected override void OnClosed()
            {
                CloseCount++;
                ClosedWithCommittedState = !IsOpen;
            }

            protected override void OnUnbindEvents() => UnbindCount++;
            protected override void OnReleased()
            {
                ReleaseCount++;
                ReleasedWithCommittedState = IsReleased;
            }
        }

        [ViewConfig("Assets/UI/KeepAliveTestView.prefab")]
        private sealed class SecondKeepAliveTestView : ViewBase
        {
        }

        [ViewConfig("Assets/UI/CreationCallbackView.prefab")]
        private sealed class CreationCallbackView : ViewBase
        {
            internal static Action Callback;
            protected override void OnCreated() => Callback?.Invoke();
        }

        [ViewConfig("Assets/UI/ReleaseCallbackView.prefab")]
        private sealed class ReleaseCallbackView : ViewBase
        {
            internal static Action Callback;
            protected override void OnReleased() => Callback?.Invoke();
        }

        private sealed class DirectConstructionView : ViewBase
        {
        }

        private sealed class DirectConstructionViewPart : ViewPartBase<Transform>
        {
        }

        private sealed class DirectConstructionWidget : WidgetBase<object>
        {
        }

        private sealed class TestWidget : WidgetBase<object>
        {
            public int CreateCount { get; private set; }
            public int SetDataCount { get; private set; }
            public int ClearDataCount { get; private set; }
            public int EventBindCount { get; private set; }
            public int EventUnbindCount { get; private set; }
            public int ReleaseCount { get; private set; }

            protected override void OnCreated() => CreateCount++;
            protected override void OnBindEvents() => EventBindCount++;
            protected override void OnDataSet(object data) => SetDataCount++;
            protected override void OnDataCleared() => ClearDataCount++;
            protected override void OnUnbindEvents() => EventUnbindCount++;
            protected override void OnReleased() => ReleaseCount++;
        }

        private sealed class TrackingController : ControllerBase<KeepAliveTestView, object>
        {
            public bool BoundWithCommittedState { get; private set; }
            public bool UnboundWithCommittedState { get; private set; }

            protected override void OnBound(KeepAliveTestView view, object model)
            {
                BoundWithCommittedState = ReferenceEquals(View, view) && ReferenceEquals(Model, model);
            }

            protected override void OnUnbound(KeepAliveTestView view, object model)
            {
                UnboundWithCommittedState = View == null && Model == null && view != null && model != null;
            }
        }

        [ViewConfig("Assets/UI/ThrowingCreateView.prefab")]
        private sealed class ThrowingCreateView : ViewBase
        {
            internal static ThrowingCreateView LastInstance { get; private set; }
            public int ReleaseCount { get; private set; }

            protected override void OnCreated()
            {
                LastInstance = this;
                throw new InvalidOperationException("create failure");
            }

            protected override void OnReleased() => ReleaseCount++;
        }

        [ViewConfig("Assets/UI/ThrowingBindView.prefab")]
        private sealed class ThrowingBindView : ViewBase
        {
            internal static ThrowingBindView LastInstance { get; private set; }
            public int UnbindCount { get; private set; }
            public int ReleaseCount { get; private set; }

            protected override void OnCreated() => LastInstance = this;

            protected override void OnBindEvents() => throw new InvalidOperationException("bind failure");

            protected override void OnUnbindEvents() => UnbindCount++;

            protected override void OnReleased() => ReleaseCount++;
        }

        [ViewConfig("Assets/UI/GeneratedVariableTestView.prefab")]
        private sealed class GeneratedVariableTestView : ViewBase
        {
            public Transform Root => GetVariable<Transform>(0);
            protected override void OnCreated() => _ = Root;
        }

        [ViewConfig("Assets/UI/ViewPartTestView.prefab")]
        private sealed class ViewPartTestView : ViewBase
        {
            public TrackingViewPart ViewPart { get; private set; }
            public TrackingViewPart ViewPartAgain { get; private set; }

            protected override void OnCreated()
            {
                ViewPart = CreateViewPart<TrackingViewPart>(Component.transform);
                ViewPartAgain = CreateViewPart<TrackingViewPart>(Component.transform);
            }
        }

        [ViewConfig("Assets/UI/WidgetOwnedView.prefab")]
        private sealed class WidgetOwnedView : ViewBase
        {
            public TestWidget Widget { get; private set; }
            public TestWidget WidgetAgain { get; private set; }

            protected override void OnCreated()
            {
                var node = new GameObject("Widget");
                node.transform.SetParent(Component.transform, false);
                var component = node.AddComponent<UIWidgetComponent>();
                Widget = CreateWidget<TestWidget>(component);
                WidgetAgain = CreateWidget<TestWidget>(component);
            }
        }

        [ViewConfig("Assets/UI/WidgetViewPartTestView.prefab")]
        private sealed class WidgetViewPartTestView : ViewBase
        {
            public WidgetViewPart Part { get; private set; }

            protected override void OnCreated()
            {
                Part = CreateViewPart<WidgetViewPart>(Component.transform);
            }
        }

        private sealed class WidgetViewPart : ViewPartBase<Transform>
        {
            public TestWidget Widget { get; private set; }
            public TestWidget WidgetAgain { get; private set; }

            protected override void OnCreated()
            {
                var node = new GameObject("Widget");
                node.transform.SetParent(Owner.Component.transform, false);
                var component = node.AddComponent<UIWidgetComponent>();
                Widget = CreateWidget<TestWidget>(component);
                WidgetAgain = CreateWidget<TestWidget>(component);
            }
        }

        private sealed class TrackingViewPart : ViewPartBase<Transform>
        {
            public int ReleaseCount { get; private set; }
            public bool CreatedWithCommittedState { get; private set; }
            public bool ReleasedWithCommittedState { get; private set; }

            protected override void OnCreated()
            {
                CreatedWithCommittedState = IsCreated && Owner != null && GameObject != null;
            }

            protected override void OnReleased()
            {
                ReleaseCount++;
                ReleasedWithCommittedState = IsReleased;
            }
        }

        [ViewConfig("Assets/UI/ClosingDuringOpenView.prefab")]
        private sealed class ClosingDuringOpenView : ViewBase
        {
            internal static Action CloseRequest { get; set; }
            public int CloseCount { get; private set; }

            protected override void OnOpened() => CloseRequest?.Invoke();
            protected override void OnClosed() => CloseCount++;
        }

        [ViewConfig(
            "Assets/UI/PopupTestView.prefab",
            UILayer.Popup,
            CacheMode = UIViewCacheMode.DestroyOnClose)]
        private sealed class PopupTestView : ViewBase
        {
            public int CloseCount { get; private set; }
            public int ReleaseCount { get; private set; }

            protected override void OnClosed() => CloseCount++;
            protected override void OnReleased() => ReleaseCount++;
        }

        [ViewConfig("Assets/UI/DestroyOnCloseCallbackView.prefab")]
        private sealed class DestroyOnCloseCallbackView : ViewBase
        {
            public int CloseCount { get; private set; }
            public int ReleaseCount { get; private set; }

            protected override void OnClosed()
            {
                CloseCount++;
                UnityEngine.Object.DestroyImmediate(Component.gameObject);
            }

            protected override void OnReleased() => ReleaseCount++;
        }

        [ViewConfig("Assets/UI/DestroyOnOpenCallbackView.prefab")]
        private sealed class DestroyOnOpenCallbackView : ViewBase
        {
            protected override void OnOpened()
            {
                UnityEngine.Object.DestroyImmediate(Component.gameObject);
            }
        }

        [ViewConfig(
            "Assets/UI/ThrowingCloseView.prefab",
            CacheMode = UIViewCacheMode.DestroyOnClose)]
        private sealed class ThrowingCloseView : ViewBase
        {
            public int CloseCount { get; private set; }
            public int ReleaseCount { get; private set; }

            protected override void OnClosed()
            {
                CloseCount++;
                throw new InvalidOperationException("close failure");
            }

            protected override void OnReleased() => ReleaseCount++;
        }

        private sealed class TestLoaderModule : GameModule, ILoader
        {
            private readonly GameObject m_Prefab;

            public int ReleasedHandleCount { get; private set; }

            public Func<CancellationToken, Task> BeforeLoadAsync { get; set; }
            public Action OnHandleReleased { get; set; }

            public TestLoaderModule(GameObject prefab)
            {
                m_Prefab = prefab;
            }

            public int LoadCount { get; private set; }

            public AssetLoadHandle<TObject> LoadAsset<TObject>(string assetPath)
                where TObject : UnityEngine.Object
            {
                LoadCount++;
                return new AssetLoadHandle<TObject>(m_Prefab as TObject, ReleaseHandle);
            }

            public async Task<AssetLoadHandle<TObject>> LoadAssetAsync<TObject>(string assetPath, CancellationToken ct = default)
                where TObject : UnityEngine.Object
            {
                ct.ThrowIfCancellationRequested();
                if (BeforeLoadAsync != null) await BeforeLoadAsync(ct);
                return LoadAsset<TObject>(assetPath);
            }

            public Task<SceneLoadHandle> LoadSceneAsync(
                string scenePath,
                bool allowSceneActivation = true,
                LoadSceneParameters parameters = default,
                Action<float> onProgress = null)
            {
                return Task.FromException<SceneLoadHandle>(new NotSupportedException());
            }

            public Task<SceneLoadHandle> UnloadSceneAsync(
                string scenePath,
                UnloadSceneOptions options = UnloadSceneOptions.None,
                Action<float> onProgress = null)
            {
                return Task.FromException<SceneLoadHandle>(new NotSupportedException());
            }

            private void ReleaseHandle()
            {
                ReleasedHandleCount++;
                OnHandleReleased?.Invoke();
            }
        }

        private static void SetVariable(UIViewComponent component, string name, Component value)
        {
            var componentType = typeof(UIViewComponent);
            var variableType = componentType.GetNestedType("ViewVariable", BindingFlags.NonPublic);
            Assert.That(variableType, Is.Not.Null);
            var variable = Activator.CreateInstance(variableType);
            Game.ReflectionUtility.SetFieldValue(variable, "m_Name", name);
            Game.ReflectionUtility.SetFieldValue(variable, "m_Value", value);

            var variables = Array.CreateInstance(variableType, 1);
            variables.SetValue(variable, 0);
            Game.ReflectionUtility.SetFieldValue(component, "m_Variables", variables);
        }
    }
}
