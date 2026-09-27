namespace Verve.Tests.Core
{
    using Verve;
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [Category("Core")]
    internal class CoreUtilitiesAndPoolTests
    {
        [Serializable]
        private sealed class SerializationPayload
        {
            public string text;
            public int count;
            public string[] tags;
        }

        private sealed class PoolItem
        {
            public int Id { get; }

            public PoolItem(int id)
            {
                Id = id;
            }
        }

        private sealed class DisposableProbe : DisposableObject
        {
            public int DisposeCount { get; private set; }
            public bool CanAccess => CheckAccess();
            public bool ThrowOnDispose { get; set; }

            protected override void OnDispose()
            {
                DisposeCount++;
                if (ThrowOnDispose)
                {
                    throw new InvalidOperationException("dispose failure");
                }
            }

            private bool CheckAccess()
            {
                ThrowIfDisposed();
                return true;
            }
        }


        [Test]
        public void ResourceCleanup_RemovesBeforeCallbacksAndCollectsEveryFailure()
        {
            var resources = new List<int> { 1, 2, 3 };
            var released = new List<int>();
            Action<int> release = null;
            release = item =>
            {
                released.Add(item);
                CollectionAssert.DoesNotContain(resources, item);
                if (item == 3) ResourceUtility.ReleaseAll(resources, release);
                else throw new InvalidOperationException(item.ToString());
            };

            var failure = Assert.Throws<AggregateException>(() => ResourceUtility.ReleaseAll(resources, release));

            Assert.That(released, Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(resources, Is.Empty);
            Assert.That(failure.InnerExceptions.Count, Is.EqualTo(2));
            ResourceUtility.ReleaseAll(resources, release);
            Assert.That(released.Count, Is.EqualTo(3));
        }

        [Test]
        public void FormattedLogs_RejectMissingArguments()
        {
            Assert.Throws<FormatException>(() => Game.Log("{0}", Array.Empty<object>()));
            Assert.Throws<FormatException>(() => Game.LogWarning("{0}", Array.Empty<object>()));
            Assert.Throws<FormatException>(() => Game.LogError("{0}", Array.Empty<object>()));
        }

        [Test]
        public void ObjectPool_AllowsEqualValuesButRejectsDuplicateReferences()
        {
            using var values = new ObjectPool<int>(() => 1, preSize: 3, capacity: 3);
            Assert.That(values.Count, Is.EqualTo(3));
            using var references = new ObjectPool<PoolItem>(() => new PoolItem(1), preSize: 0);
            var item = references.Get();
            references.Release(item);
#if UNITY_EDITOR || DEBUG
            Assert.Throws<InvalidOperationException>(() => references.Release(item));
#endif
        }

#if UNITY_5_3_OR_NEWER
        [Test]
        public void PropertyProxy_UsesComparerAndStandardEventSubscriptions()
        {
            var proxy = new PropertyProxy<string>("A", StringComparer.OrdinalIgnoreCase.Equals);
            var values = new List<string>();
            var propertyChanges = 0;
            Action<string> onValueChanged = values.Add;
            proxy.ValueChanged += onValueChanged;
            proxy.PropertyChanged += (_, change) =>
            {
                Assert.That(change.PropertyName, Is.EqualTo(nameof(proxy.Value)));
                propertyChanges++;
            };

            proxy.Value = "a";
            Assert.That(proxy.Value, Is.EqualTo("A"));
            proxy.Value = "B";
            proxy.ValueChanged -= onValueChanged;
            proxy.Value = "C";
            proxy.RemoveAllListeners();
            proxy.Value = "D";

            Assert.That(values, Is.EqualTo(new[] { "B" }));
            Assert.That(propertyChanges, Is.EqualTo(2));
        }

        [Test]
        public void GameObjectPool_PredicateMissReturnsFalse()
        {
            var prefab = new UnityEngine.GameObject("Pool test");
            prefab.SetActive(false);
            try
            {
                using var pool = new GameObjectPool(prefab, preSize: 0);
                Assert.That(pool.TryGet(out var result, UnityEngine.Vector3.one,
                    UnityEngine.Quaternion.identity, _ => false), Is.False);
                Assert.That(result, Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(prefab); }
        }

        [Test]
        public void CoroutineHandle_TracksSynchronousCompletionAndExplicitStop()
        {
            var complete = 0;
            using var finished = Task.CompletedTask.AsCoroutine(() => complete++);
            Assert.That(complete, Is.EqualTo(1));
            Assert.That(finished.IsRunning, Is.False);
            var pending = new TaskCompletionSource<bool>();
            using var running = ((Task)pending.Task).AsCoroutine();
            Assert.That(running.IsRunning, Is.True);
            running.Stop();
            Assert.That(running.IsCancellationRequested, Is.True);
            Assert.That(running.IsRunning, Is.False);
            Assert.That(pending.Task.IsCompleted, Is.False);
            pending.SetResult(true);
        }

        private sealed class ThrowingDisposeRoutine : System.Collections.IEnumerator, IDisposable
        {
            public int DisposeCount;
            public object Current => null;
            public bool MoveNext() => true;
            public void Reset() => throw new NotSupportedException();
            public void Dispose()
            {
                DisposeCount++;
                throw new InvalidOperationException("iterator cleanup");
            }
        }

        [Test]
        public void CoroutineHandle_ClearsOwnershipEvenWhenIteratorDisposalFails()
        {
            var routine = new ThrowingDisposeRoutine();
            var operation = new CoroutineOperation(_ => routine, default);
            Assert.Throws<InvalidOperationException>(() => operation.Stop());
            Assert.That(operation.IsRunning, Is.False);
            Assert.That(operation.Coroutine, Is.Null);
            CollectionAssert.DoesNotContain(CoroutineRunner.Instance.Operations, operation);
            operation.Dispose();
            Assert.That(routine.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void CoroutineRunner_DestroyCancelsAwaitAndDisposesOwnedOperations()
        {
            var awaiter = new UnityEngine.WaitForSeconds(100).GetAwaiter();
            var operation = new TaskCompletionSource<bool>().Task.AsCoroutine();
            UnityEngine.Object.DestroyImmediate(CoroutineRunner.Instance.gameObject);
            Assert.That(awaiter.IsCompleted, Is.True);
            Assert.Throws<TaskCanceledException>(() => awaiter.GetResult());
            Assert.That(operation.IsDisposed, Is.True);
            Assert.That(operation.IsRunning, Is.False);
            Assert.That(operation.IsCancellationRequested, Is.True);
        }
#endif

        [Test]
        public async Task CancelledWait_DoesNotCancelSharedOperation()
        {
            var source = new TaskCompletionSource<int>();
            using var cancellation = new CancellationTokenSource();
            var waiting = source.Task.WaitWithCancellationAsync(cancellation.Token);
            cancellation.Cancel();
            try { await waiting; Assert.Fail("Expected cancellation."); }
            catch (OperationCanceledException) { }
            Assert.That(source.Task.IsCompleted, Is.False);
            source.SetResult(42);
            Assert.That(await source.Task.WaitWithCancellationAsync(), Is.EqualTo(42));
            var failure = new InvalidOperationException("shared failure");
            try { await Task.FromException(failure).WaitWithCancellationAsync(); Assert.Fail("Expected failure."); }
            catch (InvalidOperationException actual) { Assert.That(actual, Is.SameAs(failure)); }
        }

        [Test]
        public void ObjectPool_CleanupAttemptsEveryItemAndPreservesFailures()
        {
            var destroyed = 0;
            var pool = new ObjectPool<PoolItem>(() => new PoolItem(1),
                onDestroyObject: _ => { destroyed++; throw new InvalidOperationException("destroy"); },
                preSize: 3, capacity: 3);
            var failure = Assert.Throws<AggregateException>(() => pool.Dispose());
            Assert.That(failure.InnerExceptions.Count, Is.EqualTo(3));
            Assert.That(destroyed, Is.EqualTo(3));
            Assert.That(pool.Count, Is.Zero);
            pool.Dispose();
        }

        [Test]
        public void ObjectPool_RechecksOwnershipAfterCallbacks()
        {
            foreach (var disposeInFactory in new[] { false, true })
            {
                var destroyed = new List<PoolItem>();
                ObjectPool<PoolItem> pool = null;
                pool = new ObjectPool<PoolItem>(() =>
                {
                    if (disposeInFactory) pool.Dispose();
                    return new PoolItem(1);
                }, onGetFromPool: _ => pool.Dispose(), onDestroyObject: destroyed.Add, preSize: 0);
                Assert.Throws<ObjectDisposedException>(() => pool.Get());
                Assert.That(pool.Count, Is.Zero);
                Assert.That(destroyed.Count, Is.EqualTo(1));
            }

            for (var mode = 0; mode < 3; mode++)
            {
                var destroyed = new List<int>();
                ObjectPool<PoolItem> pool = null;
                pool = new ObjectPool<PoolItem>(() => new PoolItem(0), onReleaseToPool: item =>
                {
                    if (item.Id != 1) return;
                    if (mode == 0) pool.Dispose();
                    else if (mode == 1) pool.Capacity = 0;
                    else pool.Release(new PoolItem(2));
                }, onDestroyObject: item => destroyed.Add(item.Id), preSize: 0, capacity: 1);
                pool.Release(new PoolItem(1));
                Assert.That(destroyed, Is.EqualTo(new[] { 1 }));
                Assert.That(pool.Count, Is.EqualTo(mode == 2 ? 1 : 0));
                pool.Dispose();
                Assert.That(destroyed.Count, Is.EqualTo(mode == 2 ? 2 : 1));
            }
        }

        [Test]
        public void ObjectPool_GetCallbackFailureDestroysBorrowedItem()
        {
            PoolItem destroyed = null;
            var item = new PoolItem(1);
            using var pool = new ObjectPool<PoolItem>(() => item,
                onGetFromPool: _ => throw new InvalidOperationException("get"),
                onDestroyObject: value => destroyed = value, preSize: 0);
            Assert.Throws<InvalidOperationException>(() => pool.Get());
            Assert.That(destroyed, Is.SameAs(item));
            Assert.That(pool.Count, Is.Zero);
        }

        [Test]
        public void ObjectPool_PredicateKeepsUnmatchedItemsAvailable()
        {
            var pool = new ObjectPool<PoolItem>(() => new PoolItem(-1), preSize: 0, capacity: 3);
            var first = new PoolItem(1);
            var second = new PoolItem(2);
            var third = new PoolItem(3);
            pool.Release(first);
            pool.Release(second);
            pool.Release(third);

            var match = pool.Get(item => item.Id == 2);

            Assert.That(match, Is.SameAs(second));
            Assert.That(pool.Count, Is.EqualTo(2));
            Assert.That(pool.Get(item => item.Id == 1), Is.SameAs(first));
            Assert.That(pool.Get(item => item.Id == 3), Is.SameAs(third));
            pool.Dispose();
        }

        [Test]
        public void ObjectPool_CapacityAndDisposalDestroyOwnedItems()
        {
            var destroyed = new List<int>();
            var pool = new ObjectPool<PoolItem>(
                () => new PoolItem(0),
                onDestroyObject: item => destroyed.Add(item.Id),
                preSize: 0,
                capacity: 1);

            pool.Release(new PoolItem(1));
            pool.Release(new PoolItem(2));
            pool.Capacity = 0;
            pool.Dispose();

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, destroyed);
            Assert.That(pool.Count, Is.EqualTo(0));
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
        }

        [Test]
        public void ObjectPool_NewObjectRunsGetCallback()
        {
            var getCount = 0;
            var pool = new ObjectPool<PoolItem>(
                () => new PoolItem(7),
                onGetFromPool: _ => getCount++,
                preSize: 0,
                capacity: 1);

            var item = pool.Get();

            Assert.That(item, Is.Not.Null);
            Assert.That(getCount, Is.EqualTo(1));
            pool.Dispose();
        }

        [Test]
        public void ObjectPool_TryGetReportsFailureWhenFactoryReturnsNull()
        {
            var pool = new ObjectPool<PoolItem>(() => null, preSize: 0, capacity: 1);

            Assert.That(pool.TryGet(out var item), Is.False);
            Assert.That(item, Is.Null);
            pool.Dispose();
        }

        [Test]
        public void ObjectPool_ZeroCapacityDoesNotLimitBorrowedObjects()
        {
            var pool = new ObjectPool<int>(() => 0, preSize: 0, capacity: 0);

            Assert.That(pool.TryGet(out var value), Is.True);
            Assert.That(value, Is.EqualTo(0));
            pool.Dispose();
        }

        [Test]
        public void DisposableObject_IsIdempotentAndGuardsDisposedAccess()
        {
            var disposable = new DisposableProbe();

            Assert.That(disposable.IsDisposed, Is.False);
            Assert.That(disposable.CanAccess, Is.True);

            disposable.Dispose();
            disposable.Dispose();

            Assert.That(disposable.IsDisposed, Is.True);
            Assert.That(disposable.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposableObject_RemainsDisposedWhenReleaseFails()
        {
            var disposable = new DisposableProbe { ThrowOnDispose = true };

            Assert.Throws<InvalidOperationException>(() => disposable.Dispose());
            disposable.Dispose();

            Assert.That(disposable.IsDisposed, Is.True);
            Assert.That(disposable.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Serializer_RoundTripsPayloadAndLeavesProvidedStreamOpen()
        {
            var payload = new SerializationPayload
            {
                text = "hello",
                count = 7,
                tags = new[] { "one", "two" }
            };

            using var stream = new MemoryStream();
            Game.Serializer.Serialize(stream, payload, new UTF8Encoding(false));
            Assert.That(stream.CanRead, Is.True);
            stream.Position = 0;

            var decoded = Game.Serializer.Deserialize<SerializationPayload>(stream, Encoding.UTF8);

            Assert.That(decoded.text, Is.EqualTo(payload.text));
            Assert.That(decoded.count, Is.EqualTo(payload.count));
            CollectionAssert.AreEqual(payload.tags, decoded.tags);
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void Serializer_StringRoundTripUsesRequestedEncodingWithoutEmittingPreamble()
        {
            foreach (var encoding in new Encoding[] { new UTF8Encoding(false), new UTF8Encoding(true), Encoding.Unicode, Encoding.BigEndianUnicode })
            {
                var payload = new SerializationPayload { text = "中文/🎮", count = 42 };
                var text = Game.Serializer.SerializeToString(payload, encoding);
                Assert.That(text[0], Is.EqualTo('{'));
                Assert.That(Game.Serializer.Deserialize<SerializationPayload>(text, encoding).text, Is.EqualTo(payload.text));
                var bytes = Game.Serializer.SerializeToBytes(payload, encoding);
                Assert.That(Game.Serializer.Deserialize<SerializationPayload>(bytes, encoding).count, Is.EqualTo(42));
            }
        }

        private sealed class EmptyCompressionProbe : ICompression
        {
            public int CompressCalls;
            public int DecompressCalls;
            public byte[] Compress(byte[] data)
            {
                CompressCalls++;
                Assert.That(data, Is.Empty);
                return new byte[] { 42 };
            }
            public byte[] Decompress(byte[] data)
            {
                DecompressCalls++;
                Assert.That(data, Is.Empty);
                return Encoding.UTF8.GetBytes("empty payload");
            }
        }

        [Test]
        public void Compression_EmptyInputReachesSelectedImplementationAndNullIsRejected()
        {
            var compression = new EmptyCompressionProbe();
            Assert.That(compression.Compress(string.Empty), Is.EqualTo(new byte[] { 42 }));
            Assert.That(compression.DecompressToString(Array.Empty<byte>()), Is.EqualTo("empty payload"));
            Assert.That(compression.CompressCalls, Is.EqualTo(1));
            Assert.That(compression.DecompressCalls, Is.EqualTo(1));
            Assert.Throws<ArgumentNullException>(() => compression.Compress((string)null));
            Assert.Throws<ArgumentNullException>(() => Game.Compression.DecompressToString(null));
            var compressed = Game.Compression.Compress(string.Empty);
            Assert.That(Game.Compression.DecompressToString(compressed), Is.Empty);
        }

        private static byte[] CreateCryptoKey()
        {
            var key = new byte[64];
            using var random = System.Security.Cryptography.RandomNumberGenerator.Create();
            random.GetBytes(key);
            return key;
        }

        [Test]
        public void Crypto_UsesFreshIvAndRejectsTamperingAndWrongKeys()
        {
            ICrypto crypto = new AesCrypto();
            var key = CreateCryptoKey();
            var otherKey = CreateCryptoKey();
            var plain = Encoding.UTF8.GetBytes("private payload");
            var first = crypto.Encrypt(plain, key);
            var second = crypto.Encrypt(plain, key);
            Assert.That(first, Is.Not.EqualTo(second));
            CollectionAssert.AreEqual(plain, crypto.Decrypt(first, key));
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => crypto.Decrypt(first, otherKey));
            first[0] ^= 1;
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => crypto.Decrypt(first, key));
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => crypto.Decrypt(new byte[4], key));
        }

        [Test]
        public void ProjectPaths_RejectTraversalAndEmptySegments()
        {
            foreach (var path in new[] { "Assets/../secret", "Packages/./data", "Assets//data", "Assets/" })
                Assert.Throws<ArgumentException>(() => Game.PathUtility.NormalizeProjectPath(path));
            Assert.That(Game.PathUtility.ToRequestUrl("/tmp/a b.txt"), Is.EqualTo("file:///tmp/a%20b.txt"));
        }

        [Test]
        public void CompressionAndCrypto_RoundTripUtf8Text()
        {
            ICrypto crypto = new AesCrypto();
            const string text = "Verve compression and encryption";

            var compressed = Game.Compression.Compress(text, Encoding.UTF8);
            var decompressed = Game.Compression.DecompressToString(compressed, Encoding.UTF8);
            var key = CreateCryptoKey();
            var encrypted = crypto.Encrypt(text, key, Encoding.UTF8);

            Assert.That(decompressed, Is.EqualTo(text));
            Assert.That(crypto.Decrypt(encrypted, key, Encoding.UTF8), Is.EqualTo(text));
        }

        [Test]
        public void Crypto_BorrowsKeyAndRejectsInvalidLengths()
        {
            ICrypto crypto = new AesCrypto();
            var key = CreateCryptoKey();
            var original = (byte[])key.Clone();
            var encrypted = crypto.Encrypt(Array.Empty<byte>(), key);
            Assert.That(crypto.Decrypt(encrypted, key), Is.Empty);
            Assert.That(key, Is.EqualTo(original));
            Assert.Throws<ArgumentException>(() => crypto.Encrypt(new byte[1], new byte[32]));
            Assert.Throws<ArgumentNullException>(() => crypto.Encrypt(new byte[1], null));
        }

        [Test]
        public void FileUtility_ReplacesExistingFileUsingSiblingTemporaryPath()
        {
            var directory = Path.Combine(Path.GetTempPath(), "VerveTests", Guid.NewGuid().ToString("N"));
            var targetPath = Path.Combine(directory, "config.json");
            Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(targetPath, "old", Encoding.UTF8);
                var temporaryPath = Game.FileUtility.GetTemporaryFilePath(targetPath);
                File.WriteAllText(temporaryPath, "new", Encoding.UTF8);

                Game.FileUtility.ReplaceFile(temporaryPath, targetPath);

                Assert.That(Path.GetDirectoryName(temporaryPath), Is.EqualTo(Path.GetDirectoryName(Path.GetFullPath(targetPath))));
                Assert.That(File.ReadAllText(targetPath, Encoding.UTF8), Is.EqualTo("new"));
                Assert.That(File.Exists(temporaryPath), Is.False);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void FileUtility_FormatsUnitBoundaries()
        {
            Assert.That(Game.FileUtility.FormatFileSize(1023), Is.EqualTo("1023 B"));
            Assert.That(Game.FileUtility.FormatFileSize(1024), Is.EqualTo("1 KB"));
            Assert.That(Game.FileUtility.FormatFileSize(1024L * 1024L), Is.EqualTo("1 MB"));


        }

        [Test]
        public void PathUtility_NormalizesWhitespaceAndSeparators()
        {
            Assert.That(Game.PathUtility.Normalize("  Assets\\Config\\Table.ctable  "),
                Is.EqualTo("Assets/Config/Table.ctable"));
        }
    }
}
