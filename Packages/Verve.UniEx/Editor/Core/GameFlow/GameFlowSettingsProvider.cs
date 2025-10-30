#if UNITY_EDITOR

namespace VerveEditor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    
    
    /// <summary>
    ///   <para>游戏流程设置</para>
    /// </summary>
    internal class GameFlowSettingsProvider : SettingsProvider
    {
        public GameFlowSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }
        
        public override void OnGUI(string searchContext)
        {
            GUILayout.Space(5);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Game Flow Editor", EditorStyles.boldLabel);
    
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 11;
                buttonStyle.fixedHeight = 22;
                buttonStyle.margin = new RectOffset(0, 0, 0, 0);
                
                if (GUILayout.Button("Open Graph View", buttonStyle))
                {
                    GameFlowGraphEditorWindow.OpenWindow();
                }
            }
        }
        
        /// <summary>
        ///   <para>创建游戏流程设置</para>
        /// </summary>
        /// <returns></returns>
        [SettingsProvider]
        public static SettingsProvider CreateGameFeaturesSettingsProvider()
        {
            return new GameFlowSettingsProvider("Project/Verve/Game Flow", SettingsScope.Project);
        }
    }
}

#endif