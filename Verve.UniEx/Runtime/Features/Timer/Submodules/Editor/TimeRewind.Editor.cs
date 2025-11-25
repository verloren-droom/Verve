#if UNITY_EDITOR

namespace Verve.UniEx.Timer
{
    using UnityEditor;
    using UnityEngine;
    
    
    partial class TimeRewind : IDrawableSubmodule
    {
        void IDrawableSubmodule.DrawGUI() { }

        void IDrawableSubmodule.DrawGizmos()
        {
            GUIStyle objectLabelStyle = new GUIStyle
            {
                normal = { textColor = Color.blue },
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            
            int index = 0;
            foreach (var kvp in m_Recorders)
            {
                var rewindable = kvp.Key;
                var recorder = kvp.Value;
                
                if (rewindable == null) continue;
                
                Vector3 objectPos = Vector3.right * (index + 1) + Vector3.forward * CurrentRewindTime;
                string objectType = rewindable.GetType().Name;

                if (rewindable is MonoBehaviour mb && mb.gameObject != null)
                {
                    objectPos = mb.transform.position;
                    objectType = mb.gameObject.name;
                }
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(objectPos, 0.1f);
                
                string snapshotInfo = $"快照数: {recorder.SnapshotCount}/{recorder.BufferCapacity}";
                string timeRange = $"时间范围: [{recorder.OldestTimestamp:F2}s, {recorder.NewestTimestamp:F2}s]";
                
                Handles.Label(objectPos + Vector3.up * 0.2f,
                    $"[{index}] {objectType}\n{snapshotInfo}\n{timeRange}",
                    objectLabelStyle);
                    
                index++;
            }
        }
    }
}

#endif