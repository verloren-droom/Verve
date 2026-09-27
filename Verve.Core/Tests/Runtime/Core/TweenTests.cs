#if UNITY_5_3_OR_NEWER
namespace Verve.Tests.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    [Category("Core")]
    internal class TweenTests
    {
        [Test]
        public void ZeroDuration_AppliesEndpointExactlyOnce()
        {
            var values = new List<float>();
            var animation = Tween.Run(0, values.Add);
            Assert.That(animation.MoveNext(), Is.False);
            Assert.That(values, Is.EqualTo(new[] { 1f }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Tween.Run(-1, _ => { }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Tween.Run(float.NaN, _ => { }));
        }

        [Test]
        public void CancellationAndCallbackFailure_ReachCaller()
        {
            using var cancellation = new CancellationTokenSource();
            var values = new List<float>();
            var animation = Tween.Run(1, values.Add, ct: cancellation.Token);
            Assert.That(animation.MoveNext(), Is.True);
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => animation.MoveNext());
            Assert.That(values, Is.EqualTo(new[] { 0f }));
            var failure = new InvalidOperationException("apply");
            animation = Tween.Run(0, _ => throw failure);
            Assert.That(Assert.Throws<InvalidOperationException>(() => animation.MoveNext()), Is.SameAs(failure));
        }

        [UnityTest]
        public IEnumerator UnscaledAnimation_ComposesInOrderWhileTimeIsPaused()
        {
            var previousScale = Time.timeScale;
            var values = new List<float>();
            Time.timeScale = 0;
            try
            {
                yield return Tween.Run(0.02f, values.Add, unscaledTime: true);
                Assert.That(values[0], Is.Zero);
                Assert.That(values[values.Count - 1], Is.EqualTo(1f));
                Assert.That(values, Is.Ordered);
                var endpoints = values.FindAll(value => value == 1f).Count;
                Assert.That(endpoints, Is.EqualTo(1));
                yield return Tween.Run(0, value => values.Add(value + 1f));
                Assert.That(values[values.Count - 1], Is.EqualTo(2f));
            }
            finally { Time.timeScale = previousScale; }
        }
    }
}
#endif
