namespace Verve.Tests.Loader
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [Category("Loader")]
    internal class LoaderTests
    {
        [Test]
        public void PathUtility_NormalizesProjectAndScenePaths()
        {
            Assert.That(Game.PathUtility.NormalizeProjectPath(" assets\\UI\\Panel.prefab ", "path"),
                Is.EqualTo("Assets/UI/Panel.prefab"));
            Assert.That(Game.PathUtility.NormalizeProjectPath("packages/com.verve/data.asset", "path"),
                Is.EqualTo("Packages/com.verve/data.asset"));
            Assert.That(Game.PathUtility.NormalizeScenePath("Assets/Scenes/Main.UNITY", "path"),
                Is.EqualTo("Assets/Scenes/Main.UNITY"));
            Assert.Throws<ArgumentException>(() => Game.PathUtility.NormalizeProjectPath("ui/panel", "path"));
            Assert.Throws<ArgumentException>(() => Game.PathUtility.NormalizeScenePath("Assets/UI/Panel.prefab", "path"));
        }

        [Test]
        public void LoaderModule_UsesOneConfigurableLoadingMode()
        {
            var loader = new LoaderModule();
            Assert.That(loader.LoaderMode, Is.EqualTo(AssetLoaderMode.Addressables));
            Assert.That(loader.LoaderMode == AssetLoaderMode.Addressables, Is.True);
            Assert.That(loader.LoaderMode == AssetLoaderMode.AssetBundles, Is.False);

            loader.LoaderMode = AssetLoaderMode.AssetBundles;
            loader.BundleDirectory = "Bundles";
            Assert.That(loader.LoaderMode == AssetLoaderMode.Addressables, Is.False);
            Assert.That(loader.LoaderMode == AssetLoaderMode.AssetBundles, Is.True);
            Assert.That(loader.BundleDirectory, Is.EqualTo("Bundles"));
            Assert.Throws<ArgumentOutOfRangeException>(() => loader.LoaderMode = (AssetLoaderMode)255);

            var assetBundleLoader = new LoaderModule
            {
                LoaderMode = AssetLoaderMode.AssetBundles,
                BundleDirectory = "CustomBundles",
            };
            Assert.That(assetBundleLoader.LoaderMode, Is.EqualTo(AssetLoaderMode.AssetBundles));
            Assert.That(assetBundleLoader.BundleDirectory, Is.EqualTo("CustomBundles"));
        }

        [Test]
        public void HandlesAndScopes_ReleaseExactlyOnceInReverseTrackingOrder()
        {
            var released = new List<string>();
            var first = new AssetLoadHandle<ScriptableObject>(releaseAction: () => released.Add("first"));
            var second = new AssetLoadHandle<ScriptableObject>(releaseAction: () => released.Add("second"));
            using var scope = new AssetLoadScope();

            scope.Track(first);
            scope.Track(second);
            scope.ReleaseAll();
            first.Dispose();
            second.Dispose();

            CollectionAssert.AreEqual(new[] { "second", "first" }, released);
            Assert.That(scope.Count, Is.EqualTo(0));
            Assert.That(first.IsReleased, Is.True);
            Assert.That(second.IsReleased, Is.True);
        }

        [Test]
        public void ScopeTrack_TransfersExclusiveReleaseOwnership()
        {
            var releases = 0;
            var handle = new AssetLoadHandle<ScriptableObject>(releaseAction: () => releases++);
            using var scope = new AssetLoadScope();
            using var other = new AssetLoadScope();
            scope.Track(handle);
            handle.Dispose();
            Assert.That(releases, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => other.Track(handle));
            Assert.Throws<ObjectDisposedException>(() => _ = handle.Result);
            scope.Dispose();
            Assert.That(releases, Is.EqualTo(1));
        }

        [Test]
        public async Task BoundLoad_OwnerDestructionAutomaticallyReleasesResource()
        {
            var owner = new GameObject("load owner");
            var loader = new PendingLoader();
            var releases = 0;
            loader.Completion.SetResult(new AssetLoadHandle<ScriptableObject>(releaseAction: () => releases++));
            try
            {
                await loader.LoadAssetAsync<ScriptableObject>("Assets/Test.asset", owner);
                Assert.That(releases, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
            Assert.That(releases, Is.EqualTo(1));
        }

        [Test]
        public async Task OwnerDestroyedDuringLoad_ReleasesCompletedHandle()
        {
            var owner = new GameObject("load owner");
            var loader = new PendingLoader();
            var operation = loader.LoadAssetAsync<ScriptableObject>("Assets/Test.asset", owner);
            UnityEngine.Object.DestroyImmediate(owner);
            var releases = 0;
            loader.Completion.SetResult(new AssetLoadHandle<ScriptableObject>(releaseAction: () => releases++));
            try { await operation; Assert.Fail("Expected destroyed owner rejection."); }
            catch (ArgumentNullException) { }
            Assert.That(releases, Is.EqualTo(1));
        }

        [Test]
        public async Task CancelledScopedLoad_ReleasesHandleEvenWhenProviderCompletes()
        {
            var loader = new PendingLoader();
            using var scope = new AssetLoadScope();
            using var cancellation = new CancellationTokenSource();
            var operation = loader.LoadAssetAsync<ScriptableObject>("Assets/Test.asset", scope, cancellation.Token);
            cancellation.Cancel();
            var releases = 0;
            loader.Completion.SetResult(new AssetLoadHandle<ScriptableObject>(releaseAction: () => releases++));
            try { await operation; Assert.Fail("Expected cancellation."); }
            catch (OperationCanceledException) { }
            Assert.That(releases, Is.EqualTo(1));
            Assert.That(scope.Count, Is.Zero);
        }

        [Test]
        public void ScopeRelease_AllowsReentrancyAndPreservesOriginalFailure()
        {
            using var scope = new AssetLoadScope();
            var released = 0;
            var failure = new InvalidOperationException("release failed");
            scope.Track(new AssetLoadHandle<ScriptableObject>(releaseAction: () =>
            {
                released++;
                throw failure;
            }));
            scope.Track(new AssetLoadHandle<ScriptableObject>(releaseAction: () =>
            {
                released++;
                scope.ReleaseAll();
            }));
            Assert.That(Assert.Throws<InvalidOperationException>(() => scope.ReleaseAll()), Is.SameAs(failure));
            Assert.That(released, Is.EqualTo(2));
            Assert.That(scope.Count, Is.Zero);
            scope.ReleaseAll();
        }

        [Test]
        public void ReferenceCountsAndSceneHandles_EnforceReferenceAndActivationContracts()
        {
            var reference = new TestLoadReference();
            reference.Retain();
            Assert.That(reference.RefCount, Is.EqualTo(2));
            Assert.That(reference.Release(), Is.EqualTo(1));
            Assert.That(reference.Release(), Is.EqualTo(0));
            Assert.Throws<InvalidOperationException>(() => reference.Release());

            var activations = 0;
            var sceneHandle = new SceneLoadHandle(default(Scene), () => activations++);
            sceneHandle.Activate();
            Assert.That(activations, Is.EqualTo(1));
        }

        private sealed class TestLoadReference : LoadReference { }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LifecycleCleanup_CancellationFailureStillReleasesEveryReference(bool uninstall)
        {
            var asset = new GameObject("loader reference");
            var loader = new LoaderModule { LoaderMode = AssetLoaderMode.AssetBundles };
            using var cancellation = new CancellationTokenSource();
            var cancellationFailure = new InvalidOperationException("cancel failure");
            var releaseFailure = new InvalidOperationException("release failure");
            using var registration = cancellation.Token.Register(() => throw cancellationFailure);
            var references = GetReferences(loader);
            var first = new FailingAssetReference(asset, releaseFailure);
            var second = new FailingAssetReference(asset);
            references.Add("Assets/First.prefab", first);
            references.Add("Assets/Second.prefab", second);
            Game.ReflectionUtility.SetFieldValue(loader, "m_LifecycleCancellation", cancellation);
            try
            {
                Exception failure = null;
                try
                {
                    if (uninstall)
                    {
                        var operation = (ValueTask)typeof(LoaderModule)
                            .GetMethod("OnUninstall", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(loader, new object[] { null, CancellationToken.None });
                        await operation;
                    }
                    else loader.Dispose();
                }
                catch (Exception error) { failure = error; }

                Assert.That(failure, Is.TypeOf<AggregateException>());
                CollectionAssert.AreEquivalent(new[] { cancellationFailure, releaseFailure },
                    ((AggregateException)failure).Flatten().InnerExceptions);
                Assert.That(first.ReleaseCount, Is.EqualTo(1));
                Assert.That(second.ReleaseCount, Is.EqualTo(1));
                Assert.That(references.Count, Is.Zero);
            }
            finally
            {
                loader.Dispose();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CancelledLoad_PreservesBothCancellationAndReleaseFailure()
        {
            var asset = new GameObject("cancelled asset");
            using var loader = new LoaderModule { LoaderMode = AssetLoaderMode.AssetBundles };
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var releaseFailure = new InvalidOperationException("release failure");
            var reference = new FailingAssetReference(asset, releaseFailure);
            try
            {
                var failure = Assert.Throws<TargetInvocationException>(() =>
                    CompleteAssetLoad(loader, reference, cancellation.Token));
                Assert.That(failure.InnerException, Is.TypeOf<AggregateException>());
                var errors = ((AggregateException)failure.InnerException).Flatten().InnerExceptions;
                Assert.That(errors.Count, Is.EqualTo(2));
                Assert.That(errors[0], Is.InstanceOf<OperationCanceledException>());
                Assert.That(errors[1], Is.SameAs(releaseFailure));
                Assert.That(reference.ReleaseCount, Is.EqualTo(1));
                Assert.That(GetReferences(loader).Count, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void DuplicateLoad_ReleaseFailureRollsBackRetainedCacheReference()
        {
            var asset = new GameObject("cached asset");
            using var loader = new LoaderModule { LoaderMode = AssetLoaderMode.AssetBundles };
            var state = Game.ReflectionUtility.FindField(typeof(GameModule), "m_State");
            var states = typeof(GameModule).GetNestedType("ModuleState", BindingFlags.NonPublic);
            var cached = new FailingAssetReference(asset);
            var releaseFailure = new InvalidOperationException("duplicate release failure");
            var duplicate = new FailingAssetReference(asset, releaseFailure);
            GetReferences(loader).Add("Assets/Test.prefab", cached);
            state.SetValue(loader, Convert.ToInt32(Enum.Parse(states, "Installed")));
            try
            {
                var failure = Assert.Throws<TargetInvocationException>(() => CompleteAssetLoad(loader, duplicate));
                Assert.That(failure.InnerException, Is.SameAs(releaseFailure));
                Assert.That(cached.RefCount, Is.EqualTo(1));
                Assert.That(cached.ReleaseCount, Is.Zero);
                Assert.That(duplicate.ReleaseCount, Is.EqualTo(1));
            }
            finally
            {
                state.SetValue(loader, Convert.ToInt32(Enum.Parse(states, "Uninstalled")));
                loader.Dispose();
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static Dictionary<string, AssetLoadReference> GetReferences(LoaderModule loader)
            => Game.ReflectionUtility.GetFieldValue<Dictionary<string, AssetLoadReference>>(loader, "m_AssetReferences");

        private static AssetLoadHandle<GameObject> CompleteAssetLoad(
            LoaderModule loader, AssetLoadReference reference, CancellationToken ct = default)
            => (AssetLoadHandle<GameObject>)typeof(LoaderModule)
                .GetMethod("CompleteAssetLoad", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(typeof(GameObject)).Invoke(loader, new object[] { "Assets/Test.prefab", reference, ct });

        private sealed class FailingAssetReference : AssetLoadReference
        {
            private readonly Exception m_Failure;
            public int ReleaseCount { get; private set; }
            public FailingAssetReference(UnityEngine.Object asset, Exception failure = null) : base(asset) => m_Failure = failure;
            public override void ReleaseResources()
            {
                ReleaseCount++;
                if (m_Failure != null) throw m_Failure;
            }
        }

        private sealed class PendingLoader : ILoader
        {
            public readonly TaskCompletionSource<AssetLoadHandle<ScriptableObject>> Completion = new();
            public AssetLoadHandle<T> LoadAsset<T>(string path) where T : UnityEngine.Object => throw new NotSupportedException();
            public async Task<AssetLoadHandle<T>> LoadAssetAsync<T>(string path, CancellationToken ct = default)
                where T : UnityEngine.Object => (AssetLoadHandle<T>)(object)await Completion.Task;
            public Task<SceneLoadHandle> LoadSceneAsync(string path, bool allowSceneActivation = true,
                LoadSceneParameters parameters = default, Action<float> onProgress = null) => throw new NotSupportedException();
            public Task<SceneLoadHandle> UnloadSceneAsync(string path, UnloadSceneOptions options = UnloadSceneOptions.None,
                Action<float> onProgress = null) => throw new NotSupportedException();
        }
    }
}
