#if UNITY_EDITOR

namespace VerveEditor.Gameplay
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx.Gameplay;
    
    
    /// <summary>
    ///   <para>巡逻点编辑器</para>
    /// </summary>
    [CustomEditor(typeof(PatrolPath))]
    public class PatrolPathEditor : Editor
    {
        private const float HandleSize = 0.15f;
        private static Color PointColor = new Color(0.9f, 0.3f, 0.1f, 1f);
        private static Color SelectedPointColor = new Color(1f, 0.5f, 0f, 1f);
        
        private PatrolPath m_Path;
        private Transform m_Transform;
        private int m_SelectedIndex = -1;
        
        private void OnEnable()
        {
            m_Path = target as PatrolPath;
            m_Transform = m_Path.transform;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Loop"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PathColor"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("巡逻点列表", EditorStyles.boldLabel);
            
            var pointsProp = serializedObject.FindProperty("m_Points");
            for (int i = 0; i < pointsProp.arraySize; i++)
            {
                EditorGUILayout.BeginVertical("box");
                bool isSelected = i == m_SelectedIndex;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"巡逻点 {i}", isSelected ? EditorStyles.boldLabel : EditorStyles.label);
                
                if (GUILayout.Button("选择", GUILayout.Width(60)))
                {
                    m_SelectedIndex = i;
                    SceneView.RepaintAll();
                }
                
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    Undo.RecordObject(m_Path, "Remove Patrol Point");
                    m_Path.RemovePoint(i);
                    if (m_SelectedIndex == i) m_SelectedIndex = -1;
                    EditorUtility.SetDirty(m_Path);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                
                var pointProp = pointsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(pointProp.FindPropertyRelative("Position"));
                EditorGUILayout.PropertyField(pointProp.FindPropertyRelative("WaitTime"));
                EditorGUILayout.PropertyField(pointProp.FindPropertyRelative("Radius"));
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("添加巡逻点"))
            {
                Undo.RecordObject(m_Path, "Add Patrol Point");
                Vector3 position = m_Path.Points.Count > 0 
                    ? m_Path.Points.Last().Position 
                    : Vector3.zero;
                m_Path.AddPoint(position);
                EditorUtility.SetDirty(m_Path);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void OnSceneGUI()
        {
            if (m_Path == null || m_Path.Points == null) return;
            
            for (int i = 0; i < m_Path.Points.Count; i++)
            {
                var point = m_Path.Points[i];
                Vector3 worldPos = m_Transform.TransformPoint(point.Position);
                
                Handles.color = i == m_SelectedIndex ? SelectedPointColor : PointColor;
                float size = HandleUtility.GetHandleSize(worldPos) * HandleSize;
                if (Handles.Button(worldPos, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    m_SelectedIndex = i;
                    Repaint();
                }
                
                Handles.DrawWireDisc(worldPos, Vector3.up, point.Radius);
                
                Handles.Label(worldPos + Vector3.up * 0.3f, 
                    $"点 {i}\n等待: {point.WaitTime}s",
                    new GUIStyle { 
                        normal = { textColor = Handles.color }, 
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter 
                    });
                
                if (i > 0)
                {
                    Vector3 prevPos = m_Transform.TransformPoint(m_Path.Points[i-1].Position);
                    Handles.DrawLine(prevPos, worldPos);
                }
            }
            
            if (m_Path.Loop && m_Path.Points.Count > 1)
            {
                Vector3 first = m_Transform.TransformPoint(m_Path.Points[0].Position);
                Vector3 last = m_Transform.TransformPoint(m_Path.Points.Last().Position);
                Handles.DrawLine(last, first);
            }
            
            if (m_SelectedIndex >= 0 && m_SelectedIndex < m_Path.Points.Count)
            {
                var point = m_Path.Points[m_SelectedIndex];
                Vector3 worldPos = m_Transform.TransformPoint(point.Position);
                
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_Path, "Move Patrol Point");
                    point.Position = m_Transform.InverseTransformPoint(newPos);
                    EditorUtility.SetDirty(m_Path);
                }
            }
        }
    }
}

#endif
