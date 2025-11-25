#if UNITY_EDITOR

namespace VerveEditor
{
    using UnityEditor;
    using UnityEngine.UIElements;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    /// <summary>
    ///   <para>游戏功能设置提供者</para>
    /// </summary>
    internal sealed class GameFeaturesSettingsProvider : SettingsProvider
    {
        private GameFeaturesSettingsEditor m_Editor;


        public GameFeaturesSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            GameFeaturesSettings.instance.Save();
            m_Editor = Editor.CreateEditor(GameFeaturesSettings.instance, typeof(GameFeaturesSettingsEditor)) as GameFeaturesSettingsEditor;
        }

        public override void OnGUI(string searchContext)
        {
            m_Editor?.OnInspectorGUI();
        }

        /// <summary>
        ///   <para>创建游戏功能设置</para>
        /// </summary>
        /// <returns></returns>
        [SettingsProvider]
        public static SettingsProvider CreateGameFeaturesSettingsProvider()
        {
            return new GameFeaturesSettingsProvider("Project/Verve/Game Features", SettingsScope.Project);
        }
    }
}

#endif