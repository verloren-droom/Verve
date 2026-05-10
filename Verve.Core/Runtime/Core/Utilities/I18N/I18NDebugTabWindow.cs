#if DEBUG && UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using UnityEngine.Scripting;
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>多语言调试栏窗口</para>
    /// </summary>
    [Preserve, DebugItem("I18N")]
    sealed class I18NDebugTabWindow : DebugTabWindow
    {
        private Vector2 m_ScrollPosition;
        private string m_Search = "";
        private bool m_ShowLocal = true;
        private bool m_ShowLookup = true;

        [Preserve]
        public I18NDebugTabWindow(DebugTabWindowSettings settings) : base(settings) { }

        public override void OnShow() { }

        public override void OnHide() { }

        public override void Draw()
        {
            DrawToolbar();
            DrawTables();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Language", GUILayout.Width(80));
            var languageOptions = Game.GetAvailableLanguages();
            if (languageOptions.Length > 0)
            {
                var current = Game.Language;
                var selected = Array.IndexOf(languageOptions, current);
                if (selected < 0) selected = 0;
                var next = GUILayout.Toolbar(selected, languageOptions, GUILayout.MinWidth(160));
                if (next != selected && next >= 0 && next < languageOptions.Length)
                {
                    Game.Language = languageOptions[next];
                }
            }
            else
            {
                GUILayout.Label(Game.Language ?? "(null)");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            var lookup = Game.I18NLookup;
            GUILayout.Label($"Lookup: {lookup?.GetType().Name ?? "null"}");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Local Tables: {Game.LocalTableCount}");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_ShowLocal = GUILayout.Toggle(m_ShowLocal, "Local", GUILayout.Width(80));
            m_ShowLookup = GUILayout.Toggle(m_ShowLookup, "Lookup", GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            m_Search = GUILayout.TextField(m_Search ?? "", GUILayout.Width(260));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawTables()
        {
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            if (m_ShowLocal)
            {
                DrawTable("Local Table", Game.CurrentI18NTable);
            }
            if (m_ShowLookup)
            {
                DrawTable("Lookup Table", Game.CurrentLookupTable);
            }
            GUILayout.EndScrollView();
        }

        private void DrawTable(string title, IReadOnlyDictionary<string, string> table)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"{title} ({table?.Count ?? 0})");
            if (table != null)
            {
                foreach (var kv in table)
                {
                    if (!PassSearch(kv.Key, kv.Value)) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(kv.Key ?? "", GUILayout.Width(240));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(kv.Value ?? "");
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        private bool PassSearch(string key, string value)
        {
            if (string.IsNullOrEmpty(m_Search)) return true;
            var filter = m_Search;
            if (key != null && key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (value != null && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}

#endif