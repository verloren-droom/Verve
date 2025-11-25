#if UNITY_EDITOR

namespace VerveEditor.AI
{
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx.AI;

    
    public static class VisionCheckBTNodeExtension
    {
        public static void DrawGizmos(this ref VisionCheckBTNode self, ref BTNodeDebugContext ctx)
        {
            if (!self.data.owner) return;
            
            Color gizmoColor = self.LastCheckResult ? Color.green : Color.red;
            Handles.color = gizmoColor;
            Gizmos.color = gizmoColor;
            
            Vector3 origin = self.data.owner.position + Vector3.up * self.data.eyeHeight;
            Quaternion leftRay = Quaternion.AngleAxis(-self.HalfFov, Vector3.up);
            Quaternion rightRay = Quaternion.AngleAxis(self.HalfFov, Vector3.up);
            Vector3 leftDir = leftRay * self.data.owner.forward * self.data.sightDistance;
            Vector3 rightDir = rightRay * self.data.owner.forward * self.data.sightDistance;
            
            Gizmos.DrawRay(origin, leftDir);
            Gizmos.DrawRay(origin, rightDir);
            Handles.DrawWireArc(origin, Vector3.up, leftDir, self.data.fieldOfViewAngle, self.data.sightDistance);
            Gizmos.DrawWireSphere(origin, self.data.sightDistance);
            
            if (self.LastDetectedTarget)
            {
                Gizmos.DrawLine(origin, self.LastDetectedTarget.position + Vector3.up * self.data.eyeHeight);
            }
            else if (self.data.targets != null)
            {
                foreach (var target in self.data.targets)
                {
                    if (target) Gizmos.DrawLine(origin, target.position);
                }
            }
            
            if (self.LastHitPoint.HasValue)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(self.LastHitPoint.Value, 0.1f);
            }
            
            Handles.Label(origin + Vector3.up * 0.5f, 
                $"状态: {(self.LastCheckResult ? "检测到目标" : "未检测")}\n" +
                $"目标: {(self.LastDetectedTarget ? self.LastDetectedTarget.name : "<无>")}",
                new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = gizmoColor }
                });
        }
    }
}

#endif