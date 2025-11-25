#if UNITY_EDITOR

namespace VerveEditor.AI
{
    using Verve.AI;
    using UnityEngine;
    using UnityEditor;
    using Verve.UniEx.AI;
    
    
    public static class VectorDistanceConditionBTNodeExtension
    {
        public static void DrawGizmos(this ref VectorDistanceConditionBTNode self, ref BTNodeDebugContext ctx)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(self.data.ownerPoint, 0.2f);
            Gizmos.DrawSphere(self.data.targetPoint, 0.2f);
            
            Gizmos.color = self.LastResult == BTNodeResult.Succeeded ? Color.green : Color.red;
            Gizmos.DrawLine(self.data.ownerPoint, self.data.targetPoint);
            
            float distance = Vector3.Distance(self.data.ownerPoint, self.data.targetPoint);
            string statusText = self.LastResult == BTNodeResult.Succeeded ? "Success" : "Failure";
            string comparison = self.data.compareMode == VectorDistanceConditionBTNodeData.Comparison.LessThanOrEqual 
                ? "<=" : ">";
            
            Vector3 midPoint = (self.data.ownerPoint + self.data.targetPoint) * 0.5f;
            Handles.Label(midPoint, 
                $"距离: {distance:F1}/{self.data.checkDistance:F1}\n" +
                $"条件: {comparison}\n" +
                $"状态: {statusText}",
                new GUIStyle { 
                    normal = { textColor = Color.white }, 
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                });
            
            if (self.data.compareMode == VectorDistanceConditionBTNodeData.Comparison.LessThanOrEqual)
            {
                Handles.color = new Color(0, 1, 0, 0.1f);
                Handles.DrawSolidDisc(self.data.ownerPoint, Vector3.up, self.data.checkDistance);
            }
        }
    }
}

#endif