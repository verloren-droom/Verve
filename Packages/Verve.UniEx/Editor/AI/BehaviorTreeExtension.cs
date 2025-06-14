#if UNITY_EDITOR

namespace VerveEditor.UniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using System.Linq;
    using VerveUniEx.AI;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    public static class BehaviorTreeExtension
    {
        private static readonly ConditionalWeakTable<IBehaviorTree, List<IBTNodeDebuggable>> s_DebugCache 
            = new ConditionalWeakTable<IBehaviorTree, List<IBTNodeDebuggable>>();


        /// <summary>
        /// 绘制可视化调试图形（仅在运行时生效）
        /// </summary>
        /// <param name="self"></param>
        public static void DrawGizmos(this IBehaviorTree self)
        {
            if (!Application.isPlaying) return;
            
            if (!s_DebugCache.TryGetValue(self, out var nodes))
            {
                nodes = self.FindNodes(n => n is IBTNodeDebuggable)
                    .Cast<IBTNodeDebuggable>()
                    .ToList();
                s_DebugCache.Add(self, nodes);
            }
            
            var debugContext = new BTNodeDebugContext
            {
                bb = self.BB
            };
            
            foreach (var node in nodes)
            {
                if (node?.IsDebug == true)
                {
                    var methods = node.GetType().GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        if (method.IsDefined(typeof(ExtensionAttribute), false) &&
                            method.Name == "DrawGizmos" &&
                            method.GetParameters().Length == 2 &&
                            method.GetParameters()[0].ParameterType == node.GetType().MakeByRefType() &&
                            method.GetParameters()[1].ParameterType == typeof(BTNodeDebugContext).MakeByRefType())
                        {
                            method.Invoke(null, new object[] { node, debugContext });
                            break;
                        }
                    }
                }
            }
        }
    }
}

#endif