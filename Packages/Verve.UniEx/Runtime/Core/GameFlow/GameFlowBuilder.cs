#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;


    /// <summary>
    ///  <para>游戏流程构造器</para>
    /// </summary>
    [Serializable]
    public class GameFlowBuilder
    {
        private readonly ICompositeGameFlowNode m_RootFlow;
        
        public GameFlowBuilder(IGameFlowNode rootFlow = null, string flowID = null)
        {
            m_RootFlow = (rootFlow as ICompositeGameFlowNode) ?? new SequenceNode(flowID);
        }
        
        /// <summary>
        ///  <para>添加游戏流程节点</para>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public GameFlowBuilder Append(IGameFlowNode node)
        {
            m_RootFlow.Append(node);
            return this;
        }

        /// <summary>
        ///  <para>添加延时节点</para>
        /// </summary>
        public GameFlowBuilder Delay(float duration)
        {
            Append(new DelayNode(duration));
            return this;
        }

        /// <summary>
        ///  <para>添加条件游戏流程节点</para>
        /// </summary>
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
        
        public IGameFlowNode Build() => m_RootFlow;
    }
}

#endif