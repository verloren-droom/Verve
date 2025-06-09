#if UNITY_EDITOR

namespace VerveEditor.UniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using VerveUniEx.AI;

    
    [ExecuteInEditMode]
    public class VisualizedDebuggableNode : MonoBehaviour
    {
        private IDebuggableNode m_Node;
        private NodeDebugContext m_DebugCtx;
        

        public void Init(IDebuggableNode node, ref NodeDebugContext ctx)
        {
            m_Node = node;
            m_DebugCtx = ctx;
        }
        
        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }

        private void OnDrawGizmosSelected() => m_Node.DrawGizmos(ref m_DebugCtx);
    }
}

#endif