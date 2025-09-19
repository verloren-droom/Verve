#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    internal sealed class GameFeaturesSettingsProvider : SettingsProvider
    {
        private GameFeaturesSettings m_Settings;
        private Editor m_SettingsEditor;

        
        public GameFeaturesSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            
            m_Settings = GameFeaturesSettings.GetOrCreateSettings();
            m_SettingsEditor = Editor.CreateEditor(m_Settings);
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            
            m_SettingsEditor.OnInspectorGUI();
        }


        [SettingsProvider]
        public static SettingsProvider CreateGameFeaturesSettingsProvider()
        {
            var provider = new GameFeaturesSettingsProvider("Project/Verve/Game Features", SettingsScope.Project);
            provider.keywords = new string[] { "Game", "Features", "Verve" };
            return provider;
        }
    }
}

#endif