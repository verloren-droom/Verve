namespace Verve.Tests.Network
{
    using System;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [Category("Network")]
    internal class NetworkTests
    {
        [Test]
        public async Task NetworkModule_ValidatesMessageLimitAndWebSocketScheme()
        {
            using var module = new NetworkModule();

            Assert.Throws<ArgumentOutOfRangeException>(() => module.MaxReceiveMessageBytes = 0);
            await ExpectFailure<ArgumentException>(async () =>
                await module.ConnectAsync(new Uri("https://example.invalid/socket")));
            Assert.That(module.State, Is.EqualTo(NetworkState.Disconnected));
        }

        [Test]
        public async Task NetworkModule_UnconnectedOperationsFailPredictablyAndDisconnectIsIdempotent()
        {
            using var module = new NetworkModule();

            await module.DisconnectAsync();
            Assert.That(module.State, Is.EqualTo(NetworkState.Disconnected));
            await ExpectFailure<InvalidOperationException>(async () => await module.SendTextAsync("payload"));
            await ExpectFailure<InvalidOperationException>(async () => await module.ReceiveTextAsync());
        }

        private static async Task ExpectFailure<T>(Func<Task> action) where T : Exception
        {
            try { await action(); }
            catch (T) { return; }
            Assert.Fail("Expected " + typeof(T).Name);
        }
    }
}
