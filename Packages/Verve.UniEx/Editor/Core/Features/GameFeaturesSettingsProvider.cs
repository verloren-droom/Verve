#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using System.Linq;
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using UnityEngine.UIElements;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    internal class GameFeaturesSettingsProvider : SettingsProvider
    {
        private Editor m_Editor;


        public GameFeaturesSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            GameFeaturesSettings.instance.Save();
            m_Editor = Editor.CreateEditor(GameFeaturesSettings.instance);
        }

        public override void OnGUI(string searchContext)
        {
            m_Editor?.OnInspectorGUI();
        }

        [SettingsProvider]
        public static SettingsProvider CreateGameFeaturesSettingsProvider()
        {
            return new GameFeaturesSettingsProvider("Project/Verve/Game Features", SettingsScope.Project);
        }
    }
}

#endif