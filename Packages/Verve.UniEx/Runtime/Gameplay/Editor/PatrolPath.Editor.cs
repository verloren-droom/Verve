using System;

#if UNITY_EDITOR

namespace VerveUniEx.Gameplay
{
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.SceneManagement;
    
    
    [DisallowMultipleComponent, ExecuteAlways, AddComponentMenu("Verve/Gameplay/PatrolPath")]
    public partial class PatrolPath
    {
        [SerializeField, Tooltip("路径颜色")] private Color m_PathColor = new Color(0.2f, 0.8f, 0.4f, 0.7f);

        
        private void OnDrawGizmosSelected()
        {
            DrawPathGizmos();
        }
        
        private void OnDrawGizmos()
        {
            if (!Selection.Contains(gameObject))
                DrawPathGizmos();
        }
        
        private void DrawPathGizmos()
        {
            if (m_Points == null || m_Points.Count == 0) return;
            
            Gizmos.color = m_PathColor;
            for (int i = 0; i < m_Points.Count; i++)
            {
                Vector3 worldPos = GetWorldPosition(i);
                
                Gizmos.DrawSphere(worldPos, 0.1f);
                
                Gizmos.DrawWireSphere(worldPos, m_Points[i].Radius);
                
                if (i > 0)
                {
                    Vector3 prevPos = GetWorldPosition(i-1);
                    Gizmos.DrawLine(prevPos, worldPos);
                }
                
                Handles.Label(worldPos + Vector3.up * 0.3f, 
                    $"点 {i}\n等待: {m_Points[i].WaitTime}s",
                    new GUIStyle { 
                        normal = { textColor = m_PathColor }, 
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter 
                    });
            }
            
            if (m_Loop && m_Points.Count > 1)
            {
                Vector3 first = GetWorldPosition(0);
                Vector3 last = GetWorldPosition(m_Points.Count - 1);
                Gizmos.DrawLine(last, first);
            }
        }
        
        [MenuItem("GameObject/Verve/Gameplay/PatrolPath", false, 10)]
        private static void CreatePatrolPath(MenuCommand command)
        {
            var go = new GameObject("PatrolPath");
            try
            {
                var path = go.AddComponent<PatrolPath>();
                
                path.AddPoint(new Vector3(-1, 0, 0));
                path.AddPoint(new Vector3(1, 0, 0));
                
                GameObject parent = null;
                if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    parent = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                }
                else if (command.context is GameObject context)
                {
                    parent = context;
                }
                else
                {
                    parent = Selection.activeGameObject;
                }
                
                GameObjectUtility.SetParentAndAlign(go, parent);
                Undo.RegisterCreatedObjectUndo(go, "Create Patrol Path");
                Selection.activeObject = go;
            }
            catch
            {
                DestroyImmediate(go);
            }
        }
    }
}

#endif