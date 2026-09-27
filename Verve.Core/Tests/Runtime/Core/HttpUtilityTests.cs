namespace Verve.Tests.Core
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
    using UnityEngine.Networking;
#endif

    [Category("Core")]
    internal class HttpUtilityTests
    {
        [Test]
        public async Task Get_HonorsAnAlreadyCancelledTokenBeforeStartingRequest()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await ExpectFailure<OperationCanceledException>(() =>
                Game.HttpUtility.Get("https://example.invalid/cancelled", cancellationToken: cancellation.Token));
        }

#if UNITY_5_3_OR_NEWER
        private string m_Directory;

        [SetUp]
        public void SetUp()
        {
            m_Directory = Path.Combine(Path.GetTempPath(), "verve-http-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_Directory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(m_Directory, true);

        [Test]
        public async Task GetAndSend_ReadEscapedFileUrlsAndLeaveBorrowedResponseReadable()
        {
            const string content = "文本 payload";
            var source = Path.Combine(m_Directory, "文本 #1.txt");
            File.WriteAllText(source, content, new UTF8Encoding(false));
            var url = Game.PathUtility.ToRequestUrl(source);

            Assert.That(await Game.HttpUtility.Get(url), Is.EqualTo(content));
            Assert.That(await Game.HttpUtility.DownloadBytes(url), Is.EqualTo(Encoding.UTF8.GetBytes(content)));
            using var request = UnityWebRequest.Get(url);
            await Game.HttpUtility.SendAsync(request);
            Assert.That(request.downloadHandler.text, Is.EqualTo(content));
        }

        [Test]
        public async Task DownloadFile_ReplacesOnSuccessAndPreservesTargetOnFailure()
        {
            var source = Path.Combine(m_Directory, "source.txt");
            var target = Path.Combine(m_Directory, "target.txt");
            File.WriteAllText(source, "downloaded");
            File.WriteAllText(target, "original");

            await Game.HttpUtility.DownloadFile(Game.PathUtility.ToRequestUrl(source), target);
            Assert.That(File.ReadAllText(target), Is.EqualTo("downloaded"));
            await ExpectFailure<InvalidOperationException>(() =>
                Game.HttpUtility.DownloadFile(Game.PathUtility.ToRequestUrl(Path.Combine(m_Directory, "missing")), target));
            Assert.That(File.ReadAllText(target), Is.EqualTo("downloaded"));
            Assert.That(Directory.GetFiles(m_Directory).Length, Is.EqualTo(2));
        }

        [Test]
        public async Task DownloadFile_CancellationInProgressCallbackPreservesTargetAndRemovesTemporaryFile()
        {
            var source = Path.Combine(m_Directory, "source.txt");
            var target = Path.Combine(m_Directory, "target.txt");
            File.WriteAllText(source, "downloaded");
            File.WriteAllText(target, "original");
            using var cancellation = new CancellationTokenSource();

            await ExpectFailure<OperationCanceledException>(() => Game.HttpUtility.DownloadFile(
                Game.PathUtility.ToRequestUrl(source), target,
                progressCallback: _ => cancellation.Cancel(), cancellationToken: cancellation.Token));

            Assert.That(File.ReadAllText(target), Is.EqualTo("original"));
            Assert.That(Directory.GetFiles(m_Directory).Length, Is.EqualTo(2));
        }
#endif

        private static async Task ExpectFailure<T>(Func<Task> action) where T : Exception
        {
            try { await action(); }
            catch (T) { return; }
            Assert.Fail("Expected " + typeof(T).Name);
        }
    }
}
