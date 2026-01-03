#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    

    /// <summary>
    ///   <para>游戏功能设置编辑器</para>
    /// </summary>
    [CustomEditor(typeof(GameFeaturesSettings))]
    internal sealed class GameFeaturesSettingsEditor : Editor
    {
        private GameFeaturesSettings m_Settings;

        /// <summary>
        ///   <para>排除绘制的字段</para>
        /// </summary>
        private static readonly string[] s_ExcludedFields =
        {
            "m_Script",
        };

        private void OnEnable()
        {
            m_Settings = target as GameFeaturesSettings;
            // if (m_Settings == null || target == null) return;
        }

        public override void OnInspectorGUI()
        {
            if (m_Settings == null || target == null) return;

            DrawPropertiesExcluding(serializedObject, s_ExcludedFields);
            
            using var change = new EditorGUI.ChangeCheckScope();
            if (change.changed)
            {
                serializedObject.ApplyModifiedProperties();
                GameFeaturesSettings.instance.Save();
            }
        }
    }
}

#endif