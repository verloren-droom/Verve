#if UNITY_5_3_OR_NEWER

namespace Verve.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using Verve;

    [Category("Core")]
    internal sealed class GameFlowAssetTests
    {
        [Test]
        public async Task CreateFlow_UsesEnabledAssetOrderAndContext()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var calls = new List<string>();
            var contextData = new object();
            try
            {
                asset.AddStep(new GameFlowStepDefinition
                {
                    Name = "初始化",
                    Handler = new RecordingHandler("initialize", calls)
                });
                asset.AddStep(new GameFlowStepDefinition
                {
                    Name = "跳过",
                    Enabled = false,
                    Handler = new RecordingHandler("skipped", calls)
                });
                asset.AddStep(new GameFlowStepDefinition
                {
                    Name = "启动",
                    Handler = new ContextHandler(contextData, calls)
                });

                var flow = asset.CreateFlow(new GameFlowContext(userData: contextData));
                await flow.RunAsync();

                Assert.That(calls, Is.EqualTo(new[] { "initialize", "start" }));
                Assert.That(flow.Count, Is.EqualTo(2));
                Assert.That(flow.State, Is.EqualTo(GameFlowState.Completed));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void CreateFlow_NotifiesEditorObserversWithoutRetainingRuntimeFlow()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var observed = default(GameFlow);
            Action<GameFlowAsset, GameFlow> onFlowCreated = (createdAsset, flow) =>
            {
                if (createdAsset == asset) observed = flow;
            };
            GameFlowAsset.FlowCreated += onFlowCreated;
            try
            {
                asset.AddStep(new GameFlowStepDefinition
                {
                    Name = "步骤",
                    Handler = new RecordingHandler("step", new List<string>())
                });

                var created = asset.CreateFlow();

                Assert.That(observed, Is.SameAs(created));
                Assert.That(typeof(GameFlowAsset).GetField("m_LastCreatedFlow",
                    BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
            }
            finally
            {
                GameFlowAsset.FlowCreated -= onFlowCreated;
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
#endif

        [Test]
        public void InstallModulesHandler_RequiresModuleContextAndManifest()
        {
            var handler = new GameFlowInstallModulesHandler();
            Assert.Throws<InvalidOperationException>(() => handler.ExecuteAsync(
                new GameFlowContext(), CancellationToken.None, _ => { }).GetAwaiter().GetResult());
        }

        [Test]
        public async Task CreateFlow_RunsHandlerLifecycle()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var calls = new List<string>();
            try
            {
                asset.AddStep(new GameFlowStepDefinition
                {
                    Name = "生命周期",
                    Handler = new LifecycleHandler(calls)
                });

                var flow = asset.CreateFlow();
                await flow.RunAsync();

                Assert.That(calls, Is.EqualTo(new[] { "enter", "execute", "leave:success" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CreateFlow_LeavesFailedHandlerWithoutSwallowingFailure()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var calls = new List<string>();
            try
            {
                asset.AddStep(new GameFlowStepDefinition
                {
                    Name = "失败",
                    Handler = new FailingLifecycleHandler(calls)
                });

                var flow = asset.CreateFlow();
                var failure = Assert.ThrowsAsync<InvalidOperationException>(() => flow.RunAsync().AsTask());

                Assert.That(failure.Message, Is.EqualTo("step failed"));
                Assert.That(calls, Is.EqualTo(new[] { "enter", "execute", "leave:failure" }));
                Assert.That(flow.State, Is.EqualTo(GameFlowState.Failed));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CreateFlow_CleansUpWhenEnterFails()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var calls = new List<string>();
            try
            {
                asset.AddStep(new GameFlowStepDefinition
                {
                    Name = "进入失败",
                    Handler = new EnterFailingLifecycleHandler(calls)
                });

                Assert.ThrowsAsync<InvalidOperationException>(() => asset.CreateFlow().RunAsync().AsTask());
                Assert.That(calls, Is.EqualTo(new[] { "enter", "leave:failure" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CreateFlow_RejectsEnabledStepWithoutHandler()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            try
            {
                asset.AddStep(new GameFlowStepDefinition { Name = "缺少处理器" });
                Assert.Throws<InvalidOperationException>(() => asset.CreateFlow());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void StepDefinition_PreservesEditorPositionInRuntimeSerializationLayout()
        {
            var position = typeof(GameFlowStepDefinition).GetField("m_Position",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(position, Is.Not.Null);
            Assert.That(position.FieldType, Is.EqualTo(typeof(Vector2)));
        }

        [Test]
        public void AddStep_RejectsEmptyAndDuplicateIds()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            try
            {
                Assert.Throws<ArgumentException>(() => asset.AddStep(
                    new GameFlowStepDefinition { Id = string.Empty }));

                asset.AddStep(new GameFlowStepDefinition { Id = "shared" });
                Assert.Throws<InvalidOperationException>(() => asset.AddStep(
                    new GameFlowStepDefinition { Id = "shared" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void AssetSteps_CannotBeMutatedThroughDefinitionView()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            try
            {
                asset.AddStep(new GameFlowStepDefinition { Name = "步骤" });

                Assert.That(asset.Steps, Is.Not.TypeOf<List<GameFlowStepDefinition>>());
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<GameFlowStepDefinition>)asset.Steps).Clear());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public async Task CreateFlow_UsesBooleanConditionBranches()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var calls = new List<string>();
            try
            {
                var entry = new GameFlowStepDefinition
                {
                    Name = "入口",
                    Handler = new RecordingHandler("entry", calls)
                };
                var skipped = new GameFlowStepDefinition
                {
                    Name = "跳过",
                    Handler = new RecordingHandler("skipped", calls)
                };
                var selected = new GameFlowStepDefinition
                {
                    Name = "选择",
                    Handler = new RecordingHandler("selected", calls)
                };
                asset.AddStep(entry);
                asset.AddStep(skipped);
                asset.AddStep(selected);
                var condition = new TrueCondition();
                entry.Condition = condition;
                entry.TrueTargetStepId = selected.Id;
                entry.FalseTargetStepId = skipped.Id;

                var flow = asset.CreateFlow();
                await flow.RunAsync();

                Assert.That(calls, Is.EqualTo(new[] { "entry", "selected" }));
                Assert.That(condition.EvaluationCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public async Task CreateFlow_SelectsFalseBranch()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var calls = new List<string>();
            try
            {
                var entry = new GameFlowStepDefinition
                {
                    Name = "入口",
                    Handler = new RecordingHandler("entry", calls),
                    Condition = new TrueCondition(false)
                };
                var trueStep = new GameFlowStepDefinition
                {
                    Name = "True",
                    Handler = new RecordingHandler("true", calls)
                };
                var falseStep = new GameFlowStepDefinition
                {
                    Name = "False",
                    Handler = new RecordingHandler("false", calls)
                };
                entry.TrueTargetStepId = trueStep.Id;
                entry.FalseTargetStepId = falseStep.Id;
                asset.AddStep(entry);
                asset.AddStep(trueStep);
                asset.AddStep(falseStep);

                await asset.CreateFlow().RunAsync();

                Assert.That(calls, Is.EqualTo(new[] { "entry", "false" }));
                Assert.That(((TrueCondition)entry.Condition).EvaluationCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public async Task CreateFlow_UsesDefaultOutputWhenNoConditionIsSelected()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            var calls = new List<string>();
            try
            {
                var first = new GameFlowStepDefinition
                {
                    Name = "第一步",
                    Handler = new RecordingHandler("first", calls)
                };
                var second = new GameFlowStepDefinition
                {
                    Name = "第二步",
                    Handler = new RecordingHandler("second", calls)
                };
                first.TrueTargetStepId = second.Id;
                asset.AddStep(first);
                asset.AddStep(second);

                await asset.CreateFlow().RunAsync();

                Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CreateFlow_RejectsTransitionToMissingStep()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            try
            {
                var entry = new GameFlowStepDefinition
                {
                    Name = "入口",
                    Handler = new RecordingHandler("entry", new List<string>())
                };
                asset.AddStep(entry);
                entry.Condition = new TrueCondition();
                entry.TrueTargetStepId = "missing-step";

                Assert.Throws<InvalidOperationException>(() => asset.CreateFlow());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CreateFlow_RejectsTransitionToDisabledStep()
        {
            var asset = ScriptableObject.CreateInstance<GameFlowAsset>();
            try
            {
                var entry = new GameFlowStepDefinition
                {
                    Name = "入口",
                    Handler = new RecordingHandler("entry", new List<string>())
                };
                var disabled = new GameFlowStepDefinition
                {
                    Name = "禁用",
                    Enabled = false,
                    Handler = new RecordingHandler("disabled", new List<string>())
                };
                entry.TrueTargetStepId = disabled.Id;
                asset.AddStep(entry);
                asset.AddStep(disabled);

                var exception = Assert.Throws<InvalidOperationException>(() => asset.CreateFlow());
                Assert.That(exception.Message, Does.Contain("targets missing step"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Serializable]
        private sealed class RecordingHandler : GameFlowStepHandler
        {
            private readonly string m_Name;
            [NonSerialized] private readonly List<string> m_Calls;

            public RecordingHandler(string name, List<string> calls)
            {
                m_Name = name;
                m_Calls = calls;
            }

            public override ValueTask ExecuteAsync(GameFlowContext context,
                CancellationToken ct, Action<float> reportProgress)
            {
                m_Calls.Add(m_Name);
                return default;
            }
        }

        [Serializable]
        private sealed class ContextHandler : GameFlowStepHandler
        {
            private readonly object m_Expected;
            [NonSerialized] private readonly List<string> m_Calls;

            public ContextHandler(object expected, List<string> calls)
            {
                m_Expected = expected;
                m_Calls = calls;
            }

            public override ValueTask ExecuteAsync(GameFlowContext context,
                CancellationToken ct, Action<float> reportProgress)
            {
                if (!ReferenceEquals(context?.UserData, m_Expected))
                    throw new InvalidOperationException("Flow context was not passed to the handler.");
                m_Calls.Add("start");
                return default;
            }
        }

        [Serializable]
        private sealed class LifecycleHandler : GameFlowStepHandler
        {
            [NonSerialized] private readonly List<string> m_Calls;

            public LifecycleHandler(List<string> calls) => m_Calls = calls;

            public override ValueTask OnEnterAsync(GameFlowContext context, CancellationToken ct)
            {
                m_Calls.Add("enter");
                return default;
            }

            public override ValueTask ExecuteAsync(GameFlowContext context,
                CancellationToken ct, Action<float> reportProgress)
            {
                m_Calls.Add("execute");
                return default;
            }

            public override ValueTask OnLeaveAsync(GameFlowContext context,
                CancellationToken ct, bool succeeded)
            {
                m_Calls.Add($"leave:{(succeeded ? "success" : "failure")}");
                return default;
            }
        }

        [Serializable]
        private sealed class EnterFailingLifecycleHandler : GameFlowStepHandler
        {
            [NonSerialized] private readonly List<string> m_Calls;

            public EnterFailingLifecycleHandler(List<string> calls) => m_Calls = calls;

            public override ValueTask OnEnterAsync(GameFlowContext context, CancellationToken ct)
            {
                m_Calls.Add("enter");
                throw new InvalidOperationException("enter failed");
            }

            public override ValueTask ExecuteAsync(GameFlowContext context,
                CancellationToken ct, Action<float> reportProgress)
            {
                m_Calls.Add("execute");
                return default;
            }

            public override ValueTask OnLeaveAsync(GameFlowContext context,
                CancellationToken ct, bool succeeded)
            {
                m_Calls.Add($"leave:{(succeeded ? "success" : "failure")}");
                return default;
            }
        }

        [Serializable]
        private sealed class TrueCondition : GameFlowCondition
        {
            private readonly bool m_Result;
            public int EvaluationCount { get; private set; }

            public TrueCondition(bool result = true) => m_Result = result;

            public override bool Evaluate(GameFlowContext context)
            {
                EvaluationCount++;
                return m_Result;
            }
        }

        [Serializable]
        private sealed class FailingLifecycleHandler : GameFlowStepHandler
        {
            [NonSerialized] private readonly List<string> m_Calls;

            public FailingLifecycleHandler(List<string> calls) => m_Calls = calls;

            public override ValueTask OnEnterAsync(GameFlowContext context, CancellationToken ct)
            {
                m_Calls.Add("enter");
                return default;
            }

            public override ValueTask ExecuteAsync(GameFlowContext context,
                CancellationToken ct, Action<float> reportProgress)
            {
                m_Calls.Add("execute");
                throw new InvalidOperationException("step failed");
            }

            public override ValueTask OnLeaveAsync(GameFlowContext context,
                CancellationToken ct, bool succeeded)
            {
                m_Calls.Add($"leave:{(succeeded ? "success" : "failure")}");
                return default;
            }
        }
    }
}

#endif
