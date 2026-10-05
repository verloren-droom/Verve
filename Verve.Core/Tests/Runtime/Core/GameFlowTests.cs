namespace Verve.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Verve;

    [Category("Core")]
    internal sealed class GameFlowTests
    {
        [Test]
        public async Task RunAsync_ExecutesStepsInOrderAndReportsStepProgress()
        {
            var flow = new GameFlow();
            var order = new List<string>();
            var progress = new List<GameFlowProgress>();
            flow.Add("初始化", _ =>
            {
                order.Add("initialize");
                return default;
            });
            flow.Add("下载更新", (_, report) =>
            {
                order.Add("download");
                report(0.5f);
                return default;
            });
            flow.Add("启动游戏", _ =>
            {
                order.Add("start");
                return default;
            });

            await flow.RunAsync(progress.Add);

            Assert.That(order, Is.EqualTo(new[] { "initialize", "download", "start" }));
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Completed));
            Assert.That(progress.Exists(value => value.StepName == "下载更新" &&
                Math.Abs(value.StepProgress - 0.5f) < 0.0001f), Is.True);
            Assert.That(progress[^1].StepName, Is.EqualTo("启动游戏"));
            Assert.That(progress.FindAll(value => value.StepName == "启动游戏" && value.StepProgress == 1f).Count,
                Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_WaitsForCurrentStepAndPublishesStepStates()
        {
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var followingStepCalled = false;
            var flow = new GameFlow();
            flow.Add("检查更新", _ => new ValueTask(gate.Task));
            flow.Add("启动游戏", _ =>
            {
                followingStepCalled = true;
                return default;
            });

            var running = flow.RunAsync().AsTask();
            Assert.That(flow.StepStates[0], Is.EqualTo(GameFlowStepState.Current));
            Assert.That(followingStepCalled, Is.False);

            gate.SetResult(true);
            await running;

            Assert.That(followingStepCalled, Is.True);
            Assert.That(flow.StepStates[0], Is.EqualTo(GameFlowStepState.Executed));
            Assert.That(flow.StepStates[1], Is.EqualTo(GameFlowStepState.Executed));
        }

        [Test]
        public void RunAsync_CancellationStopsFollowingStepsAndMarksFlowCanceled()
        {
            using var cancellation = new CancellationTokenSource();
            var flow = new GameFlow();
            var secondCalled = false;
            flow.Add("检查更新", _ =>
            {
                cancellation.Cancel();
                return default;
            });
            flow.Add("下载更新", _ =>
            {
                secondCalled = true;
                return default;
            });

            Assert.ThrowsAsync<OperationCanceledException>(() => flow.RunAsync(ct: cancellation.Token).AsTask());

            Assert.That(secondCalled, Is.False);
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Canceled));
            Assert.That(flow.CurrentStepIndex, Is.EqualTo(0));
        }

        [Test]
        public void RunAsync_FailureStopsFollowingStepsAndCanOnlyRunOnce()
        {
            var flow = new GameFlow();
            var secondCalled = false;
            flow.Add("检查更新", _ => throw new InvalidOperationException("update check failed"));
            flow.Add("启动游戏", _ =>
            {
                secondCalled = true;
                return default;
            });

            var failure = Assert.ThrowsAsync<InvalidOperationException>(() => flow.RunAsync().AsTask());

            Assert.That(failure.Message, Is.EqualTo("update check failed"));
            Assert.That(secondCalled, Is.False);
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Failed));
            Assert.Throws<InvalidOperationException>(() => flow.Add("late", _ => default));
            Assert.ThrowsAsync<InvalidOperationException>(() => flow.RunAsync().AsTask());
        }

        [Test]
        public void Add_RejectsInvalidStepsAndProgress()
        {
            var flow = new GameFlow();
            Assert.Throws<ArgumentException>(() => flow.Add(" ", _ => default));
            Assert.Throws<ArgumentNullException>(() => flow.Add("step", (Func<CancellationToken, ValueTask>)null));
            Assert.Throws<ArgumentOutOfRangeException>(() => flow.Add("step", _ => default));

            flow.Add("step", (_, report) =>
            {
                report(float.NaN);
                return default;
            });
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => flow.RunAsync().AsTask());
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Failed));
        }

        [Test]
        public async Task Connect_SelectsFirstMatchingTransition()
        {
            var flow = new GameFlow();
            var calls = new List<string>();
            var entry = flow.Add("入口", _ =>
            {
                calls.Add("entry");
                return default;
            });
            var skipped = flow.Add("跳过", _ =>
            {
                calls.Add("skipped");
                return default;
            });
            var selected = flow.Add("选择", _ =>
            {
                calls.Add("selected");
                return default;
            });

            flow.Connect(entry, skipped, () => false, "不满足");
            flow.Connect(entry, selected, () => true, "满足");

            await flow.RunAsync();

            Assert.That(calls, Is.EqualTo(new[] { "entry", "selected" }));
            Assert.That(entry.Transitions.Count, Is.EqualTo(2));
            Assert.That(entry.Transitions[1].Label, Is.EqualTo("满足"));
            Assert.That(flow.StepStates[0], Is.EqualTo(GameFlowStepState.Executed));
            Assert.That(flow.StepStates[1], Is.EqualTo(GameFlowStepState.NotExecuted));
            Assert.That(flow.StepStates[2], Is.EqualTo(GameFlowStepState.Executed));
        }

        [Test]
        public void FlowStep_TransitionsCannotBeMutatedThroughPublicView()
        {
            var flow = new GameFlow();
            var first = flow.Add("第一步", _ => default);
            var second = flow.Add("第二步", _ => default);
            flow.Connect(first, second);

            Assert.That(first.Transitions, Is.Not.Null);
            Assert.That(first.Transitions, Is.Not.TypeOf<List<GameFlowStepTransition>>());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<GameFlowStepTransition>)first.Transitions).Clear());
        }

        [Test]
        public void Branch_CannotBeConfiguredTwice()
        {
            var flow = new GameFlow();
            var entry = flow.Add("入口", _ => default);
            var target = flow.Add("目标", _ => default);

            flow.Branch(entry, () => true, target, null);

            Assert.Throws<InvalidOperationException>(() =>
                flow.Branch(entry, () => false, target, null));
        }

        [Test]
        public async Task RunAsync_ReleasesRuntimeDelegatesAfterCompletion()
        {
            var flow = new GameFlow();
            var entry = flow.Add("入口", _ => default);
            var target = flow.Add("目标", _ => default);
            flow.Branch(entry, () => true, target, null);

            await flow.RunAsync();

            Assert.That(typeof(GameFlowStep).GetField("m_Execute",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry), Is.Null);
            Assert.That(typeof(GameFlowStep).GetField("m_BranchCondition",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry), Is.Null);
        }
    }
}
