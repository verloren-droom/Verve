namespace Verve.Tests.Core
{
    using Verve;
    using System;
    using NUnit.Framework;

    [Category("Core")]
    internal class EventBusTests
    {
        [TearDown]
        public void TearDown()
        {
            Game.OffAll();
        }

#if DEBUG
        [Test]
        public void FailedSubscriptionNotification_RollsBackRegistrationAndReportsCleanupFailure()
        {
            using var dispatcher = new EventDispatcher<string>();
            dispatcher.OnEventRecorded += _ => throw new InvalidOperationException("observer failure");
            var failure = Assert.Throws<AggregateException>(() => dispatcher.On("event", () => { }));
            Assert.That(failure.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(dispatcher.Has("event"), Is.False);
        }
#endif

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static (IDisposable, WeakReference, WeakReference) CreateDetachedSubscription(int mode)
        {
            var dispatcher = new EventDispatcher<string>();
            var target = new System.Collections.Generic.List<int>();
            var subscription = dispatcher.On<int>("event", target.Add);
            if (mode == 0) subscription.Dispose();
            else if (mode == 1) dispatcher.Off("event");
            else if (mode == 2) dispatcher.OffAll();
            else dispatcher.Dispose();
            return (subscription, new WeakReference(dispatcher), new WeakReference(target));
        }

        [Test]
        public void DetachedSubscription_DoesNotRetainDispatcherOrCallbackTarget()
        {
            for (var mode = 0; mode < 4; mode++)
            {
                var (subscription, owner, target) = CreateDetachedSubscription(mode);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.That(owner.IsAlive, Is.False);
                Assert.That(target.IsAlive, Is.False);
                subscription.Dispose();
                GC.KeepAlive(subscription);
            }
        }

        [Test]
        public void HandlerFailureAndPayloadMismatch_ReachPublisher()
        {
            using var subscription = Game.On<int>("Tests.Failure", _ => throw new InvalidOperationException("handler"));
            Assert.Throws<InvalidOperationException>(() => Game.Emit("Tests.Failure", 1));
            Assert.Throws<InvalidOperationException>(() => Game.Emit("Tests.Failure", "wrong payload"));
            Assert.Throws<InvalidOperationException>(() => Game.On<string>("Tests.Failure", _ => { }));
        }

        [Test]
        public void StringAndIntegerKeys_AreIndependent()
        {
            var strings = 0;
            var integers = 0;
            using var first = Game.On("123", () => strings++);
            // 1916298011 是旧实现中字符串 "123" 的 FNV 哈希值。
            using var second = Game.On(1916298011, () => integers++);
            Game.Emit("123");
            Assert.That(strings, Is.EqualTo(1));
            Assert.That(integers, Is.Zero);
        }

        [Test]
        public void StringsWithSameLegacyHash_AreIndependent()
        {
            var firstCalls = 0;
            var secondCalls = 0;
            using var first = Game.On("event.14668", () => firstCalls++);
            using var second = Game.On("event.102914", () => secondCalls++);
            Game.Emit("event.14668");
            Assert.That(firstCalls, Is.EqualTo(1));
            Assert.That(secondCalls, Is.Zero);
            Game.Emit("event.102914");
            Assert.That(secondCalls, Is.EqualTo(1));
        }

        [Test]
        public void OldSubscription_CannotRemoveNewRegistration()
        {
            using var dispatcher = new EventDispatcher<string>();
            var calls = 0;
            Action handler = () => calls++;
            var old = dispatcher.On("event", handler);
            dispatcher.OffAll();
            using var current = dispatcher.On("event", handler);
            old.Dispose();
            dispatcher.Emit("event");
            Assert.That(calls, Is.EqualTo(1));
            dispatcher.Dispose();
            current.Dispose();
            Assert.Throws<ObjectDisposedException>(() => dispatcher.Emit("event"));
        }

        [Test]
        public void SubscriptionDispose_StopsOnlyTheDisposedHandler()
        {
            var total = 0;
            var first = Game.On<int>("Tests.Event.Dispose", value => total += value);
            var second = Game.On<int>("Tests.Event.Dispose", value => total += value * 10);

            Game.Emit("Tests.Event.Dispose", 2);
            first.Dispose();
            Game.Emit("Tests.Event.Dispose", 3);

            Assert.That(total, Is.EqualTo(52));
            second.Dispose();
            Assert.That(Game.Has("Tests.Event.Dispose"), Is.False);
        }

        [Test]
        public void Dispatch_UsesSnapshotWhenHandlerUnsubscribesDuringEmission()
        {
            var calls = 0;
            IDisposable second = null;
            var first = Game.On("Tests.Event.Snapshot", () =>
            {
                calls += 1;
                second.Dispose();
            });
            second = Game.On("Tests.Event.Snapshot", () => calls += 10);

            Game.Emit("Tests.Event.Snapshot");
            Game.Emit("Tests.Event.Snapshot");

            Assert.That(calls, Is.EqualTo(12));
            first.Dispose();
        }
    }
}
