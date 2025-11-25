#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;


    /// <summary>
    ///   <para>游戏流程构造器</para>
    /// </summary>
    [Serializable]
    public class GameFlowBuilder
    {
        /// <summary>
        ///  <para>游戏流程根节点</para>
        /// </summary>
        private readonly ICompositeGameFlowNode m_RootFlow;
        
        public GameFlowBuilder(IGameFlowNode rootFlow = null, string flowID = null)
        {
            m_RootFlow = (rootFlow as ICompositeGameFlowNode) ?? new SequenceNode(flowID);
        }
        
        /// <summary>
        ///   <para>添加游戏流程节点</para>
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>
        ///   <para>游戏流程构造器</para>
        /// </returns>
        public GameFlowBuilder Append(IGameFlowNode node)
        {
            m_RootFlow.Append(node);
            return this;
        }

        /// <summary>
        ///   <para>添加延时节点</para>
        /// </summary>
        /// <param name="duration">延时时长</param>
        /// <returns>
        ///   <para>游戏流程构造器</para>
        /// </returns>
        public GameFlowBuilder Delay(float duration)
        {
            Append(new DelayNode(duration));
            return this;
        }

        /// <summary>
        ///   <para>添加条件游戏流程节点</para>
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="trueBranch">条件满足分支</param>
        /// <param name="falseBranch">条件不满足分支</param>
        /// <returns>
        ///   <para>游戏流程构造器</para>
        /// </returns>
        public GameFlowBuilder If(Func<bool> condition, Action<GameFlowBuilder> trueBranch, 
            Action<GameFlowBuilder> falseBranch = null)
        {
            var conditional = new BranchNode(condition, 
                new ActionNode(() =>
                {
                    var builder = new GameFlowBuilder();
                    trueBranch?.Invoke(builder);
                }), 
                falseBranch != null ? new ActionNode(() =>
                {
                    var builder = new GameFlowBuilder();
                    falseBranch?.Invoke(builder);
                }) : null);
            return Append(conditional);
        }
        
        /// <summary>
        ///   <para>构建游戏流程</para>
        /// </summary>
        /// <returns>
        ///   <para>游戏流程根节点</para>
        /// </returns>
        public IGameFlowNode Build() => m_RootFlow;
    }
}

#endif