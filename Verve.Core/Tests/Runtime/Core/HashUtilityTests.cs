namespace Verve.Tests.Core
{
    using System;
    using System.IO;
    using System.Text;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class HashUtilityTests
    {
        [TestCase("", 0xcbf29ce484222325UL)]
        [TestCase("a", 0xaf63dc4c8601ec8cUL)]
        [TestCase("hello", 0xa430d84680aabd0bUL)]
        [TestCase("foobar", 0x85944171f73967e8UL)]
        public void Fnv1a64_MatchesKnownVectorsAndEverySplit(string text, ulong expected)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            Assert.That(Game.HashUtility.ComputeFnv1a64(bytes), Is.EqualTo(expected));
            for (int split = 0; split <= bytes.Length; split++)
            {
                var prefix = Game.HashUtility.ComputeFnv1a64(bytes.AsSpan(0, split));
                Assert.That(Game.HashUtility.ComputeFnv1a64(bytes.AsSpan(split), prefix), Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task FileHashes_MatchKnownDigestsAndReleaseTheFile()
        {
            var cases = new[]
            {
                (HashAlgorithmName.MD5, "900150983cd24fb0d6963f7d28e17f72"),
                (HashAlgorithmName.SHA1, "a9993e364706816aba3e25717850c26c9cd0d89d"),
                (HashAlgorithmName.SHA256, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"),
                (HashAlgorithmName.SHA384, "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7"),
                (HashAlgorithmName.SHA512, "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f")
            };
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, Encoding.ASCII.GetBytes("abc"));
                foreach (var (algorithm, expected) in cases)
                {
                    Assert.That(Game.HashUtility.ComputeFileHash(path, algorithm), Is.EqualTo(expected));
                    Assert.That(await Game.HashUtility.ComputeFileHashAsync(path, algorithm), Is.EqualTo(expected));
                    using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
            }
            finally { File.Delete(path); }
        }

        [Test]
        public async Task StreamHashes_ReadCurrentPositionAndKeepBorrowedStreamsOpen()
        {
            const string expected = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
            using var input = new MemoryStream(Encoding.ASCII.GetBytes("prefixabc"));
            input.Position = 6;
            Assert.That(Game.HashUtility.ComputeHash(input, HashAlgorithmName.SHA256), Is.EqualTo(expected));
            Assert.That(input.CanRead, Is.True);
            Assert.That(input.Position, Is.EqualTo(input.Length));
            input.Position = 6;
            Assert.That(await Game.HashUtility.ComputeHashAsync(input, HashAlgorithmName.SHA256), Is.EqualTo(expected));
            Assert.That(input.CanRead, Is.True);
            Assert.That(await Game.HashUtility.ComputeHashAsync(input, HashAlgorithmName.SHA256),
                Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
        }

        [Test]
        public async Task NonSeekableLargeInput_IsHashedInBoundedChunks()
        {
            var data = Encoding.ASCII.GetBytes(new string('a', 1000000));
            const string expected = "cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0";
            using var sync = new ChunkedStream(data);
            using var asyncInput = new ChunkedStream(data);
            Assert.That(Game.HashUtility.ComputeHash(sync, HashAlgorithmName.SHA256), Is.EqualTo(expected));
            Assert.That(await Game.HashUtility.ComputeHashAsync(asyncInput, HashAlgorithmName.SHA256), Is.EqualTo(expected));
            Assert.That(sync.ReadCount, Is.GreaterThan(1));
            Assert.That(asyncInput.ReadCount, Is.GreaterThan(1));
            Assert.That(sync.CanRead && asyncInput.CanRead, Is.True);
        }

        [Test]
        public void Cancellation_StopsBeforeFileAccessAndDuringBorrowedStreamRead()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.CatchAsync<OperationCanceledException>(() => Game.HashUtility.ComputeFileHashAsync(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), HashAlgorithmName.SHA256, cancellation.Token));
            using var duringRead = new CancellationTokenSource();
            using var stream = new ChunkedStream(new byte[100]) { AfterRead = duringRead.Cancel };
            Assert.CatchAsync<OperationCanceledException>(() => Game.HashUtility.ComputeHashAsync(stream, HashAlgorithmName.SHA256, duringRead.Token));
            Assert.That(stream.ReadCount, Is.EqualTo(1));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void Failures_DoNotReturnADigestOrCloseBorrowedStreams()
        {
            var path = Path.GetTempFileName();
            try
            {
                Assert.Catch<CryptographicException>(() => Game.HashUtility.ComputeFileHash(path, new HashAlgorithmName("Unsupported-Verve-Hash")));
                using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            finally { File.Delete(path); }
            Assert.Throws<FileNotFoundException>(() => Game.HashUtility.ComputeFileHash(path, HashAlgorithmName.SHA256));
            using var stream = new ChunkedStream(new byte[1]) { FailRead = true };
            Assert.Throws<IOException>(() => Game.HashUtility.ComputeHash(stream, HashAlgorithmName.SHA256));
            Assert.ThrowsAsync<IOException>(() => Game.HashUtility.ComputeHashAsync(stream, HashAlgorithmName.SHA256));
            Assert.That(stream.CanRead, Is.True);
        }

        private sealed class ChunkedStream : Stream
        {
            private readonly MemoryStream m_Input;
            internal Action AfterRead;
            internal bool FailRead;
            internal int ReadCount;
            internal ChunkedStream(byte[] data) => m_Input = new MemoryStream(data);
            public override bool CanRead => m_Input.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCount++;
                if (FailRead) throw new IOException("read failed");
                Assert.That(count, Is.LessThanOrEqualTo(81920));
                var read = m_Input.Read(buffer, offset, Math.Min(count, 4093));
                AfterRead?.Invoke();
                return read;
            }
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                Task.FromResult(Read(buffer, offset, count));
            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            protected override void Dispose(bool disposing)
            {
                if (disposing) m_Input.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
