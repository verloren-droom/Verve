#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using System.Threading;
    using System.Threading.Tasks;
    
    /// <summary>
    ///  <para>判断条件事件</para>
    /// </summary>
    [Serializable]
    public class BranchConditionEvent : UnityEvent<System.Action<bool>> { }

    /// <summary>
    ///  <para>判断节点</para>
    /// </summary>
    [Serializable, GameFlowNode("Basic/Branch", "Branch", "判断节点")]
    public class BranchNode : GameFlowNode
    {
        [SerializeField, Tooltip("条件判断事件")] private BranchConditionEvent m_ConditionEvent = new BranchConditionEvent();
        
        private readonly Func<bool> m_Condition;
        
        [GameFlowOutput("True")] private readonly IGameFlowNode m_TrueNode;
        [GameFlowOutput("False")] private readonly IGameFlowNode m_FalseNode;
        
        public BranchNode(): base() { }
        
        public BranchNode(Func<bool> condition, IGameFlowNode trueNode, IGameFlowNode falseNode = null, string nodeID = null) : base(nodeID)
        {
            m_Condition = condition;
            m_TrueNode = trueNode;
            m_FalseNode = falseNode;
        }

        protected override async Task OnExecute(CancellationToken ct = default)
        {
            bool conditionResult = false;
            
            if (m_Condition != null)
            {
                conditionResult = m_Condition.Invoke();
            }
            else if (m_ConditionEvent != null && m_ConditionEvent.GetPersistentEventCount() > 0)
            {
                var tcs = new TaskCompletionSource<bool>();
    
                Action<bool> callback = (result) => {
                    tcs.SetResult(result);
                };
                m_ConditionEvent.Invoke(callback);
    
                conditionResult = await tcs.Task;
            }

            if (conditionResult)
            {
                if (m_TrueNode != null)
                    await m_TrueNode.Execute(ct);
            }
            else
            {
                if (m_FalseNode != null)
                    await m_FalseNode.Execute(ct);
            }
            
            MarkCompleted();
        }

        protected override void OnCancel()
        {
            m_TrueNode?.Cancel();
            m_FalseNode?.Cancel();
        }
        
        protected override void OnReset()
        {
            m_TrueNode?.Reset();
            m_FalseNode?.Reset();
        }
    }
}

#endif