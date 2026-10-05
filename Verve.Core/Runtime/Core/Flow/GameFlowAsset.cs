#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>可在流程节点编辑器中配置的流程资产。</para>
    /// </summary>
    [CreateAssetMenu(fileName = "Flow", menuName = "Verve/流程")]
    public sealed class GameFlowAsset : ScriptableObject
    {
        [SerializeField] private List<GameFlowStepDefinition> m_Steps = new();
#if UNITY_EDITOR
        internal static event Action<GameFlowAsset, GameFlow> FlowCreated;
#endif
        [NonSerialized] private IReadOnlyList<GameFlowStepDefinition> m_StepList;

        /// <summary>
        ///   <para>流程步骤定义；顺序即执行顺序。</para>
        /// </summary>
        internal IReadOnlyList<GameFlowStepDefinition> Steps
        {
            get
            {
                if (m_Steps == null)
                    throw new InvalidOperationException("Flow asset step list cannot be null.");
                return m_StepList ??= m_Steps.AsReadOnly();
            }
        }

        private void OnEnable()
        {
            if (m_Steps != null)
                m_StepList = m_Steps.AsReadOnly();
        }

        /// <summary>
        ///   <para>添加一个流程步骤定义；通常由编辑器调用，运行时代码可用于测试或动态资产构建。</para>
        /// </summary>
        /// <param name="step">步骤定义。</param>
        internal void AddStep(GameFlowStepDefinition step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (string.IsNullOrWhiteSpace(step.Id))
                throw new ArgumentException("Flow step id cannot be empty.", nameof(step));
            if (ContainsStepId(m_Steps, step.Id))
                throw new InvalidOperationException($"Flow already contains step id '{step.Id}'.");
            m_Steps.Add(step);
        }

        /// <summary>
        ///   <para>将资产节点和条件分支创建为一次性运行流程。</para>
        /// </summary>
        /// <param name="context">流程上下文。</param>
        /// <returns>可执行的流程。</returns>
        public GameFlow CreateFlow(GameFlowContext context = null)
        {
            var flow = new GameFlow();
            var steps = Steps;
            ValidateStepIds(steps);
            var runtimeSteps = new Dictionary<string, GameFlowStep>(StringComparer.Ordinal);

            for (var i = 0; i < steps.Count; i++)
            {
                var definition = steps[i];
                if (!definition.Enabled) continue;
                if (definition.Handler == null)
                    throw new InvalidOperationException($"Flow step {i} has no handler.");

                var handler = definition.Handler;
                var runtimeStep = flow.Add(definition.Name, (ct, reportProgress) =>
                    handler.ExecuteWithLifecycleAsync(context, ct, reportProgress));
                runtimeSteps.Add(definition.Id, runtimeStep);
            }

            for (var i = 0; i < steps.Count; i++)
            {
                var definition = steps[i];
                if (!definition.Enabled) continue;
                var source = runtimeSteps[definition.Id];
                var trueTarget = ResolveTarget(definition.TrueTargetStepId, definition.Name,
                    runtimeSteps);
                var falseTarget = ResolveTarget(definition.FalseTargetStepId, definition.Name,
                    runtimeSteps);
                if (definition.Condition != null)
                {
                    flow.Branch(source, () => definition.Condition.Evaluate(context), trueTarget, falseTarget);
                }
                else if (trueTarget != null && falseTarget == null)
                {
                    flow.Connect(source, trueTarget);
                }
                else if (falseTarget != null)
                {
                    throw new InvalidOperationException(
                        $"Flow step '{definition.Name}' has a False target but no condition.");
                }
            }

#if UNITY_EDITOR
            FlowCreated?.Invoke(this, flow);
#endif
            return flow;
        }

        private static GameFlowStep ResolveTarget(string targetId, string sourceName,
            Dictionary<string, GameFlowStep> runtimeSteps)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return null;
            if (!runtimeSteps.TryGetValue(targetId, out var target))
                throw new InvalidOperationException(
                    $"Flow step '{sourceName}' targets missing step '{targetId}'.");

            return target;
        }

        private static void ValidateStepIds(IReadOnlyList<GameFlowStepDefinition> steps)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null)
                    throw new InvalidOperationException($"Flow contains an empty step at index {i}.");

                if (string.IsNullOrWhiteSpace(step.Id))
                    throw new InvalidOperationException($"Flow step at index {i} has no id.");
                if (!ids.Add(step.Id))
                    throw new InvalidOperationException($"Flow contains duplicate step id '{step.Id}'.");
            }
        }

        private static bool ContainsStepId(IReadOnlyList<GameFlowStepDefinition> steps, string id)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i]?.Id == id)
                    return true;
            }
            return false;
        }
    }
}

#endif