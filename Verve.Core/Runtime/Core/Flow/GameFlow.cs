namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>流程状态。</para>
    /// </summary>
    public enum GameFlowState : byte
    {
        /// <summary>
        ///   <para>尚未执行。</para>
        /// </summary>
        Created = 0,
        /// <summary>
        ///   <para>正在执行。</para>
        /// </summary>
        Running = 1,
        /// <summary>
        ///   <para>全部步骤已完成。</para>
        /// </summary>
        Completed = 2,
        /// <summary>
        ///   <para>某个步骤失败。</para>
        /// </summary>
        Failed = 3,
        /// <summary>
        ///   <para>流程被取消。</para>
        /// </summary>
        Canceled = 4,
    }

    /// <summary>
    ///   <para>流程步骤状态。</para>
    /// </summary>
    public enum GameFlowStepState : byte
    {
        /// <summary>
        ///   <para>尚未执行。</para>
        /// </summary>
        NotExecuted = 0,
        /// <summary>
        ///   <para>当前正在执行。</para>
        /// </summary>
        Current = 1,
        /// <summary>
        ///   <para>已经成功执行。</para>
        /// </summary>
        Executed = 2,
    }

    /// <summary>
    ///   <para>流程进度。</para>
    /// </summary>
    public readonly struct GameFlowProgress
    {
        /// <summary>
        ///   <para>当前步骤的从零开始索引。</para>
        /// </summary>
        public int StepIndex { get; }
        /// <summary>
        ///   <para>步骤总数。</para>
        /// </summary>
        public int StepCount { get; }
        /// <summary>
        ///   <para>当前步骤名称。</para>
        /// </summary>
        public string StepName { get; }
        /// <summary>
        ///   <para>当前步骤进度，范围为 0 到 1。</para>
        /// </summary>
        public float StepProgress { get; }

        internal GameFlowProgress(int stepIndex, int stepCount, string stepName, float stepProgress)
        {
            StepIndex = stepIndex;
            StepCount = stepCount;
            StepName = stepName;
            StepProgress = stepProgress;
        }
    }

    /// <summary>
    ///   <para>一个流程步骤。</para>
    /// </summary>
    public sealed class GameFlowStep
    {
        private readonly List<GameFlowStepTransition> m_Transitions = new();
        private readonly IReadOnlyList<GameFlowStepTransition> m_TransitionList;
        private Func<CancellationToken, Action<float>, ValueTask> m_Execute;
        private Func<bool> m_BranchCondition;
        private GameFlowStep m_TrueTarget;
        private GameFlowStep m_FalseTarget;

        /// <summary>
        ///   <para>步骤名称。</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///   <para>从当前步骤出发的转换。</para>
        /// </summary>
        public IReadOnlyList<GameFlowStepTransition> Transitions => m_TransitionList;

        internal Func<CancellationToken, Action<float>, ValueTask> Execute => m_Execute;

        internal GameFlowStep(string name, Func<CancellationToken, Action<float>, ValueTask> execute)
        {
            Name = name;
            m_Execute = execute;
            m_TransitionList = m_Transitions.AsReadOnly();
        }

        internal void AddTransition(GameFlowStep target, Func<bool> condition, string label)
        {
            if (m_BranchCondition != null)
                throw new InvalidOperationException("A flow step cannot mix branches and transitions.");
            m_Transitions.Add(new GameFlowStepTransition(target, condition, label));
        }

        internal void SetBranch(Func<bool> condition, GameFlowStep trueTarget, GameFlowStep falseTarget)
        {
            if (m_Transitions.Count != 0 || m_BranchCondition != null)
                throw new InvalidOperationException("A flow step cannot mix branches and transitions.");
            m_BranchCondition = condition ?? throw new ArgumentNullException(nameof(condition));
            m_TrueTarget = trueTarget;
            m_FalseTarget = falseTarget;
        }

        internal bool HasBranch => m_BranchCondition != null;
        internal GameFlowStep SelectBranch() => m_BranchCondition() ? m_TrueTarget : m_FalseTarget;

        internal void ReleaseRuntimeReferences()
        {
            m_Execute = null;
            m_BranchCondition = null;
            m_TrueTarget = null;
            m_FalseTarget = null;
            for (var i = 0; i < m_Transitions.Count; i++)
                m_Transitions[i].ReleaseRuntimeReferences();
        }

        public override string ToString() => Name;
    }

    /// <summary>
    ///   <para>流程运行时转换。</para>
    /// </summary>
    public sealed class GameFlowStepTransition
    {
        private Func<bool> m_Condition;

        internal GameFlowStepTransition(GameFlowStep target, Func<bool> condition, string label)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            m_Condition = condition;
            Label = string.IsNullOrWhiteSpace(label) ? "转换" : label.Trim();
        }

        /// <summary>
        ///   <para>目标步骤。</para>
        /// </summary>
        public GameFlowStep Target { get; }

        /// <summary>
        ///   <para>转换名称。</para>
        /// </summary>
        public string Label { get; }

        internal bool IsAvailable() => m_Condition == null || m_Condition();

        internal void ReleaseRuntimeReferences() => m_Condition = null;
    }

    /// <summary>
    ///   <para>可取消、支持步骤进度和条件转换的流程。</para>
    /// </summary>
    /// <remarks>
    ///   <para>步骤适合承载初始化、版本检查、更新下载和进入游戏等异步操作。</para>
    ///   <para>流程会等待每个步骤返回的 ValueTask 完成后再选择下一步；等待期间不会阻塞主线程。处理器必须在实际操作完成后才完成 ValueTask。</para>
    ///   <para>流程实例只允许执行一次；失败或取消后不能继续复用，以避免已完成步骤的外部状态不明确。</para>
    /// </remarks>
    public sealed class GameFlow
    {
        private readonly List<GameFlowStep> m_Steps = new();
        private readonly IReadOnlyList<GameFlowStep> m_StepList;
        private readonly List<GameFlowStepState> m_StepStates = new();
        private readonly IReadOnlyList<GameFlowStepState> m_StepStateList;
        private int m_State;
        private int m_CurrentStepIndex = -1;
        private bool m_HasExplicitTransitions;

        /// <summary>
        ///   <para>创建空流程。</para>
        /// </summary>
        public GameFlow()
        {
            m_StepList = m_Steps.AsReadOnly();
            m_StepStateList = m_StepStates.AsReadOnly();
        }

        /// <summary>
        ///   <para>流程步骤。</para>
        /// </summary>
        public IReadOnlyList<GameFlowStep> Steps => m_StepList;

        /// <summary>
        ///   <para>步骤数量。</para>
        /// </summary>
        public int Count => m_Steps.Count;

        /// <summary>
        ///   <para>各步骤的运行状态，与 <see cref="Steps"/> 使用相同索引。</para>
        /// </summary>
        public IReadOnlyList<GameFlowStepState> StepStates => m_StepStateList;

        /// <summary>
        ///   <para>当前流程状态。</para>
        /// </summary>
        public GameFlowState State => (GameFlowState)Volatile.Read(ref m_State);

        /// <summary>
        ///   <para>当前步骤索引；流程尚未开始时为 -1。</para>
        /// </summary>
        public int CurrentStepIndex => Volatile.Read(ref m_CurrentStepIndex);

        /// <summary>
        ///   <para>添加一个不需要细分进度的异步步骤。</para>
        /// </summary>
        /// <param name="name">步骤名称。</param>
        /// <param name="execute">步骤执行委托。</param>
        public GameFlowStep Add(string name, Func<CancellationToken, ValueTask> execute)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            return Add(name, (ct, _) => execute(ct));
        }

        /// <summary>
        ///   <para>添加一个可以报告细分进度的异步步骤。</para>
        /// </summary>
        /// <param name="name">步骤名称。</param>
        /// <param name="execute">步骤执行委托；第二个参数用于报告当前步骤的 0 到 1 进度。</param>
        public GameFlowStep Add(string name,
            Func<CancellationToken, Action<float>, ValueTask> execute)
        {
            ThrowIfNotEditable();
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Flow step name cannot be empty.", nameof(name));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            var step = new GameFlowStep(name.Trim(), execute);
            m_Steps.Add(step);
            m_StepStates.Add(GameFlowStepState.NotExecuted);
            return step;
        }

        /// <summary>
        ///   <para>连接两个步骤；转换按添加顺序判断，第一个满足条件的转换会被执行。</para>
        /// </summary>
        /// <param name="from">起始步骤。</param>
        /// <param name="to">目标步骤。</param>
        /// <param name="condition">转换条件；为空表示无条件。</param>
        /// <param name="label">转换名称。</param>
        public void Connect(GameFlowStep from, GameFlowStep to,
            Func<bool> condition = null, string label = "转换")
        {
            ThrowIfNotEditable();
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (!m_Steps.Contains(from)) throw new ArgumentException("The source step does not belong to this flow.", nameof(from));
            if (!m_Steps.Contains(to)) throw new ArgumentException("The target step does not belong to this flow.", nameof(to));

            m_HasExplicitTransitions = true;
            from.AddTransition(to, condition, label);
        }

        internal void Branch(GameFlowStep from, Func<bool> condition,
            GameFlowStep trueTarget, GameFlowStep falseTarget)
        {
            ThrowIfNotEditable();
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (!m_Steps.Contains(from)) throw new ArgumentException("The source step does not belong to this flow.", nameof(from));
            if (trueTarget != null && !m_Steps.Contains(trueTarget))
                throw new ArgumentException("The true target does not belong to this flow.", nameof(trueTarget));
            if (falseTarget != null && !m_Steps.Contains(falseTarget))
                throw new ArgumentException("The false target does not belong to this flow.", nameof(falseTarget));

            m_HasExplicitTransitions = true;
            from.SetBranch(condition, trueTarget, falseTarget);
        }

        /// <summary>
        ///   <para>执行流程步骤；没有显式转换时按添加顺序执行，存在转换时按第一个满足条件的转换继续。</para>
        /// </summary>
        /// <param name="onProgress">步骤进度回调；为空时不报告进度。</param>
        /// <param name="ct">取消令牌。</param>
        public async ValueTask RunAsync(Action<GameFlowProgress> onProgress = null, CancellationToken ct = default)
        {
            if (Interlocked.CompareExchange(ref m_State, (int)GameFlowState.Running,
                    (int)GameFlowState.Created) != (int)GameFlowState.Created)
            {
                throw new InvalidOperationException("A flow can only be run once.");
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                var index = 0;
                while (index >= 0)
                {
                    if (index >= m_Steps.Count)
                    {
                        if (!m_HasExplicitTransitions && index == m_Steps.Count)
                            break;
                        throw new InvalidOperationException($"Flow reached invalid step index {index}.");
                    }

                    ct.ThrowIfCancellationRequested();
                    var step = m_Steps[index];
                    Volatile.Write(ref m_CurrentStepIndex, index);
                    m_StepStates[index] = GameFlowStepState.Current;
                    var stepProgress = 0f;
                    ReportProgress(onProgress, index, step, stepProgress);

                    void ReportStepProgress(float value)
                    {
                        if (!Game.NumberUtility.IsFinite(value))
                            throw new ArgumentOutOfRangeException(nameof(value), value, "Flow step progress must be finite.");
                        stepProgress = Math.Clamp(value, 0, 1);
                        ReportProgress(onProgress, index, step, stepProgress);
                    }

                    await step.Execute(ct, ReportStepProgress);
                    ct.ThrowIfCancellationRequested();
                    if (stepProgress < 1f) ReportProgress(onProgress, index, step, 1f);
                    m_StepStates[index] = GameFlowStepState.Executed;

                    if (step.HasBranch)
                    {
                        var target = step.SelectBranch();
                        if (target == null) break;
                        index = m_Steps.IndexOf(target);
                        continue;
                    }

                    if (step.Transitions.Count == 0)
                    {
                        if (m_HasExplicitTransitions) break;
                        index++;
                        continue;
                    }

                    var next = -1;
                    for (var transitionIndex = 0; transitionIndex < step.Transitions.Count; transitionIndex++)
                    {
                        var transition = step.Transitions[transitionIndex];
                        if (!transition.IsAvailable()) continue;
                        next = m_Steps.IndexOf(transition.Target);
                        break;
                    }

                    if (next < 0)
                        throw new InvalidOperationException($"Flow step '{step.Name}' has no matching transition.");

                    index = next;
                }

                Volatile.Write(ref m_State, (int)GameFlowState.Completed);
            }
            catch (OperationCanceledException)
            {
                Volatile.Write(ref m_State, (int)GameFlowState.Canceled);
                throw;
            }
            catch
            {
                Volatile.Write(ref m_State, (int)GameFlowState.Failed);
                throw;
            }
            finally
            {
                for (var i = 0; i < m_Steps.Count; i++)
                    m_Steps[i].ReleaseRuntimeReferences();
            }
        }

        private void ReportProgress(Action<GameFlowProgress> onProgress, int stepIndex,
            GameFlowStep step, float stepProgress)
        {
            if (onProgress == null) return;
            onProgress(new GameFlowProgress(stepIndex, m_Steps.Count, step.Name, stepProgress));
        }

        private void ThrowIfNotEditable()
        {
            if (Volatile.Read(ref m_State) != (int)GameFlowState.Created)
                throw new InvalidOperationException("Flow steps cannot be changed after execution has started.");
        }
    }
}