#if UNITY_EDITOR

namespace VerveEditor.UniEx.AI
{
    using UnityEngine;
    using UnityEditor;
    using VerveUniEx.AI;

    
    public static class TransformMoveBTNodeExtension
    {
        public static void DrawGizmos(this ref TransformMoveBTNode self, ref BTNodeDebugContext ctx)
        {
            if (self.data.targets == null || self.data.owner == null) return;
            
            for (int i = 0; i < self.data.targets.Length; i++)
            {
                if (self.data.targets[i] == null) continue;
                
                Vector3 pos = self.data.targets[i].position;
                Gizmos.color = (i == self.CurrentIndex) ? Color.green : Color.blue;
                Gizmos.DrawSphere(pos, 0.25f);
                
                Handles.Label(pos + Vector3.up * 0.3f, $"[{i}]", 
                    new GUIStyle { normal = { textColor = Color.white }, fontSize = 9 });
            }
            
            if (self.IsMoving && self.CurrentIndex < self.data.targets.Length && 
                self.data.targets[self.CurrentIndex] != null)
            {
                Vector3 ownerPos = self.data.owner.position;
                Vector3 targetPos = self.data.targets[self.CurrentIndex].position;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(ownerPos, targetPos);
                
                if (self.data.faceMovementDirection)
                {
                    Vector3 direction = (targetPos - ownerPos).normalized;
                    Handles.color = Color.red;
                    Handles.ArrowHandleCap(0, ownerPos, Quaternion.LookRotation(direction), 1f, EventType.Repaint);
                }
                
                float distance = Vector3.Distance(ownerPos, targetPos);
                Handles.Label(ownerPos + Vector3.up * 1f, 
                    $"目标: {self.CurrentIndex}\n" +
                    $"距离: {distance:F1}\n" +
                    $"速度: {self.data.moveSpeed}/s",
                    new GUIStyle { 
                        normal = { textColor = Color.yellow }, 
                        fontSize = 10 
                    });
            }
            
            if (self.data.ignoreAxes != 0)
            {
                Vector3 pos = self.data.owner.position;
                string ignoreText = "";
                if ((self.data.ignoreAxes & AxisFlags.X) != 0) ignoreText += "X";
                if ((self.data.ignoreAxes & AxisFlags.Y) != 0) ignoreText += "Y";
                if ((self.data.ignoreAxes & AxisFlags.Z) != 0) ignoreText += "Z";
                
                Handles.Label(pos + Vector3.up * 0.5f, $"忽略轴: {ignoreText}", 
                    new GUIStyle { normal = { textColor = Color.magenta }, fontSize = 9 });
            }
        }
    }
}

#endif