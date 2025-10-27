#if UNITY_EDITOR

namespace VerveEditor
{
    using UnityEditor;
    using Verve.UniEx;

    
    /// <summary>
    ///  <para>流程节点包装器编辑</para>
    /// </summary>
    [CustomEditor(typeof(GameFlowNodeWrapper))]
    internal class GameFlowNodeWrapperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This is a wrapper for IGameFlowNode. Do not edit this object.", MessageType.Warning);
        }
    }
}

#endif