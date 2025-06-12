#if UNITY_EDITOR

namespace VerveEditor.UniEx.AI
{
    using Verve.AI;
    using UnityEngine;
    using System.Linq;
    using VerveUniEx.AI;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    public static class BehaviorTreeExtension
    {
        private static readonly ConditionalWeakTable<IBehaviorTree, List<IDebuggableNode>> s_DebugCache 
            = new ConditionalWeakTable<IBehaviorTree, List<IDebuggableNode>>();
    
        
        /// <summary>
        /// 绘制可视化调试图形（仅在运行时生效）
        /// </summary>
        /// <param name="self"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawGizmos(this IBehaviorTree self)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
            
            if (!s_DebugCache.TryGetValue(self, out var nodes))
            {
                nodes = self.FindNodes(n => n is IDebuggableNode)
                    .Cast<IDebuggableNode>()
                    .ToList();
                s_DebugCache.Add(self, nodes);
            }

            var debugContext = new NodeDebugContext
            {
                BB = self.BB
            };
    
            foreach (var node in nodes)
            {
                if (node?.IsDebug == true)
                {
                    // node.DebugTarget?.AddComponent<VisualizedDebuggableNode>().Init(node, ref debugContext);
                    node.DrawGizmos(ref debugContext);
                }
            }
#endif
        }
    }
}

#endif