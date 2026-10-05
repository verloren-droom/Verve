#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///   <para>流程资产编辑；流程和步骤参数都在独立窗口中编辑。</para>
    /// </summary>
    [CustomEditor(typeof(GameFlowAsset))]
    sealed class GameFlowAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("打开流程编辑器", GUILayout.Height(32f)))
                GameFlowEditorWindow.Open((GameFlowAsset)target);
        }
    }
}

#endif