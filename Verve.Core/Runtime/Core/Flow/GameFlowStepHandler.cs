#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    ///   <para>流程类型的编辑器展示名称。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GameFlowDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public GameFlowDisplayNameAttribute(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A flow display name is required.", nameof(displayName));
            DisplayName = displayName.Trim();
        }
    }

    /// <summary>
    ///   <para>流程运行上下文。</para>
    /// </summary>
    public sealed class GameFlowContext
    {
        /// <summary><para>模块容器；没有模块容器时为空。</para></summary>
        public GameModules Modules { get; }
        /// <summary><para>由调用方传入的业务数据或状态对象。</para></summary>
        public object UserData { get; }

        /// <summary>
        ///   <para>创建流程上下文。</para>
        /// </summary>
        /// <param name="modules">模块容器。</param>
        /// <param name="userData">业务数据或状态对象。</param>
        public GameFlowContext(GameModules modules = null, object userData = null)
        {
            Modules = modules;
            UserData = userData;
        }
    }

    /// <summary>
    ///   <para>可由流程资产配置的流程步骤处理器。</para>
    /// </summary>
    [Serializable]
    public abstract class GameFlowStepHandler
    {
        /// <summary>
        ///   <para>步骤进入时调用；派生类可用于准备临时状态。</para>
        /// </summary>
        public virtual ValueTask OnEnterAsync(GameFlowContext context, CancellationToken ct)
            => default;

        /// <summary>
        ///   <para>执行步骤。</para>
        /// </summary>
        /// <param name="context">流程上下文。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="reportProgress">报告当前步骤进度的回调，范围为 0 到 1。</param>
        public abstract ValueTask ExecuteAsync(GameFlowContext context,
            CancellationToken ct, Action<float> reportProgress);

        /// <summary>
        ///   <para>步骤执行阶段结束时调用；执行失败时参数为 false。</para>
        /// </summary>
        /// <param name="succeeded">步骤是否成功完成。</param>
        public virtual ValueTask OnLeaveAsync(GameFlowContext context,
            CancellationToken ct, bool succeeded)
            => default;

        /// <summary>
        ///   <para>按进入、执行、离开顺序运行步骤生命周期。</para>
        /// </summary>
        internal async ValueTask ExecuteWithLifecycleAsync(GameFlowContext context,
            CancellationToken ct, Action<float> reportProgress)
        {
            try
            {
                await OnEnterAsync(context, ct);
                await ExecuteAsync(context, ct, reportProgress);
            }
            catch
            {
                await OnLeaveAsync(context, ct, succeeded: false);
                throw;
            }

            await OnLeaveAsync(context, ct, succeeded: true);
        }
    }

    /// <summary>
    ///   <para>流程节点的布尔判断条件。</para>
    /// </summary>
    [Serializable]
    public abstract class GameFlowCondition
    {
        /// <summary>
        ///   <para>返回 true 时选择 True 分支，返回 false 时选择 False 分支。</para>
        /// </summary>
        public abstract bool Evaluate(GameFlowContext context);
    }

    /// <summary>
    ///   <para>流程中的可序列化步骤定义。</para>
    /// </summary>
    [Serializable]
    internal sealed class GameFlowStepDefinition
    {
        [SerializeField] private string m_Id = Guid.NewGuid().ToString("N");
        [SerializeField] private string m_Name = "流程步骤";
        [SerializeField] private bool m_Enabled = true;
        [SerializeField] private Vector2 m_Position = new(80f, 80f);
        [SerializeReference] private GameFlowStepHandler m_Handler;
        [SerializeReference] private GameFlowCondition m_Condition;
        [SerializeField] private string m_TrueTargetStepId;
        [SerializeField] private string m_FalseTargetStepId;

        /// <summary>
        ///   <para>步骤稳定标识。</para>
        /// </summary>
        public string Id { get => m_Id; set => m_Id = value; }

        /// <summary>
        ///   <para>步骤显示名称。</para>
        /// </summary>
        public string Name { get => m_Name; set => m_Name = value; }

        /// <summary>
        ///   <para>是否执行此步骤。</para>
        /// </summary>
        public bool Enabled { get => m_Enabled; set => m_Enabled = value; }

        /// <summary>
        ///   <para>步骤处理器。</para>
        /// </summary>
        public GameFlowStepHandler Handler { get => m_Handler; set => m_Handler = value; }

        /// <summary>
        ///   <para>条件。</para>
        /// </summary>
        public GameFlowCondition Condition { get => m_Condition; set => m_Condition = value; }

        public string TrueTargetStepId { get => m_TrueTargetStepId; set => m_TrueTargetStepId = value; }
        public string FalseTargetStepId { get => m_FalseTargetStepId; set => m_FalseTargetStepId = value; }
    }

    /// <summary>
    ///   <para>安装模块清单的内置流程步骤。</para>
    /// </summary>
    [Serializable]
    [GameFlowDisplayName("安装模块")]
    public sealed class GameFlowInstallModulesHandler : GameFlowStepHandler
    {
        [SerializeField] private GameModuleManifestAsset m_Manifest;

        /// <summary>
        ///   <para>模块清单资产。</para>
        /// </summary>
        public GameModuleManifestAsset Manifest
        {
            get => m_Manifest;
            set => m_Manifest = value;
        }

        public override ValueTask ExecuteAsync(GameFlowContext context,
            CancellationToken ct, Action<float> reportProgress)
        {
            if (context?.Modules == null)
                throw new InvalidOperationException("A GameModules context is required to install the flow module manifest.");
            if (m_Manifest == null)
                throw new InvalidOperationException("Flow module manifest is not assigned.");

            return context.Modules.InstallFromManifestAsync(m_Manifest.ToManifest(), ct);
        }
    }
}

#endif