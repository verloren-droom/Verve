#if UNITY_EDITOR

namespace VerveEditor.UniEx.AI
{
    using UnityEngine;
    using UnityEditor;
    using VerveUniEx.AI;
    
    
    public static class NavMeshMoveBTNodeExtension
    {
        public static void DrawGizmos(this ref NavMeshMoveBTNode self, ref BTNodeDebugContext ctx)
        {
            if (self.data.agent == null || self.data.targets == null || self.data.targets.Length == 0) return;
            
            for (int i = 0; i < self.data.targets.Length; i++)
            {
                if (self.data.targets[i] == null) continue;
                
                Vector3 pos = self.data.targets[i];
                Gizmos.color = (i == self.CurrentIndex) ? Color.green : Color.blue;
                Gizmos.DrawSphere(pos, 0.25f);
                
                Handles.Label(pos + Vector3.up * 0.5f, $"[{i}]", 
                    new GUIStyle { normal = { textColor = Color.white }, fontSize = 10 });
            }
            
            if (self.data.agent != null && self.IsMoving)
            {
                if (self.CurrentIndex < self.data.targets.Length && 
                    self.data.targets[self.CurrentIndex] != null)
                {
                    Vector3 targetPos = self.data.targets[self.CurrentIndex];
                    Vector3 agentPos = self.data.agent.transform.position;
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(agentPos, targetPos);
                    Gizmos.DrawWireSphere(targetPos, 0.3f);
                }
                
                if (self.data.agent.path != null)
                {
                    Gizmos.color = Color.cyan;
                    Vector3[] path = self.data.agent.path.corners;
                    for (int i = 0; i < path.Length - 1; i++)
                    {
                        Gizmos.DrawLine(path[i], path[i + 1]);
                    }
                }
            }
            
            if (self.data.agent != null && self.IsMoving && self.data.agent.velocity != Vector3.zero)
            {
                Vector3 pos = self.data.agent.transform.position + Vector3.up * 0.1f;
                Vector3 dir = self.data.agent.velocity.normalized;
                
                Handles.color = Color.red;
                Handles.ArrowHandleCap(0, pos, Quaternion.LookRotation(dir), 1f, EventType.Repaint);
            }
        }
    }
}

#endif